// Character faces (the expression flipbook, slice in uv1.x like FaceArray) styled like their theme: 0 ink bleed, 1 chalk (bold, solid strokes so the expression reads on a pastel cap), 2 neon glow, 3 hologram, 4 pixel, 5 terminal. The ink takes _Ink; tears / sweat keep their colour.
// Theme lab (Docs/SHADER_LAB.md). UI shader: works on a uGUI Graphic, masks included. Output is premultiplied alpha.
Shader "CasualGame/Lab/ThemeFace"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        _Faces ("Faces", 2DArray) = "" {}
        _Mode ("Style", Range(0, 5)) = 0
        _Ink ("Ink colour", Color) = (0.118, 0.133, 0.251, 1)
        _GlowR ("Glow radius (uv)", Range(0, 0.3)) = 0.06
        _Pixels ("Pixel grid", Range(4, 64)) = 16
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
            #pragma require 2darray
            #include "ArrowLabCommon.hlsl"

            TEXTURE2D_ARRAY(_Faces); SAMPLER(sampler_Faces);
            CBUFFER_START(UnityPerMaterial)
                float _Mode, _GlowR, _Pixels;
                half4 _Ink;
            CBUFFER_END
            static float gSlice;
            float FaceMask(float2 uv) { return SAMPLE_TEXTURE2D_ARRAY(_Faces, sampler_Faces, uv, gSlice).a; }

            // style a coverage mask m (0..1) sampled by `sampleMask(uv)`; returns premultiplied colour
            #define STYLE_BODY(SAMPLE) \
                float2 uv = i.uv0; \
                if (_Mode > 3.5) { float n = _Pixels; uv = (floor(uv * n) + 0.5) / n; } \
                float m = SAMPLE(uv); \
                float2 px = ScreenPx(i.screen); \
                half3 col = tint; float a = m; \
                if (_Mode < 0.5) { \
                    float wob = (Fbm(i.uv0 * 9.0 + 3.0) - 0.5) * 0.35; \
                    a = smoothstep(0.42 + wob, 0.58 + wob, m); \
                    col *= 0.85 + 0.15 * Fbm(px * 0.05); } \
                else if (_Mode < 1.5) { \
                    float g = ValueNoise(px / 1.6) * 0.6 + ValueNoise(px / 0.6) * 0.4; \
                    float bold = m; \
                    [unroll] for (int k = 0; k < 4; k++) { float ang = k * 1.5708; bold = max(bold, SAMPLE(i.uv0 + float2(cos(ang), sin(ang)) * 0.012)); } \
                    a = smoothstep(0.25, 0.6, bold) * lerp(0.82, 1.0, step(0.3, g)); } \
                else if (_Mode < 2.5) { \
                    float halo = 0; \
                    [unroll] for (int k = 0; k < 8; k++) { float ang = k * 0.7854; halo += SAMPLE(i.uv0 + float2(cos(ang), sin(ang)) * _GlowR); } \
                    halo = halo / 8.0; \
                    float core = smoothstep(0.5, 1.0, m); \
                    col = lerp(col, half3(1, 1, 1), core * 0.7) * m + col * halo * (1.0 - m) * 1.4; \
                    return half4(col * i.color.a, saturate(m + halo * 0.5) * 0.8 * i.color.a); } \
                else if (_Mode < 3.5) { \
                    float scan = 0.6 + 0.4 * step(0.5, frac(px.y / 5.0)); \
                    float band = smoothstep(0.6, 1.0, sin((px.y + _Time.y * 40.0) / 46.0 * 6.2832) * 0.5 + 0.5); \
                    float e = m * (0.55 + 0.45 * band) * scan; \
                    return half4(lerp(col, half3(1, 1, 1), 0.3 * band) * e * i.color.a, e * 0.6 * i.color.a); } \
                else if (_Mode > 4.5) { \
                    float scan = 0.7 + 0.3 * step(0.5, frac(px.y / 4.0)); \
                    return half4(col * m * scan * i.color.a, m * 0.85 * i.color.a); } \
                a *= i.color.a; \
                return half4(col * a, a);

            half4 frag(ArrowVaryings i) : SV_Target
            {
                gSlice = i.uv1.x;
                half4 raw = SAMPLE_TEXTURE2D_ARRAY(_Faces, sampler_Faces, i.uv0, gSlice);
                half inkness = saturate((0.45 - dot(raw.rgb, half3(0.2126, 0.7152, 0.0722))) / 0.3);
                half3 tint = lerp(raw.rgb, _Ink.rgb, inkness);
                STYLE_BODY(FaceMask)
            }
            ENDHLSL
        }
    }
}
