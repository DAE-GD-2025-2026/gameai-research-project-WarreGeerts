using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class WFC2D : MonoBehaviour
{
    [SerializeField] private int sizeX = 3;
    [SerializeField] private int sizeY = 3;
    [SerializeField] private List<Sprite> availableSprites = new List<Sprite>();

    private WFCCell2D[,] _grid;
    private WFCCell2D _emptyCell;

    private void Start()
    {
        _grid = new WFCCell2D[sizeX, sizeY];

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                _grid[x, y] = Instantiate(_emptyCell);
                _grid[x, y].possibleTiles = new List<Sprite>(availableSprites);
            }
        }
    }

    private void Observation()
    {
        var lowestEntropyCoord = GetLowestEntropyCell(_grid);

        if (lowestEntropyCoord.x < 0 || lowestEntropyCoord.y < 0) return;

        var cell = _grid[lowestEntropyCoord.x, lowestEntropyCoord.y];
        cell.CollapseCell();

        Propagation(lowestEntropyCoord);
    }

    private void Propagation(Vector2Int collapsedCoords)
    {
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        var collapseCellStack = new Stack<WFCCell2D>();
        var cell = _grid[collapsedCoords.x, collapsedCoords.y];

        collapseCellStack.Push(cell);

        while (collapseCellStack.Count > 0)
        {
            var collapseCell = collapseCellStack.Pop();
            if (collapseCell.IsCollapsed) continue;

            foreach (var dir in directions)
            {
                var neighborCoord = collapsedCoords + dir;

                if (neighborCoord.x < 0 || neighborCoord.x >= sizeX ||
                    neighborCoord.y < 0 || neighborCoord.y >= sizeY) continue;

                WFCCell2D neighborCell = _grid[neighborCoord.x, neighborCoord.y];

                if (neighborCell.IsCollapsed) continue;

                UpdateNeighborPossibilities(collapseCell, neighborCell, dir);
            }
        }
    }

    private void UpdateNeighborPossibilities(WFCCell2D collapseCell, WFCCell2D neighborCell, Vector2Int dir)
    {
        
    }

    private static Vector2Int GetLowestEntropyCell(WFCCell2D[,] cells)
    {
        const int minEntropy = int.MaxValue;

        var coordsCandidateCells = new List<Vector2Int>();

        for (int x = 0; x < cells.GetLength(0); x++)
        {
            for (int y = 0; y < cells.GetLength(1); y++)
            {
                WFCCell2D cell = cells[x, y];

                if (cell.IsCollapsed) continue;

                var curEntropy = cell.GetEntropy();

                if (curEntropy < minEntropy)
                {
                    coordsCandidateCells.Clear();
                    coordsCandidateCells.Add(new Vector2Int(x, y));
                }

                if (curEntropy == minEntropy)
                {
                    coordsCandidateCells.Add(new Vector2Int(x, y));
                }
            }
        }

        if (coordsCandidateCells.Count == 0) return new Vector2Int(-1, -1);

        var rand = Random.Range(0, coordsCandidateCells.Count);

        return coordsCandidateCells[rand];
    }
}