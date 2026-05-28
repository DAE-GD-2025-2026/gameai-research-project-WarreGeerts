using UnityEngine;

[System.Serializable]
public class EdgeDetail
{
    public string idName;
    public int edgeId;

    [Header("Horizontal Settings")] 
    public bool isSymmetric;
    public bool isFlipped;
    
    [Header("Vertical Settings")] 
    [Range(0,3)]
    public int rotationIndex;
    public bool isRotationallyInvariant;
}

[CreateAssetMenu(fileName = "EdgeID", menuName = "Scriptable Objects/EdgeID")]
public class EdgeID : ScriptableObject
{
    public EdgeDetail edgeDetails;
    
    
}
