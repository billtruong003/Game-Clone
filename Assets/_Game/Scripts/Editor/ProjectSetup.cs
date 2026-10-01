using System.Collections.Generic;
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

        public const string AudioMapPath = "Assets/_Game/Audio/AudioMap.asset";

        /// <summary>
        /// Library of the listed sound names (null or empty = all). Each name comes from the AudioMap (licensed pack
        /// clips) or else from a clip with that file name under Assets/_Game/Audio. A listed name also brings its
        /// "_intro" entry, which plays once before the loop.
        /// </summary>
        public static AudioLibrary BuildAudioLibrary(string path, string[] clipNames)
        {
            var byName = new Dictionary<string, AudioLibrary.Entry>();
            foreach (var g in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Game/Audio" }))
            {
                var c = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g));
                if (c != null) byName[c.name] = new AudioLibrary.Entry { name = c.name, clip = c, volume = 1f };
            }
            var map = AssetDatabase.LoadAssetAtPath<AudioMap>(AudioMapPath);
            if (map != null)
                foreach (var e in map.entries)
                    if (e.clip != null && !string.IsNullOrEmpty(e.name)) // a missing pack leaves the fallback in place
                        byName[e.name] = new AudioLibrary.Entry { name = e.name, clip = e.clip, volume = e.volume };

            bool Wanted(string n) => clipNames == null || clipNames.Length == 0 || clipNames.Contains(n) ||
                                     (n.EndsWith("_intro") && clipNames.Contains(n.Substring(0, n.Length - "_intro".Length)));
            var entries = byName.Values.Where(e => Wanted(e.name)).OrderBy(e => e.name).ToList();

            var lib = AssetDatabase.LoadAssetAtPath<AudioLibrary>(path);
            if (lib == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                lib = ScriptableObject.CreateInstance<AudioLibrary>();
                AssetDatabase.CreateAsset(lib, path);
            }
            lib.EditorSet(entries);
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
