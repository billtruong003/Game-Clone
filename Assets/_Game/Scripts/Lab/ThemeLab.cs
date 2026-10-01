using System.Collections.Generic;
using CasualGame.ArrowOut;
using CasualGame.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CasualGame.Lab
{
    /// <summary>
    /// Theme / shader lab (Docs/SHADER_LAB.md). One page fills the screen at a time, portrait like the games.
    /// <list type="bullet">
    ///   <item><b>Arrows</b>: a small Bruh Arrows board whose arrows draw on, wait and fly out on a loop. The line, the tail
    ///   cap (ThemeSprite) and the face (ThemeFace) all use the theme's shaders.</item>
    ///   <item><b>Balls</b>: a settled Meh Merge pile of all 11 tiers in one premium set (BallSkin). The balls roll, and
    ///   the monster set has procedural eyes.</item>
    ///   <item><b>Blocks</b>: a Nah Blocks board in one premium set (BlockSkin), with themed faces.</item>
    /// </list>
    /// Left / Right (or a tap) changes the page. Editor captures: <see cref="Show"/>(index, phase) freezes a page.
    /// </summary>
    public sealed class ThemeLab : MonoBehaviour
    {
        private enum Kind { Arrows, Balls, Blocks }

        private sealed class Page
        {
            public string Name, Note;
            public Kind Kind;
            public int Ground = -1;               // ThemeGround mode, -1 = flat Base colour
            public Color Base, Ink;
            public Color Text = UIKit.Paper;
            public string ArrowShader;            // null = the game's default UI shader
            public Color[] Palette;
            public float GlowPad;
            public int Style = -1;                // ThemeSprite / ThemeFace style for caps and faces, -1 = default look
            public int Set;                       // BallSkin / BlockSkin set
        }

        private static Color H(string hex) => UIKit.Hex(hex);
        private static Color[] P(params string[] hex) => System.Array.ConvertAll(hex, UIKit.Hex);

        private static readonly Page[] Pages =
        {
            // ---------------- Bruh Arrows ----------------
            new() { Kind = Kind.Arrows, Name = "Arrows · Classic (now)", Note = "Flat strokes, default shader: the baseline", Base = H("#F5F1EA"),
                Palette = P("#2E3A59", "#35B09F", "#7B4FAE", "#EDA93C"), Text = UIKit.Ink },
            new() { Kind = Kind.Arrows, Name = "Arrows · Sumi Ink", Note = "Brush line draws on; cap and face bleed like ink", Ground = 0, Base = H("#F3EEE3"), Ink = H("#D9D2C5"),
                ArrowShader = "ArrowInk", Style = 0, Palette = P("#1E2240", "#2F4A7A", "#B83A2E", "#6B4A2E"), Text = UIKit.Ink },
            new() { Kind = Kind.Arrows, Name = "Arrows · Chalkboard", Note = "Grainy chalk line, chalk cap, chalk face", Ground = 1, Base = H("#2F4F3A"), Ink = H("#5C7A64"),
                ArrowShader = "ArrowChalk", Style = 1, Palette = P("#F4F4F5", "#FFE08A", "#9BE7FF", "#FFB3C7") },
            new() { Kind = Kind.Arrows, Name = "Arrows · Blueprint", Note = "Outline, dashed centre, ticks, drafting grid", Ground = 2, Base = H("#1F4E8C"), Ink = H("#BFD7FF"),
                ArrowShader = "ArrowBlueprint", Palette = P("#FFFFFF", "#BFD7FF", "#FFE08A", "#7FD1FF") },
            new() { Kind = Kind.Arrows, Name = "Arrows · Vector CRT", Note = "Arcade vector monitor: thin phosphor beams, the spot draws them", Ground = 3, Base = H("#04080A"), Ink = H("#1F5C2A"),
                ArrowShader = "ArrowVector", GlowPad = 0.45f, Style = 2, Palette = P("#5CFF7A", "#3FD8FF", "#FFD166", "#FF6B9A") },
            new() { Kind = Kind.Arrows, Name = "Arrows · Hologram", Note = "Rims, scanlines, light bands; holo caps and faces", Ground = 4, Base = H("#0B1220"), Ink = H("#1A6B8A"),
                ArrowShader = "ArrowHolo", GlowPad = 0.45f, Style = 3, Palette = P("#3DFFB0", "#5CE1FF", "#FF9D3C", "#C38BFF") },
            new() { Kind = Kind.Arrows, Name = "Arrows · Neon", Note = "White-hot tubes; glowing caps and faces", Ground = 4, Base = H("#0B0B12"), Ink = H("#2A1E4A"),
                ArrowShader = "ArrowNeon", GlowPad = 0.55f, Style = 2, Palette = P("#00E5FF", "#FF2BD6", "#39FF14", "#FFB300") },
            // ---------------- Meh Merge premium (picks: Billiard A, Sports A, Eyeballs B, Planets A) ----------------
            new() { Kind = Kind.Balls, Set = 0, Name = "Merge · Billiard", Note = "Sphere shader: solids / stripes, numbers from a texture atlas, rolling", Base = H("#1F6B3A") },
            new() { Kind = Kind.Balls, Set = 1, Name = "Merge · Sports", Note = "Procedural seams, panels, dimples on one sphere shader", Ground = 0, Base = H("#3D8B4A"), Ink = H("#2F7A3E") },
            new() { Kind = Kind.Balls, Set = 2, Name = "Merge · Planets", Note = "Noise surfaces, clouds, Saturn's ring, the Sun's corona", Ground = 4, Base = H("#0B1026"), Ink = H("#2A3A7A") },
            new() { Kind = Kind.Balls, Set = 3, Name = "Merge · Monsters", Note = "Colour + horns, the game's faces", Base = H("#2F2552") },
            new() { Kind = Kind.Balls, Set = 4, Name = "Merge · Slimes", Note = "Wobbling jelly drops with bubbles inside", Base = H("#24304A") },
            new() { Kind = Kind.Balls, Set = 5, Name = "Merge · Eyeballs", Note = "Real eyeballs: veined, iris + pupil turn to look at you", Base = H("#3A1F2E") },
            // ---------------- Nah Blocks premium (picks: Retro A, Pixel A, Studs B, Gems A) + CRT pillow ----------------
            new() { Kind = Kind.Blocks, Set = 0, Name = "Blocks · Retro Bricks", Note = "Hard bevel, black well", Base = H("#000000"),
                Palette = P("#E04040", "#3070E0", "#F0C020", "#30B050", "#A040C0", "#F07020", "#20B0C0") },
            new() { Kind = Kind.Blocks, Set = 2, Name = "Blocks · Toy Bricks", Note = "Rounded glossy plastic, two studs on the top face", Base = H("#4C9A4F"),
                Palette = P("#E3000B", "#0055BF", "#F2CD37", "#00852B", "#FE8A18", "#A0A5A9", "#F4F4F4") },
            new() { Kind = Kind.Blocks, Set = 3, Name = "Blocks · Gems", Note = "Cut octagons, facets, a soft travelling glint", Base = H("#1B1030"),
                Palette = P("#E0115F", "#0F52BA", "#50C878", "#FFC87C", "#9966CC", "#7FFFD4", "#E4D00A") },
        };

        private const float Cell = 140f;
        private const float DrawTime = 0.7f, Hold = 1.2f, FlyGap = 0.35f, FlySpeed = 2600f;

        // A 6 x 8 board, arrows listed tail first as (col, row) cells; the head points along the last step.
        private static readonly (int color, Vector2Int[] cells)[] Board =
        {
            (0, new[] { new Vector2Int(0, 7), new Vector2Int(0, 6), new Vector2Int(0, 5), new Vector2Int(1, 5), new Vector2Int(2, 5) }),
            (1, new[] { new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(1, 3) }),
            (2, new[] { new Vector2Int(5, 0), new Vector2Int(4, 0), new Vector2Int(3, 0), new Vector2Int(3, 1) }),
            (3, new[] { new Vector2Int(5, 7), new Vector2Int(5, 6), new Vector2Int(5, 5), new Vector2Int(5, 4), new Vector2Int(4, 4) }),
            (1, new[] { new Vector2Int(3, 3), new Vector2Int(4, 3), new Vector2Int(4, 2) }),
            (0, new[] { new Vector2Int(2, 7), new Vector2Int(3, 7), new Vector2Int(3, 6) }),
            (2, new[] { new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3) }),
        };

        private RectTransform root;
        private readonly List<Material> owned = new();
        private readonly List<(ArrowStroke stroke, Material mat, Image cap, Face face, float start)> arrows = new();
        private readonly List<(Material mat, RectTransform rt, float seed)> eyes = new();
        private readonly List<(Material mat, float speed)> rollers = new();
        private int index;
        private float clock;
        private bool frozen;

        private static ThemeLab instance;
        public static int Count => Pages.Length;

        private void Start()
        {
            instance = this;
            var cam = Camera.main;
            cam.backgroundColor = Color.black;
            var canvas = UIKit.CreateCameraCanvas("Lab", cam);
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            root = UIKit.Stretch(UIKit.Rect("Page", canvas.transform));
            Build(0);
        }

        /// <summary>Editor capture hook: show a page frozen at a point of its loop (seconds from the start).</summary>
        public static void Show(int pageIndex, float phase)
        {
            if (instance == null) return;
            instance.Build(pageIndex);
            instance.frozen = true;
            instance.clock = phase;
            instance.Animate();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.rightArrowKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) { frozen = false; Build((index + 1) % Pages.Length); }
            if (kb != null && kb.leftArrowKey.wasPressedThisFrame) { frozen = false; Build((index + Pages.Length - 1) % Pages.Length); }
            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame) { frozen = false; Build((index + 1) % Pages.Length); }
            if (!frozen) clock += Time.deltaTime;
            Animate();
        }

        private Material Mat(string shader)
        {
            var m = new Material(Shader.Find("CasualGame/Lab/" + shader));
            owned.Add(m);
            return m;
        }

        private void Build(int i)
        {
            index = i;
            clock = 0f;
            foreach (Transform c in root) Destroy(c.gameObject);
            foreach (var m in owned) Destroy(m);
            owned.Clear();
            arrows.Clear();
            eyes.Clear();
            rollers.Clear();
            var page = Pages[i];

            var ground = UIKit.AddImage(UIKit.Stretch(UIKit.Rect("Ground", root)), (Sprite)null, page.Base);
            if (page.Ground >= 0)
            {
                var gm = Mat("ThemeGround");
                gm.SetFloat("_Mode", page.Ground);
                gm.SetColor("_Base", page.Base);
                gm.SetColor("_Ink", page.Ink);
                gm.SetFloat("_Cell", Cell);
                ground.material = gm;
                ground.color = Color.white;
            }
            var top = new Vector2(0.5f, 1f);
            UIKit.Label(root, $"{i + 1}/{Pages.Length}  {page.Name}", 66, top, new Vector2(0, -120), new Vector2(1040, 110), page.Text);
            UIKit.Label(root, page.Note, 36, top, new Vector2(0, -200), new Vector2(1000, 60), new Color(page.Text.r, page.Text.g, page.Text.b, 0.7f));

            switch (page.Kind)
            {
                case Kind.Arrows: BuildArrows(page); break;
                case Kind.Balls: BuildBalls(page); break;
                default: BuildBlocks(page); break;
            }
        }

        // Theme the face (and its ink) of a character with the page style.
        private void StyleFace(Face face, int style, Color ink)
        {
            if (style < 0) return;
            var fm = Mat("ThemeFace");
            fm.SetTexture("_Faces", ArtLibrary.Instance.FaceMaterial.GetTexture("_Faces"));
            fm.SetFloat("_Mode", style);
            fm.SetColor("_Ink", ink);
            face.GetComponent<FaceGraphic>().material = fm;
        }

        // ---------------- Bruh Arrows ----------------

        private void BuildArrows(Page t)
        {
            var board = UIKit.Place(UIKit.Rect("Board", root), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(Cell * 6, Cell * 8));
            board.gameObject.AddComponent<RectMask2D>().padding = new Vector4(-400, -400, -400, -400);
            var dotColor = t.Ground >= 0 ? new Color(t.Ink.r, t.Ink.g, t.Ink.b, 0.55f) : H("#D9D2C5");
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 6; c++)
                    UIKit.Image(board, "grid_dot", new Vector2(0.5f, 0.5f), CellPos(c, r), new Vector2(32, 32), dotColor);

            Material capMat = null;
            if (t.Style >= 0)
            {
                capMat = Mat("ThemeSprite");
                capMat.SetFloat("_Mode", t.Style);
                capMat.SetFloat("_Circle", 1f);
                // glowing styles get a bigger quad for the halo; neon caps are a hollow tube ring with the face inside
                capMat.SetFloat("_CircleR", t.Style is 2 or 3 or 5 ? 0.55f : 0.95f);
                capMat.SetFloat("_Ring", t.Style == 2 ? 0.28f : 0f);
                capMat.SetColor("_Inner", Color.Lerp(t.Base, Color.black, 0.2f)); // the ring is solid inside: nothing shows through the face
            }
            var capScale = t.Style is 2 or 3 or 5 ? 1.8f : 1f;
            for (int k = 0; k < Board.Length; k++)
            {
                var (ci, cells) = Board[k];
                var color = t.Palette[ci];
                var headFirst = new Vector2[cells.Length];
                for (int j = 0; j < cells.Length; j++) headFirst[j] = CellPos(cells[cells.Length - 1 - j].x, cells[cells.Length - 1 - j].y);
                var d = cells[^1] - cells[^2];
                int dir = d.x > 0 ? 1 : d.x < 0 ? 3 : d.y > 0 ? 2 : 0; // rows grow downwards: +y is the board's "down" (2)
                var stroke = ArrowBoardView.CreateStroke(board, headFirst, dir, Cell, color, 14f);
                Material mat = null;
                if (t.ArrowShader != null)
                {
                    mat = Mat(t.ArrowShader);
                    stroke.material = mat;
                    stroke.GlowPad = t.GlowPad;
                    // glowing lines start at the cap's edge, so the line never runs through the face
                    if (t.Style is 2 or 3 && mat.HasProperty("_TailClip")) mat.SetFloat("_TailClip", 0.3f);
                }
                var cap = UIKit.Image(board, "dot", new Vector2(0.5f, 0.5f), headFirst[^1], new Vector2(Cell * 0.6f, Cell * 0.6f) * capScale, color);
                if (capMat != null)
                {
                    cap.sprite = null; // the procedural circle needs the quad's own 0..1 UVs, not the atlas rect of "dot"
                    cap.material = capMat;
                }
                var face = Face.AddUI(cap.transform, new Vector2(Cell * 0.44f, Cell * 0.44f), new Vector2(0, Cell * 0.02f));
                face.InkFor(color);
                // glowing themes: the face is drawn in the arrow's own light; ink / chalk themes keep dark ink
                var dark = 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b < 0.36f;
                StyleFace(face, t.Style, t.Style >= 2 ? Color.Lerp(color, Color.white, 0.5f) : dark ? UIKit.Paper : UIKit.Ink);
                if (t.Style >= 2) face.transform.SetAsLastSibling();
                arrows.Add((stroke, mat, cap, face, k * 0.12f));
            }
        }

        private static Vector2 CellPos(int c, int r) => new((c - 2.5f) * Cell, (3.5f - r) * Cell);

        // ---------------- Meh Merge ----------------

        private static readonly Color[] BilliardColors = P("#F2C230", "#1F5FBF", "#C0392B", "#6A2C91", "#F77F00", "#1E8C45", "#8A2432", "#1A1A1A", "#F2C230", "#1F5FBF", "#C0392B");
        // sports, tier 1..11: (variant, colour A, colour B) — marble, ping-pong, golf, tennis, baseball, volleyball,
        // bowling, soccer, basketball, medicine ball, beach ball
        private static readonly (int v, string a, string b)[] Sports =
        {
            (6, "#7FD1FF", "#FFFFFF"), (6, "#FF9F1C", "#FFB347"), (5, "#FFFFFF", "#FFFFFF"), (0, "#D4E157", "#FFFFFF"), (1, "#FFFFFF", "#FFFFFF"),
            (4, "#FFFFFF", "#FFFFFF"), (6, "#2A2A40", "#5A3A8A"), (3, "#FFFFFF", "#FFFFFF"), (2, "#E8792A", "#E8792A"), (6, "#7A4A2A", "#5A3418"), (4, "#FFFFFF", "#FFFFFF"),
        };
        // planets, tier 1..11: Pluto, Moon, Mercury, Mars, Venus, Earth, Neptune, Uranus, Saturn, Jupiter, Sun
        private static readonly (int v, string a, string b)[] Planets =
        {
            (0, "#D9C7B0", "#9C8670"), (0, "#D8D8DC", "#8E8E96"), (0, "#B7A89A", "#6E6258"), (0, "#E0663A", "#8A2E16"), (2, "#F2D79A", "#D9A85A"),
            (1, "#2E6FD9", "#3FAE5A"), (2, "#3A5BD9", "#7FA0FF"), (2, "#8FE3E8", "#C8F7F7"), (3, "#E8C98A", "#C9A060"), (2, "#E0A060", "#F5E2C0"), (4, "#FFB84D", "#FFE38A"),
        };
        private static readonly FaceId[] Moods = { FaceId.Smug, FaceId.Grin, FaceId.Meh, FaceId.Stare, FaceId.Shock, FaceId.Smug, FaceId.Dizzy, FaceId.Grin, FaceId.Meh, FaceId.Cry, FaceId.Panic };

        private void BuildBalls(Page t)
        {
            const float unit = 56f, half = 4.5f * unit + 60f;
            var area = UIKit.Place(UIKit.Rect("Pile", root), new Vector2(0.5f, 0.5f), new Vector2(0, -80), new Vector2(1000, 1300));
            var floor = -600f;
            UIKit.Image(area, "round_rect", new Vector2(0.5f, 0.5f), new Vector2(0, floor - 14), new Vector2(2 * half + 30, 28), new Color(0, 0, 0, 0.35f));
            var digits = Resources.Load<Texture2D>("ball_digits");
#if UNITY_EDITOR
            if (digits == null) digits = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/Lab/ball_digits.png");
#endif
            // settled pile: big first, each ball rests at the lowest spot (as in the Merge shop preview)
            var placed = new List<(Vector2 c, float r)>();
            for (int tier = 11; tier >= 1; tier--)
            {
                var r = 0.38f * Mathf.Pow(1.25f, tier - 1) * unit;
                var room = t.Set == 3 ? 1.3f : 1f; // monsters keep room for their horns
                var best = Vector2.zero;
                var bestCost = float.MaxValue;
                var hint = (tier % 3 - 1) * 0.9f;
                for (var x = -half + r; x <= half - r; x += 4f)
                {
                    var y = floor + r;
                    foreach (var (c, pr) in placed)
                    {
                        var dx = x - c.x;
                        var reach = r + pr;
                        if (Mathf.Abs(dx) >= reach) continue;
                        var restY = c.y + Mathf.Sqrt(reach * reach - dx * dx);
                        if (room > 1f)
                        {
                            // horns stick up: resting straight on top of a monster needs room for them, side by side does not
                            var up = (restY - c.y) / reach;
                            reach = r + pr * (1f + (room - 1f) * up * up);
                            if (Mathf.Abs(dx) < reach) restY = c.y + Mathf.Sqrt(reach * reach - dx * dx);
                        }
                        y = Mathf.Max(y, restY);
                    }
                    var cost = y + 0.02f * Mathf.Abs(x - hint * (half - r));
                    if (cost < bestCost) { bestCost = cost; best = new Vector2(x, y); }
                }
                placed.Add((best, r));
                BuildBall(area, t, tier, best, r, digits);
            }
        }

        private void BuildBall(RectTransform parent, Page t, int tier, Vector2 pos, float r, Texture2D digits)
        {
            var m = Mat(t.Set == 4 ? "SlimeSkin" : "BallSkin"); // slimes have their own jelly shader
            m.SetFloat("_Set", t.Set);
            var sphereR = 0.94f;
            Color body = Color.white;
            switch (t.Set)
            {
                case 0:
                    body = BilliardColors[tier - 1];
                    m.SetColor("_ColA", body);
                    m.SetColor("_ColB", H("#FFF8EC"));
                    m.SetFloat("_Number", tier);
                    m.SetFloat("_Stripe", tier >= 9 ? 1 : 0);
                    if (digits != null) m.SetTexture("_Digits", digits);
                    break;
                case 1:
                    var s = Sports[tier - 1];
                    m.SetFloat("_Variant", s.v);
                    m.SetColor("_ColA", H(s.a));
                    m.SetColor("_ColB", H(s.b));
                    body = H(s.a);
                    break;
                case 2:
                    var pl = Planets[tier - 1];
                    m.SetFloat("_Variant", pl.v);
                    m.SetColor("_ColA", H(pl.a));
                    m.SetColor("_ColB", H(pl.b));
                    body = H(pl.a);
                    if (pl.v == 3) sphereR = 0.45f;   // room for the ring
                    if (pl.v == 4) sphereR = 0.7f;    // room for the corona
                    break;
                case 3: // monsters: colour + horns
                    body = new[] { H("#F15BB5"), H("#4EA8DE"), H("#3DDC97"), H("#FFD23F"), H("#9B5DE5"), H("#FF9F1C") }[tier % 6];
                    m.SetColor("_ColA", body);
                    m.SetColor("_ColB", H("#F3E3C3"));
                    sphereR = 0.6f;
                    break;
                case 4: // slimes
                    body = new[] { H("#7CE38B"), H("#6FC3F7"), H("#FF8FB1"), H("#C3A3FF"), H("#FFD166"), H("#FF9F6B") }[tier % 6];
                    m.SetColor("_Col", body);
                    m.SetFloat("_Seed", tier * 1.7f);
                    sphereR = 0.72f;
                    break;
                default: // eyeballs: iris outer / inner colours
                    var iris = new[] { (H("#1B4F8A"), H("#7FD1FF")), (H("#1B6E3A"), H("#9BFF7A")), (H("#6B3A12"), H("#E0A050")), (H("#4A1A7A"), H("#C38BFF")), (H("#7A1020"), H("#FF8A6B")) }[tier % 5];
                    m.SetColor("_ColA", iris.Item1);
                    m.SetColor("_ColB", iris.Item2);
                    m.SetFloat("_Variant", (tier % 3) / 2f);
                    break;
            }
            m.SetFloat("_R", sphereR);
            var size = 2f * r / sphereR;
            if (t.Set == 4) pos.y -= r * 0.14f; // a slime's flat bottom sits lower than a ball's: rest it on what is below
            var img = UIKit.Image(parent, null, new Vector2(0.5f, 0.5f), pos, new Vector2(size, size), Color.white);
            img.material = m;
            rollers.Add((m, (tier % 2 == 0 ? 1f : -1f) * (0.6f + 0.1f * tier)));
            var dark = body.grayscale < 0.4f;

            if (t.Set == 5) { eyes.Add((m, img.rectTransform, tier * 0.37f)); return; } // the ball is the eye: no face
            if (t.Set == 99)
            {
                // (old monster-eyes layout, kept for reference)
                var count = tier >= 9 ? 3 : tier >= 5 ? 2 : 1;
                var eyeSize = r * (count == 1 ? 1.15f : count == 2 ? 0.8f : 0.62f);
                for (int e = 0; e < count; e++)
                {
                    var x = count == 1 ? 0f : (e - (count - 1) / 2f) * eyeSize * 0.95f;
                    var y = count == 3 && e == 1 ? r * 0.38f : r * 0.18f;
                    var rt = UIKit.Place(UIKit.Rect("Eye", img.transform), new Vector2(0.5f, 0.5f), new Vector2(x, y), Vector2.one * eyeSize);
                    var ei = UIKit.AddImage(rt, (Sprite)null, Color.white);
                    var em = Mat("ProceduralEye");
                    em.SetColor("_IrisOut", Color.Lerp(body, Color.black, 0.55f));
                    em.SetColor("_IrisIn", Color.Lerp(body, Color.white, 0.35f));
                    em.SetColor("_Lid", Color.Lerp(body, Color.black, 0.12f));
                    em.SetFloat("_IrisR", 0.42f);
                    em.SetFloat("_PupilR", 0.17f);
                    em.SetFloat("_Slit", tier % 4 == 0 ? 1f : 0f);
                    ei.material = em;
                    eyes.Add((em, rt, tier * 0.7f + e * 0.31f));
                }
                UIKit.Image(img.transform, "round_rect", new Vector2(0.5f, 0.5f), new Vector2(0, -r * 0.45f), new Vector2(r * 0.42f, r * 0.08f), UIKit.Ink);
                return;
            }
            if (t.Set == 2 && Planets[tier - 1].v == 4) dark = false;
            var face = Face.AddUI(img.transform, Vector2.one * r * 1.3f, new Vector2(0, -r * 0.05f));
            face.SetIdle(Moods[tier - 1]);
            face.InkFor(dark ? Color.black : Color.white);
            if (t.Set == 0)
            {
                // billiard: the face sits on the cream plate (dark ink always), the number above it
                face.InkFor(Color.white);
                face.transform.localPosition = new Vector3(0, -r * 0.16f, 0);
                face.transform.localScale = Vector3.one * 0.8f;
            }
            if (t.Set == 4) face.transform.localPosition = new Vector3(0, -r * 0.12f, 0);
        }

        // ---------------- Nah Blocks ----------------

        private void BuildBlocks(Page t)
        {
            const float cell = 150f;
            int[,] grid =
            {
                { 0, 0, 2, 0, 0, 0 },
                { 0, 1, 2, 2, 0, 3 },
                { 4, 1, 1, 5, 0, 3 },
                { 4, 4, 6, 5, 5, 3 },
                { 7, 6, 6, 1, 2, 2 },
                { 7, 7, 3, 1, 1, 4 },
            };
            var board = UIKit.Place(UIKit.Rect("Board", root), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(cell * 6, cell * 6));
            var well = UIKit.AddImage(board, "round_rect", Color.Lerp(t.Base, Color.black, 0.35f));
            well.pixelsPerUnitMultiplier = 0.6f;
            var studs = false; // the toy brick now fits its square cell
            int mood = 0;
            for (int r = 0; r < 6; r++)
                for (int c = 0; c < 6; c++)
                {
                    var v = grid[r, c];
                    var pos = new Vector2((c - 2.5f) * cell, (2.5f - r) * cell);
                    if (v == 0) { UIKit.Image(board, "round_rect", new Vector2(0.5f, 0.5f), pos, new Vector2(cell - 10, cell - 10), new Color(1, 1, 1, 0.05f)); continue; }
                    var color = t.Palette[v - 1];
                    var m = Mat("BlockSkin");
                    m.SetFloat("_Set", t.Set);
                    m.SetFloat("_Seed", r * 7 + c);
                    var size = studs ? new Vector2(cell - 6, (cell - 6) * 1.25f) : new Vector2(cell - 6, cell - 6);
                    var img = UIKit.Image(board, null, new Vector2(0.5f, 0.5f), pos + (studs ? new Vector2(0, (cell - 6) * 0.125f) : Vector2.zero), size, color);
                    img.material = m;
                    var face = Face.AddUI(img.transform, Vector2.one * cell * 0.62f, new Vector2(0, t.Set == 2 ? -cell * 0.1f : -cell * 0.02f)); // toy brick: on the body, under the studs
                    face.SetIdle(Moods[mood++ % Moods.Length]);
                    face.InkFor(color);
                    if (t.Set == 3) face.transform.localScale = Vector3.one * 0.8f;
                    var dark = color.grayscale < 0.36f;
                    StyleFace(face, t.Style, t.Style == 5 ? Color.Lerp(color, Color.white, 0.6f) : dark ? UIKit.Paper : UIKit.Ink);
                }
        }

        // ---------------- animation ----------------

        private void Animate()
        {
            var page = Pages[index];
            if (page.Kind == Kind.Arrows) AnimateArrows();
            foreach (var (m, speed) in rollers) m.SetFloat("_Spin", Mathf.Sin(clock * 0.8f) * speed * 0.6f);
            AnimateEyes();
        }

        private void AnimateArrows()
        {
            var flyStart = DrawTime + Board.Length * 0.12f + Hold;
            var loop = flyStart + Board.Length * FlyGap + 1.2f;
            var t = clock % loop;
            for (int k = 0; k < arrows.Count; k++)
            {
                var (stroke, mat, cap, face, start) = arrows[k];
                var reveal = Mathf.Clamp01((t - start) / DrawTime);
                if (mat != null && mat.HasProperty("_Reveal")) mat.SetFloat("_Reveal", reveal);
                else stroke.WidthScale = reveal; // the default shader cannot draw on: grow in instead
                var fly = t - (flyStart + k * FlyGap);
                stroke.Advance = fly > 0f ? fly * fly * FlySpeed * 0.5f + fly * 400f : 0f;
                cap.rectTransform.anchoredPosition = stroke.TailPoint;
                var visible = reveal > 0.02f && stroke.Advance < stroke.BodyLength;
                cap.enabled = visible;
                face.gameObject.SetActive(visible);
            }
        }

        private void AnimateEyes()
        {
            if (eyes.Count == 0) return;
            // everybody follows one wandering point; each eye blinks on its own rhythm
            var target = new Vector2(Mathf.Sin(clock * 0.9f) * 420f, Mathf.Sin(clock * 1.3f + 1f) * 520f - 200f);
            foreach (var (mat, rt, seed) in eyes)
            {
                var world = rt.position;
                var local = (Vector2)root.InverseTransformPoint(world);
                var look = Vector2.ClampMagnitude((target - local) / 400f, 1f);
                mat.SetVector("_Look", look);
                var b = Mathf.Repeat(clock + seed, 3.1f);
                mat.SetFloat("_Blink", b < 0.16f ? Mathf.Sin(b / 0.16f * Mathf.PI) : 0f);
            }
        }
    }
}
