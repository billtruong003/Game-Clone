using System;
using System.Collections.Generic;
using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>
    /// The running game's skins: what is owned, what is worn, rewarded-video progress. A skin is owned when it is free,
    /// was bought with coins or by its ad count, or its own product / a skins bundle is owned in Google Play
    /// (<see cref="Store.Owns"/>, re-checked against Play on every start, so a refund takes it back).
    /// Saved per game, so the three games never share a key even in the editor.
    /// </summary>
    public static class Skins
    {
        public static event Action Changed;

        private static readonly Dictionary<string, SkinCatalog> catalogs = new();

        /// <summary>Called by each game assembly before the first scene loads.</summary>
        public static void Register(SkinCatalog catalog) => catalogs[catalog.GameId] = catalog;

        public static SkinCatalog CatalogOf(string gameId) =>
            gameId != null && catalogs.TryGetValue(gameId, out var c) ? c : null;

        /// <summary>The catalog of the game that is running (null in a scene without a config).</summary>
        public static SkinCatalog Catalog => CatalogOf(GameConfig.Current != null ? GameConfig.Current.gameId : null);

        private static string Key(string what) => $"skin.{Catalog?.GameId}.{what}";

        public static bool Owns(SkinDef s)
        {
            if (s == null) return false;
            return s.Price == SkinPrice.Free
                || SaveStore.GetBool(Key("own." + s.Id), false)
                || Store.Owns(Store.AllSkins) || Store.Owns(Store.FullGame)
                || (s.HasProduct && Store.Owns(s.Id));
        }

        /// <summary>The skin worn on a tab: the saved one if it is still owned (a refund falls back to the default).</summary>
        public static SkinDef Equipped(int tab)
        {
            var cat = Catalog;
            if (cat == null) return null;
            var s = cat.Find(SaveStore.GetString(Key("eq." + tab)));
            return s != null && s.Tab == tab && Owns(s) ? s : cat.Default(tab);
        }

        public static void Equip(SkinDef s)
        {
            if (s == null || !Owns(s)) return;
            SaveStore.SetString(Key("eq." + s.Tab), s.Id);
            SaveStore.Save();
            Changed?.Invoke();
        }

        /// <summary>The pieces' palette: a worn theme (scene tab) overrides the pieces tab.</summary>
        public static Color[] Palette
        {
            get
            {
                var scene = Equipped(1);
                if (scene != null && scene.Colors != null) return scene.Colors;
                return Equipped(0)?.Colors;
            }
        }

        public static bool TryBuyWithCoins(SkinDef s)
        {
            if (s == null || s.Price != SkinPrice.Coins || Owns(s)) return false;
            if (!Wallet.TrySpend(s.Cost)) return false;
            Unlock(s);
            return true;
        }

        public static int AdsWatched(SkinDef s) => SaveStore.GetInt(Key("ads." + s.Id));

        /// <summary>One rewarded video watched for an ad skin; true when that unlocked it.</summary>
        public static bool AddAdView(SkinDef s)
        {
            if (s == null || s.Price != SkinPrice.Ads || Owns(s)) return false;
            var n = AdsWatched(s) + 1;
            SaveStore.SetInt(Key("ads." + s.Id), n);
            if (n >= s.Cost) { Unlock(s); return true; }
            SaveStore.Save();
            Changed?.Invoke();
            return false;
        }

        private static void Unlock(SkinDef s)
        {
            SaveStore.SetBool(Key("own." + s.Id), true);
            SaveStore.Save();
            Changed?.Invoke();
        }

        /// <summary>True when the wallet can pay for at least one coin skin not owned yet (the shop button's red dot, SH7).</summary>
        public static bool AnyAffordable()
        {
            var cat = Catalog;
            if (cat == null) return false;
            foreach (var s in cat.Skins)
                if (s.Price == SkinPrice.Coins && !Owns(s) && Wallet.Coins >= s.Cost) return true;
            return false;
        }

        /// <summary>Tells listeners (an open shop) that ownership changed outside these methods: a store answer, an editor cheat.</summary>
        public static void RaiseChanged() => Changed?.Invoke();
    }

    /// <summary>Coins, per game. Earned by playing (never sold), spent on coin skins.</summary>
    public static class Wallet
    {
        public static event Action Changed;

        private static string Key => $"coins.{(GameConfig.Current != null ? GameConfig.Current.gameId : "")}";

        public static int Coins => SaveStore.GetInt(Key);

#if UNITY_EDITOR
        /// <summary>Editor cheat: coins for any game.</summary>
        public static void EditorAdd(string gameId, int n)
        {
            SaveStore.SetInt("coins." + gameId, Mathf.Max(0, SaveStore.GetInt("coins." + gameId) + n));
            SaveStore.Save();
            Changed?.Invoke();
        }
#endif

        public static void Add(int n)
        {
            if (n <= 0) return;
            SaveStore.SetInt(Key, Coins + n);
            SaveStore.Save();
            Changed?.Invoke();
        }

        public static bool TrySpend(int n)
        {
            if (n < 0 || Coins < n) return false;
            SaveStore.SetInt(Key, Coins - n);
            SaveStore.Save();
            Changed?.Invoke();
            return true;
        }

        /// <summary>"1 240": thin space thousands, like the mockups.</summary>
        public static string Format(int n) => n.ToString("#,0").Replace(",", " ");
    }
}
