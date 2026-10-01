// Neon tube: white-hot core, saturated tube, wide soft glow, slow breathing. Needs GlowPad > 0.
// Theme lab (Docs/SHADER_LAB.md). UI shader: works on a uGUI Graphic, masks included. Output is premultiplied alpha.
Shader "CasualGame/Lab/ArrowNeon"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        _Reveal ("Drawn (0 tail .. 1 head)", Range(0, 1)) = 1
        _Glow ("Glow", Range(0, 2)) = 1
        _Pulse ("Pulse", Range(0, 0.5)) = 0.12
        _TailClip ("Line starts this far from the tail (cells): the cap's radius", Float) = 0
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
                float _Reveal, _Glow, _Pulse, _TailClip;
            CBUFFER_END

            half4 frag(ArrowVaryings i) : SV_Target
            {
                float across = abs(i.uv0.y), t = i.uv1.x;
                float reveal = saturate((_Reveal + 0.02 - t) * 50.0);   // drawn up to the brush front (the head at t = 1 included)
                float tube = AAInside(across - 0.75);
                float core = saturate(1.0 - across / 0.35);
                float breathe = (1.0 - _Pulse + _Pulse * sin(_Time.y * 2.4 + i.uv0.x * 0.4)) * saturate((i.uv0.x - _TailClip) * 20.0);
                float halo = exp(-max(across - 0.6, 0.0) * 1.8) * (1.0 - tube) * _Glow;
                half3 c = lerp(i.color.rgb, half3(1, 1, 1), core * core) * tube + i.color.rgb * halo;
                float a = saturate(tube + halo * 0.6) * reveal * breathe * i.color.a;
                return half4(c * reveal * breathe * i.color.a, a * 0.85);
            }
            ENDHLSL
        }
    }
}
