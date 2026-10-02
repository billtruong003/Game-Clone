// Meh Merge "Snow Globe": a glass globe with a tiny winter scene from a sprite (sky = tier colour). A hit or a shake (_Shake) whirls the snow up; it settles again over a few seconds.
// Interactive skin (Docs/SHADER_LAB.md). The game / lab drives the "Live" properties every frame; everything else is
// a look you can tune on the material asset (Assets/_Game/Skins). UI shader, premultiplied alpha, masks work.
Shader "CasualGame/Lab/SkinSnowGlobe"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        [Header(Look)]
        _R ("Ball radius in the quad", Range(0.3, 1)) = 0.94
        _SceneTex ("Scenes (3 side by side: snowman, cabin, pines)", 2D) = "white" {}
        _Scene ("Scene (0 snowman, 1 cabin, 2 pines)", Range(0, 2)) = 0
        _SceneSize ("Scene size", Range(0.5, 1.4)) = 1.2
        _SceneY ("Scene height", Range(-0.5, 0.3)) = -0.02
        _SnowColor ("Snow", Color) = (1, 1, 1, 1)
        _SkyLight ("Sky lightness", Range(0, 1)) = 0.45
        _Flakes ("Snowflakes when shaken", Range(4, 40)) = 30
        _CalmFlakes ("Snowflakes when calm", Range(0, 12)) = 5
        [Header(Live (set by the game))]
        _Shake ("Shake 0..1 (decays in the game)", Range(0, 1)) = 0
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
            #include "SkinCommon.hlsl"

            TEXTURE2D(_SceneTex); SAMPLER(sampler_SceneTex);
            CBUFFER_START(UnityPerMaterial)
                float _R, _Scene, _SceneSize, _SceneY, _SkyLight, _Flakes, _CalmFlakes, _Shake;
                half4 _SnowColor;
            CBUFFER_END

            half4 frag(ArrowVaryings i) : SV_Target
            {
                float2 p = (i.uv0 * 2.0 - 1.0) / _R;
                float r = length(p);
                half3 tint = i.color.rgb;
                half3 c = lerp(lerp(tint, half3(1, 1, 1), _SkyLight), tint * 0.8, saturate(p.y * 0.5 + 0.5));
                // the scene sprite; below it the snow keeps going to the glass
                float2 suv = (p - float2(0, _SceneY)) / (_SceneSize * 1.6) + 0.5;
                if (suv.y < 0.06) c = _SnowColor.rgb * 0.97;
                else if (suv.x > 0 && suv.x < 1 && suv.y < 1)
                {
                    half4 sc = SAMPLE_TEXTURE2D(_SceneTex, sampler_SceneTex, float2((round(_Scene) + clamp(suv.x, 0.004, 0.996)) / 3.0, suv.y));
                    c = lerp(c, sc.rgb, sc.a);
                }
                // snow: a few flakes drift when calm; a shake whirls many up
                float count = lerp(_CalmFlakes, _Flakes, _Shake);
                float speed = 0.06 + 0.5 * _Shake;
                [loop] for (int k = 0; k < 40; k++)
                {
                    float vis = saturate(count - k);
                    if (vis <= 0) break;
                    float h1 = Hash11(k * 1.37 + 0.1), h2 = Hash11(k * 2.11 + 0.7), h3 = Hash11(k * 3.7 + 0.3);
                    float fall = frac(h1 + _Time.y * speed * (0.6 + h2));
                    float2 fp = float2((h2 * 2.0 - 1.0) * 0.75 + sin(_Time.y * (1.0 + h3 * 2.0) + h1 * 6.3) * (0.04 + 0.3 * _Shake),
                                       0.85 - fall * 1.45);
                    float fr = 0.022 + h3 * 0.024;
                    Paint(c, _SnowColor.rgb, AAInside(Circle(p - fp, fr)) * vis * (1.0 - smoothstep(0.85, 1.0, fall) * (1.0 - _Shake)));
                }
                c = GlassShade(p, c, tint, 0.6);
                return Out(c, AAInside(r - 1.0), i.color);
            }

            ENDHLSL
        }
    }
}
