using System.Collections.Generic;
using UnityEngine;

public class MyTerrainGenerator : MonoBehaviour
{
	public enum TerrainDrawMode { HeightMap, ColourMap, Mesh };

	public TerrainDrawMode DrawMode;
	public int EditorTerrainSize = 240;
	public bool EditorAutoUpdate;
	[Range(1,6)] public int EditorLOD = 1;

	public int TerrainResolution = 240;

	public float HeightMultiplier = 100;
	public AnimationCurve HeightCurve;
	public MyTerrainRegionPreset[] Regions;

	public FastNoise.NoiseType NoiseType;
	public int Seed = 1234;
	public float Frequency = 0.5f;
	public int Octaves = 3;
	public float Gain = 2;
	public float Lacunarity = 2;

	public void DrawTerrainInEditor()
	{
		MyTerrainData mapData = GenerateTerrainData(new Rect(new Vector2(transform.position.x-EditorTerrainSize/2f, transform.position.y-EditorTerrainSize/2f), new Vector2(EditorTerrainSize, EditorTerrainSize)));
		MyTerrainMeshData meshData = GenerateMeshData(mapData, EditorLOD);

		var meshFilter = GetComponent<MeshFilter>();
		var meshRenderer = GetComponent<MeshRenderer>();

		if (DrawMode == TerrainDrawMode.HeightMap || DrawMode == TerrainDrawMode.ColourMap)
		{
			meshFilter.mesh = CreatePlaneMesh(mapData.Area);
		}
		else if (DrawMode == TerrainDrawMode.Mesh)
		{
			meshFilter.mesh = meshData.CreateMesh();
		}
		meshRenderer.sharedMaterial.SetTexture("_MainTex", meshData.CreateTexture());
	}

	private Mesh CreatePlaneMesh(Rect area)
	{
		var mesh = new Mesh();

		var verts = new List<Vector3>();
		var uvs = new List<Vector2>();

		var offset = area.position;

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
	public MyTerrainData GenerateTerrainData(Rect area)
	{
		var noiseMap = CreateNoiseMap(area);
		Color[] colorMap;

		if (DrawMode == TerrainDrawMode.HeightMap)
		{
			colorMap = CreateHeightMap(noiseMap);
		}
        else
        {
			colorMap = CreateColorMap(noiseMap);
		}

		return new MyTerrainData(area, TerrainResolution, noiseMap, colorMap);
	}

	private float[,] CreateNoiseMap(Rect area)
    {
		var noise = new FastNoise(Seed);
		
		noise.SetFractalOctaves(Octaves);
		noise.SetFractalGain(Gain);
		noise.SetFractalLacunarity(Lacunarity);
		noise.SetFrequency(Frequency);
		noise.SetNoiseType(NoiseType);

		int NoiseMapSize = TerrainResolution + 1;

		float[,] noiseMap = new float[NoiseMapSize, NoiseMapSize];

		var noiseMapStep = area.size.x / TerrainResolution;
		var offset = area.position;

		for (int y = 0; y < NoiseMapSize; y++)
		{
			for (int x = 0; x < NoiseMapSize; x++)
			{
				noiseMap[x, y] = noise.GetNoise(offset.x + x * noiseMapStep, offset.y + y * noiseMapStep)+0.5f;
			}
		}

		return noiseMap;
	}
	private Color[] CreateHeightMap(float[,] noiseMap)
	{
		Color[] heightMap = new Color[TerrainResolution * TerrainResolution];
		var heightCurve = new AnimationCurve(HeightCurve.keys);

		for (int y = 0; y < TerrainResolution; y++)
		{
			for (int x = 0; x < TerrainResolution; x++)
			{
				var height = heightCurve.Evaluate(noiseMap[x, y]);

				heightMap[y * TerrainResolution + x] = Color.Lerp(Color.black, Color.white, height);
			}
		}

		return heightMap;
	}

	private Color[] CreateColorMap(float[,] noiseMap)
	{
		Color[] colorMap = new Color[TerrainResolution * TerrainResolution];
		var heightCurve = new AnimationCurve(HeightCurve.keys);

		for (int y = 0; y < TerrainResolution; y++)
		{
			for (int x = 0; x < TerrainResolution; x++)
			{
				var noise = noiseMap[x, y];
				var height = heightCurve.Evaluate(noise);

				foreach (var region in Regions)
				{
					if (height < region.Height)
					{
						colorMap[y * TerrainResolution + x] = region.Colour;
						break;
					}
				}
			}
		}

		return colorMap;
	}


	public MyTerrainMeshData GenerateMeshData(MyTerrainData terrainData, int lod)
	{
		if (lod <= 0) lod = 1;

		var mesh = new MyTerrainMeshData(terrainData, lod);

		var vertStep = terrainData.Area.size.x / mesh.MeshResolution;
		var uvStep = 1f / mesh.MeshResolution;
		var heightCurve = new AnimationCurve(HeightCurve.keys);
		var offset = terrainData.Area.position;

		; for (int y = 0; y < mesh.MeshResolution + 1; y++)
		{
			for (int x = 0; x < mesh.MeshResolution + 1; x++)
			{
				var height = heightCurve.Evaluate(terrainData.NoiseMap[x, y]) * HeightMultiplier;

				mesh.AddVertice(new Vector3(offset.x + x * vertStep, height, offset.y + y * vertStep));
				mesh.AddUV(new Vector2(x * uvStep, y * uvStep));

				if (x < mesh.MeshResolution && y < mesh.MeshResolution)
				{
					var idx = y * mesh.MeshResolution + x;
					mesh.AddTriangle(idx + y, idx + y + mesh.MeshResolution + 1, idx + y + mesh.MeshResolution + 2);
					mesh.AddTriangle(idx + y, idx + y + mesh.MeshResolution + 2, idx + y + 1);
				}
			}
		}

		return mesh;
	}
}


public class MyTerrainData
{
	public readonly Rect Area;
	public readonly int Resolution;

	public readonly float[,] NoiseMap;
	public readonly Color[] ColorMap;

    public MyTerrainData(Rect area, int resolution, float[,] heightMap, Color[] colorMap)
	{
		Area = area;
		Resolution = resolution;
        NoiseMap = heightMap;
        ColorMap = colorMap;
    }
}

public class MyTerrainMeshData
{
	public readonly MyTerrainData TerrainData;
	public readonly int LOD;
	public readonly int MeshResolution;

	private List<Vector3> _verts = new List<Vector3>();
	private List<Vector2> _uvs = new List<Vector2>();
	private List<int> _tris = new List<int>();

    public MyTerrainMeshData(MyTerrainData terrainData, int lod)
    {
		TerrainData = terrainData;
		LOD = lod <= 0 ? 1 : lod;
		MeshResolution = terrainData.Resolution / lod;
	}

	public void AddVertice(Vector3 vertice)
    {
		_verts.Add(vertice);
    }

	public void AddUV(Vector2 uv)
	{
		_uvs.Add(uv);
	}
	public void AddTriangle(int a, int b, int c)
	{
		_tris.AddRange(new[] { a, b, c });
	}

	public Mesh CreateMesh()
    {
		var mesh = new Mesh();
		mesh.SetVertices(_verts);
		mesh.SetTriangles(_tris, 0);
		mesh.SetUVs(0, _uvs);
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();

		mesh.Optimize();

		return mesh;
	}

	public Texture2D CreateTexture()
	{
		var texture = new Texture2D(TerrainData.Resolution, TerrainData.Resolution);

		texture.filterMode = FilterMode.Point;
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.SetPixels(TerrainData.ColorMap);
		texture.Apply();
		return texture;
	}
}