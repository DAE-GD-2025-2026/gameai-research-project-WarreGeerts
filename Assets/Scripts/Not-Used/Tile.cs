using UnityEngine;

public enum SideID
{
   g_0,
   r_0,
}

public class Tile : MonoBehaviour
{
   [Header("HardCoded")]
   [SerializeField] private Tile[] upNeighbors;
   [SerializeField] private Tile[] rightNeighbors;
   [SerializeField] private Tile[] downNeighbors;
   [SerializeField] private Tile[] leftNeighbors;
   
   [Header("ID's")]
   [SerializeField] private SideID northId;
   [SerializeField] private SideID eastId;
   [SerializeField] private SideID southId;
   [SerializeField] private SideID westId;
   
   public Tile[] UpNeighbors => upNeighbors;
   public Tile[] RightNeighbors => rightNeighbors;
   public Tile[] DownNeighbors => downNeighbors;
   public Tile[] LeftNeighbors => leftNeighbors;
   
   
   public SideID NorthId => northId;
   public SideID EastId => eastId;
   public SideID SouthId => southId;
   public SideID WestId => westId;
}
