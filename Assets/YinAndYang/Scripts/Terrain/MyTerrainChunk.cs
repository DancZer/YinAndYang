using System;
using UnityEngine;

public class MyTerrainChunk
{
    public readonly MyTerrainTile Root;
    public readonly float Tile0Size;

    public MyTerrainChunk(int level, Rect area)
    {
        Debug.Log($"MyTerrainChunk {area}");
        Root = new MyTerrainTile(level, area);
        Tile0Size = area.width / (level+1);
    }

    public void Update(ViewDistnaceHandler viewDistnaceHandler)
    {
        Root.Update(viewDistnaceHandler);
    }
}
