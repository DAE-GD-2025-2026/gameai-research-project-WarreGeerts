using UnityEngine;
using UnityEngine.UI;

public class WaveFunctionCollapse : MonoBehaviour
{
    
    private float _columns;
    private float _rows;
    private Vector3 _cellSize;
    private Vector3 _startPosition;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var grid = GetComponent<GridVisualizer>();
        _columns = grid.GetColumns;
        _rows = grid.GetRows;
        _cellSize = grid.GetCellSize;
        _startPosition = grid.GetStartPosition;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
