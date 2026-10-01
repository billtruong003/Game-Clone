using System;
using System.Collections.Generic;
using UnityEngine;

namespace CasualGame.EditorTools
{
    /// <summary>
    /// Everything that makes one shippable game out of this multi-game project: store identity, version, content,
    /// ads/IAP ids and its build history. Applying a profile (Build Switcher) rewires the whole project for that game.
    /// </summary>
    [CreateAssetMenu(menuName = "Casual Game/Game Profile")]
    public class GameProfile : ScriptableObject
    {
        [Header("Identity")]
        public string gameId;
        public string productName;
        [Tooltip("Play package name, e.g. com.billthedev.arrowout. Permanent after the first upload.")] public string applicationId;
        [Tooltip("Scripting define set while this game is active, e.g. CG_ARROWOUT")] public string defineSymbol;
        public Texture2D icon;
        [Tooltip("Adaptive icon layers (Android 8+ launchers mask to their own shape): full-bleed background, foreground inside the middle 66 %")]
        public Texture2D adaptiveBackground, adaptiveForeground;
        [Tooltip("Title logo shown on the launch splash (G1), over the splash colour")] public Sprite logo;
        public Color splashColor = new Color(0.17f, 0.18f, 0.33f);

        [Header("Version")]
        [Tooltip("User-facing version name")] public string version = "0.1.0";
        [Tooltip("Must grow with every upload to Play; bumped automatically after a release build")] public int versionCode = 1;

        [Header("Content")]
        [Tooltip("Scene names under Assets/_Game/Scenes; the first one starts the game")] public string[] scenes;
        [Tooltip("Sprite sheets (Assets/_Game/Art/Sheets) this game uses")] public string[] artSheets;
        [Tooltip("Audio clip names this game uses (empty = all)")] public string[] audioClips;

        [Header("Android")]
        [Tooltip("Key alias inside the studio upload keystore")] public string keyAlias = "upload";

        [Header("Ads (AdMob, Android). Empty = Google's test ids in test builds")]
        [Tooltip("AdMob app id, ca-app-pub-XXXX~YYYY")] public string adMobAppId;
        public string interstitialAdUnit;
        public string rewardedAdUnit;
        public string bannerAdUnit;

        [Header("Store")]
        public string removeAdsProductId = "remove_ads";
        public string privacyPolicyUrl;

        [Header("History (written by builds)")]
        public List<BuildRecord> history = new List<BuildRecord>();

        [Serializable]
        public class BuildRecord
        {
            public string date;
            public string kind;        // "release" (AAB) or "test" (APK)
            public string version;
            public int versionCode;
            public string result;
            public float sizeMB;
            public string output;
            public string commit;
        }

        /// <summary>Highest versionCode that went out in a successful release build (0 if none).</summary>
        public int LastReleasedCode()
        {
            int best = 0;
            foreach (var r in history)
                if (r.kind == "release" && r.result == "Succeeded" && r.versionCode > best) best = r.versionCode;
            return best;
        }
    }
}
