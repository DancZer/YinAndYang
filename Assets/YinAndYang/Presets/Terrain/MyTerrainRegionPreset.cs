using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Height Terrain Preset", menuName = "Scriptables/HeightTerrainPreset", order = 3)]
public class MyTerrainRegionPreset : ScriptableObject
{
	public float Height;
	public Color Color;
}