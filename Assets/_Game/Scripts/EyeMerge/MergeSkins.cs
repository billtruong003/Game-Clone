using System.Collections.Generic;
using CasualGame.Core;
using UnityEngine;

namespace CasualGame.EyeMerge
{
    /// <summary>
    /// Meh Merge's skins (mockup "Skin VFX &amp; Pricing", Store/IAP_PRODUCTS.md): ball colour sets (11 tiers each) and
    /// stage colours. Each ball set has its own merge effect. Premium sets (Sports, Eyeballs) and face
    /// packs come with their art.
    /// </summary>
    public static class MergeSkins
    {
        public const string GameId = "EyeMerge";

        private static Color[] P(params string[] hex) => System.Array.ConvertAll(hex, UIKit.Hex);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            var c = new SkinCatalog(GameId, ("Balls", "Bóng"), ("Stage", "Sân khấu"), ("Faces", "Mặt"));
            FacePacks.AddTo(c);
            // balls: tier 1 → 11
            c.Add(new SkinDef { Id = "classic", En = "Classic", Vi = "Cổ điển", Tab = 0, Price = SkinPrice.Free,
                Colors = P("#FFD23F", "#FF9F1C", "#3DDC97", "#4EA8DE", "#F15BB5", "#FF5A5F", "#9B5DE5", "#2A9D8F", "#E76F51", "#3A5BD9", "#FFC83D") });
            c.Add(new SkinDef { Id = "color_candy", En = "Candy", Vi = "Kẹo ngọt", Tab = 0, Price = SkinPrice.Coins, Cost = 300, Fx = "Skin_Confetti", FxPieceColor = false,
                Colors = P("#FFB3C7", "#FFD166", "#9BF6FF", "#A0C4FF", "#FF8FB1", "#BDB2FF", "#CAFFBF", "#FFC6FF", "#FF9AA2", "#7FD8BE", "#FFC83D") });
            c.Add(new SkinDef { Id = "color_ocean", En = "Ocean", Vi = "Đại dương", Tab = 0, Price = SkinPrice.Coins, Cost = 300, Fx = "Skin_Bubbles", FxPieceColor = false,
                Colors = P("#CAF0F8", "#90E0EF", "#48CAE4", "#2EC4B6", "#80ED99", "#00B4D8", "#FFD166", "#0096C7", "#FF9F1C", "#4361EE", "#FFC83D") });
            c.Add(new SkinDef { Id = "color_forest", En = "Forest", Vi = "Rừng xanh", Tab = 0, Price = SkinPrice.Coins, Cost = 400, Fx = "Skin_Leaves", FxPieceColor = false,
                Colors = P("#E9F5DB", "#CFE1B9", "#A7C957", "#FFD166", "#B5C99A", "#F4A259", "#6A994E", "#BC4749", "#8CB369", "#D4A373", "#FFC83D") });
            c.Add(new SkinDef { Id = "color_sunset", En = "Sunset", Vi = "Hoàng hôn", Tab = 0, Price = SkinPrice.Coins, Cost = 400, Fx = "Skin_Ring",
                Colors = P("#FFE5B4", "#FFCB77", "#FEB95F", "#FF9F1C", "#F77F00", "#F25C54", "#E63946", "#F08CAE", "#C77DFF", "#9D4EDD", "#FFD23F") });
            c.Add(new SkinDef { Id = "color_mono", En = "Mono", Vi = "Đơn sắc", Tab = 0, Price = SkinPrice.Coins, Cost = 500, Fx = "Merge_Splash", FxPieceColor = false, FxTint = UIKit.Ink,
                Colors = P("#FFFFFF", "#C9CED4", "#EEF0F2", "#AEB5BC", "#E0E3E7", "#9AA2AA", "#D2D6DB", "#8E969E", "#F7F7F7", "#A4ABB2", "#FFD23F") });
            c.Add(new SkinDef { Id = "color_pastel", En = "Pastel", Vi = "Pastel", Tab = 0, Price = SkinPrice.Coins, Cost = 500, Fx = "Skin_Powder",
                Colors = P("#FFD6E0", "#FFEFB5", "#D8F3DC", "#CDE7F0", "#E2D4F0", "#FFD8BE", "#F8E1F4", "#C7F9CC", "#FDE2E4", "#E3F2FD", "#FFD98A") });
            c.Add(new SkinDef { Id = "neon", En = "Neon", Vi = "Neon", Tab = 0, Price = SkinPrice.Ads, Cost = 3, Fx = "Skin_Zap",
                Colors = P("#FFFF3F", "#39FF14", "#00F5D4", "#00BBF9", "#FF5CCB", "#FF4D6D", "#FEE440", "#B388FF", "#FF9E00", "#72EFDD", "#FFE14D") });
            // premium sets: drawn by shaders (MergeLooks); Colors feed the HUD strip, the next bubble and the splashes
            c.Add(new SkinDef { Id = "skin_sports", En = "Sports", Vi = "Thể thao", Tab = 0, Price = SkinPrice.Premium, Fx = "Skin_Confetti", FxPieceColor = false, Colors = MergeLooks.TierColors("skin_sports") });
            c.Add(new SkinDef { Id = "skin_eyeballs", En = "Eyeballs", Vi = "Nhãn cầu", Tab = 0, Price = SkinPrice.Premium, Colors = MergeLooks.TierColors("skin_eyeballs") });
            // stage: background, floor band, shelf, pillars
            c.Add(new SkinDef { Id = "stage_classic", En = "Classic", Vi = "Cổ điển", Tab = 1, Price = SkinPrice.Free, Scene = P("#2F2552", "#271E47", "#5B4D96", "#4A3D80") });
            c.Add(new SkinDef { Id = "stage_frosted", En = "Frosted", Vi = "Sương giá", Tab = 1, Price = SkinPrice.Coins, Cost = 300, Scene = P("#3A3470", "#2C275C", "#C9D6F2", "#9FB0DA") });
            c.Add(new SkinDef { Id = "stage_amber", En = "Amber", Vi = "Hổ phách", Tab = 1, Price = SkinPrice.Coins, Cost = 300, Scene = P("#3A2440", "#2B1A30", "#FFB84D", "#D98E2E") });
            c.Add(new SkinDef { Id = "stage_mint", En = "Mint", Vi = "Bạc hà", Tab = 1, Price = SkinPrice.Coins, Cost = 400, Scene = P("#1F3A40", "#162C31", "#7FFFD4", "#4FCFAE") });
            c.Add(new SkinDef { Id = "stage_night", En = "Night", Vi = "Đêm", Tab = 1, Price = SkinPrice.Coins, Cost = 500, Scene = P("#12122C", "#0B0B1F", "#6FA8FF", "#3F6FCC") });
            Skins.Register(c);
        }

