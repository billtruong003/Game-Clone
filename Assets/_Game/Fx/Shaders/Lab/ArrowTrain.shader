// Bruh Arrows "Train": the arrow is a little train (top view, sprites) of its colour on its track: the engine leads with a signal lamp on its cab (green = the way is clear, red = blocked), the cars bunch up when it bumps, smoke puffs when it leaves. Needs GlowPad 0.22.
// Interactive skin (Docs/SHADER_LAB.md). The game / lab drives the "Live" properties every frame; everything else is
// a look you can tune on the material asset (Assets/_Game/Skins). UI shader, premultiplied alpha, masks work.
Shader "CasualGame/Lab/ArrowTrain"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        [Header(Look)]
        _TrainTex ("Train sprites (engine | car; top row tinted, bottom row details)", 2D) = "white" {}
        _CarLength ("Car length (cells)", Range(0.3, 1)) = 0.6
        _EngineLength ("Engine length (cells)", Range(0.4, 1.2)) = 0.75
        _TrainWidth ("Train half width (cells)", Range(0.12, 0.3)) = 0.22
        _RailColor ("Rails", Color) = (0.45, 0.45, 0.5, 1)
        _SleeperColor ("Sleepers", Color) = (0.55, 0.4, 0.28, 1)
        _GoColor ("Signal: clear", Color) = (0.25, 0.95, 0.4, 1)
        _StopColor ("Signal: blocked", Color) = (1, 0.25, 0.25, 1)
        _SmokeColor ("Smoke", Color) = (1, 1, 1, 1)
        [Header(Live (set by the game))]
        _Len ("Arrow length (cells, tail to tip)", Float) = 3
        _Free ("Way is clear 0..1", Range(0, 1)) = 1
        _Bump ("Bump 0..1", Range(0, 1)) = 0
        _Go ("Leaving 0..1", Range(0, 1)) = 0
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

            TEXTURE2D(_TrainTex); SAMPLER(sampler_TrainTex);
            CBUFFER_START(UnityPerMaterial)
                float _CarLength, _EngineLength, _TrainWidth, _Len, _Free, _Bump, _Go;
                half4 _RailColor, _SleeperColor, _GoColor, _StopColor, _SmokeColor;
            CBUFFER_END

            #define HALF_LINE 0.104

            half4 frag(ArrowVaryings i) : SV_Target
            {
                if (i.uv1.y > 0.5) return 0;                                  // no chevron: the engine shows the way
                float X = i.uv0.x, Y = i.uv0.y * HALF_LINE;
                half3 col = i.color.rgb;
                float d = _Len - X;                                           // distance back from the nose
                half3 c = 0; float a = 0;
                // track under the train
                float sleeper = AAInside(abs(frac(X / 0.2) - 0.5) * 0.2 - 0.035) * AAInside(abs(Y) - 0.27);
                float rail = AAInside(abs(abs(Y) - 0.15) - 0.018);
                Paint(c, _SleeperColor.rgb, sleeper); a = max(a, sleeper);
                Paint(c, _RailColor.rgb, rail); a = max(a, rail);
                // engine, then whole cars (a bump squeezes the gaps)
                float squeeze = 1.0 - 0.25 * _Bump;
                float engineL = min(_EngineLength, _Len);
                float carD, carL, cell;
                if (d < engineL) { carD = d; carL = engineL; cell = 0; }
                else
                {
                    float cars = max(1.0, round((_Len - engineL) / (_CarLength + 0.08)));
                    float pitch = (_Len - engineL) / cars * squeeze;
                    float k = floor((d - engineL) / pitch);
                    carD = d - engineL - k * pitch - 0.08 * squeeze;
                    carL = pitch - 0.08 * squeeze;
                    cell = 1;
                }
                float2 uv = float2(1.0 - carD / carL, Y / _TrainWidth * 0.5 + 0.5);
                if (all(uv > 0) && all(uv < 1) && X > -0.2 && d > 0)
                {
                    float ux = (cell + clamp(uv.x, 0.004, 0.996)) / 2.0;
                    half4 base = SAMPLE_TEXTURE2D(_TrainTex, sampler_TrainTex, float2(ux, 0.5 + uv.y * 0.5));
                    half4 det = SAMPLE_TEXTURE2D(_TrainTex, sampler_TrainTex, float2(ux, uv.y * 0.5));
                    half3 tc = base.rgb * col;
                    tc = lerp(tc, det.rgb, det.a);
                    float ta = max(base.a, det.a);
                    if (cell < 0.5)                                           // signal lamp on the cab roof
                    {
                        float2 lp = float2(carD - engineL * 0.8, Y);
                        half3 lamp = lerp(_StopColor.rgb, _GoColor.rgb, _Free);
                        Paint(tc, INK, AAInside(Circle(lp, 0.075)));
                        Paint(tc, lamp, AAInside(Circle(lp, 0.055)));
                        Paint(tc, half3(1, 1, 1), AAInside(Circle(lp - float2(0.015, 0.02), 0.018)) * 0.8);
                    }
                    c = lerp(c, tc, ta); a = max(a, ta);
                }
                // lamp glow
                float glow = exp(-length(float2(d - engineL * 0.8, Y)) * 9.0) * 0.5 * (1.0 - a);
                c += lerp(_StopColor.rgb, _GoColor.rgb, _Free) * glow; a = max(a, glow);
                // smoke puffs drift back over the train while it leaves
                [unroll] for (int s = 0; s < 4; s++)
                {
                    float age = frac(_Time.y * 1.6 + s * 0.25);
                    float2 spos = float2(engineL * 0.35 + age * 1.1, sin(age * 6.0 + s) * 0.06);
                    float pr = 0.05 + age * 0.12;
                    float puff = AAInside(Circle(float2(d, Y) - spos, pr)) * (1.0 - age) * _Go;
                    Paint(c, _SmokeColor.rgb, puff); a = max(a, puff * 0.9);
                }
                return half4(c * a * i.color.a, a * i.color.a);
            }

            ENDHLSL
        }
    }
}
