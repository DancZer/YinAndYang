using UnityEngine;

public static class StaticObjectAccessor
{
    private static GameTimeManager gameTimeManager;
    public static GameTimeManager GetTimeManager()
    {
        if (gameTimeManager != null) return gameTimeManager;

        return gameTimeManager = GameObject.FindObjectOfType<GameTimeManager>();
    }

    private static TerrainManager terrainManager;
    public static TerrainManager GetTerrainManager()
    {
        if (terrainManager != null) return terrainManager;

        return terrainManager = GameObject.FindObjectOfType<TerrainManager>();
    }

    private static TerrainGenerator terrainGenerator;
    public static TerrainGenerator GetTerrainGenerator()
    {
        if (terrainGenerator != null) return terrainGenerator;

        return terrainGenerator = GameObject.FindObjectOfType<TerrainGenerator>();
    }

    private static PlayerStatHandler playerStatHandler;
    public static PlayerStatHandler GetPlayerStatHandler()
    {
        if (playerStatHandler != null) return playerStatHandler;

        foreach (var stat in GameObject.FindObjectsOfType<PlayerStatHandler>())
        {
            if (stat != null && stat.IsOwner) return playerStatHandler = stat;
        }

        throw new GodNotFoundException();
    }
}
public class GodNotFoundException : UnityException
{
}