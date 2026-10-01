// Hologram: see-through body with bright rims (fresnel-like), screen-space scanlines scrolling down, light bands, flicker and a soft glow halo. Needs ArrowStroke.GlowPad > 0.
// Theme lab (Docs/SHADER_LAB.md). UI shader: works on a uGUI Graphic, masks included. Output is premultiplied alpha.
Shader "CasualGame/Lab/ArrowHolo"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        _Reveal ("Drawn (0 tail .. 1 head)", Range(0, 1)) = 1
        _ScanDensity ("Scanline spacing (px)", Range(2, 24)) = 6
        _BandSpacing ("Light band spacing (px)", Range(10, 200)) = 46
        _BandSpeed ("Band scroll speed", Range(0, 200)) = 40
        _Glow ("Glow halo", Range(0, 1.5)) = 0.8
        _Flicker ("Flicker", Range(0, 1)) = 0.25
        _TailClip ("Line starts this far from the tail (cells): the cap's radius", Float) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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
            #include "ArrowLabCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Reveal, _ScanDensity, _BandSpacing, _BandSpeed, _Glow, _Flicker, _TailClip;
            CBUFFER_END

            half4 frag(ArrowVaryings i) : SV_Target
            {
                float across = abs(i.uv0.y), t = i.uv1.x;
                float reveal = saturate((_Reveal + 0.02 - t) * 50.0);   // drawn up to the brush front (the head at t = 1 included)
                float2 px = ScreenPx(i.screen);
                float inside = AAInside(across - 1.0);
                float rim = pow(saturate(across), 4.0);                       // edges glow brighter than the middle
                float scan = 0.65 + 0.35 * step(0.5, frac(px.y / _ScanDensity));
                float band = smoothstep(0.55, 1.0, sin((px.y + _Time.y * _BandSpeed) / _BandSpacing * 6.2832) * 0.5 + 0.5);
                float flick = 1.0 - _Flicker * step(0.93, ValueNoise(float2(_Time.y * 18.0, 3.0)));
                float body = inside * (0.28 + 0.55 * rim + 0.45 * band) * scan;
                float halo = (1.0 - inside) * exp(-(across - 1.0) * 2.2) * _Glow * (0.6 + 0.4 * band);
                float energy = (body + halo) * flick * reveal * i.color.a * saturate((i.uv0.x - _TailClip) * 20.0);
                half3 c = lerp(i.color.rgb, half3(1, 1, 1), 0.35 * rim * inside + 0.25 * band * inside);
                // additive-ish: colour adds light, alpha only darkens a little
                return half4(c * energy, energy * 0.55);
            }
            ENDHLSL
        }
    }
}
