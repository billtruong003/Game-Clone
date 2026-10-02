// Bruh Arrows "Tape": the arrow is a strip of coloured tape. A free strip lifts off the paper (it casts a shadow), ready to grab; tapping peels it off from the tail (_Peel); a blocked one peels part-way and sticks back down. Needs GlowPad 0.12.
// Interactive skin (Docs/SHADER_LAB.md). The game / lab drives the "Live" properties every frame; everything else is
// a look you can tune on the material asset (Assets/_Game/Skins). UI shader, premultiplied alpha, masks work.
Shader "CasualGame/Lab/ArrowTape"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        [Header(Look)]
        _TapeWidth ("Tape half width (cells)", Range(0.08, 0.25)) = 0.17
        _Opacity ("Tape opacity", Range(0.5, 1)) = 0.9
        _Fibres ("Paper fibres", Range(0, 0.3)) = 0.12
        _Teeth ("Torn edge teeth", Range(0, 0.06)) = 0.03
        _UnderColor ("Underside (glue side)", Color) = (1, 1, 1, 1)
        _Lift ("Shadow offset when free (cells)", Range(0, 0.12)) = 0.06
        [Header(Live (set by the game))]
        _Len ("Arrow length (cells, tail to tip)", Float) = 3
        _Free ("Free (lifted) 0..1", Range(0, 1)) = 1
        _Peel ("Peeled 0..1 (tail to head)", Range(0, 1)) = 0
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
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
            #include "SkinCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _TapeWidth, _Opacity, _Fibres, _Teeth, _Lift, _Len, _Free, _Peel;
                half4 _UnderColor;
            CBUFFER_END

            #define HALF_LINE 0.104

            half4 frag(ArrowVaryings i) : SV_Target
            {
                float X = i.uv0.x, Y = i.uv0.y * HALF_LINE;
                half3 col = i.color.rgb;
                float W = _TapeWidth;
                float chevron = step(0.5, i.uv1.y);
                // torn tail end: a zigzag across the tape
                float teeth = abs(frac(Y * 18.0) - 0.5) * 2.0 * _Teeth;
                float peelX = _Peel * (_Len + 0.5) - 0.12;
                float body = chevron > 0.5 ? abs(Y) - W * 0.62 : max(abs(Y) - W, -(X + 0.12 - teeth));
                float edge = max(body, -(X - peelX));                          // the peeled part is gone
                float tape = AAInside(edge);
                // lifted: a soft shadow beside the strip
                float lift = _Lift * _Free;
                float shadow = AAInside(max(abs(Y + lift) - W * lerp(1.0, 0.62, chevron), -(X - peelX) - 0.02) - 0.02) * (1.0 - tape) * 0.22 * _Free;
                half3 c = col * (0.95 + 0.05 * sin(Y * 40.0));
                c *= 1.0 - _Fibres * ValueNoise(float2(X * 30.0, Y * 4.0));
                c = lerp(c, half3(1, 1, 1), smoothstep(0.06, 0.0, abs(Y + W * 0.3)) * (0.12 + 0.12 * _Free));
                // the curl at the peel front: the lighter glue side, a thin shadow ahead
                float front = X - peelX;
                float peeling = step(0.001, _Peel);
                Paint(c, lerp(_UnderColor.rgb, col, 0.35), (1.0 - smoothstep(0.0, 0.14, front)) * peeling);
                c *= 1.0 - smoothstep(0.08, 0.0, abs(front - 0.16)) * 0.35 * peeling;
                Paint(c, col * 0.55, AAInside(abs(edge) - 0.01) * 0.35);
                float a = tape * lerp(_Opacity, 1.0, chevron);                // the chevron's arms overlap: keep it solid
                shadow *= 1.0 - chevron;
                c = c * a;                                                     // premultiply, then lay the shadow under
                c += half3(0, 0, 0) * shadow;
                a = a + shadow * (1.0 - a);
                return half4(c * i.color.a, a * i.color.a);
            }

            ENDHLSL
        }
    }
}
