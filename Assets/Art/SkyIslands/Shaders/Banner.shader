// Goal flag that waves in the wind. Mesh pivot sits at the pole edge (x = 0), so the wave grows toward the free end.
Shader "SkyIslands/Banner"
{
    Properties
    {
        _Color ("Color", Color) = (0.85, 0.2, 0.12, 1)
        _Amp ("Wave Amplitude", Float) = 0.16
        _Speed ("Wave Speed", Float) = 5
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard vertex:vert addshadow fullforwardshadows
        #pragma target 3.0
        fixed4 _Color;
        float _Amp, _Speed;
        struct Input { float3 worldPos; fixed facing : VFACE; };

        void vert (inout appdata_full v)
        {
            float x = v.vertex.x;
            float ph = x * 2.6 - _Time.y * _Speed + v.vertex.y * 0.9;
            v.vertex.z += sin(ph) * _Amp * x;
            // slope of the wave -> bent normal so the folds catch the light
            float dzdx = cos(ph) * 2.6 * _Amp * x + sin(ph) * _Amp;
            float dzdy = cos(ph) * 0.9 * _Amp * x;
            v.normal = normalize(float3(-dzdx, -dzdy, 1));
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = _Color.rgb;
            o.Normal = float3(0, 0, IN.facing > 0 ? 1 : -1);
            o.Smoothness = 0.25;
        }
        ENDCG
    }
}
