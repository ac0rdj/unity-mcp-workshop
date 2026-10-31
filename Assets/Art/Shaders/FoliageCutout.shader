Shader "Art/FoliageCutout"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB) Alpha (A)", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        _Glossiness ("Smoothness", Range(0,1)) = 0.1
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard addshadow fullforwardshadows
        #pragma target 3.0
        #pragma multi_compile_instancing
        sampler2D _MainTex; sampler2D _BumpMap;
        fixed4 _Color; half _Cutoff; half _Glossiness;
        struct Input { float2 uv_MainTex; fixed facing : VFACE; };
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            clip(c.a - _Cutoff);
            o.Albedo = c.rgb;
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            o.Normal.z *= (IN.facing > 0) ? 1 : -1;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    Fallback "Transparent/Cutout/VertexLit"
}