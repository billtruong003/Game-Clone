// Front glass of the Eye Merge jar, on the "Glass" sorting layer over the balls. Toon glass after Cyanilux's
// "Toon Glass Shader Breakdown": a faint translucent tint plus a few hard diagonal highlight lines, nothing else.
//   d     = |x + y| of the position relative to a pivot (the camera in the original; here the jar centre, nudged
//           by _Shift so a ball hitting the wall slides the lines across)
//   lines = step(width, frac(pow(1 - saturate((d - offset) * scale), A + 1.01) * B)) * lineAlpha
// Tuned for a tall jar: per side one thin line then one wide line, close to the centre diagonal. The dark outline is the jar_line sprite on top.
// Geometry is in world units, set by GlassJar.cs: _Jar = interior (xMin, yMin, xMax, yMax), _Corner = corner radius.
Shader "CasualGame/GlassJar"
{
    Properties
    {
        _Jar ("Interior (xMin, yMin, xMax, yMax)", Vector) = (-3, -4, 3, 4)
        _Corner ("Corner radius", Float) = 0.7
        _Color ("Glass colour (a = tint)", Color) = (0.85, 0.93, 1, 0.05)
        _Offset ("Offset", Float) = 0.35
        _Scale ("Scale", Float) = 2.2
        _A ("A (line falloff)", Float) = 3
        _B ("B (lines per side)", Float) = 2
        _LineWidth ("Line width", Range(0, 1)) = 0.55
        _LineAlpha ("Line alpha", Range(0, 1)) = 0.3
        _Shift ("Pattern shift", Float) = 0
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
                float4 _Jar;
                float _Corner, _Offset, _Scale, _A, _B, _LineWidth, _LineAlpha, _Shift;
                half4 _Color;
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

            half4 frag(Varyings i) : SV_Target
            {
                float2 halfSize = (_Jar.zw - _Jar.xy) * 0.5;
                float2 centre = _Jar.xy + halfSize;
                // rounded-rectangle mask, same corner as the jar outline
                float2 q = abs(i.world - centre) - halfSize + _Corner;
                float sd = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - _Corner;
                float aa = max(fwidth(sd), 1e-4);
                float inside = 1.0 - smoothstep(-aa, aa, sd);
                if (inside <= 0.0) discard;

                // pattern in units of the jar's half width, so it scales with the jar
                float2 p = (i.world - centre) / halfSize.x;
                float d = abs(p.x + _Shift + p.y);
                float g = pow(1.0 - saturate((d - _Offset) * _Scale), _A + 1.01) * _B;
                float lines = step(_LineWidth, frac(g));
                return half4(_Color.rgb, saturate(_Color.a + lines * _LineAlpha) * inside);
            }
            ENDHLSL
        }
    }
}
