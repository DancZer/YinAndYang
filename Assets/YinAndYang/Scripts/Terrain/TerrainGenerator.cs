using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using System.Linq;

[ExecuteInEditMode]
public class TerrainGenerator : NetworkBehaviour
{
	public enum TerrainDrawMode { HeightMap, ColourMap, Mesh };

	public static int[] MeshStepSizeByLOD = { 4, 8, 12, 20, 24, 30, 48 };

	public TerrainDrawMode DrawMode;
#if UNITY_EDITOR
	public int EditorTerrainSize = 240;
	public bool EditorAutoUpdate;
	public TerrainTileDisplay EditorCenterTile;
	public TerrainTileDisplay EditorLeftTile;
	public TerrainTileDisplay EditorRightTile;
	public TerrainTileDisplay EditorForwardTile;
	public TerrainTileDisplay EditorBackwardTile;
	public BuildingOnTerrain EditorBuilding;
	public float FlatAreaHeight = 10;
#endif
	public Material BaseMaterial;
	public int Resolution = 240;

	public float HeightMultiplier = 100;
	public bool UseHeightCurveEvaluator = true;
	public AnimationCurve HeightCurve;
	public MyTerrainRegionPreset[] Regions;

	public FastNoiseLite.NoiseType NoiseType;
	public FastNoiseLite.FractalType FractalType;
	public int Seed = 1234;
	public float Frequency = 0.5f;
	public int Octaves = 3;
	public float Gain = 2;
	public float Lacunarity = 2;

#if UNITY_EDITOR
	[ReadOnly] public float MinVal;
	[ReadOnly] public float MaxVal;

	[ReadOnly] public float MinHeight;
	[ReadOnly] public float MaxHeight;
#endif

#if UNITY_EDITOR

	List<Vector3> lastPosList = new();

    public void DrawTerrainInEditor()
	{
		InitMaterial();

		MinVal = MinHeight = float.MaxValue;
		MaxVal = MaxHeight = float.MinValue;

		UpdateEditorDisplay(EditorCenterTile
			, new TerrainTileDisplay[] { 
				EditorLeftTile,
				EditorForwardTile,
				EditorRightTile,
				EditorBackwardTile
			}
			);
		UpdateEditorDisplay(EditorLeftTile);
		UpdateEditorDisplay(EditorRightTile);
		UpdateEditorDisplay(EditorForwardTile);
		UpdateEditorDisplay(EditorBackwardTile);
	}

	private void UpdateEditorDisplay(TerrainTileDisplay tileDisplay, TerrainTileDisplay[] neigbours = null)
    {
		if (tileDisplay == null) return;

		var editorDisplayArea = new Rect(
				tileDisplay.transform.position.x - EditorTerrainSize / 2f,
				tileDisplay.transform.position.z - EditorTerrainSize / 2f,
				EditorTerrainSize,
				EditorTerrainSize);

		TerrainTile tile = new TerrainTile(editorDisplayArea);

		GenerateTerrainData(tile);

		if (EditorBuilding != null)
		{
			var flatArea = EditorBuilding.GetComponentInChildren<BuildingFootprint>().GetFootprint();
			var pos = EditorBuilding.transform.position;
			tile.FlatHeightMap(flatArea, pos.y);
		}

		if (tileDisplay.EditorViewDistance == null) return;

		GenerateMeshData(tile, tileDisplay.EditorViewDistance.DisplayLOD);
		GenerateMeshData(tile, tileDisplay.EditorViewDistance.CollisionLOD);

		if (DrawMode == TerrainDrawMode.HeightMap || DrawMode == TerrainDrawMode.ColourMap)
		{
			var meshFilter = tileDisplay.GetComponent<MeshFilter>();
			var meshRenderer = tileDisplay.GetComponent<MeshRenderer>();

			meshFilter.sharedMesh = CreatePlaneMesh(tile.Area);
			meshRenderer.sharedMaterial = BaseMaterial;
		}
		else if (DrawMode == TerrainDrawMode.Mesh)
		{
			if(neigbours != null && neigbours.Length == 4)
            {
				tile.MeshDatas[tileDisplay.EditorViewDistance.DisplayLOD].AdjustMeshToNeighboursLOD(neigbours.Select(n => n.EditorViewDistance.DisplayLOD).ToArray());
			}

			tileDisplay.SetTile(tile);
			tileDisplay.Display(tileDisplay.EditorViewDistance);
		}
	}

