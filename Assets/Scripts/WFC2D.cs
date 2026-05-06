using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class WFC2D : MonoBehaviour
{
    [SerializeField] private int sizeX = 3;
    [SerializeField] private int sizeY = 3;
    [SerializeField] private List<GameObject> availableSprites = new List<GameObject>();

    [SerializeField] private WFCCell2D emptyCell;
    private WFCCell2D[,] _grid;

    private void Start()
    {
        _grid = new WFCCell2D[sizeX, sizeY];

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                _grid[x, y] = Instantiate(emptyCell, new Vector3(x, y, 0), Quaternion.identity);
                _grid[x, y].possibleTiles = new List<GameObject>(availableSprites);
            }
        }

        Observation();
    }

    private void Observation()
    {
        var lowestEntropyCoord = GetLowestEntropyCell(_grid);

        if (lowestEntropyCoord.x < 0 || lowestEntropyCoord.y < 0) return;

        var cell = _grid[lowestEntropyCoord.x, lowestEntropyCoord.y];

        if (cell.possibleTiles.Count == 0)
        {
            Debug.LogError("Contradiction reached! No possible tiles for cell at " + lowestEntropyCoord);
            return;
        }

        cell.CollapseCell();

        Propagation(lowestEntropyCoord);
    }

    private void Propagation(Vector2Int firstCollapsedCoords)
    {
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        var collapseCellStack = new Stack<Vector2Int>();
        collapseCellStack.Push(firstCollapsedCoords);

        while (collapseCellStack.Count > 0)
        {
            var collapseCellCoords = collapseCellStack.Pop();
            var collapsedCell = _grid[collapseCellCoords.x, collapseCellCoords.y];

            directions = directions.OrderBy(x => Random.value).ToArray();

            foreach (var dir in directions)
            {
                var neighborCoord = collapseCellCoords + dir;

                if (neighborCoord.x < 0 || neighborCoord.x >= sizeX ||
                    neighborCoord.y < 0 || neighborCoord.y >= sizeY) continue;

                WFCCell2D neighborCell = _grid[neighborCoord.x, neighborCoord.y];

                if (neighborCell.IsCollapsed) continue;

                if (UpdateNeighborPossibilities(collapsedCell, neighborCell, dir))
                {
                    collapseCellStack.Push(neighborCoord);
                }
            }
        }

        Observation();
    }

    private bool UpdateNeighborPossibilities(WFCCell2D sourceCell, WFCCell2D neighborCell, Vector2Int dir)
    {
        var removed = false;

        List<GameObject> sourceSprites = sourceCell.IsCollapsed
            ? new List<GameObject> { sourceCell.CollapsedTile }
            : sourceCell.possibleTiles;

        foreach (var neighborSprite in neighborCell.possibleTiles.ToList())
        {
            bool hasMatch = false;
            foreach (var sourceSprite in sourceSprites)
            {
                // Pass the Prefab GameObjects directly to the check
                if (CheckValidNeighbor(sourceSprite, neighborSprite, dir))
                {
                    hasMatch = true;
                    break;
                }
            }

            if (!hasMatch)
            {
                neighborCell.possibleTiles.Remove(neighborSprite);
                removed = true;
            }
        }

        return removed;
    }

    private bool CheckValidNeighbor(GameObject sourcePrefab, GameObject neighborPrefab, Vector2Int dir)
    {
        WFCCell2D sourceData = sourcePrefab.GetComponent<WFCCell2D>();
        WFCCell2D neighborData = neighborPrefab.GetComponent<WFCCell2D>();

        if (dir == Vector2Int.up)
        {
            // Source Top must match Neighbor Bottom
            return sourceData.Up == neighborData.Down;
        }
        if (dir == Vector2Int.down)
        {
            // Source Bottom must match Neighbor Top
            return sourceData.Down == neighborData.Up;
        }
        if (dir == Vector2Int.right)
        {
            // Source Right must match Neighbor Left
            return sourceData.Right == neighborData.Left;
        }
        if (dir == Vector2Int.left)
        {
            // Source Left must match Neighbor Right
            return sourceData.Left == neighborData.Right;
        }

        return false;
    }


    private static Vector2Int GetLowestEntropyCell(WFCCell2D[,] cells)
    {
        int minEntropy = int.MaxValue;

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
                    minEntropy = curEntropy;
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