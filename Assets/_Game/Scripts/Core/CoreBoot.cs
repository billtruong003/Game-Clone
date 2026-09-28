using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace CasualGame.Core
{
    /// <summary>Runs before the first scene: persistent services, frame rate, and an EventSystem in every scene.</summary>
    public static class CoreBoot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            Application.targetFrameRate = 60;
            Input.multiTouchEnabled = false;
            GameAudio.Create();
            SceneManager.sceneLoaded += (_, _) => EnsureEventSystem();
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            UnityEngine.Object.DontDestroyOnLoad(go);
        }
    }

    /// <summary>Scene switch behind a quick fade.</summary>
    public static class SceneFlow
    {
        public static void Load(string sceneName)
        {
            var canvas = UIKit.CreateOverlayCanvas("Fade", 9000);
            UnityEngine.Object.DontDestroyOnLoad(canvas.gameObject);
            var rt = UIKit.Stretch(UIKit.Rect("Black", canvas.transform));
            UIKit.AddImage(rt, (Sprite)null, UIKit.Ink).raycastTarget = true;
            var group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            Tween.Fade(group, 1f, 0.18f, 0f, () =>
            {
                SceneManager.LoadScene(sceneName);
                Tween.Fade(group, 0f, 0.25f, 0.05f, () => UnityEngine.Object.Destroy(canvas.gameObject));
            });
        }
    }
}
