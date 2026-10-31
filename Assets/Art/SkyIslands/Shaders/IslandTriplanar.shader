// Triplanar rock on the sides, grass on anything facing up. No UVs needed, so generated meshes just work.
// Vertex color: R = baked occlusion (darker toward island tips), G = where grass is allowed.
Shader "SkyIslands/IslandTriplanar"
{
    Properties
    {
        _GrassTex ("Grass", 2D) = "white" {}
        _GrassNormal ("Grass Normal", 2D) = "bump" {}
        _GrassTint ("Grass Tint", Color) = (0.8, 1, 0.6, 1)
        _GrassScale ("Grass Scale", Float) = 0.3
        _MossTex ("Moss / Breakup", 2D) = "white" {}
        _MossTint ("Moss Tint", Color) = (1, 1, 1, 1)
        _RockTex ("Rock", 2D) = "white" {}
        _RockNormal ("Rock Normal", 2D) = "bump" {}
        _RockTint ("Rock Tint", Color) = (1, 1, 1, 1)
        _RockScale ("Rock Scale", Float) = 0.12
        _GrassLevel ("Grass Level (normal.y)", Range(0, 1)) = 0.7
        _GrassBlend ("Grass Blend", Range(0.01, 0.5)) = 0.12
        _NormalStrength ("Normal Strength", Range(0, 2)) = 1
        _AOStrength ("Vertex AO Strength", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.5

        sampler2D _GrassTex, _GrassNormal, _MossTex, _RockTex, _RockNormal;
        fixed4 _GrassTint, _MossTint, _RockTint;
        float _GrassScale, _RockScale, _GrassLevel, _GrassBlend, _NormalStrength, _AOStrength;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
            float4 color : COLOR;
            INTERNAL_DATA
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 n = normalize(WorldNormalVector(IN, float3(0, 0, 1)));
            float3 p = IN.worldPos;
            float3 w = pow(abs(n), 4);
            w /= (w.x + w.y + w.z);

            // Rock: triplanar albedo + whiteout-blended normals
            float2 ux = p.zy * _RockScale, uy = p.xz * _RockScale, uz = p.xy * _RockScale;
            fixed3 rock = tex2D(_RockTex, ux).rgb * w.x + tex2D(_RockTex, uy).rgb * w.y + tex2D(_RockTex, uz).rgb * w.z;
            float3 nx = UnpackNormal(tex2D(_RockNormal, ux));
            float3 ny = UnpackNormal(tex2D(_RockNormal, uy));
            float3 nz = UnpackNormal(tex2D(_RockNormal, uz));
            nx.xy *= _NormalStrength; ny.xy *= _NormalStrength; nz.xy *= _NormalStrength;
            nx = float3(nx.xy + n.zy, abs(nx.z) * n.x);
            ny = float3(ny.xy + n.xz, abs(ny.z) * n.y);
            nz = float3(nz.xy + n.xy, abs(nz.z) * n.z);
            float3 rockN = normalize(nx.zyx * w.x + ny.xzy * w.y + nz.xyz * w.z);

            // Grass: top-down projection
            float2 ug = p.xz * _GrassScale;
            fixed3 grass = tex2D(_GrassTex, ug).rgb * _GrassTint.rgb;
            float3 gn = UnpackNormal(tex2D(_GrassNormal, ug));
            gn.xy *= _NormalStrength;
            gn = float3(gn.xy + n.xz, abs(gn.z) * n.y);
            float3 grassN = normalize(gn.xzy);

            fixed3 moss = tex2D(_MossTex, p.xz * _GrassScale * 0.37).rgb * _MossTint.rgb;
            float breakup = tex2D(_MossTex, p.xz * 0.031).g;
            float g = saturate((n.y - _GrassLevel + (breakup - 0.5) * 0.35) / _GrassBlend) * IN.color.g;
            float mossBand = saturate((n.y - (_GrassLevel - 0.3) + (breakup - 0.5) * 0.35) / 0.2) * IN.color.g;

            fixed3 col = lerp(rock * _RockTint.rgb, moss, mossBand * (1 - g) * 0.85);
            col = lerp(col, grass, g);
            float3 wn = normalize(lerp(rockN, grassN, g));
            float ao = lerp(1, IN.color.r, _AOStrength);

            float3 tx = WorldNormalVector(IN, float3(1, 0, 0));
            float3 ty = WorldNormalVector(IN, float3(0, 1, 0));
            o.Normal = normalize(float3(dot(tx, wn), dot(ty, wn), dot(n, wn)));
            o.Albedo = col * ao;
            o.Occlusion = ao;
            o.Smoothness = 0.06 + 0.08 * g;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
