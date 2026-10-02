// Nah Blocks "Night City": every block is a window (frame = Image colour) looking out on ONE city behind the board. The skyline layers are drawn in screen space and slide at different depths with the finger (_Parallax), so the board feels like glass over a deep scene. City windows light up as the block's row fills (_Lit).
// Interactive skin (Docs/SHADER_LAB.md). The game / lab drives the "Live" properties every frame; everything else is
// a look you can tune on the material asset (Assets/_Game/Skins). UI shader, premultiplied alpha, masks work.
Shader "CasualGame/Lab/SkinBuildingBlock"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        [Header(Look)]
        _FarTex ("Far skyline (tiles sideways)", 2D) = "white" {}
        _MidTex ("Buildings (tiles sideways)", 2D) = "white" {}
        _WinTex ("Building windows (grey = when it lights)", 2D) = "black" {}
        _SkyTop ("Sky top", Color) = (0.05, 0.06, 0.18, 1)
        _SkyBottom ("Sky at the horizon", Color) = (0.25, 0.2, 0.45, 1)
        _LightColor ("Window light", Color) = (1, 0.84, 0.45, 1)
        _MoonColor ("Moon", Color) = (1, 0.96, 0.82, 1)
        _CityScale ("City size (bigger = smaller buildings)", Range(0.5, 4)) = 1.5
        _Horizon ("Street level (0 bottom .. 1 top of screen)", Range(0, 1)) = 0.27
        _Depth ("Parallax strength", Range(0, 0.4)) = 0.12
        _Frame ("Window frame width", Range(0.04, 0.3)) = 0.12
        _GlassTint ("Glass takes the block colour", Range(0, 0.6)) = 0.12
        [Header(Live (set by the game))]
        _Parallax ("Finger offset (x, y), -1..1", Vector) = (0, 0, 0, 0)
        _Lit ("Share of windows lit 0..1", Range(0, 1)) = 0.3
        _Clear ("Row clear 0..1 (flash, then dark)", Range(0, 1)) = 0
        _Seed ("Seed", Float) = 0
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

            TEXTURE2D(_FarTex); SAMPLER(sampler_FarTex);
            TEXTURE2D(_MidTex); SAMPLER(sampler_MidTex);
            TEXTURE2D(_WinTex); SAMPLER(sampler_WinTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _SkyTop, _SkyBottom, _LightColor, _MoonColor;
                float _CityScale, _Horizon, _Depth, _Frame, _GlassTint, _Lit, _Clear, _Seed;
                float4 _Parallax;
            CBUFFER_END

            half4 frag(ArrowVaryings i) : SV_Target
            {
                float2 p = i.uv0 * 2.0 - 1.0;
                float body = RoundBox(p, float2(0.95, 0.95), 0.08);
                float2 s = ScreenPx(i.screen) / _ScreenParams.y;               // screen, height = 1
                float2 par = _Parallax.xy * _Depth;
                // sky, stars, moon (farthest: barely move)
                half3 c = lerp(_SkyBottom.rgb, _SkyTop.rgb, saturate((s.y - _Horizon) / 0.7));
                float2 sp = s + par * 0.1;
                float star = step(0.992, Hash21(floor(sp * 160.0))) * (0.6 + 0.4 * sin(_Time.y * 3.0 + Hash21(floor(sp * 160.0)) * 40.0));
                c += star * 0.8;
                float2 moon = float2(_ScreenParams.x / _ScreenParams.y * 0.66, _Horizon + 0.36) - par * 0.15;
                Paint(c, _MoonColor.rgb, AAInside(Circle(sp - moon, 0.06)));
                c += _MoonColor.rgb * exp(-max(length(sp - moon) - 0.06, 0.0) * 18.0) * 0.25;
                // far skyline (4:1 texture)
                float2 fuv = (s - float2(0, _Horizon + 0.04)) * _CityScale * float2(0.7, 2.8) - par * 0.4;
                if (fuv.y < 1)
                {
                    half4 f = SAMPLE_TEXTURE2D(_FarTex, sampler_FarTex, float2(fuv.x, saturate(fuv.y)));
                    c = lerp(c, f.rgb, fuv.y < 0 ? 1.0 : f.a);
                }
                // buildings with their windows (2:1 textures)
                float2 muv = (s - float2(0, _Horizon - 0.1)) * _CityScale * float2(1.0, 2.0) - par;
                if (muv.y < 1)
                {
                    float2 mm = float2(muv.x + _Seed * 0.0, max(muv.y, 0.02));
                    half4 m = SAMPLE_TEXTURE2D(_MidTex, sampler_MidTex, mm);
                    c = lerp(c, m.rgb, m.a);
                    half4 w = SAMPLE_TEXTURE2D(_WinTex, sampler_WinTex, mm);
                    float flash = smoothstep(0.0, 0.25, _Clear) * (1.0 - smoothstep(0.35, 0.6, _Clear));
                    float on = saturate(step(w.r, _Lit) * (1.0 - smoothstep(0.4, 0.7, _Clear)) + flash);
                    Paint(c, _LightColor.rgb * (0.9 + 0.1 * w.r), w.a * m.a * on);
                }
                // the window we look through: glass tint and a streak, then the frame in the block colour
                c = lerp(c, i.color.rgb, _GlassTint);
                c += smoothstep(0.08, 0.0, abs(p.x + p.y * 0.8 - 0.55)) * 0.12;
                float inner = RoundBox(p, float2(0.95 - _Frame, 0.95 - _Frame), 0.05);
                float mull = min(abs(p.x), abs(p.y)) - _Frame * 0.3;
                float frame = saturate(AAInside(-inner) + AAInside(mull));
                half3 fc = i.color.rgb * lerp(0.8, 1.12, saturate(p.y * 0.5 + 0.5));
                Paint(c, fc, frame);
                Paint(c, INK, AAInside(abs(inner) - 0.02) * 0.5);
                Paint(c, INK, 1.0 - AAInside(body + 0.05));
                return Out(c, AAInside(body), i.color);
            }

            ENDHLSL
        }
    }
}