	private Mesh CreatePlaneMesh(Rect area)
	{
		var mesh = new Mesh();

		var verts = new List<Vector3>();
		var uvs = new List<Vector2>();

		var offset = Vector2.zero;

		var numFaces = 1;
		verts.Add(new Vector3(offset.x, 0, offset.y));
		verts.Add(new Vector3(offset.x, 0, offset.y + area.size.y));
		verts.Add(new Vector3(offset.x + area.size.x, 0, offset.y + area.size.y));
		verts.Add(new Vector3(offset.x + area.size.x, 0, offset.y));

		uvs.Add(new Vector2(0, 0));
		uvs.Add(new Vector2(0, 1));
		uvs.Add(new Vector2(1, 1));
		uvs.Add(new Vector2(1, 0));

		var tris = new List<int>();
		int tl = verts.Count - 4 * numFaces;
		for (int i = 0; i < numFaces; i++)
		{
			var p0 = tl + i * 4;
			var p1 = tl + i * 4 + 1;
			var p2 = tl + i * 4 + 2;
			var p3 = tl + i * 4 + 3;

			tris.AddRange(new int[] {
				p0, p1, p2,
				p0, p2, p3 }
			);
		}

		mesh.SetVertices(verts);
		mesh.SetTriangles(tris, 0);
		mesh.SetUVs(0, uvs);
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();

		mesh.Optimize();

		return mesh;
	}

    void Update()
    {
		var list = new List<Transform>();

		if (EditorBuilding != null)
			list.Add(EditorBuilding.transform);

		if (EditorCenterTile != null)
			list.Add(EditorCenterTile.transform);

		if (EditorLeftTile != null)
			list.Add(EditorLeftTile.transform);
		if (EditorRightTile != null)
			list.Add(EditorRightTile.transform);

		if (EditorForwardTile != null)
			list.Add(EditorForwardTile.transform);
		if (EditorBackwardTile != null)
			list.Add(EditorBackwardTile.transform);

		if (ShouldDraw(list))
        {
			DrawTerrainInEditor();
		}
	}
	private bool ShouldDraw(List<Transform> list)
    {
		if(lastPosList.Count == list.Count)
        {
			var changed = false;

            for (int i = 0; i < list.Count; i++)
            {
				if(lastPosList[i] != list[i].position)
                {
					lastPosList[i] = list[i].position;
					changed = true;
				}
            }

			return changed;
		}
        else
        {
			lastPosList.Clear();

			foreach (var t in list)
            {
				lastPosList.Add(t.position);
			}
			return true;
        }
	}

#endif

	public void InitMaterial()
	{
		var heightCurve = new AnimationCurve(HeightCurve.keys);

		var minHeight = heightCurve.Evaluate(-1) * HeightMultiplier;
		var maxHeight = heightCurve.Evaluate(1) * HeightMultiplier;
		var heightColourCount = Regions.Length;
		var heightColours = Regions.Select(r => r.Color).ToArray();
		var heightColoursStartHeight = Regions.Select(r => r.Height).ToArray();

		Debug.Log($"InitMaterial {minHeight} {maxHeight} {heightColourCount} {string.Join(",", heightColours)} {string.Join(",",heightColoursStartHeight)}");

		BaseMaterial.SetFloat("minHeight", minHeight);
		BaseMaterial.SetFloat("maxHeight", maxHeight);
		BaseMaterial.SetInt("heightColourCount", heightColourCount);
		BaseMaterial.SetColorArray("heightColours", heightColours);
		BaseMaterial.SetFloatArray("heightColoursStartHeight", heightColoursStartHeight);
	}

