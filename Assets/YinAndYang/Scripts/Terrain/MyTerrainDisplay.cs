using System.Collections.Generic;
using UnityEngine;

public class MyTerrainDisplay : MonoBehaviour
{
    public enum DrawMode { NoiseMap, ColourMap, Mesh };


    public void DisplayTerrain(MyTerrainData terrainData, TerrainType[] regions, float heightScale, DrawMode drawMode)
    {
        gameObject.transform.position = new Vector3(terrainData.Area.position.x, 0, terrainData.Area.position.y);
        var meshFilter = gameObject.GetComponent<MeshFilter>();
        var collider = gameObject.GetComponent<MeshCollider>();

        Mesh mesh;

        if(drawMode == DrawMode.Mesh)
        {
            mesh = terrainData.GetMesh(heightScale);
        }
        else
        {
            mesh = GetPlaneMesh(terrainData.Area);

        }

        collider.sharedMesh = meshFilter.mesh = mesh;

        Color[] texColor;
        if(drawMode == DrawMode.Mesh || drawMode == DrawMode.ColourMap)
        {
            texColor = terrainData.GetColorMap(regions);
        }
        else
        {
            texColor = terrainData.GetGrayscaleMap();
        }

        Texture2D mainTex = TextureFromColor(texColor, terrainData.Resolution, terrainData.Resolution);
        var renderer = gameObject.GetComponent<MeshRenderer>();
        renderer.material.SetTexture("_MainTex", mainTex);
    }
    public static Texture2D TextureFromColor(Color[] colourMap, int width, int height)
    {
        var texture = new Texture2D(width, height);

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colourMap); //TODO change it to Color32
        texture.Apply();
        return texture;
    }

    private Mesh GetPlaneMesh(Rect area)
    {
        var mesh = new Mesh();

        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();

        var numFaces = 1;
        verts.Add(new Vector3(area.x, 0, area.y));
        verts.Add(new Vector3(area.x, 0, area.y + area.height));
        verts.Add(new Vector3(area.x + area.width, 0, area.y + area.height));
        verts.Add(new Vector3(area.x + area.width, 0, area.y));

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
