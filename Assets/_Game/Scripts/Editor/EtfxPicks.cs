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
    /// above the characters. Output: Assets/_Game/Fx/Prefabs (the FX sandbox list).
    /// </summary>
    public static class EtfxPicks
    {
        private const string Src = "Assets/References/Epic Toon FX/Prefabs/";
        private const int BehindOrder = 2; // below characters (sort 10+), above the background

        // (our name, ETFX prefab, one-shot?)
        private static readonly (string name, string src, bool oneShot)[] Picks =
        {
            ("Merge_Fusion_Pink", "Combat/Explosions/SparkleExplosion/SparkleExplosionPink", true),
            ("Merge_Fusion_Blue", "Combat/Explosions/SparkleExplosion/SparkleExplosionBlue", true),
            ("Merge_Fusion_Green", "Combat/Explosions/SparkleExplosion/SparkleExplosionGreen", true),
            ("Merge_Fusion_Yellow", "Combat/Explosions/SparkleExplosion/SparkleExplosionYellow", true),
            ("Merge_BigFusion", "Combat/Explosions/StarIntenseExplosion/StarIntenseExplosionOrange", true),
            ("Blast_BlockPop_Pink", "Combat/Explosions/GlitterExplosion/GlitterExplosionPink", true),
            ("Blast_BlockPop_Blue", "Combat/Explosions/GlitterExplosion/GlitterExplosionBlue", true),
            ("Blast_BlockPop_Green", "Combat/Explosions/GlitterExplosion/GlitterExplosionGreen", true),
            ("Blast_BlockPop_Yellow", "Combat/Explosions/GlitterExplosion/GlitterExplosionYellow", true),
            ("Blast_Place", "Combat/Explosions (Misc)/HitDustExplosion", true),
            ("Land_Poof", "Combat/Explosions (Text)/Poof", true),
            ("Blast_MultiLine", "Combat/Explosions (Misc)/FlashExplosionRadial", true),
            ("Combo_Nova", "Combat/Explosions/MagicNovaExplosion/MagicNovaExplosionYellow", true),
            ("Arrow_LevelStar", "Interactive/Stars/StarPoof", true),
            ("Win_Confetti", "Environment/Confetti/Blast/ConfettiBlastRainbow", true),
            ("Sparkle", "Interactive/Sparkle/SparkleSolo/SparkleSoloWhite", true),
            ("Fire", "Environment/Fire/Cartoon/Tall/ToonTallFireRed", false),
        };

        // Which effects each game ships (by name prefix); "Dev" (Hub + all games) gets everything.
        private static readonly (string game, string[] prefixes)[] CatalogContents =
        {
            ("EyeMerge", new[] { "Merge_", "Land_", "Win_", "Sparkle" }),
            ("EyeBlast", new[] { "Blast_", "Combo_", "Win_", "Sparkle" }),
            ("ArrowOut", new[] { "Arrow_", "Land_", "Win_", "Sparkle" }),
            ("Dev", new[] { "" }),
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

        [MenuItem("Tools/Casual Game/FX/Build Epic Toon FX Picks")]
        public static void Build()
        {
            Directory.CreateDirectory(FxSandbox.EffectsFolder);
            int made = 0;
            foreach (var (name, src, oneShot) in Picks)
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
                }
                // merge bursts and landing dust sit under the characters: the ball pops out of the light / squashes onto the dust
                int baseOrder = name.StartsWith("Merge_") || name.StartsWith("Land_") ? BehindOrder : Fx.SortingOrder;
                foreach (var r in inst.GetComponentsInChildren<Renderer>(true)) r.sortingOrder += baseOrder;
                inst.AddComponent<FxEffect>();
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
