Shader "Custom/MaterialBlend"
{
 Properties
    {
        _FrontTex("Back(Main) Texture (RGB)", 2D) = "white" {}
        _FrontTex2("Front Texture (RGB) Transparent (A)", 2D) = "white" {}
        _Blend("Blend", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 200
        ZWrite Off

        CGPROGRAM

        #pragma surface surf Lambert

        sampler2D _FrontTex;
        sampler2D _FrontTex2;
        float _Blend;

        struct Input
        {
            float2 uv_FrontTex;
            float2 uv_FrontTex2;
        };

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 frontTex = tex2D(_FrontTex, IN.uv_FrontTex);
            fixed4 frontTex2 = tex2D(_FrontTex2, IN.uv_FrontTex2);

            fixed4 mainOutput = frontTex.rgba * (1.0 - (frontTex2.a * _Blend));
            fixed4 combineOutput = frontTex2.rgba * (frontTex2.a * _Blend);

            o.Albedo = mainOutput.rgb + combineOutput.rgb;
            o.Alpha = mainOutput.a + combineOutput.a;
        }

        ENDCG
    }

    FallBack "Diffuse"
}
