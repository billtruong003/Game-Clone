using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CasualGame.Core
{
    /// <summary>
    /// Modal panel: dimmed backdrop, cream card, ribbon title, and a top-to-bottom stack of content.
    /// Everything is built from the UI sheet, so it restyles with the art.
    /// </summary>
    public class Popup
    {
        public RectTransform Root { get; private set; }
        public RectTransform Card { get; private set; }
        private float cursor;
        private readonly float width;

        public static Popup Open(Transform parent, string title, float height = 1150f, float width = 900f)
        {
            var p = new Popup(width);
            p.Root = UIKit.Stretch(UIKit.Rect("Popup", parent));
            var dim = UIKit.AddImage(p.Root, (Sprite)null, new Color(0.05f, 0.06f, 0.15f, 0.62f));
            dim.raycastTarget = true;
            p.Card = UIKit.Place(UIKit.Rect("Card", p.Root), new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(width, height));
            UIKit.AddImage(p.Card, "panel");
            if (!string.IsNullOrEmpty(title))
            {
                var ribbon = UIKit.Image(p.Card, "ribbon", new Vector2(0.5f, 1f), new Vector2(0, 10), new Vector2(Mathf.Min(760, width - 40), 170));
                var t = UIKit.Label(ribbon.transform, title, 66);
                UIKit.Stretch(t.rectTransform).offsetMin = new Vector2(90, 28);
                t.rectTransform.offsetMax = new Vector2(-90, -8);
                t.enableAutoSizing = true;
                t.fontSizeMax = 66;
                t.fontSizeMin = 36;
            }
            p.cursor = string.IsNullOrEmpty(title) ? 70f : 150f;
            p.Card.localScale = Vector3.one * 0.7f;
            Tween.Scale(p.Card, Vector3.one, 0.28f, Ease.OutBack);
            GameAudio.Play("ui_open");
            return p;
        }

        private Popup(float width) => this.width = width;

        public void Space(float h) => cursor += h;

        public TextMeshProUGUI Text(string text, float size, Color? color = null, float height = 0f)
        {
            var h = height > 0 ? height : size * 1.35f;
            var t = UIKit.Label(Card, text, size, new Vector2(0.5f, 1f), new Vector2(0, -cursor - h / 2), new Vector2(width - 120, h), color);
            cursor += h + 10;
            return t;
        }

        public Button Button(string sprite, string label, Action onClick, string icon = null, float w = 640f, float h = 150f)
        {
            var b = UIKit.Button(Card, sprite, label, onClick, new Vector2(0.5f, 1f), new Vector2(0, -cursor - h / 2), new Vector2(w, h), icon);
            cursor += h + 22;
            return b;
        }

        /// <summary>A horizontal strip of the given height to lay custom elements into.</summary>
        public RectTransform Row(float h)
        {
            var rt = UIKit.Place(UIKit.Rect("Row", Card), new Vector2(0.5f, 1f), new Vector2(0, -cursor - h / 2), new Vector2(width - 100, h));
            cursor += h + 16;
            return rt;
        }

        /// <summary>Shrinks the card to its content (call after adding everything).</summary>
        public Popup Fit(float bottomPadding = 60f)
        {
            Card.sizeDelta = new Vector2(width, cursor + bottomPadding);
            return this;
        }

        public void Close(Action after = null)
        {
            if (Root == null) return;
            GameAudio.Play("ui_close");
            var root = Root;
            Root = null;
            Tween.Scale(Card, Vector3.one * 0.8f, 0.14f, Ease.InBack, 0f, () =>
            {
                UnityEngine.Object.Destroy(root.gameObject);
                after?.Invoke();
            });
        }
    }

    /// <summary>Shared pause/settings popup: sound, music, vibration, remove ads, plus caller-provided actions.</summary>
    public static class SettingsPopup
    {
        public static Popup Show(Transform parent, Action onClose, params (string label, string sprite, string icon, Action action)[] actions)
        {
            Popup popup = null;
            popup = Popup.Open(parent, "Cài đặt", 520 + 170 * actions.Length + (Ads.RemoveAdsOwned ? 0 : 170));
            ToggleRow(popup, "icon_sound_on", "Âm thanh", () => GameSettings.Sound, v => GameSettings.Sound = v);
            ToggleRow(popup, "icon_play", "Nhạc nền", () => GameSettings.Music, v => GameSettings.Music = v);
            ToggleRow(popup, "icon_vibrate", "Rung", () => GameSettings.Vibration, v => GameSettings.Vibration = v);
            popup.Space(10);
            foreach (var a in actions)
            {
                var action = a.action;
                popup.Button(a.sprite, a.label, () => popup.Close(action), a.icon);
            }
            if (!Ads.RemoveAdsOwned)
                popup.Button("btn_yellow", "Gỡ quảng cáo", () => Store.BuyRemoveAds(_ => popup.Close(onClose)), "icon_noads");
            var close = UIKit.IconButton(popup.Card, "round_white", "icon_close", () => popup.Close(onClose), new Vector2(1f, 1f), new Vector2(-40, -40), 110);
            close.transform.SetAsLastSibling();
            return popup.Fit();
        }

        private static void ToggleRow(Popup popup, string icon, string label, Func<bool> get, Action<bool> set)
        {
            var row = popup.Row(110);
            UIKit.Image(row, icon, new Vector2(0f, 0.5f), new Vector2(60, 0), new Vector2(72, 72)).preserveAspect = true;
            var t = UIKit.Label(row, label, 54, new Vector2(0f, 0.5f), new Vector2(300, 0), new Vector2(380, 100));
            t.alignment = TextAlignmentOptions.Left;
            var toggle = UIKit.Place(UIKit.Rect("Toggle", row), new Vector2(1f, 0.5f), new Vector2(-100, 0), new Vector2(160, 88));
            var img = UIKit.AddImage(toggle, get() ? "toggle_on" : "toggle_off");
            img.raycastTarget = true;
            var btn = toggle.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() =>
            {
                set(!get());
                img.sprite = ArtLibrary.Instance.Get(get() ? "toggle_on" : "toggle_off");
                GameAudio.Play("ui_click");
            });
        }
    }
}
