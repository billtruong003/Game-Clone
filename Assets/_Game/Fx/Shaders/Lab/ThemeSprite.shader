// Any sprite (arrow tail caps, icons) styled like its theme: 0 ink bleed, 1 chalk, 2 neon glow, 3 hologram, 4 pixel, 5 terminal. Colour = the Image colour.
// Theme lab (Docs/SHADER_LAB.md). UI shader: works on a uGUI Graphic, masks included. Output is premultiplied alpha.
Shader "CasualGame/Lab/ThemeSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Mode ("Style", Range(0, 5)) = 0
        _GlowR ("Glow radius (uv)", Range(0, 0.3)) = 0.12
        _Pixels ("Pixel grid", Range(4, 64)) = 12
        _Circle ("Procedural circle instead of the sprite", Range(0, 1)) = 0
        _CircleR ("Circle radius (fraction of the quad)", Range(0.1, 1)) = 0.95
        _Ring ("Ring width (0 = filled disc)", Range(0, 0.5)) = 0
        _Inner ("Fill inside the ring (premultiplied)", Color) = (0, 0, 0, 0)
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

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float _Mode, _GlowR, _Pixels, _Circle, _CircleR, _Ring;
                half4 _Inner;
            CBUFFER_END
            // a sprite from an atlas cannot glow past its own rect: round caps are drawn as a procedural circle in a
            // bigger quad instead, so halos and pixel grids have room and never sample a neighbour in the atlas
            float SpriteMask(float2 uv)
            {
                if (_Circle < 0.5) return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
                float d = length(uv * 2.0 - 1.0) / _CircleR;
                float disc = saturate((1.0 - d) * 40.0);
                if (_Ring > 0.001) disc *= saturate((d - (1.0 - _Ring)) * 40.0);
                return disc;
            }

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
                    a = m * step(0.28, g); col = lerp(col, half3(1, 1, 1), 0.15); } \
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
                half3 tint = i.color.rgb;
                if (_Circle > 0.5 && _Ring > 0.001 && length(i.uv0 * 2.0 - 1.0) / _CircleR < 1.0 - _Ring * 0.5)
                    return half4(_Inner.rgb, 1) * i.color.a;
                STYLE_BODY(SpriteMask)
            }
            ENDHLSL
        }
    }
}
