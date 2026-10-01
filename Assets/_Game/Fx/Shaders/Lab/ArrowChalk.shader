// Chalk on a board: grainy coverage that thins toward the edges, eroded borders, a light dust halo, drawn on with _Reveal.
// Theme lab (Docs/SHADER_LAB.md). UI shader: works on a uGUI Graphic, masks included. Output is premultiplied alpha.
Shader "CasualGame/Lab/ArrowChalk"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        _Reveal ("Drawn (0 tail .. 1 head)", Range(0, 1)) = 1
        _Grain ("Grain size (px)", Range(0.5, 6)) = 1.6
        _Coverage ("Coverage", Range(0, 1)) = 0.78
        _Dust ("Dust halo", Range(0, 0.6)) = 0.16
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
                float _Reveal, _Grain, _Coverage, _Dust;
            CBUFFER_END

            half4 frag(ArrowVaryings i) : SV_Target
            {
                float across = abs(i.uv0.y), t = i.uv1.x;
                float reveal = saturate((_Reveal + 0.02 - t) * 50.0);   // drawn up to the brush front (the head at t = 1 included)
                float2 px = ScreenPx(i.screen) / _Grain;
                float grain = ValueNoise(px) * 0.6 + ValueNoise(px * 2.7 + 13.0) * 0.4;
                // the stick deposits more in the middle of the stroke, less at the borders
                float pressure = 1.0 - smoothstep(0.2, 1.15, across);
                float stroke = step(1.0 - _Coverage * pressure, grain);
                // eroded border: noise pushes the edge in and out
                float border = AAInside(across - (1.0 + (ValueNoise(px * 0.35) - 0.5) * 0.5));
                float dust = saturate(1.0 - (across - 1.0) / 0.9) * (across > 1.0) * _Dust * grain;
                float a = saturate(stroke * border + dust) * reveal * i.color.a;
                half3 c = lerp(i.color.rgb, half3(1, 1, 1), 0.15);
                return half4(c * a, a);
            }
            ENDHLSL
        }
    }
}
