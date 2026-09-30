using UnityEditor;
using UnityEngine;

namespace CasualGame.EditorTools
{
    /// <summary>
    /// Release settings shared by every game in the project (one publisher account, one upload key, one ads
    /// account). Per-game values live in each <see cref="GameProfile"/>.
    /// </summary>
    public class StudioSettings : ScriptableObject
    {
        public const string Path = "Assets/_Game/Build/StudioSettings.asset";

        [Tooltip("Player Settings company name")] public string companyName = "Bill The Dev";
        [Tooltip("Prefix for new games' package names. The package name is permanent once a game is uploaded to Play.")]
        public string packagePrefix = "com.billthedev";

        [Header("Android")]
        [Tooltip("Google Play's current minimum target API for new apps and updates")] public int targetSdk = 36;
        public int minSdk = 25;
        [Tooltip("Upload keystore (outside the repo). Passwords are typed in the Build Switcher each session, never saved.")]
        public string keystorePath = "";


        public static StudioSettings Get()
        {
            var s = AssetDatabase.LoadAssetAtPath<StudioSettings>(Path);
            if (s != null) return s;
            s = CreateInstance<StudioSettings>();
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            AssetDatabase.CreateAsset(s, Path);
            AssetDatabase.SaveAssets();
            return s;
        }
    }
}
