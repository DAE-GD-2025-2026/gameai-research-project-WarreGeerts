/*using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WaveFunction : MonoBehaviour
{
    private struct GenerationSnapshot
    {
        public List<(int index, Tile[] options, bool collapsed)> cellStates;
        public int iterations;
    }

    [Header("default options")] [SerializeField]
    private int dimensions;

    [SerializeField] private Tile[] tileObjects; //has every possible tile that can be spawned
    [SerializeField] private Cell cellObj;

    [Header(" ")] [SerializeField] private float generationSpeed = 0.01f;
    [SerializeField] private bool debugState = true;


    private readonly Stack<GenerationSnapshot> _history = new Stack<GenerationSnapshot>();

    private List<Cell> _gridComponents;
    private Cell _failedCell;
    private Cell _currentCell;

    private int _iterations;

    public static Action CombineCellsAction;

    //UI events to listen to, subscribe onEnable
    private void OnEnable()
    {
        UIControl.SpeedChangeAction += i => generationSpeed = i;
        UIControl.DebugChangeAction += b => debugState = b;
        UIControl.GenerateAction += InitializeGrid;
    }

    //UI events to listen to, unsubscribe onEnable
    private void OnDisable()
    {
        UIControl.SpeedChangeAction -= i => generationSpeed = i;
        UIControl.DebugChangeAction -= b => debugState = b;
        UIControl.GenerateAction -= InitializeGrid;
    }

    private void Awake()
    {
        _gridComponents = new List<Cell>();
    }

    /// <summary>
    /// Resets every shared variable and
    /// Initializes the new grid with empty Cell objects.
    /// At the end stops every coroutine and starts the CheckEntropy coroutine.
    /// </summary>
    private void InitializeGrid()
    {
        //reset previous generation
        _history.Clear();
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

        StopAllCoroutines();
        StartCoroutine(CheckEntropy());
    }

    /// <summary>
    /// The main generation loop that identifies cells with the lowest entropy (fewest remaining options) 
    /// and processes them one by one. It pauses based on generationSpeed until the entire grid is collapsed.
    /// </summary>
    private IEnumerator CheckEntropy()
    {
        while (true)
        {
            List<Cell> tempGrid = _gridComponents.Where(c => !c.Collapsed).ToList();

            if (tempGrid.Count == 0)
            {
                Debug.Log("Finished");
                CleanUpCells();
                CombineCellsAction?.Invoke();
                yield break;
            }

            tempGrid.Sort((a, b) => a.TileOptions.Length.CompareTo(b.TileOptions.Length));
            int minEntropy = tempGrid[0].TileOptions.Length;
            tempGrid = tempGrid.Where(c => c.TileOptions.Length == minEntropy).ToList();

            if (generationSpeed > 0f)
                yield return new WaitForSeconds(generationSpeed);
            else
                yield return null;

            CollapseCell(tempGrid);
        }
    }

    /// <summary>
    /// Randomly selects a cell to collapse, assigns it a final tile from its available options, 
    /// and triggers a grid-wide constraint update. Includes backtracking logic to handle 
    /// contradictions where a cell has zero valid options.
    /// </summary>
    private void CollapseCell(List<Cell> tempGrid)
    {
        int randIndex = UnityEngine.Random.Range(0, tempGrid.Count);
        Cell cellToCollapse = tempGrid[randIndex];

        if (cellToCollapse.TileOptions.Length == 0)
        {
            _failedCell = cellToCollapse;

            if (RestoreSnapshot())
            {
                Debug.LogWarning("Contradiction — backtracking...");
            }
            else
            {
                Debug.LogError("Out of backtracks — full reset.");
                InitializeGrid();
            }

            return;
        }

        SaveSnapshot();

        _currentCell = cellToCollapse;
        cellToCollapse.Collapsed = true;
        Tile selectedTile = cellToCollapse.TileOptions[UnityEngine.Random.Range(0, cellToCollapse.TileOptions.Length)];
        cellToCollapse.TileOptions = new[] { selectedTile };

        Tile foundTile = cellToCollapse.TileOptions[0];
        var tile = Instantiate(foundTile, cellToCollapse.transform.position, foundTile.gameObject.transform.rotation);
        tile.transform.parent = gameObject.transform;

        UpdateGeneration();
    }

    /// <summary>
    /// Updates the entropy of the grid by propagating constraints. 
    /// For each uncollapsed cell, it filters the available tile options based on 
    /// the valid neighbors of its four adjacent cells.
    /// </summary>
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
                    List<SideID> sideIds = new List<SideID>();
                    
                    //gets every tile that get used into a new list
                    foreach (var t in tileObjects)
                    {
                        options.Add(t);
                    }

                    if (y > 0) //north
                    {
                        Cell up = _gridComponents[x + (y - 1) * dimensions]; //Gets the neighboring cell in the north
                        List<Tile> validOptions = new List<Tile>(); //gets the valid options from the tile (hardcoded)

                        //for every possible option in the TileOptions for the northern neighbor
                        foreach (var possibleOptions in up.TileOptions) 
                        {
                            //gets the current index of the tile 
                            var valOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions); 
                            //Gets the upNeighbors from the Tile with index valOption
                            var valid = tileObjects[valOption].UpNeighbors; 

                            validOptions = validOptions.Concat(valid).ToList(); //adds the valid to validOptions
                        }

                        //checks if validOptions contains the items in options if not than remove the option in options
                        CheckValidity(options, validOptions);
                        CheckValidityID();
                    }

                    if (x < dimensions - 1) // east
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

                    if (y < dimensions - 1) //south
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

                    if (x > 0) //west
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
    }

    /// <summary>
    /// Filters the available options by removing any tile that is not present 
    /// in the provided list of valid neighbor constraints.
    /// </summary>
    /// <param name="optionList">List of all available Tiles.</param>
    /// <param name="validOption">List of valid Tiles that can be adjacent.</param>
    private static void CheckValidity(List<Tile> optionList, List<Tile> validOption)
    {
        for (var x = optionList.Count - 1; x >= 0; x--)
        {
            var element = optionList[x];
            if (!validOption.Contains(element))
            {
                optionList.RemoveAt(x);
            }
        }
    }
    
    
    /// <summary>
    /// Filters the available options by removing any IDs that is equal
    /// to provided valid ID
    /// </summary>
    /// <param name="optionList">List of all available IDs.</param>
    /// <param name="validID">Valid ID of the side</param>
    private static void CheckValidityID(List<SideID> optionList, SideID validID)
    {
        for (var x = optionList.Count - 1; x >= 0; x--)
        {
            var element = optionList[x];
            if (validID != element)
            {
                optionList.RemoveAt(x);
            }
        }
    }

    /// <summary>
    /// Captures the current state of the entire grid, including all remaining tile options 
    /// and the iteration count, then pushes it onto a history stack to allow for future backtracking.
    /// </summary>
    private void SaveSnapshot()
    {
        var snapshot = new GenerationSnapshot
        {
            cellStates = _gridComponents.Select((c, i) => (i, c.TileOptions.ToArray(), c.Collapsed)).ToList(),
            iterations = _iterations
        };
        _history.Push(snapshot);
    }

    /// <summary>
    /// Reverts the grid to the most recent valid state by popping the last snapshot 
    /// from the history stack. Restores all tile options, collapse statuses, and 
    /// the iteration counter. Returns false if no history exists to backtrack to.
    /// </summary>
    /// <returns>Returns a bool based on if the Stack that has been used is equal to 0 or not</returns>
    private bool RestoreSnapshot()
    {
        if (_history.Count == 0) return false;

        var snapshot = _history.Pop();
        foreach (var (index, options, collapsed) in snapshot.cellStates)
        {
            _gridComponents[index].RecreateCell(options);
            _gridComponents[index].Collapsed = collapsed;
        }

        _iterations = snapshot.iterations;
        return true;
    }


    /// <summary>
    /// Cleans up all the empty cell types inside the gameObject
    /// </summary>
    private void CleanUpCells()
    {
        for (var i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);

            if (child.name.ToLower().Contains("cell"))
            {
                Destroy(child.gameObject);
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
}*/