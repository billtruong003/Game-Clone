// Nah Blocks "Aquarium": glass blocks of water (tinted by the Image colour) with a sprite fish and a tank bed. The water tilts and sloshes when the block is dragged (_Tilt) or bumped (_Slosh); it drains when the row clears.
// Interactive skin (Docs/SHADER_LAB.md). The game / lab drives the "Live" properties every frame; everything else is
// a look you can tune on the material asset (Assets/_Game/Skins). UI shader, premultiplied alpha, masks work.
Shader "CasualGame/Lab/SkinAquariumBlock"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        [Header(Look)]
        _FishTex ("Fish (2 frames across x 3 kinds down)", 2D) = "white" {}
        _BedTex ("Tank bed (sand, pebbles, weed)", 2D) = "white" {}
        _FishSize ("Fish size", Range(0.3, 1)) = 0.85
        _WaterTint ("Water takes the block colour", Range(0, 1)) = 0.8
        _WaterColor ("Water", Color) = (0.35, 0.75, 0.95, 1)
        _AirColor ("Air above the water", Color) = (0.92, 0.97, 1, 1)
        _Level ("Water level", Range(0, 1)) = 0.78
        _Sway ("Weed sway", Range(0, 0.1)) = 0.03
        [Header(Live (set by the game))]
        _Tilt ("Tilt -1..1", Range(-1, 1)) = 0
        _Slosh ("Slosh 0..1", Range(0, 1)) = 0
        _Drain ("Drain (row clear) 0..1", Range(0, 1)) = 0
        _Seed ("Seed (fish kind and path)", Float) = 0
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

            TEXTURE2D(_FishTex); SAMPLER(sampler_FishTex);
            TEXTURE2D(_BedTex); SAMPLER(sampler_BedTex);
            CBUFFER_START(UnityPerMaterial)
                float _FishSize, _WaterTint, _Level, _Sway, _Tilt, _Slosh, _Drain, _Seed;
                half4 _WaterColor, _AirColor;
            CBUFFER_END

            half4 frag(ArrowVaryings i) : SV_Target
            {
                float2 p = i.uv0 * 2.0 - 1.0;
                float body = RoundBox(p, float2(0.93, 0.93), 0.14);
                half3 water = lerp(_WaterColor.rgb, i.color.rgb, _WaterTint);
                float level = (_Level * (1.0 - _Drain)) * 2.0 - 1.0;
                float surf = level + _Tilt * p.x * 0.45 + sin(p.x * 5.0 + _Time.y * 4.0 + _Seed) * 0.035 * (0.25 + _Slosh);
                float inWater = AAInside(p.y - surf);
                half3 c = _AirColor.rgb;
                half3 wc = water * lerp(0.72, 1.05, saturate((p.y + 1.0) / max(surf + 1.0, 0.01)));
                wc += smoothstep(0.62, 0.8, Fbm(p * 2.5 + float2(_Time.y * 0.3, _Seed))) * 0.15;     // caustics
                Paint(c, wc, inWater);
                Paint(c, half3(1, 1, 1), AAInside(abs(p.y - surf) - 0.025) * 0.8);
                // tank bed sprite along the bottom, the weed swaying
                float2 buv = float2(p.x * 0.5 + 0.5, (p.y + 0.93) / 0.93);
                buv.x += sin(_Time.y * 1.5 + p.x * 3.0 + _Seed) * _Sway * saturate(buv.y - 0.15);
                if (buv.y < 1 && buv.y > 0)
                {
                    half4 bed = SAMPLE_TEXTURE2D(_BedTex, sampler_BedTex, buv);
                    c = lerp(c, bed.rgb, bed.a);
                }
                // the fish swims left and right under the surface (sprite, two tail frames)
                float t = _Time.y * 0.7 + _Seed * 1.7;
                float hw = _FishSize * 0.5;
                float2 fp = float2(sin(t) * (0.9 - hw), min(-0.12 + 0.14 * sin(t * 1.7), surf - hw * 0.8));
                float face = cos(t) >= 0 ? 1.0 : -1.0;
                float2 fl = float2((p.x - fp.x) * face / hw, (p.y - fp.y) / (hw * 0.75)) * 0.5 + 0.5;
                float kind = fmod(abs(round(_Seed)), 3.0);
                float frame = step(0.5, frac(_Time.y * 2.5 + _Seed * 0.3));
                if (all(fl > 0) && all(fl < 1) && surf > -0.9)
                {
                    half4 f = SAMPLE_TEXTURE2D(_FishTex, sampler_FishTex, float2((frame + clamp(fl.x, 0.004, 0.996)) / 2.0, (2.0 - kind + clamp(fl.y, 0.004, 0.996)) / 3.0));
                    c = lerp(c, f.rgb, f.a);
                }
                // bubbles
                [unroll] for (int b = 0; b < 3; b++)
                {
                    float rise = frac(_Time.y * 0.35 + b * 0.33 + _Seed * 0.1);
                    float2 bp = float2(0.55 - b * 0.12 + sin(rise * 9.0) * 0.04, -0.6 + rise * 1.6);
                    Paint(c, half3(1, 1, 1), AAInside(abs(Circle(p - bp, 0.05)) - 0.012) * step(bp.y, surf) * 0.8);
                }
                // glass
                c += smoothstep(0.12, 0.0, abs(p.x - p.y * 0.3 + 0.62)) * 0.25;
                Paint(c, lerp(water, half3(1, 1, 1), 0.5), AAInside(abs(body + 0.06) - 0.025) * 0.6);
                Paint(c, INK, 1.0 - AAInside(body + 0.04));
                return Out(c, AAInside(body), i.color);
            }

            ENDHLSL
        }
    }
}
