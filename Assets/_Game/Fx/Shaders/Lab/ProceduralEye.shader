// Procedural eye (ref: distance-based iris / pupil): sclera with a soft shaded rim, iris as a lerp of two colours over the distance, pupil (round or slit), a highlight, eyelids closing with _Blink. Put it on a square Image; _Look moves the iris (-1..1).
// Theme lab (Docs/SHADER_LAB.md). UI shader: works on a uGUI Graphic, masks included. Output is premultiplied alpha.
Shader "CasualGame/Lab/ProceduralEye"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        _IrisOut ("Iris outer", Color) = (0.1, 0.35, 0.8, 1)
        _IrisIn ("Iris inner", Color) = (0.4, 0.85, 1, 1)
        _Sclera ("Sclera", Color) = (1, 0.97, 0.92, 1)
        _Lid ("Lid colour", Color) = (0.95, 0.45, 0.5, 1)
        _IrisR ("Iris radius", Range(0.1, 0.45)) = 0.26
        _PupilR ("Pupil radius", Range(0.02, 0.3)) = 0.11
        _Slit ("Slit pupil", Range(0, 1)) = 0
        _Look ("Look direction", Vector) = (0, 0, 0, 0)
        _Blink ("Blink (0 open .. 1 shut)", Range(0, 1)) = 0
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
                half4 _IrisOut, _IrisIn, _Sclera, _Lid;
                float _IrisR, _PupilR, _Slit, _Blink;
                float4 _Look;
            CBUFFER_END

            half4 frag(ArrowVaryings i) : SV_Target
            {
                float2 p = i.uv0 * 2.0 - 1.0;                                  // -1..1 over the Image
                float r = length(p);
                float ball = AAInside(r - 0.94);
                float outline = ball * (1.0 - AAInside(r - 0.86));
                float2 irisC = _Look.xy * 0.35;
                float d = length(p - irisC) / _IrisR;
                float iris = AAInside(d - 1.0);
                float2 pp = (p - irisC) / _PupilR;
                pp.x *= lerp(1.0, 3.5, _Slit);
                float pupil = AAInside(length(pp) - 1.0);
                half3 irisCol = lerp(_IrisIn.rgb, _IrisOut.rgb, saturate(d)) * (0.85 + 0.15 * ValueNoise(float2(atan2(p.y - irisC.y, p.x - irisC.x) * 8.0, d * 3.0)));
                half3 c = _Sclera.rgb * (1.0 - 0.25 * smoothstep(0.4, 0.94, r));   // shaded sclera edge
                c = lerp(c, irisCol, iris);
                c = lerp(c, half3(0.05, 0.05, 0.08), pupil);
                // lids: top and bottom close towards the middle
                float lidLine = 1.0 - _Blink;
                float lid = 1.0 - AAInside(abs(p.y) - lidLine * sqrt(saturate(1.0 - p.x * p.x)) * 0.94);
                c = lerp(c, _Lid.rgb, lid);
                c = lerp(c, half3(0.118, 0.133, 0.251), outline);
                float a = ball * i.color.a;
                return half4(c * a, a);
            }
            ENDHLSL
        }
    }
}
