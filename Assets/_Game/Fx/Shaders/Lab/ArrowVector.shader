// Vector CRT (oscilloscope / arcade vector monitor): a thin phosphor beam with a bright core and a soft bloom, the beam spot racing ahead while the line is drawn (no blinking), a faint afterglow along the line and slight beam jitter. Needs GlowPad > 0. _TailClip keeps the line out of the tail cap.
// Theme lab (Docs/SHADER_LAB.md). UI shader: works on a uGUI Graphic, masks included. Output is premultiplied alpha.
Shader "CasualGame/Lab/ArrowVector"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        _Reveal ("Drawn (0 tail .. 1 head)", Range(0, 1)) = 1
        _Beam ("Beam width (fraction of the line)", Range(0.1, 1)) = 0.45
        _Glow ("Bloom", Range(0, 2)) = 1
        _TailClip ("Line starts this far from the tail (cells)", Float) = 0
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
                float _Reveal, _Beam, _Glow, _TailClip;
            CBUFFER_END

            half4 fragBody(ArrowVaryings i)
            {
                float across = abs(i.uv0.y), t = i.uv1.x;
                float reveal = saturate((_Reveal + 0.02 - t) * 50.0);
                float jitter = (ValueNoise(float2(i.uv0.x * 3.0, _Time.y * 30.0)) - 0.5) * 0.12;
                float d = abs(i.uv0.y + jitter);
                float core = saturate(1.0 - d / (_Beam * 0.5));
                float beam = AAInside(d - _Beam);
                float bloom = exp(-max(d - _Beam, 0.0) * 2.6) * _Glow;
                // the spot at the drawing front: brighter, a little wider, only while drawing
                float drawing = step(_Reveal, 0.995);
                float spot = drawing * exp(-abs(t - _Reveal) * 60.0) * exp(-d * 1.5) * 1.5;
                float afterglow = 0.85 + 0.15 * sin(t * 6.0 - _Time.y * 3.0);
                float clip = saturate((i.uv0.x - _TailClip) * 20.0);
                float e = (beam * (0.75 + 0.6 * core) + bloom * 0.55 + spot) * afterglow * reveal * clip * i.color.a;
                half3 c = lerp(i.color.rgb, half3(1, 1, 1), core * 0.6 + spot * 0.4);
                return half4(c * e, saturate(e) * 0.75);
            }

            // inside a UI mask (scroll list, card), nothing draws outside it
            half4 frag(ArrowVaryings i) : SV_Target { return fragBody(i) * UIClip(i.local); }
            ENDHLSL
        }
    }
}
