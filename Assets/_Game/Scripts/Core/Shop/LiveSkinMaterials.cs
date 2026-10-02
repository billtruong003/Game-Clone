using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>
    /// The interactive premium skins' material templates (Assets/_Game/Skins/*/*.mat, Docs/SHADER_LAB.md). This asset
    /// lives in a Resources folder so builds carry the materials, their textures and shaders. Tune a skin on its template
    /// in the Inspector; every block / ball / arrow wearing it gets its own copy (its live values differ per piece).
    /// </summary>
    [CreateAssetMenu(menuName = "Casual Game/Live Skin Materials")]
    public sealed class LiveSkinMaterials : ScriptableObject
    {
        public const string ResourcePath = "LiveSkinMaterials";

        public Material[] templates = new Material[0];

        [Tooltip("Bruh Arrows Tape theme: the tape roll that rides the peel front (left half tinted, right half details)")]
        public Texture2D tapeRoll;

        private static LiveSkinMaterials instance;

        private static LiveSkinMaterials Instance => instance != null ? instance : instance = Resources.Load<LiveSkinMaterials>(ResourcePath);

        public static Texture2D TapeRoll => Instance != null ? Instance.tapeRoll : null;

        /// <summary>The template whose shader is CasualGame/Lab/&lt;shader&gt;, or null.</summary>
        public static Material Template(string shader)
        {
            var lib = Instance;
            if (lib == null) return null;
            foreach (var m in lib.templates)
                if (m != null && m.shader != null && m.shader.name.EndsWith("/" + shader)) return m;
            return null;
        }

        /// <summary>A copy of the skin's template for one piece (falls back to a bare material of the shader).</summary>
        public static Material Create(string shader)
        {
            var t = Template(shader);
            if (t != null) return new Material(t) { name = shader };
            var s = Shader.Find("CasualGame/Lab/" + shader);
            return s != null ? new Material(s) { name = shader } : null;
        }
    }
}
