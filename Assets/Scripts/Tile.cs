using UnityEngine;

public class Tile : MonoBehaviour
{
   [SerializeField] private Tile[] upNeighbors;
   [SerializeField] private Tile[] rightNeighbors;
   [SerializeField] private Tile[] downNeighbors;
   [SerializeField] private Tile[] leftNeighbors;
   
   public Tile[] UpNeighbors => upNeighbors;
   public Tile[] RightNeighbors => rightNeighbors;
   public Tile[] DownNeighbors => downNeighbors;
   public Tile[] LeftNeighbors => leftNeighbors;
}
