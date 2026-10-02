# Generates the interactive skin shaders (python Tools/shaders/make_skins.py) (UI shaders, premultiplied, stencil-aware) into Fx/Shaders/Lab.
import io, os
import os
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "../../Assets/_Game/Fx/Shaders/Lab/")

TEMPLATE = """// {doc}
// Interactive skin (Docs/SHADER_LAB.md). The game / lab drives the "Live" properties every frame; everything else is
// a look you can tune on the material asset (Assets/_Game/Skins). UI shader, premultiplied alpha, masks work.
Shader "CasualGame/Lab/{name}"
{{
    Properties
    {{
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {{}}
{props}
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }}
    SubShader
    {{
        Tags {{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }}
        Stencil {{ Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }}
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {{
            HLSLPROGRAM
            #pragma vertex ArrowVert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "SkinCommon.hlsl"
{textures}
            CBUFFER_START(UnityPerMaterial)
{cbuffer}
            CBUFFER_END
{body}
            // inside a UI mask (scroll list, card), nothing draws outside it
            half4 frag(ArrowVaryings i) : SV_Target {{ return fragBody(i) * UIClip(i.local); }}
            ENDHLSL
        }}
    }}
}}
"""


def prop_lines(props):
    out, cb, tex = [], {}, []
    for p in props:
        name, label, kind, default = p
        if kind == "Color":
            out.append(f'        {name} ("{label}", Color) = {default}')
            cb.setdefault("half4", []).append(name)
        elif kind == "Vector":
            out.append(f'        {name} ("{label}", Vector) = {default}')
            cb.setdefault("float4", []).append(name)
        elif kind == "2D":
            out.append(f'        {name} ("{label}", 2D) = "{default}" {{}}')
            tex.append(f"            TEXTURE2D({name}); SAMPLER(sampler{name});")
        elif kind.startswith("Range") or kind == "Float":
            out.append(f'        {name} ("{label}", {kind}) = {default}')
            cb.setdefault("float", []).append(name)
        elif kind == "Header":
            out.append(f'        [Header({label})]')
        else:
            raise ValueError(kind)
    cbl = [f"                {t} {', '.join(v)};" for t, v in cb.items()]
    return "\n".join(out), "\n".join(cbl), "\n".join(tex)


def header(label):
    return ("", label, "Header", "")


def emit(name, doc, props, body):
    # headers need a following property: fold them into the next line
    lines = []
    pending = None
    for p in props:
        if p[2] == "Header":
            pending = p[1]
            continue
        lines.append(p)
        if pending:
            lines[-1] = (p[0], p[1], p[2], p[3], pending)
            pending = None
    out, cb, tex = [], {}, []
    for p in lines:
        hdr = p[4] if len(p) > 4 else None
        if hdr:
            out.append(f"        [Header({hdr})]")
        o, c, t = prop_lines([p[:4]])
        out.append(o)
        for k in c.splitlines():
            pass
        n, label, kind, default = p[:4]
        if kind == "Color": cb.setdefault("half4", []).append(n)
        elif kind == "Vector": cb.setdefault("float4", []).append(n)
        elif kind == "2D": tex.append(f"            TEXTURE2D({n}); SAMPLER(sampler{n});")
        else: cb.setdefault("float", []).append(n)
    cbl = "\n".join(f"                {t} {', '.join(v)};" for t, v in cb.items())
    src = TEMPLATE.format(doc=doc, name=name, props="\n".join(out), textures=("\n" + "\n".join(tex)) if tex else "", cbuffer=cbl, body=body)
    io.open(OUT + name + ".shader", "w", encoding="utf-8", newline="\n").write(src)
    print("wrote", name)


C = "Color"
W = "(1, 1, 1, 1)"

# ============================================================ Meh Merge ============================================================

