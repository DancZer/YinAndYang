using UnityEngine;

public class TerrainManager : MonoBehaviour
{
    public Vector3 GetGroundPosAtCord(Vector3 pos)
    {
        return new Vector3(pos.x, 0, pos.z);
    }
}
