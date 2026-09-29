using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>
    /// Privacy entry points required by Google Play and the consent rules: the policy link (always) and the
    /// consent form again (only for players in regions where consent applies, e.g. EEA/UK).
    /// </summary>
    public static class Privacy
    {
        public static bool OptionsRequired =>
            MaxAdProvider.Ready && MaxSdk.GetSdkConfiguration().ConsentFlowUserGeography == MaxSdkBase.ConsentFlowUserGeography.Gdpr;

        public static bool HasPolicy => GameConfig.Current != null && !string.IsNullOrEmpty(GameConfig.Current.privacyPolicyUrl);

        public static void ShowOptions() => MaxSdk.CmpService.ShowCmpForExistingUser(_ => { });

        public static void OpenPolicy()
        {
            if (HasPolicy) Application.OpenURL(GameConfig.Current.privacyPolicyUrl);
        }
    }
}
