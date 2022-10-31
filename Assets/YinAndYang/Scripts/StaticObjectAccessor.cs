using UnityEngine;

public static class StaticObjectAccessor
{
    public static PlayerStatHandler GetPlayerStatHandler()
    {
        foreach(var go in GameObject.FindGameObjectsWithTag("Player")){
            var stat = go.GetComponent<PlayerStatHandler>();

            if (stat != null && stat.IsOwner) return stat;
        }

        throw new GodNotFoundException();
    }
    public static GameObject GetPlayerTemple()
    {
        return GameObject.FindGameObjectWithTag("Temple");
    }

    public static GameTimeManager GetTimeManager()
    {
        return GameObject.FindGameObjectWithTag("GameLogic").GetComponent<GameTimeManager>();
    }
    public static MyTerrainManager GetMyTerrainManager()
    {
        return GetTerrainObject().GetComponent<MyTerrainManager>();
    }
    public static GameObject GetTerrainObject()
    {
        return GameObject.FindGameObjectWithTag("TerrainObject");
    }

    public class GodNotFoundException : UnityException
    {
    }
}
