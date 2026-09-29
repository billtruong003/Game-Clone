using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>
    /// Privacy entry points required by Google Play and the consent rules: the policy link (always) and Google's
    /// consent form again (only when UMP says the player's region needs a privacy options entry, e.g. EEA/UK).
    /// </summary>
    public static class Privacy
    {
        public static bool OptionsRequired =>
            !Application.isEditor && ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

        public static bool HasPolicy => GameConfig.Current != null && !string.IsNullOrEmpty(GameConfig.Current.privacyPolicyUrl);

        public static void ShowOptions() => ConsentForm.ShowPrivacyOptionsForm(error => { if (error != null) Debug.LogWarning("UMP: " + error.Message); });

        public static void OpenPolicy()
        {
            if (HasPolicy) Application.OpenURL(GameConfig.Current.privacyPolicyUrl);
        }
    }
}