emit("SkinHungryBall",
     "Meh Merge \"Hungry\": a plain ball with big eyes and a mouth. Balls of the held ball's tier look up at it and open wide; the others look away and sulk. Body colour = the Image colour (the tier).",
     [header("Look"),
      ("_R", "Ball radius in the quad", "Range(0.3, 1)", "0.94"),
      ("_EyeWhite", "Eye white", C, "(1, 1, 1, 1)"),
      ("_InkColor", "Ink (pupils, outline)", C, "(0.118, 0.133, 0.251, 1)"),
      ("_MouthColor", "Inside of the mouth", C, "(0.35, 0.08, 0.14, 1)"),
      ("_TongueColor", "Tongue", C, "(1, 0.48, 0.55, 1)"),
      ("_BlushColor", "Blush when hungry", C, "(1, 0.45, 0.55, 1)"),
      ("_EyeSize", "Eye size", "Range(0.6, 1.6)", "1"),
      header("Live (set by the game)"),
      ("_Look", "Look direction (x, y)", "Vector", "(0, 0, 0, 0)"),
      ("_Mouth", "Mouth open 0..1", "Range(0, 1)", "0"),
      ("_Sulk", "Sulk (half-closed eyes) 0..1", "Range(0, 1)", "0"),
      ("_Blink", "Blink 0..1", "Range(0, 1)", "0")],
     r"""
            half4 fragBody(ArrowVaryings i)
            {
                float2 p = (i.uv0 * 2.0 - 1.0) / _R;
                float r = length(p);
                half3 body = i.color.rgb;
                half3 c = body;
                float2 look = clamp(_Look.xy, -1.0, 1.0);
                float open = _Mouth;
                float2 f = p - look * 0.13;                                  // the face turns toward what it looks at
                // blush
                [unroll] for (int b = 0; b < 2; b++)
                    Paint(c, _BlushColor.rgb, AAInside(Circle(f - float2(b == 0 ? -0.52 : 0.52, -0.16), 0.13)) * 0.45 * open);
                // eyes: white, ink ring, pupil, lids
                float2 eyeR = float2(0.17, lerp(0.17, 0.22, open)) * _EyeSize;
                float lid = max(_Blink, _Sulk * 0.5);
                [unroll] for (int k = 0; k < 2; k++)
                {
                    float2 e = f - float2(k == 0 ? -0.32 : 0.32, 0.2);
                    float d = Ellipse(e, eyeR);
                    float white = AAInside(d);
                    Paint(c, _InkColor.rgb, AAInside(d - 0.035));
                    Paint(c, _EyeWhite.rgb, white);
                    float pr = eyeR.x * lerp(0.55, 0.42, open);
                    Paint(c, _InkColor.rgb, AAInside(Circle(e - look * eyeR * 0.42, pr)) * white);
                    float lidLine = eyeR.y * (1.0 - 2.0 * lid);
                    float cover = AAInside(lidLine - e.y) * white;
                    Paint(c, body * 0.9, cover * step(0.02, lid));
                    Paint(c, _InkColor.rgb, AAInside(abs(e.y - lidLine) - 0.018) * white * step(0.02, lid));
                }
                // mouth: a flat line when closed, a round open mouth with a tongue when hungry
                float2 m = f - float2(0, -0.34 - open * 0.06);
                float2 mr = float2(lerp(0.13, 0.25, open), lerp(0.02, 0.26, open));
                float md = Ellipse(m, mr);
                float mouth = AAInside(md);
                Paint(c, _InkColor.rgb, AAInside(md - 0.035) * step(0.08, open));
                Paint(c, _MouthColor.rgb, mouth * step(0.08, open));
                float tongue = AAInside(Ellipse(m - float2(0, -mr.y * 0.62), float2(mr.x * 0.68, mr.y * 0.5))) * mouth;
                Paint(c, _TongueColor.rgb, tongue * step(0.08, open));
                Paint(c, _InkColor.rgb, AAInside(Segment(m, float2(-0.13, 0), float2(0.13, 0)) - 0.025) * (1.0 - step(0.08, open)));
                c = lerp(body, c, AAInside(r - 0.9));                       // the face stays on the ball
                c = BallShade(p, c, 1.0);
                return Out(c, AAInside(r - 1.0), i.color);
            }
""")

emit("SkinHamsterBall",
     "Meh Merge \"Hamster Ball\": a clear plastic ball (the tier colour) with a hamster inside. It runs the way the ball rolls, tumbles on a hard landing and falls asleep when the ball rests.",
     [header("Look"),
      ("_R", "Ball radius in the quad", "Range(0.3, 1)", "0.94"),
      ("_ShellTint", "Shell tint strength", "Range(0, 1)", "0.6"),
      ("_FurColor", "Fur", C, "(0.93, 0.66, 0.36, 1)"),
      ("_BellyColor", "Belly", C, "(1, 0.93, 0.8, 1)"),
      ("_PinkColor", "Ears / nose / paws", C, "(1, 0.6, 0.65, 1)"),
      ("_InkColor", "Ink", C, "(0.118, 0.133, 0.251, 1)"),
      header("Live (set by the game)"),
      ("_Spin", "Roll angle", "Float", "0"),
      ("_Run", "Run speed -1..1 (sign = direction)", "Range(-1, 1)", "0"),
      ("_Stride", "Run cycle phase", "Float", "0"),
      ("_Tumble", "Tumble angle", "Float", "0"),
      ("_Sleep", "Asleep 0..1", "Range(0, 1)", "0")],
     r"""
            void Hamster(float2 h, inout half3 c, float run, float stride, float sleep)
            {
                // h: hamster space, facing +x, ball centre at the origin
                float breathe = sin(_Time.y * 2.0) * 0.012 * sleep;
                float bob = abs(sin(stride)) * 0.04 * run;
                float2 b = h - float2(0, -0.38 + bob - sleep * 0.06);
                // paws (two pairs, opposite phase)
                [unroll] for (int k = 0; k < 2; k++)
                {
                    float ph = stride + k * 3.1416;
                    float2 paw = b - float2((k == 0 ? 0.22 : -0.18) + sin(ph) * 0.12 * run, -0.33 + max(0.0, cos(ph)) * 0.06 * run);
                    float d = Ellipse(paw, float2(0.1, 0.06));
                    Paint(c, _InkColor.rgb, AAInside(d - 0.03));
                    Paint(c, _PinkColor.rgb, AAInside(d));
                }
                // body
                float2 br = float2(0.5 + breathe, lerp(0.38, 0.33, sleep) + breathe);
                float body = Ellipse(b, br);
                Paint(c, _InkColor.rgb, AAInside(body - 0.035));
                Paint(c, _FurColor.rgb, AAInside(body));
                Paint(c, _BellyColor.rgb, AAInside(Ellipse(b - float2(0.16, -0.12), float2(0.3, 0.2))) * AAInside(body));
                // ears
                [unroll] for (int e = 0; e < 2; e++)
                {
                    float2 ep = b - float2(e == 0 ? 0.16 : -0.04, br.y * 0.86);
                    float ed = Circle(ep, 0.1);
                    Paint(c, _InkColor.rgb, AAInside(ed - 0.03));
                    Paint(c, _FurColor.rgb, AAInside(ed));
                    Paint(c, _PinkColor.rgb, AAInside(Circle(ep, 0.05)));
                }
                // face
                float2 eye = b - float2(0.3, 0.1);
                Paint(c, _InkColor.rgb, AAInside(Circle(eye, 0.055)) * (1.0 - sleep));
                Paint(c, half3(1, 1, 1), AAInside(Circle(eye - float2(0.018, 0.02), 0.018)) * (1.0 - sleep));
                Paint(c, _InkColor.rgb, AAInside(Segment(eye, float2(-0.05, 0), float2(0.05, 0)) - 0.016) * sleep);
                Paint(c, _PinkColor.rgb, AAInside(Circle(b - float2(0.5, 0.02), 0.04)));
                Paint(c, _PinkColor.rgb, AAInside(Circle(b - float2(0.3, -0.06), 0.07)) * 0.5);
            }

            half4 fragBody(ArrowVaryings i)
            {
                float2 p = (i.uv0 * 2.0 - 1.0) / _R;
                float r = length(p);
                float3 n = SphereNormal(p);
                half3 tint = i.color.rgb;
                half3 c = lerp(half3(0.97, 0.97, 0.98), tint, _ShellTint) * (0.9 + 0.1 * n.z);
                // vent lines on the far side of the shell, rolling with the ball
                float a = atan2(p.y, p.x) + _Spin;
                float vent = AAInside(abs(frac(a / 6.2832 * 10.0) - 0.5) * 0.6 - 0.02) * smoothstep(0.55, 0.85, r);
                Paint(c, tint * 0.75, vent * 0.5);
                // the hamster stays upright (gravity) and tumbles on hard landings
                float2 h = Rot2(p, -_Tumble);
                float dir = _Run < -0.02 ? -1.0 : 1.0;
                h.x *= dir;
                Hamster(h, c, saturate(abs(_Run) * 1.5), _Stride, _Sleep);
                c = GlassShade(p, c, tint, 0.85);
                return Out(c, AAInside(r - 1.0), i.color);
            }
""")

