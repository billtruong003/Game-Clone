using System.Collections.Generic;
using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>How a skin is unlocked (FEATURE_SPEC SH5/SH6, Store/IAP_PRODUCTS.md).</summary>
    public enum SkinPrice
    {
        Free,
        /// <summary>Earned with coins, or bought at once for $0.99 (product id = skin id).</summary>
        Coins,
        /// <summary>Unlocked by watching <see cref="SkinDef.Cost"/> rewarded videos; no product.</summary>
        Ads,
        /// <summary>Real money only: $1.99 (product id = skin id), or any skins bundle.</summary>
        Premium,
    }

    /// <summary>
    /// One skin of one game. What it looks like is game data: <see cref="Colors"/> is the game's palette
    /// (ball tiers, block colours, arrow colours) and <see cref="Scene"/> its background colours (stage, board, paper).
    /// A skin on the scene tab may also carry <see cref="Colors"/> (a theme recolours the pieces while it is worn).
    /// </summary>
    public sealed class SkinDef
    {
        public string Id;
        public string En, Vi;
        public int Tab;
        public SkinPrice Price;
        /// <summary>Coins, or rewarded videos.</summary>
        public int Cost;
        public Color[] Colors;
        public Color[] Scene;
        /// <summary>Effect played instead of the game's default (merge splash, block pop); null = the default.</summary>
        public string Fx;
        /// <summary>Effect tint: true = the piece's own colour, false = <see cref="FxTint"/>.</summary>
        public bool FxPieceColor = true;
        public Color FxTint = Color.white;
        /// <summary>Size of <see cref="Fx"/> relative to the default effect it replaces.</summary>
        public float FxScale = 1f;
        /// <summary>Faces tab: the face pack this skin wears ("sleepy"), null = the default faces.</summary>
        public string FacePack;
        /// <summary>Cream face ink on dark bodies / a dark scene.</summary>
        public bool Dark;

        public string Name => Loc.T(En, Vi);
        public bool HasProduct => Price is SkinPrice.Coins or SkinPrice.Premium;
        /// <summary>The USD price written in Store/IAP_PRODUCTS.md, shown when Google Play's localized price is not known (editor, offline).</summary>
        public string UsdPrice => Price == SkinPrice.Premium ? "$1.99" : "$0.99";
    }

    /// <summary>
    /// A game's skins, registered from its own assembly before the first scene loads (Core never references the games).
    /// Tab 0 = the pieces (Balls / Blocks / Arrows), tab 1 = the scene (Stage / Board / Paper), tab 2 = faces.
    /// The first skin of each tab is the free default.
    /// </summary>
    public sealed class SkinCatalog
    {
        public readonly string GameId;
        public readonly (string en, string vi)[] Tabs;
        public readonly List<SkinDef> Skins = new();

        public SkinCatalog(string gameId, params (string en, string vi)[] tabs)
        {
            GameId = gameId;
            Tabs = tabs;
        }

        public SkinCatalog Add(SkinDef skin)
        {
            Skins.Add(skin);
            return this;
        }

        public SkinDef Find(string id) => Skins.Find(s => s.Id == id);

        public SkinDef Default(int tab) => Skins.Find(s => s.Tab == tab);

        public IEnumerable<SkinDef> InTab(int tab)
        {
            foreach (var s in Skins) if (s.Tab == tab) yield return s;
        }

        /// <summary>Every Play product of this game: one per coin / premium skin, the two bundles and Remove ads.</summary>
        public IEnumerable<string> ProductIds()
        {
            foreach (var s in Skins) if (s.HasProduct) yield return s.Id;
            yield return Store.AllSkins;
            yield return Store.FullGame;
        }
    }
}
