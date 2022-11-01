using System.Collections.Generic;
using UnityEngine;

public class MyTerrainMeshProvider : MonoBehaviour
{
	public int Seed = 12345;

	public NoisePreset[] NoisePresets;
	public float Slices = 2;

	private FastNoise _noise = new FastNoise();

	public void Start()
    {
		_noise.SetSeed(Seed);
	}

    public Mesh CreateMesh(MyTerrainTile tile)
    {
		var mesh = new Mesh
		{
			name = "Procedural Mesh"
		};
		var area = tile.Area;

		mesh.vertices = new Vector3[] {
			new Vector3(area.position.x, 0, area.position.y),
			new Vector3(area.position.x+area.size.x, 0, area.position.y),
			new Vector3(area.position.x, 0, area.position.y + area.size.y),
			new Vector3(area.position.x+area.size.x, 0, area.position.y + area.size.y)
		};

		mesh.normals = new Vector3[] {
			Vector3.up, Vector3.up, Vector3.up, Vector3.up
		};
		/*
		mesh.tangents = new Vector4[] {
			new Vector4(1f, 0f, 0f, -1f),
			new Vector4(1f, 0f, 0f, -1f),
			new Vector4(1f, 0f, 0f, -1f),
			new Vector4(1f, 0f, 0f, -1f)
		};*/

		mesh.uv = new Vector2[] {
			Vector2.zero, Vector2.right, Vector2.up, Vector2.one
		};

		mesh.triangles = new int[] {
			0, 2, 1, 1, 2, 3
		};

		return mesh;
	}

	public Mesh CreateSimpleProceduralMesh(MyTerrainTile tile)
	{
		var mesh = new Mesh
		{
			name = "Procedural Mesh"
		};

		var area = tile.Area;

		mesh.vertices = new Vector3[] {
			GetVertice(area.position), 
			GetGroundLevelPos(area.position.x + area.size.x, area.position.y),
			GetGroundLevelPos(area.position.x, area.position.y + area.size.y),
			GetGroundLevelPos(area.position.x + area.size.x, area.position.y + area.size.y)
		};

        foreach (var ver in mesh.vertices)
        {
			Debug.Log($"CreateProceduralMesh {area} {ver}");
		}

		mesh.normals = new Vector3[] {
			Vector3.up, Vector3.up, Vector3.up, Vector3.up
		};
		/*
		mesh.tangents = new Vector4[] {
			new Vector4(1f, 0f, 0f, -1f),
			new Vector4(1f, 0f, 0f, -1f),
			new Vector4(1f, 0f, 0f, -1f),
			new Vector4(1f, 0f, 0f, -1f)
		};*/

		mesh.uv = new Vector2[] {
			Vector2.zero, Vector2.right, Vector2.up, Vector2.one
		};

		mesh.triangles = new int[] {
			0, 2, 1, 
			1, 2, 3
		};

		return mesh;
	}


	public Mesh CreateSmoothProceduralMesh(MyTerrainTile tile)
	{
		var mesh = new Mesh
		{
			name = "Procedural Mesh"
		};

		var verts = new List<Vector3>();
		var uvs = new List<Vector2>();

		mesh.GetVertices(verts);
		mesh.GetUVs(0, uvs);

		var numFaces = 0;
		var area = tile.Area;

		var xStep = area.size.x / Slices;
		var yStep = area.size.x / Slices;

		for (int x = 0; x < Slices; x++)
        {
			for (int y = 0; y < Slices; y++)
			{
				verts.Add(GetGroundLevelPos(area.position.x + x * xStep, area.position.y + y * yStep));
				verts.Add(GetGroundLevelPos(area.position.x + x * xStep, area.position.y + (y+1) * yStep));
				verts.Add(GetGroundLevelPos(area.position.x + (x+1) * xStep, area.position.y + (y+1) * yStep));
				verts.Add(GetGroundLevelPos(area.position.x + (x+1) * xStep, area.position.y + y * yStep));

				uvs.Add(new Vector2(0, 0));
				uvs.Add(new Vector2(0, 1));
				uvs.Add(new Vector2(1, 1));
				uvs.Add(new Vector2(1, 0));
			
				numFaces++;
			}
		}

		var tris = new List<int>();
		int tl = verts.Count - 4 * numFaces;
		for (int i = 0; i < numFaces; i++)
		{
			tris.AddRange(new int[] { tl + i * 4, tl + i * 4 + 1, tl + i * 4 + 2, tl + i * 4, tl + i * 4 + 2, tl + i * 4 + 3 });
		}

		mesh.SetVertices(verts);
		mesh.SetTriangles(tris, 0);
		mesh.SetUVs(0, uvs);

		return mesh;
	}

	private Vector3 GetVertice(Vector2 v)
	{
		return GetGroundLevelPos(v.x, v.y);
	}

	public Vector3 GetGroundLevelPos(float x, float y)
    {
		return new Vector3(x, GetNoiseVal(x, y), y);
    }

	private float GetNoiseVal(float x, float y)
	{
		var result = 0f;

		foreach (var preset in NoisePresets)
		{
			result = preset.GetNoiseValue(_noise, x, y, result);
		}

		return result;
	}
}
