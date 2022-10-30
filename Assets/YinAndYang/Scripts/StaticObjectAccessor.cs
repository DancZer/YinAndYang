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

    public static TimeManager GetTimeManager()
    {
        return GameObject.FindGameObjectWithTag("GameLogic").GetComponent<TimeManager>();
    }

    public static GameObject GetWorldObject()
    {
        return GameObject.FindGameObjectWithTag("WorldObject");
    }

    public static GameObject GetGroundObject()
    {
        return GameObject.FindGameObjectWithTag("GroundObject");
    }

    public class GodNotFoundException : UnityException
    {
    }
}
