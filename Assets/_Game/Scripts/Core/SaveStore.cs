using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>Thin PlayerPrefs wrapper with a key prefix and JSON helpers.</summary>
    public static class SaveStore
    {
        private const string Prefix = "cg.v1.";

        public static int GetInt(string key, int fallback = 0) => PlayerPrefs.GetInt(Prefix + key, fallback);
        public static void SetInt(string key, int value) => PlayerPrefs.SetInt(Prefix + key, value);

        public static bool GetBool(string key, bool fallback) => PlayerPrefs.GetInt(Prefix + key, fallback ? 1 : 0) == 1;
        public static void SetBool(string key, bool value) => PlayerPrefs.SetInt(Prefix + key, value ? 1 : 0);

        public static string GetString(string key, string fallback = "") => PlayerPrefs.GetString(Prefix + key, fallback);
        public static void SetString(string key, string value) => PlayerPrefs.SetString(Prefix + key, value);

        public static T GetJson<T>(string key) where T : new()
        {
            var raw = GetString(key);
            if (string.IsNullOrEmpty(raw)) return new T();
            try { return JsonUtility.FromJson<T>(raw) ?? new T(); }
            catch { return new T(); }
        }

        public static void SetJson<T>(string key, T value) => SetString(key, JsonUtility.ToJson(value));

        public static bool Has(string key) => PlayerPrefs.HasKey(Prefix + key);

        public static void Delete(string key) => PlayerPrefs.DeleteKey(Prefix + key);

        /// <summary>Stores max(current, value) and returns the new best.</summary>
        public static int SubmitBest(string key, int value)
        {
            var best = Mathf.Max(GetInt(key), value);
            SetInt(key, best);
            SaveSoon();
            return best;
        }

        public static void Save()
        {
            PlayerPrefs.Save();
            pending = false;
            lastSave = Time.unscaledTime;
        }

        // PlayerPrefs.Save() writes the whole file to disk. Per-move state (run in progress, a best beaten on every
        // move) goes through here instead: at most one write every few seconds, and whatever is still pending is
        // written when the app goes to the background or quits (AppEvents), which is when Android may kill it.
        private const float SaveInterval = 5f;
        private static bool pending;
        private static float lastSave = -SaveInterval;

        public static void SaveSoon()
        {
            if (Time.unscaledTime - lastSave >= SaveInterval) Save();
            else pending = true;
        }

        /// <summary>Called every frame by AppEvents: writes a pending save once the interval has passed.</summary>
        internal static void FlushIfDue()
        {
            if (pending && Time.unscaledTime - lastSave >= SaveInterval) Save();
        }
    }
}