	public void GenerateTerrainData(TerrainTile tile)
	{
		var heightMap = CreateHeightMap(tile.Area);
		Color[] colorMap;

		if (DrawMode == TerrainDrawMode.HeightMap)
		{
			colorMap = CreateGrayscaleMap(heightMap);
		}
        else
        {
			colorMap = CreateColorMap(heightMap);
		}

		tile.SetMap(heightMap, colorMap, Resolution);
	}

	private float[] CreateHeightMap(Rect area)
    {
		var heightCurve = new AnimationCurve(HeightCurve.keys);
		var noise = new FastNoiseLite(Seed);
		
		noise.SetFractalOctaves(Octaves);
		noise.SetFractalGain(Gain);
		noise.SetFractalLacunarity(Lacunarity);
		noise.SetFrequency(Frequency);
		noise.SetFractalType(FractalType);
		noise.SetNoiseType(NoiseType);

		int NoiseMapSize = Resolution + 1;

		float[] heightMap = new float[NoiseMapSize * NoiseMapSize];

		var noiseMapStep = area.size.x / Resolution;
		var offset = area.position;

		for (int y = 0; y < NoiseMapSize; y++)
		{
			for (int x = 0; x < NoiseMapSize; x++)
			{
				var idx = y * NoiseMapSize + x;

				var val = noise.GetNoise(offset.x + x * noiseMapStep, offset.y + y * noiseMapStep);
				var height = (UseHeightCurveEvaluator ? heightCurve.Evaluate(val) : heightMap[idx]) * HeightMultiplier;
				heightMap[idx] = height;
#if UNITY_EDITOR
				LogMinMax(ref MinVal, ref MaxVal, val);
				LogMinMax(ref MinHeight, ref MaxHeight, height);
#endif
			}
		}

		return heightMap;
	}

	private Color[] CreateGrayscaleMap(float[] heightMap)
	{
		Color[] grayscaleMap = new Color[Resolution * Resolution];

		for (int y = 0; y < Resolution; y++)
		{
			for (int x = 0; x < Resolution; x++)
			{
				grayscaleMap[y * Resolution + x] = Color.Lerp(Color.black, Color.white, heightMap[y * (Resolution+1) + x] / HeightMultiplier);
			}
		}

		return grayscaleMap;
	}

	private Color[] CreateColorMap(float[] heightMap)
	{
		Color[] colorMap = new Color[Resolution * Resolution];

		for (int y = 0; y < Resolution; y++)
		{
			for (int x = 0; x < Resolution; x++)
			{
				var height = heightMap[y * (Resolution+1) + x];

				foreach (var region in Regions)
				{
					if (height < region.Height)
					{
						colorMap[y * Resolution + x] = region.Color;
						break;
					}
				}
			}
		}

		return colorMap;
	}

	public void GenerateMeshData(TerrainTile tile, int lod)
	{
		if (tile.HasMesh(lod)) return;

		lod = lod < 0 ? 0 : lod;

		var meshStepSize = MeshStepSizeByLOD[lod];
		var meshSizeInQuads = tile.MapSize / meshStepSize;
		var vertStep = tile.Area.size.x / meshSizeInQuads;
		var uvStep = 1f / meshSizeInQuads;
		var offset = tile.Area.size / -2f;

		var meshData = new TerrainMeshData(lod, meshSizeInQuads+1);

		; for (int y = 0; y < meshData.SizeInVert; y++)
		{
			for (int x = 0; x < meshData.SizeInVert; x++)
			{
				meshData.Verts.Add(new Vector3(offset.x + x * vertStep, tile.HeightMap[y * meshStepSize * tile.HeightMapSize + x * meshStepSize], offset.y + y * vertStep));
				meshData.UVs.Add(new Vector2(x * uvStep, y * uvStep));

				if (x < meshSizeInQuads && y < meshSizeInQuads)
				{
					var idx = y * meshSizeInQuads + x;
					meshData.AddTriangle(idx + y, idx + y + meshSizeInQuads + 1, idx + y + meshSizeInQuads + 2);
					meshData.AddTriangle(idx + y, idx + y + meshSizeInQuads + 2, idx + y + 1);
				}
			}
		}

		tile.AddOrUpdateMesh(meshData);
	}
#if UNITY_EDITOR
	private void LogMinMax(ref float minVal, ref float maxVal, float val)
    {
		if (val < minVal)
		{
			minVal = val;
		}

		if (val > maxVal)
		{
			maxVal = val;
		}
	}
#endif
}

