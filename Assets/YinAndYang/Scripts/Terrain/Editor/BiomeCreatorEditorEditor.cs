#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BiomePresetConfigurator))]
public class BiomePresetConfiguratorEditor : Editor
{
	private void OnValidate()
	{
		BiomePresetConfigurator mapGen = (BiomePresetConfigurator)target;

		if (mapGen.EditorAutoUpdateMesh)
		{
			EditorApplication.update += UpdateTerrainGenerator;
		}
	}

    public override void OnInspectorGUI()
	{
		BiomePresetConfigurator mapGen = (BiomePresetConfigurator)target;

		if (DrawDefaultInspector())
		{
			if (mapGen.EditorAutoUpdateMesh)
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

		BiomePresetConfigurator mapGen = (BiomePresetConfigurator)target;
		mapGen.UpdateEditor();
	}
}
#endif