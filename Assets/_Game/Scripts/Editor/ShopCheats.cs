using CasualGame.Core;
using UnityEditor;

namespace CasualGame.EditorTools
{
    /// <summary>
    /// Editor-only shop cheats for testing every skin quickly (never in a build: this is the Editor assembly).
    /// Work in edit mode and in play mode (an open shop refreshes at once). They act on all three games.
    /// </summary>
    public static class ShopCheats
    {
        private static readonly string[] Games = { "EyeMerge", "EyeBlast", "ArrowOut" };

        [MenuItem("Tools/Casual Game/Shop Cheats/Unlock All Skins (Full Game)")]
        private static void UnlockAll()
        {
            foreach (var g in Games) Store.EditorSetOwned(g, Store.FullGame, true);
        }

        [MenuItem("Tools/Casual Game/Shop Cheats/Lock All Skins Again")]
        private static void LockAll()
        {
            foreach (var g in Games)
            {
                Store.EditorSetOwned(g, Store.FullGame, false);
                Store.EditorSetOwned(g, Store.AllSkins, false);
                var catalog = Skins.CatalogOf(g);
                if (catalog == null) continue;
                foreach (var s in catalog.Skins)
                {
                    if (s.HasProduct) Store.EditorSetOwned(g, s.Id, false);
                    SaveStore.Delete($"skin.{g}.own.{s.Id}");
                    SaveStore.Delete($"skin.{g}.ads.{s.Id}");
                }
                for (int t = 0; t < catalog.Tabs.Length; t++) SaveStore.Delete($"skin.{g}.eq.{t}");
            }
            SaveStore.Save();
            Skins.RaiseChanged();
        }

        [MenuItem("Tools/Casual Game/Shop Cheats/+1000 Coins")]
        private static void AddCoins()
        {
            foreach (var g in Games) Wallet.EditorAdd(g, 1000);
        }

        [MenuItem("Tools/Casual Game/Shop Cheats/Reset Coins")]
        private static void ResetCoins()
        {
            foreach (var g in Games) Wallet.EditorAdd(g, -999999);
        }
    }
}