public class TerrainTile
{
	public readonly string Name;
	public readonly Rect Area;

	public int HeightMapSize;
	public float[] HeightMap;

	public int MapSize;
	public Color[] ColorMap;

	public Dictionary<int, TerrainMeshData> MeshDatas = new();
	public float HeightMapStepSize;

	public TerrainTile()
	{

	}

	public TerrainTile(Rect area)
	{
		Name = $"Tile {area}";
		Area = area;

	}
	public void ClearMeshes()
	{
		MeshDatas.Clear();
	}

	public bool HasMesh(int lod)
	{
		return MeshDatas.ContainsKey(lod);
	}

	public TerrainMeshData GetMeshData(int lod)
    {
		return MeshDatas[lod];
    }

	public void AddOrUpdateMesh(TerrainMeshData meshData)
    {
		if (MeshDatas.ContainsKey(meshData.LOD))
		{
			MeshDatas[meshData.LOD] = meshData;
		}
		else
		{
			MeshDatas.Add(meshData.LOD, meshData);
		}
	}

	public void SetMap(float[] heightMap, Color[] colorMap, int size)
	{
		HeightMap = heightMap;
		ColorMap = colorMap;

		HeightMapSize = size + 1;
		MapSize = size;

		HeightMapStepSize = Area.width / MapSize;
	}

	public float GetHeightAt(Vector2 localPos)
	{
		var heightMapPos = Vector2Int.FloorToInt(localPos / HeightMapStepSize);

		return HeightMap[heightMapPos.y * HeightMapSize + heightMapPos.x];
	}


	public bool FlatHeightMap(Rect flatArea, float flatValue)
	{
		var flatAreaLocal = new Rect(flatArea.position - Area.position, flatArea.size);

		var startPosScaled = Vector2Int.FloorToInt((flatAreaLocal.position) * HeightMapStepSize) - Vector2Int.one;
		var endPosScaled = Vector2Int.FloorToInt((flatAreaLocal.position + flatArea.size) * HeightMapStepSize) + Vector2Int.one * 2;

		var startPos = Vector2Int.Min(Vector2Int.Max(startPosScaled, Vector2Int.zero), new Vector2Int(HeightMapSize, HeightMapSize));
		var endPos = Vector2Int.Min(Vector2Int.Max(endPosScaled, Vector2Int.zero), new Vector2Int(HeightMapSize, HeightMapSize));

		//Debug.Log($"MyTerrainData.FlatHeightMap {Area} {MapSize} {HeightMapStepSize} Flat {flatArea} {flatAreaLocal} {flatValue} SP {startPosScaled} {startPos} EP {endPosScaled} {endPos}");

		var modified = false;
		for (int y = startPos.y; y < endPos.y; y++)
		{
			for (int x = startPos.x; x < endPos.x; x++)
			{
				var idx = y * HeightMapSize + x;
				if (HeightMap[idx] != flatValue)
				{
					HeightMap[idx] = flatValue;
					modified = true;
				}
			}
		}

		return modified;
	}
}

public class TerrainMeshData
{
	public readonly int LOD;
	public readonly int SizeInVert;

	public readonly List<Vector3> Verts = new();
	public readonly List<Vector2> UVs = new();
	public readonly List<int> Tris = new();

