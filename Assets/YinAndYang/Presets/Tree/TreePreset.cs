using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Tree Preset", menuName = "Scriptables/Tree Preset", order = 2)]
public class TreePreset : ScriptableObject
{
    public string ForestTypeName = "Tree";

    public int ForestDensity = 5;
    public int ForestMinDistance = 4;
    public int ForestMaxDistance = 10;

    public float ForestMinMaturity = 0.5f;
    public float ForestMaxMaturity = 1f;

    public float ForestTimeToMature = 10;
}