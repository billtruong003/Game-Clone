// Two balls melting into each other like water drops: a smooth union of two circles, drawn to match the ball sprites
// (flat fill, inner navy outline, white highlight ellipse). All parameters are in world units, set by GooMerge.cs.
Shader "CasualGame/GooMerge"
{
    Properties
    {
        _A ("Ball A (xy center, z radius)", Vector) = (-0.5, 0, 0.5, 0)
        _B ("Ball B (xy center, z radius)", Vector) = (0.5, 0, 0.5, 0)
        _ColorA ("Color A", Color) = (1, 0.35, 0.37, 1)
        _ColorB ("Color B", Color) = (1, 0.35, 0.37, 1)
        _K ("Blend (world units)", Float) = 0.2
        _Outline ("Outline width (fraction of radius)", Float) = 0.098
        _OutlineColor ("Outline color", Color) = (0.118, 0.133, 0.251, 1)
        _Flash ("White flash", Range(0, 1)) = 0
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _A, _B;
                half4 _ColorA, _ColorB, _OutlineColor;
                float _K, _Outline, _Flash;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 world : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 w = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(w);
                o.world = w.xy;
                return o;
            }

            // polynomial smooth minimum: the "neck" between the drops grows with k
            float SMin(float a, float b, float k, out float h)
            {
                h = saturate(0.5 + 0.5 * (b - a) / max(k, 1e-5)); // 1 = a dominates
                return lerp(b, a, h) - k * h * (1.0 - h);
            }

            // the ball sprite's highlight: a tilted ellipse up-left of the center, 55% white
            float Highlight(float2 p, float3 ball, float aa)
            {
                float2 q = (p - ball.xy) / (2.0 * ball.z * 1.049) - float2(-0.172, 0.219); // sprite space (diameter = 1)
                float s = sin(0.61), c = cos(0.61);                                         // 35 degrees
                q = float2(c * q.x + s * q.y, -s * q.x + c * q.y);
                float d = length(q / float2(0.117, 0.07)) - 1.0;
                return (1.0 - smoothstep(-aa * 12.0, aa * 12.0, d)) * 0.55;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 p = i.world;
                float dA = length(p - _A.xy) - _A.z;
                float dB = length(p - _B.xy) - _B.z;
                float h;
                float d = SMin(dA, dB, _K, h);
                float aa = max(fwidth(d), 1e-5);

                float inside = 1.0 - smoothstep(-aa, aa, d);
                if (inside <= 0.0) discard;

                float r = lerp(_B.z, _A.z, h);
                float ow = _Outline * r;
                float fill = 1.0 - smoothstep(-ow - aa, -ow + aa, d);

                half3 col = lerp(_ColorB.rgb, _ColorA.rgb, h);
                // both lobes keep their own highlight; they slide into one as the centers meet (no hard switch)
                float hl = max(Highlight(p, _A.xyz, aa), Highlight(p, _B.xyz, aa));
                col = lerp(col, half3(1, 1, 1), hl * fill);
                col = lerp(_OutlineColor.rgb, col, fill);
                col = lerp(col, half3(1, 1, 1), _Flash);
                return half4(col, inside);
            }
            ENDHLSL
        }
    }
}