	[System.NonSerialized]
	private Mesh _mesh;

    public TerrainMeshData()
    {
    }

	public TerrainMeshData(int lod, int size)
	{
		LOD = lod < 0 ? 0 : lod;
		SizeInVert = size;
	}
	public void AddTriangle(int a, int b, int c)
	{
		Tris.AddRange(new[] { a, b, c });
	}

	public Mesh GetMesh()
    {
		if (_mesh != null) return _mesh;

		_mesh = new Mesh();
		_mesh.name = $"Mesh LOD {LOD}";
		_mesh.SetVertices(Verts);
		_mesh.SetTriangles(Tris, 0);
		_mesh.SetUVs(0, UVs);
		_mesh.RecalculateNormals();
		_mesh.RecalculateTangents();

		_mesh.Optimize();

		return _mesh;
	}

	public void ResetMeshToDefault()
    {
        for (int i = 0; i < SizeInVert; i++)
        {
			//x axis
			var vertexPos = 0 * SizeInVert + i;
			_mesh.vertices[vertexPos] = Verts[vertexPos];
			vertexPos = SizeInVert * SizeInVert + i;
			_mesh.vertices[vertexPos] = Verts[vertexPos];

			//y axis
			vertexPos = i * SizeInVert + 0;
			_mesh.vertices[vertexPos] = Verts[vertexPos];
			vertexPos = i * SizeInVert + SizeInVert;
			_mesh.vertices[vertexPos] = Verts[vertexPos];
		}

		_mesh.RecalculateNormals();
		_mesh.RecalculateTangents();

		//_mesh.Optimize();
	}

