using System;
using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>
    /// AppLovin MAX ads. MAX mediates every network (AppLovin, AdMob via its adapter, ...), so the game only talks
    /// to MAX. The SDK shows Google's consent form (Terms &amp; Privacy Policy flow) before the first ad in regions
    /// that need it; the flow is configured per game by the Build Switcher.
    /// </summary>
    public class MaxAdProvider : IAdProvider
    {
        private readonly string interstitialId, rewardedId;
        private int interstitialRetry, rewardedRetry;
        private Action pendingInterstitial;
        private Action<bool> pendingRewarded;
        private bool rewardEarned;

        public static bool Ready { get; private set; }

        public MaxAdProvider(GameConfig config)
        {
            interstitialId = config.interstitialAdUnit;
            rewardedId = config.rewardedAdUnit;

            MaxSdkCallbacks.OnSdkInitializedEvent += _ =>
            {
                Ready = true;
                LoadInterstitial();
                LoadRewarded();
            };

            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += (_, _) => interstitialRetry = 0;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += (_, _) => Retry(ref interstitialRetry, LoadInterstitial);
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += (_, _, _) => { FinishInterstitial(); LoadInterstitial(); };
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += (_, _) => { FinishInterstitial(); LoadInterstitial(); };

            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += (_, _) => rewardedRetry = 0;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += (_, _) => Retry(ref rewardedRetry, LoadRewarded);
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += (_, _, _) => rewardEarned = true;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += (_, _, _) => { FinishRewarded(); LoadRewarded(); };
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += (_, _) => { FinishRewarded(); LoadRewarded(); };

            MaxSdk.InitializeSdk();
        }

        public void ShowInterstitial(string placement, Action onDone)
        {
            if (!Ready || string.IsNullOrEmpty(interstitialId) || !MaxSdk.IsInterstitialReady(interstitialId))
            {
                onDone?.Invoke();
                return;
            }
            pendingInterstitial = onDone;
            MaxSdk.ShowInterstitial(interstitialId, placement);
        }

        public void ShowRewarded(string placement, Action<bool> onDone)
        {
            if (!Ready || string.IsNullOrEmpty(rewardedId) || !MaxSdk.IsRewardedAdReady(rewardedId))
            {
                FakeAdProvider.ShowOverlay(Loc.T("No video available right now.\nPlease try again in a moment.", "Chưa có video.\nThử lại sau ít phút nhé."), 1.4f, () => onDone?.Invoke(false));
                return;
            }
            rewardEarned = false;
            pendingRewarded = onDone;
            MaxSdk.ShowRewardedAd(rewardedId, placement);
        }

        private void LoadInterstitial()
        {
            if (!string.IsNullOrEmpty(interstitialId)) MaxSdk.LoadInterstitial(interstitialId);
        }

        private void LoadRewarded()
        {
            if (!string.IsNullOrEmpty(rewardedId)) MaxSdk.LoadRewardedAd(rewardedId);
        }

        // MAX callbacks arrive on the main thread; the ad's own UI is gone by the time Hidden fires.
        private void FinishInterstitial()
        {
            var done = pendingInterstitial;
            pendingInterstitial = null;
            done?.Invoke();
        }

        private void FinishRewarded()
        {
            var done = pendingRewarded;
            pendingRewarded = null;
            done?.Invoke(rewardEarned);
        }

        // Exponential back-off (2, 4, 8 … 64 s) as MAX recommends after a failed load.
        private static void Retry(ref int attempt, Action load)
        {
            attempt = Math.Min(attempt + 1, 6);
            RetryTimer.After(Mathf.Pow(2, attempt), load);
        }

        private class RetryTimer : MonoBehaviour
        {
            public static void After(float seconds, Action action)
            {
                var go = new GameObject("MaxRetry");
                DontDestroyOnLoad(go);
                var t = go.AddComponent<RetryTimer>();
                t.remaining = seconds;
                t.action = action;
            }

            private float remaining;
            private Action action;

            private void Update()
            {
                remaining -= Time.unscaledDeltaTime;
                if (remaining > 0f) return;
                Destroy(gameObject);
                action?.Invoke();
            }
        }
    }
}
