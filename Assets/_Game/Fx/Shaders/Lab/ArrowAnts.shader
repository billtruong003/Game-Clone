// Bruh Arrows "Ants": the arrow is a column of sprite ants on a scent trail of its colour. Free columns walk in place; a blocked column bunches up and fidgets at the front; leaving, they march off fast. Needs GlowPad 0.18.
// Interactive skin (Docs/SHADER_LAB.md). The game / lab drives the "Live" properties every frame; everything else is
// a look you can tune on the material asset (Assets/_Game/Skins). UI shader, premultiplied alpha, masks work.
Shader "CasualGame/Lab/ArrowAnts"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        [Header(Look)]
        _AntTex ("Ant walk frames (side by side, facing right)", 2D) = "white" {}
        _Frames ("Frames in the strip", Float) = 4
        _AntTint ("Ants take the arrow colour", Range(0, 1)) = 0.3
        _TrailAlpha ("Scent trail strength", Range(0, 1)) = 0.45
        _Spacing ("Ant spacing (cells)", Range(0.2, 0.8)) = 0.48
        _AntSize ("Ant length (cells)", Range(0.15, 0.6)) = 0.44
        [Header(Live (set by the game))]
        _Len ("Arrow length (cells, tail to tip)", Float) = 3
        _Free ("Free 0..1", Range(0, 1)) = 1
        _March ("Walk cycle phase", Float) = 0
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

            TEXTURE2D(_AntTex); SAMPLER(sampler_AntTex);
            CBUFFER_START(UnityPerMaterial)
                float _Frames, _AntTint, _TrailAlpha, _Spacing, _AntSize, _Len, _Free, _March;
            CBUFFER_END

            #define HALF_LINE 0.104

            half4 fragBody(ArrowVaryings i)
            {
                if (i.uv1.y > 0.5) return 0;
                float X = i.uv0.x, Y = i.uv0.y * HALF_LINE;
                half3 col = i.color.rgb;
                float blocked = 1.0 - _Free;
                // scent trail: the arrow's colour, faint, so the path still reads
                float trail = AAInside(abs(Y) - 0.05) * step(-0.05, X) * step(X, _Len) * _TrailAlpha;
                half3 c = col; float a = trail;
                // ants, counted back from the head; a blocked column bunches up at the front
                float d = _Len - X;
                float dd = d / (1.0 - 0.4 * blocked * exp(-d * 1.2));
                float k = floor(dd / _Spacing);
                float u = dd - k * _Spacing;                                  // 0 at this ant's head
                float fidget = blocked * step(k, 2.0) * sin(_Time.y * 16.0 + k * 2.0) * 0.03;
                float antW = _AntSize * 80.0 / 128.0;
                float2 uv = float2(1.0 - u / _AntSize, (Y - fidget) / antW + 0.5);
                if (all(uv > 0) && all(uv < 1) && dd >= 0 && X > -0.15)
                {
                    float frame = fmod(floor(_March + k * 1.7), _Frames);
                    half4 s = SAMPLE_TEXTURE2D(_AntTex, sampler_AntTex, float2((frame + clamp(uv.x, 0.004, 0.996)) / _Frames, uv.y));
                    half lum = dot(s.rgb, half3(0.3, 0.59, 0.11));
                    half3 ant = lerp(s.rgb, col * (0.3 + lum * 1.4), _AntTint);
                    c = lerp(c, ant, s.a);
                    a = max(a, s.a);
                }
                return half4(c * a * i.color.a, a * i.color.a);
            }

            // inside a UI mask (scroll list, card), nothing draws outside it
            half4 frag(ArrowVaryings i) : SV_Target { return fragBody(i) * UIClip(i.local); }
            ENDHLSL
        }
    }
}
