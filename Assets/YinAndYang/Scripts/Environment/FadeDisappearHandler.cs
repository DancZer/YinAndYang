using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

public class FadeDisappearHandler : NetworkBehaviour
{
    public float TimeoutInSec = 10;
    public float FadeOutInSec = 5;

    private float _timeTillFadeOut;
    private float _transparentChangeInSec;

    private Material _material;

    public override void OnStartClient()
    {
        base.OnStartClient();

        _material = GetComponentInChildren<MeshRenderer>().material;

        if (IsOwner)
        {
            _timeTillFadeOut = TimeoutInSec;
            _transparentChangeInSec = 1f / FadeOutInSec;    
        }
        else
        {
            enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;

        _timeTillFadeOut -= Time.deltaTime;

        if(_timeTillFadeOut < 0){
            Color color = _material.color;
            var alpha = (-_timeTillFadeOut)*_transparentChangeInSec;

            color.a = Mathf.Clamp( alpha, 0, 1 );

            ChangeColorServer(color);

            if (_timeTillFadeOut < -FadeOutInSec){
                DespawnDirtLump();
            }
        }
    }

    [ServerRpc]
    public void ChangeColorServer(Color color, NetworkConnection conn = null)
    {
        ChangeColor(color);
    }

    [ObserversRpc]
    public void ChangeColor(Color color)
    {
        _material.color = color;
    }

    [ServerRpc]
    public void DespawnDirtLump(NetworkConnection conn = null)
    {
        ServerManager.Despawn(gameObject);
    }
}