emit("SkinCompassBall",
     "Meh Merge \"Compass\": every ball is a compass in a case of its tier colour. The needle points at the nearest ball of the same tier, so it hints where to drop; with no match it slowly searches.",
     [header("Look"),
      ("_R", "Ball radius in the quad", "Range(0.3, 1)", "0.94"),
      ("_DialColor", "Dial", C, "(0.98, 0.95, 0.87, 1)"),
      ("_BezelColor", "Brass bezel", C, "(0.85, 0.66, 0.3, 1)"),
      ("_NeedleColor", "Needle (pointing end)", C, "(0.9, 0.2, 0.22, 1)"),
      ("_NeedleTail", "Needle (back end)", C, "(0.8, 0.82, 0.86, 1)"),
      ("_TickColor", "Ticks", C, "(0.25, 0.25, 0.3, 1)"),
      ("_DialSize", "Dial size", "Range(0.4, 0.85)", "0.66"),
      header("Live (set by the game)"),
      ("_Spin", "Roll angle (the dial ticks turn with it)", "Float", "0"),
      ("_Needle", "Needle angle (radians, 0 = right)", "Float", "1.5708")],
     r"""
            half4 fragBody(ArrowVaryings i)
            {
                float2 p = (i.uv0 * 2.0 - 1.0) / _R;
                float r = length(p);
                half3 c = i.color.rgb;
                float R1 = _DialSize;
                // brass bezel ring
                float bezel = AAInside(r - (R1 + 0.1));
                half3 brass = _BezelColor.rgb * (0.8 + 0.35 * (p.y * 0.5 + 0.5)) + smoothstep(0.92, 1.0, sin(atan2(p.y, p.x) * 2.0 + 1.0)) * 0.25;
                Paint(c, INK, bezel);
                Paint(c, brass, AAInside(r - (R1 + 0.07)));
                // dial with ticks (they turn with the case)
                float dial = AAInside(r - R1);
                half3 dc = _DialColor.rgb * (1.0 - 0.12 * r / R1);
                float a = atan2(p.y, p.x) - _Spin;
                float seg = a / 6.2832 * 16.0;
                float tickD = abs(frac(seg + 0.5) - 0.5) / 16.0 * 6.2832 * r;
                float major = step(abs(frac(seg / 4.0 + 0.125) - 0.125), 0.02);
                float tick = AAInside(tickD - lerp(0.012, 0.022, major)) * step(R1 * lerp(0.8, 0.7, major), r) * step(r, R1 * 0.95);
                Paint(dc, _TickColor.rgb, tick);
                // north marker
                float2 nq = Rot2(p, -_Spin) - float2(0, R1 * 0.82);
                Paint(dc, _NeedleColor.rgb, AAInside(max(abs(nq.x) * 2.0 + nq.y * 1.2, -nq.y - 0.08) - 0.06));
                Paint(c, dc, dial);
                // needle shadow, needle, pin
                float2 q = Rot2(p, -(_Needle - 1.5708));
                float2 qs = q - Rot2(float2(0.03, -0.035), -(_Needle - 1.5708));
                float len = R1 * 0.86, wid = R1 * 0.13;
                float sh = abs(qs.x) / wid + abs(qs.y) / len - 1.0;
                Paint(c, half3(0, 0, 0), AAInside(sh * wid) * 0.22 * dial);
                float nd = (abs(q.x) / wid + abs(q.y) / len - 1.0) * wid;
                Paint(c, INK, AAInside(nd - 0.02) * dial);
                Paint(c, q.y > 0 ? _NeedleColor.rgb : _NeedleTail.rgb, AAInside(nd));
                Paint(c, INK, AAInside(Circle(p, 0.065)));
                Paint(c, brass, AAInside(Circle(p, 0.045)));
                // glass over the dial
                c += smoothstep(0.3, 0.0, length(p - float2(-0.25, 0.3))) * 0.25 * dial;
                c = BallShade(p, c, 1.0);
                return Out(c, AAInside(r - 1.0), i.color);
            }
""")

