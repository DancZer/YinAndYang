using UnityEngine;

public static class StaticObjectAccessor
{
    public static GodLogic GetGodLogic()
    {
        foreach(var go in GameObject.FindGameObjectsWithTag("Player")){
            var god = go.GetComponent<GodLogic>();

            if (god != null && god.IsOwner) return god;
        }

        throw new GodNotFoundException();
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
