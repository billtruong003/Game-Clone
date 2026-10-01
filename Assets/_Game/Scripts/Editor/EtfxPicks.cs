using System.IO;
using CasualGame.Core;
using CasualGame.Sandbox;
using UnityEditor;
using UnityEngine;

namespace CasualGame.EditorTools
{
    /// <summary>
    /// The Epic Toon FX effects picked for each game event, as prefab VARIANTS of the originals (still linked to the pack,
    /// so a pack update flows through): adds FxEffect on the root, turns looping bursts into one-shots, and lifts sorting
    /// above the characters. "White" picks get their colours bleached to grey so GameFx's tint paints them exactly
    /// (one splash / dust puff serves every ball and block colour). Output: Assets/_Game/Fx/Prefabs (the FX sandbox list).
    /// </summary>
    public static class EtfxPicks
    {
        private const string Src = "Assets/References/Epic Toon FX/Prefabs/";
        private const int BehindOrder = 2; // below characters (sort 10+), above the background

        // (our name, ETFX prefab, one-shot?, bleach for tinting?)
        private static readonly (string name, string src, bool oneShot, bool white)[] Picks =
        {
            // Meh Merge: a jelly splat in the ball's colour (the pack's blood splats, gore-free once recoloured)
            ("Merge_Splash", "Combat/Blood/Red/BloodExplosionRound", true, true),
            ("Merge_SplashBig", "Combat/Blood/Red/BloodExplosionRound2", true, true),
            // Nah Blocks: a glitter sparkle per cleared block, in the block's colour family
            ("Blast_BlockPop_Pink", "Combat/Explosions/GlitterExplosion/GlitterExplosionPink", true, false),
            ("Blast_BlockPop_Blue", "Combat/Explosions/GlitterExplosion/GlitterExplosionBlue", true, false),
            ("Blast_BlockPop_Green", "Combat/Explosions/GlitterExplosion/GlitterExplosionGreen", true, false),
            ("Blast_BlockPop_Yellow", "Combat/Explosions/GlitterExplosion/GlitterExplosionYellow", true, false),
            // a tinted cartoon dust puff, kept for future themes (earthy look); not used by the default skin
            ("Blast_Dust", "Environment/Smoke/White/SmokeExplosionWhite", true, true),
            ("Blast_Place", "Combat/Explosions (Misc)/HitDustExplosion", true, false),
            ("Land_Poof", "Combat/Explosions (Text)/Poof", true, false),
            ("Arrow_LevelStar", "Interactive/Stars/StarPoof", true, false),
            ("Win_Confetti", "Environment/Confetti/Blast/ConfettiBlastRainbow", true, false),
            ("Sparkle", "Interactive/Sparkle/SparkleSolo/SparkleSoloWhite", true, false),
            ("Fire", "Environment/Fire/Cartoon/Tall/ToonTallFireRed", false, false),
            // shop skins: each colour set has its own merge / line-clear effect (mockup "Skin VFX & Pricing")
            ("Skin_Confetti", "Environment/Confetti/Blast/ConfettiBlastRainbow", true, false),
            ("Skin_Bubbles", "Environment/Bubbles/SoapBubbleBlast", true, false),
            ("Skin_Leaves", "Environment/Weather/Wind & Leaves/LeafExplosion", true, false),
            ("Skin_Ring", "Combat/Explosions/NovaSmallExplosion/ExplosionNovaSmallFire", true, true),
            ("Skin_Zap", "Combat/Explosions/LightningSoftExplosion/LightningSoftExplosionBlue", true, true),
            ("Skin_Powder", "Environment/Smoke/White/SmokeBurstWhiteSoft", true, true),
            ("Skin_Pixel", "Combat/Explosions/StarExplosion/StarExplosionGreen", true, true),
        };

        // Retired picks, deleted on the next build so no catalog keeps them.
        private static readonly string[] Retired =
        {
            "Merge_Fusion_Pink", "Merge_Fusion_Blue", "Merge_Fusion_Green", "Merge_Fusion_Yellow", "Merge_BigFusion",
            "Blast_MultiLine", "Combo_Nova",
        };

        // Which effects each game ships (by name prefix).
        private static readonly (string game, string[] prefixes)[] CatalogContents =
        {
            ("EyeMerge", new[] { "Merge_", "Land_", "Win_", "Sparkle", "Skin_" }),
            ("EyeBlast", new[] { "Blast_", "Win_", "Sparkle", "Skin_", "Merge_Splash" }),
            ("ArrowOut", new[] { "Arrow_", "Land_", "Win_", "Sparkle", "Skin_" }),
        };

