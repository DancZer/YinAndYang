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

    private PlayerStatHandler _playerStat;

    public override void OnStartClient()
    {
        base.OnStartClient();

        _playerStat = StaticObjectAccessor.GetPlayerStatHandler();
    }

    private void Update()
    {
        if (_playerStat == null) return;

        UpdateTexture();
        UpdateShape();
    }

    private void UpdateTexture()
    {
        var shaderMaterial = Renderer.sharedMaterial;

        if (_playerStat.IsGood)
        {
            shaderMaterial.SetFloat("_Blend", _playerStat.Blend);
            shaderMaterial.SetTexture("_FrontTex", Neutral);
            shaderMaterial.SetTexture("_FrontTex2", Good);
        }
        else
        {
            shaderMaterial.SetFloat("_Blend", _playerStat.Blend);
            shaderMaterial.SetTexture("_FrontTex", Neutral);
            shaderMaterial.SetTexture("_FrontTex2", Evil);
        }
    }

    private void UpdateShape()
    {
        if (BlendShape == BlendShapes.None) return;

        if (_playerStat.IsGood)
        {
            if(BlendShape == BlendShapes.GoodOnly)
            {
                Renderer.SetBlendShapeWeight(0, _playerStat.Blend100);
            }
            else if (BlendShape == BlendShapes.GoodAndEvil)
            {
                Renderer.SetBlendShapeWeight(1, _playerStat.Blend100);
            }
        }
        else
        {
            if (BlendShape == BlendShapes.EvilOnly || BlendShape == BlendShapes.GoodAndEvil)
            {
                Renderer.SetBlendShapeWeight(0, _playerStat.Blend100);
            }
        }
    }
}
