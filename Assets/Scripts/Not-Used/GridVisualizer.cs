using System;
using UnityEditor;
using UnityEngine;

public class GridVisualizer : MonoBehaviour
{
    [Header("Grid")] [SerializeField] private int columns = 5;
    [SerializeField] private int rows = 5;
    [SerializeField] private Vector3 cellSize = new Vector3(2, 2, 2);
    [SerializeField] private Vector3 startPosition = Vector3.zero;
    [Header("Gizmos")] [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color color = Color.yellow;

    public int GetColumns => columns;
    public int GetRows => rows;
    public Vector3 GetCellSize => cellSize;
    public Vector3 GetStartPosition => startPosition;

    private void OnEnable()
    {
        UIControl.DebugChangeAction += b => drawGizmos = b;
    }

    private void OnDisable()
    {
        UIControl.DebugChangeAction -= b => drawGizmos = b;
    }

    private Vector3 position;

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        
        position = startPosition;
        Gizmos.color = color;

        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                Gizmos.DrawWireCube(position, cellSize);
                position.x += cellSize.x;
            }

            position.x = startPosition.x;
            position.z += cellSize.z;
        }

        position.x = startPosition.x;
        position.z = startPosition.z;
    }
}