using System;
using System.Collections.Generic;
using UnityEngine;

namespace CasualGame.Core
{
    public interface IAdProvider
    {
        bool RewardedReady { get; }
        /// <summary>onDone(true) only when the player watched to the reward.</summary>
        void ShowRewarded(string placement, Action<bool> onDone);
        /// <summary>onDone(true) when an ad was actually shown.</summary>
        void ShowInterstitial(string placement, Action<bool> onDone);
    }

    /// <summary>
    /// Ads facade used by all games. Rewarded ads always run (the player asked for them); interstitials are
    /// frequency-capped and skipped entirely once "Remove Ads" is owned. On a device the provider is AdMob (with Unity
    /// Ads through AdMob mediation); in the editor, or without ad unit ids, a fake overlay.
    /// </summary>
    public static class Ads
    {
        public static IAdProvider Provider { get; private set; } = new FakeAdProvider();

        private const float InterstitialCooldown = 90f;
        private static float lastInterstitial = -InterstitialCooldown, lastRewarded = -InterstitialCooldown;
        private static int sessionsEnded;
        private static bool initialized;

        public static bool RemoveAdsOwned
        {
            get => SaveStore.GetBool("iap.noads", false);
            internal set { SaveStore.SetBool("iap.noads", value); SaveStore.Save(); }
        }

        /// <summary>Called once by the first GameContext, after the game's config is known.</summary>
        public static void Init(GameConfig config)
        {
            if (initialized) return;
            initialized = true;
            if (config != null && config.HasAdUnits && !Application.isEditor)
                Provider = new AdMobAdProvider(config);
            Store.Init(config);
        }

        public static bool RewardedReady => Provider.RewardedReady;

        public static string NoVideoText => Loc.T("No video right now. Try again later.", "Chưa có video, thử lại sau nhé.");

        /// <summary>
        /// Always player-initiated (a button with the ad icon). onDone(false) = no fill / closed early / failed: the caller
        /// stays where it was (see Popup.RewardedButton), so the player is never left stuck.
        /// </summary>
        public static void ShowRewarded(string placement, Action<bool> onDone) =>
            Provider.ShowRewarded(placement, ok =>
            {
                if (ok) lastRewarded = Time.realtimeSinceStartup;
                onDone?.Invoke(ok);
            });

        /// <summary>
        /// Call at natural breaks, after the player chose to continue (Next level / Play again). Skips the first two
        /// breaks of a session, never right after a rewarded ad, at most one per 90 s counted from ads actually shown.
        /// </summary>
        public static void OnBreak(string placement, Action onDone)
        {
            sessionsEnded++;
            var now = Time.realtimeSinceStartup;
            if (RemoveAdsOwned || sessionsEnded <= 2 || now - lastInterstitial < InterstitialCooldown || now - lastRewarded < InterstitialCooldown)
            {
                onDone?.Invoke();
                return;
            }
            Provider.ShowInterstitial(placement, shown =>
            {
                if (shown) lastInterstitial = Time.realtimeSinceStartup;
                onDone?.Invoke();
            });
        }
    }

    /// <summary>
    /// In-app purchases (Store/IAP_PRODUCTS.md): Google Play Billing through Unity IAP on device, simulated in the editor.
    /// Every product is a non-consumable. Ownership is cached in SaveStore and re-checked against Play on every start.
    /// <see cref="FullGame"/> counts as <see cref="AllSkins"/> + Remove ads.
    /// </summary>
    public static class Store
    {
        public const string AllSkins = "all_skins", FullGame = "all_skins_noads";
        /// <summary>USD prices of IAP_PRODUCTS.md, shown until Google Play's localized prices are known (editor, offline).</summary>
        public const string UsdAllSkins = "$4.99", UsdFullGame = "$6.99", UsdRemoveAds = "$2.99";

        public static event Action PurchasesChanged;
        private static IapStore iap;
        private static GameConfig config;

        public static string RemoveAdsId => config != null && !string.IsNullOrEmpty(config.removeAdsProductId) ? config.removeAdsProductId : "remove_ads";

        /// <summary>Starts the store connection (the boot calls it early so prices are ready by the shop); safe to call twice.</summary>
        public static void Init(GameConfig cfg)
        {
            if (cfg == null || config != null) return;
            config = cfg;
            if (Application.isEditor) return;
            var ids = new List<string> { RemoveAdsId };
            var catalog = Skins.CatalogOf(cfg.gameId);
            if (catalog != null) ids.AddRange(catalog.ProductIds());
            iap = new IapStore(ids, SetOwned);
        }

        public static bool Ready => iap != null && iap.Ready;

        public static bool Owns(string id) => SaveStore.GetBool(OwnKey(id), false);

