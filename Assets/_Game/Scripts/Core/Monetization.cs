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
    /// frequency-capped and skipped entirely once "Remove Ads" is owned.
    /// Swap <see cref="Provider"/> for a LevelPlay/AdMob implementation when the SDK is added.
    /// </summary>
    public static class Ads
    {
        public static IAdProvider Provider = new FakeAdProvider();

        private const float InterstitialCooldown = 90f;
        private static float lastInterstitial = -InterstitialCooldown;
        private static int sessionsEnded;

        public static bool RemoveAdsOwned
        {
            get => SaveStore.GetBool("iap.noads", false);
            internal set { SaveStore.SetBool("iap.noads", value); SaveStore.Save(); }
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

    /// <summary>In-app purchases. Stubbed until Unity IAP / Play Billing is wired up.</summary>
    public static class Store
    {
        public const string RemoveAdsProduct = "remove_ads";
        public static event Action PurchasesChanged;

        public static void BuyRemoveAds(Action<bool> onDone)
        {
            FakeAdProvider.ShowOverlay("Mua “Gỡ quảng cáo” (giả lập)…", 1.2f, () =>
            {
                Ads.RemoveAdsOwned = true;
                PurchasesChanged?.Invoke();
                onDone?.Invoke(true);
            });
        }

        public static void Restore(Action<bool> onDone) => onDone?.Invoke(Ads.RemoveAdsOwned);
    }

    /// <summary>Editor/prototype stand-in: a full-screen overlay with a countdown instead of a real ad.</summary>
    public class FakeAdProvider : IAdProvider
    {
        public void ShowRewarded(string placement, Action<bool> onDone) =>
            ShowOverlay($"Quảng cáo có thưởng (giả lập)\n{placement}", 1.5f, () => onDone?.Invoke(true));

        public void ShowInterstitial(string placement, Action onDone) =>
            ShowOverlay($"Quảng cáo (giả lập)\n{placement}", 1.0f, onDone);

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