	/// <summary>
	/// Neighbour lods should be in order of Left, Forward, Right, Backward
	/// </summary>
	/// <param name="lods"></param>
	public void AdjustMeshToNeighboursLOD(int[] lods)
	{
		if (lods == null) throw new UnityException($"Argument null {nameof(lods)}");
		if (lods.Length != 4) throw new UnityException($"Argument lods has not 4 values but {lods.Length}");

		if (lods[0] == LOD && lods[1] == LOD && lods[2] == LOD && lods[3] == LOD) return;

		var selfLODStep = TerrainGenerator.MeshStepSizeByLOD[LOD];
		var adjustedMeshVerts = new List<Vector3>();
		adjustedMeshVerts.AddRange(Verts);
		var lastIdx = SizeInVert - 1;

		//LEFT
		var neighbourLOD = lods[0];
		if (neighbourLOD > LOD)
        {
			var leftLODStep = TerrainGenerator.MeshStepSizeByLOD[neighbourLOD];
			float stepDif = leftLODStep/(float)selfLODStep;

			float startIdx = 0f;
			float endIdx = stepDif;

			for (int i = 0; i < SizeInVert; i++)
			{
                if (i >= endIdx)
                {
					startIdx = endIdx;
					endIdx += stepDif;

					if (startIdx >= SizeInVert)
					{
						startIdx = SizeInVert - 1;
					}

					if (endIdx >= SizeInVert)
                    {
						endIdx = SizeInVert - 1;
                    }
                }

				var percent = Mathf.InverseLerp(startIdx, endIdx, i);

				var startVert = Verts[Mathf.FloorToInt(startIdx) * SizeInVert + 0];
				var endVert = Verts[Mathf.FloorToInt(endIdx) * SizeInVert + 0];
				var resultVert = adjustedMeshVerts[i * SizeInVert + 0];

				resultVert.y = Mathf.Lerp(startVert.y, endVert.y, percent);
				adjustedMeshVerts[i * SizeInVert + 0] = resultVert;
			}
		}

		//FORWARD
		neighbourLOD = lods[1];
		if (neighbourLOD > LOD)
		{
			var leftLODStep = TerrainGenerator.MeshStepSizeByLOD[neighbourLOD];
			float stepDif = leftLODStep / (float)selfLODStep;

			float startIdx = 0f;
			float endIdx = stepDif;

			for (int i = 0; i < SizeInVert; i++)
			{
				if (i >= endIdx)
				{
					startIdx = endIdx;
					endIdx += stepDif;

					if (startIdx >= SizeInVert)
					{
						startIdx = SizeInVert - 1;
					}

					if (endIdx >= SizeInVert)
					{
						endIdx = SizeInVert - 1;
					}
				}

				var percent = Mathf.InverseLerp(startIdx, endIdx, i);

				var startVert = Verts[lastIdx * SizeInVert + Mathf.FloorToInt(startIdx)];
				var endVert = Verts[lastIdx * SizeInVert + Mathf.FloorToInt(endIdx)];
				var resultVert = adjustedMeshVerts[lastIdx * SizeInVert + i];

				resultVert.y = Mathf.Lerp(startVert.y, endVert.y, percent);
				adjustedMeshVerts[lastIdx * SizeInVert + i] = resultVert;
			}
		}

		//RIGHT
		neighbourLOD = lods[2];
		if (neighbourLOD > LOD)
		{
			var leftLODStep = TerrainGenerator.MeshStepSizeByLOD[neighbourLOD];
			float stepDif = leftLODStep / (float)selfLODStep;

			float startIdx = 0f;
			float endIdx = stepDif;

			for (int i = 0; i < SizeInVert; i++)
			{
				if (i >= endIdx)
				{
					startIdx = endIdx;
					endIdx += stepDif;

					if (startIdx >= SizeInVert)
					{
						startIdx = SizeInVert - 1;
					}

					if (endIdx >= SizeInVert)
					{
						endIdx = SizeInVert - 1;
					}
				}

				var percent = Mathf.InverseLerp(startIdx, endIdx, i);

				var startVert = Verts[Mathf.FloorToInt(startIdx) * SizeInVert + lastIdx];
				var endVert = Verts[Mathf.FloorToInt(endIdx) * SizeInVert + lastIdx];
				var resultVert = adjustedMeshVerts[i * SizeInVert + lastIdx];

				resultVert.y = Mathf.Lerp(startVert.y, endVert.y, percent);
				adjustedMeshVerts[i * SizeInVert + lastIdx] = resultVert;
			}
		}


		//Backward
		neighbourLOD = lods[3];
		if (neighbourLOD > LOD)
		{
			var leftLODStep = TerrainGenerator.MeshStepSizeByLOD[neighbourLOD];
			float stepDif = leftLODStep / (float)selfLODStep;

			float startIdx = 0f;
			float endIdx = stepDif;

			for (int i = 0; i < SizeInVert; i++)
			{
				if (i >= endIdx)
				{
					startIdx = endIdx;
					endIdx += stepDif;

					if (startIdx >= SizeInVert)
					{
						startIdx = SizeInVert - 1;
					}

					if (endIdx >= SizeInVert)
					{
						endIdx = SizeInVert - 1;
					}
				}

				var percent = Mathf.InverseLerp(startIdx, endIdx, i);

				var startVert = Verts[0 * SizeInVert + Mathf.FloorToInt(startIdx)];
				var endVert = Verts[0 * SizeInVert + Mathf.FloorToInt(endIdx)];
				var resultVert = adjustedMeshVerts[0 * SizeInVert + i];

				resultVert.y = Mathf.Lerp(startVert.y, endVert.y, percent);
				adjustedMeshVerts[0 * SizeInVert + i] = resultVert;
			}
		}

		_mesh = new Mesh();
		_mesh.name = $"Mesh LOD {LOD} Adjusted to {string.Join(",", lods)}";
		_mesh.SetVertices(adjustedMeshVerts);
		_mesh.SetTriangles(Tris, 0);
		_mesh.SetUVs(0, UVs);
		_mesh.RecalculateNormals();
		_mesh.RecalculateTangents();

		_mesh.Optimize();
	}

}