emit("SkinSnowGlobe",
     "Meh Merge \"Snow Globe\": a glass globe with a tiny winter scene from a sprite (sky = tier colour). A hit or a shake (_Shake) whirls the snow up; it settles again over a few seconds.",
     [header("Look"),
      ("_R", "Ball radius in the quad", "Range(0.3, 1)", "0.94"),
      ("_SceneTex", "Scenes (3 side by side: snowman, cabin, pines)", "2D", "white"),
      ("_Scene", "Scene (0 snowman, 1 cabin, 2 pines)", "Range(0, 2)", "0"),
      ("_SceneSize", "Scene size", "Range(0.5, 1.4)", "1.2"),
      ("_SceneY", "Scene height", "Range(-0.5, 0.3)", "-0.02"),
      ("_SnowColor", "Snow", C, "(1, 1, 1, 1)"),
      ("_SkyLight", "Sky lightness", "Range(0, 1)", "0.45"),
      ("_Flakes", "Snowflakes when shaken", "Range(4, 40)", "30"),
      ("_CalmFlakes", "Snowflakes when calm", "Range(0, 12)", "5"),
      header("Live (set by the game)"),
      ("_Shake", "Shake 0..1 (decays in the game)", "Range(0, 1)", "0")],
     r"""
            half4 fragBody(ArrowVaryings i)
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
""")

emit("SkinWatchBlock",
     "Nah Blocks \"Watchers\": soft blocks (Image colour) with big eyes that follow your finger. Blocks right under the dragged piece go wide-eyed; a cleared row squeezes its eyes shut.",
     [header("Look"),
      ("_EyeWhite", "Eye white", C, "(1, 1, 1, 1)"),
      ("_InkColor", "Ink", C, "(0.118, 0.133, 0.251, 1)"),
      ("_EyeSize", "Eye size", "Range(0.6, 1.4)", "1"),
      ("_PupilSize", "Pupil size", "Range(0.4, 1.6)", "1"),
      ("_Corner", "Corner roundness", "Range(0.05, 0.5)", "0.28"),
      header("Live (set by the game)"),
      ("_Look", "Look direction (x, y)", "Vector", "(0, 0, 0, 0)"),
      ("_Alarm", "Alarmed (piece right above) 0..1", "Range(0, 1)", "0"),
      ("_Blink", "Blink / squeeze 0..1", "Range(0, 1)", "0")],
     r"""
            half4 fragBody(ArrowVaryings i)
            {
                float2 p = i.uv0 * 2.0 - 1.0;
                half3 base = i.color.rgb;
                float body = RoundBox(p, float2(0.92, 0.92), _Corner);
                half3 c = base * lerp(0.92, 1.1, smoothstep(-0.6, 0.9, p.y));
                c = lerp(c, half3(1, 1, 1), smoothstep(0.2, 0.0, abs(p.x + 0.62)) * 0.15 * step(p.y, 0.6));
                float2 look = clamp(_Look.xy, -1.0, 1.0);
                float2 eyeR = float2(0.27, lerp(0.27, 0.33, _Alarm)) * _EyeSize;
                [unroll] for (int k = 0; k < 2; k++)
                {
                    float2 e = p - float2(k == 0 ? -0.38 : 0.38, 0.1);
                    float d = Ellipse(e, eyeR);
                    float white = AAInside(d);
                    Paint(c, _InkColor.rgb, AAInside(d - 0.05));
                    Paint(c, _EyeWhite.rgb, white);
                    float pr = eyeR.x * 0.5 * _PupilSize * lerp(1.0, 0.55, _Alarm);
                    Paint(c, _InkColor.rgb, AAInside(Circle(e - look * eyeR * 0.45, pr)) * white);
                    Paint(c, half3(1, 1, 1), AAInside(Circle(e - look * eyeR * 0.45 - float2(pr * 0.35, pr * 0.35), pr * 0.3)) * white);
                    float lidLine = eyeR.y * (1.0 - 2.0 * _Blink);
                    Paint(c, base * 0.88, AAInside(lidLine - e.y) * white * step(0.02, _Blink));
                    Paint(c, _InkColor.rgb, AAInside(abs(e.y - lidLine) - 0.03) * white * step(0.02, _Blink));
                }
                // mouth: a line, a small "o" when alarmed
                float2 m = p - float2(0, -0.5);
                Paint(c, _InkColor.rgb, AAInside(Segment(m, float2(-0.12, 0), float2(0.12, 0)) - 0.035) * (1.0 - step(0.5, _Alarm)));
                Paint(c, _InkColor.rgb, AAInside(abs(Ellipse(m, float2(0.09, 0.11))) - 0.03) * step(0.5, _Alarm));
                Paint(c, _InkColor.rgb, 1.0 - AAInside(body + 0.06));
                return Out(c, AAInside(body), i.color);
            }
""")

