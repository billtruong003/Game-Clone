// Nah Blocks premium blocks on one sprite (the block's square): 0 retro bevel bricks, 2 toy brick (rounded glossy plastic, two studs inside the top of the cell, seen slightly from above), 3 cut gem with a soft travelling glint. Colour = the Image colour.
// Theme lab (Docs/SHADER_LAB.md). UI shader: works on a uGUI Graphic, masks included. Output is premultiplied alpha.
Shader "CasualGame/Lab/BlockSkin"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        _Set ("Set", Range(0, 3)) = 0
        _Seed ("Seed", Float) = 0
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
                float _Set, _Seed;
            CBUFFER_END
            float RoundBox(float2 p, float2 b, float r) { float2 q = abs(p) - b + r; return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r; }

            half4 frag(ArrowVaryings i) : SV_Target
            {
                half3 base = i.color.rgb;
                half3 ink = half3(0.118, 0.133, 0.251);
                float2 uv = i.uv0;
                half3 c = base; float a = 1;
                if (_Set < 0.5)              // retro bevel: hard light top/left, dark bottom/right, black frame
                {
                    float b = 0.16;
                    float edge = min(min(uv.x, uv.y), min(1 - uv.x, 1 - uv.y));
                    if (edge < b) c = (uv.y > 1 - b && uv.y > uv.x && uv.y > 1 - uv.x) || (uv.x < b && uv.x < uv.y && uv.x < 1 - uv.y) ? lerp(base, 1, 0.45) : base * 0.55;
                    float inner = step(0.24, uv.x) * step(uv.x, 0.4) * step(0.62, uv.y) * step(uv.y, 0.74);
                    c = lerp(c, 1, inner * 0.6);
                    c = lerp(c, half3(0, 0, 0), 1 - AAInside(-edge + 0.025));
                }
                else if (_Set < 2.5)         // toy brick: a rounded plastic block with two studs standing on top
                {
                    float2 p = uv * 2 - 1;
                    float body = RoundBox(p - float2(0, -0.18), float2(0.94, 0.76), 0.2);   // top edge at y = 0.58
                    float bodyA = AAInside(body);
                    a = 0; c = 0;
                    // studs first (the body covers their feet): ink outline, cylinder side, lit cap
                    [unroll] for (int k = 0; k < 2; k++)
                    {
                        float sx = k == 0 ? -0.46 : 0.46;
                        float2 q = p - float2(sx, 0);
                        float side = step(abs(q.x), 0.3) * step(0.45, q.y) * step(q.y, 0.78);
                        float cap = AAInside(length((q - float2(0, 0.78)) / float2(0.3, 0.11)) - 1.0);
                        float sideInk = step(abs(q.x), 0.36) * step(0.45, q.y) * step(q.y, 0.78);
                        float capInk = AAInside(length((q - float2(0, 0.78)) / float2(0.36, 0.17)) - 1.0);
                        float inkA = max(sideInk, capInk);
                        float fillA = max(side, cap);
                        float shadeX = 1.0 - 0.25 * smoothstep(-0.3, 0.3, q.x);           // lit from the left
                        half3 studCol = lerp(base * 0.85 * shadeX, lerp(base, 1, 0.25), cap);
                        c = lerp(c, ink, inkA);
                        c = lerp(c, studCol, fillA);
                        a = max(a, inkA);
                    }
                    // body: plastic shading, a lighter top band, a soft sheen
                    float topBand = smoothstep(0.3, 0.42, p.y);
                    half3 bc = base * lerp(0.97, 1.12, topBand);
                    bc = lerp(bc, 1, smoothstep(0.22, 0.0, abs(p.x + 0.58)) * 0.2 * (1 - topBand));
                    bc *= 1.0 - 0.12 * smoothstep(-0.3, -0.95, p.y);
                    bc = lerp(bc, ink, 1 - AAInside(body + 0.06));
                    c = lerp(c, bc, bodyA);
                    a = max(a, bodyA);
                }
                else                         // cut gem: an octagon with a table and 8 facets, a soft glint
                {
                    float2 p = uv * 2 - 1;
                    float oct = max(max(abs(p.x), abs(p.y)), (abs(p.x) + abs(p.y)) * 0.7071);
                    a = AAInside(oct - 0.95);
                    float table = AAInside(oct - 0.5);
                    float sector = floor((atan2(p.y, p.x) / 6.2832 + 0.5) * 8.0);
                    float facet = 0.62 + 0.38 * frac(sin(sector * 12.9898 + _Seed) * 43758.5);
                    c = base * (table > 0.5 ? 1.15 : facet);
                    float sweep = frac(_Time.y * 0.25 + _Seed * 0.13) * 6.0 - 3.0;
                    float glint = smoothstep(0.25, 0.0, abs(p.x + p.y - sweep)) * 0.35;
                    c = lerp(c, 1, glint);
                    c = lerp(c, ink, a * (1 - AAInside(oct - 0.88)));
                }
                a *= i.color.a;
                return half4(c * a, a);
            }
            ENDHLSL
        }
    }
}
