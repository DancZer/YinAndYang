using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class TerrainTileTest
{
    [Test]
    public void DefaultData_FlatHeightMapInside_Value50()
    {
        var data = GetDefaultData();

        data.FlatHeightMap(new Rect(new Vector2(25,25), new Vector2(50,50)), 50);

        Assert.AreEqual(50, data.GetHeightAt(new Vector2(25, 25)));
        Assert.AreEqual(50, data.GetHeightAt(new Vector2(50, 50)));
        Assert.AreEqual(50, data.GetHeightAt(new Vector2(25, 25)));
    }

    [Test]
    public void DefaultData_FlatHeightMapOverlapX_Value50()
    {
        var data = GetDefaultData();

        data.FlatHeightMap(new Rect(new Vector2(-100, 25), new Vector2(150, 50)), 50);

        Assert.AreEqual(50, data.GetHeightAt(new Vector2(25, 25)));
        Assert.AreEqual(50, data.GetHeightAt(new Vector2(50, 50)));
        Assert.AreEqual(50, data.GetHeightAt(new Vector2(25, 25)));
    }

    [Test]
    public void DefaultData_FlatHeightMapOverlapXY_Value50()
    {
        var data = GetDefaultData();

        data.FlatHeightMap(new Rect(new Vector2(-100, -100), new Vector2(150, 150)), 50);

        Assert.AreEqual(50, data.GetHeightAt(new Vector2(25, 25)));
        Assert.AreEqual(50, data.GetHeightAt(new Vector2(50, 50)));
        Assert.AreEqual(50, data.GetHeightAt(new Vector2(25, 25)));
    }



    [Test]
    public void LowResData_FlatHeightMapInside_Value50()
    {
        var data = GetLowResData();

        data.FlatHeightMap(new Rect(new Vector2(25, 25), new Vector2(50, 50)), 50);

        Assert.AreEqual(50, data.GetHeightAt(new Vector2(25, 25)));
        Assert.AreEqual(50, data.GetHeightAt(new Vector2(50, 50)));
        Assert.AreEqual(50, data.GetHeightAt(new Vector2(25, 25)));
    }

    [Test]
    public void LowResData_FlatHeightMapOverlapX_Value50()
    {
        var data = GetLowResData();

        data.FlatHeightMap(new Rect(new Vector2(-100, 25), new Vector2(150, 50)), 50);

        Assert.AreEqual(50, data.GetHeightAt(new Vector2(25, 25)));
        Assert.AreEqual(50, data.GetHeightAt(new Vector2(50, 50)));
        Assert.AreEqual(50, data.GetHeightAt(new Vector2(25, 25)));
    }

    [Test]
    public void LowResData_FlatHeightMapOverlapXY_Value50()
    {
        var data = GetLowResData();

        data.FlatHeightMap(new Rect(new Vector2(-100, -100), new Vector2(150, 150)), 50);

        Assert.AreEqual(50, data.GetHeightAt(new Vector2(25, 25)));
        Assert.AreEqual(50, data.GetHeightAt(new Vector2(50, 50)));
        Assert.AreEqual(50, data.GetHeightAt(new Vector2(25, 25)));
    }



    [Test]
    public void HighResData_FlatHeightMapInside_Value50()
    {
        var data = GetHighResData();

        data.FlatHeightMap(new Rect(new Vector2(25, 25), new Vector2(50, 50)), 50);

        Assert.AreEqual(50, data.GetHeightAt(new Vector2(25, 25)));
        Assert.AreEqual(50, data.GetHeightAt(new Vector2(50, 50)));
        Assert.AreEqual(50, data.GetHeightAt(new Vector2(25, 25)));
    }

    [Test]
    public void HighResData_FlatHeightMapOverlapX_Value50()
    {
        var data = GetHighResData();

        data.FlatHeightMap(new Rect(new Vector2(-100, 25), new Vector2(150, 50)), 50);

        Assert.AreEqual(50, data.GetHeightAt(new Vector2(25, 25)));
        Assert.AreEqual(50, data.GetHeightAt(new Vector2(50, 50)));
        Assert.AreEqual(50, data.GetHeightAt(new Vector2(25, 25)));
    }

    [Test]
    public void HighResData_FlatHeightMapOverlapXY_Value50()
    {
        var data = GetHighResData();

        data.FlatHeightMap(new Rect(new Vector2(-100, -100), new Vector2(150, 150)), 50);

        Assert.AreEqual(50, data.GetHeightAt(new Vector2(25, 25)));
        Assert.AreEqual(50, data.GetHeightAt(new Vector2(50, 50)));
        Assert.AreEqual(50, data.GetHeightAt(new Vector2(25, 25)));
    }



    [Test]
    public void DefaultData_GetHeight0x0_Is0()
    {
        var data = GetDefaultData();

        Assert.AreEqual(0, data.GetHeightAt(new Vector2(0, 0)));
    }

    [Test]
    public void DefaultData_GetHeight10x10_Is1010()
    {
        var data = GetDefaultData();

        Assert.AreEqual(1020, data.GetHeightAt(new Vector2(10, 10)));
    }

    [Test]
    public void LowResData_GetHeight0x0_Is0()
    {
        var data = GetLowResData();

        Assert.AreEqual(0, data.GetHeightAt(new Vector2(0, 0)));
    }

    [Test]
    public void LowResData_GetHeight10x10_Is1010()
    {
        var data = GetLowResData();

        Assert.AreEqual(1020, data.GetHeightAt(new Vector2(10, 10)));
    }

    [Test]
    public void HighResData_GetHeight0x0_Is0()
    {
        var data = GetHighResData();

        Assert.AreEqual(0, data.GetHeightAt(new Vector2(0, 0)));
    }

    [Test]
    public void HighResData_GetHeight10x10_Is1010()
    {
        var data = GetHighResData();

        Assert.AreEqual(1020, data.GetHeightAt(new Vector2(10, 10)));
    }

    private static TerrainTile GetDefaultData()
    {
        int textResolution = 100;
        float size = 100;

        var area = new Rect(size / -2f, size / -2f, size, size);

        var tile = new TerrainTile(area);
        tile.SetMap(GenerateHeightMap(textResolution + 1, size / textResolution), GenerateColorMap(textResolution), textResolution);

        return tile;
    }
    private static TerrainTile GetLowResData()
    {
        int textResolution = 100;
        float size = 200;

        var area = new Rect(size / -2f, size / -2f, size, size);

        var tile = new TerrainTile(area);
        tile.SetMap(GenerateHeightMap(textResolution + 1, size / textResolution), GenerateColorMap(textResolution), textResolution);

        return tile;
    }

    private static TerrainTile GetHighResData()
    {
        int textResolution = 100;
        float size = 50;

        var area = new Rect(size / -2f, size / -2f, size, size);

        var tile = new TerrainTile(area);
        tile.SetMap(GenerateHeightMap(textResolution + 1, size / textResolution), GenerateColorMap(textResolution), textResolution);

        return tile;
    }

    private static float[] GenerateHeightMap(int size, float step)
    {
        var result = new float[size*size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                result[y*size+x] = y*size* step + x* step;
            }
        }

        return result;
    }
    private static Color[] GenerateColorMap(int size)
    {
        var result = new Color[size * size];

        var maxVal = size * size;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var i = y * size + x;
                result[y * size + x] = Color.Lerp(Color.white, Color.black, Mathf.Clamp(i, 0, maxVal));
            }
        }

        return result;
    }
}
