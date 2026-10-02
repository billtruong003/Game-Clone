// Nah Blocks "Chrome": polished metal blocks, anodised in the Image colour. The studio light they reflect slides with your finger, and the dragged piece shows up as a dark reflection when it passes close.
// Interactive skin (Docs/SHADER_LAB.md). The game / lab drives the "Live" properties every frame; everything else is
// a look you can tune on the material asset (Assets/_Game/Skins). UI shader, premultiplied alpha, masks work.
Shader "CasualGame/Lab/SkinChromeBlock"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        [Header(Look)]
        _Tint ("Anodised tint strength", Range(0, 1)) = 0.7
        _Bevel ("Bevel width", Range(0.05, 0.4)) = 0.2
        _EnvTex ("Reflection (matcap, optional)", 2D) = "white" {}
        _UseEnv ("Use the matcap texture", Range(0, 1)) = 0
        _Sky ("Studio top", Color) = (0.95, 0.97, 1, 1)
        _Floor ("Studio bottom", Color) = (0.18, 0.2, 0.26, 1)
        _Strip ("Light strip brightness", Range(0, 2)) = 1.2
        [Header(Live (set by the game))]
        _Reflect ("Finger offset (x, y), -1..1", Vector) = (0, 0, 0, 0)
        _Ghost ("Dragged piece reflection 0..1", Range(0, 1)) = 0
        _GhostColor ("Dragged piece colour", Color) = (0.2, 0.2, 0.3, 1)
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

            TEXTURE2D(_EnvTex); SAMPLER(sampler_EnvTex);
            CBUFFER_START(UnityPerMaterial)
                float _Tint, _Bevel, _UseEnv, _Strip, _Ghost;
                half4 _Sky, _Floor, _GhostColor;
                float4 _Reflect;
            CBUFFER_END

            half3 Studio(float2 v)
            {
                if (_UseEnv > 0.5) return SAMPLE_TEXTURE2D(_EnvTex, sampler_EnvTex, saturate(v * 0.5 + 0.5)).rgb;
                half3 c = lerp(_Floor.rgb, _Sky.rgb, smoothstep(-0.6, 0.7, v.y));
                c += smoothstep(0.09, 0.0, abs(v.y - 0.3 - v.x * 0.15)) * _Strip;
                c += smoothstep(0.05, 0.0, abs(v.y + 0.25 + v.x * 0.1)) * _Strip * 0.5;
                return c;
            }

            half4 frag(ArrowVaryings i) : SV_Target
            {
                float2 p = i.uv0 * 2.0 - 1.0;
                float body = RoundBox(p, float2(0.94, 0.94), 0.1);
                float inner = 0.94 - _Bevel;
                float2 ap = abs(p);
                float2 nrm = 0;
                if (ap.x > inner || ap.y > inner) nrm = ap.x > ap.y ? float2(sign(p.x), 0) : float2(0, sign(p.y));
                float2 v = p * 0.35 + nrm * 0.8 - _Reflect.xy * 0.7;
                half3 c = Studio(v);
                // the dragged piece reflected as a dark rounded shape
                float ghost = AAInside(RoundBox(p + _Reflect.xy * 1.6, float2(0.5, 0.5), 0.2) - 0.1) * _Ghost;
                Paint(c, _GhostColor.rgb * 0.6, ghost * 0.85);
                c = lerp(c, c * i.color.rgb * 1.35, _Tint);
                Paint(c, half3(1, 1, 1), AAInside(abs(max(ap.x, ap.y) - inner) - 0.012) * 0.35);
                Paint(c, INK, 1.0 - AAInside(body + 0.05));
                return Out(c, AAInside(body), i.color);
            }

            ENDHLSL
        }
    }
}
