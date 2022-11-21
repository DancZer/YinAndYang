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

        float4 _BaseColor;
        half _Glossiness;
        half _Metallic;
        sampler2D _BiomeTileTex;
            
        int _BiomeCount;
        float _BiomeLayerCounts[maxLayerCount];
        float _BiomeMinHeights[maxLayerCount];
        float _BiomeMaxHeights[maxLayerCount];
        
        UNITY_DECLARE_TEX2DARRAY(_BiomeMapWeightTex);
        
        struct Input
        {
            float3 worldPos;
            float2 uv_BiomeTileTex;
        };

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard OUT)
        {
            
            float tileUV = 1.0 / maxLayerCount;
            OUT.Albedo.rgb = float3(0,0,0);
            
            for(int biomeId=0; biomeId < _BiomeCount; biomeId++)
            {
                float heightStepSize = (_BiomeMaxHeights[biomeId] -_BiomeMinHeights[biomeId]) / _BiomeLayerCounts[biomeId];
                float heightTileId = floor((IN.worldPos.y - _BiomeMinHeights[biomeId]) / heightStepSize);

                float2 uv_tile = IN.uv_BiomeTileTex * tileUV;
                uv_tile.x += heightTileId * tileUV;
                uv_tile.y += biomeId * tileUV;

                float3 uv_biomeMap;
                uv_biomeMap.xy =  IN.uv_BiomeTileTex;
                uv_biomeMap.z = biomeId;

                OUT.Albedo.rgb += tex2D(_BiomeTileTex, uv_tile) * UNITY_SAMPLE_TEX2DARRAY(_BiomeMapWeightTex, uv_biomeMap).r;
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
