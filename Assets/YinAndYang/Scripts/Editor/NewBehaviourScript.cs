using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MyTerrainManager))]
public class MapGeneratorEditor : Editor
{
	public override void OnInspectorGUI()
	{
		MyTerrainManager mapGen = (MyTerrainManager)target;

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