using System.Collections.Generic;
using UnityEngine;

public class WFCCell3D : MonoBehaviour
{
    [Header("Side Id's")]
    [SerializeField] private EdgeID yP; 
    [SerializeField] private EdgeID yN;
    [SerializeField] private EdgeID zP; 
    [SerializeField] private EdgeID zN; 
    [SerializeField] private EdgeID xP; 
    [SerializeField] private EdgeID xN; 
    [Header("Weight")]
    [SerializeField] private float weight = 1;
    
    [Header("Negative Constraints")]
    [SerializeField] private List<DirectionalExclusion> excludedNeighbors = new List<DirectionalExclusion>();
    [Header("don't touch")]
    public List<TileCandidate> possibleTiles = new List<TileCandidate>();

    private bool _isCollapsed;
    private TileCandidate _collapsedTile;

    public bool IsCollapsed => _isCollapsed;
    public TileCandidate CollapsedTile => _collapsedTile;

    public EdgeID Yp => yP;
    public EdgeID Yn => yN;
    public EdgeID Zp => zP;
    public EdgeID Zn => zN;
    public EdgeID Xp => xP;
    public EdgeID Xn => xN;

    public bool IsPrefabExcluded(GameObject prefab, Vector3Int localDir)
    {
        for (int i = 0; i < excludedNeighbors.Count; i++)
        {
            if (excludedNeighbors[i].direction == localDir)
            {
                if (excludedNeighbors[i].forbiddenPrefabs.Contains(prefab))
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    public int GetEntropy()
    {
        return possibleTiles.Count;
    }

    public void CollapseCell()
    {
        //contradiction
        if (possibleTiles.Count == 0) return;
        
        //total weight var
        float totalWeight = 0f;
        //get the total weight of every tile
        for (int i = 0; i < possibleTiles.Count; i++)
        {
            totalWeight += possibleTiles[i].cellData.weight;
        }
        //select a random point in the range of 0 to the total weight
        float randomWeightPoint = Random.Range(0f, totalWeight);
        //fallback
        _collapsedTile = possibleTiles[0]; 
        //for every possible tile check if the random weight is smaller than the cell weight,
        //else subtract the weight from the random weight
        for (int i = 0; i < possibleTiles.Count; i++)
        {
            if (randomWeightPoint < possibleTiles[i].cellData.weight)
            {
                _collapsedTile = possibleTiles[i];
                break;
            }
            randomWeightPoint -= possibleTiles[i].cellData.weight;
        }

        /*//take a random outside the possible tiles
        var rand = Random.Range(0, possibleTiles.Count);
        //set the collapsed tile equal to the possibleTile with the random index
        _collapsedTile = possibleTiles[rand];*/

        //Get the prefab's own baked rotation
        Quaternion prefabBaseRotation = _collapsedTile.prefab.transform.rotation;
    
        //Apply WFC Y-spin ON TOP of the prefab's base rotation
        Quaternion wfcGridSpin = Quaternion.Euler(0f, _collapsedTile.rotationIndex * 90f, 0f);
        Quaternion finalRotation = wfcGridSpin * prefabBaseRotation;

        //instantiate the new prefab
        GameObject visual = Instantiate(_collapsedTile.prefab, transform.position, finalRotation);
        //set its parent to the current cell
        visual.transform.SetParent(this.transform);

        //cell is collapsed
        _isCollapsed = true;
        //clear to avoid confusions
        possibleTiles.Clear();
    }
}

public struct ResolvedEdge
{
    public int edgeId;
    public bool isSymmetric;
    public bool isFlipped;
    public bool isRotationallyInvariant;    
    public int rotationIndex;

    public static ResolvedEdge FromEdgeID(EdgeID id, int tileRotation, bool isVertical)
    {
        //null check
        if (id == null || id.edgeDetails == null)
            return new ResolvedEdge { edgeId = -999 };

        var d = id.edgeDetails;
        
        //return a newly made ResolvedEdge using the edgeDetails of the EdgeID
        return new ResolvedEdge
        {
            edgeId = d.edgeId,
            isSymmetric = d.isSymmetric,
            isFlipped = d.isFlipped,
            isRotationallyInvariant = d.isRotationallyInvariant,
            rotationIndex = isVertical ? tileRotation : d.rotationIndex
        };
    }
}

[System.Serializable]
public struct TileCandidate
{
    public GameObject prefab;
    public int rotationIndex; // 0, 1, 2, or 3
    public WFCCell3D cellData;
    
    public TileCandidate(GameObject prefab, int rotationIndex)
    {
        this.prefab = prefab;
        this.rotationIndex = rotationIndex;
        this.cellData = prefab.GetComponent<WFCCell3D>();
    }
}

[System.Serializable]
public struct DirectionalExclusion
{
    public Vector3Int direction;
    public List<GameObject> forbiddenPrefabs;
}