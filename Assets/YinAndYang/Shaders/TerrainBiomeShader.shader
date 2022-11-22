Shader "Custom/TerrainBiomeShader"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _BiomeTileTex ("Biome Tile Tex", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        const static int maxLayerCount = 16;
		const static float epsilon = 1E-4;

        float4 _BaseColor;
        half _Glossiness;
        half _Metallic;
        sampler2D _BiomeTileTex;
            
        int _BiomeCount;
        float _BiomeTexIds[maxLayerCount];
        float3 _BiomeColors[maxLayerCount];
        float _BiomeLayerCounts[maxLayerCount];
        float _BiomeMinHeights[maxLayerCount];
        float _BiomeMaxHeights[maxLayerCount];

        float _BiomesBaseBlends[maxLayerCount * maxLayerCount];
        float _BiomesBaseStartHeights[maxLayerCount * maxLayerCount];
        float3 _BiomesBaseColors[maxLayerCount * maxLayerCount];
        float _BiomesBaseColorStrengths[maxLayerCount * maxLayerCount];
        
        UNITY_DECLARE_TEX2DARRAY(_BiomeMapWeightTex);
        
        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
            float2 uv_BiomeTileTex;
        };

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        float inverseLerp(float a, float b, float value) {
			return saturate((value-a)/(b-a));
		}

        float3 triplanar(float3 tex, float3 blendAxes) {

			float3 xProjection = tex * blendAxes.x;
			float3 yProjection = tex * blendAxes.y;
			float3 zProjection = tex * blendAxes.z;

			return xProjection + yProjection + zProjection;
		}

        void surf (Input IN, inout SurfaceOutputStandard OUT)
        {
            float tileUV = 1.0 / maxLayerCount;
            OUT.Albedo.rgb = float3(0,0,0);

            float3 blendAxes = abs(IN.worldNormal);
			blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;
            
            for(int biomeIdx=0; biomeIdx < _BiomeCount; biomeIdx++)
            {
                float minMaxHeightDistance = _BiomeMaxHeights[biomeIdx] - _BiomeMinHeights[biomeIdx];

                float heightPercent = inverseLerp(_BiomeMinHeights[biomeIdx],_BiomeMaxHeights[biomeIdx], IN.worldPos.y);
                
                float3 uv_biomeMap;
                uv_biomeMap.xy =  IN.uv_BiomeTileTex;
                uv_biomeMap.z = biomeIdx;

                float3 biomeColor = _BiomeColors[biomeIdx];
                float3 biomeBlendTex = triplanar(UNITY_SAMPLE_TEX2DARRAY(_BiomeMapWeightTex, uv_biomeMap), blendAxes);
                float biomeBlendStrength = saturate(biomeBlendTex.r);

                int layerCount = (int)_BiomeLayerCounts[biomeIdx];

                float3 biomeAlbedo;

                for(int layerIdx=0; layerIdx < layerCount; layerIdx++)
                {
                    float2 uv_tile = IN.uv_BiomeTileTex * tileUV; //scale down to tile size
                    uv_tile.x += layerIdx * tileUV;
                    uv_tile.y += biomeIdx * tileUV;

                    int layerIdxx = biomeIdx * maxLayerCount + layerIdx;
                    float baseStartHeightPercent = (_BiomesBaseStartHeights[layerIdxx] - _BiomeMinHeights[biomeIdx]) / minMaxHeightDistance;

                    float layerDrawStrength = inverseLerp(-_BiomesBaseBlends[layerIdxx]/2 - epsilon, _BiomesBaseBlends[layerIdxx]/2, heightPercent - baseStartHeightPercent);
                    float3 baseColour = _BiomesBaseColors[layerIdxx] * _BiomesBaseColorStrengths[layerIdxx];
                    float3 textureColour = triplanar(tex2D(_BiomeTileTex, uv_tile), blendAxes);

                    biomeAlbedo = biomeAlbedo * (1-layerDrawStrength) + (baseColour+textureColour) * layerDrawStrength;
                }

                OUT.Albedo = OUT.Albedo * (1-biomeBlendStrength) + biomeAlbedo * biomeBlendStrength;
            }
            
            //OUT.Albedo = _BaseColor;
            OUT.Metallic = _Metallic;
            OUT.Smoothness = _Glossiness;
            OUT.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
