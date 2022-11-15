#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainGenerator))]
public class MyTerrainGeneratorEditor : Editor
{
	private void OnValidate()
	{
		TerrainGenerator mapGen = (TerrainGenerator)target;

		if (mapGen.EditorAutoUpdate)
		{
			EditorApplication.update += DrawTerrainInEditor;
		}
	}

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

	public void DrawTerrainInEditor()
    {
		EditorApplication.update -= DrawTerrainInEditor;

		TerrainGenerator mapGen = (TerrainGenerator)target;
		mapGen.DrawTerrainInEditor();
	}
}
#endif