        public static string CatalogPath(string game) => $"{BuildSwitcher.LibrariesFolder}/FxCatalog_{game}.asset";

        [MenuItem("Tools/Casual Game/FX/Rebuild FX Catalogs")]
        public static void BuildCatalogs()
        {
            var all = new System.Collections.Generic.List<FxEffect>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { FxSandbox.EffectsFolder }))
            {
                var fx = AssetDatabase.LoadAssetAtPath<FxEffect>(AssetDatabase.GUIDToAssetPath(guid));
                if (fx != null) all.Add(fx);
            }
            Directory.CreateDirectory(BuildSwitcher.LibrariesFolder);
            foreach (var (game, prefixes) in CatalogContents)
            {
                var path = CatalogPath(game);
                var cat = AssetDatabase.LoadAssetAtPath<FxCatalog>(path);
                if (cat == null)
                {
                    cat = ScriptableObject.CreateInstance<FxCatalog>();
                    AssetDatabase.CreateAsset(cat, path);
                }
                cat.EditorSet(all.FindAll(e => System.Array.Exists(prefixes, p => e.name.StartsWith(p))));
                EditorUtility.SetDirty(cat);
            }
            AssetDatabase.SaveAssets();
        }

        // Start colour to white (alpha kept), colour-over-life to the grey of its brightness relative to the start, so the
        // effect keeps its shading but takes its hue from the tint alone.
        private static void Bleach(ParticleSystem ps)
        {
            var main = ps.main;
            var start = main.startColor;
            var reference = Mathf.Max(0.05f, start.mode == ParticleSystemGradientMode.TwoColors
                ? Mathf.Max(start.colorMin.grayscale, start.colorMax.grayscale)
                : start.color.grayscale);
            Color White(Color c) => new(1f, 1f, 1f, c.a);
            main.startColor = start.mode == ParticleSystemGradientMode.TwoColors
                ? new ParticleSystem.MinMaxGradient(White(start.colorMin), White(start.colorMax))
                : new ParticleSystem.MinMaxGradient(White(start.color));
            var col = ps.colorOverLifetime;
            if (!col.enabled || col.color.gradient == null) return;
            var g = col.color.gradient;
            var keys = g.colorKeys;
            for (int i = 0; i < keys.Length; i++)
            {
                var v = Mathf.Clamp01(keys[i].color.grayscale / reference);
                keys[i].color = new Color(v, v, v);
            }
            var bleached = new Gradient { mode = g.mode };
            bleached.SetKeys(keys, g.alphaKeys);
            col.color = new ParticleSystem.MinMaxGradient(bleached);
        }

        [MenuItem("Tools/Casual Game/FX/Build Epic Toon FX Picks")]
        public static void Build()
        {
            Directory.CreateDirectory(FxSandbox.EffectsFolder);
            foreach (var old in Retired)
                AssetDatabase.DeleteAsset($"{FxSandbox.EffectsFolder}/{old}.prefab");
            int made = 0;
            foreach (var (name, src, oneShot, white) in Picks)
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(Src + src + ".prefab");
                if (source == null) { Debug.LogWarning("ETFX pick missing: " + src); continue; }
                var path = $"{FxSandbox.EffectsFolder}/{name}.prefab";
                if (File.Exists(path)) AssetDatabase.DeleteAsset(path);

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(source);
                inst.name = name;
                foreach (var ps in inst.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = ps.main;
                    main.playOnAwake = false;
                    if (oneShot) main.loop = false;
                    else main.duration = Mathf.Max(main.duration, 3f); // sandbox preview: burn for a few seconds
                    if (white) Bleach(ps);
                }
                // merge bursts and landing dust sit under the characters: the ball pops out of the light / squashes onto the dust
                int baseOrder = name.StartsWith("Merge_") || name.StartsWith("Land_") ? BehindOrder : Fx.SortingOrder;
                foreach (var r in inst.GetComponentsInChildren<Renderer>(true)) r.sortingOrder += baseOrder;
                var fx = inst.AddComponent<FxEffect>();
                if (white) // every system takes the tint
                {
                    var so = new SerializedObject(fx);
                    var list = so.FindProperty("tintable");
                    var systems = inst.GetComponentsInChildren<ParticleSystem>(true);
                    list.arraySize = systems.Length;
                    for (int i = 0; i < systems.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = systems[i];
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(inst, path); // an instance of a prefab saves as a Variant
                Object.DestroyImmediate(inst);
                made++;
            }
            AssetDatabase.Refresh();
            BuildCatalogs();
            Debug.Log($"ETFX picks: {made} variant prefab(s) in {FxSandbox.EffectsFolder}");
        }
    }
}