emit("SkinBuildingBlock",
     "Nah Blocks \"Night City\": every block is a window (frame = Image colour) looking out on ONE city behind the board. The skyline layers are drawn in screen space and slide at different depths with the finger (_Parallax), so the board feels like glass over a deep scene. City windows light up as the block's row fills (_Lit).",
     [header("Look"),
      ("_FarTex", "Far skyline (tiles sideways)", "2D", "white"),
      ("_MidTex", "Buildings (tiles sideways)", "2D", "white"),
      ("_WinTex", "Building windows (grey = when it lights)", "2D", "black"),
      ("_SkyTop", "Sky top", C, "(0.05, 0.06, 0.18, 1)"),
      ("_SkyBottom", "Sky at the horizon", C, "(0.25, 0.2, 0.45, 1)"),
      ("_LightColor", "Window light", C, "(1, 0.84, 0.45, 1)"),
      ("_MoonColor", "Moon", C, "(1, 0.96, 0.82, 1)"),
      ("_CityScale", "City size (bigger = smaller buildings)", "Range(0.5, 4)", "1.5"),
      ("_Horizon", "Street level (0 bottom .. 1 top of screen)", "Range(0, 1)", "0.27"),
      ("_Depth", "Parallax strength", "Range(0, 0.4)", "0.12"),
      ("_Frame", "Window frame width", "Range(0.04, 0.3)", "0.12"),
      ("_GlassTint", "Glass takes the block colour", "Range(0, 0.6)", "0.12"),
      header("Live (set by the game)"),
      ("_Parallax", "Finger offset (x, y), -1..1", "Vector", "(0, 0, 0, 0)"),
      ("_Lit", "Share of windows lit 0..1", "Range(0, 1)", "0.3"),
      ("_Clear", "Row clear 0..1 (flash, then dark)", "Range(0, 1)", "0"),
      ("_Seed", "Seed", "Float", "0")],
     r"""
            half4 fragBody(ArrowVaryings i)
            {
                float2 p = i.uv0 * 2.0 - 1.0;
                float body = RoundBox(p, float2(0.95, 0.95), 0.08);
                float2 s = ScreenPx(i.screen) / _ScreenParams.y;               // screen, height = 1
                float2 par = _Parallax.xy * _Depth;
                // sky, stars, moon (farthest: barely move)
                half3 c = lerp(_SkyBottom.rgb, _SkyTop.rgb, saturate((s.y - _Horizon) / 0.7));
                float2 sp = s + par * 0.1;
                float star = step(0.992, Hash21(floor(sp * 160.0))) * (0.6 + 0.4 * sin(_Time.y * 3.0 + Hash21(floor(sp * 160.0)) * 40.0));
                c += star * 0.8;
                float2 moon = float2(_ScreenParams.x / _ScreenParams.y * 0.66, _Horizon + 0.36) - par * 0.15;
                Paint(c, _MoonColor.rgb, AAInside(Circle(sp - moon, 0.06)));
                c += _MoonColor.rgb * exp(-max(length(sp - moon) - 0.06, 0.0) * 18.0) * 0.25;
                // far skyline (4:1 texture)
                float2 fuv = (s - float2(0, _Horizon + 0.04)) * _CityScale * float2(0.7, 2.8) - par * 0.4;
                if (fuv.y < 1)
                {
                    half4 f = SAMPLE_TEXTURE2D(_FarTex, sampler_FarTex, float2(fuv.x, saturate(fuv.y)));
                    c = lerp(c, f.rgb, fuv.y < 0 ? 1.0 : f.a);
                }
                // buildings with their windows (2:1 textures)
                float2 muv = (s - float2(0, _Horizon - 0.1)) * _CityScale * float2(1.0, 2.0) - par;
                if (muv.y < 1)
                {
                    float2 mm = float2(muv.x + _Seed * 0.0, max(muv.y, 0.02));
                    half4 m = SAMPLE_TEXTURE2D(_MidTex, sampler_MidTex, mm);
                    c = lerp(c, m.rgb, m.a);
                    half4 w = SAMPLE_TEXTURE2D(_WinTex, sampler_WinTex, mm);
                    float flash = smoothstep(0.0, 0.25, _Clear) * (1.0 - smoothstep(0.35, 0.6, _Clear));
                    float on = saturate(step(w.r, _Lit) * (1.0 - smoothstep(0.4, 0.7, _Clear)) + flash);
                    Paint(c, _LightColor.rgb * (0.9 + 0.1 * w.r), w.a * m.a * on);
                }
                // the window we look through: glass tint and a streak, then the frame in the block colour
                c = lerp(c, i.color.rgb, _GlassTint);
                c += smoothstep(0.08, 0.0, abs(p.x + p.y * 0.8 - 0.55)) * 0.12;
                float inner = RoundBox(p, float2(0.95 - _Frame, 0.95 - _Frame), 0.05);
                float mull = min(abs(p.x), abs(p.y)) - _Frame * 0.3;
                float frame = saturate(AAInside(-inner) + AAInside(mull));
                half3 fc = i.color.rgb * lerp(0.8, 1.12, saturate(p.y * 0.5 + 0.5));
                Paint(c, fc, frame);
                Paint(c, INK, AAInside(abs(inner) - 0.02) * 0.5);
                Paint(c, INK, 1.0 - AAInside(body + 0.05));
                return Out(c, AAInside(body), i.color);
            }
""")

