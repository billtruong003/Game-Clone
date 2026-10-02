// Shared helpers for the interactive skin shaders (Docs/SHADER_LAB.md). Everything works in the Image's quad:
// uv0 0..1; ball skins use p = (uv0 * 2 - 1) / _R so the sphere has radius 1. Colours are painted back to front
// with Paint(colour, coverage); coverage comes from AAInside(signed distance) for a one-pixel soft edge.
#ifndef SKIN_COMMON
#define SKIN_COMMON

#include "ArrowLabCommon.hlsl"

static const half3 INK = half3(0.118, 0.133, 0.251);

float2 Rot2(float2 v, float a) { float c = cos(a), s = sin(a); return float2(c * v.x - s * v.y, s * v.x + c * v.y); }
float RoundBox(float2 p, float2 b, float r) { float2 q = abs(p) - b + r; return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r; }
// approximate signed distance to an ellipse (good enough for soft edges)
float Ellipse(float2 p, float2 r) { return (length(p / r) - 1.0) * min(r.x, r.y); }
float Segment(float2 p, float2 a, float2 b) { float2 pa = p - a, ba = b - a; float h = saturate(dot(pa, ba) / dot(ba, ba)); return length(pa - ba * h); }
float Circle(float2 p, float r) { return length(p) - r; }
void Paint(inout half3 c, half3 col, float cover) { c = lerp(c, col, saturate(cover)); }
float Hash11(float n) { return frac(sin(n * 12.9898) * 43758.5453); }

float3 SphereNormal(float2 p) { return float3(p, sqrt(saturate(1.0 - dot(p, p)))); }

// Toon ball light over a surface colour: soft terminator, rim light, a gloss dot, the ink outline.
half3 BallShade(float2 p, half3 surf, float gloss)
{
    float3 n = SphereNormal(p);
    float3 L = normalize(float3(-0.5, 0.6, 0.65));
    float shade = lerp(0.74, 1.0, smoothstep(-0.05, 0.1, dot(n, L)));
    half3 c = surf * shade + pow(1.0 - n.z, 3.0) * 0.22;
    c += smoothstep(0.15, 0.1, length(p - float2(-0.38, 0.42))) * 0.6 * gloss;
    float r = length(p);
    Paint(c, INK, AAInside(r - 1.0) * (1.0 - AAInside(r - 0.93)));
    return c;
}

// Glass dome over whatever is inside (snow globe, hamster ball): a coloured rim, a long highlight arc, the outline.
half3 GlassShade(float2 p, half3 inside, half3 tint, float tintRim)
{
    float3 n = SphereNormal(p);
    float rim = pow(1.0 - n.z, 2.2);
    half3 c = lerp(inside, tint, rim * tintRim);
    float arc = AAInside(abs(length(p - float2(0.1, -0.1)) - 0.78) - 0.05) * step(p.x, -0.15) * step(0.0, p.y);
    c = lerp(c, half3(1, 1, 1), arc * 0.55);
    c += smoothstep(0.1, 0.06, length(p - float2(0.45, 0.5))) * 0.7;
    float r = length(p);
    Paint(c, INK, AAInside(r - 1.0) * (1.0 - AAInside(r - 0.94)));
    return c;
}

// UI output: premultiplied colour with the Graphic's alpha.
half4 Out(half3 c, float a, half4 vertexColor) { a *= vertexColor.a; return half4(c * a, a); }

#endif
