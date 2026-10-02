// Sumi ink: a brush line with a wobbly bleeding edge, ink pooling darker at the edges, dry-brush streaks toward the tail and a wet bead at the front while the line is drawn on (_Reveal 0..1).
// Theme lab (Docs/SHADER_LAB.md). UI shader: works on a uGUI Graphic, masks included. Output is premultiplied alpha.
Shader "CasualGame/Lab/ArrowInk"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        _Reveal ("Drawn (0 tail .. 1 head)", Range(0, 1)) = 1
        _Bleed ("Edge wobble / bleed", Range(0, 0.6)) = 0.28
        _Pool ("Edge pooling (darker)", Range(0, 1)) = 0.55
        _Dry ("Dry brush at the tail", Range(0, 1)) = 0.6
        _Wash ("Wash halo opacity", Range(0, 0.6)) = 0.18
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
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "ArrowLabCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Reveal, _Bleed, _Pool, _Dry, _Wash;
            CBUFFER_END

            half4 fragBody(ArrowVaryings i)
            {
                float along = i.uv0.x, across = abs(i.uv0.y), t = i.uv1.x;
                // the line is drawn from tail to head: everything past the brush front is not there yet
                float front = _Reveal;
                float reveal = saturate((front + 0.02 - t) * 50.0);   // drawn up to the brush front (the head at t = 1 included)
                // ragged, bleeding edge: the edge radius wobbles with low-frequency noise along the line
                float wobble = (Fbm(float2(along * 3.1, i.uv0.y * 0.7 + 7.0)) - 0.5) * 2.0 * _Bleed;
                float edge = 1.0 + wobble;
                float core = AAInside(across - edge);
                // dry brush: streaks along the line drop out near the tail (bristles run dry)
                float bristle = ValueNoise(float2(along * 1.4, i.uv0.y * 9.0));
                float dryness = saturate((0.35 - t) / 0.35) * _Dry;
                core *= step(dryness * 0.9, bristle);
                // ink pools at the edges: darker rim, lighter middle
                float pool = smoothstep(0.35, 1.0, across / edge) * _Pool;
                half3 ink = i.color.rgb * (1.0 - 0.55 * pool);
                ink *= lerp(0.92, 1.05, Fbm(i.local * 0.05)); // paper absorbs unevenly
                // a faint wash bleeding into the paper around the line
                float wash = saturate(1.0 - (across - edge) / 0.6) * (across > edge) * _Wash;
                // wet bead at the brush front while drawing
                float bead = (1.0 - saturate(abs(t - front) * 25.0)) * step(front, 0.999) * 0.35;
                float a = saturate(core + wash + bead * core) * reveal * i.color.a;
                return half4(ink * a, a);
            }

            // inside a UI mask (scroll list, card), nothing draws outside it
            half4 frag(ArrowVaryings i) : SV_Target { return fragBody(i) * UIClip(i.local); }
            ENDHLSL
        }
    }
}
