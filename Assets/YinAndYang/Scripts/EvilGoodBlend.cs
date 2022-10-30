using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

/// <summary>
/// Data order should be Evil then Good in the arrays.
/// </summary>
public class EvilGoodBlend : NetworkBehaviour
{
    public enum BlendShapes
    {
        None, GoodOnly, EvilOnly, GoodAndEvil
    }

    public Texture EvilMaterial;
    public Texture NeutralMaterial;
    public Texture GoodMaterial;

    public BlendShapes BlendShape;

    public SkinnedMeshRenderer Renderer;

    /// <summary>
    /// 1 is good, -1 evil
    /// </summary>
    [SyncVar] [Range(-1, 1)] public float GoodEvil;

    private bool IsGood { get { return GoodEvil > 0f; } }
    private float Blend 
    { 
        get 
        {
            return IsGood ? GoodEvil : -GoodEvil;
        } 
    }

    private float Blend100
    {
        get
        {
            return Blend * 100;
        }
    }

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

        UpdateTexture();
        UpdateShape();
    }

    private void UpdateTexture()
    {
        var shaderMaterial = Renderer.sharedMaterial;

        if (IsGood)
        {
            shaderMaterial.SetFloat("_Blend", Blend);
            shaderMaterial.SetTexture("_FrontTex", NeutralMaterial);
            shaderMaterial.SetTexture("_FrontTex2", GoodMaterial);
        }
        else
        {
            shaderMaterial.SetFloat("_Blend", Blend);
            shaderMaterial.SetTexture("_FrontTex", NeutralMaterial);
            shaderMaterial.SetTexture("_FrontTex2", EvilMaterial);
        }
    }

    private void UpdateShape()
    {
        if (BlendShape == BlendShapes.None) return;

        if (IsGood)
        {
            if(BlendShape == BlendShapes.GoodOnly)
            {
                Renderer.SetBlendShapeWeight(0, Blend100);
            }
            else if (BlendShape == BlendShapes.GoodAndEvil)
            {
                Renderer.SetBlendShapeWeight(1, Blend100);
            }
        }
        else
        {
            if (BlendShape == BlendShapes.EvilOnly || BlendShape == BlendShapes.GoodAndEvil)
            {
                Renderer.SetBlendShapeWeight(0, Blend100);
            }
        }
    }
}
