using System.IO;
using CasualGame.Core;
using CasualGame.Sandbox;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CasualGame.EditorTools
{
    /// <summary>
    /// Tools/Casual Game/FX Sandbox: creates (only when missing, never overwriting your tuning) the sandbox scene and the
    /// DRAFT effect prefabs built on the ToonParticle shader, then opens the scene.
    /// "Rebuild Toon Drafts" deletes and regenerates only the draft prefabs listed below.
    /// </summary>
    public static class FxSandboxBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/Sandbox/FxSandbox.unity";
        /// <summary>Own ToonParticle drafts, kept out of the sandbox list (the games use the Epic Toon FX picks, see EtfxPicks).</summary>
        public const string DraftsFolder = "Assets/_Game/Fx/ToonDrafts";

        [MenuItem("Tools/Casual Game/FX Sandbox")]
        public static void Open()
        {
            if (ToonFxBuilder.Material("Toon_Puff") == null) ToonFxBuilder.BuildAll();
            Directory.CreateDirectory(FxSandbox.EffectsFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)!);
            EtfxPicks.Build();
            int made = 0;
            if (!File.Exists(ScenePath)) BuildScene();
            else EditorSceneManager.OpenScene(ScenePath);
            SceneBuilder.GameViewPortrait();
            Debug.Log($"FX Sandbox: {made} draft prefab(s) created in {FxSandbox.EffectsFolder}. Enter Play mode and click.");
        }

        [MenuItem("Tools/Casual Game/Toon FX/Rebuild Toon Drafts (overwrites the draft prefabs)")]
        public static void RebuildDrafts()
        {
            ToonFxBuilder.BuildAll();
            Debug.Log($"FX Sandbox: {MakeDrafts(true)} toon draft prefab(s) rebuilt.");
        }

        private static int MakeDrafts(bool overwrite)
        {
            int made = 0;
            foreach (var (name, build) in Drafts)
            {
                Directory.CreateDirectory(DraftsFolder);
                var path = $"{DraftsFolder}/{name}.prefab";
                if (File.Exists(path))
                {
                    if (!overwrite) continue;
                    AssetDatabase.DeleteAsset(path);
                }
                var root = new GameObject(name);
                var fx = root.AddComponent<FxEffect>();
                build(root.transform, new SerializedObject(fx));
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
                made++;
            }
            AssetDatabase.Refresh();
            return made;
        }

        private static void BuildScene()
        {
            var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = UIKit.Reference.y / 200f;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UIKit.Hex("#2B2F55");

            var go = new GameObject("FxSandbox");
            var sandbox = go.AddComponent<FxSandbox>();
            var merge = go.AddComponent<MergeChainDemo>();
            var line = go.AddComponent<LineClearDemo>();
            var drop = go.AddComponent<DropDemo>();
            var jar = go.AddComponent<GlassJarDemo>();
            var ink = go.AddComponent<InkArrowDemo>();
            Wire(sandbox, "mergeChain", merge);
            Wire(sandbox, "lineClear", line);
            Wire(merge, "sandbox", sandbox);
            Wire(line, "sandbox", sandbox);
            Wire(sandbox, "drop", drop);
            Wire(drop, "sandbox", sandbox);
            Wire(sandbox, "jar", jar);
            Wire(jar, "sandbox", sandbox);
            Wire(sandbox, "ink", ink);
            Wire(ink, "sandbox", sandbox);
            GlassRendererSetup.Setup(); // "Glass" sorting layer for the jar glass
            EditorSceneManager.SaveScene(s, ScenePath);
        }

        private static void Wire(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- toon draft effects (starting points: reshape them in the inspector) ----------
        // Recipe learned from the Epic Toon FX reference (Assets/References, study notes in Docs/References/ETFX_NOTES.md):
        // glow halo + many small body puffs + thin fast sparks + slow secondary bits (+ a hero shape);
        // wide random speed/lifetime with dampening; puffs SHRINK to nothing and only fade in the last 10%;
        // colors cool down over life.

        private class Sys
        {
            public string name, mat = "Toon_Puff";
            public bool tint = true, glow, behind;
            public Color color = Color.white;
            public Gradient randomColors, overLife;
            public int count = 8, order;
            public float rate, duration = 0.2f;   // rate > 0 = continuous emission for `duration` seconds
            public float delay, lifeMin = 0.4f, lifeMax = 0.6f, speedMin, speedMax, sizeMin = 0.3f, sizeMax = 0.5f;
            public float aspect = 1f;             // height / width (> 1 = tall, e.g. flame tongues)
            public float gravity, dampen, radius = 0.1f, cone = -1f, spin, stretch, lengthScale = 1.2f, noise, offsetY, startRot = Mathf.PI;
            public AnimationCurve sizeCurve;
            public AnimationCurve erode;           // Custom1.x over lifetime: 0 = nominal shape, ~0.7 = fully eroded
            public float rampRow;                  // Custom1.y: added to the material ramp row (one row = 1/8)
        }

        private static AnimationCurve Curve(params float[] timeValue)
        {
            var keys = new Keyframe[timeValue.Length / 2];
            for (int i = 0; i < keys.Length; i++) keys[i] = new Keyframe(timeValue[i * 2], timeValue[i * 2 + 1]);
            var c = new AnimationCurve(keys);
            for (int i = 0; i < keys.Length; i++) c.SmoothTangents(i, 0f);
            return c;
        }

        private static Gradient Grad(float fadeFrom, params (float t, string hex)[] colors)
        {
            var g = new Gradient();
            var keys = new GradientColorKey[colors.Length];
            for (int i = 0; i < colors.Length; i++) keys[i] = new GradientColorKey(UIKit.Hex(colors[i].hex), colors[i].t);
            g.SetKeys(keys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, fadeFrom), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        private static Gradient FadeInOut(string hex, float inEnd = 0.2f)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(UIKit.Hex(hex), 0f), new GradientColorKey(UIKit.Hex(hex), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, inEnd), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        // pop in with a little overshoot, hold, then shrink to nothing (the softer ETFX way of disappearing)
        private static AnimationCurve PopShrink => Curve(0f, 0.4f, 0.12f, 1.1f, 0.3f, 1f, 1f, 0f);
        private static AnimationCurve Shrink => Curve(0f, 1f, 0.2f, 1f, 1f, 0f);
        private static AnimationCurve Twinkle => Curve(0f, 0f, 0.3f, 1f, 0.53f, 0.15f, 0.65f, 0.7f, 1f, 0f);
        private static AnimationCurve ErodeSoft => Curve(0f, -0.06f, 0.6f, -0.02f, 1f, 0.3f);
        private static AnimationCurve ErodeNone => Curve(0f, -0.04f, 1f, -0.04f);
        private static Gradient CoolDown => Grad(0.9f, (0f, "#FFFFFF"), (1f, "#C9CCE0"));
        private static Gradient Keep => Grad(0.9f, (0f, "#FFFFFF"), (1f, "#FFFFFF"));

        private static void Add(Transform root, SerializedObject fx, params Sys[] systems)
        {
            var tintList = fx.FindProperty("tintable");
            foreach (var d in systems)
            {
                var go = new GameObject(d.name);
                go.transform.SetParent(root, false);
                var ps = go.AddComponent<ParticleSystem>();
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                var main = ps.main;
                main.playOnAwake = false;
                main.loop = false;
                main.duration = d.rate > 0f ? d.duration : 0.2f;
                main.startDelay = d.delay;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.startLifetime = new ParticleSystem.MinMaxCurve(d.lifeMin, d.lifeMax);
                main.startSpeed = new ParticleSystem.MinMaxCurve(d.speedMin, d.speedMax);
                if (Mathf.Approximately(d.aspect, 1f)) main.startSize = new ParticleSystem.MinMaxCurve(d.sizeMin, d.sizeMax);
                else
                {
                    main.startSize3D = true;
                    main.startSizeX = new ParticleSystem.MinMaxCurve(d.sizeMin, d.sizeMax);
                    main.startSizeY = new ParticleSystem.MinMaxCurve(d.sizeMin * d.aspect, d.sizeMax * d.aspect);
                    main.startSizeZ = 1f;
                }
                main.startRotation = d.stretch > 0f || d.startRot <= 0f ? new ParticleSystem.MinMaxCurve(0f) : new ParticleSystem.MinMaxCurve(-d.startRot, d.startRot);
                main.gravityModifier = d.gravity;
                main.startColor = d.randomColors != null
                    ? new ParticleSystem.MinMaxGradient(d.randomColors) { mode = ParticleSystemGradientMode.RandomColor }
                    : new ParticleSystem.MinMaxGradient(d.color);

                var emission = ps.emission;
                if (d.rate > 0f) emission.rateOverTime = d.rate;
                else
                {
                    emission.rateOverTime = 0f;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)d.count) });
                }

                var shape = ps.shape;
                shape.position = new Vector3(0f, d.offsetY, 0f);
                if (d.cone >= 0f)
                {
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = d.cone;
                    shape.radius = Mathf.Max(0.0001f, d.radius);
                    shape.rotation = new Vector3(-90f, 0f, 0f); // cone opens toward +Y (up on screen)
                }
                else
                {
                    shape.shapeType = ParticleSystemShapeType.Circle;
                    shape.radius = Mathf.Max(0.0001f, d.radius);
                }

                if (d.dampen > 0f)
                {
                    var limit = ps.limitVelocityOverLifetime;
                    limit.enabled = true;
                    limit.limit = 0f;
                    limit.dampen = d.dampen;
                }
                if (d.sizeCurve != null)
                {
                    var size = ps.sizeOverLifetime;
                    size.enabled = true;
                    size.size = new ParticleSystem.MinMaxCurve(1f, d.sizeCurve);
                }
                if (d.spin != 0f)
                {
                    var rot = ps.rotationOverLifetime;
                    rot.enabled = true;
                    rot.z = new ParticleSystem.MinMaxCurve(-d.spin, d.spin);
                }
                if (d.overLife != null)
                {
                    var col = ps.colorOverLifetime;
                    col.enabled = true;
                    col.color = new ParticleSystem.MinMaxGradient(d.overLife);
                }
                if (d.noise > 0f)
                {
                    var n = ps.noise;
                    n.enabled = true;
                    n.strength = d.noise;
                    n.frequency = 1f;
                    n.damping = true;
                }

                var r = go.GetComponent<ParticleSystemRenderer>();
                r.sharedMaterial = ToonFxBuilder.Material(d.glow ? "Soft_Glow" : d.mat);
                r.sortingOrder = (d.behind ? BehindOrder : Fx.SortingOrder) + d.order;
                if (d.stretch > 0f)
                {
                    r.renderMode = ParticleSystemRenderMode.Stretch;
                    r.velocityScale = d.stretch;
                    r.lengthScale = d.lengthScale;
                }
                if (!d.glow) ToonFxBuilder.ConfigureStreams(ps, d.erode ?? ErodeSoft, d.rampRow);

                if (d.tint)
                {
                    tintList.arraySize++;
                    tintList.GetArrayElementAtIndex(tintList.arraySize - 1).objectReferenceValue = ps;
                }
            }
            fx.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Gradient PaletteGradient()
        {
            var g = new Gradient();
            var p = SandboxArt.Palette;
            var keys = new GradientColorKey[p.Length];
            for (int i = 0; i < p.Length; i++) keys[i] = new GradientColorKey(p[i], i / (p.Length - 1f));
            g.SetKeys(keys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        /// <summary>Below characters (sandbox bodies sort at 10+), above the background.</summary>
        private const int BehindOrder = 2;

        private static readonly Color Cream = new(1f, 0.973f, 0.925f, 1f);

        // Glow halo: short, big, additive, tinted.
        private static Sys Glow(string name, float size, float life, bool tint = true, string hex = "#FFFFFF", bool behind = true) =>
            new() { name = name, glow = true, tint = tint, color = UIKit.Hex(hex), count = 1, lifeMin = life, lifeMax = life, sizeMin = size, sizeMax = size, radius = 0f,
                    behind = behind, order = -2, startRot = 0f, sizeCurve = Curve(0f, 0.6f, 0.25f, 1f, 1f, 0.8f), overLife = Grad(0f, (0f, "#FFFFFF"), (1f, "#FFFFFF")) };

        // Thin fast sparks (stretched drops), dampened.
        private static Sys Sparks(int count, float speedMax, bool tint, string hex = "#FFFFFF", int order = 1) =>
            new() { name = "sparks", mat = "Toon_Drop", tint = tint, color = UIKit.Hex(hex), count = count, lifeMin = 0.2f, lifeMax = 0.5f, speedMin = speedMax * 0.45f, speedMax = speedMax,
                    sizeMin = 0.1f, sizeMax = 0.16f, dampen = 0.25f, radius = 0.2f, order = order, stretch = 0.02f, lengthScale = 1.5f, sizeCurve = Shrink, erode = ErodeNone };

        private static readonly (string, System.Action<Transform, SerializedObject>)[] Drafts =
        {
            // Merge: poof of many small puffs, the new ball pops out of it (ETFX StarPoof structure)
            ("Merge_Splat", (t, fx) => Add(t, fx,
                Glow("glow", 3.2f, 0.45f),
                new Sys { name = "puffs", count = 18, lifeMin = 0.4f, lifeMax = 0.9f, speedMin = 2.2f, speedMax = 6.5f, sizeMin = 0.55f, sizeMax = 1f, dampen = 0.2f, radius = 0.25f,
                          spin = Mathf.PI, behind = true, sizeCurve = PopShrink, overLife = CoolDown, erode = ErodeSoft },
                Sparks(16, 13f, true),
                new Sys { name = "tiny_stars", mat = "Toon_Star", tint = false, color = UIKit.Hex("#FFF4C2"), count = 6, lifeMin = 0.7f, lifeMax = 1.2f, speedMin = 0.8f, speedMax = 2.5f,
                          sizeMin = 0.28f, sizeMax = 0.42f, gravity = -0.1f, dampen = 0.15f, radius = 0.45f, spin = 2f, order = 2,
                          sizeCurve = Curve(0f, 0f, 0.15f, 1f, 0.75f, 1f, 1f, 0f), overLife = Keep, erode = ErodeNone },
                new Sys { name = "flash_star", mat = "Toon_Star", tint = false, count = 1, lifeMin = 0.1f, lifeMax = 0.1f, sizeMin = 1f, sizeMax = 1f, radius = 0f, order = 3, startRot = 0f,
                          sizeCurve = Curve(0f, 0.3f, 0.35f, 1.1f, 1f, 0.5f), erode = ErodeNone })),
            // Merge: goo drops that arc and fall
            ("Merge_Droplets", (t, fx) => Add(t, fx,
                new Sys { name = "drops", mat = "Toon_Drop", count = 10, lifeMin = 0.4f, lifeMax = 0.7f, speedMin = 4f, speedMax = 9f, sizeMin = 0.15f, sizeMax = 0.26f, gravity = 1.4f,
                          dampen = 0.15f, radius = 0.3f, order = 2, stretch = 0.03f, sizeCurve = Shrink, overLife = Keep, erode = ErodeNone })),
            // Blast: white hit flash, a few colored clouds, sticker shards, thin sparks (ETFX HitDust structure)
            ("Blast_BlockPop", (t, fx) => Add(t, fx,
                Glow("glow", 2.2f, 0.2f, tint: false, behind: false),
                new Sys { name = "clouds", count = 6, lifeMin = 0.3f, lifeMax = 0.55f, speedMin = 1f, speedMax = 3f, sizeMin = 0.45f, sizeMax = 0.75f, dampen = 0.2f, radius = 0.2f,
                          spin = Mathf.PI, sizeCurve = PopShrink, overLife = CoolDown },
                new Sys { name = "shards", mat = "Toon_Shard", count = 6, lifeMin = 0.45f, lifeMax = 0.75f, speedMin = 3f, speedMax = 7f, sizeMin = 0.26f, sizeMax = 0.38f, gravity = 2f,
                          dampen = 0.1f, spin = 10f, radius = 0.3f, order = 1, sizeCurve = Curve(0f, 1f, 0.7f, 1f, 1f, 0f), overLife = Keep, erode = ErodeNone },
                Sparks(10, 11f, false))),
            ("Blast_PlaceDust", (t, fx) => Add(t, fx,
                new Sys { name = "dust", tint = false, color = Cream, count = 7, lifeMin = 0.3f, lifeMax = 0.5f, speedMin = 0.8f, speedMax = 2.2f, sizeMin = 0.45f, sizeMax = 0.7f,
                          dampen = 0.3f, radius = 0.45f, spin = Mathf.PI, sizeCurve = PopShrink, overLife = CoolDown })),
            ("Arrow_ExitPuff", (t, fx) => Add(t, fx,
                new Sys { name = "puffs", count = 8, lifeMin = 0.3f, lifeMax = 0.55f, speedMin = 1f, speedMax = 3.5f, sizeMin = 0.35f, sizeMax = 0.6f, dampen = 0.2f, radius = 0.15f,
                          spin = Mathf.PI, sizeCurve = PopShrink, overLife = CoolDown },
                Sparks(6, 8f, true))),
            ("Ring_Shock", (t, fx) => Add(t, fx,
                Glow("glow", 1.8f, 0.18f, behind: false),
                new Sys { name = "ring", mat = "Toon_Ring", count = 1, lifeMin = 0.35f, lifeMax = 0.35f, sizeMin = 3.5f, sizeMax = 3.5f, radius = 0f, startRot = 0f,
                          sizeCurve = Curve(0f, 0.2f, 1f, 1.3f), erode = Curve(0f, -0.08f, 1f, 0.55f) })),
            // double-pulse twinkle + halo (ETFX SparkleSolo structure)
            ("Sparkle_Glint", (t, fx) => Add(t, fx,
                Glow("glow", 1.3f, 0.4f, tint: false, hex: "#FFF2A0", behind: false),
                new Sys { name = "glint", mat = "Toon_Star", tint = false, count = 1, lifeMin = 0.4f, lifeMax = 0.4f, sizeMin = 0.9f, sizeMax = 0.9f, radius = 0f, startRot = 0f,
                          sizeCurve = Twinkle, overLife = Grad(1f, (0.25f, "#FFFFFF"), (0.7f, "#FFF27A"), (1f, "#FFC24B")), erode = ErodeNone })),
            // pop glow + white clouds + confetti that floats down (ETFX ConfettiBlast structure)
            ("Win_Confetti", (t, fx) => Add(t, fx,
                Glow("glow", 3f, 0.15f, tint: false, behind: false),
                new Sys { name = "clouds", tint = false, color = Cream, count = 14, lifeMin = 0.3f, lifeMax = 0.9f, speedMin = 1f, speedMax = 5f, sizeMin = 0.3f, sizeMax = 0.55f, dampen = 0.2f,
                          radius = 0.1f, spin = Mathf.PI, order = -1, sizeCurve = PopShrink, overLife = CoolDown },
                new Sys { name = "confetti", mat = "Toon_Shard", tint = false, randomColors = PaletteGradient(), count = 60, lifeMin = 1.5f, lifeMax = 2.2f, speedMin = 3f, speedMax = 12f,
                          sizeMin = 0.16f, sizeMax = 0.32f, gravity = 0.45f, dampen = 0.18f, radius = 0.15f, spin = 8f,
                          sizeCurve = Curve(0f, 1f, 0.78f, 1f, 1f, 0f), erode = ErodeNone })),
            // toon explosion: hot clouds that cool into smoke, sparks falling (ETFX SmallExplosion structure)
            ("Fire_Burst", (t, fx) => Add(t, fx,
                Glow("glow", 4.5f, 0.3f, tint: false, hex: "#FFB02E", behind: false),
                new Sys { name = "clouds", tint = false, count = 7, lifeMin = 0.55f, lifeMax = 0.75f, speedMin = 2f, speedMax = 3.5f, sizeMin = 0.8f, sizeMax = 1.1f, dampen = 0.15f,
                          radius = 0.15f, spin = Mathf.PI, gravity = -0.2f, sizeCurve = Curve(0f, 0.6f, 0.12f, 1.2f, 1f, 0f),
                          overLife = Grad(0.9f, (0f, "#FFF3B0"), (0.25f, "#FFA200"), (0.6f, "#5B4E6E"), (1f, "#3E4556")), erode = ErodeSoft },
                new Sys { name = "sparks", mat = "Toon_Drop", tint = false, count = 10, lifeMin = 0.25f, lifeMax = 0.45f, speedMin = 6f, speedMax = 15f, sizeMin = 0.1f, sizeMax = 0.15f,
                          gravity = 0.7f, dampen = 0.2f, radius = 0.25f, order = 1, stretch = 0.03f, sizeCurve = Shrink,
                          overLife = Grad(1f, (0f, "#FFFFFF"), (0.55f, "#FFFF4B"), (1f, "#FF7900")), erode = ErodeNone })),
            // real fire: tongues rising continuously, breathing glow at the base, embers on noise, smoke above (ETFX ToonTallFire structure)
            ("Fire_Flame", (t, fx) => Add(t, fx,
                new Sys { name = "glow", glow = true, tint = false, rate = 3f, duration = 3f, lifeMin = 0.7f, lifeMax = 0.9f, sizeMin = 2.2f, sizeMax = 2.2f, radius = 0.01f,
                          startRot = 0f, order = -2, sizeCurve = Curve(0f, 0.75f, 0.5f, 1f, 1f, 0.75f), overLife = FadeInOut("#FF8A1C", 0.5f) },
                new Sys { name = "smoke", mat = "Toon_Smoke", tint = false, color = new Color(1f, 1f, 1f, 0.55f), rate = 6f, duration = 3f, lifeMin = 1f, lifeMax = 1.3f, speedMin = 1.6f, speedMax = 2.2f, sizeMin = 0.7f, sizeMax = 1f,
                          gravity = -0.1f, dampen = 0.08f, cone = 5f, radius = 0.08f, offsetY = 0.45f, spin = 1.5f, order = -1,
                          sizeCurve = Curve(0f, 0.5f, 1f, 1.2f), overLife = FadeInOut("#5E5670", 0.2f), erode = ErodeSoft },
                // tongues stretched along their upward velocity, like the reference: tall and narrow instead of round
                new Sys { name = "flames", mat = "Toon_Flame", tint = false, rate = 16f, duration = 3f, lifeMin = 0.5f, lifeMax = 0.7f, speedMin = 1.8f, speedMax = 2.6f,
                          sizeMin = 0.42f, sizeMax = 0.58f, aspect = 1.9f, dampen = 0.15f, cone = 5f, radius = 0.14f, startRot = 0.12f,
                          sizeCurve = Curve(0f, 0.5f, 0.3f, 1f, 1f, 0.3f), overLife = Grad(0.85f, (0f, "#FFF2A2"), (0.27f, "#FFB500"), (1f, "#FF5000")),
                          erode = Curve(0f, -0.05f, 0.5f, 0.05f, 1f, 0.5f) },
                new Sys { name = "core", mat = "Toon_Flame", tint = false, rate = 6f, duration = 3f, lifeMin = 0.4f, lifeMax = 0.5f, speedMin = 1f, speedMax = 1.4f,
                          sizeMin = 0.45f, sizeMax = 0.55f, aspect = 1.6f, dampen = 0.2f, cone = 2f, radius = 0.03f, order = 1, startRot = 0.06f,
                          sizeCurve = Curve(0f, 0.6f, 0.3f, 1f, 1f, 0.4f), overLife = Grad(0.85f, (0f, "#FFFBE0"), (0.4f, "#FFE066"), (1f, "#FFA000")),
                          erode = Curve(0f, 0f, 1f, 0.45f) },
                new Sys { name = "embers", mat = "Toon_Drop", tint = false, rate = 10f, duration = 3f, lifeMin = 0.5f, lifeMax = 0.9f, speedMin = 1.8f, speedMax = 3f, sizeMin = 0.07f, sizeMax = 0.11f,
                          gravity = -0.1f, dampen = 0.15f, cone = 8f, radius = 0.25f, noise = 0.5f, order = 2, stretch = 0.03f,
                          sizeCurve = Curve(0f, 0.4f, 0.25f, 1f, 1f, 0.3f), overLife = Grad(0.75f, (0f, "#FFFFFF"), (0.5f, "#FFD54B"), (1f, "#FF3400")), erode = ErodeNone })),
        };
    }
}
