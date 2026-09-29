using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>
    /// Two-language UI text: English by default, Vietnamese when the device language is Vietnamese.
    /// Both versions sit next to each other at the call site, so a string can never be missing a translation.
    /// </summary>
    public static class Loc
    {
        public static bool Vietnamese { get; set; } = Application.systemLanguage == SystemLanguage.Vietnamese;

        public static string T(string en, string vi) => Vietnamese ? vi : en;

        public static string F(string en, string vi, params object[] args) => string.Format(T(en, vi), args);
    }
}
