Shader "Custom/TerrainShader"
{
    Properties
    {
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

        const static int maxColourCount = 10;

        float minHeight;
        float maxHeight;

        int heightColourCount;
		float3 heightColours[maxColourCount];
		float heightColoursStartHeight[maxColourCount];

		struct Input 
        {
			float3 worldPos;
		};

		float inverseLerp(float a, float b, float value) 
        {
			return saturate((value-a)/(b-a));
		}

		void surf (Input IN, inout SurfaceOutputStandard OUT) 
        {
            float heightPercentage = inverseLerp(minHeight, maxHeight, IN.worldPos.y);
            
            OUT.Albedo = heightColours[0];

			for (int i = 0; i < heightColourCount; i++) 
            {
                float colorPercentage = inverseLerp(minHeight, maxHeight, heightColoursStartHeight[i]);

                float colorStrenght = inverseLerp(-0.007, 0.007, heightPercentage - colorPercentage);

                OUT.Albedo = OUT.Albedo * (1-colorStrenght) + heightColours[i] * colorStrenght;
			}

		}
        ENDCG
    }
    FallBack "Diffuse"
}
