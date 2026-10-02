// A jelly slime on one sprite: a soft blob that wobbles (its outline is a radius that ripples with angle and time), settles wider at the bottom, and is lit like translucent jelly: a deeper, more saturated core, a light fresnel rim, light scattering up from the floor, slow caustic swirls inside, rising bubbles, a big soft sheen and a sharp glint, and an ink outline. _Squash (0..1) flattens it (landing). Colour = _Col; _Seed desyncs neighbours.
// Theme lab (Docs/SHADER_LAB.md). UI shader: works on a uGUI Graphic, masks included. Output is premultiplied alpha.
Shader "CasualGame/Lab/SlimeSkin"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        _Col ("Slime colour", Color) = (0.49, 0.89, 0.55, 1)
        _Seed ("Seed", Float) = 0
        _Wobble ("Wobble", Range(0, 0.15)) = 0.05
        _Squash ("Squash", Range(0, 1)) = 0
        _R ("Blob radius in the quad", Range(0.4, 1)) = 0.72
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
                half4 _Col;
                float _Seed, _Wobble, _Squash, _R;
            CBUFFER_END

            half4 fragBody(ArrowVaryings i)
            {
                half3 ink = half3(0.118, 0.133, 0.251);
                float2 p = (i.uv0 * 2.0 - 1.0) / _R;
                float tt = _Time.y * 2.2 + _Seed * 1.7;
                // squash and stretch: wider + lower when squashed, breathing a little all the time
                float sq = _Squash * 0.35 + sin(tt * 0.7) * 0.03;
                p = float2(p.x * (1.0 - sq * 0.6), (p.y + 0.2) * (1.0 + sq) - 0.2);
                // drop shape: wider at the bottom, a slightly pointed top
                float ang = atan2(p.y, p.x);
                float radius = 1.0
                    + _Wobble * (sin(ang * 3.0 + tt) * 0.6 + sin(ang * 5.0 - tt * 1.3) * 0.4)
                    + 0.04 * saturate(-p.y)                    // a little belly: the blob hugs its round collider
                    - 0.06 * saturate(p.y) * (1 - abs(p.x));   // peak
                // the flat bottom (it sits on the floor) is part of the shape, so the outline runs along it too
                float d = max(length(p) - radius, -(p.y + 0.94));
                float body = AAInside(d);
                float edgeDist = saturate(-d / radius);          // 0 at the rim .. 1 deep inside
                // jelly lighting
                half3 deep = _Col.rgb * 0.8;
                half3 light = lerp(_Col.rgb, half3(1, 1, 1), 0.45);
                half3 c = lerp(light, deep, smoothstep(0.0, 0.85, edgeDist));          // fresnel: rim light, deep core
                c += _Col.rgb * 0.45 * smoothstep(0.2, -0.9, p.y) * edgeDist;          // light scattered up from the floor
                // soft caustic swirls (smooth waves: grid noise left square patches)
                float caustic = sin(p.x * 3.1 + tt * 0.6 + sin(p.y * 2.3 - tt * 0.4)) * sin(p.y * 3.7 - tt * 0.5 + sin(p.x * 2.1 + tt * 0.3));
                c *= 0.94 + 0.12 * caustic;
                // bubbles rising through the jelly
                float bub = 0;
                [unroll] for (int k = 0; k < 4; k++)
                {
                    float fk = k + _Seed * 0.37;
                    float2 bc = float2(sin(fk * 2.4) * 0.5, frac(fk * 0.29 + _Time.y * 0.07) * 1.6 - 0.85);
                    float br = 0.04 + 0.025 * frac(fk * 0.71);
                    float ring = AAInside(length(p - bc) - br) * (1 - AAInside(length(p - bc) - br * 0.6));
                    bub = max(bub, ring);
                }
                c = lerp(c, half3(1, 1, 1), bub * 0.55 * edgeDist);
                // a big soft sheen and a sharp glint
                float sheen = smoothstep(0.42, 0.0, length((p - float2(-0.35, 0.42)) / float2(1.0, 0.7))) * 0.35;
                float glint = AAInside(length(p - float2(-0.48, 0.5)) - 0.08) * 0.9;
                c = lerp(c, half3(1, 1, 1), saturate(sheen + glint));
                // ink outline
                c = lerp(c, ink, body * (1 - AAInside(d + 0.07)));
                float a = body * lerp(0.9, 1.0, 1 - edgeDist) * i.color.a;     // a hint see-through in the middle
                return half4(c * a, a);
            }

            // inside a UI mask (scroll list, card), nothing draws outside it
            half4 frag(ArrowVaryings i) : SV_Target { return fragBody(i) * UIClip(i.local); }
            ENDHLSL
        }
    }
}
