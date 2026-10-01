using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CasualGame.Core
{
    /// <summary>
    /// The coins block of a result card (mockup SHOP_Coins, FEATURE_SPEC SH1): a "+N" pill, then a row with
    /// "x2 coins" (rewarded video, once per run) and "Shop". The coins are already in the wallet when the card shows.
    /// </summary>
    public static class CoinReward
    {
        /// <param name="doubled">The run's coins were doubled already (the card is shown again after the shop).</param>
        /// <param name="onDoubled">Remember that the x2 was used.</param>
        public static void Add(Popup p, int earned, bool doubled, string placement, Action onDoubled, Action openShop)
        {
            var shown = doubled ? earned * 2 : earned;
            var row = p.Row(92);
            var pill = UIKit.Pill(row, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260, 84), UIKit.Hex("#FFF1C2"), out _);
            UIKit.Image(pill, "icon_coin", new Vector2(0f, 0.5f), new Vector2(56, 2), new Vector2(64, 64)).preserveAspect = true;
            var text = UIKit.Label(pill, "+" + Wallet.Format(shown), 56, new Vector2(0.5f, 0.5f), new Vector2(30, 4), new Vector2(180, 80));
            UIKit.SetPillWidth(pill, Mathf.Max(220f, text.GetPreferredValues(text.text).x + 150f));

            var buttons = p.Row(132);
            Button x2 = null;
            if (earned > 0 && !doubled)
            {
                x2 = UIKit.Button(buttons, "btn_blue", Loc.T("x2 coins", "x2 xu"), null, new Vector2(0.5f, 0.5f), new Vector2(-150, 0), new Vector2(380, 132), "icon_ad", 50);
                var busy = false;
                x2.onClick.AddListener(() =>
                {
                    if (busy) return;
                    busy = true;
                    Ads.ShowRewarded(placement, ok =>
                    {
                        busy = false;
                        if (!ok) { if (p.Root != null) Toast.Show(p.Root, Ads.NoVideoText); return; }
                        Wallet.Add(earned);
                        onDoubled?.Invoke();
                        if (text == null) return;
                        text.text = "+" + Wallet.Format(earned * 2);
                        Tween.Punch(pill, 0.3f);
                        GameAudio.Play("star");
                        UIKit.SetInteractable(x2, false);
                    });
                });
            }
            UIKit.Button(buttons, "btn_yellow", Loc.T("Shop", "Cửa hàng"), openShop, new Vector2(0.5f, 0.5f),
                new Vector2(x2 != null ? 210 : 0, 0), new Vector2(300, 132), "icon_bag", 50);
        }
    }
}
