// Scrolling falling-water ribbon. uv.x = across (0..1), uv.y = metres along the fall. Vertex alpha fades the ends.
Shader "SkyIslands/Waterfall"
{
    Properties
    {
        _NoiseTex ("Noise", 2D) = "gray" {}
        _Color ("Water", Color) = (0.6, 0.76, 0.85, 1)
        _FoamColor ("Foam (HDR)", Color) = (1.4, 1.45, 1.5, 1)
        _Speed ("Speed", Float) = 1
        _Opacity ("Opacity", Range(0, 1)) = 0.92
        _ScaleU ("Across Tiling", Float) = 2
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            sampler2D _NoiseTex;
            float4 _Color, _FoamColor;
            float _Speed, _Opacity, _ScaleU;

            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 col : COLOR; UNITY_FOG_COORDS(1) };

            v2f vert (appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord.xy;
                o.col = v.color;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y * _Speed;
                float a = tex2D(_NoiseTex, float2(i.uv.x * _ScaleU, i.uv.y * 0.035 - t * 0.30)).r;
                float b = tex2D(_NoiseTex, float2(i.uv.x * _ScaleU * 2.3 + 0.37, i.uv.y * 0.09 - t * 0.75)).r;
                float c = tex2D(_NoiseTex, float2(i.uv.x * _ScaleU * 5.1 + 0.71, i.uv.y * 0.22 - t * 1.6)).r;
                float s = a * 0.5 + b * 0.3 + c * 0.2;
                float edge = smoothstep(0.0, 0.2, i.uv.x) * smoothstep(1.0, 0.8, i.uv.x);
                float foam = saturate((s - 0.42) * 3.0);
                fixed4 col = lerp(_Color, _FoamColor, foam);
                float alpha = saturate(0.4 + s * 0.9) * saturate((edge - (1 - s) * 0.45) * 3.0);
                col.a = alpha * _Opacity * i.col.a;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
