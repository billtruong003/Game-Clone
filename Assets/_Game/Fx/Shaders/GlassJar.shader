// Front glass of the Eye Merge jar, drawn on the "Glass" sorting layer over the balls. Flat toon glass: a flat tint,
// one hard-edged highlight stripe on the left, and a hard-edged glint band that sweeps across when a ball hits the wall.
// No gradients, no refraction, no soft reflections (the dark outline is the jar_line sprite on top).
// All geometry is in world units, set by GlassJar.cs: _Jar = interior (xMin, yMin, xMax, yMax), _Corner = corner radius.
Shader "CasualGame/GlassJar"
{
    Properties
    {
        _Jar ("Interior (xMin, yMin, xMax, yMax)", Vector) = (-3, -4, 3, 4)
        _Corner ("Corner radius", Float) = 0.7
        _Tint ("Glass tint (a = amount)", Color) = (0.62, 0.8, 1, 0.08)
        _StreakX ("Highlight stripe x (0..1 across)", Range(0, 1)) = 0.08
        _StreakWidth ("Highlight stripe width (0..1)", Range(0, 0.2)) = 0.03
        _StreakStrength ("Highlight stripe opacity", Range(0, 1)) = 0.35
        _GlintPos ("Glint position (0..1 across, <0 = off)", Float) = -1
        _GlintWidth ("Glint width (0..1)", Range(0, 0.3)) = 0.05
        _GlintStrength ("Glint opacity", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Blend One OneMinusSrcAlpha   // premultiplied: layers are composited in the shader
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Jar;
                float _Corner, _StreakX, _StreakWidth, _StreakStrength, _GlintPos, _GlintWidth, _GlintStrength;
                half4 _Tint;
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

            void Over(inout half3 c, inout half a, half3 lc, half la)
            {
                c = c * (1.0 - la) + lc * la;
                a = a * (1.0 - la) + la;
            }

            // 1 inside a band of half-width w around 0, with a one-pixel anti-aliased edge (hard, not soft)
            float HardBand(float x, float w)
            {
                float aa = max(fwidth(x), 1e-4);
                return 1.0 - smoothstep(w - aa, w + aa, abs(x));
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 p = i.world;
                // rounded rectangle silhouette, same corner as the jar outline
                float2 hb = (_Jar.zw - _Jar.xy) * 0.5;
                float2 q = abs(p - (_Jar.xy + hb)) - hb + _Corner;
                float sdBox = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - _Corner;
                float aa = max(fwidth(sdBox), 1e-4);
                float inside = 1.0 - smoothstep(-aa, aa, sdBox);
                if (inside <= 0.0) discard;

                float2 size = _Jar.zw - _Jar.xy;
                float u = (p.x - _Jar.x) / size.x;
                float v = (p.y - _Jar.y) / size.y;

                half3 c = 0;
                half a = 0;
                Over(c, a, _Tint.rgb, _Tint.a);

                // flat highlight stripe on the left wall, cut square between 18% and 82% of the height
                float stripe = HardBand(u - _StreakX, _StreakWidth * 0.5) * HardBand(v - 0.5, 0.32);
                Over(c, a, half3(1, 1, 1), (half)(stripe * _StreakStrength));

                if (_GlintPos >= 0.0)
                {
                    float g = u + (1.0 - v) * 0.6;   // diagonal band, leaning right
                    float band = HardBand(g - _GlintPos, _GlintWidth * 0.5);
                    float thin = HardBand(g - _GlintPos - _GlintWidth * 1.4, _GlintWidth * 0.12);
                    float along = HardBand(v - 0.5, 0.36);
                    Over(c, a, half3(1, 1, 1), (half)(saturate(band * 0.6 + thin) * along * _GlintStrength));
                }

                return half4(c * inside, a * inside);
            }
            ENDHLSL
        }
    }
}
