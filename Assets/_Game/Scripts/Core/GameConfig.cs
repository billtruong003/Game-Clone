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

        [Header("Ads (AppLovin MAX)")]
        public string maxSdkKey;
        public string interstitialAdUnit;
        public string rewardedAdUnit;
        public string bannerAdUnit;
        [Tooltip("Dev builds: fake ads even when ids are set")] public bool fakeAds = true;

        [Header("Store")]
        public string removeAdsProductId = "remove_ads";
        public string privacyPolicyUrl;

        public bool HasRealAds => !fakeAds && !string.IsNullOrEmpty(maxSdkKey) &&
                                  (!string.IsNullOrEmpty(interstitialAdUnit) || !string.IsNullOrEmpty(rewardedAdUnit));

        public string StoreUrl => "https://play.google.com/store/apps/details?id=" + applicationId;

        /// <summary>The config of the game that is running (set by GameContext before any other script).</summary>
        public static GameConfig Current { get; private set; }

        internal static void SetCurrent(GameConfig config)
        {
            if (config != null) Current = config;
        }
    }
}
