// Meh Merge "Compass": every ball is a compass in a case of its tier colour. The needle points at the nearest ball of the same tier, so it hints where to drop; with no match it slowly searches.
// Interactive skin (Docs/SHADER_LAB.md). The game / lab drives the "Live" properties every frame; everything else is
// a look you can tune on the material asset (Assets/_Game/Skins). UI shader, premultiplied alpha, masks work.
Shader "CasualGame/Lab/SkinCompassBall"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        [Header(Look)]
        _R ("Ball radius in the quad", Range(0.3, 1)) = 0.94
        _DialColor ("Dial", Color) = (0.98, 0.95, 0.87, 1)
        _BezelColor ("Brass bezel", Color) = (0.85, 0.66, 0.3, 1)
        _NeedleColor ("Needle (pointing end)", Color) = (0.9, 0.2, 0.22, 1)
        _NeedleTail ("Needle (back end)", Color) = (0.8, 0.82, 0.86, 1)
        _TickColor ("Ticks", Color) = (0.25, 0.25, 0.3, 1)
        _DialSize ("Dial size", Range(0.4, 0.85)) = 0.66
        [Header(Live (set by the game))]
        _Spin ("Roll angle (the dial ticks turn with it)", Float) = 0
        _Needle ("Needle angle (radians, 0 = right)", Float) = 1.5708
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
                float _R, _DialSize, _Spin, _Needle;
                half4 _DialColor, _BezelColor, _NeedleColor, _NeedleTail, _TickColor;
            CBUFFER_END

            half4 fragBody(ArrowVaryings i)
            {
                float2 p = (i.uv0 * 2.0 - 1.0) / _R;
                float r = length(p);
                half3 c = i.color.rgb;
                float R1 = _DialSize;
                // brass bezel ring
                float bezel = AAInside(r - (R1 + 0.1));
                half3 brass = _BezelColor.rgb * (0.8 + 0.35 * (p.y * 0.5 + 0.5)) + smoothstep(0.92, 1.0, sin(atan2(p.y, p.x) * 2.0 + 1.0)) * 0.25;
                Paint(c, INK, bezel);
                Paint(c, brass, AAInside(r - (R1 + 0.07)));
                // dial with ticks (they turn with the case)
                float dial = AAInside(r - R1);
                half3 dc = _DialColor.rgb * (1.0 - 0.12 * r / R1);
                float a = atan2(p.y, p.x) - _Spin;
                float seg = a / 6.2832 * 16.0;
                float tickD = abs(frac(seg + 0.5) - 0.5) / 16.0 * 6.2832 * r;
                float major = step(abs(frac(seg / 4.0 + 0.125) - 0.125), 0.02);
                float tick = AAInside(tickD - lerp(0.012, 0.022, major)) * step(R1 * lerp(0.8, 0.7, major), r) * step(r, R1 * 0.95);
                Paint(dc, _TickColor.rgb, tick);
                // north marker
                float2 nq = Rot2(p, -_Spin) - float2(0, R1 * 0.82);
                Paint(dc, _NeedleColor.rgb, AAInside(max(abs(nq.x) * 2.0 + nq.y * 1.2, -nq.y - 0.08) - 0.06));
                Paint(c, dc, dial);
                // needle shadow, needle, pin
                float2 q = Rot2(p, -(_Needle - 1.5708));
                float2 qs = q - Rot2(float2(0.03, -0.035), -(_Needle - 1.5708));
                float len = R1 * 0.86, wid = R1 * 0.13;
                float sh = abs(qs.x) / wid + abs(qs.y) / len - 1.0;
                Paint(c, half3(0, 0, 0), AAInside(sh * wid) * 0.22 * dial);
                float nd = (abs(q.x) / wid + abs(q.y) / len - 1.0) * wid;
                Paint(c, INK, AAInside(nd - 0.02) * dial);
                Paint(c, q.y > 0 ? _NeedleColor.rgb : _NeedleTail.rgb, AAInside(nd));
                Paint(c, INK, AAInside(Circle(p, 0.065)));
                Paint(c, brass, AAInside(Circle(p, 0.045)));
                // glass over the dial
                c += smoothstep(0.3, 0.0, length(p - float2(-0.25, 0.3))) * 0.25 * dial;
                c = BallShade(p, c, 1.0);
                return Out(c, AAInside(r - 1.0), i.color);
            }

            // inside a UI mask (scroll list, card), nothing draws outside it
            half4 frag(ArrowVaryings i) : SV_Target { return fragBody(i) * UIClip(i.local); }
            ENDHLSL
        }
    }
}
