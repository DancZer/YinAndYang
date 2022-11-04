using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Height Map Preset", menuName = "Scriptables/HeightMapPreset", order = 3)]
public class MyTerrainRegionPreset : ScriptableObject
{
	public float Height;
	public Color Colour;
}
