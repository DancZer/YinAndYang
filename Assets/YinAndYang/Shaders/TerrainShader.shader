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
            if(IN.worldPos.y <= heightColoursStartHeight[0])
            {
                 OUT.Albedo = heightColours[0];
            }
            else if(IN.worldPos.y >= heightColoursStartHeight[heightColourCount-1])
            {
                OUT.Albedo = heightColours[heightColourCount-1];
            }
            else
            {
                int endIdx = 0;

			    for (int i = 1; i < heightColourCount; i++) 
                {
                    if(IN.worldPos.y < heightColoursStartHeight[i])
                    {
                        endIdx = i;
                    }else{
                        break;
                    }
			    }

                int startIdx = (endIdx > 0) ? (endIdx - 1) : endIdx;
            
			    float percentage = inverseLerp(heightColoursStartHeight[startIdx]-minHeight, heightColoursStartHeight[endIdx]-minHeight, IN.worldPos.y-minHeight);

                OUT.Albedo = heightColours[startIdx] * (1-percentage) * heightColours[endIdx] * percentage;
            }
		}
        ENDCG
    }
    FallBack "Diffuse"
}
