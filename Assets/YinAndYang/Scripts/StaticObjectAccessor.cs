using UnityEngine;

public static class StaticObjectAccessor
{
    private static GameTimeManager gameTimeManager;
    public static GameTimeManager GetTimeManager()
    {
        if (gameTimeManager != null) return gameTimeManager;

        return gameTimeManager = GameObject.FindObjectOfType<GameTimeManager>();
    }

    private static MyTerrainManager myTerrainManager;
    public static MyTerrainManager GetMyTerrainManager()
    {
        if (myTerrainManager != null) return myTerrainManager;

        return myTerrainManager = GameObject.FindObjectOfType<MyTerrainManager>();
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