        /// <summary>The worn stage colours: background, floor, shelf, pillars.</summary>
        public static Color[] Stage => (Skins.Equipped(1) ?? Skins.CatalogOf(GameId).Default(1)).Scene;
    }

    /// <summary>
    /// Meh Merge in the shop (mockup SHOP_EM_Balls). Card: a row of balls growing left to right on the paper, all on one
    /// baseline. Preview: the open stage with a real-looking pile: every ball is dropped straight down until it rests on
    /// the shelf or on balls already there, so nothing floats or overlaps.
    /// </summary>
    public sealed class MergeShopPainter : IShopPainter
    {
        private const float Unit = 56f; // preview px per world unit
        private static readonly FaceId[] Moods = { FaceId.Smug, FaceId.Grin, FaceId.Meh, FaceId.Stare, FaceId.Shock, FaceId.Smug, FaceId.Dizzy, FaceId.Grin, FaceId.Meh, FaceId.Cry };

        public Color Background => MergeSkins.Stage[0];

        private static Color[] Default => Skins.CatalogOf(MergeSkins.GameId).Default(0).Colors;

        public void Card(RectTransform area, SkinDef skin)
        {
            if (skin.Tab == 1)
            {
                // a little stage: its ground in a framed box, the shelf, three balls resting on it
                var stage = skin.Scene;
                var colors = Skins.Equipped(0)?.Colors ?? Default;
                var edge = UIKit.Image(area, "round_rect", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 150), UIKit.Ink);
                var box = UIKit.Image(edge.transform, "round_rect", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(388, 138), stage[0]);
                var b = box.rectTransform;
                UIKit.Image(b, "round_rect", new Vector2(0.5f, 0f), new Vector2(0, 14), new Vector2(388, 28), stage[1]);
                Pill(b, new Vector2(0, -38), new Vector2(300, 14), stage[2]);
                foreach (var px in new[] { -157f, 157f })
                    Pill(b, new Vector2(px, 2), new Vector2(84, 14), stage[3]).localEulerAngles = new Vector3(0, 0, 90);
                var worn = MergeLooks.Current;
                Ball(b, new Vector2(-60, -31 + 38), 76, colors[5], FaceId.Smug, worn, 6);
                Ball(b, new Vector2(10, -31 + 30), 60, colors[3], FaceId.Meh, worn, 4);
                Ball(b, new Vector2(66, -31 + 23), 46, colors[1], FaceId.Grin, worn, 2);
                return;
            }
            // mockup card: tiers 1..5 left to right, radius 24..46, one baseline
            var c = skin.Colors ?? Default;
            float[] x = { -178, -114, -40, 46, 144 };
            float[] r = { 24, 28, 34, 40, 46 };
            const float baseline = -50f;
            // premium cards show the middle of the set (tiers 5..9), colour sets its first five
            var look = MergeLooks.Of(skin);
            var first = look != null ? 5 : 1;
            for (int i = 0; i < 5; i++) Ball(area, new Vector2(x[i], baseline + r[i]), r[i] * 2f, c[first - 1 + i], Moods[i], look, first + i);
        }

        public void Preview(RectTransform area, SkinDef pieces, SkinDef scene)
        {
            var stage = scene?.Scene ?? MergeSkins.Stage;
            var colors = pieces?.Colors ?? Default;
            var look = MergeLooks.Of(pieces);
            var h = area.rect.height > 0 ? area.rect.height : 556f;
            var bottom = -h / 2f;
            UIKit.Image(area, "round_rect", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(964, h), stage[0]).pixelsPerUnitMultiplier = 0.5f;
            UIKit.Image(area, "round_rect", new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(964, 60), stage[1]);

            // the game's stage, scaled: shelf on the floor band, pillars at the walls
            var half = (EyeMergeGame.JarRight - EyeMergeGame.JarLeft) / 2f * Unit;
            var thick = 0.36f * Unit;
            var shelfTop = bottom + 60f + thick;
            Pill(area, new Vector2(0, shelfTop - thick / 2f), new Vector2(2f * half + 0.72f * Unit, thick), stage[2]);
            var pillarTop = bottom + h * 0.8f;
            foreach (var x in new[] { -half - thick / 2f, half + thick / 2f })
                Pill(area, new Vector2(x, (pillarTop + shelfTop) / 2f), new Vector2(pillarTop - shelfTop, thick), stage[3]).localEulerAngles = new Vector3(0, 0, 90);

            // a settled pile: each ball goes to the lowest spot it can rest in (on the shelf, or in the gap between two
            // balls), big ones first, so it reads like the jar after a few drops, never balls hanging off each other
            (int tier, float hint)[] drops =
            {
                (8, -1f), (7, 1f), (6, 0f), (5, 1f), (5, -1f), (4, 0f), (4, 1f), (3, -1f), (3, 0.3f), (2, 1f), (2, -0.5f), (1, 0f),
            };
            var placed = new List<(Vector2 c, float r)>();
            for (int i = 0; i < drops.Length; i++)
            {
                var (tier, hint) = drops[i];
                var r = EyeMergeGame.Radius(tier) * Unit;
                var best = new Vector2(0f, float.MaxValue);
                var bestCost = float.MaxValue;
                for (var x = -half + r; x <= half - r + 0.01f; x += 4f)
                {
                    var y = RestY(x, r, shelfTop, placed);
                    var cost = y + 0.02f * Mathf.Abs(x - hint * (half - r));
                    if (cost < bestCost) { bestCost = cost; best = new Vector2(x, y); }
                }
                placed.Add((best, r));
                Ball(area, best, 2f * r, colors[tier - 1], Moods[i % Moods.Length], look, tier);
            }
        }

        // where a ball of radius r dropped straight down at x comes to rest
        private static float RestY(float x, float r, float floor, List<(Vector2 c, float r)> placed)
        {
            var y = floor + r;
            foreach (var (c, pr) in placed)
            {
                var dx = x - c.x;
                var reach = r + pr;
                if (Mathf.Abs(dx) < reach) y = Mathf.Max(y, c.y + Mathf.Sqrt(reach * reach - dx * dx));
            }
            return y;
        }

        // circle_fill / circle_line draw their outline at radius 116 + half the 12 px stroke (122 of 128), not at its edge:
        // the rect is grown so the drawn ball is exactly `size` across and rests on what it touches (no gap)
        private const float DrawnToRect = 128f / 122f;

        // look: a premium set draws the ball with its shader (tier picks the ball of the set)
        private static void Ball(RectTransform parent, Vector2 pos, float size, Color color, FaceId mood, MergeLooks.Look look = null, int tier = 1)
        {
            if (look != null)
            {
                var img = MergeLooks.UIBall(parent, look, tier, pos, size);
                if (!look.Face) return;
                var f = Face.AddUI(img.transform, new Vector2(size * 0.66f, size * 0.66f), new Vector2(0, look.FacePlate ? -size * 0.08f : size * 0.04f));
                f.SetIdle(mood);
                if (look.FacePlate) f.transform.localScale *= 0.8f;
                f.InkFor(MergeLooks.Dark(look, tier) ? Color.black : Color.white);
                return;
            }
            size *= DrawnToRect;
            var fill = UIKit.Image(parent, "circle_fill", new Vector2(0.5f, 0.5f), pos, new Vector2(size, size), color);
            UIKit.Image(fill.transform, "circle_line", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            Face.AddUI(fill.transform, new Vector2(size * 0.66f, size * 0.66f), new Vector2(0, size * 0.04f)).SetIdle(mood);
        }

        // an ink-edged pill (the stage's shelf and pillars), like StagePill in the game
        private static RectTransform Pill(RectTransform parent, Vector2 pos, Vector2 size, Color color)
        {
            var edge = UIKit.Pill(parent, new Vector2(0.5f, 0.5f), pos, size + new Vector2(10, 10), UIKit.Ink, out _);
            UIKit.Pill(edge, new Vector2(0.5f, 0.5f), Vector2.zero, size, color, out _);
            return edge;
        }
    }
}
