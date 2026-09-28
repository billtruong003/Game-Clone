using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace References.Study
{
    /// <summary>
    /// Study tool for reference VFX packs (never used by the games): dumps every ParticleSystem setting of a prefab
    /// and renders the effect at chosen times into a contact strip, in edit mode, far away from the open scene.
    /// </summary>
    public static class FxStudy
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private static string F(float v) => v.ToString("0.###", Inv);

        private static string Curve(ParticleSystem.MinMaxCurve c)
        {
            switch (c.mode)
            {
                case ParticleSystemCurveMode.Constant: return F(c.constant);
                case ParticleSystemCurveMode.TwoConstants: return $"{F(c.constantMin)}..{F(c.constantMax)}";
                case ParticleSystemCurveMode.Curve: return $"x{F(c.curveMultiplier)} [{Keys(c.curve)}]";
                default: return $"x{F(c.curveMultiplier)} [{Keys(c.curveMin)}]..[{Keys(c.curveMax)}]";
            }
        }

        private static string Keys(AnimationCurve a) => a == null ? "" : string.Join(" ", a.keys.Select(k => $"{F(k.time)}:{F(k.value)}"));

        private static string Grad(ParticleSystem.MinMaxGradient g)
        {
            string G(Gradient x) => x == null ? "" :
                "rgb{" + string.Join(" ", x.colorKeys.Select(k => $"{F(k.time)}:#{ColorUtility.ToHtmlStringRGB(k.color)}")) + "} a{" +
                string.Join(" ", x.alphaKeys.Select(k => $"{F(k.time)}:{F(k.alpha)}")) + "}";
            switch (g.mode)
            {
                case ParticleSystemGradientMode.Color: return "#" + ColorUtility.ToHtmlStringRGBA(g.color);
                case ParticleSystemGradientMode.TwoColors: return $"#{ColorUtility.ToHtmlStringRGBA(g.colorMin)}..#{ColorUtility.ToHtmlStringRGBA(g.colorMax)}";
                case ParticleSystemGradientMode.Gradient: return G(g.gradient);
                case ParticleSystemGradientMode.TwoGradients: return G(g.gradientMin) + " .. " + G(g.gradientMax);
                default: return "random " + G(g.gradient);
            }
        }

        public static string Dump(string prefabPath)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var sb = new StringBuilder();
            sb.AppendLine($"# {Path.GetFileNameWithoutExtension(prefabPath)}  ({prefabPath})");
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
            {
                var m = ps.main;
                var t = ps.transform;
                sb.AppendLine($"## {HierarchyPath(t, go.transform)}  pos{t.localPosition} rot{t.localEulerAngles} scale{t.localScale}");
                sb.AppendLine($"  main: dur {F(m.duration)} loop {m.loop} delay {Curve(m.startDelay)} life {Curve(m.startLifetime)} speed {Curve(m.startSpeed)} size {Curve(m.startSize)}" +
                              $" rot {Curve(m.startRotation)} grav {Curve(m.gravityModifier)} space {m.simulationSpace} max {m.maxParticles} color {Grad(m.startColor)}");
                var e = ps.emission;
                if (e.enabled)
                {
                    var bursts = new ParticleSystem.Burst[e.burstCount];
                    e.GetBursts(bursts);
                    sb.AppendLine($"  emission: rate {Curve(e.rateOverTime)} bursts [{string.Join(", ", bursts.Select(b => $"t{F(b.time)} n{Curve(b.count)} x{b.cycleCount}"))}]");
                }
                var sh = ps.shape;
                if (sh.enabled) sb.AppendLine($"  shape: {sh.shapeType} r {F(sh.radius)} thick {F(sh.radiusThickness)} angle {F(sh.angle)} arc {F(sh.arc)} rot {sh.rotation} scale {sh.scale}");
                var vol = ps.velocityOverLifetime;
                if (vol.enabled) sb.AppendLine($"  velocity: x {Curve(vol.x)} y {Curve(vol.y)} z {Curve(vol.z)} radial {Curve(vol.radial)} orbital {Curve(vol.orbitalZ)} space {vol.space}");
                var lim = ps.limitVelocityOverLifetime;
                if (lim.enabled) sb.AppendLine($"  limitVelocity: limit {Curve(lim.limit)} dampen {F(lim.dampen)} drag {Curve(lim.drag)}");
                var col = ps.colorOverLifetime;
                if (col.enabled) sb.AppendLine($"  colorOverLife: {Grad(col.color)}");
                var sz = ps.sizeOverLifetime;
                if (sz.enabled) sb.AppendLine($"  sizeOverLife: {(sz.separateAxes ? $"x {Curve(sz.x)} y {Curve(sz.y)}" : Curve(sz.size))}");
                var rot = ps.rotationOverLifetime;
                if (rot.enabled) sb.AppendLine($"  rotationOverLife: {Curve(rot.z)}");
                var n = ps.noise;
                if (n.enabled) sb.AppendLine($"  noise: strength {Curve(n.strength)} freq {F(n.frequency)} scroll {Curve(n.scrollSpeed)} damping {n.damping} octaves {n.octaveCount}");
                var ts = ps.textureSheetAnimation;
                if (ts.enabled) sb.AppendLine($"  textureSheet: {ts.mode} {ts.numTilesX}x{ts.numTilesY} anim {ts.animation} frame {Curve(ts.frameOverTime)} start {Curve(ts.startFrame)} cycles {F(ts.cycleCount)}");
                var sub = ps.subEmitters;
                if (sub.enabled && sub.subEmittersCount > 0) sb.AppendLine($"  subEmitters: {sub.subEmittersCount}");
                var tr = ps.trails;
                if (tr.enabled) sb.AppendLine($"  trails: ratio {F(tr.ratio)} life {Curve(tr.lifetime)} width {Curve(tr.widthOverTrail)} color {Grad(tr.colorOverLifetime)}");
                var r = ps.GetComponent<ParticleSystemRenderer>();
                if (r != null && r.enabled)
                {
                    var mat = r.sharedMaterial;
                    string tex = mat != null && mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null ? mat.GetTexture("_BaseMap").name
                               : mat != null && mat.mainTexture != null ? mat.mainTexture.name : "-";
                    string blend = mat == null ? "-" : mat.HasProperty("_Blend") ? BlendName(mat.GetFloat("_Blend")) : "?";
                    string tint = mat != null && mat.HasProperty("_BaseColor") ? "#" + ColorUtility.ToHtmlStringRGBA(mat.GetColor("_BaseColor")) : "";
                    sb.AppendLine($"  renderer: {r.renderMode} mat {(mat ? mat.name : "-")} tex {tex} blend {blend} tint {tint} order {r.sortingOrder} fudge {F(r.sortingFudge)}" +
                                  (r.renderMode == ParticleSystemRenderMode.Stretch ? $" lengthScale {F(r.lengthScale)} velScale {F(r.velocityScale)}" : "") +
                                  (r.renderMode == ParticleSystemRenderMode.Mesh && r.mesh ? $" mesh {r.mesh.name}" : ""));
                }
            }
            return sb.ToString();
        }

        private static string BlendName(float b) => b switch { 0 => "Alpha", 1 => "Premultiply", 2 => "Additive", 3 => "Multiply", _ => F(b) };

        private static string HierarchyPath(Transform t, Transform root)
        {
            var parts = new List<string>();
            for (var x = t; x != null && x != root.parent; x = x.parent) parts.Insert(0, x.name);
            return string.Join("/", parts);
        }

        /// <summary>Renders the prefab at each time into one horizontal strip (cell px square). Returns the output path.</summary>
        public static string Render(string prefabPath, float[] times, string outPng, int cell = 360, float viewSize = 0f, Color? bg = null)
        {
            var far = new Vector3(5000f, 5000f, 0f);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
            inst.hideFlags = HideFlags.DontSave;
            inst.transform.position = far;
            var camGo = new GameObject("FxStudyCam") { hideFlags = HideFlags.HideAndDontSave };
            var cam = camGo.AddComponent<Camera>();
            cam.enabled = false;
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bg ?? new Color(0.169f, 0.184f, 0.333f);
            cam.transform.position = far + new Vector3(0f, 0f, -50f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;

            var systems = inst.GetComponentsInChildren<ParticleSystem>(true);
            var roots = systems.Where(p => p.transform.parent == null || p.transform.parent.GetComponentInParent<ParticleSystem>() == null).ToArray();
            // auto-fit: bounds of everything over all times
            if (viewSize <= 0f)
            {
                var b = new Bounds(far, Vector3.one * 0.5f);
                foreach (var tm in times)
                {
                    foreach (var p in roots) p.Simulate(tm, true, true, true);
                    foreach (var r in inst.GetComponentsInChildren<ParticleSystemRenderer>()) if (r.bounds.size.sqrMagnitude > 0) b.Encapsulate(r.bounds);
                }
                var ext = Mathf.Max(b.extents.x, b.extents.y);
                cam.transform.position = new Vector3(b.center.x, b.center.y, far.z - 50f);
                viewSize = Mathf.Clamp(ext * 1.1f, 0.5f, 40f);
            }
            cam.orthographicSize = viewSize;

            var strip = new Texture2D(cell * times.Length, cell, TextureFormat.RGB24, false);
            var rt = new RenderTexture(cell, cell, 24);
            var tex = new Texture2D(cell, cell, TextureFormat.RGB24, false);
            for (int i = 0; i < times.Length; i++)
            {
                foreach (var p in roots) p.Simulate(times[i], true, true, true);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, cell, cell), 0, 0);
                tex.Apply();
                strip.SetPixels(i * cell, 0, cell, cell, tex.GetPixels());
            }
            cam.targetTexture = null;
            RenderTexture.active = null;
            strip.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(outPng)!);
            File.WriteAllBytes(outPng, strip.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(strip);
            rt.Release();
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(inst);
            return outPng + $"  (view {F(viewSize)})";
        }
    }
}
