using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class PlayerStatHandler : NetworkBehaviour
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

    private const float duration = 8f;

    private float _playerSeed;

    public override void OnStartServer()
    {
        base.OnStartServer();

        _playerSeed = Random.Range(0, duration);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        enabled = false;
    }

    void Update()
    {
        if (!IsServer) return;
        
        GoodEvil = Mathf.PingPong(Time.time + _playerSeed, duration) / (duration / 2f) - 1;
    }
}
