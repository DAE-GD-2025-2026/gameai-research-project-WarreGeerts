using System.Collections.Generic;
using UnityEngine;

public class WFCCell2D : MonoBehaviour
{
    public List<Sprite> possibleTiles = new List<Sprite>();
    private bool _isCollapsed;
    private Sprite _collapsedTile;

    public bool IsCollapsed => _isCollapsed;
    public Sprite CollapsedTile => _collapsedTile;

    public int GetEntropy()
    {
        return possibleTiles.Count;
    }

    public void CollapseCell()
    {
        var rand = Random.Range(0, possibleTiles.Count);
        
        _collapsedTile = possibleTiles[rand];
        
        SpriteRenderer sRenderer = gameObject.AddComponent<SpriteRenderer>();
        sRenderer.sprite = _collapsedTile;
        
        _isCollapsed = true;
        
        possibleTiles.Clear();
    }
}