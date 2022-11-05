using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "View Distance", menuName = "Scriptables/View Distance", order = 3)]
public class ViewDistancePreset : ScriptableObject
{
    public float ViewDistance;
    public int LOD;
}
