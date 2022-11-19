using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "View Distance", menuName = "Scriptables/View Distance", order = 3)]
public class RequiredTileStatePreset : ScriptableObject
{
    public float Distance;
    
    /// <summary>
    /// If it is less than zero it means tile will be not displayed
    /// </summary>
    public int DisplayLOD = -1;
    public int CollisionLOD = -1;

    public TerrainTileState RequiredState;

    public override string ToString()
    {
        return $"Distance:{Distance}, DisplayLOD:{DisplayLOD}, CollisionLOD:{CollisionLOD}, RequiredState:{RequiredState}";
    }
}
