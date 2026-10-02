// Meh Merge "Hungry": a plain ball with big eyes and a mouth. Balls of the held ball's tier look up at it and open wide; the others look away and sulk. Body colour = the Image colour (the tier).
// Interactive skin (Docs/SHADER_LAB.md). The game / lab drives the "Live" properties every frame; everything else is
// a look you can tune on the material asset (Assets/_Game/Skins). UI shader, premultiplied alpha, masks work.
Shader "CasualGame/Lab/SkinHungryBall"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        [Header(Look)]
        _R ("Ball radius in the quad", Range(0.3, 1)) = 0.94
        _EyeWhite ("Eye white", Color) = (1, 1, 1, 1)
        _InkColor ("Ink (pupils, outline)", Color) = (0.118, 0.133, 0.251, 1)
        _MouthColor ("Inside of the mouth", Color) = (0.35, 0.08, 0.14, 1)
        _TongueColor ("Tongue", Color) = (1, 0.48, 0.55, 1)
        _BlushColor ("Blush when hungry", Color) = (1, 0.45, 0.55, 1)
        _EyeSize ("Eye size", Range(0.6, 1.6)) = 1
        [Header(Live (set by the game))]
        _Look ("Look direction (x, y)", Vector) = (0, 0, 0, 0)
        _Mouth ("Mouth open 0..1", Range(0, 1)) = 0
        _Sulk ("Sulk (half-closed eyes) 0..1", Range(0, 1)) = 0
        _Blink ("Blink 0..1", Range(0, 1)) = 0
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
                float _R, _EyeSize, _Mouth, _Sulk, _Blink;
                half4 _EyeWhite, _InkColor, _MouthColor, _TongueColor, _BlushColor;
                float4 _Look;
            CBUFFER_END

            half4 fragBody(ArrowVaryings i)
            {
                float2 p = (i.uv0 * 2.0 - 1.0) / _R;
                float r = length(p);
                half3 body = i.color.rgb;
                half3 c = body;
                float2 look = clamp(_Look.xy, -1.0, 1.0);
                float open = _Mouth;
                float2 f = p - look * 0.13;                                  // the face turns toward what it looks at
                // blush
                [unroll] for (int b = 0; b < 2; b++)
                    Paint(c, _BlushColor.rgb, AAInside(Circle(f - float2(b == 0 ? -0.52 : 0.52, -0.16), 0.13)) * 0.45 * open);
                // eyes: white, ink ring, pupil, lids
                float2 eyeR = float2(0.17, lerp(0.17, 0.22, open)) * _EyeSize;
                float lid = max(_Blink, _Sulk * 0.5);
                [unroll] for (int k = 0; k < 2; k++)
                {
                    float2 e = f - float2(k == 0 ? -0.32 : 0.32, 0.2);
                    float d = Ellipse(e, eyeR);
                    float white = AAInside(d);
                    Paint(c, _InkColor.rgb, AAInside(d - 0.035));
                    Paint(c, _EyeWhite.rgb, white);
                    float pr = eyeR.x * lerp(0.55, 0.42, open);
                    Paint(c, _InkColor.rgb, AAInside(Circle(e - look * eyeR * 0.42, pr)) * white);
                    float lidLine = eyeR.y * (1.0 - 2.0 * lid);
                    float cover = AAInside(lidLine - e.y) * white;
                    Paint(c, body * 0.9, cover * step(0.02, lid));
                    Paint(c, _InkColor.rgb, AAInside(abs(e.y - lidLine) - 0.018) * white * step(0.02, lid));
                }
                // mouth: a flat line when closed, a round open mouth with a tongue when hungry
                float2 m = f - float2(0, -0.34 - open * 0.06);
                float2 mr = float2(lerp(0.13, 0.25, open), lerp(0.02, 0.26, open));
                float md = Ellipse(m, mr);
                float mouth = AAInside(md);
                Paint(c, _InkColor.rgb, AAInside(md - 0.035) * step(0.08, open));
                Paint(c, _MouthColor.rgb, mouth * step(0.08, open));
                float tongue = AAInside(Ellipse(m - float2(0, -mr.y * 0.62), float2(mr.x * 0.68, mr.y * 0.5))) * mouth;
                Paint(c, _TongueColor.rgb, tongue * step(0.08, open));
                Paint(c, _InkColor.rgb, AAInside(Segment(m, float2(-0.13, 0), float2(0.13, 0)) - 0.025) * (1.0 - step(0.08, open)));
                c = lerp(body, c, AAInside(r - 0.9));                       // the face stays on the ball
                c = BallShade(p, c, 1.0);
                return Out(c, AAInside(r - 1.0), i.color);
            }

            // inside a UI mask (scroll list, card), nothing draws outside it
            half4 frag(ArrowVaryings i) : SV_Target { return fragBody(i) * UIClip(i.local); }
            ENDHLSL
        }
    }
}
