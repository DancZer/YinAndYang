using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class GodLogic : NetworkBehaviour
{
    /// <summary>
    /// 1 is good, -1 evil
    /// </summary>
    [SyncVar] [Range(-1, 1)] public float GoodEvil;

    public bool IsGood { get { return GoodEvil > 0f; } }
    public float Blend
    {
        get
        {
            return IsGood ? GoodEvil : -GoodEvil;
        }
    }

    public float Blend100
    {
        get
        {
            return Blend * 100;
        }
    }

    void Update()
    {
        if (IsOwner)
        {
            var duration = 8f;
            GoodEvil = Mathf.PingPong(Time.time, duration) / (duration / 2f) - 1;
        }
    }
}
