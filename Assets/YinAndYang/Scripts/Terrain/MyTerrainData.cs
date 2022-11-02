using System.Collections.Generic;
using UnityEngine;

public class MyTerrainData
{
	public readonly Rect Area;
    public readonly float[,] HeightMap;
    public readonly float Magnitude;
    public readonly Color[] ColorMap;

	public readonly int Resolution;
	public readonly int Height;

	public MyTerrainData(Rect area, float[,] heightMap, int resolution, float magnitude, Color[] colorMap)
    {
		Area = area;
		HeightMap = heightMap;
		Magnitude = magnitude;
		ColorMap = colorMap;
		Resolution = resolution;
	}

    public Mesh GetMesh(float heightScale)
    {
		var mesh = new Mesh();

		var verts = new List<Vector3>();
		var uvs = new List<Vector2>();

		var numFaces = 0;

		var vertStep = Area.size.x / Resolution;
		var uvStep = 1f / Resolution;

		for (int x = 0; x < Resolution; x++)
		{
			for (int y = 0; y < Resolution; y++)
			{
				verts.Add(new Vector3(x * vertStep, HeightMap[x, y] * heightScale, y * vertStep));
				verts.Add(new Vector3(x * vertStep, HeightMap[x, y+1] * heightScale, (y + 1) * vertStep));
				verts.Add(new Vector3((x + 1) * vertStep, HeightMap[x+1, y+1] * heightScale, (y + 1) * vertStep));
				verts.Add(new Vector3((x + 1) * vertStep, HeightMap[x+1, y] * heightScale, y * vertStep));

				uvs.Add(new Vector2(x * uvStep, y * uvStep));
				uvs.Add(new Vector2(x * uvStep, (y+1) * uvStep));
				uvs.Add(new Vector2((x+1) * uvStep, (y + 1) * uvStep));
				uvs.Add(new Vector2((x + 1) * uvStep, y * uvStep));

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
}
