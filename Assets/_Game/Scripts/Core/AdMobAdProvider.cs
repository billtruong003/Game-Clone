using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>
    /// Google AdMob ads. AdMob mediates the other networks (Unity Ads through its adapter), so the game only talks to
    /// AdMob. Before the first ad, Google's consent form (UMP) is shown to players in regions that need it; ads are
    /// only requested once UMP says so. Interstitial and rewarded are preloaded and reloaded after every show.
    /// </summary>
    public class AdMobAdProvider : IAdProvider
    {
        private readonly string interstitialId, rewardedId;
        private InterstitialAd interstitial;
        private RewardedAd rewarded;
        private int interstitialRetry, rewardedRetry;

        public static bool Ready { get; private set; }

        public AdMobAdProvider(GameConfig config)
        {
            interstitialId = config.interstitialAdUnit;
            rewardedId = config.rewardedAdUnit;
            // consent first (EEA/UK/CH get Google's form); ads start once UMP allows requests
            ConsentInformation.Update(new ConsentRequestParameters(), updateError => Main(() =>
            {
                if (updateError != null) Debug.LogWarning("UMP update: " + updateError.Message);
                ConsentForm.LoadAndShowConsentFormIfRequired(formError => Main(() =>
                {
                    if (formError != null) Debug.LogWarning("UMP form: " + formError.Message);
                    if (ConsentInformation.CanRequestAds()) StartAds();
                }));
            }));
            // consent from an earlier session: no need to wait for the update round trip
            if (ConsentInformation.CanRequestAds()) StartAds();
        }

        private void StartAds()
        {
            if (Ready) return;
            Ready = true;
            MobileAds.Initialize(_ => Main(() =>
            {
                LoadInterstitial();
                LoadRewarded();
            }));
        }

        public void ShowInterstitial(string placement, Action onDone)
        {
            if (interstitial == null || !interstitial.CanShowAd())
            {
                onDone?.Invoke();
                return;
            }
            var ad = interstitial;
            interstitial = null;
            ad.OnAdFullScreenContentClosed += () => Main(() => { ad.Destroy(); LoadInterstitial(); onDone?.Invoke(); });
            ad.OnAdFullScreenContentFailed += _ => Main(() => { ad.Destroy(); LoadInterstitial(); onDone?.Invoke(); });
            ad.Show();
        }

        public void ShowRewarded(string placement, Action<bool> onDone)
        {
            if (rewarded == null || !rewarded.CanShowAd())
            {
                FakeAdProvider.ShowOverlay(Loc.T("No video available right now.\nPlease try again in a moment.", "Chưa có video.\nThử lại sau ít phút nhé."), 1.4f, () => onDone?.Invoke(false));
                if (rewarded == null) LoadRewarded();
                return;
            }
            var ad = rewarded;
            rewarded = null;
            bool earned = false;
            ad.OnAdFullScreenContentClosed += () => Main(() => { ad.Destroy(); LoadRewarded(); onDone?.Invoke(earned); });
            ad.OnAdFullScreenContentFailed += _ => Main(() => { ad.Destroy(); LoadRewarded(); onDone?.Invoke(false); });
            ad.Show(_ => earned = true);
        }

        private void LoadInterstitial()
        {
            if (string.IsNullOrEmpty(interstitialId)) return;
            InterstitialAd.Load(interstitialId, new AdRequest(), (ad, error) => Main(() =>
            {
                if (error != null || ad == null) { Retry(ref interstitialRetry, LoadInterstitial); return; }
                interstitialRetry = 0;
                interstitial = ad;
            }));
        }

        private void LoadRewarded()
        {
            if (string.IsNullOrEmpty(rewardedId)) return;
            RewardedAd.Load(rewardedId, new AdRequest(), (ad, error) => Main(() =>
            {
                if (error != null || ad == null) { Retry(ref rewardedRetry, LoadRewarded); return; }
                rewardedRetry = 0;
                rewarded = ad;
            }));
        }

        /// <summary>Google's Ad Inspector: checks app id, ad units, consent and mediation networks on the device.</summary>
        public static void OpenInspector() => MobileAds.OpenAdInspector(error => { if (error != null) Debug.LogWarning("Ad Inspector: " + error.GetMessage()); });

        // SDK callbacks can arrive on a background thread: game code runs on Unity's main thread
        private static void Main(Action action) => MobileAdsEventExecutor.ExecuteInUpdate(action);

        // Exponential back-off (2, 4, 8 … 64 s) after a failed load.
        private static void Retry(ref int attempt, Action load)
        {
            attempt = Math.Min(attempt + 1, 6);
            RetryTimer.After(Mathf.Pow(2, attempt), load);
        }

        private class RetryTimer : MonoBehaviour
        {
            public static void After(float seconds, Action action)
            {
                var go = new GameObject("AdRetry");
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
