// Theme backgrounds in one shader: 0 paper (fibres + grain), 1 chalkboard (slate with smudges), 2 blueprint (grid on blue), 3 CRT (dark glass, scanlines, vignette, curved corners), 4 holo void (dark gradient and floor glow). Colour = _Base; _Ink = lines / grid colour.
// Theme lab (Docs/SHADER_LAB.md). UI shader: works on a uGUI Graphic, masks included. Output is premultiplied alpha.
Shader "CasualGame/Lab/ThemeGround"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        _Mode ("Mode", Range(0, 4)) = 0
        _Base ("Base colour", Color) = (0.96, 0.94, 0.9, 1)
        _Ink ("Line / grid colour", Color) = (0.85, 0.82, 0.77, 1)
        _Cell ("Grid cell (px)", Float) = 96
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex ArrowVert
            #pragma fragment frag
            #include "ArrowLabCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Mode, _Cell;
                half4 _Base, _Ink;
            CBUFFER_END

            half4 frag(ArrowVaryings i) : SV_Target
            {
                float2 px = ScreenPx(i.screen);
                float2 uv = i.screen.xy / max(i.screen.w, 1e-5);
                half3 c = _Base.rgb;
                if (_Mode < 0.5)        // paper: long fibres + fine grain
                {
                    float fibre = ValueNoise(float2(px.x * 0.02, px.y * 0.6)) - 0.5;
                    float grain = ValueNoise(px * 0.7) - 0.5;
                    c *= 1.0 + fibre * 0.035 + grain * 0.03;
                }
                else if (_Mode < 1.5)   // chalkboard: slate, eraser smudges, chalk dust
                {
                    float smudge = Fbm(px * 0.004 + float2(0, px.x * 0.002));
                    float dust = step(0.985, ValueNoise(px * 0.9)) * 0.25;
                    c = lerp(c, _Ink.rgb, smudge * 0.22) + dust;
                }
                else if (_Mode < 2.5)   // blueprint: fine grid + every 4th line stronger
                {
                    float2 g = abs(frac(px / _Cell * 4.0 + 0.5) - 0.5) * _Cell / 4.0;
                    float fine = 1.0 - saturate(min(g.x, g.y) - 0.5);
                    float2 G = abs(frac(px / _Cell + 0.5) - 0.5) * _Cell;
                    float major = 1.0 - saturate(min(G.x, G.y) - 1.0);
                    c = lerp(c, _Ink.rgb, fine * 0.18 + major * 0.35);
                    c *= 1.0 + (ValueNoise(px * 0.5) - 0.5) * 0.05;
                }
                else if (_Mode < 3.5)   // CRT: scanlines, vignette, rounded dark corners
                {
                    float scan = 0.85 + 0.15 * step(0.5, frac(px.y / 4.0));
                    float2 q = uv * 2.0 - 1.0;
                    float vig = saturate(1.0 - dot(q * q, q * q) * 0.35);
                    float corner = AAInside(length(max(abs(q) - 0.9, 0.0)) - 0.08);
                    float2 g = abs(frac(px / _Cell + 0.5) - 0.5) * _Cell;
                    float grid = 1.0 - saturate(min(g.x, g.y) - 0.5);
                    c = (c + _Ink.rgb * grid * 0.05) * lerp(1.0, scan, 0.5) * vig * corner;
                }
                else                    // holo void: dark gradient with a floor glow
                {
                    float floorGlow = smoothstep(0.45, 0.0, uv.y) * 0.25;
                    c = c * (0.6 + 0.4 * uv.y) + _Ink.rgb * floorGlow;
                }
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
