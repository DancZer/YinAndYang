using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class BiomeGenerator : MonoBehaviour
{
	public int Resolution = 100;
	[Range(1, 10000)]
	public int BiomeSize = 5;

	public int Seed = 12345;
	[Range(0.00001f, 100000)]
	public FastNoiseLite.CellularDistanceFunction DistanceFunction;
	[Range(0f, 1f)]
	public float Jitter = 1;

	public Biome[] Biomes;

	Dictionary<int, Biome> _biomeMap = new();
	FastNoiseLite _noise;

#if UNITY_EDITOR
	private void OnValidate()
    {
		DrawBiomeMapInEditor();
    }

    private void DrawBiomeMapInEditor()
	{
		SetupGenerator();

		var area = new Rect(transform.position.x + Resolution / -2f, transform.position.z + Resolution / -2f, Resolution, Resolution);
		var tex = GetTexture(area);

		var meshFilter = GetComponent<MeshFilter>();
		meshFilter.sharedMesh = CreatePlaneMesh(area);
		var meshRenderer = GetComponent<MeshRenderer>();
		meshRenderer.sharedMaterial.SetTexture("_MainTex", tex);
	}
	private Mesh CreatePlaneMesh(Rect area)
	{
		var mesh = new Mesh();

		var verts = new List<Vector3>();
		var uvs = new List<Vector2>();

		area.center = Vector2.zero;

		var numFaces = 1;
		verts.Add(new Vector3(area.position.x, 0, area.position.y));
		verts.Add(new Vector3(area.position.x, 0, area.position.y + area.size.y));
		verts.Add(new Vector3(area.position.x + area.size.x, 0, area.position.y + area.size.y));
		verts.Add(new Vector3(area.position.x + area.size.x, 0, area.position.y));

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

	public Texture GetTexture(Rect area)
	{
		var texture = new Texture2D(Resolution, Resolution);
		texture.filterMode = FilterMode.Point;
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.SetPixels(CreateColorMap(area));
		texture.Apply();

		return texture;

	}

	private Color[] CreateColorMap(Rect area)
	{
		Color[] colorMap = new Color[Resolution * Resolution];

		for (int y = 0; y < Resolution; y++)
		{
			for (int x = 0; x < Resolution; x++)
			{
				var biome = GetBiome(area.position.x + x, area.position.y + y);

				if (biome != null)
				{
					colorMap[y * Resolution + x] = biome.Color;
				}
				else
				{
					colorMap[y * Resolution + x] = Color.black;
				}

			}
		}

		return colorMap;
	}

	List<Vector3> lastPosList;

	void Update()
    {
		if (ShouldDraw(new List<Transform> { transform }))
        {
			DrawBiomeMapInEditor();
        }
    }

    private bool ShouldDraw(List<Transform> list)
	{
		if (lastPosList.Count == list.Count)
		{
			var changed = false;

			for (int i = 0; i < list.Count; i++)
			{
				if (lastPosList[i] != list[i].position)
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

    void Start()
    {
		SetupGenerator();
	}

	private void SetupGenerator()
    {
		_noise = new FastNoiseLite(Seed);
		_noise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
		_noise.SetFrequency(1f / BiomeSize);
		_noise.SetCellularDistanceFunction(DistanceFunction);
		_noise.SetCellularJitter(Jitter);
		_noise.SetCellularReturnType(FastNoiseLite.CellularReturnType.CellValue);

		_biomeMap.Clear();

		int biomeId = 0;
        foreach (var biome in Biomes)
        {
			_biomeMap.Add(biomeId, biome);
			biomeId++;
		}
	}

	private Biome GetBiome(float x, float y)
	{
		var biomeID = Mathf.RoundToInt(Mathf.InverseLerp(-1f, 1f, _noise.GetNoise(x, y)) * (Biomes.Length-1));

		return _biomeMap[biomeID];
	}
}

[System.Serializable]
public class Biome
{
	public Color Color;
}