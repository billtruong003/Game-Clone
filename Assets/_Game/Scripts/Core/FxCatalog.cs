using System.Collections.Generic;
using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>
    /// The effect prefabs one game ships (Epic Toon FX picks, see Editor/EtfxPicks). Referenced by the scene's
    /// GameContext, so a single-game build only includes that game's effects.
    /// </summary>
    [CreateAssetMenu(menuName = "Casual Game/FX Catalog")]
    public class FxCatalog : ScriptableObject
    {
        [SerializeField] private List<FxEffect> effects = new();

        public IReadOnlyList<FxEffect> Effects => effects;

        [System.NonSerialized] private Dictionary<string, FxEffect> byName;

        // every GameFx.Play looks an effect up: a dictionary instead of a list scan with a lambda
        public FxEffect Find(string effectName)
        {
            if (byName == null)
            {
                byName = new Dictionary<string, FxEffect>(effects.Count);
                foreach (var e in effects) if (e != null && !byName.ContainsKey(e.name)) byName[e.name] = e; // first one wins, as before
            }
            return byName.TryGetValue(effectName, out var fx) ? fx : null;
        }

#if UNITY_EDITOR
        public void EditorSet(List<FxEffect> list)
        {
            effects = list;
            byName = null;
        }
#endif
    }
}
