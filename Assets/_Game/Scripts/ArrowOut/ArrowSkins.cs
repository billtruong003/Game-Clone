using CasualGame.Core;
using UnityEngine;

namespace CasualGame.ArrowOut
{
    /// <summary>
    /// Bruh Arrows' skins (mockup "Skin VFX &amp; Pricing", Store/IAP_PRODUCTS.md): arrow palettes (4 colours, different
    /// in brightness too, A15), papers, and the premium themes, which are a paper plus their own arrow colours.
    /// Scene = background, grid dots, text ink, muted text. Face packs come with their art.
    /// </summary>
    public static class ArrowSkins
    {
        public const string GameId = "ArrowOut";

        private static Color[] P(params string[] hex) => System.Array.ConvertAll(hex, UIKit.Hex);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            var c = new SkinCatalog(GameId, ("Arrows", "Mũi tên"), ("Paper", "Giấy"));
            c.Add(new SkinDef { Id = "classic", En = "Classic", Vi = "Cổ điển", Tab = 0, Price = SkinPrice.Free, Colors = P("#2E3A59", "#35B09F", "#7B4FAE", "#EDA93C") });
            c.Add(new SkinDef { Id = "color_candy", En = "Candy", Vi = "Kẹo ngọt", Tab = 0, Price = SkinPrice.Coins, Cost = 300, Colors = P("#8A5CF0", "#E85D9E", "#3FB8E0", "#F2A53A") });
            c.Add(new SkinDef { Id = "color_ocean", En = "Ocean", Vi = "Đại dương", Tab = 0, Price = SkinPrice.Coins, Cost = 300, Colors = P("#1F5FA8", "#1FA3B8", "#5E7CE2", "#2EC4B6") });
            c.Add(new SkinDef { Id = "color_forest", En = "Forest", Vi = "Rừng xanh", Tab = 0, Price = SkinPrice.Coins, Cost = 400, Colors = P("#3E6B45", "#6A994E", "#BC4749", "#C98B2E") });
            c.Add(new SkinDef { Id = "color_mono", En = "Mono", Vi = "Đơn sắc", Tab = 0, Price = SkinPrice.Coins, Cost = 400, Colors = P("#343A40", "#6C757D", "#495057", "#868E96") });
            c.Add(new SkinDef { Id = "neon", En = "Neon", Vi = "Neon", Tab = 0, Price = SkinPrice.Ads, Cost = 3, Colors = P("#3A86FF", "#FF4D9A", "#9B5DE5", "#FB8500") });
            // papers: background, grid dots, text, muted text
            c.Add(new SkinDef { Id = "paper_cream", En = "Cream", Vi = "Kem", Tab = 1, Price = SkinPrice.Free, Scene = P("#F5F1EA", "#D9D2C5", "#1E2240", "#6B7090") });
            c.Add(new SkinDef { Id = "paper_grid", En = "Grid", Vi = "Ô li", Tab = 1, Price = SkinPrice.Coins, Cost = 300, Scene = P("#FFFFFF", "#C9D7E3", "#1E2240", "#6B7090") });
            c.Add(new SkinDef { Id = "paper_kraft", En = "Kraft", Vi = "Giấy kraft", Tab = 1, Price = SkinPrice.Coins, Cost = 400, Scene = P("#E6D2AE", "#B89A6C", "#1E2240", "#5E5040") });
            c.Add(new SkinDef { Id = "paper_night", En = "Night", Vi = "Đêm", Tab = 1, Price = SkinPrice.Coins, Cost = 500, Dark = true, Scene = P("#1D2140", "#3A4072", "#FFF8EC", "#A9AED0") });
            // premium themes: their own paper and arrow colours
            c.Add(new SkinDef { Id = "theme_blueprint", En = "Blueprint", Vi = "Bản vẽ", Tab = 1, Price = SkinPrice.Premium, Dark = true,
                Scene = P("#1F4E8C", "#4A76B5", "#FFFFFF", "#BFD7FF"), Colors = P("#FFFFFF", "#BFD7FF", "#FFE08A", "#7FD1FF") });
            c.Add(new SkinDef { Id = "theme_chalkboard", En = "Chalkboard", Vi = "Bảng phấn", Tab = 1, Price = SkinPrice.Premium, Dark = true,
                Scene = P("#2F4F3A", "#4C6E57", "#F4F4F5", "#BFD3C4"), Colors = P("#F4F4F5", "#FFE08A", "#9BE7FF", "#FFB3C7") });
            c.Add(new SkinDef { Id = "theme_terminal", En = "Terminal", Vi = "Terminal", Tab = 1, Price = SkinPrice.Premium, Dark = true,
                Scene = P("#0B0B0F", "#1F3A1F", "#39FF14", "#1F9E2C"), Colors = P("#39FF14", "#1F9E2C", "#B6FF9E", "#E8FFE0") });
            Skins.Register(c);
        }

