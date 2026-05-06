using System.Collections.Generic;
using UnityEngine;


public class WFCCell2D : MonoBehaviour
{
    public enum WFCCell2DType
    {
        g,
        b
    }
    
    [SerializeField] private WFCCell2DType up;
    [SerializeField] private WFCCell2DType down;
    [SerializeField] private WFCCell2DType left;
    [SerializeField] private WFCCell2DType right;
    
    public List<GameObject> possibleTiles = new List<GameObject>();
    
    private bool _isCollapsed;
    private GameObject _collapsedTile;

    public bool IsCollapsed => _isCollapsed;
    public GameObject CollapsedTile => _collapsedTile;
    
    public WFCCell2DType Up => up;
    public WFCCell2DType Down => down;
    public WFCCell2DType Left => left;
    public WFCCell2DType Right => right;

    public int GetEntropy()
    {
        return possibleTiles.Count;
    }

    public void CollapseCell()
    {
        if (possibleTiles.Count == 0) return;

        var rand = Random.Range(0, possibleTiles.Count);
        _collapsedTile = possibleTiles[rand];

        GameObject visual = Instantiate(_collapsedTile, transform.position, Quaternion.identity);
    
        visual.transform.SetParent(this.transform);

        _isCollapsed = true;
        possibleTiles.Clear();
    }
}