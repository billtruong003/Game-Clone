using System;
using UnityEngine;

namespace CasualGame.Core
{
    public interface IAdProvider
    {
        void ShowRewarded(string placement, Action<bool> onDone);
        void ShowInterstitial(string placement, Action onDone);
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
        private static float lastInterstitial = -InterstitialCooldown;
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

        public static void ShowRewarded(string placement, Action<bool> onDone) => Provider.ShowRewarded(placement, onDone);

        /// <summary>Call at natural breaks (game over, level cleared). Skips the first two, then at most one per 90 s.</summary>
        public static void OnBreak(string placement, Action onDone)
        {
            sessionsEnded++;
            if (RemoveAdsOwned || sessionsEnded <= 2 || Time.realtimeSinceStartup - lastInterstitial < InterstitialCooldown)
            {
                onDone?.Invoke();
                return;
            }
            lastInterstitial = Time.realtimeSinceStartup;
            Provider.ShowInterstitial(placement, onDone);
        }
    }

    /// <summary>In-app purchases: Google Play Billing through Unity IAP on device, a simulated purchase in the editor.</summary>
    public static class Store
    {
        public static event Action PurchasesChanged;
        private static IapStore iap;

        internal static void Init(GameConfig config)
        {
            if (Application.isEditor || config == null || string.IsNullOrEmpty(config.removeAdsProductId)) return;
            iap = new IapStore(config.removeAdsProductId, OnOwned);
        }

        public static void BuyRemoveAds(Action<bool> onDone)
        {
            if (iap != null)
            {
                iap.Buy(onDone);
                return;
            }
            FakeAdProvider.ShowOverlay(Loc.T("Buying \"Remove ads\" (simulated)…", "Mua \"Gỡ quảng cáo\" (giả lập)…"), 1.2f, () =>
            {
                OnOwned();
                onDone?.Invoke(true);
            });
        }

        public static void Restore(Action<bool> onDone)
        {
            if (iap != null) iap.Restore(onDone);
            else onDone?.Invoke(Ads.RemoveAdsOwned);
        }

        private static void OnOwned()
        {
            if (Ads.RemoveAdsOwned) return;
            Ads.RemoveAdsOwned = true;
            PurchasesChanged?.Invoke();
        }
    }

    /// <summary>Editor/prototype stand-in: a full-screen overlay with a countdown instead of a real ad.</summary>
    public class FakeAdProvider : IAdProvider
    {
        public void ShowRewarded(string placement, Action<bool> onDone) =>
            ShowOverlay(Loc.T("Rewarded ad (simulated)", "Quảng cáo có thưởng (giả lập)") + "\n" + placement, 1.5f, () => onDone?.Invoke(true));

        public void ShowInterstitial(string placement, Action onDone) =>
            ShowOverlay(Loc.T("Ad (simulated)", "Quảng cáo (giả lập)") + "\n" + placement, 1.0f, onDone);

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
