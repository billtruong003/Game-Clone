using System.Collections.Generic;
using CasualGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CasualGame.EyeBlast
{
    /// <summary>
    /// Nah Blocks' skins (mockup "Skin VFX &amp; Pricing", Store/IAP_PRODUCTS.md): block colour sets (7 colours each)
    /// and boards. Each block set has its own line-clear effect, played once per cleared block. Premium sets (Retro
    /// Bricks, Pixel, Toy Studs, Gems) and face packs come with their art.
    /// </summary>
    public static class BlastSkins
    {
        public const string GameId = "EyeBlast";

        private static Color[] P(params string[] hex) => System.Array.ConvertAll(hex, UIKit.Hex);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            var c = new SkinCatalog(GameId, ("Blocks", "Khối"), ("Board", "Bàn"));
            c.Add(new SkinDef { Id = "classic", En = "Classic", Vi = "Cổ điển", Tab = 0, Price = SkinPrice.Free,
                Colors = P("#FF5A5F", "#FF9F1C", "#FFD23F", "#3DDC97", "#4EA8DE", "#9B5DE5", "#F15BB5") });
            c.Add(new SkinDef { Id = "color_candy", En = "Candy", Vi = "Kẹo ngọt", Tab = 0, Price = SkinPrice.Coins, Cost = 300, Fx = "Skin_Confetti", FxPieceColor = false,
                Colors = P("#FF8FB1", "#FFB870", "#FFE07A", "#8EF0C4", "#8CCBFF", "#C3A3FF", "#FFA3E0") });
            c.Add(new SkinDef { Id = "color_ocean", En = "Ocean", Vi = "Đại dương", Tab = 0, Price = SkinPrice.Coins, Cost = 300, Fx = "Skin_Bubbles", FxPieceColor = false,
                Colors = P("#48CAE4", "#90E0EF", "#FFD166", "#2EC4B6", "#4CC9F0", "#7B8CFF", "#CAF0F8") });
            c.Add(new SkinDef { Id = "color_retro", En = "Retro green", Vi = "Xanh cổ điển", Tab = 0, Price = SkinPrice.Coins, Cost = 400, Fx = "Skin_Pixel",
                Colors = P("#C4D86A", "#9BBC0F", "#B7CC5A", "#8BAC0F", "#D0E080", "#A9C23F", "#E0EBA0") });
            c.Add(new SkinDef { Id = "color_mono", En = "Mono", Vi = "Đơn sắc", Tab = 0, Price = SkinPrice.Coins, Cost = 400, Fx = "Merge_Splash", FxPieceColor = false, FxTint = UIKit.Ink,
                Colors = P("#FFFFFF", "#E9ECEF", "#DEE2E6", "#CED4DA", "#F1F3F5", "#C3C9CF", "#B8BEC4") });
            c.Add(new SkinDef { Id = "color_pastel", En = "Pastel", Vi = "Pastel", Tab = 0, Price = SkinPrice.Coins, Cost = 500, Fx = "Skin_Powder",
                Colors = P("#FFD6E0", "#FFD8BE", "#FFEFB5", "#D8F3DC", "#CDE7F0", "#E2D4F0", "#F8E1F4") });
            c.Add(new SkinDef { Id = "color_jelly", En = "Jelly", Vi = "Thạch", Tab = 0, Price = SkinPrice.Coins, Cost = 500, Fx = "Merge_Splash",
                Colors = P("#FF5A5F", "#FF9F1C", "#FFD23F", "#3DDC97", "#4EA8DE", "#9B5DE5", "#F15BB5") });
            c.Add(new SkinDef { Id = "neon", En = "Neon", Vi = "Neon", Tab = 0, Price = SkinPrice.Ads, Cost = 3, Fx = "Skin_Zap",
                Colors = P("#FF4D6D", "#FF9E00", "#FFFF3F", "#39FF14", "#00BBF9", "#B388FF", "#FF5CCB") });
            // premium sets: their own block shader (BlockSkin) and colours
            c.Add(new SkinDef { Id = "skin_retro_bricks", En = "Retro Bricks", Vi = "Gạch cổ điển", Tab = 0, Price = SkinPrice.Premium, Fx = "Skin_Pixel",
                Colors = P("#E04040", "#3070E0", "#F0C020", "#30B050", "#A040C0", "#F07020", "#20B0C0") });
            c.Add(new SkinDef { Id = "skin_gems", En = "Gems", Vi = "Đá quý", Tab = 0, Price = SkinPrice.Premium, Fx = "Skin_Zap",
                Colors = P("#E0115F", "#0F52BA", "#50C878", "#FFC87C", "#9966CC", "#7FFFD4", "#E4D00A") });
            // boards: background, frame, empty slot
            c.Add(new SkinDef { Id = "board_classic", En = "Navy", Vi = "Xanh đêm", Tab = 1, Price = SkinPrice.Free, Scene = P("#2B2F55", "#1A1D3A", "#252A4E") });
            c.Add(new SkinDef { Id = "board_paper", En = "Paper", Vi = "Giấy", Tab = 1, Price = SkinPrice.Coins, Cost = 300, Scene = P("#F4EFE8", "#E8E1D5", "#DCD3C4") });
            c.Add(new SkinDef { Id = "board_midnight", En = "Midnight", Vi = "Nửa đêm", Tab = 1, Price = SkinPrice.Coins, Cost = 400, Scene = P("#0F1026", "#07081A", "#161838") });
            Skins.Register(c);
        }

        /// <summary>The worn board colours: background, frame, empty slot.</summary>
        public static Color[] Board => (Skins.Equipped(1) ?? Skins.CatalogOf(GameId).Default(1)).Scene;

        // premium block sets drawn by the BlockSkin shader (Docs/SHADER_LAB.md): skin id → shader set
        private static readonly Dictionary<string, int> Looks = new() { ["skin_retro_bricks"] = 0, ["skin_gems"] = 3 };
        private static readonly Dictionary<int, Material> mats = new();

        /// <summary>
        /// Draws a block body (a block_fill Image) in a premium set's shader; true when it did, so the caller leaves out
        /// the separate block_line (the shader draws its own edge).
        /// </summary>
        public static bool Dress(Image body, SkinDef skin)
        {
            if (skin == null || !Looks.TryGetValue(skin.Id, out var set)) return false;
            if (!mats.TryGetValue(set, out var m) || m == null)
            {
                m = new Material(Shader.Find("CasualGame/Lab/BlockSkin")) { name = "BlockSkin." + set };
                m.SetFloat("_Set", set);
                mats[set] = m;
            }
            body.sprite = null;
            body.material = m;
            return true;
        }

        /// <summary>A light board needs dark text on it.</summary>
        public static bool LightBoard(Color[] board) => board[0].grayscale > 0.6f;
    }

    /// <summary>Nah Blocks in the shop: blocks with faces on a little board.</summary>
    public sealed class BlastShopPainter : IShopPainter
    {
        private static readonly FaceId[] Moods = { FaceId.Smug, FaceId.Grin, FaceId.Meh, FaceId.Shock, FaceId.Stare, FaceId.Panic, FaceId.Dizzy };

        public Color Background => BlastSkins.Board[0];

        public void Card(RectTransform area, SkinDef skin)
        {
            var board = skin.Scene ?? BlastSkins.Board;
            var colors = skin.Colors ?? Skins.Equipped(0)?.Colors ?? Skins.CatalogOf(BlastSkins.GameId).Default(0).Colors;
            UIKit.AddImage(area, "round_rect", board[0]);
            if (skin.Tab == 1)
            {
                var frame = UIKit.Image(area, "frame", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260, 130), board[1]);
                for (int i = 0; i < 4; i++)
                    Block(frame.rectTransform, new Vector2(-87 + i * 58, 0), 52, i == 1 ? colors[4] : i == 2 ? colors[2] : board[2], i == 1 || i == 2 ? Moods[i] : (FaceId?)null);
                return;
            }
            Block(area, new Vector2(-104, 0), 92, colors[0], FaceId.Smug, skin);
            Block(area, new Vector2(0, 0), 92, colors[4], FaceId.Grin, skin);
            Block(area, new Vector2(104, 0), 92, colors[2], FaceId.Meh, skin);
        }

        public void Preview(RectTransform area, SkinDef pieces, SkinDef scene)
        {
            var board = scene?.Scene ?? BlastSkins.Board;
            var colors = pieces?.Colors ?? Skins.CatalogOf(BlastSkins.GameId).Default(0).Colors;
            UIKit.AddImage(area, "round_rect", board[0]);
            const int rows = 4, cols = 8;
            const float cell = 104f;
            var frame = UIKit.Image(area, "frame", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(cols * cell + 36, rows * cell + 36), board[1]);
            // a few pieces resting on the board; row 2 is one block short of a clear
            int[,] grid =
            {
                { 0, 0, 6, 0, 0, 0, 2, 0 },
                { 0, 1, 6, 6, 0, 3, 2, 0 },
                { 4, 4, 4, 5, 5, 0, 7, 7 },
                { 1, 1, 3, 3, 5, 2, 2, 7 },
            };
            var mood = 0;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    var v = grid[r, c];
                    var pos = new Vector2((c - (cols - 1) / 2f) * cell, ((rows - 1) / 2f - r) * cell);
                    Block(frame.rectTransform, pos, cell - 6, v == 0 ? board[2] : colors[v - 1], v == 0 ? null : Moods[mood++ % Moods.Length], pieces);
                }
        }

        private static void Block(RectTransform parent, Vector2 pos, float size, Color color, FaceId? mood, SkinDef skin = null)
        {
            var fill = UIKit.Image(parent, "block_fill", new Vector2(0.5f, 0.5f), pos, new Vector2(size, size), color);
            if (mood == null) return;
            if (!BlastSkins.Dress(fill, skin)) UIKit.Image(fill.transform, "block_line", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            Face.AddUI(fill.transform, new Vector2(size * 0.66f, size * 0.66f), new Vector2(0, size * 0.02f)).SetIdle(mood.Value);
        }
    }
}
