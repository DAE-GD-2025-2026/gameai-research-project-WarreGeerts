using System;
using System.Collections.Generic;
using UnityEngine;

public enum TileId{
    Water = 0,
    Grass = 1,
    Road = 2,
    Overhang = 3
}

public class TileIds : MonoBehaviour
{
    [Header("Tile Side Ids")]
    [SerializeField] private TileId north;
    [SerializeField] private TileId east;
    [SerializeField] private TileId south;
    [SerializeField] private TileId west;
    
    private Dictionary<int, TileId> _tileIds = new Dictionary<int, TileId>();
    
    public Dictionary<int, TileId> GetTileIds => _tileIds;

    private void Start()
    {
        _tileIds.TryAdd(0, north);
        _tileIds.TryAdd(1, east);
        _tileIds.TryAdd(2, south);
        _tileIds.TryAdd(3, west);
    }
}
