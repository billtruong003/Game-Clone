// Meh Merge premium balls on one sprite: a fake 3D ball (toon light, rim, gloss, ink outline) with a surface per set: 0 billiard (rolling stripes, a cream face plate with a small number from _Digits), 1 sports (_Variant 0 tennis 1 baseball 2 basketball 3 soccer 4 beach 5 golf 6 marble), 2 planet (_Variant 0 rocky 1 earth 2 gas 3 ringed 4 sun), 3 monster (toon colour + two horns), 4 slime (a wobbling jelly drop), 5 eyeball (a real eye: veined sclera, iris and pupil turning to _Look). _Pattern (equirect) can replace the procedural surface.
// Theme lab (Docs/SHADER_LAB.md). UI shader: works on a uGUI Graphic, masks included. Output is premultiplied alpha.
Shader "CasualGame/Lab/BallSkin"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        _Set ("Set", Range(0, 5)) = 0
        _Variant ("Variant", Float) = 0
        _ColA ("Colour A", Color) = (0.95, 0.76, 0.19, 1)
        _ColB ("Colour B", Color) = (1, 0.97, 0.92, 1)
        _Number ("Number", Float) = 1
        _Stripe ("Stripe ball", Range(0, 1)) = 0
        _Digits ("Digits atlas (16 cells)", 2D) = "black" {}
        _Pattern ("Pattern (equirect)", 2D) = "white" {}
        _UsePattern ("Use pattern", Range(0, 1)) = 0
        _Spin ("Roll angle", Float) = 0
        _Look ("Look direction (eyeball)", Vector) = (0, 0, 0, 0)
        _R ("Sphere radius in the quad", Range(0.3, 1)) = 0.94
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

            TEXTURE2D(_Digits); SAMPLER(sampler_Digits);
            TEXTURE2D(_Pattern); SAMPLER(sampler_Pattern);
            CBUFFER_START(UnityPerMaterial)
                float _Set, _Variant, _Number, _Stripe, _UsePattern, _Spin, _R;
                half4 _ColA, _ColB;
                float4 _Look;
            CBUFFER_END

            // the Image is the ball's square: p in -1..1, the sphere fills radius _R (room around it for rings / glow)
            float3 SphereNormal(float2 p) { float r2 = dot(p, p); return float3(p, sqrt(saturate(1.0 - r2))); }
            float3 RotY(float3 v, float a) { float c = cos(a), s = sin(a); return float3(c * v.x + s * v.z, v.y, -s * v.x + c * v.z); }
            float3 RotZ(float3 v, float a) { float c = cos(a), s = sin(a); return float3(c * v.x - s * v.y, s * v.x + c * v.y, v.z); }
            float Noise3(float3 p) { return ValueNoise(p.xy * 1.7 + p.z * 3.1) * 0.5 + ValueNoise(p.yz * 2.3 - p.x * 1.3) * 0.5; }
            float Fbm3(float3 p) { float s = 0, a = 0.5; [unroll] for (int k = 0; k < 4; k++) { s += a * Noise3(p); p *= 2.07; a *= 0.5; } return s; }

            half4 frag(ArrowVaryings i) : SV_Target
            {
                half3 ink = half3(0.118, 0.133, 0.251);
                float2 p = (i.uv0 * 2.0 - 1.0) / _R;
                // slime: the drop squashes and wobbles; its shape is a sphere with a flatter, wider bottom
                if (_Set > 3.5 && _Set < 4.5)
                {
                    float w = sin(_Time.y * 3.0 + _Variant) * 0.04;
                    p = float2(p.x * (1.0 - w), (p.y + 0.08) * (1.0 + w));
                    p.x *= lerp(1.0, 0.86, saturate(-p.y));          // wider at the bottom
                }
                float r = length(p);
                float3 n = SphereNormal(p);
                float3 s = RotY(RotZ(n, _Spin), 0.35);
                float lon = atan2(s.x, s.z), lat = asin(clamp(s.y, -1.0, 1.0));
                half3 surf = _ColA.rgb;
                half emissive = 0, glossK = 1;
                if (_UsePattern > 0.5)
                    surf = SAMPLE_TEXTURE2D(_Pattern, sampler_Pattern, float2(lon / 6.2832 + 0.5, lat / 3.1416 + 0.5)).rgb;
                else if (_Set < 0.5)        // billiard: the stripes roll, the face plate stays in front
                {
                    surf = (_Stripe > 0.5 && abs(s.y) > 0.42) ? _ColB.rgb : _ColA.rgb;
                    // the plate turns with the ball (the face on it does too); the stripes roll under it
                    float2 pl = RotZ(float3(p, 0), -_Spin).xy;
                    float plate = AAInside(length(pl - float2(0, -0.06)) - 0.66);
                    surf = lerp(surf, _ColB.rgb, plate);
                    // small number at the top of the plate
                    float2 duv = (pl - float2(0, 0.36)) / 0.26 * 0.5 + 0.5;
                    float cell = clamp(_Number, 1, 16) - 1;
                    float digit = (duv.x >= 0 && duv.x <= 1 && duv.y >= 0 && duv.y <= 1)
                        ? SAMPLE_TEXTURE2D(_Digits, sampler_Digits, float2((cell + duv.x) / 16.0, duv.y)).a : 0;
                    surf = lerp(surf, ink, digit * plate);
                    float plateRim = plate * (1 - AAInside(length(pl - float2(0, -0.06)) - 0.62));
                    surf = lerp(surf, ink, plateRim * 0.25);
                }
                else if (_Set < 1.5)        // sports
                {
                    int v = (int)round(_Variant);
                    if (v == 0) { float seam = abs(s.y - 0.55 * sin(lon * 2.0)); surf = lerp(_ColA.rgb, half3(1, 1, 1), 1 - smoothstep(0.05, 0.08, seam)); }
                    else if (v == 1) { surf = half3(0.97, 0.95, 0.9); float seam = abs(s.y - 0.55 * sin(lon * 2.0)); float st = step(0.5, frac(lon * 9.0)) * (1 - smoothstep(0.03, 0.06, abs(seam - 0.12))) + step(0.5, frac(lon * 9.0)) * (1 - smoothstep(0.03, 0.06, abs(seam + 0.12))); surf = lerp(surf, half3(0.8, 0.12, 0.15), saturate(st)); }
                    else if (v == 2) { surf = half3(0.93, 0.48, 0.15); float l = min(min(abs(s.y), abs(s.x)), abs(abs(s.z) - 0.75 + 0.6 * abs(s.y))); surf = lerp(surf, half3(0.12, 0.08, 0.06), 1 - smoothstep(0.03, 0.06, l)); }
                    else if (v == 3)
                    {
                        static const float3 P[6] = { float3(0, 0.526, 0.851), float3(0, -0.526, 0.851), float3(0.526, 0.851, 0), float3(-0.526, 0.851, 0), float3(0.851, 0, 0.526), float3(-0.851, 0, 0.526) };
                        float best = 0;
                        [unroll] for (int k = 0; k < 6; k++) best = max(best, abs(dot(s, P[k])));
                        surf = best > 0.93 ? half3(0.12, 0.12, 0.14) : half3(0.98, 0.98, 0.98);
                    }
                    else if (v == 4) { int seg = (int)floor((lon / 6.2832 + 0.5) * 6.0); half3 cs[3] = { half3(0.94, 0.25, 0.3), half3(1, 0.82, 0.25), half3(0.25, 0.6, 0.95) }; surf = (seg % 2 == 0) ? half3(1, 1, 1) : cs[(seg / 2) % 3]; if (abs(s.y) > 0.9) surf = half3(1, 1, 1); }
                    else if (v == 5) { float dim = ValueNoise(float2(lon, lat) * 9.0); surf = half3(0.97, 0.97, 0.96) * (0.88 + 0.12 * smoothstep(0.3, 0.7, dim)); }
                    else { float sw = Fbm3(s * 1.5 + float3(_Variant, 0, 0)); surf = lerp(_ColA.rgb, _ColB.rgb, smoothstep(0.35, 0.65, sin(sw * 12.0) * 0.5 + 0.5)); }
                }
                else if (_Set < 2.5)        // planets
                {
                    int v = (int)round(_Variant);
                    float f = Fbm3(s * 2.2);
                    if (v == 0) { surf = lerp(_ColA.rgb, _ColB.rgb, f); float crater = smoothstep(0.62, 0.66, Noise3(s * 5.0)); surf *= 1 - crater * 0.35; }
                    else if (v == 1) { float land = step(0.52, f); surf = lerp(half3(0.18, 0.45, 0.85), lerp(half3(0.3, 0.7, 0.35), half3(0.85, 0.75, 0.5), saturate((f - 0.55) * 4)), land); float cloud = smoothstep(0.55, 0.7, Fbm3(s * 3.0 + _Time.y * 0.05)); surf = lerp(surf, half3(1, 1, 1), cloud * 0.8); if (abs(s.y) > 0.88) surf = half3(0.95, 0.97, 1); }
                    else if (v == 2 || v == 3) { float band = sin(s.y * 14.0 + (f - 0.5) * 3.0); surf = lerp(_ColA.rgb, _ColB.rgb, band * 0.5 + 0.5); }
                    else { surf = lerp(_ColA.rgb, _ColB.rgb, f); emissive = 1; }
                }
                else if (_Set < 3.5)        // monster: flat toon colour with a darker belly patch
                {
                    float belly = AAInside(length((p - float2(0, -0.45)) / float2(0.7, 0.45)) - 1.0);
                    surf = lerp(_ColA.rgb, lerp(_ColA.rgb, half3(1, 1, 1), 0.35), belly);
                }
                else if (_Set < 4.5)        // slime: translucent jelly, darker towards the middle, bubbles inside
                {
                    float depth = 1.0 - n.z;
                    surf = lerp(_ColA.rgb * 0.82, lerp(_ColA.rgb, half3(1, 1, 1), 0.45), depth);
                    float bub = 0;
                    [unroll] for (int k = 0; k < 3; k++)
                    {
                        float2 bc = float2(sin(k * 2.4 + _Variant) * 0.45, frac(k * 0.37 + _Time.y * 0.08 + _Variant * 0.1) * 1.4 - 0.7);
                        bub = max(bub, AAInside(length(p - bc) - (0.05 + 0.03 * k)) * (1 - AAInside(length(p - bc) - (0.03 + 0.03 * k))));
                    }
                    surf = lerp(surf, half3(1, 1, 1), bub * 0.6);
                }
                else                         // eyeball: veined sclera, iris + pupil on the sphere, turned to _Look
                {
                    glossK = 0.35;
                    float3 gaze = normalize(float3(_Look.x * 0.55, _Look.y * 0.55, 1.0));
                    float ang = acos(clamp(dot(n, gaze), -1.0, 1.0));
                    surf = half3(0.98, 0.95, 0.92) * (1.0 - 0.18 * smoothstep(0.6, 1.0, r));
                    float vein = (1 - smoothstep(0.0, 0.035, abs(Fbm(float2(atan2(p.y, p.x) * 3.0, r * 4.0)) - 0.5))) * smoothstep(0.55, 0.95, r);
                    surf = lerp(surf, half3(0.85, 0.25, 0.3), vein * 0.7);
                    float irisR = 0.48, pupilR = lerp(0.16, 0.22, _Variant);
                    float iris = AAInside(ang - irisR);
                    float fibre = 0.8 + 0.2 * ValueNoise(float2(atan2(n.y - gaze.y, n.x - gaze.x) * 10.0, ang * 12.0));
                    half3 irisCol = lerp(_ColB.rgb, _ColA.rgb, saturate(ang / irisR)) * fibre;
                    surf = lerp(surf, irisCol, iris);
                    surf = lerp(surf, ink * 0.6, iris * (1 - AAInside(ang - (irisR - 0.035))) * 0.8);   // limbal ring
                    surf = lerp(surf, half3(0.04, 0.04, 0.06), AAInside(ang - pupilR));
                }
                float3 L = normalize(float3(-0.5, 0.6, 0.65));
                float ndl = dot(n, L);
                float shade = emissive > 0.5 ? 1.0 : lerp(0.72, 1.0, smoothstep(-0.05, 0.1, ndl));
                float rim = pow(1.0 - n.z, 3.0) * 0.25;
                float gloss = emissive > 0.5 ? 0 : smoothstep(0.14, 0.1, length(p - float2(-0.38, 0.42))) * 0.6 * glossK;
                half3 c = surf * shade + rim + gloss;
                float ball = AAInside(r - 1.0);
                float outline = ball * (1 - AAInside(r - 0.93));
                c = lerp(c, ink, outline * (1 - emissive));
                float a = ball;
                if (_Set > 3.5 && _Set < 4.5) a *= 0.94;   // jelly: a hint see-through
                if (_Set > 2.5 && _Set < 3.5)                // monster horns: two curved cones above the head
                {
                    [unroll] for (int k = 0; k < 2; k++)
                    {
                        float side = k == 0 ? -1.0 : 1.0;
                        float2 h = p - float2(0.5 * side, 0.68);
                        h.x *= side;
                        float tcoord = saturate((h.y + 0.1) / 0.85);                   // 0 base .. 1 tip
                        float bend = 0.3 * tcoord * tcoord;
                        float width = lerp(0.26, 0.0, pow(tcoord, 0.8));
                        float dist = abs(h.x - bend) - width;
                        float horn = AAInside(dist) * step(-0.1, h.y) * step(h.y, 0.75) * (1 - ball);
                        float hornEdge = horn * (1 - AAInside(dist + 0.05));
                        // striped horn: rings around it (bending with the horn), darker towards the tip
                        float stripe = step(0.5, frac(tcoord * 5.0 + (h.x - bend) * 0.8));
                        half3 hc = lerp(_ColB.rgb, _ColB.rgb * 0.75, tcoord);
                        hc = lerp(hc, lerp(_ColA.rgb, ink, 0.35), stripe * 0.85);
                        c = lerp(c, lerp(hc, ink, hornEdge), horn);
                        a = max(a, horn);
                    }
                }
                if (_Set > 1.5 && _Set < 2.5)
                {
                    int v = (int)round(_Variant);
                    if (v == 3)
                    {
                        float2 q = float2(p.x, p.y / 0.32);
                        float rr = length(RotZ(float3(q, 0), -0.35).xy);
                        float ring = AAInside(abs(rr - 1.38) - 0.2) * (1 - AAInside(abs(rr - 1.38) - 0.05) * 0.4);   // fits the stage walls
                        bool front = p.y * cos(0.35) + p.x * sin(0.35) < 0;
                        if (ring > 0 && (front || ball < 0.5)) { c = lerp(c, _ColB.rgb * 1.1, ring); a = max(a, ring); }
                    }
                    if (v == 4)
                    {
                        float quadEdge = saturate((1.0 - length(i.uv0 * 2.0 - 1.0)) * 5.0);
                        float corona = exp(-max(r - 1.0, 0) * 4.0) * (1 - ball) * quadEdge;
                        c = lerp(c, _ColA.rgb, corona); a = max(a, corona * 0.8);
                    }
                }
                a *= i.color.a;
                return half4(c * a, a);
            }
            ENDHLSL
        }
    }
}
