// A drifting sea of clouds below the islands. Fake self-shadowing by sampling the noise again toward the sun.
Shader "SkyIslands/CloudSea"
{
    Properties
    {
        _NoiseTex ("Noise", 2D) = "gray" {}
        _Lit ("Lit Color (HDR)", Color) = (1.2, 1.2, 1.18, 1)
        _Shade ("Shade Color", Color) = (0.55, 0.7, 0.8, 1)
        _Cover ("Cover", Range(0, 1)) = 0.42
        _Sharp ("Sharpness", Float) = 3.5
        _Opacity ("Opacity", Range(0, 1)) = 1
        _Scale ("Scale", Float) = 0.0025
        _Speed ("Drift Speed", Float) = 0.004
        _SunDir ("Sun Dir (xz)", Vector) = (0.5, 0, -0.85, 0)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-20" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            sampler2D _NoiseTex;
            float4 _Lit, _Shade, _SunDir;
            float _Cover, _Sharp, _Opacity, _Scale, _Speed;

            struct v2f { float4 pos : SV_POSITION; float3 wpos : TEXCOORD0; };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float Density (float2 uv)
            {
                float2 drift = _Time.y * _Speed * float2(1, 0.4);
                float a = tex2D(_NoiseTex, uv + drift).r;
                float b = tex2D(_NoiseTex, uv * 2.7 + float2(0.31, 0.17) + drift * 1.7).r;
                float c = tex2D(_NoiseTex, uv * 7.3 + float2(0.71, 0.53) + drift * 2.9).r;
                return a * 0.6 + b * 0.28 + c * 0.12;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.wpos.xz * _Scale;
                float d = Density(uv);
                float dens = saturate((d - _Cover) * _Sharp);
                float toward = Density(uv + normalize(_SunDir.xz) * 0.012);
                float light = saturate(0.6 + (d - toward) * 6.0);
                fixed4 col;
                col.rgb = lerp(_Shade.rgb, _Lit.rgb, saturate(light * 0.8 + dens * 0.35 - 0.15));

                float dist = length(i.wpos - _WorldSpaceCameraPos);
                float fog = saturate(dist * unity_FogParams.z + unity_FogParams.w); // linear fog: 1 = clear
                col.rgb = lerp(unity_FogColor.rgb, col.rgb, fog);
                col.a = lerp(1, dens, fog) * _Opacity;
                return col;
            }
            ENDCG
        }
    }
}
