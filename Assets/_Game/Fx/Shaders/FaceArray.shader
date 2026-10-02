// Character faces: one Texture2DArray holds every expression (Assets/_Game/Art/Faces/faces.png, 3×3 flipbook).
// The slice comes from TEXCOORD1.x, written per quad by FaceGraphic (UI) or baked into FaceQuad's shared meshes (world),
// so every face on screen shares this one material and batches together. Vertex colour × _Color tints / fades.
// TEXCOORD1.y = 1 redraws the ink in cream, for faces sitting on a dark body (a navy arrow tail): the tears and
// sweat keep their colour because only dark pixels are swapped.
Shader "CasualGame/FaceArray"
{
    Properties
    {
        [HideInInspector] _MainTex ("Unused (UI/sprite slot)", 2D) = "white" {}
        _Faces ("Faces", 2DArray) = "" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _UseGlobalPack ("Follow the worn face pack", Float) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma require 2darray
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_ARRAY(_Faces);
            SAMPLER(sampler_Faces);
            // the worn shop face pack, set globally (FacePacks.Apply); _FacePackOn = 0 keeps the default faces
            TEXTURE2D_ARRAY(_FacePack);
            float _FacePackOn;
            // UI masks: Unity sets these on masked faces (scroll lists)
            float4 _ClipRect;
            float _UIMaskSoftnessX, _UIMaskSoftnessY;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _UseGlobalPack;
                float4 _MainTex_ST;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float3 uv : TEXCOORD0;
                half lightInk : TEXCOORD1;
                float2 local : TEXCOORD2;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color * _Color;
                o.uv = float3(v.uv, v.uv1.x);
                o.lightInk = v.uv1.y;
                o.local = v.positionOS.xy;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 c = _FacePackOn * _UseGlobalPack > 0.5
                    ? SAMPLE_TEXTURE2D_ARRAY(_FacePack, sampler_Faces, i.uv.xy, i.uv.z)
                    : SAMPLE_TEXTURE2D_ARRAY(_Faces, sampler_Faces, i.uv.xy, i.uv.z);
                half ink = saturate((0.45h - dot(c.rgb, half3(0.2126h, 0.7152h, 0.0722h))) / 0.3h);
                c.rgb = lerp(c.rgb, half3(1.0h, 0.973h, 0.925h), ink * i.lightInk);
                c *= i.color;
            #ifdef UNITY_UI_CLIP_RECT
                float2 soft = max(float2(_UIMaskSoftnessX, _UIMaskSoftnessY), 1.0);
                float2 m = saturate(min(i.local - _ClipRect.xy, _ClipRect.zw - i.local) / soft);
                c.a *= m.x * m.y;
            #endif
                return c;
            }
            ENDHLSL
        }
    }
}
