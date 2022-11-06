using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class MyTerrainDataTest
{
    [Test]
    public void DefaultData_FlatHeightMapInside_Value50()
    {
        var data = GetDefaultData();

        data.FlatHeightMap(new RectXZ(new VectorXZ(25,25), new VectorXZ(50,50)), 50);

        Assert.AreEqual(50, data.GetHeightAt(new VectorXZ(25, 25)));
        Assert.AreEqual(50, data.GetHeightAt(new VectorXZ(50, 50)));
        Assert.AreEqual(50, data.GetHeightAt(new VectorXZ(25, 25)));
    }

    [Test]
    public void DefaultData_FlatHeightMapOverlapX_Value50()
    {
        var data = GetDefaultData();

        data.FlatHeightMap(new RectXZ(new VectorXZ(-100, 25), new VectorXZ(150, 50)), 50);

        Assert.AreEqual(50, data.GetHeightAt(new VectorXZ(25, 25)));
        Assert.AreEqual(50, data.GetHeightAt(new VectorXZ(50, 50)));
        Assert.AreEqual(50, data.GetHeightAt(new VectorXZ(25, 25)));
    }

    [Test]
    public void DefaultData_FlatHeightMapOverlapXY_Value50()
    {
        var data = GetDefaultData();

        data.FlatHeightMap(new RectXZ(new VectorXZ(-100, -100), new VectorXZ(150, 150)), 50);

        Assert.AreEqual(50, data.GetHeightAt(new VectorXZ(25, 25)));
        Assert.AreEqual(50, data.GetHeightAt(new VectorXZ(50, 50)));
        Assert.AreEqual(50, data.GetHeightAt(new VectorXZ(25, 25)));
    }



    [Test]
    public void LowResData_FlatHeightMapInside_Value50()
    {
        var data = GetLowResData();

        data.FlatHeightMap(new RectXZ(new VectorXZ(25, 25), new VectorXZ(50, 50)), 50);

        Assert.AreEqual(50, data.GetHeightAt(new VectorXZ(25, 25)));
        Assert.AreEqual(50, data.GetHeightAt(new VectorXZ(50, 50)));
        Assert.AreEqual(50, data.GetHeightAt(new VectorXZ(25, 25)));
    }

    [Test]
    public void LowResData_FlatHeightMapOverlapX_Value50()
    {
        var data = GetLowResData();

        data.FlatHeightMap(new RectXZ(new VectorXZ(-100, 25), new VectorXZ(150, 50)), 50);

        Assert.AreEqual(50, data.GetHeightAt(new VectorXZ(25, 25)));
        Assert.AreEqual(50, data.GetHeightAt(new VectorXZ(50, 50)));
        Assert.AreEqual(50, data.GetHeightAt(new VectorXZ(25, 25)));
    }

    [Test]
    public void LowResData_FlatHeightMapOverlapXY_Value50()
    {
        var data = GetLowResData();

        data.FlatHeightMap(new RectXZ(new VectorXZ(-100, -100), new VectorXZ(150, 150)), 50);

        Assert.AreEqual(50, data.GetHeightAt(new VectorXZ(25, 25)));
        Assert.AreEqual(50, data.GetHeightAt(new VectorXZ(50, 50)));
        Assert.AreEqual(50, data.GetHeightAt(new VectorXZ(25, 25)));
    }



    [Test]
    public void HighResData_FlatHeightMapInside_Value50()
    {
        var data = GetHighResData();

        data.FlatHeightMap(new RectXZ(new VectorXZ(25, 25), new VectorXZ(50, 50)), 50);

        Assert.AreEqual(50, data.GetHeightAt(new VectorXZ(25, 25)));
        Assert.AreEqual(50, data.GetHeightAt(new VectorXZ(50, 50)));
        Assert.AreEqual(50, data.GetHeightAt(new VectorXZ(25, 25)));
    }

    [Test]
    public void HighResData_FlatHeightMapOverlapX_Value50()
    {
        var data = GetHighResData();

        data.FlatHeightMap(new RectXZ(new VectorXZ(-100, 25), new VectorXZ(150, 50)), 50);

        Assert.AreEqual(50, data.GetHeightAt(new VectorXZ(25, 25)));
        Assert.AreEqual(50, data.GetHeightAt(new VectorXZ(50, 50)));
        Assert.AreEqual(50, data.GetHeightAt(new VectorXZ(25, 25)));
    }

    [Test]
    public void HighResData_FlatHeightMapOverlapXY_Value50()
    {
        var data = GetHighResData();

        data.FlatHeightMap(new RectXZ(new VectorXZ(-100, -100), new VectorXZ(150, 150)), 50);

        Assert.AreEqual(50, data.GetHeightAt(new VectorXZ(25, 25)));
        Assert.AreEqual(50, data.GetHeightAt(new VectorXZ(50, 50)));
        Assert.AreEqual(50, data.GetHeightAt(new VectorXZ(25, 25)));
    }



    [Test]
    public void DefaultData_GetHeight0x0_Is0()
    {
        var data = GetDefaultData();

        Assert.AreEqual(0, data.GetHeightAt(new VectorXZ(0, 0)));
    }

    [Test]
    public void DefaultData_GetHeight10x10_Is1010()
    {
        var data = GetDefaultData();

        Assert.AreEqual(1020, data.GetHeightAt(new VectorXZ(10, 10)));
    }

    [Test]
    public void LowResData_GetHeight0x0_Is0()
    {
        var data = GetLowResData();

        Assert.AreEqual(0, data.GetHeightAt(new VectorXZ(0, 0)));
    }

    [Test]
    public void LowResData_GetHeight10x10_Is1010()
    {
        var data = GetLowResData();

        Assert.AreEqual(1020, data.GetHeightAt(new VectorXZ(10, 10)));
    }

    [Test]
    public void HighResData_GetHeight0x0_Is0()
    {
        var data = GetHighResData();

        Assert.AreEqual(0, data.GetHeightAt(new VectorXZ(0, 0)));
    }

    [Test]
    public void HighResData_GetHeight10x10_Is1010()
    {
        var data = GetHighResData();

        Assert.AreEqual(1020, data.GetHeightAt(new VectorXZ(10, 10)));
    }

    private static MyTerrainData GetDefaultData()
    {
        int textResolution = 100;
        float size = 100;

        var area = new RectXZ(size / -2f, size / -2f, size, size);

        return new MyTerrainData(area, GenerateHeightMap(textResolution+1, size / textResolution), GenerateColorMap(textResolution));
    }
    private static MyTerrainData GetLowResData()
    {
        int textResolution = 100;
        float size = 200;

        var area = new RectXZ(size / -2f, size / -2f, size, size);

        return new MyTerrainData(area, GenerateHeightMap(textResolution + 1, size / textResolution), GenerateColorMap(textResolution));
    }

    private static MyTerrainData GetHighResData()
    {
        int textResolution = 100;
        float size = 50;

        var area = new RectXZ(size / -2f, size / -2f, size, size);

        return new MyTerrainData(area, GenerateHeightMap(textResolution + 1, size/textResolution), GenerateColorMap(textResolution));
    }

    private static float[,] GenerateHeightMap(int size, float step)
    {
        var result = new float[size,size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                result[x, y] = y*size* step + x* step;
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
