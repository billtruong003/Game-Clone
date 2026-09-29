using System.Collections.Generic;
using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>
    /// Plays the game's effect prefabs by name (FxCatalog set by GameContext), pooled per name: instances are
    /// deactivated when done and reused, so there is no Instantiate/Destroy per effect after warm-up.
    /// "{color}" in a name is replaced by the nearest color family (Pink / Blue / Green / Yellow variants).
    /// </summary>
    public static class GameFx
    {
        private static FxCatalog catalog;
        private static GameFxRunner runner;
        private static readonly Dictionary<string, Stack<FxEffect>> pool = new();

        public static void SetCatalog(FxCatalog c)
        {
            catalog = c;
            pool.Clear();
        }

        /// <summary>Nearest color family among the shipped effect variants.</summary>
        public static string Family(Color c)
        {
            Color.RGBToHSV(c, out var h, out var s, out _);
            if (s < 0.2f) return "Blue";
            float deg = h * 360f;
            if (deg < 25f || deg >= 270f) return "Pink";   // red, pink, purple
            if (deg < 75f) return "Yellow";                 // orange, yellow
            if (deg < 170f) return "Green";
            return "Blue";
        }

        public static string Colored(string effect, Color c) => effect.Contains("{color}") ? effect.Replace("{color}", Family(c)) : effect;

        public static void Play(string effectName, Vector3 position, float scale = 1f) => Play(effectName, position, Color.white, scale);

        public static void Play(string effectName, Vector3 position, Color tint, float scale = 1f)
        {
            var prefab = Find(effectName);
            if (prefab == null) return;
            if (runner == null) runner = GameFxRunner.Create();
            if (!pool.TryGetValue(effectName, out var stack)) pool[effectName] = stack = new Stack<FxEffect>();
            FxEffect fx = null;
            while (stack.Count > 0 && fx == null) fx = stack.Pop();
            if (fx == null)
            {
                fx = Object.Instantiate(prefab, runner.transform);
                fx.name = effectName;
            }
            fx.gameObject.SetActive(true);
            fx.Play(position, tint, scale);
            runner.Track(fx, fx.Duration + 0.1f);
        }

        /// <summary>Ends every running effect, e.g. when the screen changes so nothing keeps falling over the new one.</summary>
        public static void StopAll()
        {
            if (runner != null) runner.StopAll();
        }

        internal static void Return(FxEffect fx)
        {
            if (fx == null) return;
            fx.gameObject.SetActive(false);
            if (!pool.TryGetValue(fx.name, out var stack)) pool[fx.name] = stack = new Stack<FxEffect>();
            stack.Push(fx);
        }

        private static FxEffect Find(string effectName)
        {
            var fx = catalog != null ? catalog.Find(effectName) : null;
#if UNITY_EDITOR
            // editor fallback (scene without a catalog, e.g. opened directly): every effect prefab
            if (fx == null)
                fx = UnityEditor.AssetDatabase.LoadAssetAtPath<FxEffect>($"Assets/_Game/Fx/Prefabs/{effectName}.prefab");
#endif
            if (fx == null) Debug.LogWarning($"GameFx: no effect '{effectName}' in the catalog");
            return fx;
        }
    }
}
