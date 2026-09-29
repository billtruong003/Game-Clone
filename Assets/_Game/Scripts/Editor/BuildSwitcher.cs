using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CasualGame.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CasualGame.EditorTools
{
    /// <summary>
    /// One project, many games. A profile holds everything one game ships with: store identity (name, package,
    /// version code), scenes, art sheets, sounds, ads/IAP ids and its build history. Applying a profile rewires Build
    /// Settings, Player Settings (Android: IL2CPP/ARM64/target API/keystore), define symbols, the game's own
    /// libraries and its runtime GameConfig, so a build only contains that game. Builds are validated first.
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
                .OrderBy(p => p.gameId)
                .ToList();
        }

        private static readonly string[] UiSounds = { "ui_click", "ui_open", "ui_close", "star", "win", "lose", "big" };

        private static void EnsureProfiles()
        {
            Directory.CreateDirectory(ProfilesFolder);
            Make("ArrowOut", "Arrow Out", "com.casualgame.arrowout", new[] { "ArrowOut" },
                new[] { "shapes", "ui", "fx" }, UiSounds.Concat(new[] { "fly", "blocked", "hint", "music_arrow" }).ToArray());
            Make("EyeBlast", "Eye Blast", "com.casualgame.eyeblast", new[] { "EyeBlast" },
                new[] { "faces_a", "faces_b", "shapes", "ui", "fx" }, UiSounds.Concat(new[] { "pick", "place", "clear", "music_blast" }).ToArray());
            Make("EyeMerge", "Eye Merge", "com.casualgame.eyemerge", new[] { "EyeMerge" },
                new[] { "faces_a", "faces_b", "shapes", "ui", "fx" }, UiSounds.Concat(new[] { "drop", "merge", "thud", "music_merge" }).ToArray());
        }

        private static void Make(string id, string product, string appId, string[] scenes, string[] sheets, string[] sounds)
        {
            var path = $"{ProfilesFolder}/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<GameProfile>(path) != null) return; // never overwrite hand edits
            var p = CreateInstance<GameProfile>();
            p.gameId = id;
            p.productName = product;
            p.applicationId = appId;
            p.scenes = scenes;
            p.artSheets = sheets ?? Array.Empty<string>();
            p.audioClips = sounds ?? Array.Empty<string>();
            p.defineSymbol = DefinePrefix + id.ToUpperInvariant();
            AssetDatabase.CreateAsset(p, path);
        }

        // ---------------- libraries & scene wiring ----------------

        public static string ArtLibraryPath(GameProfile p) => $"{LibrariesFolder}/ArtLibrary_{p.gameId}.asset";
        public static string AudioLibraryPath(GameProfile p) => $"{LibrariesFolder}/AudioLibrary_{p.gameId}.asset";

        /// <summary>Per-game art/audio libraries containing only that game's sheets and clips.</summary>
        public static void RebuildGameLibraries()
        {
            foreach (var p in Profiles())
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

        /// <summary>Puts a GameContext in every scene pointing at the right libraries (each game → its own).</summary>
        public static void WireScenes()
        {
            EnsureRuntimeShaders();
            var profiles = Profiles();
            var active = SceneManager.GetActiveScene().path;
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            foreach (var (owner, sceneName) in profiles.SelectMany(p => p.scenes.Select(s => (p, s))))
            {
                var path = ScenesFolder + sceneName + ".unity";
                if (!File.Exists(path)) continue;
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var ctx = UnityEngine.Object.FindFirstObjectByType<GameContext>() ?? new GameObject("GameContext").AddComponent<GameContext>();
                ctx.EditorWire(owner.gameId,
                    AssetDatabase.LoadAssetAtPath<ArtLibrary>(ArtLibraryPath(owner)),
                    AssetDatabase.LoadAssetAtPath<AudioLibrary>(AudioLibraryPath(owner)),
                    AssetDatabase.LoadAssetAtPath<FxCatalog>(EtfxPicks.CatalogPath(owner.gameId)),
                    AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath(owner)));
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

        // ---------------- runtime config ----------------

        public static string GameConfigPath(GameProfile p) => $"{LibrariesFolder}/GameConfig_{p.gameId}.asset";

        /// <summary>Writes the profile's release data into the runtime GameConfig the game reads.</summary>
        public static GameConfig WriteConfig(GameProfile p, bool release)
        {
            var path = GameConfigPath(p);
            var c = AssetDatabase.LoadAssetAtPath<GameConfig>(path);
            if (c == null)
            {
                Directory.CreateDirectory(LibrariesFolder);
                c = CreateInstance<GameConfig>();
                AssetDatabase.CreateAsset(c, path);
            }
            var studio = StudioSettings.Get();
            c.gameId = p.gameId;
            c.productName = p.productName;
            c.applicationId = p.applicationId;
            c.version = p.version;
            c.versionCode = p.versionCode;
            c.maxSdkKey = studio.maxSdkKey;
            c.interstitialAdUnit = p.interstitialAdUnit;
            c.rewardedAdUnit = p.rewardedAdUnit;
            c.bannerAdUnit = p.bannerAdUnit;
            c.fakeAds = !release;
            c.removeAdsProductId = p.removeAdsProductId;
            c.privacyPolicyUrl = p.privacyPolicyUrl;
            EditorUtility.SetDirty(c);
            return c;
        }

        // ---------------- validation ----------------

        public struct Issue
        {
            public bool error;
            public string text;
            public Issue(bool error, string text) { this.error = error; this.text = text; }
        }

        private static readonly Regex PackageName = new Regex(@"^[a-z][a-z0-9_]*(\.[a-z][a-z0-9_]*){2,}$");

        /// <summary>Everything that would make a build of this profile wrong or rejected by Play. Errors block a build.</summary>
        public static List<Issue> Validate(GameProfile p, bool release)
        {
            var list = new List<Issue>();
            void Err(string t) => list.Add(new Issue(true, t));
            void Warn(string t) => list.Add(new Issue(false, t));
            void ReleaseErr(string t) => list.Add(new Issue(release, t));
            var others = Profiles().Where(o => o != p).ToList();
            var studio = StudioSettings.Get();

            if (string.IsNullOrWhiteSpace(p.productName)) Err("Product name is empty");
            if (string.IsNullOrEmpty(p.applicationId) || !PackageName.IsMatch(p.applicationId)) Err($"Package name '{p.applicationId}' is not valid (lowercase, at least 3 parts)");
            if (others.Any(o => o.applicationId == p.applicationId)) Err($"Package name '{p.applicationId}' is used by another game");
            if (others.Any(o => o.defineSymbol == p.defineSymbol)) Err($"Define '{p.defineSymbol}' is used by another game");
            if (p.applicationId != null && p.applicationId.StartsWith("com.casualgame.")) ReleaseErr("Package name still uses the placeholder prefix 'com.casualgame.' (permanent after the first upload)");
            if (p.scenes == null || p.scenes.Length == 0) Err("No scenes");
            else foreach (var s in p.scenes) if (!File.Exists(ScenesFolder + s + ".unity")) Err($"Scene '{s}' does not exist");
            if (p.icon == null) ReleaseErr("No app icon");
            if (p.versionCode <= p.LastReleasedCode()) Err($"versionCode {p.versionCode} was already released (last {p.LastReleasedCode()})");

            if (string.IsNullOrEmpty(studio.maxSdkKey) || string.IsNullOrEmpty(p.interstitialAdUnit) || string.IsNullOrEmpty(p.rewardedAdUnit))
                ReleaseErr("AppLovin MAX SDK key / ad unit ids missing: fake ads only");
            if (string.IsNullOrEmpty(p.privacyPolicyUrl)) ReleaseErr("Privacy policy URL missing (required by Play and for ads)");
            if (release)
            {
                if (string.IsNullOrEmpty(studio.keystorePath) || !File.Exists(studio.keystorePath)) Err("Upload keystore not set in Studio settings");
                else if (string.IsNullOrEmpty(PlayerSettings.Android.keystorePass)) Err("Keystore passwords not entered this session");
            }
            if (AdMobAdapterInstalled && string.IsNullOrEmpty(p.adMobAppId)) Err("AdMob adapter is installed but the AdMob app id is empty (the app would crash at launch)");
            if (studio.targetSdk < 35) Warn($"Target API {studio.targetSdk} is below the Google Play minimum");
            return list;
        }

        // ---------------- switching ----------------

        public static void Switch(GameProfile p, bool release = false)
        {
            RebuildGameLibraries();
            foreach (var o in Profiles()) WriteConfig(o, release && o == p);
            WireScenes();

            EditorBuildSettings.scenes = p.scenes
                .Select(s => ScenesFolder + s + ".unity")
                .Where(File.Exists)
                .Select(path => new EditorBuildSettingsScene(path, true))
                .ToArray();

            var studio = StudioSettings.Get();
            PlayerSettings.companyName = studio.companyName;
            PlayerSettings.productName = p.productName;
            foreach (var t in Targets) PlayerSettings.SetApplicationIdentifier(t, p.applicationId);
            PlayerSettings.bundleVersion = p.version;
            PlayerSettings.Android.bundleVersionCode = p.versionCode;
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, p.icon != null ? new[] { p.icon } : Array.Empty<Texture2D>(), IconKind.Any);

            // Play requirements: 64-bit (IL2CPP + ARM64), current target API
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)studio.minSdk;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)studio.targetSdk;
            if (!string.IsNullOrEmpty(studio.keystorePath))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = studio.keystorePath;
                PlayerSettings.Android.keyaliasName = p.keyAlias;
            }

            ApplyMaxSettings(p, studio);

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
            Debug.Log($"Build Switcher: now building '{p.productName}' ({p.applicationId} {p.version}/{p.versionCode}) with {EditorBuildSettings.scenes.Length} scene(s)");
        }

        public static bool AdMobAdapterInstalled => Directory.Exists("Assets/MaxSdk/Mediation/Google");

        /// <summary>
        /// AppLovin's settings are project-wide, so each switch writes this game's values into them: SDK key,
        /// AdMob app id, and Google's consent flow (Terms &amp; Privacy Policy) pointing at this game's privacy policy.
        /// </summary>
        private static void ApplyMaxSettings(GameProfile p, StudioSettings studio)
        {
            var max = AppLovinSettings.Instance;
            max.SdkKey = studio.maxSdkKey;
            max.AdMobAndroidAppId = p.adMobAppId ?? "";
            max.SaveAsync();
            var consent = AppLovinMax.Scripts.IntegrationManager.Editor.AppLovinInternalSettings.Instance;
            consent.ConsentFlowEnabled = !string.IsNullOrEmpty(p.privacyPolicyUrl);
            if (consent.ConsentFlowEnabled) consent.ConsentFlowPrivacyPolicyUrl = p.privacyPolicyUrl;
            consent.Save();
        }

        // ---------------- building ----------------

        /// <summary>Scriptable entry: BuildSwitcher.BuildById("ArrowOut", release: false).</summary>
        public static string BuildById(string gameId, bool release)
        {
            var p = Profiles().FirstOrDefault(o => o.gameId == gameId);
            return p == null ? "No profile " + gameId : Build(p, release);
        }

        /// <summary>
        /// Release = signed AAB for Play with real ads; test = development APK with fake ads for a device. A release
        /// build that succeeds is recorded and bumps versionCode so the next upload can never reuse it.
        /// </summary>
        public static string Build(GameProfile p, bool release)
        {
            var errors = Validate(p, release).Where(i => i.error).ToList();
            if (errors.Count > 0)
            {
                var msg = $"Build {p.gameId} blocked:\n  " + string.Join("\n  ", errors.Select(e => e.text));
                Debug.LogError(msg);
                return msg;
            }
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            Switch(p, release);
            EditorUserBuildSettings.buildAppBundle = release;
            var output = $"Builds/{p.gameId}/{p.gameId}-{p.version}-{p.versionCode}{(release ? ".aab" : "-test.apk")}";
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Select(s => s.path).ToArray(),
                locationPathName = output,
                target = BuildTarget.Android,
                options = release ? BuildOptions.None : BuildOptions.Development,
            });
            var result = report.summary.result.ToString();
            p.history.Add(new GameProfile.BuildRecord
            {
                date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                kind = release ? "release" : "test",
                version = p.version,
                versionCode = p.versionCode,
                result = result,
                sizeMB = (float)Math.Round(report.summary.totalSize / 1048576.0, 1),
                output = output,
                commit = GitHead(),
            });
            if (release && report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded) p.versionCode++;
            EditorUtility.SetDirty(p);
            AssetDatabase.SaveAssets();
            var line = $"Build {p.gameId} {(release ? "AAB" : "APK")} {p.version}: {result}, {report.summary.totalSize / 1048576f:0.0} MB -> {output}";
            Debug.Log(line);
            return line;
        }

        private static string GitHead()
        {
            try
            {
                var head = File.ReadAllText(".git/HEAD").Trim();
                if (!head.StartsWith("ref: ")) return head.Substring(0, Math.Min(7, head.Length));
                var refPath = ".git/" + head.Substring(5);
                return File.Exists(refPath) ? File.ReadAllText(refPath).Trim().Substring(0, 7) : "";
            }
            catch (Exception) { return ""; }
        }

        // ---------------- new game ----------------

        /// <summary>Creates a profile (and an empty scene) for a new game: package name from the studio prefix, own define.</summary>
        public static GameProfile NewGame(string id, string product)
        {
            id = new string(id.Where(char.IsLetterOrDigit).ToArray());
            if (id.Length == 0 || AssetDatabase.LoadAssetAtPath<GameProfile>($"{ProfilesFolder}/{id}.asset") != null) return null;
            Make(id, product, $"{StudioSettings.Get().packagePrefix}.{id.ToLowerInvariant()}", new[] { id }, new[] { "ui", "fx" }, UiSounds);
            var scenePath = ScenesFolder + id + ".unity";
            if (!File.Exists(scenePath))
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                var s = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(s, scenePath);
            }
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<GameProfile>($"{ProfilesFolder}/{id}.asset");
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

            var otherSheets = Profiles().SelectMany(o => o.artSheets).Distinct().Except(p.artSheets).ToArray();
            var otherScenes = Profiles().SelectMany(o => o.scenes).Distinct().Except(p.scenes).ToArray();
            bool Foreign(string a) =>
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

        private string newId = "", newName = "";
        private string keystorePass = "", keyPass = "";

        private void OnGUI()
        {
            var profiles = Profiles();
            var active = ActiveProfileId;
            var studio = StudioSettings.Get();
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.LabelField("Active game", string.IsNullOrEmpty(active) ? "(none)" : active, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Platform", EditorUserBuildSettings.activeBuildTarget.ToString());
            EditorGUILayout.Space();

            foreach (var p in profiles)
            {
                var issues = Validate(p, true);
                var status = issues.Any(i => i.error) ? "✗" : issues.Count > 0 ? "!" : "✓";
                using (new EditorGUILayout.HorizontalScope("box"))
                {
                    var label = (p.gameId == active ? "● " : "   ") + status + " " + p.productName;
                    if (GUILayout.Button(label, EditorStyles.label, GUILayout.Width(170))) selected = p;
                    EditorGUILayout.LabelField($"{p.applicationId}  v{p.version} ({p.versionCode})", GUILayout.Width(280));
                    if (GUILayout.Button("Apply", GUILayout.Width(60))) { Switch(p); report = Report(p); selected = p; }
                    if (GUILayout.Button("Test APK", GUILayout.Width(75)) &&
                        EditorUtility.DisplayDialog("Build", $"Build a test APK of {p.productName}?", "Build", "Cancel"))
                        report = Build(p, false);
                    if (GUILayout.Button("Release AAB", GUILayout.Width(90)) &&
                        EditorUtility.DisplayDialog("Build", $"Build release AAB {p.productName} {p.version} ({p.versionCode})?", "Build", "Cancel"))
                        report = Build(p, true);
                }
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rebuild libraries + rewire scenes")) { RebuildGameLibraries(); foreach (var o in profiles) WriteConfig(o, false); WireScenes(); }
                if (GUILayout.Button("Studio settings")) selected = null;
            }

            using (new EditorGUILayout.HorizontalScope("box"))
            {
                EditorGUILayout.LabelField("New game", GUILayout.Width(70));
                newId = EditorGUILayout.TextField(newId, GUILayout.Width(120));
                newName = EditorGUILayout.TextField(newName);
                if (GUILayout.Button("Create", GUILayout.Width(60)) && newId.Length > 0)
                {
                    selected = NewGame(newId, string.IsNullOrEmpty(newName) ? newId : newName);
                    newId = newName = "";
                }
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Keystore passwords (this session only, never saved)", EditorStyles.miniBoldLabel);
                keystorePass = EditorGUILayout.PasswordField("Keystore", keystorePass);
                keyPass = EditorGUILayout.PasswordField("Key", keyPass);
                if (keystorePass.Length > 0) PlayerSettings.Android.keystorePass = keystorePass;
                if (keyPass.Length > 0) PlayerSettings.Android.keyaliasPass = keyPass;
            }

            EditorGUILayout.Space();
            if (selected == null)
            {
                EditorGUILayout.LabelField("Studio settings (all games)", EditorStyles.boldLabel);
                Editor.CreateCachedEditor(studio, null, ref profileEditor);
                profileEditor.OnInspectorGUI();
            }
            else
            {
                EditorGUILayout.LabelField($"Profile: {selected.gameId}  (release checks)", EditorStyles.boldLabel);
                foreach (var i in Validate(selected, true))
                    EditorGUILayout.HelpBox(i.text, i.error ? MessageType.Error : MessageType.Warning);
                Editor.CreateCachedEditor(selected, null, ref profileEditor);
                profileEditor.OnInspectorGUI();
            }

            EditorGUILayout.Space();
            EditorGUILayout.TextArea(report, GUILayout.MinHeight(120));
            EditorGUILayout.EndScrollView();
        }
    }
}
