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

    public Texture Evil;
    public Texture Neutral;
    public Texture Good;

    public BlendShapes BlendShape;

    public SkinnedMeshRenderer Renderer;

    private GodLogic _godLogic;

    public override void OnStartClient()
    {
        base.OnStartClient();

        _godLogic = StaticObjectAccessor.GetGodLogic();
    }

    private void Update()
    {
        if (_godLogic == null) return;

        UpdateTexture();
        UpdateShape();
    }

    private void UpdateTexture()
    {
        var shaderMaterial = Renderer.sharedMaterial;

        if (_godLogic.IsGood)
        {
            shaderMaterial.SetFloat("_Blend", _godLogic.Blend);
            shaderMaterial.SetTexture("_FrontTex", Neutral);
            shaderMaterial.SetTexture("_FrontTex2", Good);
        }
        else
        {
            shaderMaterial.SetFloat("_Blend", _godLogic.Blend);
            shaderMaterial.SetTexture("_FrontTex", Neutral);
            shaderMaterial.SetTexture("_FrontTex2", Evil);
        }
    }

    private void UpdateShape()
    {
        if (BlendShape == BlendShapes.None) return;

        if (_godLogic.IsGood)
        {
            if(BlendShape == BlendShapes.GoodOnly)
            {
                Renderer.SetBlendShapeWeight(0, _godLogic.Blend100);
            }
            else if (BlendShape == BlendShapes.GoodAndEvil)
            {
                Renderer.SetBlendShapeWeight(1, _godLogic.Blend100);
            }
        }
        else
        {
            if (BlendShape == BlendShapes.EvilOnly || BlendShape == BlendShapes.GoodAndEvil)
            {
                Renderer.SetBlendShapeWeight(0, _godLogic.Blend100);
            }
        }
    }
}
