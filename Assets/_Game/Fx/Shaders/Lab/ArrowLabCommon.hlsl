// Shared helpers for the arrow theme lab shaders (Bruh Arrows). Arrow strokes (ArrowStroke) carry:
//   uv0.x = distance from the tail along the line, in cells     uv0.y = across the line, ±1 at the line edge
//   uv1.x = 0 at the tail .. 1 at the head                        uv1.y = 1 on the chevron
#ifndef ARROW_LAB_COMMON
#define ARROW_LAB_COMMON

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct ArrowAttributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv0 : TEXCOORD0; float2 uv1 : TEXCOORD1; };
struct ArrowVaryings
{
    float4 positionCS : SV_POSITION;
    half4 color : COLOR;
    float2 uv0 : TEXCOORD0;
    float2 uv1 : TEXCOORD1;
    float4 screen : TEXCOORD2;
    float2 local : TEXCOORD3;
};

ArrowVaryings ArrowVert(ArrowAttributes v)
{
    ArrowVaryings o;
    o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
    o.color = v.color;
    o.uv0 = v.uv0;
    o.uv1 = v.uv1;
    o.screen = ComputeScreenPos(o.positionCS);
    o.local = v.positionOS.xy;
    return o;
}

float Hash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float ValueNoise(float2 p)
{
    float2 i = floor(p), f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float a = Hash21(i), b = Hash21(i + float2(1, 0)), c = Hash21(i + float2(0, 1)), d = Hash21(i + float2(1, 1));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float Fbm(float2 p)
{
    float s = 0.0, a = 0.5;
    for (int i = 0; i < 4; i++) { s += a * ValueNoise(p); p *= 2.03; a *= 0.5; }
    return s;
}

// Anti-aliased "inside" for a signed distance-like value d (inside < 0), one screen pixel wide.
float AAInside(float d)
{
    float w = max(fwidth(d), 1e-4);
    return saturate(0.5 - d / w);
}

// Screen position in pixels.
float2 ScreenPx(float4 screen) { return screen.xy / max(screen.w, 1e-5) * _ScreenParams.xy; }

#endif
