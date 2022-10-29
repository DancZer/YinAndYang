using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class GoodEvilHand : NetworkBehaviour
{
    public Material GoodMaterial;
    public Material NeutralMaterial;
    public Material EvilMaterial;

    public Renderer HandRenderer;

    [SyncVar] [Range(1, -1)] public float GoodEvil;

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    private void Update()
    {
        if (IsOwner)
        {
            var duration = 8f;
            GoodEvil = Mathf.PingPong(Time.time, duration)/(duration/2f) -1;
        }

        if(GoodEvil > 0)
        {
            HandRenderer.material.Lerp(NeutralMaterial, GoodMaterial, GoodEvil);
        }
        else
        {
            HandRenderer.material.Lerp(NeutralMaterial, EvilMaterial, -GoodEvil);
        }
        
    }
}
