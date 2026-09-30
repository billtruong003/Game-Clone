using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CasualGame.Core
{
    /// <summary>
    /// "Watch an ad for X" button. Greyed with "Loading…" until a rewarded ad is ready; tapping shows the ad over the
    /// popup it sits in. Reward earned → the popup closes and <c>onEarned</c> runs. No video / closed early → the
    /// popup stays and a toast explains, so the player is never stuck on a dead screen.
    /// </summary>
    public sealed class RewardedButton : MonoBehaviour
    {
        private Button button;
        private TextMeshProUGUI label;
        private string text;
        private bool busy, lastReady = true;

        public static Button Add(Popup popup, string text, string placement, Action onEarned)
        {
            var b = popup.Button("btn_blue", text, null, "icon_ad");
            var rb = b.gameObject.AddComponent<RewardedButton>();
            rb.button = b;
            rb.label = b.GetComponentInChildren<TextMeshProUGUI>();
            rb.text = text;
            b.onClick.AddListener(() =>
            {
                if (rb.busy) return;
                rb.busy = true;
                Ads.ShowRewarded(placement, ok =>
                {
                    rb.busy = false;
                    if (ok) popup.Close(onEarned);
                    else if (popup.Root != null) Toast.Show(popup.Root, Ads.NoVideoText);
                });
            });
            rb.Refresh(true);
            return b;
        }

        private void Update() => Refresh(false);

        private void Refresh(bool force)
        {
            var ready = Ads.RewardedReady;
            if (!force && ready == lastReady) return;
            lastReady = ready;
            UIKit.SetInteractable(button, ready);
            label.text = ready ? text : Loc.T("Loading…", "Đang tải…");
        }
    }

    /// <summary>Short message in the middle of the screen (y ≈ 700 of 1920); fades out after 1.5 s and never blocks taps.</summary>
    public static class Toast
    {
        private static readonly System.Collections.Generic.List<RectTransform> live = new();

        public static void Show(Transform root, string message)
        {
            // two toasts at once stack downwards instead of printing over each other
            live.RemoveAll(x => x == null);
            var y = -700f - 124f * live.Count;
            var rt = UIKit.Place(UIKit.Rect("Toast", root), new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(900, 104));
            live.Add(rt);
            var bg = UIKit.AddImage(rt, "round_rect", new Color(0.08f, 0.09f, 0.19f, 0.88f));
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = false;
            var t = UIKit.Label(rt, message, 44, UIKit.Paper);
            t.enableWordWrapping = false;
            UIKit.Stretch(t.rectTransform);
            rt.sizeDelta = new Vector2(Mathf.Min(980f, t.GetPreferredValues(message).x + 90f), 104f); // a pill that fits the text
            var group = rt.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            rt.localScale = Vector3.one * 0.85f;
            Tween.Scale(rt, Vector3.one, 0.2f, Ease.OutBack);
            Tween.Fade(group, 0f, 0.35f, 1.5f, () => UnityEngine.Object.Destroy(rt.gameObject));
        }
    }
}
