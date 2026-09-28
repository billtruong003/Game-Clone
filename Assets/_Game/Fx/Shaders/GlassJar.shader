// Front glass of the Eye Merge jar, drawn on the "Glass" sorting layer over the balls. Layers (front to back):
//   glint band (sweeps diagonally when a ball hits the wall) · tall reflection streak on the left · rim light along the
//   walls · a faint blue tint · refraction: near the walls the scene behind is sampled slightly toward the jar center,
//   as if the curved glass bent it (needs the Camera Sorting Layer Texture, see GlassRendererSetup).
// All geometry is in world units, set by GlassJar.cs: _Jar = interior (xMin, yMin, xMax, yMax), _Corner = bottom radius.
Shader "CasualGame/GlassJar"
{
    Properties
    {
        _Jar ("Interior (xMin, yMin, xMax, yMax)", Vector) = (-3, -4, 3, 4)
        _Corner ("Bottom corner radius", Float) = 0.8
        _Tint ("Glass tint (a = amount)", Color) = (0.62, 0.8, 1, 0.07)
        _RimColor ("Rim color", Color) = (0.85, 0.95, 1, 1)
        _RimWidth ("Rim width (world)", Float) = 0.45
        _RimStrength ("Rim strength", Range(0, 1)) = 0.2
        _StreakX ("Reflection streak x (0..1 across)", Range(0, 1)) = 0.1
        _StreakWidth ("Reflection streak width (0..1)", Range(0, 0.2)) = 0.035
        _StreakStrength ("Reflection streak strength", Range(0, 1)) = 0.35
        _GlintPos ("Glint position (0..1 across, <0 = off)", Float) = -1
        _GlintWidth ("Glint width (0..1)", Range(0, 0.3)) = 0.035
        _GlintStrength ("Glint strength", Range(0, 1)) = 0.5
        _Refraction ("Refraction shift (world, toward the center)", Range(0, 0.6)) = 0.18
        _RefractionZone ("Refraction zone from the walls (world)", Float) = 0.9
        [Toggle(_REFRACTION)] _UseRefraction ("Use refraction (needs Camera Sorting Layer Texture)", Float) = 1
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
            #pragma shader_feature_local _REFRACTION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_CameraSortingLayerTexture); SAMPLER(sampler_CameraSortingLayerTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _Jar;
                float _Corner, _RimWidth, _RimStrength, _StreakX, _StreakWidth, _StreakStrength;
                float _GlintPos, _GlintWidth, _GlintStrength, _Refraction, _RefractionZone, _UseRefraction;
                half4 _Tint, _RimColor;
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

            // distance to the jar wall (>0 inside): sides + bottom with rounded bottom corners, open top
            float WallDistance(float2 p, out float2 inward)
            {
                float2 mn = _Jar.xy, mx = _Jar.zw;
                float r = _Corner;
                float left = p.x - mn.x, right = mx.x - p.x, bottom = p.y - mn.y;
                float d = min(min(left, right), bottom);
                inward = left < right ? float2(1, 0) : float2(-1, 0);
                if (bottom < min(left, right)) inward = float2(0, 1);
                // rounded bottom corners
                float2 c = float2(clamp(p.x, mn.x + r, mx.x - r), mn.y + r);
                if (p.y < mn.y + r && (p.x < mn.x + r || p.x > mx.x - r))
                {
                    float2 dv = p - c;
                    d = r - length(dv);
                    inward = -normalize(dv + 1e-5);
                }
                return d;
            }

            void Over(inout half3 c, inout half a, half3 lc, half la)
            {
                c = c * (1.0 - la) + lc * la;
                a = a * (1.0 - la) + la;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 p = i.world;
                float2 inward;
                float d = WallDistance(p, inward);
                // silhouette: rounded at all four corners like the jar outline (the top is open only for the rim)
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

                #ifdef _REFRACTION
                    // bend: the closer to a wall, the further toward the center we look
                    float k = saturate(1.0 - d / _RefractionZone);
                    k = k * k;
                    float2 suv = GetNormalizedScreenSpaceUV(i.positionCS);
                    float2 shiftWorld = inward * _Refraction * k;
                    float4 shiftCS = mul(UNITY_MATRIX_VP, float4(shiftWorld, 0, 0));
                    float2 shiftUV = shiftCS.xy * 0.5;
                    #if UNITY_UV_STARTS_AT_TOP
                        shiftUV.y = -shiftUV.y;
                    #endif
                    half3 bent = SAMPLE_TEXTURE2D(_CameraSortingLayerTexture, sampler_CameraSortingLayerTexture, suv + shiftUV).rgb;
                    Over(c, a, bent, (half)saturate(k * 1.5));
                #endif

                Over(c, a, _Tint.rgb, _Tint.a);

                float rim = pow(saturate(1.0 - d / _RimWidth), 2.0) * _RimStrength;
                Over(c, a, _RimColor.rgb, (half)rim);

                float streak = (1.0 - smoothstep(_StreakWidth * 0.5, _StreakWidth, abs(u - _StreakX)))
                             * smoothstep(0.08, 0.22, v) * (1.0 - smoothstep(0.78, 0.9, v)) * _StreakStrength;
                Over(c, a, half3(1, 1, 1), (half)streak);

                if (_GlintPos >= 0.0)
                {
                    float g = u + (1.0 - v) * 0.6;   // diagonal band, leaning right
                    float band = 1.0 - smoothstep(_GlintWidth * 0.3, _GlintWidth, abs(g - _GlintPos));
                    float thin = 1.0 - smoothstep(_GlintWidth * 0.08, _GlintWidth * 0.22, abs(g - _GlintPos - _GlintWidth * 1.8));
                    // brightest mid-height, fading toward the bottom and the mouth
                    float along = smoothstep(0.0, 0.3, v) * (1.0 - smoothstep(0.7, 1.0, v));
                    Over(c, a, half3(1, 1, 1), (half)(saturate(band * 0.55 + thin * 0.8) * along * _GlintStrength));
                }

                return half4(c * inside, a * inside);
            }
            ENDHLSL
        }
    }
}
