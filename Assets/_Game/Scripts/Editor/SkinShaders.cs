using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CasualGame.EditorTools
{
    /// <summary>
    /// The premium skin shaders are only found by name at runtime (Shader.Find), so a build would strip them. This
    /// adds them to Graphics Settings › Always Included Shaders (run once; the Build Switcher runs it before a build).
    /// </summary>
    public static class SkinShaders
    {
        private static readonly string[] Names =
        {
            "ArrowInk", "ArrowChalk", "ArrowBlueprint", "ArrowVector", "ArrowHolo", "ArrowNeon",
            "ThemeGround", "ThemeSprite", "ThemeFace", "ThemeTrail", "BallSkin", "SlimeSkin", "BlockSkin", "ProceduralEye",
        };

        [MenuItem("Tools/Casual Game/Include Skin Shaders In Builds")]
        public static void Include()
        {
            var settings = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
            var so = new SerializedObject(settings);
            var list = so.FindProperty("m_AlwaysIncludedShaders");
            foreach (var n in Names)
            {
                var shader = Shader.Find("CasualGame/Lab/" + n);
                if (shader == null) { Debug.LogWarning("Skin shader missing: " + n); continue; }
                var present = false;
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) { present = true; break; }
                if (present) continue;
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }
    }
}
