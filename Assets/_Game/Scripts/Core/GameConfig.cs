using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>
    /// What the running game needs to know about its own release: store identity, ad unit ids, IAP product ids,
    /// privacy link. Generated from the game's GameProfile by the Build Switcher (never edit by hand) and wired
    /// into the scene's GameContext, so each game build carries only its own ids.
    /// </summary>
    public class GameConfig : ScriptableObject
    {
        public string gameId;
        public string productName;
        public string applicationId;
        public string version;
        public int versionCode;

        [Header("Ads (AdMob ad unit ids; test builds get Google's test units)")]
        public string interstitialAdUnit;
        public string rewardedAdUnit;
        public string bannerAdUnit;

        [Header("Store")]
        public string removeAdsProductId = "remove_ads";
        public string privacyPolicyUrl;

        [Header("Launch splash (G1)")]
        public Sprite logo;
        public Color splashColor = new Color(0.17f, 0.18f, 0.33f);
        [Tooltip("The game's characters on a transparent ground (the adaptive icon foreground), shown on the loading screen")]
        public Texture2D splashArt;
        [Tooltip("Music the loading screen starts, so it is already playing when the game fades in")]
        public string music;

        public bool HasAdUnits => !string.IsNullOrEmpty(interstitialAdUnit) || !string.IsNullOrEmpty(rewardedAdUnit);

        public string StoreUrl => "https://play.google.com/store/apps/details?id=" + applicationId;

        /// <summary>The config of the game that is running (set by GameContext before any other script).</summary>
        public static GameConfig Current { get; private set; }

        internal static void SetCurrent(GameConfig config)
        {
            if (config != null) Current = config;
        }
    }
}
