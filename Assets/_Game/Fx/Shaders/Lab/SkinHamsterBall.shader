// Meh Merge "Hamster Ball": a clear plastic ball (the tier colour) with a hamster inside. It runs the way the ball rolls, tumbles on a hard landing and falls asleep when the ball rests.
// Interactive skin (Docs/SHADER_LAB.md). The game / lab drives the "Live" properties every frame; everything else is
// a look you can tune on the material asset (Assets/_Game/Skins). UI shader, premultiplied alpha, masks work.
Shader "CasualGame/Lab/SkinHamsterBall"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        [Header(Look)]
        _R ("Ball radius in the quad", Range(0.3, 1)) = 0.94
        _ShellTint ("Shell tint strength", Range(0, 1)) = 0.6
        _FurColor ("Fur", Color) = (0.93, 0.66, 0.36, 1)
        _BellyColor ("Belly", Color) = (1, 0.93, 0.8, 1)
        _PinkColor ("Ears / nose / paws", Color) = (1, 0.6, 0.65, 1)
        _InkColor ("Ink", Color) = (0.118, 0.133, 0.251, 1)
        [Header(Live (set by the game))]
        _Spin ("Roll angle", Float) = 0
        _Run ("Run speed -1..1 (sign = direction)", Range(-1, 1)) = 0
        _Stride ("Run cycle phase", Float) = 0
        _Tumble ("Tumble angle", Float) = 0
        _Sleep ("Asleep 0..1", Range(0, 1)) = 0
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
                float _R, _ShellTint, _Spin, _Run, _Stride, _Tumble, _Sleep;
                half4 _FurColor, _BellyColor, _PinkColor, _InkColor;
            CBUFFER_END

            void Hamster(float2 h, inout half3 c, float run, float stride, float sleep)
            {
                // h: hamster space, facing +x, ball centre at the origin
                float breathe = sin(_Time.y * 2.0) * 0.012 * sleep;
                float bob = abs(sin(stride)) * 0.04 * run;
                float2 b = h - float2(0, -0.38 + bob - sleep * 0.06);
                // paws (two pairs, opposite phase)
                [unroll] for (int k = 0; k < 2; k++)
                {
                    float ph = stride + k * 3.1416;
                    float2 paw = b - float2((k == 0 ? 0.22 : -0.18) + sin(ph) * 0.12 * run, -0.33 + max(0.0, cos(ph)) * 0.06 * run);
                    float d = Ellipse(paw, float2(0.1, 0.06));
                    Paint(c, _InkColor.rgb, AAInside(d - 0.03));
                    Paint(c, _PinkColor.rgb, AAInside(d));
                }
                // body
                float2 br = float2(0.5 + breathe, lerp(0.38, 0.33, sleep) + breathe);
                float body = Ellipse(b, br);
                Paint(c, _InkColor.rgb, AAInside(body - 0.035));
                Paint(c, _FurColor.rgb, AAInside(body));
                Paint(c, _BellyColor.rgb, AAInside(Ellipse(b - float2(0.16, -0.12), float2(0.3, 0.2))) * AAInside(body));
                // ears
                [unroll] for (int e = 0; e < 2; e++)
                {
                    float2 ep = b - float2(e == 0 ? 0.16 : -0.04, br.y * 0.86);
                    float ed = Circle(ep, 0.1);
                    Paint(c, _InkColor.rgb, AAInside(ed - 0.03));
                    Paint(c, _FurColor.rgb, AAInside(ed));
                    Paint(c, _PinkColor.rgb, AAInside(Circle(ep, 0.05)));
                }
                // face
                float2 eye = b - float2(0.3, 0.1);
                Paint(c, _InkColor.rgb, AAInside(Circle(eye, 0.055)) * (1.0 - sleep));
                Paint(c, half3(1, 1, 1), AAInside(Circle(eye - float2(0.018, 0.02), 0.018)) * (1.0 - sleep));
                Paint(c, _InkColor.rgb, AAInside(Segment(eye, float2(-0.05, 0), float2(0.05, 0)) - 0.016) * sleep);
                Paint(c, _PinkColor.rgb, AAInside(Circle(b - float2(0.5, 0.02), 0.04)));
                Paint(c, _PinkColor.rgb, AAInside(Circle(b - float2(0.3, -0.06), 0.07)) * 0.5);
            }

            half4 fragBody(ArrowVaryings i)
            {
                float2 p = (i.uv0 * 2.0 - 1.0) / _R;
                float r = length(p);
                float3 n = SphereNormal(p);
                half3 tint = i.color.rgb;
                half3 c = lerp(half3(0.97, 0.97, 0.98), tint, _ShellTint) * (0.9 + 0.1 * n.z);
                // vent lines on the far side of the shell, rolling with the ball
                float a = atan2(p.y, p.x) + _Spin;
                float vent = AAInside(abs(frac(a / 6.2832 * 10.0) - 0.5) * 0.6 - 0.02) * smoothstep(0.55, 0.85, r);
                Paint(c, tint * 0.75, vent * 0.5);
                // the hamster stays upright (gravity) and tumbles on hard landings
                float2 h = Rot2(p, -_Tumble);
                float dir = _Run < -0.02 ? -1.0 : 1.0;
                h.x *= dir;
                Hamster(h, c, saturate(abs(_Run) * 1.5), _Stride, _Sleep);
                c = GlassShade(p, c, tint, 0.85);
                return Out(c, AAInside(r - 1.0), i.color);
            }

            // inside a UI mask (scroll list, card), nothing draws outside it
            half4 frag(ArrowVaryings i) : SV_Target { return fragBody(i) * UIClip(i.local); }
            ENDHLSL
        }
    }
}
