using System.IO;
using System.Linq;
using CasualGame.Core;
using UnityEditor;
using UnityEngine;

namespace CasualGame.EditorTools
{
    /// <summary>One-click project wiring: audio library, particle material, portrait orientation.</summary>
    public static class ProjectSetup
    {
        [MenuItem("Tools/Casual Game/Project Setup")]
        public static void Run()
        {
            BuildAudioLibrary();
            BuildFxMaterial();
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            AssetDatabase.SaveAssets();
            Debug.Log("Casual Game: project setup done");
        }

        public const string AudioAllPath = "Assets/_Game/Audio/Libraries/AudioLibrary_All.asset";

        [MenuItem("Tools/Casual Game/Rebuild Audio Library")]
        public static void BuildAudioLibrary()
        {
            BuildAudioLibrary(AudioAllPath, null);
            BuildSwitcher.RebuildGameLibraries();
        }

        /// <summary>Library of clips under Assets/_Game/Audio whose name is listed (null or empty = all).</summary>
        public static AudioLibrary BuildAudioLibrary(string path, string[] clipNames)
        {
            var clips = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Game/Audio" })
                .Select(g => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(c => c != null && (clipNames == null || clipNames.Length == 0 || clipNames.Contains(c.name)))
                .OrderBy(c => c.name)
                .ToList();
            var lib = AssetDatabase.LoadAssetAtPath<AudioLibrary>(path);
            if (lib == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                lib = ScriptableObject.CreateInstance<AudioLibrary>();
                AssetDatabase.CreateAsset(lib, path);
            }
            lib.EditorSet(clips);
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssetIfDirty(lib);
            return lib;
        }

        private static void BuildFxMaterial()
        {
            const string path = "Assets/_Game/Resources/FxMaterial.mat";
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(SheetSlicer.Folder + "fx.png"));
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
        }
    }
}
