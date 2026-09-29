using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CasualGame.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CasualGame.EditorTools
{
    /// <summary>
    /// Creates one scene per game with a camera and the game's root component, and registers
    /// them in Build Settings. Games build their UI in code, so a scene only needs this minimal skeleton.
    /// </summary>
    public static class SceneBuilder
    {
        private const string Folder = "Assets/_Game/Scenes/";

        private static readonly (string scene, string type)[] Scenes =
        {
            ("ArrowOut", "CasualGame.ArrowOut.ArrowOutGame, CasualGame.ArrowOut"),
            ("EyeBlast", "CasualGame.EyeBlast.EyeBlastGame, CasualGame.EyeBlast"),
            ("EyeMerge", "CasualGame.EyeMerge.EyeMergeGame, CasualGame.EyeMerge"),
        };

        [MenuItem("Tools/Casual Game/Build Scenes")]
        public static void BuildAll()
        {
            var built = new List<EditorBuildSettingsScene>();
            foreach (var (scene, typeName) in Scenes)
            {
                var type = Type.GetType(typeName);
                if (type == null) { Debug.Log($"SceneBuilder: skipping {scene} (script not written yet)"); continue; }
                var path = Folder + scene + ".unity";
                var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
                var cam = camGo.GetComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = UIKit.Reference.y / 200f; // 1 unit = 100 px on a 1920 px tall reference
                cam.transform.position = new Vector3(0, 0, -10);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = UIKit.Hex("#2B2F55");

                new GameObject(scene, type);
                EditorSceneManager.SaveScene(s, path);
                built.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = built.ToArray();
            EditorSceneManager.OpenScene(Folder + Scenes[0].scene + ".unity");
            Debug.Log("SceneBuilder: " + string.Join(", ", built.Select(b => b.path)));
        }

        /// <summary>Adds and selects a 1080×1920 portrait size in the Game view (Unity has no public API for this).</summary>
        [MenuItem("Tools/Casual Game/Game View Portrait 1080x1920")]
        public static void GameViewPortrait()
        {
            var asm = typeof(Editor).Assembly;
            var sizesType = asm.GetType("UnityEditor.GameViewSizes");
            var instance = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance")!.GetValue(null);
            var group = sizesType.GetMethod("GetGroup")!.Invoke(instance, new object[] { (int)sizesType.GetProperty("currentGroupType")!.GetValue(instance) });
            var groupType = group.GetType();
            var count = (int)groupType.GetMethod("GetTotalCount")!.Invoke(group, null);
            int index = -1;
            for (int i = 0; i < count; i++)
            {
                var size = groupType.GetMethod("GetGameViewSize")!.Invoke(group, new object[] { i });
                var w = (int)size.GetType().GetProperty("width")!.GetValue(size);
                var h = (int)size.GetType().GetProperty("height")!.GetValue(size);
                if (w == 1080 && h == 1920) { index = i; break; }
            }
            if (index < 0)
            {
                var sizeType = asm.GetType("UnityEditor.GameViewSize");
                var kind = Enum.ToObject(asm.GetType("UnityEditor.GameViewSizeType"), 1); // FixedResolution
                var ctor = sizeType.GetConstructor(new[] { kind.GetType(), typeof(int), typeof(int), typeof(string) });
                groupType.GetMethod("AddCustomSize")!.Invoke(group, new[] { ctor!.Invoke(new[] { kind, 1080, 1920, "Portrait 1080x1920" }) });
                index = count;
            }
            var gvType = asm.GetType("UnityEditor.GameView");
            var gv = EditorWindow.GetWindow(gvType);
            gvType.GetMethod("SizeSelectionCallback", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(gv, new object[] { index, null });
        }
    }
}
