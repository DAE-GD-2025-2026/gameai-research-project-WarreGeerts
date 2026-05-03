using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WaveFunction : MonoBehaviour
{
    [Header("default options")] [SerializeField]
    private int dimensions;

    [SerializeField] private Tile[] tileObjects;
    [SerializeField] private Cell cellObj;

    [Header(" ")] [SerializeField] private float generationSpeed = 0.01f;
    [SerializeField] private bool debugState = true;

    public float GenerationSpeed
    {
        get => generationSpeed;
        set => generationSpeed = value;
    }

    public bool DebugState
    {
        get => debugState;
        set => debugState = value;
    }

    private List<Cell> _gridComponents;
    private Cell _failedCell;
    private Cell _currentCell;


    private int _iterations = 0;

    private void Awake()
    {
        _gridComponents = new List<Cell>();
        InitializeGrid();
    }

    private void InitializeGrid()
    {
        //reset previous generation
        _failedCell = null;
        _currentCell = null;
        _iterations = 0;
        
        if (_gridComponents.Count > 0)
            _gridComponents.Clear();

        if (gameObject.transform.childCount > 0)
        {
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                Destroy(gameObject.transform.GetChild(i).gameObject);
            }
        }

        //start new generation
        for (int y = 0; y < dimensions; y++)
        {
            for (int x = 0; x < dimensions; x++)
            {
                Cell newCell = Instantiate(cellObj, new Vector3(x * 2, 0, y * 2), Quaternion.identity);
                newCell.transform.parent = gameObject.transform;
                newCell.CreateCell(false, tileObjects);
                _gridComponents.Add(newCell);
            }
        }

        StartCoroutine(CheckEntropy());
    }

    IEnumerator CheckEntropy()
    {
        List<Cell> tempGrid = _gridComponents.Where(c => !c.Collapsed).ToList();

        if (tempGrid.Count == 0)
            yield break;
                
        tempGrid.Sort((a, b) => a.TileOptions.Length.CompareTo(b.TileOptions.Length));

        int minEntropy = tempGrid[0].TileOptions.Length;
        tempGrid = tempGrid.Where(c => c.TileOptions.Length == minEntropy).ToList();

        yield return new WaitForSeconds(generationSpeed);
        CollapseCell(tempGrid);
    }

    private void CollapseCell(List<Cell> tempGrid)
    {
        int randIndex = UnityEngine.Random.Range(0, tempGrid.Count);

        Cell cellToCollapse = tempGrid[randIndex];

        if (cellToCollapse.TileOptions.Length == 0)
        {
            _failedCell = cellToCollapse;
            Debug.LogError($"Contradiction at {cellToCollapse.name}!");
            InitializeGrid();
            return;
        }

        _currentCell = cellToCollapse;

        cellToCollapse.Collapsed = true;
        Tile selectedTile = cellToCollapse.TileOptions[UnityEngine.Random.Range(0, cellToCollapse.TileOptions.Length)];
        cellToCollapse.TileOptions = new Tile[] { selectedTile };

        Tile foundTile = cellToCollapse.TileOptions[0];
        var tile = Instantiate(foundTile, cellToCollapse.transform.position, foundTile.gameObject.transform.rotation);
        tile.transform.parent = gameObject.transform;

        UpdateGeneration();
    }

    private void UpdateGeneration()
    {
        List<Cell> newGenerationCell = new List<Cell>(_gridComponents);

        for (int y = 0; y < dimensions; y++)
        {
            for (int x = 0; x < dimensions; x++)
            {
                var index = x + y * dimensions;
                if (_gridComponents[index].Collapsed)
                {
                    newGenerationCell[index] = _gridComponents[index];
                }
                else
                {
                    List<Tile> options = new List<Tile>();
                    foreach (var t in tileObjects)
                    {
                        options.Add(t);
                    }

                    if (y > 0)
                    {
                        Cell up = _gridComponents[x + (y - 1) * dimensions];
                        List<Tile> validOptions = new List<Tile>();

                        foreach (var possibleOptions in up.TileOptions)
                        {
                            var valOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
                            var valid = tileObjects[valOption].UpNeighbors;

                            validOptions = validOptions.Concat(valid).ToList();
                        }

                        CheckValidity(options, validOptions);
                    }

                    if (x < dimensions - 1)
                    {
                        Cell right = _gridComponents[x + 1 + y * dimensions];
                        List<Tile> validOptions = new List<Tile>();

                        foreach (var possibleOptions in right.TileOptions)
                        {
                            var valOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
                            var valid = tileObjects[valOption].LeftNeighbors;

                            validOptions = validOptions.Concat(valid).ToList();
                        }

                        CheckValidity(options, validOptions);
                    }

                    if (y < dimensions - 1)
                    {
                        Cell down = _gridComponents[x + (y + 1) * dimensions];
                        List<Tile> validOptions = new List<Tile>();

                        foreach (var possibleOptions in down.TileOptions)
                        {
                            var valOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
                            var valid = tileObjects[valOption].DownNeighbors;

                            validOptions = validOptions.Concat(valid).ToList();
                        }

                        CheckValidity(options, validOptions);
                    }

                    if (x > 0)
                    {
                        Cell left = _gridComponents[x - 1 + y * dimensions];
                        List<Tile> validOptions = new List<Tile>();

                        foreach (var possibleOptions in left.TileOptions)
                        {
                            var valOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
                            var valid = tileObjects[valOption].RightNeighbors;

                            validOptions = validOptions.Concat(valid).ToList();
                        }

                        CheckValidity(options, validOptions);
                    }

                    Tile[] newTileList = new Tile[options.Count];

                    for (int i = 0; i < options.Count; i++)
                    {
                        newTileList[i] = options[i];
                    }

                    newGenerationCell[index].RecreateCell(newTileList);
                }
            }
        }

        _gridComponents = newGenerationCell;
        _iterations++;

        if (_iterations < dimensions * dimensions)
        {
            StartCoroutine(CheckEntropy());
        }
        else
        {
            Debug.Log("Finished");
            _currentCell = null;
            
            GameObject container = GameObject.Find("FinishedMap");
            if (container == null)
            {
                container = new GameObject("FinishedMap");
            }
            
            List<Transform> children = new List<Transform>();
            foreach (Transform child in transform)
            {
                children.Add(child);
            }

            foreach (Transform child in children)
            {
                if (!child.name.Contains("Cell"))
                {
                    child.SetParent(container.transform);
                }
                else 
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }

    void CheckValidity(List<Tile> optionList, List<Tile> validOption)
    {
        for (int x = optionList.Count - 1; x >= 0; x--)
        {
            var element = optionList[x];
            if (!validOption.Contains(element))
            {
                optionList.RemoveAt(x);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!debugState) return;
        if (_failedCell != null)
        {
            // Set the color to bright red
            Gizmos.color = Color.red;

            // Draw a wire cube at the cell's position
            // Assuming your cells are roughly 2x2 based on your Instantiate logic
            Gizmos.DrawWireCube(_failedCell.transform.position, new Vector3(2f, 2f, 2f));

            // Optional: Draw a solid transparent cube to make it pop
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawCube(_failedCell.transform.position, new Vector3(2f, 2f, 2f));
        }
        else if (_currentCell != null)
        {
            // Set the color to bright red
            Gizmos.color = Color.purple;

            // Draw a wire cube at the cell's position
            // Assuming your cells are roughly 2x2 based on your Instantiate logic
            Gizmos.DrawWireCube(_currentCell.transform.position, new Vector3(2.1f, 2.1f, 2.1f));

            // Optional: Draw a solid transparent cube to make it pop
            Gizmos.color = new Color(1, 0, 1, 0.3f);
            Gizmos.DrawCube(_currentCell.transform.position, new Vector3(2.1f, 2.1f, 2.1f));
        }
    }
}