using System.Collections.Generic;
using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>
    /// Every sprite sliced from Assets/_Game/Art/Sheets plus the face material (Texture2DArray of expressions, see Face).
    /// Rebuilt automatically by the editor when a sheet is imported (see SheetSlicer), so code looks sprites up by name.
    /// </summary>
    [CreateAssetMenu(menuName = "Casual Game/Art Library")]
    public class ArtLibrary : ScriptableObject
    {
        [SerializeField] private List<Sprite> sprites = new();
        [SerializeField] private Material faceMaterial;
        [SerializeField] private List<Texture2DArray> facePacks = new(); // shop face packs (faces_<pack>), same slices as the default
        [SerializeField] private Texture2D ballDigits;                  // Meh Merge billiard numbers 1..16 (Art/Lab/ball_digits.png)

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

        public Material FaceMaterial => faceMaterial;
        public Texture2D BallDigits => ballDigits;

        /// <summary>A shop face pack by name ("sleepy"), or null.</summary>
        public Texture2DArray FacePack(string pack) => facePacks.Find(t => t != null && t.name == "faces_" + pack);

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

#if UNITY_EDITOR
        public void EditorSet(List<Sprite> allSprites, Material faces, List<Texture2DArray> packs, Texture2D digits)
        {
            ballDigits = digits;
            sprites = allSprites;
            faceMaterial = faces;
            facePacks = packs;
            byName = null;
        }
#endif
    }
}
