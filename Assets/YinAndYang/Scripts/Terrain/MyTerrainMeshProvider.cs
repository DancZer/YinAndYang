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

	public Mesh CreateProceduralMesh(MyTerrainTile tile)
	{
		var mesh = new Mesh
		{
			name = $"{tile.TileName}"
		};

		var verts = new List<Vector3>();
		var uvs = new List<Vector2>();
		//var normals = new List<Vector3>();

		mesh.GetVertices(verts);
		//mesh.GetNormals(normals);
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
			var p0 = tl + i * 4;
			var p1 = tl + i * 4 + 1;
			var p2 = tl + i * 4 + 2;
			var p3 = tl + i * 4 + 3;
			/*
			var v0 = verts[p0];
			var v1 = verts[p1];
			var v2 = verts[p2];
			var v3 = verts[p3];

			normals.Add(Vector3.Cross(v1 - v0, v2 - v0).normalized);
			normals.Add(Vector3.Cross(v2 - v0, v3 - v0).normalized);*/

			tris.AddRange(new int[] { 
				p0, p1, p2, 
				p0, p2, p3 }
			);
		}

		mesh.SetVertices(verts);
		//mesh.SetNormals(normals);
		mesh.SetTriangles(tris, 0);
		mesh.SetUVs(0, uvs);
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();

		mesh.Optimize();

		return mesh;
	}

	private Vector3 GetGroundLevelPos(float x, float y)
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
