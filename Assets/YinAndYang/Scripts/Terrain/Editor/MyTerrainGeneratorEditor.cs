#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapGenerator))]
public class MyTerrainGeneratorEditor : Editor
{
	public override void OnInspectorGUI()
	{
		MapGenerator mapGen = (MapGenerator)target;

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