emit("SkinChromeBlock",
     "Nah Blocks \"Chrome\": polished metal blocks, anodised in the Image colour. The studio light they reflect slides with your finger, and the dragged piece shows up as a dark reflection when it passes close.",
     [header("Look"),
      ("_Tint", "Anodised tint strength", "Range(0, 1)", "0.7"),
      ("_Bevel", "Bevel width", "Range(0.05, 0.4)", "0.2"),
      ("_EnvTex", "Reflection (matcap, optional)", "2D", "white"),
      ("_UseEnv", "Use the matcap texture", "Range(0, 1)", "0"),
      ("_Sky", "Studio top", C, "(0.95, 0.97, 1, 1)"),
      ("_Floor", "Studio bottom", C, "(0.18, 0.2, 0.26, 1)"),
      ("_Strip", "Light strip brightness", "Range(0, 2)", "1.2"),
      header("Live (set by the game)"),
      ("_Reflect", "Finger offset (x, y), -1..1", "Vector", "(0, 0, 0, 0)"),
      ("_Ghost", "Dragged piece reflection 0..1", "Range(0, 1)", "0"),
      ("_GhostColor", "Dragged piece colour", C, "(0.2, 0.2, 0.3, 1)")],
     r"""
            half3 Studio(float2 v)
            {
                if (_UseEnv > 0.5) return SAMPLE_TEXTURE2D(_EnvTex, sampler_EnvTex, saturate(v * 0.5 + 0.5)).rgb;
                half3 c = lerp(_Floor.rgb, _Sky.rgb, smoothstep(-0.6, 0.7, v.y));
                c += smoothstep(0.09, 0.0, abs(v.y - 0.3 - v.x * 0.15)) * _Strip;
                c += smoothstep(0.05, 0.0, abs(v.y + 0.25 + v.x * 0.1)) * _Strip * 0.5;
                return c;
            }

            half4 fragBody(ArrowVaryings i)
            {
                float2 p = i.uv0 * 2.0 - 1.0;
                float body = RoundBox(p, float2(0.94, 0.94), 0.1);
                float inner = 0.94 - _Bevel;
                float2 ap = abs(p);
                float2 nrm = 0;
                if (ap.x > inner || ap.y > inner) nrm = ap.x > ap.y ? float2(sign(p.x), 0) : float2(0, sign(p.y));
                float2 v = p * 0.35 + nrm * 0.8 - _Reflect.xy * 0.7;
                half3 c = Studio(v);
                // the dragged piece reflected as a dark rounded shape
                float ghost = AAInside(RoundBox(p + _Reflect.xy * 1.6, float2(0.5, 0.5), 0.2) - 0.1) * _Ghost;
                Paint(c, _GhostColor.rgb * 0.6, ghost * 0.85);
                c = lerp(c, c * i.color.rgb * 1.35, _Tint);
                Paint(c, half3(1, 1, 1), AAInside(abs(max(ap.x, ap.y) - inner) - 0.012) * 0.35);
                Paint(c, INK, 1.0 - AAInside(body + 0.05));
                return Out(c, AAInside(body), i.color);
            }
""")

emit("SkinAquariumBlock",
     "Nah Blocks \"Aquarium\": glass blocks of water (tinted by the Image colour) with a sprite fish and a tank bed. The water tilts and sloshes when the block is dragged (_Tilt) or bumped (_Slosh); it drains when the row clears.",
     [header("Look"),
      ("_FishTex", "Fish (2 frames across x 3 kinds down)", "2D", "white"),
      ("_BedTex", "Tank bed (sand, pebbles, weed)", "2D", "white"),
      ("_FishSize", "Fish size", "Range(0.3, 1)", "0.85"),
      ("_WaterTint", "Water takes the block colour", "Range(0, 1)", "0.8"),
      ("_WaterColor", "Water", C, "(0.35, 0.75, 0.95, 1)"),
      ("_AirColor", "Air above the water", C, "(0.92, 0.97, 1, 1)"),
      ("_Level", "Water level", "Range(0, 1)", "0.78"),
      ("_Sway", "Weed sway", "Range(0, 0.1)", "0.03"),
      header("Live (set by the game)"),
      ("_Tilt", "Tilt -1..1", "Range(-1, 1)", "0"),
      ("_Slosh", "Slosh 0..1", "Range(0, 1)", "0"),
      ("_Drain", "Drain (row clear) 0..1", "Range(0, 1)", "0"),
      ("_Seed", "Seed (fish kind and path)", "Float", "0")],
     r"""
            half4 fragBody(ArrowVaryings i)
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
""")

# ============================================================ Bruh Arrows ============================================================
# Arrow strokes: X = uv0.x (cells from the tail), Y = uv0.y * 0.104 (cells across, the line is 0.208 wide), t = uv1.x,
# chevron where uv1.y = 1. GlowPad (in cells) widens the mesh so these skins can draw wider than the line.

ARROW_PRE = r"""
            #define HALF_LINE 0.104
"""

