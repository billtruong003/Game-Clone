// Ink brush stroke for trails (Arrow Out): bristle streaks running along the stroke, ragged edges, ink pooling darker
// at the edges, and the brush drying out toward the tail (bristles drop out one by one). For a TrailRenderer /
// LineRenderer with Texture Mode = Stretch: uv.x runs along the stroke, uv.y across it. Color = vertex color.
Shader "CasualGame/InkBrush"
{
    Properties
    {
        _Length ("Stroke length (world, for bristle scale)", Float) = 4
        _Bristles ("Bristles across the stroke", Float) = 14
        _StreakLength ("Streak length (world)", Float) = 0.9
        _DryStart ("Dry-out starts at (0 head .. 1 tail)", Range(0, 1)) = 0.35
        _DryAmount ("Dry-out amount", Range(0, 1.5)) = 1.05
        _Ragged ("Ragged edges", Range(0, 0.6)) = 0.22
        _EdgeDark ("Edge ink (darker)", Range(0, 1)) = 0.35
        _EdgeWidth ("Edge ink width", Range(0, 0.5)) = 0.18
        [Toggle] _HeadAtZero ("Head at uv.x = 0", Float) = 1
        _Seed ("Seed", Float) = 0
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
                float _Length, _Bristles, _StreakLength, _DryStart, _DryAmount, _Ragged, _EdgeDark, _EdgeWidth, _HeadAtZero, _Seed;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _HeadAtZero > 0.5 ? i.uv.x : 1.0 - i.uv.x;   // 0 = head, 1 = tail
                float y = i.uv.y * 2.0 - 1.0;                             // -1..1 across
                float along = t * _Length / max(_StreakLength, 0.01);

                // ragged silhouette: the brush edge wanders a little
                float edge = 1.0 - _Ragged * Noise(float2(along * 3.0 + _Seed, y > 0 ? 7.3 : 1.9));
                float ay = abs(y);
                float aa = max(fwidth(ay), 1e-4);
                float body = 1.0 - smoothstep(edge - aa, edge + aa, ay);

                // bristles: long streaks along the stroke, narrow across it
                float b = Noise(float2(along + _Seed * 3.1, (y * 0.5 + 0.5) * _Bristles));
                float b2 = Noise(float2(along * 2.3 + 11.0, (y * 0.5 + 0.5) * _Bristles * 2.1));
                float bristle = b * 0.7 + b2 * 0.3;
                float dry = smoothstep(_DryStart, 1.0, t) * _DryAmount;
                float baw = max(fwidth(bristle), 1e-4) * 1.5;
                float ink = smoothstep(dry - baw, dry + baw, bristle);

                // ink pools at the edges of the stroke
                float rim = smoothstep(edge - _EdgeWidth, edge, ay);
                half3 col = i.color.rgb * (1.0 - _EdgeDark * rim);

                return half4(col, body * ink * i.color.a);
            }
            ENDHLSL
        }
    }
}
