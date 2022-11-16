#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainGenerator))]
public class MyTerrainGeneratorEditor : Editor
{
	private void OnValidate()
	{
		TerrainGenerator mapGen = (TerrainGenerator)target;

		if (mapGen.EditorAutoUpdateMesh || mapGen.EditorAutoUpdateBiome)
		{
			EditorApplication.update += UpdateTerrainGenerator;
		}
	}

    public override void OnInspectorGUI()
	{
		TerrainGenerator mapGen = (TerrainGenerator)target;

		if (DrawDefaultInspector())
		{
			if (mapGen.EditorAutoUpdateMesh || mapGen.EditorAutoUpdateBiome)
			{
				UpdateTerrainGenerator();
			}
		}

		if (GUILayout.Button("Generate"))
		{
			UpdateTerrainGenerator();
		}
	}

	public void UpdateTerrainGenerator()
    {
		EditorApplication.update -= UpdateTerrainGenerator;

		TerrainGenerator mapGen = (TerrainGenerator)target;
		mapGen.UpdateEditor();
	}
}
#endif