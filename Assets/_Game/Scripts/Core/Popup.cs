using System;
using System.Collections.Generic;
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
        /// <summary>What the Android Back button does while this is the top popup; null = Back is ignored (e.g. game over).</summary>
        public Action OnBack;
        private static readonly List<Popup> open = new();
        private float cursor;
        private readonly float width;

        public static Popup Open(Transform parent, string title, float height = 1150f, float width = 900f)
        {
            var p = new Popup(width);
            p.Root = UIKit.Stretch(UIKit.Rect("Popup", parent));
            // own canvas above everything else, world-space FX particles included
            var parentCanvas = parent.GetComponentInParent<Canvas>();
            var c = p.Root.gameObject.AddComponent<Canvas>();
            c.overrideSorting = true;
            if (parentCanvas != null) c.sortingLayerID = parentCanvas.rootCanvas.sortingLayerID;
            c.sortingOrder = 1000;
            p.Root.gameObject.AddComponent<GraphicRaycaster>();
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
            open.Add(p);
            return p;
        }

        public static bool AnyOpen
        {
            get
            {
                open.RemoveAll(x => x.Root == null);
                return open.Count > 0;
            }
        }

        /// <summary>Routes Back to the top popup. True when a popup is open (Back is consumed even if it ignores it).</summary>
        internal static bool HandleBack()
        {
            if (!AnyOpen) return false;
            open[^1].OnBack?.Invoke();
            return true;
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
            open.Remove(this);
            Tween.Scale(Card, Vector3.one * 0.8f, 0.14f, Ease.InBack, 0f, () =>
            {
                UnityEngine.Object.Destroy(root.gameObject);
                after?.Invoke();
            });
        }
    }

    /// <summary>
    /// Shared pause / settings popup (G3). In a game (caller passes actions): "Paused" with Resume on top, the caller's
    /// actions, a row of three round toggles (sound, music, vibration), Remove ads (or "Ads removed"), and the privacy
    /// links. On a menu (no actions): "Settings" with the same toggles and links.
    /// </summary>
    public static class SettingsPopup
    {
        public static Popup Show(Transform parent, Action onClose, params (string label, string sprite, string icon, Action action)[] actions)
        {
            var inGame = actions.Length > 0;
            var popup = Popup.Open(parent, inGame ? Loc.T("Paused", "Tạm dừng") : Loc.T("Settings", "Cài đặt"), 1400);
            popup.OnBack = () => popup.Close(onClose);
            if (inGame) popup.Button("btn_green", Loc.T("Resume", "Tiếp tục"), () => popup.Close(onClose), "icon_play");
            foreach (var a in actions)
            {
                var action = a.action;
                popup.Button("btn_white", a.label, () => popup.Close(action), a.icon);
            }

            var row = popup.Row(150);
            RoundToggle(row, -170, "icon_sound_on", () => GameSettings.Sound, v => GameSettings.Sound = v);
            RoundToggle(row, 0, "icon_music", () => GameSettings.Music, v => GameSettings.Music = v);
            RoundToggle(row, 170, "icon_vibrate", () => GameSettings.Vibration, v => GameSettings.Vibration = v);

            if (!Ads.RemoveAdsOwned)
                popup.Button("btn_yellow", Loc.T("Remove ads", "Gỡ quảng cáo"), () => Store.BuyRemoveAds(ok => { if (ok) popup.Close(onClose); }), "icon_noads");
            else
            {
                var chip = popup.Row(90);
                UIKit.Image(chip, "icon_check", new Vector2(0.5f, 0.5f), new Vector2(-170, 0), new Vector2(56, 56)).color = UIKit.Hex("#1E7A55");
                UIKit.Label(chip, Loc.T("Ads removed", "Đã gỡ quảng cáo"), 46, new Vector2(0.5f, 0.5f), new Vector2(30, 0), new Vector2(420, 80), UIKit.Hex("#1E7A55"));
            }

            // privacy links: the policy always, Google's options form only where UMP requires it
            if (Privacy.HasPolicy || Privacy.OptionsRequired)
            {
                var links = popup.Row(80);
                var both = Privacy.HasPolicy && Privacy.OptionsRequired;
                if (Privacy.HasPolicy) Link(links, Loc.T("Privacy policy", "Chính sách bảo mật"), both ? -210 : 0, Privacy.OpenPolicy);
                if (Privacy.OptionsRequired) Link(links, Loc.T("Privacy options", "Quyền riêng tư"), both ? 210 : 0, Privacy.ShowOptions);
            }
            var credit = popup.Row(60);
            Link(credit, Loc.T("Made by Bill The Dev", "Làm bởi Bill The Dev"), 0, Credits.Open);
            if (Debug.isDebugBuild && AdMobAdProvider.Ready) // test builds only: Google's check of app id, ad units, consent, networks
                popup.Button("btn_gray", "Ad inspector", AdMobAdProvider.OpenInspector, "icon_ad");
            var close = UIKit.IconButton(popup.Card, "round_white", "icon_close", () => popup.Close(onClose), new Vector2(1f, 1f), new Vector2(-40, -40), 110);
            close.transform.SetAsLastSibling();
            return popup.Fit();
        }

        // Round on/off button: yellow when on, grey with a red slash when off.
        private static void RoundToggle(RectTransform row, float x, string icon, Func<bool> get, Action<bool> set)
        {
            Button btn = null;
            Image slash = null;
            void Paint()
            {
                var on = get();
                btn.GetComponent<Image>().sprite = ArtLibrary.Instance.Get(on ? "round_yellow" : "round_white");
                slash.enabled = !on;
            }
            btn = UIKit.IconButton(row, "round_yellow", icon, () => { set(!get()); Paint(); }, new Vector2(0.5f, 0.5f), new Vector2(x, 0), 140);
            slash = UIKit.Image(btn.transform, "round_rect", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120, 14), UIKit.Hex("#FF5A5F"));
            slash.rectTransform.localEulerAngles = new Vector3(0, 0, 45);
            Paint();
        }

        private static void Link(RectTransform row, string text, float x, Action onClick)
        {
            var t = UIKit.Label(row, $"<u>{text}</u>", 40, new Vector2(0.5f, 0.5f), new Vector2(x, 0), new Vector2(400, 70), UIKit.Muted);
            t.raycastTarget = true;
            var b = t.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { GameAudio.Play("ui_click"); onClick(); });
        }
    }

    /// <summary>Studio credit: every "Bill The Dev" line opens the studio site.</summary>
    public static class Credits
    {
        public const string Url = "https://www.billthedev.com";

        public static void Open() => Application.OpenURL(Url);
    }
}