emit("ArrowTrain",
     "Bruh Arrows \"Train\": the arrow is a little train (top view, sprites) of its colour on its track: the engine leads with a signal lamp on its cab (green = the way is clear, red = blocked), the cars bunch up when it bumps, smoke puffs when it leaves. Needs GlowPad 0.22.",
     [header("Look"),
      ("_TrainTex", "Train sprites (engine | car; top row tinted, bottom row details)", "2D", "white"),
      ("_CarLength", "Car length (cells)", "Range(0.3, 1)", "0.6"),
      ("_EngineLength", "Engine length (cells)", "Range(0.4, 1.2)", "0.75"),
      ("_TrainWidth", "Train half width (cells)", "Range(0.12, 0.3)", "0.22"),
      ("_RailColor", "Rails", C, "(0.45, 0.45, 0.5, 1)"),
      ("_SleeperColor", "Sleepers", C, "(0.55, 0.4, 0.28, 1)"),
      ("_GoColor", "Signal: clear", C, "(0.25, 0.95, 0.4, 1)"),
      ("_StopColor", "Signal: blocked", C, "(1, 0.25, 0.25, 1)"),
      ("_SmokeColor", "Smoke", C, "(1, 1, 1, 1)"),
      header("Live (set by the game)"),
      ("_Len", "Arrow length (cells, tail to tip)", "Float", "3"),
      ("_Free", "Way is clear 0..1", "Range(0, 1)", "1"),
      ("_Bump", "Bump 0..1", "Range(0, 1)", "0"),
      ("_Go", "Leaving 0..1", "Range(0, 1)", "0")],
     ARROW_PRE + r"""
            half4 fragBody(ArrowVaryings i)
            {
                if (i.uv1.y > 0.5) return 0;                                  // no chevron: the engine shows the way
                float X = i.uv0.x, Y = i.uv0.y * HALF_LINE;
                half3 col = i.color.rgb;
                float d = _Len - X;                                           // distance back from the nose
                half3 c = 0; float a = 0;
                // track under the train
                float sleeper = AAInside(abs(frac(X / 0.2) - 0.5) * 0.2 - 0.035) * AAInside(abs(Y) - 0.27);
                float rail = AAInside(abs(abs(Y) - 0.15) - 0.018);
                Paint(c, _SleeperColor.rgb, sleeper); a = max(a, sleeper);
                Paint(c, _RailColor.rgb, rail); a = max(a, rail);
                // engine, then whole cars (a bump squeezes the gaps)
                float squeeze = 1.0 - 0.25 * _Bump;
                float engineL = min(_EngineLength, _Len);
                float carD, carL, cell;
                if (d < engineL) { carD = d; carL = engineL; cell = 0; }
                else
                {
                    float cars = max(1.0, round((_Len - engineL) / (_CarLength + 0.08)));
                    float pitch = (_Len - engineL) / cars * squeeze;
                    float k = floor((d - engineL) / pitch);
                    carD = d - engineL - k * pitch - 0.08 * squeeze;
                    carL = pitch - 0.08 * squeeze;
                    cell = 1;
                }
                float2 uv = float2(1.0 - carD / carL, Y / _TrainWidth * 0.5 + 0.5);
                if (all(uv > 0) && all(uv < 1) && X > -0.2 && d > 0)
                {
                    float ux = (cell + clamp(uv.x, 0.004, 0.996)) / 2.0;
                    half4 base = SAMPLE_TEXTURE2D(_TrainTex, sampler_TrainTex, float2(ux, 0.5 + uv.y * 0.5));
                    half4 det = SAMPLE_TEXTURE2D(_TrainTex, sampler_TrainTex, float2(ux, uv.y * 0.5));
                    half3 tc = base.rgb * col;
                    tc = lerp(tc, det.rgb, det.a);
                    float ta = max(base.a, det.a);
                    if (cell < 0.5)                                           // signal lamp on the cab roof
                    {
                        float2 lp = float2(carD - engineL * 0.8, Y);
                        half3 lamp = lerp(_StopColor.rgb, _GoColor.rgb, _Free);
                        Paint(tc, INK, AAInside(Circle(lp, 0.075)));
                        Paint(tc, lamp, AAInside(Circle(lp, 0.055)));
                        Paint(tc, half3(1, 1, 1), AAInside(Circle(lp - float2(0.015, 0.02), 0.018)) * 0.8);
                    }
                    c = lerp(c, tc, ta); a = max(a, ta);
                }
                // lamp glow
                float glow = exp(-length(float2(d - engineL * 0.8, Y)) * 9.0) * 0.5 * (1.0 - a);
                c += lerp(_StopColor.rgb, _GoColor.rgb, _Free) * glow; a = max(a, glow);
                // smoke puffs drift back over the train while it leaves
                [unroll] for (int s = 0; s < 4; s++)
                {
                    float age = frac(_Time.y * 1.6 + s * 0.25);
                    float2 spos = float2(engineL * 0.35 + age * 1.1, sin(age * 6.0 + s) * 0.06);
                    float pr = 0.05 + age * 0.12;
                    float puff = AAInside(Circle(float2(d, Y) - spos, pr)) * (1.0 - age) * _Go;
                    Paint(c, _SmokeColor.rgb, puff); a = max(a, puff * 0.9);
                }
                return half4(c * a * i.color.a, a * i.color.a);
            }
""")

emit("ArrowTape",
     "Bruh Arrows \"Tape\": the arrow is a strip of coloured tape. A free strip lifts off the paper (it casts a shadow), ready to grab; tapping peels it off from the tail (_Peel); a blocked one peels part-way and sticks back down. Needs GlowPad 0.12.",
     [header("Look"),
      ("_TapeWidth", "Tape half width (cells)", "Range(0.08, 0.25)", "0.17"),
      ("_Opacity", "Tape opacity", "Range(0.5, 1)", "0.9"),
      ("_Fibres", "Paper fibres", "Range(0, 0.3)", "0.12"),
      ("_Teeth", "Torn edge teeth", "Range(0, 0.06)", "0.03"),
      ("_UnderColor", "Underside (glue side)", C, "(1, 1, 1, 1)"),
      ("_Lift", "Shadow offset when free (cells)", "Range(0, 0.12)", "0.06"),
      header("Live (set by the game)"),
      ("_Len", "Arrow length (cells, tail to tip)", "Float", "3"),
      ("_Free", "Free (lifted) 0..1", "Range(0, 1)", "1"),
      ("_Peel", "Peeled 0..1 (tail to head)", "Range(0, 1)", "0")],
     ARROW_PRE + r"""
            half4 fragBody(ArrowVaryings i)
            {
                float X = i.uv0.x, Y = i.uv0.y * HALF_LINE;
                half3 col = i.color.rgb;
                float W = _TapeWidth;
                float chevron = step(0.5, i.uv1.y);
                // torn tail end: a zigzag across the tape
                float teeth = abs(frac(Y * 18.0) - 0.5) * 2.0 * _Teeth;
                float peelX = _Peel * (_Len + 0.5) - 0.12;
                float body = chevron > 0.5 ? abs(Y) - W * 0.62 : max(abs(Y) - W, -(X + 0.12 - teeth));
                float edge = max(body, -(X - peelX));                          // the peeled part is gone
                float tape = AAInside(edge);
                // lifted: a soft shadow beside the strip
                float lift = _Lift * _Free;
                float shadow = AAInside(max(abs(Y + lift) - W * lerp(1.0, 0.62, chevron), -(X - peelX) - 0.02) - 0.02) * (1.0 - tape) * 0.22 * _Free;
                half3 c = col * (0.95 + 0.05 * sin(Y * 40.0));
                c *= 1.0 - _Fibres * ValueNoise(float2(X * 30.0, Y * 4.0));
                c = lerp(c, half3(1, 1, 1), smoothstep(0.06, 0.0, abs(Y + W * 0.3)) * (0.12 + 0.12 * _Free));
                // the curl at the peel front: the lighter glue side, a thin shadow ahead
                float front = X - peelX;
                float peeling = step(0.001, _Peel);
                Paint(c, lerp(_UnderColor.rgb, col, 0.35), (1.0 - smoothstep(0.0, 0.14, front)) * peeling);
                c *= 1.0 - smoothstep(0.08, 0.0, abs(front - 0.16)) * 0.35 * peeling;
                Paint(c, col * 0.55, AAInside(abs(edge) - 0.01) * 0.35);
                float a = tape * lerp(_Opacity, 1.0, chevron);                // the chevron's arms overlap: keep it solid
                shadow *= 1.0 - chevron;
                c = c * a;                                                     // premultiply, then lay the shadow under
                c += half3(0, 0, 0) * shadow;
                a = a + shadow * (1.0 - a);
                return half4(c * i.color.a, a * i.color.a);
            }
""")