        /// <summary>Google Play's localized price, else the USD price from IAP_PRODUCTS.md.</summary>
        public static string Price(string id, string usd) => iap?.Price(id) ?? usd;

        public static void Buy(string id, string label, Action<bool> onDone)
        {
            if (iap != null)
            {
                iap.Buy(id, onDone);
                return;
            }
            if (!Application.isEditor) // a device build without a store must never hand out a free purchase
            {
                FakeAdProvider.ShowOverlay(Loc.T("The store is not available right now.", "Cửa hàng chưa sẵn sàng."), 1.4f, () => onDone?.Invoke(false));
                return;
            }
            FakeAdProvider.ShowOverlay(Loc.F("Buying \"{0}\" (simulated)…", "Mua \"{0}\" (giả lập)…", label), 1.2f, () =>
            {
                SetOwned(id, true);
                onDone?.Invoke(true);
            });
        }

        public static void BuyRemoveAds(Action<bool> onDone) => Buy(RemoveAdsId, Loc.T("Remove ads", "Gỡ quảng cáo"), onDone);

        public static void Restore(Action<bool> onDone)
        {
            if (iap != null) iap.Restore(onDone);
            else onDone?.Invoke(true);
        }

#if UNITY_EDITOR
        /// <summary>Editor cheat (Tools/Casual Game/Shop): own or drop a product of any game, without a store.</summary>
        public static void EditorSetOwned(string gameId, string id, bool owned)
        {
            SaveStore.SetBool($"iap.{gameId}.{id}", owned);
            if (config != null && config.gameId == gameId) Ads.RemoveAdsOwned = Owns(RemoveAdsId) || Owns(FullGame);
            SaveStore.Save();
            PurchasesChanged?.Invoke();
            Skins.RaiseChanged();
        }

        /// <summary>Editor: forget every simulated purchase of this game.</summary>
        public static void EditorResetPurchases()
        {
            var catalog = Skins.CatalogOf(config != null ? config.gameId : null);
            if (catalog != null) foreach (var id in catalog.ProductIds()) SaveStore.Delete(OwnKey(id));
            SaveStore.Delete(OwnKey(RemoveAdsId));
            Ads.RemoveAdsOwned = false;
            PurchasesChanged?.Invoke();
            Skins.RaiseChanged();
        }
#endif

        private static string OwnKey(string id) => $"iap.{(config != null ? config.gameId : "")}.{id}";

        private static void SetOwned(string id, bool owned)
        {
            if (Owns(id) == owned) return;
            SaveStore.SetBool(OwnKey(id), owned);
            Ads.RemoveAdsOwned = Owns(RemoveAdsId) || Owns(FullGame);
            SaveStore.Save();
            PurchasesChanged?.Invoke();
            Skins.RaiseChanged();
        }
    }

    /// <summary>Editor/prototype stand-in: a full-screen overlay with a countdown instead of a real ad.</summary>
    public class FakeAdProvider : IAdProvider
    {
        /// <summary>Editor switch to exercise the no-fill path.</summary>
        public static bool SimulateNoFill;

        public bool RewardedReady => !SimulateNoFill;

        public void ShowRewarded(string placement, Action<bool> onDone)
        {
            if (SimulateNoFill) { onDone?.Invoke(false); return; }
            ShowOverlay(Loc.T("Rewarded ad (simulated)", "Quảng cáo có thưởng (giả lập)") + "\n" + placement, 1.5f, () => onDone?.Invoke(true));
        }

        public void ShowInterstitial(string placement, Action<bool> onDone) =>
            ShowOverlay(Loc.T("Ad (simulated)", "Quảng cáo (giả lập)") + "\n" + placement, 1.0f, () => onDone?.Invoke(true));

        public static void ShowOverlay(string message, float seconds, Action onDone)
        {
            var canvas = UIKit.CreateOverlayCanvas("FakeAd", 5000);
            var dim = UIKit.Rect("Dim", canvas.transform);
            UIKit.Stretch(dim);
            UIKit.AddImage(dim, (Sprite)null, new Color(0f, 0f, 0f, 0.88f)).raycastTarget = true;
            var label = UIKit.Label(dim, message, 56, Color.white);
            UIKit.Stretch(label.rectTransform);
            canvas.gameObject.AddComponent<Delay>().Run(seconds, () =>
            {
                UnityEngine.Object.Destroy(canvas.gameObject);
                onDone?.Invoke();
            });
        }

        private class Delay : MonoBehaviour
        {
            private float remaining;
            private Action done;

            public void Run(float seconds, Action onDone)
            {
                remaining = seconds;
                done = onDone;
            }

            private void Update()
            {
                remaining -= Time.unscaledDeltaTime;
                if (remaining > 0f) return;
                enabled = false;
                done?.Invoke();
            }
        }
    }
}
