#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainGenerator))]
public class MyTerrainGeneratorEditor : Editor
{
	public override void OnInspectorGUI()
	{
		TerrainGenerator mapGen = (TerrainGenerator)target;

		if (DrawDefaultInspector())
		{
			if (mapGen.EditorAutoUpdate)
			{
				mapGen.DrawTerrainInEditor();
			}
		}

		if (GUILayout.Button("Generate"))
		{
			mapGen.DrawTerrainInEditor();
		}
	}
}
#endif