emit("ArrowAnts",
     "Bruh Arrows \"Ants\": the arrow is a column of sprite ants on a scent trail of its colour. Free columns walk in place; a blocked column bunches up and fidgets at the front; leaving, they march off fast. Needs GlowPad 0.18.",
     [header("Look"),
      ("_AntTex", "Ant walk frames (side by side, facing right)", "2D", "white"),
      ("_Frames", "Frames in the strip", "Float", "4"),
      ("_AntTint", "Ants take the arrow colour", "Range(0, 1)", "0.3"),
      ("_TrailAlpha", "Scent trail strength", "Range(0, 1)", "0.45"),
      ("_Spacing", "Ant spacing (cells)", "Range(0.2, 0.8)", "0.48"),
      ("_AntSize", "Ant length (cells)", "Range(0.15, 0.6)", "0.44"),
      header("Live (set by the game)"),
      ("_Len", "Arrow length (cells, tail to tip)", "Float", "3"),
      ("_Free", "Free 0..1", "Range(0, 1)", "1"),
      ("_March", "Walk cycle phase", "Float", "0")],
     ARROW_PRE + r"""
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
""")

emit("ArrowZipper",
     "Bruh Arrows \"Zipper\": the arrow is a zip on fabric of its colour. The slider (the face cap) runs from the tail to the head (_Unzip); behind it the teeth part and the zip vanishes. Blocked: the slider jams. Needs GlowPad 0.16.",
     [header("Look"),
      ("_FabricWidth", "Fabric half width (cells)", "Range(0.1, 0.3)", "0.2"),
      ("_TeethColor", "Teeth", C, "(0.85, 0.85, 0.9, 1)"),
      ("_ToothPitch", "Tooth pitch (cells)", "Range(0.03, 0.12)", "0.06"),
      ("_Weave", "Fabric weave", "Range(0, 0.3)", "0.12"),
      header("Live (set by the game)"),
      ("_Len", "Arrow length (cells, tail to tip)", "Float", "3"),
      ("_Unzip", "Unzipped 0..1 (tail to head)", "Range(0, 1)", "0")],
     ARROW_PRE + r"""
            half4 fragBody(ArrowVaryings i)
            {
                float X = i.uv0.x, Y = i.uv0.y * HALF_LINE;
                half3 col = i.color.rgb;
                float W = _FabricWidth;
                float front = _Unzip * (_Len + 0.2) - 0.1;
                float behind = front - X;                                     // > 0: already unzipped
                float part = saturate(behind / 0.5);
                // the two halves slide apart behind the slider and fade out
                float side = Y >= 0 ? 1.0 : -1.0;
                float y = Y - side * part * 0.12;
                float fade = 1.0 - saturate((behind - 0.25) / 0.5);
                float fabric = AAInside(max(abs(y) - W, -X - 0.1));
                if (i.uv1.y > 0.5) fabric = AAInside(abs(Y) - HALF_LINE * 1.1);    // chevron: plain fabric, line-wide arms
                half3 c = col * (1.0 - _Weave * (step(0.5, frac(X * 40.0)) * 0.5 + step(0.5, frac(Y * 40.0)) * 0.5));
                // stitch lines
                Paint(c, col * 0.6, AAInside(abs(abs(y) - W * 0.82) - 0.008) * step(0.5, frac(X * 12.0)));
                // teeth alternate sides along the middle
                float kx = X / _ToothPitch;
                float alt = step(0.5, frac(kx * 0.5));
                float toothSide = alt > 0.5 ? 1.0 : -1.0;
                float2 tq = float2((frac(kx) - 0.5) * _ToothPitch, y - toothSide * 0.03 + side * 0.0);
                float tooth = AAInside(RoundBox(tq, float2(_ToothPitch * 0.32, 0.045), 0.01)) * step(0.0, toothSide * Y + (1.0 - part) * 0.06);
                tooth *= step(i.uv1.y, 0.5) * step(X, _Len - 0.25);
                Paint(c, INK, AAInside(RoundBox(tq, float2(_ToothPitch * 0.32, 0.045), 0.01) - 0.012) * tooth);
                Paint(c, _TeethColor.rgb * (0.85 + 0.3 * step(0.0, tq.y)), tooth);
                float a = fabric * (behind > 0 ? fade : 1.0);
                a = max(a, tooth * fade);
                Paint(c, INK, AAInside(abs(abs(y) - W) - 0.012) * 0.4);
                return half4(c * a * i.color.a, a * i.color.a);
            }
""")
print("done")
