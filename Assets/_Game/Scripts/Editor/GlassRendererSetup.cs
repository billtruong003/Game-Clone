using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CasualGame.EditorTools
{
    /// <summary>
    /// The Merge jar glass lives on its own "Glass" sorting layer, drawn after "Default" (over the balls, under the
    /// outline and HUD). The glass is flat toon now (no refraction), so no extra renderer is needed: the old
    /// "Renderer2D_Glass" (Camera Sorting Layer Texture) is removed from the pipeline if it is still there.
    /// </summary>
    public static class GlassRendererSetup
    {
        public const string SortingLayerName = "Glass";
        private const string OldGlassRendererPath = "Assets/Settings/Renderer2D_Glass.asset";

        [MenuItem("Tools/Casual Game/FX/Setup Glass Layer")]
        public static void Setup()
        {
            EnsureSortingLayer();
            RemoveOldGlassRenderer();
        }

        private static void EnsureSortingLayer()
        {
            foreach (var l in SortingLayer.layers) if (l.name == SortingLayerName) return;
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("m_SortingLayers");
            layers.arraySize++;
            var entry = layers.GetArrayElementAtIndex(layers.arraySize - 1); // appended = drawn after Default
            entry.FindPropertyRelative("name").stringValue = SortingLayerName;
            entry.FindPropertyRelative("uniqueID").intValue = (int)(System.DateTime.Now.Ticks & 0x7FFFFFFF);
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RemoveOldGlassRenderer()
        {
            var old = AssetDatabase.LoadAssetAtPath<ScriptableObject>(OldGlassRendererPath);
            if (old == null) return;
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
                var so = new SerializedObject(urp);
                var list = so.FindProperty("m_RendererDataList");
                for (int i = list.arraySize - 1; i >= 0; i--)
                {
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue != old) continue;
                    list.GetArrayElementAtIndex(i).objectReferenceValue = null;
                    list.DeleteArrayElementAtIndex(i);
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.DeleteAsset(OldGlassRendererPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Glass: removed the old refraction renderer (Renderer2D_Glass).");
        }
    }
}
