// Nah Blocks "Watchers": soft blocks (Image colour) with big eyes that follow your finger. Blocks right under the dragged piece go wide-eyed; a cleared row squeezes its eyes shut.
// Interactive skin (Docs/SHADER_LAB.md). The game / lab drives the "Live" properties every frame; everything else is
// a look you can tune on the material asset (Assets/_Game/Skins). UI shader, premultiplied alpha, masks work.
Shader "CasualGame/Lab/SkinWatchBlock"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        [Header(Look)]
        _EyeWhite ("Eye white", Color) = (1, 1, 1, 1)
        _InkColor ("Ink", Color) = (0.118, 0.133, 0.251, 1)
        _EyeSize ("Eye size", Range(0.6, 1.4)) = 1
        _PupilSize ("Pupil size", Range(0.4, 1.6)) = 1
        _Corner ("Corner roundness", Range(0.05, 0.5)) = 0.28
        [Header(Live (set by the game))]
        _Look ("Look direction (x, y)", Vector) = (0, 0, 0, 0)
        _Alarm ("Alarmed (piece right above) 0..1", Range(0, 1)) = 0
        _Blink ("Blink / squeeze 0..1", Range(0, 1)) = 0
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
                half4 _EyeWhite, _InkColor;
                float _EyeSize, _PupilSize, _Corner, _Alarm, _Blink;
                float4 _Look;
            CBUFFER_END

            half4 frag(ArrowVaryings i) : SV_Target
            {
                float2 p = i.uv0 * 2.0 - 1.0;
                half3 base = i.color.rgb;
                float body = RoundBox(p, float2(0.92, 0.92), _Corner);
                half3 c = base * lerp(0.92, 1.1, smoothstep(-0.6, 0.9, p.y));
                c = lerp(c, half3(1, 1, 1), smoothstep(0.2, 0.0, abs(p.x + 0.62)) * 0.15 * step(p.y, 0.6));
                float2 look = clamp(_Look.xy, -1.0, 1.0);
                float2 eyeR = float2(0.27, lerp(0.27, 0.33, _Alarm)) * _EyeSize;
                [unroll] for (int k = 0; k < 2; k++)
                {
                    float2 e = p - float2(k == 0 ? -0.38 : 0.38, 0.1);
                    float d = Ellipse(e, eyeR);
                    float white = AAInside(d);
                    Paint(c, _InkColor.rgb, AAInside(d - 0.05));
                    Paint(c, _EyeWhite.rgb, white);
                    float pr = eyeR.x * 0.5 * _PupilSize * lerp(1.0, 0.55, _Alarm);
                    Paint(c, _InkColor.rgb, AAInside(Circle(e - look * eyeR * 0.45, pr)) * white);
                    Paint(c, half3(1, 1, 1), AAInside(Circle(e - look * eyeR * 0.45 - float2(pr * 0.35, pr * 0.35), pr * 0.3)) * white);
                    float lidLine = eyeR.y * (1.0 - 2.0 * _Blink);
                    Paint(c, base * 0.88, AAInside(lidLine - e.y) * white * step(0.02, _Blink));
                    Paint(c, _InkColor.rgb, AAInside(abs(e.y - lidLine) - 0.03) * white * step(0.02, _Blink));
                }
                // mouth: a line, a small "o" when alarmed
                float2 m = p - float2(0, -0.5);
                Paint(c, _InkColor.rgb, AAInside(Segment(m, float2(-0.12, 0), float2(0.12, 0)) - 0.035) * (1.0 - step(0.5, _Alarm)));
                Paint(c, _InkColor.rgb, AAInside(abs(Ellipse(m, float2(0.09, 0.11))) - 0.03) * step(0.5, _Alarm));
                Paint(c, _InkColor.rgb, 1.0 - AAInside(body + 0.06));
                return Out(c, AAInside(body), i.color);
            }

            ENDHLSL
        }
    }
}