        /// <summary>The worn paper: background, grid dots, text, muted text.</summary>
        public static Color[] Paper => (Skins.Equipped(1) ?? Skins.CatalogOf(GameId).Default(1)).Scene;

        /// <summary>The colour an arrow is drawn in on a paper: on a dark paper a dark arrow (classic navy) is lifted so it still reads.</summary>
        public static Color OnPaper(Color arrow, Color[] paper) =>
            paper[0].grayscale < 0.4f && arrow.grayscale < 0.35f ? Color.Lerp(arrow, Color.white, 0.55f) : arrow;
    }

    /// <summary>Bruh Arrows in the shop: arrows with a face on their tail, on the paper.</summary>
    public sealed class ArrowShopPainter : IShopPainter
    {
        public Color Background => ArrowSkins.Paper[0];

        public void Card(RectTransform area, SkinDef skin)
        {
            var paper = skin.Scene ?? ArrowSkins.Paper;
            var colors = skin.Colors ?? (skin.Tab == 1 ? Skins.Palette : null) ?? Skins.Equipped(0)?.Colors ?? Skins.CatalogOf(ArrowSkins.GameId).Default(0).Colors;
            UIKit.AddImage(area, "round_rect", paper[0]);
            Grid(area, paper[1], 7, 3, 44f);
            Arrow(area, new[] { new Vector2(-150, -44), new Vector2(-150, 0), new Vector2(-62, 0) }, ArrowSkins.OnPaper(colors[1], paper), 44f);
            Arrow(area, new[] { new Vector2(10, 44), new Vector2(98, 44), new Vector2(98, 0), new Vector2(150, 0) }, ArrowSkins.OnPaper(colors[3], paper), 44f);
            Arrow(area, new[] { new Vector2(-18, -44), new Vector2(98, -44) }, ArrowSkins.OnPaper(colors[0], paper), 44f);
        }

        public void Preview(RectTransform area, SkinDef pieces, SkinDef scene)
        {
            var paper = scene?.Scene ?? ArrowSkins.Paper;
            var colors = scene?.Colors ?? pieces?.Colors ?? Skins.CatalogOf(ArrowSkins.GameId).Default(0).Colors;
            UIKit.AddImage(area, "round_rect", paper[0]);
            const float cell = 96f;
            Grid(area, paper[1], 9, 5, cell);
            Vector2 C(float col, float row) => new((col - 4f) * cell, (2f - row) * cell);
            Color On(int i) => ArrowSkins.OnPaper(colors[i], paper);
            Arrow(area, new[] { C(0, 4), C(0, 1), C(2, 1) }, On(1), cell);
            Arrow(area, new[] { C(1, 3), C(4, 3), C(4, 0) }, On(0), cell);
            Arrow(area, new[] { C(6, 4), C(6, 2), C(8, 2) }, On(3), cell);
            Arrow(area, new[] { C(8, 0), C(5, 0) }, On(2), cell);
            Arrow(area, new[] { C(3, 4), C(5, 4) }, On(2), cell);
        }

        private static void Grid(RectTransform area, Color color, int cols, int rows, float cell)
        {
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    UIKit.Image(area, "grid_dot", new Vector2(0.5f, 0.5f), new Vector2((c - (cols - 1) / 2f) * cell, ((rows - 1) / 2f - r) * cell), new Vector2(cell * 0.28f, cell * 0.28f), color);
        }

        // points: tail first, head last (the head points along the last segment)
        private static void Arrow(RectTransform area, Vector2[] points, Color color, float cell)
        {
            var layer = UIKit.Stretch(UIKit.Rect("Arrow", area));
            var d = points[^1] - points[^2];
            var dir = Mathf.Abs(d.x) > Mathf.Abs(d.y) ? (d.x > 0 ? 1 : 3) : (d.y > 0 ? 0 : 2); // board dirs: up, right, down, left
            var headFirst = (Vector2[])points.Clone();
            System.Array.Reverse(headFirst);
            ArrowBoardView.CreateStroke(layer, headFirst, dir, cell, color);
            var cap = UIKit.Image(layer, "dot", new Vector2(0.5f, 0.5f), points[0], new Vector2(cell * 0.6f, cell * 0.6f), color);
            var face = Face.AddUI(cap.transform, new Vector2(cell * 0.44f, cell * 0.44f), new Vector2(0, cell * 0.02f));
            face.InkFor(color);
        }
    }
}
