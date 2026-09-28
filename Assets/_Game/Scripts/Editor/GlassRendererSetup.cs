using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CasualGame.EditorTools
{
    /// <summary>
    /// Refraction for the Merge glass jar needs URP 2D's Camera Sorting Layer Texture (a copy of everything drawn so far).
    /// That copy costs a full-screen blit per frame, so it lives in a SEPARATE renderer ("Renderer2D_Glass") that only the
    /// Merge / sandbox cameras select; Blast and Arrow keep the plain renderer. Also adds a "Glass" sorting layer drawn
    /// after "Default": the texture is grabbed after Default, the glass on "Glass" samples it.
    /// </summary>
    public static class GlassRendererSetup
    {
        public const string SortingLayerName = "Glass";
        private const string BasePath = "Assets/Settings/Renderer2D.asset";
        private const string GlassPath = "Assets/Settings/Renderer2D_Glass.asset";

        [MenuItem("Tools/Casual Game/FX/Setup Glass Renderer")]
        public static int Setup()
        {
            EnsureSortingLayer();
            if (!File.Exists(GlassPath)) AssetDatabase.CopyAsset(BasePath, GlassPath);
            var glass = AssetDatabase.LoadAssetAtPath<ScriptableObject>(GlassPath);
            var so = new SerializedObject(glass);
            so.FindProperty("m_UseCameraSortingLayersTexture").boolValue = true;
            so.FindProperty("m_CameraSortingLayersTextureBound").intValue = SortingLayer.NameToID("Default");
            so.FindProperty("m_CameraSortingLayerDownsamplingMethod").intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();

            // register it in the pipeline asset's renderer list (once)
            var urp = Pipeline();
            var pso = new SerializedObject(urp);
            var list = pso.FindProperty("m_RendererDataList");
            int index = -1;
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == glass) index = i;
            if (index < 0)
            {
                index = list.arraySize;
                list.arraySize++;
                list.GetArrayElementAtIndex(index).objectReferenceValue = glass;
                pso.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"Glass renderer ready: renderer index {index}, sorting layer '{SortingLayerName}' after Default.");
            return index;
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

        /// <summary>The URP asset in use: the quality level's override, else the graphics default.</summary>
        private static UniversalRenderPipelineAsset Pipeline() =>
            (UniversalRenderPipelineAsset)(QualitySettings.renderPipeline != null ? QualitySettings.renderPipeline : UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline);

        /// <summary>Index of the glass renderer in the pipeline asset (for UniversalAdditionalCameraData.SetRenderer).</summary>
        public static int RendererIndex()
        {
            var glass = AssetDatabase.LoadAssetAtPath<ScriptableObject>(GlassPath);
            var urp = Pipeline();
            var list = new SerializedObject(urp).FindProperty("m_RendererDataList");
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == glass) return i;
            return -1;
        }
    }
}
