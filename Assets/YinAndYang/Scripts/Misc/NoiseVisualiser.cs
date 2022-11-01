using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class NoiseVisualiser : MonoBehaviour
{
    public int Seed = 12345;

    public NoisePreset[] NoisePresets;
    
    [Range(10, 1000)] public int TextureSize = 100;
    [Range(0.1f, 5)] public float RefreshTime = 1;

    private FastNoise _noise = new FastNoise();
    private float _lastUpdate;

    void Update()
    {
        if(_lastUpdate + RefreshTime < Time.realtimeSinceStartup)
        {
            _lastUpdate = Time.realtimeSinceStartup;
            DrawTexture();
            Debug.Log("NoiseVisualiser.Update");
        }
    }

    private void DrawTexture()
    {
        _noise.SetSeed(Seed);

        var tex = new Texture2D(TextureSize, TextureSize);

        float minVal = 0;
        float maxVal = 0;

        float[] vals = new float[TextureSize * TextureSize];

        for (int x = 0; x < TextureSize; x++)
        {
            for (int y = 0; y < TextureSize; y++)
            {
                float val = GetNoiseVal(x - transform.position.x, y - transform.position.z);

                if (minVal > val)
                {
                    minVal = val;
                }

                if (maxVal < val)
                {
                    maxVal = val;
                }

                vals[x * TextureSize + y] = val;
            }
        }

        var middle = (minVal + maxVal) / 2f;

        minVal -= middle;
        maxVal -= middle;

        for (int x = 0; x < TextureSize; x++)
        {
            for (int y = 0; y < TextureSize; y++)
            {
                var val = vals[x * TextureSize + y];

                val -= middle;

                var lowVal = val < 0 ? (val / minVal) : 0;
                var highVal = val > 0 ? (val / maxVal) : 0;

                var c = new Color(Mathf.Abs(highVal), 0, Mathf.Abs(lowVal));

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();

        var renderer = GetComponentInChildren<MeshRenderer>();

        if (Application.isEditor)
        {
            renderer.sharedMaterial.SetTexture("_MainTex", tex);
            renderer.sharedMaterial.SetTexture("_ParallaxMap", tex);
        }
        else
        {
            renderer.material.SetTexture("_MainTex", tex);
            renderer.material.SetTexture("_ParallaxMap", tex);
        }
    }

    private float GetNoiseVal(float x, float y)
    {
        var result = 0f;

        foreach(var preset in NoisePresets)
        {
            result = preset.GetNoiseValue(_noise, x, y, result);
        }

        return result;
    }
}
