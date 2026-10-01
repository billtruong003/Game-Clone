using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CasualGame.Core
{
    /// <summary>
    /// App-level events every game listens to: the Android Back button (Escape in the editor) and going to the
    /// background. Back first closes the top popup that allows it; only if none did is <see cref="Back"/> raised.
    /// Going to the background saves immediately and raises <see cref="Suspended"/> (games open their pause popup).
    /// </summary>
    public sealed class AppEvents : MonoBehaviour
    {
        public static event Action Back;
        public static event Action Suspended;

        private static AppEvents instance;

        internal static void Create()
        {
            if (instance != null) return;
            var go = new GameObject("AppEvents");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<AppEvents>();
        }

        private void Update()
        {
            SaveStore.FlushIfDue();
            var kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
            if (Popup.HandleBack()) return;
            Back?.Invoke();
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused) return;
#if !UNITY_EDITOR // in the editor this fires on every focus change, which is not the app going to the background
            Suspended?.Invoke();
#endif
            SaveStore.Save();
        }

        private void OnApplicationQuit() => SaveStore.Save();
    }
}
