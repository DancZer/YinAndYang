using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MyTerrainGenerator))]
public class MyTerrainGeneratorEditor : Editor
{
	public override void OnInspectorGUI()
	{
		MyTerrainGenerator mapGen = (MyTerrainGenerator)target;

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