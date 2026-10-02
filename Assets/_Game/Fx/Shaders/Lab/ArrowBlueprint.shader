// Blueprint drafting: the arrow is drawn as its outline (two thin ink lines), a dashed centre line and measure ticks every cell; drawn on with _Reveal.
// Theme lab (Docs/SHADER_LAB.md). UI shader: works on a uGUI Graphic, masks included. Output is premultiplied alpha.
Shader "CasualGame/Lab/ArrowBlueprint"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        _Reveal ("Drawn (0 tail .. 1 head)", Range(0, 1)) = 1
        _Line ("Outline width (fraction of half width)", Range(0.05, 0.5)) = 0.2
        _Fill ("Fill tint", Range(0, 0.5)) = 0.12
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
                float _Reveal, _Line, _Fill;
            CBUFFER_END

            half4 frag(ArrowVaryings i) : SV_Target
            {
                float along = i.uv0.x, y = i.uv0.y, across = abs(y), t = i.uv1.x;
                float reveal = saturate((_Reveal + 0.02 - t) * 50.0);   // drawn up to the brush front (the head at t = 1 included)
                float inside = AAInside(across - 1.0);
                float outline = inside * (1.0 - AAInside(across - (1.0 - _Line)));
                float dash = step(0.5, frac(along * 2.0)) * AAInside(across - 0.09) * (1.0 - i.uv1.y);
                float tick = step(frac(along + 0.04), 0.08) * inside * (1.0 - i.uv1.y);
                // the chevron is drawn solid: its two arms overlap, and two outlines would cross into an X
                float body = lerp(outline + dash * 0.8 + tick + inside * _Fill, inside * 0.9, i.uv1.y);
                float a = saturate(body) * reveal * i.color.a;
                return half4(i.color.rgb * a, a);
            }
            ENDHLSL
        }
    }
}
