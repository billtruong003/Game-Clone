// Green-screen terminal: the line is built from phosphor blocks (like text cells), scanlines, a soft phosphor glow and a blinking block cursor at the head. Needs GlowPad > 0.
// Theme lab (Docs/SHADER_LAB.md). UI shader: works on a uGUI Graphic, masks included. Output is premultiplied alpha.
Shader "CasualGame/Lab/ArrowTerminal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        _Reveal ("Drawn (0 tail .. 1 head)", Range(0, 1)) = 1
        _Blocks ("Blocks per cell", Range(1, 8)) = 3
        _Scan ("Scanline spacing (px)", Range(2, 12)) = 4
        _Glow ("Phosphor glow", Range(0, 1.5)) = 0.7
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
                float _Reveal, _Blocks, _Scan, _Glow;
            CBUFFER_END

            half4 frag(ArrowVaryings i) : SV_Target
            {
                float along = i.uv0.x, across = abs(i.uv0.y), t = i.uv1.x;
                float reveal = step(t, _Reveal + 1e-3);                       // terminal: types on in blocks
                float2 px = ScreenPx(i.screen);
                float cellPos = frac(along * _Blocks);
                float gap = step(0.12, cellPos) * step(cellPos, 0.92);
                float lit = AAInside(across - 1.0) * lerp(1.0, gap, 1.0 - i.uv1.y);
                float flickerRow = 0.85 + 0.15 * ValueNoise(float2(floor(along * _Blocks), _Time.y * 6.0));
                float scan = 0.7 + 0.3 * step(0.5, frac(px.y / _Scan));
                float glow = (1.0 - AAInside(across - 1.0)) * exp(-(across - 1.0) * 3.0) * _Glow;
                float cursor = i.uv1.y * step(0.5, frac(_Time.y * 1.6));
                float e = (lit * flickerRow * scan + glow + cursor * 0.4) * reveal * i.color.a;
                return half4(i.color.rgb * e, saturate(e) * 0.8);
            }
            ENDHLSL
        }
    }
}
