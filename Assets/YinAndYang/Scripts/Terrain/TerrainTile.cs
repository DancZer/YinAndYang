using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
public enum TerrainTileState
{
	Empty = 0,
	BiomeMap = 1,
	HeightMap = 2,
	BlendedHeightMap = 3,
	MeshData = 4,
	AdjustedMeshData = 5
}

public class TerrainTile
{
	public readonly string Id;
	public readonly Vector2 PhysicalPos;
	public readonly int DataSize;
	public readonly int BlendSize;
	public readonly Vector2Int BlendSizeVect;

	public float[] HeightDataMap;
	public int[] BiomeDataMap;

	/// <summary>
	/// BiomeID * TileMeshResolution * TileMeshResolution + y * TileMeshResolution + x
	/// </summary>
	public float[] BiomeWeightColorMap;

	public readonly Dictionary<int, TerrainMeshData> MeshDatas = new();

	public TerrainTileState CurrentState;
	public TerrainTileState PreviousState;
	public float LastChangedTime = 0;

	public Vector2 PhysicalCenter
	{
		get
		{
			return PhysicalPos + TerrainGenerator.TilePhysicalSizeHalfVect;
		}
	}

	public TerrainTile()
	{
	}

	public TerrainTile(Vector2 pos, int tileDataResolution, int blendSize)
	{
		Id = $"Tile_{pos}";
		PhysicalPos = pos;
		DataSize = tileDataResolution + 2 * blendSize;
		BlendSize = blendSize;
		BlendSizeVect = new Vector2Int(blendSize, blendSize);
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
	public void SetState(TerrainTileState newState)
	{
		PreviousState = CurrentState;
		CurrentState = newState;
	}

	public Texture2D CreateBiomeMapText(int biomeId)
    {
		return CreateTexture(GetBiomeMapColors(biomeId));
	}

	public static Texture2D CreateTexture(Color[] colors)
	{
		var texture = new Texture2D(TerrainGenerator.TileMeshResolution, TerrainGenerator.TileMeshResolution);
		texture.filterMode = FilterMode.Point;
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.SetPixels(colors);
		texture.Apply();

		return texture;

	}

	public Texture2DArray CreateBiomeMapWeightTexArray(BiomeLayerData biomeData)
    {
		var texArray = new Texture2DArray(TerrainGenerator.TileMeshResolution, TerrainGenerator.TileMeshResolution, biomeData.BiomeCount, TextureFormat.RGB565, false);

        for (int biomeId = 0; biomeId < biomeData.BiomeCount; biomeId++)
        {
			texArray.SetPixels(GetBiomeMapColors(biomeId), biomeId);
		}
		texArray.Apply();

		return texArray;
    }

	private Color[] GetBiomeMapColors(int biomeId)
	{
		var result = new Color[TerrainGenerator.TileMeshResolution * TerrainGenerator.TileMeshResolution];

		for (int mY = 0; mY < TerrainGenerator.TileMeshResolution; mY++)
		{
			for (int mX = 0; mX < TerrainGenerator.TileMeshResolution; mX++)
			{
				result[mY * TerrainGenerator.TileMeshResolution + mX] = GetBiomeMapColor(mX, mY, biomeId);
			}
		}

		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Color GetBiomeMapColor(int mX, int mY, int biomeId)
    {
		var weight = BiomeWeightColorMap[biomeId * TerrainGenerator.TileMeshResolution2 + mY * TerrainGenerator.TileMeshResolution + mX];

		return Color.Lerp(Color.black, Color.white, weight);
	}

	public float GetHeightAt(Vector2 localPos)
	{
		var heightMapPos = TerrainGenerator.PhysicalSizeToDataResolution(localPos);

		//Debug.Log($"GetHeightAt Tile:{this}, localPos:{localPos}, heightMapPos:{heightMapPos}");

		return HeightDataMap[(heightMapPos.y + BlendSize) * DataSize + (heightMapPos.x + BlendSize)];
	}

	public bool FlatHeightMap(Rect globalFlatArea, float flatValue)
	{
		var flatAreaPhysLocal = new Rect(globalFlatArea.position - PhysicalPos, globalFlatArea.size);

		var dataStartPos = TerrainGenerator.PhysicalSizeToDataResolution(flatAreaPhysLocal.position) - Vector2Int.one + BlendSizeVect;
		var dataEndPos = TerrainGenerator.PhysicalSizeToDataResolution(flatAreaPhysLocal.position + globalFlatArea.size) + Vector2Int.one * 2 + BlendSizeVect;

		if (dataStartPos.x < 0 || dataStartPos.y < 0 || dataEndPos.x >= DataSize || dataEndPos.y >= DataSize) return false;

		//Debug.Log($"FlatHeightMap tile:{this}, globalFlatArea:{globalFlatArea}, flatValue:{flatValue}, flatAreaPhysLocal:{flatAreaPhysLocal}, dataStartPos:{dataStartPos}, dataEndPos:{dataEndPos}");

		var modified = false;
		for (int dY = dataStartPos.y; dY < dataEndPos.y; dY++)
		{
			for (int dX = dataStartPos.x; dX < dataEndPos.x; dX++)
			{
				var dIdx = dY * DataSize + dX;
				if (HeightDataMap[dIdx] != flatValue)
				{
					HeightDataMap[dIdx] = flatValue;
					modified = true;
				}
			}
		}

		return modified;
	}

	/// <summary>
	/// Returns null if no preset should be applied
	/// </summary>
	/// <param name="viewPos"></param>
	/// <param name="requiredStatePresets"></param>
	/// <returns></returns>
	public RequiredTileStatePreset SelectTilePreset(Vector2 viewPos, RequiredTileStatePreset[] requiredStatePresets)
	{
		var tileDistance = Vector2.Distance(viewPos, PhysicalPos);

		return requiredStatePresets.FirstOrDefault(p => p.Distance > tileDistance);
	}

	public override string ToString()
	{
		return $"Tile Id:{Id} Pos:{PhysicalPos}, CurrentState:{CurrentState}, PreviousState:{PreviousState}, DataSize:{DataSize}, BlendSize:{BlendSize} LastChangedTime:{LastChangedTime} BiomeDataMap:{BiomeDataMap.Length} BiomeWeightColorMap:{BiomeWeightColorMap.Length}";
	}
}
