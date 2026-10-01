using System.IO;
using System.Linq;
using CasualGame.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CasualGame.EditorTools
{
    /// <summary>
    /// Builds and wires the shared Boot scene (credits → Made with Unity → splash + real loading) for the game being
    /// switched to: its libraries and config, the scene to load next, and the fox credit sprites from Tools/art/make-boot.mjs.
    /// Called by the Build Switcher; the Boot scene is always the first scene of a build.
    /// </summary>
    public static class BootSceneBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/Boot.unity";
        private const string ArtFolder = "Assets/_Game/Art/Boot/";

        public static void Wire(GameProfile p)
        {
            var active = SceneManager.GetActiveScene().path;
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            var scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = Object.FindFirstObjectByType<Camera>();
            if (cam == null)
            {
                cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
            }
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>(); // the boot plays the credit click and starts the music
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UIKit.Hex("#0B0B0F");
            cam.orthographic = true;

            var ctx = Object.FindFirstObjectByType<GameContext>() ?? new GameObject("GameContext").AddComponent<GameContext>();
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(BuildSwitcher.GameConfigPath(p));
            var audio = AssetDatabase.LoadAssetAtPath<AudioLibrary>(BuildSwitcher.AudioLibraryPath(p));
            ctx.EditorWire(p.gameId, AssetDatabase.LoadAssetAtPath<ArtLibrary>(BuildSwitcher.ArtLibraryPath(p)), audio,
                AssetDatabase.LoadAssetAtPath<FxCatalog>(EtfxPicks.CatalogPath(p.gameId)), config, boot: true);
            EditorUtility.SetDirty(ctx);

            var loader = Object.FindFirstObjectByType<BootLoader>() ?? new GameObject("BootLoader").AddComponent<BootLoader>();
            var so = new SerializedObject(loader);
            so.FindProperty("nextScene").stringValue = p.scenes.FirstOrDefault() ?? "";
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("audioLibrary").objectReferenceValue = audio;
            so.FindProperty("foxHead").objectReferenceValue = Sprite("fox_head");
            so.FindProperty("foxShades").objectReferenceValue = Sprite("fox_shades");
            so.FindProperty("foxShadesMask").objectReferenceValue = Sprite("fox_shades_mask");
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            if (!string.IsNullOrEmpty(active) && active != ScenePath && File.Exists(active)) EditorSceneManager.OpenScene(active);
        }

        // The boot PNGs are plain textures until imported as sprites with alpha.
        private static Sprite Sprite(string name)
        {
            var path = ArtFolder + name + ".png";
            if (AssetImporter.GetAtPath(path) is TextureImporter ti && (ti.textureType != TextureImporterType.Sprite || ti.mipmapEnabled))
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
