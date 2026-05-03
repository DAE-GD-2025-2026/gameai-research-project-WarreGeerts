using UnityEngine;

public class Cell : MonoBehaviour
{
   private bool _collapsed = false;
   private Tile[] _tileOptions;
   
   public bool Collapsed
   {
      get { return _collapsed; }
      set { _collapsed = value; }
   }

   public Tile[] TileOptions
   {
      get { return _tileOptions; }
      set { _tileOptions = value; }
   }

   public void CreateCell(bool collapseState, Tile[] tiles)
   {
      _collapsed = collapseState;
      _tileOptions = tiles;
   }

   public void RecreateCell(Tile[] tiles)
   {
      _tileOptions = tiles;
   }

}
