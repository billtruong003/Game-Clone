using System;
using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>
    /// Google Play In-App Review, asked at a happy moment. No "do you like the game?" pre-question (Play forbids
    /// filtering who gets sent to the review): the Play dialog itself decides whether to show. At most once every
    /// 30 days, and only after a few good moments.
    /// </summary>
    public static class ReviewPrompt
    {
        private const int MomentsBeforeFirstAsk = 3;
        private const double DaysBetweenAsks = 30;

        /// <summary>
        /// Call on a good moment (level cleared, new best), AFTER the result card closed (G10). Asks when enough moments
        /// have passed. Returns true when the Play dialog was requested, so the caller can skip an interstitial there.
        /// </summary>
        public static bool GoodMoment()
        {
            var moments = SaveStore.GetInt("review.moments") + 1;
            SaveStore.SetInt("review.moments", moments);
            SaveStore.Save();
            if (moments < MomentsBeforeFirstAsk) return false;

            var last = SaveStore.GetString("review.last", "");
            if (DateTime.TryParse(last, null, System.Globalization.DateTimeStyles.RoundtripKind, out var when) &&
                (DateTime.UtcNow - when).TotalDays < DaysBetweenAsks) return false;

            SaveStore.SetString("review.last", DateTime.UtcNow.ToString("o"));
            SaveStore.Save();
            Launch();
            return true;
        }

        private static void Launch()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                var manager = new AndroidJavaClass("com.google.android.play.core.review.ReviewManagerFactory")
                    .CallStatic<AndroidJavaObject>("create", activity);
                manager.Call<AndroidJavaObject>("requestReviewFlow").Call<AndroidJavaObject>("addOnCompleteListener",
                    new OnComplete(task =>
                    {
                        if (!task.Call<bool>("isSuccessful")) return;
                        manager.Call<AndroidJavaObject>("launchReviewFlow", activity, task.Call<AndroidJavaObject>("getResult"));
                    }));
            }
            catch (Exception e)
            {
                Debug.LogWarning("In-app review unavailable: " + e.Message);
            }
#else
            Debug.Log("ReviewPrompt: would open the Google Play review dialog here");
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private class OnComplete : AndroidJavaProxy
        {
            private readonly Action<AndroidJavaObject> done;
            public OnComplete(Action<AndroidJavaObject> done) : base("com.google.android.gms.tasks.OnCompleteListener") => this.done = done;
            public void onComplete(AndroidJavaObject task) => done(task);
        }
#endif
    }
}
