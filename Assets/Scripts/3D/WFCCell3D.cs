using System.Collections.Generic;
using UnityEngine;


using System.Collections.Generic;
using UnityEngine;

public class WFCCell3D : MonoBehaviour
{
    [SerializeField] private EdgeID yP; 
    [SerializeField] private EdgeID yN;
    [SerializeField] private EdgeID zP; 
    [SerializeField] private EdgeID zN; 
    [SerializeField] private EdgeID xP; 
    [SerializeField] private EdgeID xN; 

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

    public int GetEntropy()
    {
        return possibleTiles.Count;
    }

    public void CollapseCell()
    {
        //contradiction
        if (possibleTiles.Count == 0) return;

        //Get random inside the possible tiles
        var rand = Random.Range(0, possibleTiles.Count);
        //Set the collapsedTile to the random index inside possibleTiles
        _collapsedTile = possibleTiles[rand];

        //Add the rotation to the tile
        Quaternion wfcGridSpin = Quaternion.Euler(0f, _collapsedTile.rotationIndex * 90f, 0f);
        Quaternion finalRotation = wfcGridSpin;

        //instantiate the tile
        GameObject visual = Instantiate(_collapsedTile.prefab, transform.position, finalRotation);
        //set the parent to the pre-generated cell
        visual.transform.SetParent(this.transform);

        //its collapsed
        _isCollapsed = true;
        //clear the possible tiles
        possibleTiles.Clear();
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