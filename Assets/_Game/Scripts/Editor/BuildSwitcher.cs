using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CasualGame.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CasualGame.EditorTools
{
    /// <summary>
    /// One project, many games. A profile says which scenes, art sheets and sounds a game ships with, plus its
    /// store identity. Switching a profile rewires Build Settings, Player Settings, define symbols and the game's
    /// own art/audio libraries, so a build only contains that game (Unity strips everything unreferenced).
    /// </summary>
    public class BuildSwitcher : EditorWindow
    {
        public const string ProfilesFolder = "Assets/_Game/Build/Profiles";
        public const string LibrariesFolder = "Assets/_Game/Build/Libraries";
        private const string ScenesFolder = "Assets/_Game/Scenes/";
        private const string StatePath = "ProjectSettings/CasualGameBuild.json";
        private const string DefinePrefix = "CG_";

        private static readonly NamedBuildTarget[] Targets = { NamedBuildTarget.Android, NamedBuildTarget.Standalone, NamedBuildTarget.iOS };

        [Serializable] private class State { public string active; }

        private Vector2 scroll;
        private string report = "";
        private GameProfile selected;
        private Editor profileEditor;

        [MenuItem("Tools/Casual Game/Build Switcher %#b")]
        public static void Open() => GetWindow<BuildSwitcher>("Build Switcher");

        public static string ActiveProfileId =>
            File.Exists(StatePath) ? JsonUtility.FromJson<State>(File.ReadAllText(StatePath))?.active ?? "" : "";

        // ---------------- profiles ----------------

        public static List<GameProfile> Profiles()
        {
            EnsureProfiles();
            return AssetDatabase.FindAssets("t:GameProfile", new[] { ProfilesFolder })
                .Select(g => AssetDatabase.LoadAssetAtPath<GameProfile>(AssetDatabase.GUIDToAssetPath(g)))
                .OrderBy(p => p.isDev ? 0 : 1).ThenBy(p => p.gameId)
                .ToList();
        }

        private static readonly string[] UiSounds = { "ui_click", "ui_open", "ui_close", "star", "win", "lose", "big" };

        private static void EnsureProfiles()
        {
            Directory.CreateDirectory(ProfilesFolder);
            Make("Dev", "Casual Game (Dev)", "com.casualgame.dev", true, new[] { "Hub", "ArrowOut", "EyeBlast", "EyeMerge" }, null, null);
            Make("ArrowOut", "Arrow Out", "com.casualgame.arrowout", false, new[] { "ArrowOut" },
                new[] { "shapes", "ui", "fx" }, UiSounds.Concat(new[] { "fly", "blocked", "hint", "music_arrow" }).ToArray());
            Make("EyeBlast", "Eye Blast", "com.casualgame.eyeblast", false, new[] { "EyeBlast" },
                new[] { "faces_a", "faces_b", "shapes", "ui", "fx" }, UiSounds.Concat(new[] { "pick", "place", "clear", "music_blast" }).ToArray());
            Make("EyeMerge", "Eye Merge", "com.casualgame.eyemerge", false, new[] { "EyeMerge" },
                new[] { "faces_a", "faces_b", "shapes", "ui", "fx" }, UiSounds.Concat(new[] { "drop", "merge", "thud", "music_merge" }).ToArray());
        }

        private static void Make(string id, string product, string appId, bool dev, string[] scenes, string[] sheets, string[] sounds)
        {
            var path = $"{ProfilesFolder}/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<GameProfile>(path) != null) return; // never overwrite hand edits
            var p = CreateInstance<GameProfile>();
            p.gameId = id;
            p.productName = product;
            p.applicationId = appId;
            p.isDev = dev;
            p.scenes = scenes;
            p.artSheets = sheets ?? Array.Empty<string>();
            p.audioClips = sounds ?? Array.Empty<string>();
            p.defineSymbol = DefinePrefix + id.ToUpperInvariant();
            AssetDatabase.CreateAsset(p, path);
        }

        // ---------------- libraries & scene wiring ----------------

        public static string ArtLibraryPath(GameProfile p) => p.isDev ? ArtLibrary.EditorAllPath : $"{LibrariesFolder}/ArtLibrary_{p.gameId}.asset";
        public static string AudioLibraryPath(GameProfile p) => p.isDev ? ProjectSetup.AudioAllPath : $"{LibrariesFolder}/AudioLibrary_{p.gameId}.asset";

        /// <summary>Per-game art/audio libraries containing only that game's sheets and clips.</summary>
        public static void RebuildGameLibraries()
        {
            foreach (var p in Profiles().Where(p => !p.isDev))
            {
                SheetSlicer.RebuildLibrary(ArtLibraryPath(p), p.artSheets);
                ProjectSetup.BuildAudioLibrary(AudioLibraryPath(p), p.audioClips);
            }
        }

        // Shaders the games create materials for at runtime (Shader.Find): must be in every build.
        private static readonly string[] RuntimeShaders =
            { "CasualGame/GooMerge", "CasualGame/GlassJar", "CasualGame/Silhouette", "CasualGame/UISilhouette", "CasualGame/InkBrush" };

        public static void EnsureRuntimeShaders()
        {
            var gs = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var list = gs.FindProperty("m_AlwaysIncludedShaders");
            foreach (var name in RuntimeShaders)
            {
                var shader = Shader.Find(name);
                if (shader == null) { Debug.LogWarning("Missing shader " + name); continue; }
                bool has = false;
                for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) has = true;
                if (has) continue;
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
            }
            gs.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Puts a GameContext in every scene pointing at the right libraries (Hub → all games, game → its own).</summary>
        public static void WireScenes()
        {
            EnsureRuntimeShaders();
            var profiles = Profiles();
            var dev = profiles.First(p => p.isDev);
            var active = SceneManager.GetActiveScene().path;
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            foreach (var sceneName in dev.scenes)
            {
                var path = ScenesFolder + sceneName + ".unity";
                if (!File.Exists(path)) continue;
                var owner = profiles.FirstOrDefault(p => !p.isDev && p.scenes.Contains(sceneName)) ?? dev;
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var ctx = UnityEngine.Object.FindFirstObjectByType<GameContext>() ?? new GameObject("GameContext").AddComponent<GameContext>();
                ctx.EditorWire(owner.gameId,
                    AssetDatabase.LoadAssetAtPath<ArtLibrary>(ArtLibraryPath(owner)),
                    AssetDatabase.LoadAssetAtPath<AudioLibrary>(AudioLibraryPath(owner)),
                    AssetDatabase.LoadAssetAtPath<FxCatalog>(EtfxPicks.CatalogPath(owner.isDev ? "Dev" : owner.gameId)));
                EditorUtility.SetDirty(ctx);
                WireGameSpecific(sceneName);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            if (!string.IsNullOrEmpty(active) && File.Exists(active)) EditorSceneManager.OpenScene(active);
        }

        private static void WireGameSpecific(string sceneName)
        {
            if (sceneName == "EyeMerge")
            {
                // the jar's glass refracts: this camera uses the renderer with the Camera Sorting Layer Texture
                var cam = UnityEngine.Object.FindFirstObjectByType<Camera>();
                if (cam != null)
                {
                    var data = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                    if (data == null) data = cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                    data.SetRenderer(GlassRendererSetup.Setup());
                    EditorUtility.SetDirty(data);
                }
            }
            if (sceneName != "ArrowOut") return;
            var game = UnityEngine.Object.FindFirstObjectByType<ArrowOut.ArrowOutGame>();
            if (game == null) return;
            var so = new SerializedObject(game);
            so.FindProperty("levelsJson").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(ArrowLevelBaker.OutputPath);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------- switching ----------------

        public static void Switch(GameProfile p)
        {
            RebuildGameLibraries();
            WireScenes();

            EditorBuildSettings.scenes = p.scenes
                .Select(s => ScenesFolder + s + ".unity")
                .Where(File.Exists)
                .Select(path => new EditorBuildSettingsScene(path, true))
                .ToArray();

            PlayerSettings.productName = p.productName;
            foreach (var t in Targets) PlayerSettings.SetApplicationIdentifier(t, p.applicationId);
            PlayerSettings.bundleVersion = p.version;
            PlayerSettings.Android.bundleVersionCode = p.versionCode;
            if (p.icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { p.icon }, IconKind.Any);

            foreach (var t in Targets)
            {
                var defines = PlayerSettings.GetScriptingDefineSymbols(t)
                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Where(d => !d.StartsWith(DefinePrefix))
                    .Append(p.defineSymbol);
                PlayerSettings.SetScriptingDefineSymbols(t, string.Join(";", defines.Distinct()));
            }

            File.WriteAllText(StatePath, JsonUtility.ToJson(new State { active = p.gameId }));
            AssetDatabase.SaveAssets();
            var first = ScenesFolder + p.scenes[0] + ".unity";
            if (File.Exists(first)) EditorSceneManager.OpenScene(first);
            Debug.Log($"Build Switcher: now building '{p.productName}' ({p.applicationId}) with {EditorBuildSettings.scenes.Length} scene(s)");
        }

        public static void Build(GameProfile p, BuildTarget target)
        {
            Switch(p);
            var ext = target == BuildTarget.Android ? (EditorUserBuildSettings.buildAppBundle ? ".aab" : ".apk") : ".exe";
            var output = $"Builds/{p.gameId}/{target}/{p.gameId}{ext}";
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Select(s => s.path).ToArray(),
                locationPathName = output,
                target = target,
                options = BuildOptions.None,
            });
            Debug.Log($"Build Switcher: {p.gameId} → {output}: {result.summary.result}, {result.summary.totalSize / 1048576f:0.0} MB");
        }

        // ---------------- content report ----------------

        /// <summary>
        /// What a build of this profile would contain: dependencies of its scenes plus everything in any Resources
        /// folder (always shipped). Flags assets that belong to another game.
        /// </summary>
        public static string Report(GameProfile p)
        {
            var scenePaths = p.scenes.Select(s => ScenesFolder + s + ".unity").Where(File.Exists).ToArray();
            var resources = AssetDatabase.GetAllAssetPaths().Where(a => a.Contains("/Resources/") && !AssetDatabase.IsValidFolder(a)).ToArray();
            var deps = AssetDatabase.GetDependencies(scenePaths.Concat(resources).ToArray(), true)
                .Where(a => !a.EndsWith(".cs") && !a.EndsWith(".asmdef") && !a.StartsWith("Packages/"))
                .Distinct().ToList();

            var otherSheets = Profiles().Where(o => !o.isDev).SelectMany(o => o.artSheets).Distinct().Except(p.isDev ? Array.Empty<string>() : p.artSheets).ToArray();
            var otherScenes = Profiles().First(o => o.isDev).scenes.Except(p.scenes).ToArray();
            bool Foreign(string a) => !p.isDev &&
                (otherSheets.Any(s => a == $"{SheetSlicer.Folder}{s}.png") || otherScenes.Any(s => a == $"{ScenesFolder}{s}.unity"));

            var sb = new StringBuilder();
            sb.AppendLine($"{p.productName}: {scenePaths.Length} scene(s), {deps.Count} assets");
            long total = 0;
            foreach (var group in deps.GroupBy(Category).OrderByDescending(g => g.Sum(Size)))
            {
                var size = group.Sum(Size);
                total += size;
                sb.AppendLine($"  {group.Key,-10} {group.Count(),4} files  {size / 1024f,9:0} KB");
            }
            sb.AppendLine($"  {"TOTAL",-10} {deps.Count,4} files  {total / 1024f,9:0} KB");
            var foreign = deps.Where(Foreign).ToList();
            sb.AppendLine(foreign.Count == 0 ? "  ✓ no assets from other games" : "  ✗ from other games:\n    " + string.Join("\n    ", foreign));
            foreach (var t in deps.Where(a => a.StartsWith(SheetSlicer.Folder) && a.EndsWith(".png")))
                sb.AppendLine($"    sheet {Path.GetFileName(t)}");
            return sb.ToString();
        }

        private static string Category(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".png" or ".tga" or ".psd" or ".jpg" => "Textures",
                ".ogg" or ".wav" or ".mp3" => "Audio",
                ".unity" => "Scenes",
                ".ttf" or ".otf" => "Fonts",
                ".mat" or ".shader" or ".shadergraph" => "Materials",
                ".json" or ".txt" => "Data",
                _ => "Other",
            };
        }

        /// <summary>Approximate shipped size: compressed texture size for the current platform, file size otherwise.</summary>
        private static long Size(string path)
        {
            if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(Texture2D))
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var util = typeof(Editor).Assembly.GetType("UnityEditor.TextureUtil");
                var m = util?.GetMethod("GetStorageMemorySizeLong", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                if (tex != null && m != null) return (long)m.Invoke(null, new object[] { tex });
            }
            return File.Exists(path) ? new FileInfo(path).Length : 0;
        }

        // ---------------- window ----------------

        private void OnGUI()
        {
            var profiles = Profiles();
            var active = ActiveProfileId;
            EditorGUILayout.LabelField("Active build", string.IsNullOrEmpty(active) ? "(none)" : active, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Platform", EditorUserBuildSettings.activeBuildTarget.ToString());
            EditorGUILayout.Space();

            foreach (var p in profiles)
            {
                using (new EditorGUILayout.HorizontalScope("box"))
                {
                    var label = (p.gameId == active ? "● " : "   ") + p.productName;
                    if (GUILayout.Button(label, EditorStyles.label, GUILayout.Width(170))) selected = p;
                    EditorGUILayout.LabelField(p.applicationId, GUILayout.Width(200));
                    if (GUILayout.Button("Switch", GUILayout.Width(70))) { Switch(p); report = Report(p); }
                    if (GUILayout.Button("Report", GUILayout.Width(70))) report = Report(p);
                    if (GUILayout.Button("Build Android", GUILayout.Width(100)) &&
                        EditorUtility.DisplayDialog("Build", $"Build {p.productName} for Android?", "Build", "Cancel"))
                        Build(p, BuildTarget.Android);
                }
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Rebuild libraries + rewire scenes")) { RebuildGameLibraries(); WireScenes(); }

            if (selected != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Profile: {selected.gameId}", EditorStyles.boldLabel);
                Editor.CreateCachedEditor(selected, null, ref profileEditor);
                profileEditor.OnInspectorGUI();
            }

            EditorGUILayout.Space();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.TextArea(report, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }
    }
}
