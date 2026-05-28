// WFCSocketAnalyzer.cs  — place in Assets/Editor/
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class WFCSocketAnalyzer : EditorWindow
{
    // ── Tuning ────────────────────────────────────────────────────────────────
    // SNAP: vertex positions are rounded to this grid before comparison.
    // Keeps floating-point noise from creating spurious socket splits.
    private const float SNAP = 0.005f;

    // ── Window state ──────────────────────────────────────────────────────────
    private GameObject _target;
    private string     _outputPath = "Assets/WFC/tiles.json";
    private bool       _appendMode = true;
    private float      _tileSize   = 1.0f;   // world-space edge length of one grid cell
    private float      _faceThresh = 0.02f;  // how close (world units) a vertex must be to a face plane

    [MenuItem("Tools/WFC/Analyze Sockets")]
    public static void ShowWindow() => GetWindow<WFCSocketAnalyzer>("WFC Socket Analyzer");

    private void OnGUI()
    {
        GUILayout.Label("WFC Socket Analyzer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        _target      = (GameObject)EditorGUILayout.ObjectField(
            "Target Prefab / GameObject", _target, typeof(GameObject), true);
        _outputPath  = EditorGUILayout.TextField("Output JSON Path",   _outputPath);
        _tileSize    = EditorGUILayout.FloatField("Tile Size (world units)", _tileSize);
        _faceThresh  = EditorGUILayout.FloatField("Face Threshold (world units)", _faceThresh);
        _appendMode  = EditorGUILayout.Toggle("Append to existing JSON", _appendMode);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "All meshes are compared in the same RAW local space — no normalisation. " +
            "Face planes sit at ±(TileSize/2). Vertices within Face Threshold of a plane " +
            "are projected and used to build the socket pattern. " +
            "Because every tile shares the same coordinate space, matching works correctly " +
            "across tiles of different shapes (floor, wall, corner, etc.).\n\n" +
            "Requirements: each tile's pivot must be at its grid-cell centre, and vertices " +
            "must reach the face planes (i.e. the mesh fills its cell to the boundary).",
            MessageType.Info);

        EditorGUILayout.Space();
        GUI.enabled = _target != null;
        if (GUILayout.Button("Analyze & Export")) AnalyzeAndExport();
        GUI.enabled = true;
    }

    // ── Main ──────────────────────────────────────────────────────────────────
    private void AnalyzeAndExport()
    {
        MeshFilter[] filters = _target.GetComponentsInChildren<MeshFilter>(true);
        if (filters.Length == 0)
        {
            EditorUtility.DisplayDialog("WFC", "No MeshFilters found.", "OK");
            return;
        }

        TileLibrary    lib = LoadLibrary(_outputPath);
        SocketRegistry reg = new SocketRegistry();

        // Pass 1 — register all face patterns
        var pending = new List<(TileEntry entry, int[] fi)>();
        foreach (MeshFilter mf in filters)
        {
            var (entry, fi) = AnalyzeMesh(mf, reg);
            lib.tiles.RemoveAll(t => t.name == entry.name);
            lib.tiles.Add(entry);
            pending.Add((entry, fi));
        }

        // Pass 2 — resolve symmetry/flip relationships
        reg.ResolveSymmetry(SNAP * 2f);

        // Pass 3 — write final string labels
        foreach (var (entry, fi) in pending)
            PatchEntry(entry, fi, reg);

        SaveLibrary(lib, _outputPath);
        EditorUtility.DisplayDialog("WFC",
            $"Done!  {filters.Length} tile(s) written.\n" +
            $"Unique socket patterns: {reg.Count}\n" +
            $"Output: {_outputPath}", "OK");
        AssetDatabase.Refresh();
    }

    // ── Per-mesh analysis ─────────────────────────────────────────────────────
    private (TileEntry entry, int[] fi) AnalyzeMesh(MeshFilter mf, SocketRegistry reg)
    {
        // Work in the mesh's own LOCAL space.
        // The combiner already recentred vertices so the AABB is centred on (0,0,0).
        // Face planes are therefore at exactly ±half in each axis.
        Vector3[] verts = mf.sharedMesh.vertices;
        float     half  = _tileSize * 0.5f;

        // Six face planes: +X −X +Y −Y +Z −Z
        (int axis, float plane, bool vertical)[] faces =
        {
            (0,  half, false),
            (0, -half, false),
            (1,  half, true),
            (1, -half, true),
            (2,  half, false),
            (2, -half, false),
        };

        int[] fi = new int[6];
        for (int i = 0; i < 6; i++)
        {
            var (axis, plane, vertical) = faces[i];
            List<Vector2> pts = GatherFaceVerts(verts, axis, plane, _faceThresh);

            if (pts.Count == 0) { fi[i] = -1; continue; }

            List<Vector2> canon = Canonicalise(pts, SNAP);
            fi[i] = reg.Register(canon, vertical);
        }

        return (new TileEntry { name = MeshKey(mf) }, fi);
    }

    // ── Gather verts that lie on a face plane ─────────────────────────────────
    // Returns their 2-D position in the plane's local UV space.
    private static List<Vector2> GatherFaceVerts(
        Vector3[] verts, int axis, float plane, float thresh)
    {
        var result = new List<Vector2>();
        foreach (var v in verts)
        {
            float coord = axis == 0 ? v.x : axis == 1 ? v.y : v.z;
            if (Mathf.Abs(coord - plane) > thresh) continue;

            // Project onto the 2 axes perpendicular to the face normal.
            // We keep a consistent winding so that mirroring detection works:
            //   X face → (Z, Y)
            //   Y face → (X, Z)
            //   Z face → (X, Y)
            switch (axis)
            {
                case 0: result.Add(new Vector2(v.z, v.y)); break;
                case 1: result.Add(new Vector2(v.x, v.z)); break;
                default:result.Add(new Vector2(v.x, v.y)); break;
            }
        }
        return result;
    }

    // ── Canonicalise: snap → dedupe → sort ────────────────────────────────────
    private static List<Vector2> Canonicalise(List<Vector2> pts, float snap)
        => pts
            .Select(p => new Vector2(
                Mathf.Round(p.x / snap) * snap,
                Mathf.Round(p.y / snap) * snap))
            .Distinct(new V2Eq(snap * 0.5f))
            .OrderBy(p => p.x).ThenBy(p => p.y)
            .ToList();

    // ── Label assignment ──────────────────────────────────────────────────────
    private void PatchEntry(TileEntry e, int[] fi, SocketRegistry reg)
    {
        e.px = Lbl(fi[0], false, reg); e.nx = Lbl(fi[1], false, reg);
        e.py = Lbl(fi[2], true,  reg); e.ny = Lbl(fi[3], true,  reg);
        e.pz = Lbl(fi[4], false, reg); e.nz = Lbl(fi[5], false, reg);
    }

    private static string Lbl(int idx, bool vert, SocketRegistry reg)
    {
        if (idx < 0) return "-1";
        var r = reg[idx];
        if (vert)        return $"v{r.id}_{r.rots}";
        if (r.symmetric) return $"{r.id}s";
        if (r.flipped)   return $"{r.id}f";
        return $"{r.id}";
    }

    // ── Utilities ─────────────────────────────────────────────────────────────
    private static string MeshKey(MeshFilter mf)
    {
        string n = PrefabUtility.GetCorrespondingObjectFromSource(mf.gameObject)?.name;
        return string.IsNullOrEmpty(n) ? mf.sharedMesh.name : n;
    }

    private TileLibrary LoadLibrary(string p)
    {
        if (_appendMode && File.Exists(p))
            try { return JsonUtility.FromJson<TileLibrary>(File.ReadAllText(p)); } catch { }
        return new TileLibrary { tiles = new List<TileEntry>() };
    }

    private static void SaveLibrary(TileLibrary lib, string p)
    {
        string d = Path.GetDirectoryName(p);
        if (!Directory.Exists(d)) Directory.CreateDirectory(d);
        File.WriteAllText(p, JsonUtility.ToJson(lib, true));
    }

    // ── Socket registry ───────────────────────────────────────────────────────
    private class SocketRegistry
    {
        public struct Rec
        {
            public int id;
            public List<Vector2> pattern;
            public bool vertical, symmetric, flipped;
            public int rots;
        }

        private readonly List<Rec> _recs = new List<Rec>();
        public int  Count      => _recs.Count;
        public Rec  this[int i] => _recs[i];

        public int Register(List<Vector2> pat, bool vert)
        {
            for (int i = 0; i < _recs.Count; i++)
                if (_recs[i].vertical == vert && Same(pat, _recs[i].pattern, SNAP * 2f))
                    return i;
            int id = _recs.Count;
            _recs.Add(new Rec { id = id, pattern = pat, vertical = vert });
            return id;
        }

        public void ResolveSymmetry(float tol)
        {
            for (int i = 0; i < _recs.Count; i++)
            {
                var r = _recs[i];
                if (r.vertical)
                {
                    r.rots = RotsToSelf(r.pattern, tol);
                }
                else
                {
                    var mir = Mirror(r.pattern);
                    if (Same(r.pattern, mir, tol))
                    {
                        r.symmetric = true;
                    }
                    else
                    {
                        int twin = -1;
                        for (int j = 0; j < _recs.Count; j++)
                            if (i != j && !_recs[j].vertical && Same(_recs[j].pattern, mir, tol))
                            { twin = j; break; }

                        if (twin >= 0)
                        {
                            if (i < twin)
                            {
                                var t = _recs[twin]; t.flipped = true; _recs[twin] = t;
                            }
                            else r.flipped = true;
                        }
                    }
                }
                _recs[i] = r;
            }
        }

        private static bool Same(List<Vector2> a, List<Vector2> b, float tol)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (Vector2.Distance(a[i], b[i]) > tol) return false;
            return true;
        }

        private static List<Vector2> Mirror(List<Vector2> p)
            => p.Select(v => new Vector2(-v.x, v.y))
                .OrderBy(v => v.x).ThenBy(v => v.y).ToList();

        private static List<Vector2> Rot90(List<Vector2> p)
            => p.Select(v => new Vector2(-v.y, v.x))
                .OrderBy(v => v.x).ThenBy(v => v.y).ToList();

        private static int RotsToSelf(List<Vector2> p, float tol)
        {
            var c = p;
            for (int r = 1; r <= 3; r++) { c = Rot90(c); if (Same(p, c, tol)) return r; }
            return 0;
        }
    }

    // ── Serialisable types ────────────────────────────────────────────────────
    [Serializable] private class TileLibrary { public List<TileEntry> tiles = new List<TileEntry>(); }

    [Serializable]
    private class TileEntry
    {
        public string name, px, nx, py, ny, pz, nz;
    }

    private class V2Eq : IEqualityComparer<Vector2>
    {
        readonly float _t;
        public V2Eq(float t) { _t = t; }
        public bool Equals(Vector2 a, Vector2 b) => Vector2.Distance(a, b) <= _t;
        public int GetHashCode(Vector2 v)
        {
            int x = Mathf.RoundToInt(v.x / _t), y = Mathf.RoundToInt(v.y / _t);
            return x * 397 ^ y;
        }
    }
}
#endif