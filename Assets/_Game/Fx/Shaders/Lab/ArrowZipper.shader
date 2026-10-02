// Bruh Arrows "Zipper": the arrow is a zip on fabric of its colour. The slider (the face cap) runs from the tail to the head (_Unzip); behind it the teeth part and the zip vanishes. Blocked: the slider jams. Needs GlowPad 0.16.
// Interactive skin (Docs/SHADER_LAB.md). The game / lab drives the "Live" properties every frame; everything else is
// a look you can tune on the material asset (Assets/_Game/Skins). UI shader, premultiplied alpha, masks work.
Shader "CasualGame/Lab/ArrowZipper"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        [Header(Look)]
        _FabricWidth ("Fabric half width (cells)", Range(0.1, 0.3)) = 0.2
        _TeethColor ("Teeth", Color) = (0.85, 0.85, 0.9, 1)
        _ToothPitch ("Tooth pitch (cells)", Range(0.03, 0.12)) = 0.06
        _Weave ("Fabric weave", Range(0, 0.3)) = 0.12
        [Header(Live (set by the game))]
        _Len ("Arrow length (cells, tail to tip)", Float) = 3
        _Unzip ("Unzipped 0..1 (tail to head)", Range(0, 1)) = 0
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
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "SkinCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _FabricWidth, _ToothPitch, _Weave, _Len, _Unzip;
                half4 _TeethColor;
            CBUFFER_END

            #define HALF_LINE 0.104

            half4 fragBody(ArrowVaryings i)
            {
                float X = i.uv0.x, Y = i.uv0.y * HALF_LINE;
                half3 col = i.color.rgb;
                float W = _FabricWidth;
                float front = _Unzip * (_Len + 0.2) - 0.1;
                float behind = front - X;                                     // > 0: already unzipped
                float part = saturate(behind / 0.5);
                // the two halves slide apart behind the slider and fade out
                float side = Y >= 0 ? 1.0 : -1.0;
                float y = Y - side * part * 0.12;
                float fade = 1.0 - saturate((behind - 0.25) / 0.5);
                float fabric = AAInside(max(abs(y) - W, -X - 0.1));
                if (i.uv1.y > 0.5) fabric = AAInside(abs(Y) - HALF_LINE * 1.1);    // chevron: plain fabric, line-wide arms
                half3 c = col * (1.0 - _Weave * (step(0.5, frac(X * 40.0)) * 0.5 + step(0.5, frac(Y * 40.0)) * 0.5));
                // stitch lines
                Paint(c, col * 0.6, AAInside(abs(abs(y) - W * 0.82) - 0.008) * step(0.5, frac(X * 12.0)));
                // teeth alternate sides along the middle
                float kx = X / _ToothPitch;
                float alt = step(0.5, frac(kx * 0.5));
                float toothSide = alt > 0.5 ? 1.0 : -1.0;
                float2 tq = float2((frac(kx) - 0.5) * _ToothPitch, y - toothSide * 0.03 + side * 0.0);
                float tooth = AAInside(RoundBox(tq, float2(_ToothPitch * 0.32, 0.045), 0.01)) * step(0.0, toothSide * Y + (1.0 - part) * 0.06);
                tooth *= step(i.uv1.y, 0.5) * step(X, _Len - 0.25);
                Paint(c, INK, AAInside(RoundBox(tq, float2(_ToothPitch * 0.32, 0.045), 0.01) - 0.012) * tooth);
                Paint(c, _TeethColor.rgb * (0.85 + 0.3 * step(0.0, tq.y)), tooth);
                float a = fabric * (behind > 0 ? fade : 1.0);
                a = max(a, tooth * fade);
                Paint(c, INK, AAInside(abs(abs(y) - W) - 0.012) * 0.4);
                return half4(c * a * i.color.a, a * i.color.a);
            }

            // inside a UI mask (scroll list, card), nothing draws outside it
            half4 frag(ArrowVaryings i) : SV_Target { return fragBody(i) * UIClip(i.local); }
            ENDHLSL
        }
    }
}
