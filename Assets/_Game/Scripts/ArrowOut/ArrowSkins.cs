using System.Collections.Generic;
using CasualGame.Core;
using UnityEngine;
using UnityEngine.UI;

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
            var c = new SkinCatalog(GameId, ("Arrows", "Mũi tên"), ("Paper", "Giấy"), ("Faces", "Mặt"));
            FacePacks.AddTo(c);
            c.Add(new SkinDef { Id = "classic", En = "Classic", Vi = "Cổ điển", Tab = 0, Price = SkinPrice.Free, Colors = P("#2E3A59", "#35B09F", "#7B4FAE", "#EDA93C") });
            c.Add(new SkinDef { Id = "color_candy", En = "Candy", Vi = "Kẹo ngọt", Tab = 0, Price = SkinPrice.Coins, Cost = 300, Colors = P("#8A5CF0", "#E85D9E", "#3FB8E0", "#F2A53A") });
            c.Add(new SkinDef { Id = "color_ocean", En = "Ocean", Vi = "Đại dương", Tab = 0, Price = SkinPrice.Coins, Cost = 300, Colors = P("#1F5FA8", "#1FA3B8", "#5E7CE2", "#2EC4B6") });
            c.Add(new SkinDef { Id = "color_forest", En = "Forest", Vi = "Rừng xanh", Tab = 0, Price = SkinPrice.Coins, Cost = 400, Colors = P("#3E6B45", "#6A994E", "#BC4749", "#C98B2E") });
            c.Add(new SkinDef { Id = "color_mono", En = "Mono", Vi = "Đơn sắc", Tab = 0, Price = SkinPrice.Coins, Cost = 400, Colors = P("#343A40", "#6C757D", "#495057", "#868E96") });
            c.Add(new SkinDef { Id = "color_sunset", En = "Sunset", Vi = "Hoàng hôn", Tab = 0, Price = SkinPrice.Coins, Cost = 500, Colors = P("#E4572E", "#F28C28", "#C2185B", "#7B4FAE") });
            c.Add(new SkinDef { Id = "color_pastel", En = "Pastel", Vi = "Pastel", Tab = 0, Price = SkinPrice.Coins, Cost = 500, Colors = P("#6F9BDB", "#E07FAA", "#5DB894", "#E6A24A") });
            c.Add(new SkinDef { Id = "neon", En = "Neon", Vi = "Neon", Tab = 0, Price = SkinPrice.Ads, Cost = 3, Colors = P("#3A86FF", "#FF4D9A", "#9B5DE5", "#FB8500") });
            // papers: background, grid dots, text, muted text
            c.Add(new SkinDef { Id = "paper_cream", En = "Cream", Vi = "Kem", Tab = 1, Price = SkinPrice.Free, Scene = P("#F5F1EA", "#D9D2C5", "#1E2240", "#6B7090") });
            c.Add(new SkinDef { Id = "paper_grid", En = "Grid", Vi = "Ô li", Tab = 1, Price = SkinPrice.Coins, Cost = 300, Scene = P("#FFFFFF", "#C9D7E3", "#1E2240", "#6B7090") });
            c.Add(new SkinDef { Id = "paper_kraft", En = "Kraft", Vi = "Giấy kraft", Tab = 1, Price = SkinPrice.Coins, Cost = 400, Scene = P("#E6D2AE", "#B89A6C", "#1E2240", "#5E5040") });
            c.Add(new SkinDef { Id = "paper_night", En = "Night", Vi = "Đêm", Tab = 1, Price = SkinPrice.Coins, Cost = 500, Dark = true, Scene = P("#1D2140", "#3A4072", "#FFF8EC", "#A9AED0") });
            c.Add(new SkinDef { Id = "paper_linen", En = "Linen", Vi = "Vải lanh", Tab = 1, Price = SkinPrice.Coins, Cost = 500, Scene = P("#EEE8DF", "#CBBFAE", "#1E2240", "#6B7090") });
            // premium themes: their own paper and arrow colours
            c.Add(new SkinDef { Id = "theme_blueprint", En = "Blueprint", Vi = "Bản vẽ", Tab = 1, Price = SkinPrice.Premium, Dark = true,
                Scene = P("#1F4E8C", "#4A76B5", "#FFFFFF", "#BFD7FF"), Colors = P("#FFFFFF", "#BFD7FF", "#FFE08A", "#7FD1FF") });
            c.Add(new SkinDef { Id = "theme_chalkboard", En = "Chalkboard", Vi = "Bảng phấn", Tab = 1, Price = SkinPrice.Premium, Dark = true,
                Scene = P("#2F4F3A", "#4C6E57", "#F4F4F5", "#BFD3C4"), Colors = P("#F4F4F5", "#FFE08A", "#9BE7FF", "#FFB3C7") });
            c.Add(new SkinDef { Id = "theme_vector", En = "Vector CRT", Vi = "Màn vector", Tab = 1, Price = SkinPrice.Premium, Dark = true,
                Scene = P("#04080A", "#1F5C2A", "#E8FFE0", "#7FBF8A"), Colors = P("#5CFF7A", "#3FD8FF", "#FFD166", "#FF6B9A") });
            c.Add(new SkinDef { Id = "theme_hologram", En = "Hologram", Vi = "Hologram", Tab = 1, Price = SkinPrice.Premium, Dark = true,
                Scene = P("#0B1220", "#1A6B8A", "#E6FBFF", "#8FC7D8"), Colors = P("#3DFFB0", "#5CE1FF", "#FF9D3C", "#C38BFF") });
            c.Add(new SkinDef { Id = "theme_neon", En = "Neon", Vi = "Neon", Tab = 1, Price = SkinPrice.Premium, Dark = true,
                Scene = P("#0B0B12", "#2A1E4A", "#FFF8EC", "#A9AED0"), Colors = P("#00E5FF", "#FF2BD6", "#39FF14", "#FFB300") });
            Skins.Register(c);
        }

        /// <summary>The worn paper: background, grid dots, text, muted text.</summary>
        public static Color[] Paper => (Skins.Equipped(1) ?? Skins.CatalogOf(GameId).Default(1)).Scene;

        /// <summary>The colour an arrow is drawn in on a paper: on a dark paper a dark arrow (classic navy) is lifted so it still reads.</summary>
        public static Color OnPaper(Color arrow, Color[] paper) =>
            paper[0].grayscale < 0.4f && arrow.grayscale < 0.35f ? Color.Lerp(arrow, Color.white, 0.55f) : arrow;

        // ---------------- shader themes (Docs/SHADER_LAB.md) ----------------

        /// <summary>How a premium theme draws: its line shader, ground, glow room, and the style of tail caps and faces.</summary>
        public sealed class Look
        {
            public string Line;      // CasualGame/Lab/<Line>
            public int Ground;       // ThemeGround mode
            public float GlowPad;    // cells of halo around the line
            public int Style = -1;   // ThemeSprite / ThemeFace style, -1 = the default cap and face
            public bool Ring;        // neon / vector: the cap is a glowing ring with the face inside
            public int Trail;        // ThemeTrail mode left behind a flying arrow: 0 glow, 1 chalk, 2 blueprint dashes
        }

        private static readonly Dictionary<string, Look> Looks = new()
        {
            ["theme_blueprint"] = new Look { Line = "ArrowBlueprint", Ground = 2, Trail = 2 },
            ["theme_chalkboard"] = new Look { Line = "ArrowChalk", Ground = 1, Style = 1, Trail = 1 },
            ["theme_vector"] = new Look { Line = "ArrowVector", Ground = 3, GlowPad = 0.45f, Style = 2, Ring = true },
            ["theme_hologram"] = new Look { Line = "ArrowHolo", Ground = 4, GlowPad = 0.45f, Style = 3, Ring = true },
            ["theme_neon"] = new Look { Line = "ArrowNeon", Ground = 4, GlowPad = 0.55f, Style = 2, Ring = true },
        };

        public static Look LookOf(SkinDef paper) => paper != null && Looks.TryGetValue(paper.Id, out var l) ? l : null;

        /// <summary>The worn theme's look, or null for a plain paper.</summary>
        public static Look Current => LookOf(Skins.Equipped(1));

        // one material per look and use (they batch); faces get one per ink colour
        private static readonly Dictionary<string, Material> mats = new();

        private static Material Mat(string key, string shader)
        {
            if (mats.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Shader.Find("CasualGame/Lab/" + shader)) { name = key };
            mats[key] = m;
            return m;
        }

        /// <summary>The line material of a look (shared: _Reveal on it draws every arrow on at once).</summary>
        public static Material LineMaterial(Look look)
        {
            var m = Mat("line." + look.Line, look.Line);
            if (m.HasProperty("_Reveal") && !m.name.EndsWith("!")) { m.SetFloat("_Reveal", 1f); m.name += "!"; } // fully drawn unless a board animates it
            // glowing lines start at the tail cap's edge, so the line never runs through the face
            if (m.HasProperty("_TailClip")) m.SetFloat("_TailClip", look.Style is 2 or 3 ? 0.3f : 0f);
            return m;
        }

        /// <summary>The material of the trail a flying arrow leaves in a look (glow streak, chalk dust, dashed line).</summary>
        public static Material TrailMaterial(Look look, float worldLength)
        {
            var m = Mat("trail." + look.Line, "ThemeTrail");
            m.SetFloat("_Mode", look.Trail);
            m.SetFloat("_Length", worldLength);
            var additive = look.Trail == 0;
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            return m;
        }

        /// <summary>The ground behind the board: a ThemeGround material for the look, null for a flat paper.</summary>
        public static Material GroundMaterial(Look look, Color[] scene, float cell)
        {
            if (look == null) return null;
            var m = Mat("ground." + look.Line, "ThemeGround");
            m.SetFloat("_Mode", look.Ground);
            m.SetColor("_Base", scene[0]);
            m.SetColor("_Ink", scene[1]);
            m.SetFloat("_Cell", cell);
            return m;
        }

        /// <summary>
        /// Dresses an arrow (line, tail cap, face) in a look. The cap becomes a procedural circle (glowing styles get a
        /// bigger quad for the halo; ring styles a hollow ring with a solid inside, so nothing shows through the face).
        /// </summary>
        public static void Dress(Look look, ArrowStroke stroke, Image cap, Face face, Color color, Color ground)
        {
            if (look == null) return;
            stroke.material = LineMaterial(look);
            stroke.GlowPad = look.GlowPad;
            if (look.Style < 0) return;
            var glow = look.Style is 2 or 3;
            var capMat = Mat("cap." + look.Line, "ThemeSprite");
            capMat.SetFloat("_Mode", look.Style);
            capMat.SetFloat("_Circle", 1f);
            capMat.SetFloat("_CircleR", glow ? 0.55f : 0.95f);
            capMat.SetFloat("_Ring", look.Ring ? 0.28f : 0f);
            capMat.SetColor("_Inner", Color.Lerp(ground, Color.black, 0.2f));
            cap.sprite = null;
            cap.material = capMat;
            if (glow) cap.rectTransform.sizeDelta *= 1.8f;
            // glowing styles draw the face in the arrow's own light; chalk keeps a dark-on-light ink
            var ink = glow ? Color.Lerp(color, Color.white, 0.5f) : color.grayscale < 0.36f ? UIKit.Paper : UIKit.Ink;
            // the worn face pack (shop faces tab) instead of the default faces
            var packName = Skins.Equipped(FacePacks.Tab)?.FacePack;
            var pack = packName != null ? ArtLibrary.Instance.FacePack(packName) : null;
            var key = "face." + look.Line + "." + ColorUtility.ToHtmlStringRGB(ink) + "." + (packName ?? "deadpan");
            var faceMat = Mat(key, "ThemeFace");
            faceMat.SetTexture("_Faces", pack != null ? pack : ArtLibrary.Instance.FaceMaterial.GetTexture("_Faces"));
            faceMat.SetFloat("_Mode", look.Style);
            faceMat.SetColor("_Ink", ink);
            face.GetComponent<FaceGraphic>().material = faceMat;
        }
    }

    /// <summary>Bruh Arrows in the shop: arrows with a face on their tail, on the paper.</summary>
    public sealed class ArrowShopPainter : IShopPainter
    {
        public Color Background => ArrowSkins.Paper[0];

        public void Card(RectTransform area, SkinDef skin)
        {
            var paper = skin.Scene ?? ArrowSkins.Paper;
            var colors = skin.Colors ?? (skin.Tab == 1 ? Skins.Palette : null) ?? Skins.Equipped(0)?.Colors ?? Skins.CatalogOf(ArrowSkins.GameId).Default(0).Colors;
            var look = skin.Tab == 1 ? ArrowSkins.LookOf(skin) : ArrowSkins.Current;
            Ground(area, look, paper, 44f);
            Grid(area, paper[1], 7, 3, 44f);
            Arrow(area, new[] { new Vector2(-150, -44), new Vector2(-150, 0), new Vector2(-62, 0) }, ArrowSkins.OnPaper(colors[1], paper), 44f, look, paper[0]);
            Arrow(area, new[] { new Vector2(10, 44), new Vector2(98, 44), new Vector2(98, 0), new Vector2(150, 0) }, ArrowSkins.OnPaper(colors[3], paper), 44f, look, paper[0]);
            Arrow(area, new[] { new Vector2(-18, -44), new Vector2(98, -44) }, ArrowSkins.OnPaper(colors[0], paper), 44f, look, paper[0]);
        }

        public void Preview(RectTransform area, SkinDef pieces, SkinDef scene)
        {
            var paper = scene?.Scene ?? ArrowSkins.Paper;
            var colors = scene?.Colors ?? pieces?.Colors ?? Skins.CatalogOf(ArrowSkins.GameId).Default(0).Colors;
            var look = ArrowSkins.LookOf(scene);
            const float cell = 96f;
            Ground(area, look, paper, cell);
            Grid(area, paper[1], 9, 5, cell);
            Vector2 C(float col, float row) => new((col - 4f) * cell, (2f - row) * cell);
            Color On(int i) => ArrowSkins.OnPaper(colors[i], paper);
            Arrow(area, new[] { C(0, 4), C(0, 1), C(2, 1) }, On(1), cell, look, paper[0]);
            Arrow(area, new[] { C(1, 3), C(4, 3), C(4, 0) }, On(0), cell, look, paper[0]);
            Arrow(area, new[] { C(6, 4), C(6, 2), C(8, 2) }, On(3), cell, look, paper[0]);
            Arrow(area, new[] { C(8, 0), C(5, 0) }, On(2), cell, look, paper[0]);
            Arrow(area, new[] { C(3, 4), C(5, 4) }, On(2), cell, look, paper[0]);
        }

        // the paper, or the theme's shader ground (chalk smudges, blueprint grid, CRT glass…)
        private static void Ground(RectTransform area, ArrowSkins.Look look, Color[] paper, float cell)
        {
            var img = UIKit.AddImage(area, "round_rect", paper[0]);
            var m = ArrowSkins.GroundMaterial(look, paper, cell);
            if (m == null) return;
            img.sprite = null;
            img.color = Color.white;
            img.material = m;
        }

        private static void Grid(RectTransform area, Color color, int cols, int rows, float cell)
        {
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    UIKit.Image(area, "grid_dot", new Vector2(0.5f, 0.5f), new Vector2((c - (cols - 1) / 2f) * cell, ((rows - 1) / 2f - r) * cell), new Vector2(cell * 0.28f, cell * 0.28f), color);
        }

        // points: tail first, head last (the head points along the last segment)
        private static void Arrow(RectTransform area, Vector2[] points, Color color, float cell, ArrowSkins.Look look, Color ground)
        {
            var layer = UIKit.Stretch(UIKit.Rect("Arrow", area));
            var d = points[^1] - points[^2];
            var dir = Mathf.Abs(d.x) > Mathf.Abs(d.y) ? (d.x > 0 ? 1 : 3) : (d.y > 0 ? 0 : 2); // board dirs: up, right, down, left
            var headFirst = (Vector2[])points.Clone();
            System.Array.Reverse(headFirst);
            var stroke = ArrowBoardView.CreateStroke(layer, headFirst, dir, cell, color);
            var cap = UIKit.Image(layer, "dot", new Vector2(0.5f, 0.5f), points[0], new Vector2(cell * 0.6f, cell * 0.6f), color);
            var face = Face.AddUI(cap.transform, new Vector2(cell * 0.44f, cell * 0.44f), new Vector2(0, cell * 0.02f));
            face.InkFor(color);
            ArrowSkins.Dress(look, stroke, cap, face, color, ground);
        }
    }
}
