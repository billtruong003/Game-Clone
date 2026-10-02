// The trail an arrow leaves when it flies out, in its theme (Docs/SHADER_LAB.md). For a TrailRenderer with
// Texture Mode = Stretch: uv.x runs from the newest point (0, at the arrow's tail) to the oldest (1), uv.y across.
// _Mode: 0 glow (neon / vector / hologram: a light streak, additive), 1 chalk (grainy dust), 2 blueprint (dashed
// construction line). Colour = the trail's vertex colour.
Shader "CasualGame/Lab/ThemeTrail"
{
    Properties
    {
        _Mode ("Mode", Range(0, 2)) = 0
        _Length ("Trail length (world, for dash / grain scale)", Float) = 4
        [HideInInspector] _SrcBlend ("Src", Float) = 1
        [HideInInspector] _DstBlend ("Dst", Float) = 10
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Blend [_SrcBlend] [_DstBlend]
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Mode, _Length, _SrcBlend, _DstBlend;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; float2 screen : TEXCOORD1; };

            float Hash21(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float ValueNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash21(i), Hash21(i + float2(1, 0)), u.x), lerp(Hash21(i + float2(0, 1)), Hash21(i + float2(1, 1)), u.x), u.y);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv = v.uv;
                float4 sp = ComputeScreenPos(o.positionCS);
                o.screen = sp.xy / max(sp.w, 1e-5) * _ScreenParams.xy;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float across = abs(i.uv.y * 2.0 - 1.0);       // 0 in the middle .. 1 at the edge
                float age = i.uv.x;                            // 0 newest .. 1 oldest
                float fade = 1.0 - age;
                if (_Mode < 0.5)                               // glow: hot core, soft halo, fading out (additive)
                {
                    float core = saturate(1.0 - across / 0.25);
                    float halo = exp(-across * 3.0);
                    half3 c = lerp(i.color.rgb, half3(1, 1, 1), core * 0.6) * (core * 0.9 + halo * 0.6) * fade * fade;
                    return half4(c * i.color.a, 1);
                }
                if (_Mode < 1.5)                               // chalk: grains that thin out with age
                {
                    float g = ValueNoise(i.screen / 1.6) * 0.6 + ValueNoise(i.screen / 0.7) * 0.4;
                    float body = step(across, 0.75 + (ValueNoise(i.screen * 0.2) - 0.5) * 0.4);
                    float a = body * step(1.0 - 0.75 * fade, g) * i.color.a * fade;
                    return half4(i.color.rgb * a, a);
                }
                // blueprint: a thin dashed construction line with tick marks
                float along = age * _Length * 3.0;
                float dash = step(0.45, frac(along));
                float stroke = step(across, 0.18);
                float tick = step(frac(along * 0.5 + 0.02), 0.06) * step(across, 0.7);
                float a2 = saturate(stroke * dash + tick) * i.color.a * fade;
                return half4(i.color.rgb * a2, a2);
            }
            ENDHLSL
        }
    }
}
