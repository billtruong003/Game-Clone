// Toon particle: hard-edged erosion instead of alpha fade, navy outline band, one cel shadow, optional ramp.
// Shape comes from a mask whose alpha is a field: 0.5 = nominal edge, 1 = core, 0 = far outside.
//   field = mask + (noise - 0.5) * _NoiseStrength;  visible where field > threshold.
// Per-particle data through Custom Vertex Streams (set up by ToonFxBuilder.ConfigureStreams):
//   TEXCOORD0.xy = UV, TEXCOORD0.z = Custom1.x (threshold over lifetime), TEXCOORD0.w = Custom1.y (ramp row offset),
//   TEXCOORD1.x = StableRandom.x (noise offset, so no two particles erode the same way).
Shader "CasualGame/ToonParticle"
{
    Properties
    {
        _MainTex ("Shape mask (alpha field)", 2D) = "white" {}
        _NoiseTex ("Noise (tileable, R)", 2D) = "gray" {}
        _NoiseScale ("Noise scale", Float) = 1.6
        _NoiseStrength ("Noise strength", Range(0, 1)) = 0.35
        _NoiseScroll ("Noise scroll (xy per second)", Vector) = (0, 0, 0, 0)
        _Threshold ("Threshold offset", Range(-1, 1)) = 0
        _OutlineWidth ("Outline width (field units)", Range(0, 0.3)) = 0.08
        _OutlineColor ("Outline color", Color) = (0.118, 0.133, 0.251, 1)
        _ShadeOffset ("Cel shade offset (uv)", Range(0, 0.3)) = 0.08
        _LightDir ("Light direction (xy, uv space)", Vector) = (-0.62, 0.78, 0, 0)
        _ShadeColor ("Shade multiply", Color) = (0.70, 0.64, 0.80, 1)
        _Highlight ("Highlight", Range(0, 1)) = 0.85
        _HighlightPos ("Highlight center (uv) + radius (z)", Vector) = (0.36, 0.64, 0.07, 0)
        [Toggle(_RAMP)] _UseRamp ("Use ramp", Float) = 0
        _RampTex ("Ramp (one palette per row)", 2D) = "white" {}
        _RampRange ("Ramp depth range", Range(0.05, 1)) = 0.45
        _RampRow ("Ramp row (v)", Range(0, 1)) = 0.0625
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
            #pragma shader_feature_local _RAMP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_RampTex); SAMPLER(sampler_RampTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _NoiseScale, _NoiseStrength, _Threshold, _OutlineWidth, _ShadeOffset, _Highlight, _RampRange, _RampRow;
                float4 _NoiseScroll, _LightDir, _HighlightPos;
                half4 _OutlineColor, _ShadeColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float4 uv : TEXCOORD0;   // xy uv, z threshold, w ramp row
                float seed : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float4 uv : TEXCOORD0;
                float seed : TEXCOORD1;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv = float4(TRANSFORM_TEX(v.uv.xy, _MainTex), v.uv.zw);
                o.seed = v.seed;
                return o;
            }

            float Field(float2 uv, float2 nuv)
            {
                float m = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
                float n = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, nuv).r;
                return m + (n - 0.5) * _NoiseStrength;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float th = 0.5 + _Threshold + i.uv.z;
                float2 nuv = i.uv.xy * _NoiseScale + i.seed * float2(7.13, 3.71) + _NoiseScroll.xy * _Time.y;
                float f = Field(i.uv.xy, nuv);
                float w = max(fwidth(f) * 0.75, 1e-4);

                float body = smoothstep(th - w, th + w, f);
                // the outline thins out as the shape erodes, so no loose navy strands survive the body
                float ow = _OutlineWidth * saturate((1.0 - th) * 2.5);
                float outer = smoothstep(th - ow - w, th - ow + w, f);
                if (outer <= 0.0) discard;

                // cel: the lit area is the shape shifted toward the light; what it doesn't cover is the shadow side
                float2 sh = normalize(_LightDir.xy) * _ShadeOffset;
                float fl = Field(i.uv.xy - sh, nuv - sh * _NoiseScale);
                float lit = smoothstep(th - w, th + w, fl);

                half3 baseCol = i.color.rgb;
                #ifdef _RAMP
                    float depth = saturate((f - th) / _RampRange);
                    baseCol *= SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(depth, _RampRow + i.uv.w)).rgb;
                #endif
                half3 col = lerp(baseCol * _ShadeColor.rgb, baseCol, lit);

                // highlight shrinks as the shape erodes
                float hr = _HighlightPos.z * saturate(1.0 - (th - 0.5) * 1.6);
                float2 hq = (i.uv.xy - _HighlightPos.xy) * float2(1.0, 1.5);
                float hw = max(fwidth(hq.x) * 1.5, 1e-4);
                float hl = (1.0 - smoothstep(hr - hw, hr + hw, length(hq))) * lit * _Highlight;
                col = lerp(col, half3(1, 1, 1), hl);

                col = lerp(_OutlineColor.rgb, col, body);
                return half4(col, outer * i.color.a);
            }
            ENDHLSL
        }
    }
}
