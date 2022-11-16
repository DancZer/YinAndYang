using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MiscHelper
{
    public static bool IsOnTheScreen(Vector3 vector)
    {
        return vector.x >= 0 && vector.x <= Screen.width && vector.y >= 0 && vector.y <= Screen.height;
    }

    public static int ToTilePos(float v)
    {
        return Mathf.RoundToInt(v / TerrainGenerator.TileSize) * TerrainGenerator.TileSize - TerrainGenerator.TileSizeHalf;
    }
}
