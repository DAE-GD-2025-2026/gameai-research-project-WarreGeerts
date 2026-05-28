using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class WFC3D : MonoBehaviour
{
    [SerializeField] private int sizeX = 3;
    [SerializeField] private int sizeY = 3;
    [SerializeField] private int sizeZ = 3;
    [SerializeField] private GameObject sourcePrefabs;

    [Header("Layer Constraints")] [SerializeField]
    private List<GameObject> bottomExclusions;

    [SerializeField] private List<GameObject> topExclusions;

    [SerializeField] private WFCCell3D emptyCell;
    private WFCCell3D[,,] _grid;

    private List<TileCandidate> _allRotatedCandidates = new List<TileCandidate>();

    private readonly Vector3Int[] _directions =
    {
        Vector3Int.up,
        Vector3Int.down,
        Vector3Int.forward,
        Vector3Int.back,
        Vector3Int.right,
        Vector3Int.left
    };

    private Vector3Int _currentProcessingCoords = new Vector3Int(-1, -1, -1);
    private Vector3Int _lastContradictionCoords = new Vector3Int(-1, -1, -1);

    private void Start()
    {
        //pre-compute all possible rotations from all the tiles
        PrecomputeRotatedCandidates();
        //start the co-routine, Generate the map and when a contradiction is found retry (no backtracking)
        StartCoroutine(GenerateWithRetry());
    }

    private IEnumerator GenerateWithRetry()
    {
        //continue to loop
        while (true)
        {
            //Clear the grid of all its tiles
            ClearGrid();
            //Generate the empty tiles inside the grid
            GenerateGrid();

            //Run the co-routine and set solver = to the co-routine
            IEnumerator solver = RunWfcCoroutine();
            //contradiction boolean
            bool contradiction = false;

            //loop while the co-routine moves to the next element
            while (solver.MoveNext())
            {
                //Gets the current element sets it to bool and checks if not success.
                if (solver.Current is false)
                {
                    //If not success set contradiction to true
                    contradiction = true;
                    break;
                }

                //wait 0.01 seconds
                yield return new WaitForSeconds(0.01f);
            }

            //if there are no contradictions print a green log "Success!"
            if (!contradiction)
            {
                Debug.Log("<color=#00FF00>Success!</color>");
                yield break;
            }

            //if there are contradictions print this:
            Debug.LogWarning("Contradiction! Retrying...");
            //wait for 0.5 seconds before returning
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void ClearGrid()
    {
        //check if grid isn't null
        if (_grid == null) return;

        //foreach cell inside the grid check if cell isn't null and then destroy the cell gameObject
        foreach (var cell in _grid)
        {
            if (cell != null) Destroy(cell.gameObject);
        }

        //set grid to null
        _grid = null;
    }

    private void PrecomputeRotatedCandidates()
    {
        //clear the candidates to be sure
        _allRotatedCandidates.Clear();

        //foreach child inside sourcePrefabs add 4 variations inside the rotation candidates, using the TileCandidate 
        foreach (Transform prefab in sourcePrefabs.transform)
        {
            for (int i = 0; i < 4; i++)
            {
                TileCandidate candidate = new TileCandidate(prefab.gameObject, i);
                if (candidate.cellData != null)
                {
                    _allRotatedCandidates.Add(candidate);
                }
            }
        }
    }

    private void GenerateGrid()
    {
        //set the grid to a new grid with these sizes
        _grid = new WFCCell3D[sizeX, sizeY, sizeZ];

        for (int y = 0; y < sizeY; y++)
        {
            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    //for this coord instantiate an empty cell with the current coord and an identity rotation 
                    _grid[x, y, z] = Instantiate(emptyCell, new Vector3(x, y, z), Quaternion.identity);
                    //set its parent to this transform
                    _grid[x, y, z].transform.SetParent(this.transform);
                    _grid[x, y, z].name = $"{x}, {y}, {z}";

                    //make a list of valid tiles using the rotation candidates
                    List<TileCandidate> validTiles = new List<TileCandidate>(_allRotatedCandidates);

                    //If you need to remove some to make generation better depending on coords
                    if (y == 0)
                    {
                        validTiles.RemoveAll(t => bottomExclusions.Contains(t.prefab));
                    }
                    if (y == sizeY - 1)
                    {
                        validTiles.RemoveAll(t => topExclusions.Contains(t.prefab));
                    }

                    //add validTiles to the possibleTiles of the cell
                    _grid[x, y, z].possibleTiles = validTiles;
                }
            }
        }
    }

    private IEnumerator RunWfcCoroutine()
    {
        //continue to loop
        while (true)
        {
            //Get the lowest entropy cell inside the grid
            var lowestEntropyCoord = GetLowestEntropyCell(_grid);

            //Contradiction if x coord is smaller than 0 (-1 fall back)
            if (lowestEntropyCoord.x < 0) yield break;

            //set the current processing coord to the lowest entropy cell
            _currentProcessingCoords = lowestEntropyCoord;

            //make a cell object using the grid and the lowest entropy cell
            var cell = _grid[lowestEntropyCoord.x, lowestEntropyCoord.y, lowestEntropyCoord.z];

            //Contradiction if the cells possible tile count is 0
            if (cell.possibleTiles.Count == 0)
            {
                _lastContradictionCoords = lowestEntropyCoord;
                yield return false;
                yield break;
            }

            //if no contradiction than collapse the cell 
            cell.CollapseCell();
            //Go to propagation stage with the lowest entropy coord
            Propagation(lowestEntropyCoord);

            //return true because no contradiction happened
            yield return true;
        }
    }

    private static Vector3Int GetLowestEntropyCell(WFCCell3D[,,] cells)
    {
        //Set the minimum to the highest int possible
        int minEntropy = int.MaxValue;
        //makes a list for all possible candidates
        var candidates = new List<Vector3Int>();
        //a boolean to check if the layer has been uncollapsed
        bool layerHasUncollapsed = false;
        //for loop using the grid cells (range is from 0 to cells length (in dimension 1, y))
        for (int y = 0; y < cells.GetLength(1); y++){

            //for loop using the grid cells (range is from 0 to cells length (in dimension 0, x))
            for (int x = 0; x < cells.GetLength(0); x++)
            {
                //for loop using the grid cells (range is from 0 to cells length (in dimension 2, z))
                for (int z = 0; z < cells.GetLength(2); z++)
                {
                    //Gets the current cell with the current coords
                    var cell = cells[x, y, z];
                    //if it has been collapsed go to the next cell
                    if (cell.IsCollapsed) continue;

                    //if not collapsed set the bool to true
                    layerHasUncollapsed = true;
                    //Gets the cell's current entropy
                    int curEntropy = cell.GetEntropy();

                    //if the entropy is lower than the minimum set current entropy to that new entropy
                    if (curEntropy < minEntropy)
                    {
                        minEntropy = curEntropy;
                        //clear the candidates and add the new location of the cell
                        candidates.Clear();
                        candidates.Add(new Vector3Int(x, y, z));
                    }
                    //if the current entropy is the same as the minimum, add the location to the candidates
                    else if (curEntropy == minEntropy)
                    {
                        candidates.Add(new Vector3Int(x, y, z));
                    }
                }
            }

            //if the layer has not been collapsed return a random coords inside the candidates list
            if (layerHasUncollapsed)
            {
                return candidates[Random.Range(0, candidates.Count)];
            }
        }

        //fall back value
        return new Vector3Int(-1, -1, -1);
    }

    private void Propagation(Vector3Int firstCollapsedCoords)
    {
        //make a new stack for collapsing cells
        var collapseCellStack = new Stack<Vector3Int>();
        var inStack = new HashSet<Vector3Int>();

        //add the first coord we got as a parameter
        collapseCellStack.Push(firstCollapsedCoords);
        inStack.Add(firstCollapsedCoords);


        //while the collapse stack isn't empty
        while (collapseCellStack.Count > 0)
        {
            //get the latest coords and pop them out the stack
            var currentCoords = collapseCellStack.Pop();
            inStack.Remove(currentCoords);
            //get the corresponding cell to the location
            var currentCell = _grid[currentCoords.x, currentCoords.y, currentCoords.z];

            //for every direction (6 directions)
            foreach (var dir in _directions)
            {
                //make a neighbor position
                var neighborCoord = currentCoords + dir;
                //check if it's inside the bounds
                if (neighborCoord.x < 0 || neighborCoord.x >= sizeX ||
                    neighborCoord.y < 0 || neighborCoord.y >= sizeY ||
                    neighborCoord.z < 0 || neighborCoord.z >= sizeZ) continue;

                //make a neighbor cell using the new coords 
                WFCCell3D neighborCell = _grid[neighborCoord.x, neighborCoord.y, neighborCoord.z];
                //if that cell has been collapsed continue to next neighbor
                if (neighborCell.IsCollapsed) continue;

                //Update the neighbors possible tiles, returns a boolean based on if a tile has been removed or not
                if (UpdateNeighborPossibilities(currentCell, neighborCell, dir))
                {
                    //if it returned true, then push the coords on top of the stack for the next execution
                    if (!inStack.Contains(neighborCoord))
                    {
                        collapseCellStack.Push(neighborCoord);
                        inStack.Add(neighborCoord);
                    }
                }
            }
        }
    }

    private bool UpdateNeighborPossibilities(WFCCell3D currentCell, WFCCell3D neighborCell, Vector3Int dir)
    {
        //boolean to check if removed
        var removed = false;
        //make a list of tile candidates, if the cell is collapsed than make the items the Collapsed Tile,
        //if not than add the sourceCells possible tiles
        List<TileCandidate> currentCandidates =
            currentCell.IsCollapsed ? new List<TileCandidate> { currentCell.CollapsedTile } : currentCell.possibleTiles;

        //foreach tile inside the possible tiles
        foreach (var neighborCandidate in neighborCell.possibleTiles.ToList())
        {
            //boolean to check if a match has been found
            bool hasMatch = false;
            //for each tile inside the source candidates
            foreach (var currentCandidate in currentCandidates)
            {
                //check if the candidate and the neighbor are valid neighbors using the direction
                if (CheckValidNeighbor(currentCandidate, neighborCandidate, dir))
                {
                    //if so set the match to true
                    hasMatch = true;
                    break;
                }
            }

            //if it's not a match than remove the neighbor candidate out of the possible tiles in the neighborCell
            if (!hasMatch)
            {
                neighborCell.possibleTiles.Remove(neighborCandidate);
                removed = true;
            }
        }

        //return removed
        return removed;
    }

    private bool CheckValidNeighbor(TileCandidate current, TileCandidate neighbor, Vector3Int dir)
    {
        //null checks
        if (current.cellData == null || neighbor.cellData == null) return false;

        //Get the resolvedEdge of this function, using the current cellData, the rotation Index and the direction (+ & -)
        ResolvedEdge currentEdge = GetRotatedEdgeID(current.cellData, current.rotationIndex, dir);
        ResolvedEdge neighborEdge = GetRotatedEdgeID(neighbor.cellData, neighbor.rotationIndex, -dir);
        
        //returns true or false if the edges are compatible
        return AreEdgesCompatible(currentEdge, neighborEdge);
    }

    private ResolvedEdge GetRotatedEdgeID(WFCCell3D data, int rotationIndex, Vector3Int direction)
    {
        //checks if direction is up or down, if so call the ResolveEdge.FromEdgeID with the data inside the Yp or Yn part,
        //the rotation index and setting the bool tru because it's vertical
        if (direction == Vector3Int.up)
            return ResolvedEdge.FromEdgeID(data.Yp, rotationIndex, true);
        if (direction == Vector3Int.down)
            return ResolvedEdge.FromEdgeID(data.Yn, rotationIndex, true);

        //Convert the direction into index
        int compassIndex = ConvertDirectionToCompassIndex(direction);
        //make sure its always positive
        int rotatedIndex = (compassIndex - rotationIndex) % 4;
        if (rotatedIndex < 0) rotatedIndex += 4;
        
        //get the corresponding data with each index
        EdgeID horizontalEdge = rotatedIndex switch
        {
            0 => data.Zp,
            1 => data.Xp,
            2 => data.Zn,
            3 => data.Xn,
            _ => data.Zp
        };

        //return the resolved Edge based on the horizontalEdge, rotationIndex and setting the bool to false because its horizontal
        return ResolvedEdge.FromEdgeID(horizontalEdge, rotationIndex, false);
    }

    private int ConvertDirectionToCompassIndex(Vector3Int dir)
    {
        //returns the index based on the vector 3 direction
        if (dir == Vector3Int.forward) return 0;
        if (dir == Vector3Int.right) return 1;
        if (dir == Vector3Int.back) return 2;
        if (dir == Vector3Int.left) return 3;
        //fall back return
        return 0;
    }

    private bool AreEdgesCompatible(ResolvedEdge edgeA, ResolvedEdge edgeB)
    {
        // Null sentinel check
        if (edgeA.edgeId == -999 || edgeB.edgeId == -999) return true;

        // IDs must match
        if (edgeA.edgeId != edgeB.edgeId) return false;

        // Invariant or symmetric = always compatible with itself
        if (edgeA.isRotationallyInvariant || edgeA.isSymmetric) return true;

        // Asymmetric horizontal = must be opposite flips
        if (edgeA.isFlipped || edgeB.isFlipped)
            return edgeA.isFlipped != edgeB.isFlipped;

        // Rotated vertical faces = rotation indices must match
        return edgeA.rotationIndex == edgeB.rotationIndex;
    }

    private void OnDrawGizmos()
    {
        //null check
        if (_grid == null) return;

        //if the current processing coord isn't -1 (fall back value) then draw a yellow wire cube on that position
        if (_currentProcessingCoords.x != -1)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(_currentProcessingCoords + new Vector3(0.5f, 0.5f, 0.5f), Vector3.one * 1.1f);
        }

        //if the last contradiction coord isn't -1 (fall back value) then draw a red cube on that position
        if (_lastContradictionCoords.x != -1)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawCube(_lastContradictionCoords + new Vector3(0.5f, 0.5f, 0.5f), Vector3.one * 0.8f);
        }
    }
}