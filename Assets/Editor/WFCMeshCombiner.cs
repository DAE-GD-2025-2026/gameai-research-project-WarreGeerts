// WFCMeshCombiner.cs
// Place in Assets/Editor/WFCMeshCombiner.cs
// Menu: Tools > WFC > Combine Tile Meshes
//
// Select your root prefab/GameObject (TileSet), run the tool.
// Each direct child (Floor, Gate_f, Wall_f, …) gets all its descendant
// meshes merged into ONE mesh, saved as an asset, and the child is cleaned
// up so it has only a MeshFilter + MeshRenderer — ready for WFCSocketAnalyzer.

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class WFCMeshCombiner : EditorWindow
{
    // ── Window state ─────────────────────────────
    private GameObject _root;
    private string     _meshSaveFolder = "Assets/WFC/CombinedMeshes";
    private bool       _keepOriginals  = true;   // back up original hierarchy?

    [MenuItem("Tools/WFC/Combine Tile Meshes")]
    public static void ShowWindow()
        => GetWindow<WFCMeshCombiner>("WFC Mesh Combiner");

    private void OnGUI()
    {
        GUILayout.Label("WFC Mesh Combiner", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        _root = (GameObject)EditorGUILayout.ObjectField(
            "Root GameObject (TileSet)", _root, typeof(GameObject), true);

        _meshSaveFolder = EditorGUILayout.TextField("Mesh Save Folder", _meshSaveFolder);
        _keepOriginals  = EditorGUILayout.Toggle("Keep backup copy", _keepOriginals);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Each direct child of the root will have ALL its descendant meshes merged " +
            "into one combined mesh. The child will be left with only a MeshFilter + " +
            "MeshRenderer. Meshes are saved to disk so they survive domain reloads.",
            MessageType.Info);

        EditorGUILayout.Space();
        GUI.enabled = _root != null;
        if (GUILayout.Button("Combine Meshes"))
            Run();
        GUI.enabled = true;
    }

    // ── Main ─────────────────────────────────────
    private void Run()
    {
        // Make sure the save folder exists
        if (!AssetDatabase.IsValidFolder(_meshSaveFolder))
            CreateFolderRecursive(_meshSaveFolder);

        // Work on scene instance — if user handed us a project asset, instantiate it
        bool instantiated = false;
        GameObject root = _root;
        if (PrefabUtility.IsPartOfPrefabAsset(_root))
        {
            root = (GameObject)PrefabUtility.InstantiatePrefab(_root);
            instantiated = true;
        }

        // Optional backup
        if (_keepOriginals)
        {
            GameObject backup = Instantiate(root);
            backup.name = root.name + "_backup";
            backup.SetActive(false);
            Undo.RegisterCreatedObjectUndo(backup, "WFC backup");
        }

        int count = 0;
        // Iterate over direct children only
        List<Transform> children = new List<Transform>();
        foreach (Transform t in root.transform)
            children.Add(t);

        foreach (Transform child in children)
        {
            CombineChildIntoSelf(child.gameObject);
            count++;
        }

        // If we instantiated a project asset, write it back as a prefab variant
        if (instantiated)
        {
            string prefabPath = AssetDatabase.GetAssetPath(_root);
            if (string.IsNullOrEmpty(prefabPath))
                prefabPath = $"Assets/WFC/{root.name}_combined.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.UserAction);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("WFC Mesh Combiner",
            $"Done! Combined {count} tile(s).\nMeshes saved to: {_meshSaveFolder}", "OK");
    }

    // ── Per-tile logic ────────────────────────────
    private void CombineChildIntoSelf(GameObject tile)
    {
        MeshFilter[] filters = tile.GetComponentsInChildren<MeshFilter>(true);
        if (filters.Length == 0) return;

        // --- Build CombineInstance list in the tile's local space ---
        var combines = new List<CombineInstance>();
        Material sharedMat = null;

        foreach (MeshFilter mf in filters)
        {
            if (mf.sharedMesh == null) continue;

            // World → tile-local matrix
            Matrix4x4 mat = tile.transform.worldToLocalMatrix
                            * mf.transform.localToWorldMatrix;

            // Grab all sub-meshes individually so triangles from every
            // material slot are included even on multi-material meshes
            Mesh mesh = mf.sharedMesh;
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                combines.Add(new CombineInstance
                {
                    mesh            = mesh,
                    subMeshIndex    = s,
                    transform       = mat
                });
            }

            // Grab the first valid material we find for the combined renderer
            if (sharedMat == null)
            {
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr != null && mr.sharedMaterials.Length > s_Zero)
                    sharedMat = mr.sharedMaterials[0];
            }
        }

        if (combines.Count == 0) return;

        // --- Merge ---
        Mesh combined = new Mesh();
        combined.name = tile.name + "_combined";
        combined.CombineMeshes(combines.ToArray(), mergeSubMeshes: true, useMatrices: true);
        combined.RecalculateBounds();

        // --- Recenter vertices on the mesh pivot ---
        // After combining, the AABB centre is often not at (0,0,0) because child
        // transforms were offset (e.g. tile sits at y=0..1 instead of y=-0.5..0.5).
        // Shifting every vertex so the bounds centre is at the local origin means
        // WFCSocketAnalyzer will correctly find vertices at exactly ±(tileSize/2).
        Vector3 boundsOffset = combined.bounds.center;
        if (boundsOffset.sqrMagnitude > 0.0001f)
        {
            Vector3[] verts = combined.vertices;
            for (int vi = 0; vi < verts.Length; vi++)
                verts[vi] -= boundsOffset;
            combined.vertices = verts;
            combined.RecalculateBounds();
        }

        combined.RecalculateNormals();
        combined.RecalculateTangents();
        combined.Optimize();

        // --- Save mesh asset ---
        string meshPath = $"{_meshSaveFolder}/{combined.name}.asset";
        // Overwrite if exists
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(combined, existing);
            combined = existing;
        }
        else
        {
            AssetDatabase.CreateAsset(combined, meshPath);
        }

        // --- Destroy all children of the tile ---
        // Collect first because DestroyImmediate modifies the hierarchy
        var childList = new List<GameObject>();
        foreach (Transform t in tile.transform)
            childList.Add(t.gameObject);
        foreach (var go in childList)
            DestroyImmediate(go);

        // --- Remove any stale MeshFilter / MeshRenderer on the tile itself ---
        // The tile may already have its own mesh components (it was a mesh object too).
        // Destroying before adding ensures Unity fully initialises the new instances
        // before we assign sharedMesh — prevents the MissingComponentException.
        var oldMf = tile.GetComponent<MeshFilter>();
        if (oldMf != null) DestroyImmediate(oldMf);
        var oldMr = tile.GetComponent<MeshRenderer>();
        if (oldMr != null) DestroyImmediate(oldMr);

        // --- Add fresh components then assign ---
        MeshFilter   mfOut = tile.AddComponent<MeshFilter>();
        MeshRenderer mrOut = tile.AddComponent<MeshRenderer>();

        mfOut.sharedMesh     = combined;
        mrOut.sharedMaterial = sharedMat;

        // Reset tile transform so it sits at origin within the root
        tile.transform.localPosition = Vector3.zero;
        tile.transform.localRotation = Quaternion.identity;
        tile.transform.localScale    = Vector3.one;
    }

    // ── Folder helper ─────────────────────────────
    private static void CreateFolderRecursive(string path)
    {
        // e.g. "Assets/WFC/CombinedMeshes"
        string[] parts  = path.Split('/');
        string   current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static readonly int s_Zero = 0; // tiny trick to avoid magic-number warning
}
#endif