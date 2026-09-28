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

        public FxEffect Find(string effectName) => effects.Find(e => e != null && e.name == effectName);

#if UNITY_EDITOR
        public void EditorSet(List<FxEffect> list) => effects = list;
#endif
    }
}
