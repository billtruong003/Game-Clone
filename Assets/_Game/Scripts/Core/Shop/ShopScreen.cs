using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CasualGame.Core
{
    /// <summary>What a game draws inside the shop: the small preview of a card and the big live preview on top.</summary>
    public interface IShopPainter
    {
        /// <summary>The shop's ground (the game's own background).</summary>
        Color Background { get; }
        /// <summary>A card's preview strip (its own coloured box included).</summary>
        void Card(RectTransform area, SkinDef skin);
        /// <summary>The big preview: the pieces skin on the scene skin. Called again into a cleared area whenever either changes.</summary>
        void Preview(RectTransform area, SkinDef pieces, SkinDef scene);
    }

    /// <summary>
    /// The shop (mockup "Shop &amp; Skins", FEATURE_SPEC SH3/SH4). Top: back, title, coins. Below: the big live preview,
    /// the tabs and a 2-column grid of skin cards; a fixed "Unlock all skins" button at the bottom.
    /// Tapping a card tries it on the preview (nothing is bought); its button buys or wears it: coins (yellow),
    /// rewarded videos (blue, n/3), premium (purple, Google Play price), owned ("Use"), worn ("In use").
    /// Lives on its own canvas layer above the game (400), under effects (500) and popups (1000).
    /// </summary>
    public sealed class ShopScreen
    {
        private static readonly Color Yellow = UIKit.Hex("#FFD23F"), Purple = UIKit.Hex("#9B5DE5");
        private const float CardW = 476f, CardH = 376f, CardGap = 32f, RowStep = CardH + 32f;
        private const float GridTop = 940f, GridBottom = 180f;

        private readonly SkinCatalog catalog;
        private readonly IShopPainter painter;
        private readonly Action onClose;
        private readonly RectTransform page, previewArea, content;
        private RectTransform root;
        private readonly TextMeshProUGUI coinsText, tryChip;
        private readonly RectTransform tryChipBox, tryChipFill;
        private readonly Button unlockNow;
        private readonly SkinDef[] tried;
        private readonly List<(SkinDef skin, RectTransform card, Button button)> cards = new();
        private readonly List<(Image bg, TextMeshProUGUI label)> tabButtons = new();
        private Button allSkins;
        private int tab;
        // a light ground (Bruh Arrows' paper) takes ink text; a dark one cream text
        private readonly bool light;
        private Color TextOnGround => light ? UIKit.Ink : UIKit.Paper;

        public static ShopScreen Open(Transform parent, IShopPainter painter, Action onClose = null)
        {
            var catalog = Skins.Catalog;
            return catalog == null ? null : new ShopScreen(parent, catalog, painter, onClose);
        }

        private ShopScreen(Transform parent, SkinCatalog catalog, IShopPainter painter, Action onClose)
        {
            this.catalog = catalog;
            this.painter = painter;
            this.onClose = onClose;
            light = painter.Background.grayscale > 0.6f;
            tried = new SkinDef[catalog.Tabs.Length];
            for (int t = 0; t < tried.Length; t++) tried[t] = Skins.Equipped(t);

            root = UIKit.Stretch(UIKit.Rect("Shop", parent));
            var parentCanvas = parent.GetComponentInParent<Canvas>();
            var c = root.gameObject.AddComponent<Canvas>();
            c.overrideSorting = true;
            if (parentCanvas != null) c.sortingLayerID = parentCanvas.rootCanvas.sortingLayerID;
            c.sortingOrder = 400;
            root.gameObject.AddComponent<GraphicRaycaster>();
            UIKit.AddImage(root, (Sprite)null, painter.Background).raycastTarget = true;
            page = UIKit.Stretch(UIKit.Rect("Page", root));
            page.gameObject.AddComponent<SafeArea>();

            var top = new Vector2(0.5f, 1f);
            UIKit.IconButton(page, "round_white", "icon_back", Close, new Vector2(0f, 1f), new Vector2(100, -100), 116);
            var title = UIKit.Label(page, Loc.T("Shop", "Cửa hàng"), 88, new Vector2(0f, 1f), new Vector2(420, -100), new Vector2(400, 120), TextOnGround);
            title.alignment = TextAlignmentOptions.Left;
            var pill = UIKit.Image(page, "btn_white", new Vector2(1f, 1f), new Vector2(-206, -108), new Vector2(324, 108));
            UIKit.Image(pill.transform, "icon_coin", new Vector2(0f, 0.5f), new Vector2(62, 2), new Vector2(64, 64)).preserveAspect = true;
            coinsText = UIKit.Label(pill.transform, "", 52, new Vector2(0.5f, 0.5f), new Vector2(30, 4), new Vector2(220, 90));

            // big preview
            // mockup: a 10 px ink frame with round corners around the live preview
            var frame = UIKit.Image(page, "round_rect", top, new Vector2(0, -200 - 288), new Vector2(984, 576), UIKit.Ink);
            frame.pixelsPerUnitMultiplier = 0.5f; // corner radius 56
            previewArea = UIKit.Place(UIKit.Rect("Preview", frame.transform), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(964, 556));
            previewArea.gameObject.AddComponent<RectMask2D>();
            // "Candy · try it": a paper pill with an ink edge
            tryChipBox = UIKit.Pill(page, new Vector2(0f, 1f), new Vector2(88, -256), new Vector2(372, 68), UIKit.Ink, out _);
            tryChipBox.pivot = new Vector2(0f, 0.5f);
            tryChipBox.anchoredPosition = new Vector2(80, -256);
            tryChipFill = UIKit.Pill(tryChipBox, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360, 56), UIKit.Paper, out _);
            tryChip = UIKit.Label(tryChipBox, "", 38);
            UIKit.Stretch(tryChip.rectTransform).offsetMin = new Vector2(24, 2);
            tryChip.rectTransform.offsetMax = new Vector2(-24, 0);
            tryChip.enableWordWrapping = false;
            unlockNow = UIKit.Button(page, "btn_purple", "", null, top, new Vector2(286, -264), new Vector2(340, 80), null, 34); // top-right of the preview, across from the "try it" chip
            unlockNow.GetComponentInChildren<TextMeshProUGUI>().color = UIKit.Paper;
            unlockNow.onClick.AddListener(() => BuyProduct(tried[tab]));

            // tabs
            var tabW = 300f;
            for (int t = 0; t < catalog.Tabs.Length; t++)
            {
                var x = (t - (catalog.Tabs.Length - 1) / 2f) * (tabW + 24f);
                var rt = UIKit.Pill(page, top, new Vector2(x, -858), new Vector2(tabW, 100), Color.white, out var bg);
                bg.raycastTarget = true;
                var b = rt.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                var index = t;
                b.onClick.AddListener(() => { GameAudio.Play("ui_click"); SelectTab(index); });
                rt.gameObject.AddComponent<PressScale>();
                var label = UIKit.Label(rt, Loc.T(catalog.Tabs[t].en, catalog.Tabs[t].vi), 44);
                UIKit.Stretch(label.rectTransform);
                tabButtons.Add((bg, label));
            }

            // grid
            var viewport = UIKit.Rect("Grid", page);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(0, GridBottom);
            viewport.offsetMax = new Vector2(0, -GridTop + 30);
            viewport.gameObject.AddComponent<RectMask2D>();
            UIKit.AddImage(viewport, (Sprite)null, new Color(0, 0, 0, 0)).raycastTarget = true; // drag anywhere
            content = UIKit.Rect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;

            Skins.Changed += Refresh;
            Wallet.Changed += Refresh;
            Store.PurchasesChanged += Refresh;
            root.gameObject.AddComponent<OnDestroyed>().Action = () =>
            {
                Skins.Changed -= Refresh;
                Wallet.Changed -= Refresh;
                Store.PurchasesChanged -= Refresh;
            };

#if UNITY_EDITOR
            AddDevButton();
#endif
            SelectTab(0);
            page.localScale = Vector3.one * 0.96f;
            Tween.Scale(page, Vector3.one, 0.22f, Ease.OutBack);
            GameAudio.Play("ui_open");
        }

        public bool IsOpen => root != null;

        public void Close()
        {
            if (root == null) return;
            GameAudio.Play("ui_close");
            var r = root;
            root = null;
            UnityEngine.Object.Destroy(r.gameObject);
            onClose?.Invoke();
        }

#if UNITY_EDITOR
        // Editor only (never in a build): a DEV pill left of the coins with the shop cheats, for testing every skin in play.
        private void AddDevButton()
        {
            var dev = UIKit.Button(page, "btn_red", "DEV", null, new Vector2(1f, 1f), new Vector2(-460, -108), new Vector2(150, 80), null, 34);
            dev.onClick.AddListener(() =>
            {
                var gameId = catalog.GameId;
                var p = Popup.Open(root, "DEV", 900, 800);
                p.OnBack = () => p.Close();
                p.Button("btn_green", "Unlock all skins", () => { Store.EditorSetOwned(gameId, Store.FullGame, true); p.Close(); }, null, 600, 130);
                p.Button("btn_yellow", "+1000 coins", () => { Wallet.EditorAdd(gameId, 1000); p.Close(); }, null, 600, 130);
                p.Button("btn_white", "Lock all + reset coins", () =>
                {
                    Store.EditorSetOwned(gameId, Store.FullGame, false);
                    Store.EditorSetOwned(gameId, Store.AllSkins, false);
                    foreach (var s in catalog.Skins)
                    {
                        if (s.HasProduct) Store.EditorSetOwned(gameId, s.Id, false);
                        SaveStore.Delete($"skin.{gameId}.own.{s.Id}");
                        SaveStore.Delete($"skin.{gameId}.ads.{s.Id}");
                    }
                    for (int t = 0; t < catalog.Tabs.Length; t++) SaveStore.Delete($"skin.{gameId}.eq.{t}");
                    Wallet.EditorAdd(gameId, -999999);
                    for (int t = 0; t < tried.Length; t++) tried[t] = Skins.Equipped(t);
                    Skins.RaiseChanged();
                    p.Close();
                }, null, 600, 130);
                p.Fit();
            });
        }
#endif

        private void SelectTab(int t)
        {
            tab = t;
            for (int i = 0; i < tabButtons.Count; i++)
            {
                var on = i == t;
                tabButtons[i].bg.color = on ? Yellow : light ? new Color(UIKit.Ink.r, UIKit.Ink.g, UIKit.Ink.b, 0.1f) : new Color(1f, 1f, 1f, 0.12f);
                tabButtons[i].label.color = on ? UIKit.Ink : TextOnGround;
            }
            foreach (Transform child in content) UnityEngine.Object.Destroy(child.gameObject);
            cards.Clear();
            int n = 0;
            foreach (var skin in catalog.InTab(t))
            {
                var col = n % 2;
                var row = n / 2;
                var card = UIKit.Place(UIKit.Rect(skin.Id, content), new Vector2(0.5f, 1f),
                    new Vector2((col == 0 ? -1 : 1) * (CardW + CardGap) / 2f, -40 - CardH / 2f - row * RowStep), new Vector2(CardW, CardH));
                BuildCard(card, skin);
                n++;
            }
            content.sizeDelta = new Vector2(0, 40 + Mathf.CeilToInt(n / 2f) * RowStep + 40);
            content.anchoredPosition = Vector2.zero;
            Refresh();
        }

        private void BuildCard(RectTransform card, SkinDef skin)
        {
            // mockup: the card sits on a short dark shadow
            var shadow = UIKit.Image(card, "round_rect", new Vector2(0.5f, 0.5f), new Vector2(0, -12), new Vector2(CardW, CardH), new Color(0.05f, 0.06f, 0.14f, 0.25f));
            shadow.pixelsPerUnitMultiplier = 0.64f;
            var bg = UIKit.Image(card, "card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CardW, CardH));
            bg.raycastTarget = true;
            card.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0); // the tap target
            card.GetComponent<Image>().raycastTarget = true;
            var tap = card.gameObject.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(() => Try(skin));
            var art = UIKit.Place(UIKit.Rect("Art", card), new Vector2(0.5f, 1f), new Vector2(0, -24 - 75), new Vector2(CardW - 48, 150));
            painter.Card(art, skin);
            UIKit.Label(card, skin.Name, 44, new Vector2(0.5f, 1f), new Vector2(0, -216), new Vector2(CardW - 40, 60));
            var button = UIKit.Button(card, "btn_white", "", () => Act(skin), new Vector2(0.5f, 0f), new Vector2(0, 70), new Vector2(CardW - 64, 100), null, 44);
            if (skin.Price == SkinPrice.Premium)
            {
                var tag = UIKit.Pill(card, new Vector2(0f, 1f), new Vector2(70, 4), new Vector2(170, 48), Purple, out _);
                var t = UIKit.Label(tag, Loc.T("PREMIUM", "CAO CẤP"), 28, UIKit.Paper);
                UIKit.Stretch(t.rectTransform);
            }
            cards.Add((skin, card, button));
        }

        private void Refresh()
        {
            if (root == null) return;
            coinsText.text = Wallet.Format(Wallet.Coins);
            foreach (var (skin, card, button) in cards) PaintButton(skin, card, button);

            // preview: the tried skin of this tab over what is worn on the others
            // a fresh container per paint: the painter draws its own ground on it
            foreach (Transform child in previewArea) UnityEngine.Object.Destroy(child.gameObject);
            var look = UIKit.Stretch(UIKit.Rect("Look", previewArea));
            var pieces = tried.Length > 0 ? tried[0] : null;
            var scene = tried.Length > 1 ? tried[1] : null;
            painter.Preview(look, pieces, scene);
            var shown = tried[tab];
            var trying = shown != null && shown != Skins.Equipped(tab);
            tryChipBox.gameObject.SetActive(trying);
            if (trying)
            {
                tryChip.text = Loc.F("{0} · try it", "{0} · thử", shown.Name);
                var w = tryChip.GetPreferredValues(tryChip.text).x + 56;
                UIKit.SetPillWidth(tryChipBox, w + 12);
                UIKit.SetPillWidth(tryChipFill, w);
            }
            var offer = shown != null && shown.Price == SkinPrice.Coins && !Skins.Owns(shown);
            unlockNow.gameObject.SetActive(offer);
            if (offer)
                unlockNow.GetComponentInChildren<TextMeshProUGUI>().text = Loc.F("Unlock now · {0}", "Mở ngay · {0}", Store.Price(shown.Id, shown.UsdPrice));
            unlockNow.transform.SetAsLastSibling();

            var bundleOwned = Store.Owns(Store.AllSkins) || Store.Owns(Store.FullGame);
            if (allSkins == null && !bundleOwned)
            {
                allSkins = UIKit.Button(page, "btn_yellow", "", () => AllSkinsPopup.Show(root), new Vector2(0.5f, 0f), new Vector2(0, 92), new Vector2(984, 120), null, 50);
            }
            if (allSkins != null)
            {
                allSkins.gameObject.SetActive(!bundleOwned);
                allSkins.GetComponentInChildren<TextMeshProUGUI>().text =
                    Loc.F("Unlock all skins · {0}", "Mở mọi skin · {0}", Store.Price(Store.AllSkins, Store.UsdAllSkins));
            }
        }

        private void PaintButton(SkinDef skin, RectTransform card, Button button)
        {
            var img = button.GetComponent<Image>();
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            var icon = button.transform.Find("Icon") as RectTransform;
            if (icon == null)
            {
                icon = UIKit.Image(button.transform, "icon_coin", new Vector2(0f, 0.5f), new Vector2(76, 4), new Vector2(64, 64)).rectTransform;
                icon.name = "Icon";
                icon.GetComponent<Image>().preserveAspect = true;
            }
            var iconImg = icon.GetComponent<Image>();
            string sprite, text, iconName = null;
            var textColor = UIKit.Ink;
            if (skin == Skins.Equipped(skin.Tab)) { sprite = "btn_green"; text = Loc.T("In use", "Đang dùng"); iconName = "icon_check"; }
            else if (Skins.Owns(skin)) { sprite = "btn_white"; text = Loc.T("Use", "Dùng"); }
            else switch (skin.Price)
            {
                case SkinPrice.Coins:
                    sprite = Wallet.Coins >= skin.Cost ? "btn_yellow" : "btn_gray";
                    text = Wallet.Format(skin.Cost);
                    iconName = "icon_coin";
                    break;
                case SkinPrice.Ads:
                    sprite = "btn_blue";
                    text = Loc.F("Watch {0}/{1}", "Xem {0}/{1}", Skins.AdsWatched(skin), skin.Cost);
                    iconName = "icon_ad";
                    break;
                default:
                    sprite = "btn_purple";
                    text = Store.Price(skin.Id, skin.UsdPrice);
                    textColor = UIKit.Paper;
                    break;
            }
            img.sprite = ArtLibrary.Instance.Get(sprite);
            label.text = text;
            label.color = textColor;
            icon.gameObject.SetActive(iconName != null);
            if (iconName != null) iconImg.sprite = ArtLibrary.Instance.Get(iconName);
            label.rectTransform.offsetMin = new Vector2(iconName != null ? 110 : 20, 14);
        }

        private void Try(SkinDef skin)
        {
            GameAudio.Play("ui_click");
            tried[skin.Tab] = skin;
            Refresh();
        }

        private void Act(SkinDef skin)
        {
            tried[skin.Tab] = skin;
            if (Skins.Owns(skin))
            {
                if (skin != Skins.Equipped(skin.Tab)) Skins.Equip(skin);
                Refresh();
                return;
            }
            switch (skin.Price)
            {
                case SkinPrice.Coins:
                    if (Skins.TryBuyWithCoins(skin)) Celebrate(skin);
                    else
                    {
                        // the shake runs on the label (its own tween owner), so the button's press spring is left alone
                        var entry = cards.Find(x => x.skin == skin);
                        var label = entry.button.GetComponentInChildren<TextMeshProUGUI>().rectTransform;
                        Tween.Kill(label);
                        PaintButton(entry.skin, entry.card, entry.button); // puts the label back where it belongs
                        var home = label.anchoredPosition;
                        Tween.Loop(label, s => label.anchoredPosition = home + new Vector2(Mathf.Sin(s * 60f) * 14f * Mathf.Max(0f, 1f - s * 3f), 0f));
                        Tween.Delay(label, 0.35f, () => { Tween.Kill(label); label.anchoredPosition = home; });
                        GameAudio.Play("blocked");
                        Toast.Show(root, Loc.F("Not enough coins · {0} more", "Chưa đủ xu · thiếu {0}", Wallet.Format(skin.Cost - Wallet.Coins)));
                        Refresh();
                    }
                    break;
                case SkinPrice.Ads:
                    Ads.ShowRewarded("shop_" + skin.Id, ok =>
                    {
                        if (!ok) { if (root != null) Toast.Show(root, Ads.NoVideoText); return; }
                        if (Skins.AddAdView(skin)) Celebrate(skin);
                    });
                    break;
                case SkinPrice.Premium:
                    BuyProduct(skin);
                    break;
            }
        }

        private void BuyProduct(SkinDef skin)
        {
            if (skin == null || Skins.Owns(skin)) return;
            Store.Buy(skin.Id, skin.Name, ok => { if (ok && Skins.Owns(skin)) Celebrate(skin); });
        }

        private void Celebrate(SkinDef skin)
        {
            Skins.Equip(skin);
            if (root == null) return;
            GameAudio.Play("win");
            GameAudio.Haptic(HapticLevel.Medium);
            var entry = cards.Find(x => x.skin == skin);
            if (entry.card != null)
            {
                entry.card.localScale = Vector3.one * 1.12f;
                Tween.Scale(entry.card, Vector3.one, 0.35f, Ease.OutBack);
                GameFx.Play("Win_Confetti", entry.card.position, 0.6f);
            }
            GameFx.Play("Win_Confetti", previewArea.position, 1f);
        }
    }

    /// <summary>
    /// The bundles (mockup SHOP_AllSkins): All Skins, Full Game (all skins + no ads, best value), Restore purchases.
    /// One-time purchases; prices come from Google Play.
    /// </summary>
    public static class AllSkinsPopup
    {
        public static Popup Show(Transform parent)
        {
            var p = Popup.Open(parent, Loc.T("All Skins", "Trọn bộ skin"), 1300);
            p.OnBack = () => p.Close();
            void Line(string text)
            {
                var row = p.Row(76);
                UIKit.Image(row, "icon_check", new Vector2(0f, 0.5f), new Vector2(60, 0), new Vector2(56, 56)).color = UIKit.Hex("#1E7A55");
                var t = UIKit.Label(row, text, 40, new Vector2(0f, 0.5f), new Vector2(110 + 330, 0), new Vector2(660, 76));
                t.alignment = TextAlignmentOptions.Left;
                t.enableAutoSizing = true;
                t.fontSizeMin = 28;
                t.fontSizeMax = 40;
            }
            Line(Loc.T("Every premium set", "Mọi bộ cao cấp"));
            Line(Loc.T("Every colour set and scene", "Mọi bộ màu và khung cảnh"));
            Line(Loc.T("New skins added later, too", "Cả các skin ra sau này"));
            p.Space(16);
            var full = p.Button("btn_green", Loc.F("Full Game · {0}", "Trọn bộ game · {0}", Store.Price(Store.FullGame, Store.UsdFullGame)),
                () => Store.Buy(Store.FullGame, Loc.T("Full Game", "Trọn bộ game"), ok => { if (ok) p.Close(); }), null, 760, 150);
            var tag = UIKit.Pill(full.transform, new Vector2(1f, 1f), new Vector2(-90, 0), new Vector2(220, 56), UIKit.Hex("#D6334A"), out _);
            var tagText = UIKit.Label(tag, Loc.T("Best value", "Hời nhất"), 30, UIKit.Paper);
            UIKit.Stretch(tagText.rectTransform);
            p.Text(Loc.T("All skins + no ads between rounds", "Mọi skin + không quảng cáo giữa ván"), 36, UIKit.Muted);
            p.Button("btn_yellow", Loc.F("All Skins · {0}", "Trọn bộ skin · {0}", Store.Price(Store.AllSkins, Store.UsdAllSkins)),
                () => Store.Buy(Store.AllSkins, Loc.T("All Skins", "Trọn bộ skin"), ok => { if (ok) p.Close(); }), null, 760, 140);
            p.Space(6);
            var restore = p.Text($"<u>{Loc.T("Restore purchases", "Khôi phục giao dịch")}</u>", 40, UIKit.Muted);
            restore.raycastTarget = true;
            var rb = restore.gameObject.AddComponent<Button>();
            rb.transition = Selectable.Transition.None;
            rb.onClick.AddListener(() =>
            {
                GameAudio.Play("ui_click");
                Store.Restore(ok => Toast.Show(p.Root != null ? p.Root : parent,
                    ok ? Loc.T("Purchases restored", "Đã khôi phục") : Loc.T("Could not reach Google Play", "Không kết nối được Google Play")));
            });
            p.Text(Loc.T("One-time purchases. Prices come from Google Play.", "Mua một lần. Giá theo Google Play."), 32, UIKit.Muted);
            var close = UIKit.IconButton(p.Card, "round_white", "icon_close", () => p.Close(), new Vector2(1f, 1f), new Vector2(-40, -40), 110);
            close.transform.SetAsLastSibling();
            return p.Fit();
        }
    }
}
