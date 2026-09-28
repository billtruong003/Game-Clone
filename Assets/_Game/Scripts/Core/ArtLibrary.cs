using System;
using System.Collections.Generic;
using UnityEngine;

namespace CasualGame.Core
{
    [Serializable]
    public class EmojiDef
    {
        public string name;
        public float fps = 8f;
        public int[] sequence;
        public Sprite[] frames;
    }

    /// <summary>
    /// Every sprite sliced from Assets/_Game/Art/Sheets plus the emoji animation table.
    /// Rebuilt automatically by the editor when a sheet is imported (see SheetSlicer), so code looks sprites up by name.
    /// </summary>
    [CreateAssetMenu(menuName = "Casual Game/Art Library")]
    public class ArtLibrary : ScriptableObject
    {
        [SerializeField] private List<Sprite> sprites = new();
        [SerializeField] private List<EmojiDef> emojis = new();

        [System.NonSerialized] private Dictionary<string, Sprite> byName;
        private static ArtLibrary instance;

        public const string EditorAllPath = "Assets/_Game/Art/Libraries/ArtLibrary_All.asset";

        /// <summary>The library of the running game (set by the scene's GameContext). In the editor it falls back to the all-games library.</summary>
        public static ArtLibrary Instance
        {
            get
            {
#if UNITY_EDITOR
                if (instance == null) instance = UnityEditor.AssetDatabase.LoadAssetAtPath<ArtLibrary>(EditorAllPath);
#endif
                return instance;
            }
        }

        public static void SetCurrent(ArtLibrary library) => instance = library;

        public IReadOnlyList<EmojiDef> Emojis => emojis;

        public Sprite Get(string spriteName)
        {
            if (byName == null)
            {
                byName = new Dictionary<string, Sprite>(sprites.Count);
                foreach (var s in sprites)
                    if (s != null) byName[s.name] = s;
            }
            if (byName.TryGetValue(spriteName, out var sprite)) return sprite;
            Debug.LogWarning($"ArtLibrary: no sprite named '{spriteName}'");
            return null;
        }

        public EmojiDef GetEmoji(string emojiName) => emojis.Find(e => e.name == emojiName);

        // Negative faces are kept for reactions (game over, danger) so they stand out when they appear.
        private static readonly HashSet<string> ReactionOnly = new() { "angry", "cry", "dizzy" };
        // NonSerialized: Unity would otherwise restore this cache as an empty list after a domain reload, and the
        // empty-pool fallback then hands out reaction-only faces (cry, angry) as idle faces.
        [System.NonSerialized] private List<EmojiDef> idlePool;

        public EmojiDef RandomEmoji()
        {
            idlePool ??= emojis.FindAll(e => !ReactionOnly.Contains(e.name));
            var pool = idlePool.Count > 0 ? idlePool : emojis;
            return pool[UnityEngine.Random.Range(0, pool.Count)];
        }

#if UNITY_EDITOR
        public void EditorSet(List<Sprite> allSprites, List<EmojiDef> allEmojis)
        {
            sprites = allSprites;
            emojis = allEmojis;
            byName = null;
            idlePool = null;
        }
#endif
    }
}
