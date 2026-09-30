// Character faces: one Texture2DArray holds every expression (Assets/_Game/Art/Faces/faces.png, 3×3 flipbook).
// The slice comes from TEXCOORD1.x, written per quad by FaceGraphic (UI) or baked into FaceQuad's shared meshes (world),
// so every face on screen shares this one material and batches together. Vertex colour × _Color tints / fades.
Shader "CasualGame/FaceArray"
{
    Properties
    {
        [HideInInspector] _MainTex ("Unused (UI/sprite slot)", 2D) = "white" {}
        _Faces ("Faces", 2DArray) = "" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_ARRAY(_Faces);
            SAMPLER(sampler_Faces);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
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
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color * _Color;
                o.uv = float3(v.uv, v.uv1.x);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D_ARRAY(_Faces, sampler_Faces, i.uv.xy, i.uv.z);
                return c * i.color;
            }
            ENDHLSL
        }
    }
}
