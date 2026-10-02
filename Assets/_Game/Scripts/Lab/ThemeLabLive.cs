using System;
using System.Collections.Generic;
using CasualGame.ArrowOut;
using CasualGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CasualGame.Lab
{
    /// <summary>
    /// The lab's interactive skin pages (Docs/SHADER_LAB.md): each skin reacts to a finger. The finger wanders on its own;
    /// drag in the Game view to take over (release drops the Merge ball / places the Blocks piece; click an arrow to tap it).
    /// Every skin's look comes from its material template in Assets/_Game/Skins: edit it in the Inspector while the page
    /// runs and the change shows at once (the lab copies the template every frame, then writes the live values).
    /// </summary>
    public sealed partial class ThemeLab
    {
        private sealed class Live
        {
            public Material Mat;
            public Image Img;
            public Face Face;
            public Action<Material> Init;      // per-object values the template copy would overwrite
            public Vector2 Pos;
            public int Tier, Row, Col, Seed;
            // per-skin state
            public float Spin, SpinVel, Stride, Tumble, TumbleT = 99f, Sleep, Needle = 1.57f, NeedleVel, Shake, Mouth, Sulk, Heat, Tilt, TiltVel, Slosh, Lit, Clear;
            public Vector2 Look;
        }

        private readonly List<Live> live = new();
        private Material template;
        private RectTransform hand;
        private Vector2 finger, lastFinger, fingerVel;
        private bool manual, released;
        private float eventClock;

        private static readonly Color[] Rainbow = P("#FF6B6B", "#FFA94D", "#FFD43B", "#69DB7C", "#38D9A9", "#4DABF7", "#748FFC", "#B197FC", "#F783AC", "#E8590C", "#2B8A3E");

        private Material Template(string shader)
        {
#if UNITY_EDITOR
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets(shader + " t:Material", new[] { "Assets/_Game/Skins" }))
            {
                var m = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                if (m != null && m.shader != null && m.shader.name.EndsWith("/" + shader)) return m;
            }
#endif
            return null;
        }

        private Material LiveMat(string shader) => template != null ? Own(new Material(template)) : Mat(shader);
        private Material Own(Material m) { owned.Add(m); return m; }

        private void BuildFinger()
        {
            hand = UIKit.Hand(root, new Vector2(0.5f, 0.5f), Vector2.zero, 170f);
            hand.GetComponent<Image>().raycastTarget = false;
            UIKit.Label(root, "drag = your finger   ·   tap the title = next page", 30, new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(1000, 50), new Color(1, 1, 1, 0.55f));
        }

        // Pointer input on live pages: a drag moves the finger, a release is a drop / tap.
        private void LiveInput(bool down, bool held, bool up, Vector2 local)
        {
            released = false;
            if (down || held) { manual = true; finger = local; }
            if (up && manual) released = true;
        }

        private void AutoFinger(Vector2 a, Vector2 b)
        {
            if (manual) return;
            finger = new Vector2(Mathf.Sin(clock * a.x) * b.x, Mathf.Sin(clock * a.y + 1f) * b.y);
        }

        private void LiveFrame()
        {
            var dt = frozen ? 0f : Time.deltaTime;
            fingerVel = dt > 0 ? Vector2.Lerp(fingerVel, (finger - lastFinger) / dt, 0.2f) : fingerVel;
            lastFinger = finger;
            if (template != null)
                foreach (var l in live) { l.Mat.CopyPropertiesFromMaterial(template); l.Init?.Invoke(l.Mat); }
        }

        // ================================================= Meh Merge =================================================

        private static readonly int[] PileTiers = { 7, 6, 6, 5, 5, 4, 4, 4, 3, 3, 3, 2, 2, 2, 1, 1, 1 };
        private const float BallUnit = 80f, PileFloor = -560f;
        private Live heldBall;
        private int heldTier = 3;

        private static float BallR(int tier) => 0.38f * Mathf.Pow(1.25f, tier - 1) * BallUnit;

        private void BuildLiveBalls(Page t)
        {
            template = Template(t.Skin);
            live.Clear();
            var area = UIKit.Place(UIKit.Rect("Pile", root), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 1400));
            const float half = 420f;
            UIKit.Image(area, "round_rect", new Vector2(0.5f, 0.5f), new Vector2(0, PileFloor - 14), new Vector2(2 * half + 30, 28), new Color(0, 0, 0, 0.35f));
            var placed = new List<(Vector2 c, float r)>();
            for (int k = 0; k < PileTiers.Length; k++)
            {
                var tier = PileTiers[k];
                var r = BallR(tier);
                var best = Vector2.zero;
                var bestCost = float.MaxValue;
                var hint = ((k * 7) % 5 - 2) * 0.45f;
                for (var x = -half + r; x <= half - r; x += 4f)
                {
                    var y = PileFloor + r;
                    foreach (var (c, pr) in placed)
                    {
                        var dx = x - c.x;
                        var reach = r + pr;
                        if (Mathf.Abs(dx) < reach) y = Mathf.Max(y, c.y + Mathf.Sqrt(reach * reach - dx * dx));
                    }
                    var cost = y + 0.03f * Mathf.Abs(x - hint * (half - r));
                    if (cost < bestCost) { bestCost = cost; best = new Vector2(x, y); }
                }
                placed.Add((best, r));
                live.Add(MakeBall(area, t, tier, best, k));
            }
            heldBall = MakeBall(area, t, heldTier, new Vector2(0, 470), 99);
            live.Add(heldBall);
            BuildFinger();
        }

        private Live MakeBall(RectTransform parent, Page t, int tier, Vector2 pos, int seed)
        {
            var r = BallR(tier);
            var l = new Live { Tier = tier, Pos = pos, Seed = seed, Mat = LiveMat(t.Skin) };
            l.Img = UIKit.Image(parent, null, new Vector2(0.5f, 0.5f), pos, Vector2.one * (2f * r / 0.94f), Rainbow[tier - 1]);
            l.Img.material = l.Mat;
            l.Init = m => { m.SetFloat("_R", 0.94f); if (m.HasProperty("_Scene")) m.SetFloat("_Scene", tier % 3); };
            l.Init(l.Mat);
            return l;
        }

        private void SetHeldTier(int tier)
        {
            heldTier = tier;
            heldBall.Tier = tier;
            var r = BallR(tier);
            heldBall.Img.color = Rainbow[tier - 1];
            heldBall.Img.rectTransform.sizeDelta = Vector2.one * (2f * r / 0.94f);
        }

        private void AnimateLiveBalls(Page t)
        {
            var dt = frozen ? 0.016f : Time.deltaTime;
            AutoFinger(new Vector2(0.55f, 0.3f), new Vector2(360f, 0f));
            if (!manual) finger.y = 430f;                                  // the hand holds the ball
            // the held ball follows the finger along the top; every few seconds (or on release) it "drops"
            var heldX = Mathf.Clamp(finger.x, -360f, 360f);
            heldBall.Pos = new Vector2(heldX, 470f);
            heldBall.Img.rectTransform.anchoredPosition = heldBall.Pos;
            eventClock += dt;
            var drop = released || (!manual && eventClock > 4.5f);
            if (drop)
            {
                eventClock = 0f;
                Impact(new Vector2(heldX, -200f));
                SetHeldTier(1 + (heldTier % 4));
            }
            for (int i = 0; i < live.Count; i++)
            {
                var b = live[i];
                var m = b.Mat;
                var held = b == heldBall;
                // rolling in bursts; impacts add to it
                var roll = held ? -fingerVel.x / 700f : Mathf.Sin(clock * 0.45f + b.Seed * 1.7f) * 1.4f;
                b.SpinVel = Mathf.Lerp(b.SpinVel, roll, dt * 3f);
                b.Spin += b.SpinVel * dt;
                m.SetFloat("_Spin", b.Spin);
                switch (t.Skin)
                {
                    case "SkinHungryBall": Hungry(b, held, dt); break;
                    case "SkinHamsterBall": Hamster(b, dt); break;
                    case "SkinCompassBall": Compass(b, held, dt); break;
                    case "SkinSnowGlobe":
                        if (held) b.Shake = Mathf.Max(b.Shake, Mathf.Clamp01(Mathf.Abs(fingerVel.x) / 1600f));
                        b.Shake *= Mathf.Exp(-dt * 0.6f);
                        m.SetFloat("_Shake", b.Shake);
                        break;
                }
            }
        }

        private void Impact(Vector2 at)
        {
            foreach (var b in live)
            {
                if (b == heldBall) continue;
                var k = Mathf.Clamp01(1f - Vector2.Distance(b.Pos, at) / 500f);
                b.Shake = Mathf.Max(b.Shake, k);
                b.SpinVel += k * 3f * (b.Pos.x > at.x ? -1f : 1f);
                if (k > 0.55f) b.TumbleT = 0f;
            }
        }

        private void Hungry(Live b, bool held, float dt)
        {
            var target = heldBall.Pos;
            if (held)
            {
                // the held ball eyes its nearest twin below
                var twin = Nearest(b);
                target = twin != null ? twin.Pos : b.Pos + Vector2.down * 300f;
            }
            var to = target - b.Pos;
            var same = held ? Nearest(b) != null : b.Tier == heldTier;
            var near = Mathf.InverseLerp(1100f, 250f, to.magnitude);
            var look = Vector2.ClampMagnitude(to / 350f, 1f);
            b.Look = Vector2.Lerp(b.Look, same ? look : -look.normalized * 0.7f, dt * 6f);
            b.Mouth = Mathf.Lerp(b.Mouth, same ? Mathf.Lerp(0.35f, 1f, near) : 0f, dt * 5f);
            b.Sulk = Mathf.Lerp(b.Sulk, same ? 0f : 0.8f, dt * 4f);
            var m = b.Mat;
            m.SetVector("_Look", b.Look);
            m.SetFloat("_Mouth", b.Mouth);
            m.SetFloat("_Sulk", b.Sulk);
            var blink = Mathf.Repeat(clock + b.Seed * 0.73f, 3.7f);
            m.SetFloat("_Blink", blink < 0.14f ? Mathf.Sin(blink / 0.14f * Mathf.PI) : 0f);
        }

        private void Hamster(Live b, float dt)
        {
            var run = Mathf.Clamp(-b.SpinVel / 1.4f, -1f, 1f);
            b.Stride += Mathf.Abs(run) * dt * 14f;
            var resting = Mathf.Abs(run) < 0.2f;
            b.Sleep = Mathf.MoveTowards(b.Sleep, resting ? 1f : 0f, dt * (resting ? 0.6f : 4f));
            b.TumbleT += dt;
            b.Tumble = b.TumbleT < 0.7f ? Mathf.SmoothStep(0f, Mathf.PI * 2f, b.TumbleT / 0.7f) : 0f;
            if (b.TumbleT < 0.7f) b.Sleep = 0f;
            var m = b.Mat;
            m.SetFloat("_Run", run);
            m.SetFloat("_Stride", b.Stride);
            m.SetFloat("_Tumble", b.Tumble);
            m.SetFloat("_Sleep", b.Sleep);
        }

        private Live Nearest(Live b)
        {
            Live best = null;
            var bestD = float.MaxValue;
            foreach (var o in live)
            {
                if (o == b || o == heldBall || o.Tier != b.Tier) continue;
                var d = (o.Pos - b.Pos).sqrMagnitude;
                if (d < bestD) { bestD = d; best = o; }
            }
            return best;
        }

        private void Compass(Live b, bool held, float dt)
        {
            Live target = held ? Nearest(b) : null;
            if (!held)
            {
                target = Nearest(b);
                if (b.Tier == heldTier && (target == null || (heldBall.Pos - b.Pos).sqrMagnitude < (target.Pos - b.Pos).sqrMagnitude)) target = heldBall;
            }
            var want = target != null ? Mathf.Atan2(target.Pos.y - b.Pos.y, target.Pos.x - b.Pos.x) : clock * 0.8f + b.Seed;
            var diff = Mathf.DeltaAngle(b.Needle * Mathf.Rad2Deg, want * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            b.NeedleVel += diff * 60f * dt - b.NeedleVel * 7f * dt;
            b.Needle += b.NeedleVel * dt;
            b.Mat.SetFloat("_Needle", b.Needle);
        }

        // ================================================= Nah Blocks =================================================

        private const float BlockCell = 150f;
        private static readonly int[,] LiveGrid =
        {
            { 0, 0, 0, 0, 0, 0 },
            { 0, 0, 2, 0, 0, 0 },
            { 0, 1, 2, 2, 0, 3 },
            { 4, 1, 1, 5, 0, 3 },
            { 4, 4, 6, 5, 0, 0 },   // the piece fills (4, 4) and (4, 5): this row clears
            { 7, 6, 0, 1, 2, 2 },
        };
        private readonly List<Live> piece = new();
        private RectTransform boardRt;
        private float placeT = -1f;
        private Page blockPage;

        private static Vector2 BlockPos(int r, int c) => new((c - 2.5f) * BlockCell, (2.5f - r) * BlockCell);
        private static readonly Vector2 PieceTarget = (BlockPos(4, 4) + BlockPos(4, 5)) * 0.5f;
        private static readonly Vector2 PieceLift = new(0, 150f);  // the piece rides above the finger so it stays visible

        private void BuildLiveBlocks(Page t)
        {
            template = Template(t.Skin);
            blockPage = t;
            live.Clear();
            piece.Clear();
            placeT = -1f;
            boardRt = UIKit.Place(UIKit.Rect("Board", root), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(BlockCell * 6, BlockCell * 6));
            var well = UIKit.AddImage(boardRt, "round_rect", Color.Lerp(t.Base, Color.black, 0.35f));
            well.pixelsPerUnitMultiplier = 0.6f;
            for (int r = 0; r < 6; r++)
                for (int c = 0; c < 6; c++)
                {
                    UIKit.Image(boardRt, "round_rect", new Vector2(0.5f, 0.5f), BlockPos(r, c), new Vector2(BlockCell - 10, BlockCell - 10), new Color(1, 1, 1, 0.05f));
                    if (LiveGrid[r, c] != 0) live.Add(MakeBlock(boardRt, t, t.Palette[LiveGrid[r, c] - 1], r, c, BlockPos(r, c)));
                }
            for (int k = 0; k < 2; k++)
            {
                var b = MakeBlock(boardRt, t, t.Palette[2], -1, k, Vector2.zero);
                piece.Add(b);
                live.Add(b);
            }
            BuildFinger();
        }

        private Live MakeBlock(RectTransform parent, Page t, Color color, int r, int c, Vector2 pos)
        {
            var seed = (r + 2) * 7 + c;
            var l = new Live { Row = r, Col = c, Pos = pos, Seed = seed, Mat = LiveMat(t.Skin) };
            l.Img = UIKit.Image(parent, null, new Vector2(0.5f, 0.5f), pos, Vector2.one * (BlockCell - 6), color);
            l.Img.material = l.Mat;
            l.Init = m => m.SetFloat("_Seed", seed);
            l.Init(l.Mat);
            return l;
        }

        private void AnimateLiveBlocks(Page t)
        {
            var dt = frozen ? 0.016f : Time.deltaTime;
            // auto loop: wander 4.5 s, carry the piece to the gap, place, clear the row, reset at 8 s
            var loop = manual ? 0f : clock % 8f;
            if (!manual)
            {
                if (loop < 4.5f) finger = new Vector2(Mathf.Sin(clock * 0.9f) * 330f, Mathf.Sin(clock * 0.6f + 1f) * 300f - 60f);
                else if (loop < 5.4f) finger = Vector2.Lerp(finger, PieceTarget - PieceLift, dt * 8f);
                if (loop >= 5.4f && placeT < 0f) placeT = 0f;
                if (loop < 1f && placeT >= 0f) ResetBlocks();
            }
            else if (released)
            {
                if (placeT >= 0f) ResetBlocks();
                else if (Vector2.Distance(finger + PieceLift, PieceTarget) < 200f) placeT = 0f;
            }
            if (placeT >= 0f) placeT += dt;
            // piece: under the finger (raised so the finger does not hide it), or in the gap once placed
            var pieceCentre = placeT >= 0f ? PieceTarget : finger + PieceLift;
            for (int k = 0; k < piece.Count; k++)
            {
                piece[k].Pos = pieceCentre + new Vector2((k - 0.5f) * BlockCell, 0);
                piece[k].Img.rectTransform.anchoredPosition = piece[k].Pos;
            }
            var clearK = placeT < 0f ? 0f : Mathf.Clamp01((placeT - 0.15f) / 0.9f);
            // row fill (with the hovering piece as a preview)
            var rowFill = new float[6];
            for (int r = 0; r < 6; r++)
                for (int c = 0; c < 6; c++) if (LiveGrid[r, c] != 0) rowFill[r] += 1f / 6f;
            var hoverRow = Mathf.RoundToInt(2.5f - pieceCentre.y / BlockCell);
            if (hoverRow >= 0 && hoverRow < 6) rowFill[hoverRow] = Mathf.Min(1f, rowFill[hoverRow] + 2f / 6f);
            var pieceColor = t.Palette[2];
            foreach (var b in live)
            {
                var isPiece = b.Row < 0;
                var inRow = b.Row == 4 || (isPiece && placeT >= 0f);
                var clear = inRow ? clearK : 0f;
                var m = b.Mat;
                var to = finger - b.Pos;
                switch (t.Skin)
                {
                    case "SkinWatchBlock":
                        b.Look = Vector2.Lerp(b.Look, isPiece ? Vector2.down * 0.6f : Vector2.ClampMagnitude(to / 380f, 1f), dt * 10f);
                        m.SetVector("_Look", b.Look);
                        m.SetFloat("_Alarm", isPiece ? 0f : Mathf.InverseLerp(260f, 120f, Vector2.Distance(b.Pos, pieceCentre)));
                        var blink = Mathf.Repeat(clock + b.Seed * 0.61f, 4.1f);
                        m.SetFloat("_Blink", Mathf.Max(clear > 0f ? 1f : 0f, blink < 0.14f ? Mathf.Sin(blink / 0.14f * Mathf.PI) : 0f));
                        break;
                    case "SkinBuildingBlock":
                        var want = isPiece ? 0.5f : Mathf.Pow(rowFill[b.Row], 1.6f);
                        b.Lit = Mathf.MoveTowards(b.Lit, want, dt * 1.5f);
                        m.SetFloat("_Lit", b.Lit);
                        m.SetVector("_Parallax", Vector2.ClampMagnitude(finger / 450f, 1f));
                        m.SetFloat("_Clear", clear);
                        break;
                    case "SkinChromeBlock":
                        m.SetVector("_Reflect", Vector2.ClampMagnitude(to / 700f, 1f));
                        m.SetFloat("_Ghost", isPiece ? 0f : Mathf.InverseLerp(450f, 150f, Vector2.Distance(b.Pos, pieceCentre)));
                        m.SetColor("_GhostColor", pieceColor);
                        break;
                    case "SkinAquariumBlock":
                        var tiltWant = isPiece && placeT < 0f ? Mathf.Clamp(-fingerVel.x / 1400f, -1f, 1f) : 0f;
                        b.TiltVel += ((tiltWant - b.Tilt) * 60f - b.TiltVel * 5f) * dt;
                        b.Tilt += b.TiltVel * dt;
                        if (placeT >= 0f && placeT < dt * 1.5f && !isPiece && Vector2.Distance(b.Pos, PieceTarget) < 400f) b.TiltVel += 6f;
                        m.SetFloat("_Tilt", Mathf.Clamp(b.Tilt, -1f, 1f));
                        m.SetFloat("_Slosh", Mathf.Clamp01(Mathf.Abs(b.TiltVel) * 0.2f));
                        m.SetFloat("_Drain", clear);
                        break;
                }
                if (b.Face != null) b.Face.gameObject.SetActive(clear < 0.6f);
                var s = clear > 0.7f ? Mathf.Clamp01(1f - (clear - 0.7f) / 0.3f) : 1f;
                b.Img.rectTransform.localScale = Vector3.one * s;
            }
        }

        private void ResetBlocks()
        {
            placeT = -1f;
            foreach (var b in live) { b.Img.rectTransform.localScale = Vector3.one; b.Lit = 0f; }
        }

        // ================================================= Bruh Arrows =================================================

        private sealed class LiveArrow
        {
            public ArrowStroke Stroke;
            public Material Mat;
            public Image Cap;
            public Face Face;
            public Vector2Int[] Cells;
            public Vector2Int Dir;          // board step (rows grow downwards)
            public float Len;               // cells, tail to tip
            public bool Gone, Leaving;
            public float GoT = -1f, BumpT = 99f, Free = 1f, March;
            public RectTransform Roll;      // tape: the roll that turns as it winds the tape up
        }

        private readonly List<LiveArrow> liveArrows = new();
        private RectTransform arrowBoard;
        private float stepClock;
        private int stepCount;

        private void BuildLiveArrows(Page t)
        {
            template = Template(t.Skin);
            liveArrows.Clear();
            live.Clear();
            arrowBoard = UIKit.Place(UIKit.Rect("Board", root), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(Cell * 6, Cell * 8));
            arrowBoard.gameObject.AddComponent<RectMask2D>().padding = new Vector4(-400, -400, -400, -400);
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 6; c++)
                    UIKit.Image(arrowBoard, "grid_dot", new Vector2(0.5f, 0.5f), CellPos2(c, r), new Vector2(32, 32), H("#D9D2C5"));
            for (int k = 0; k < Board.Length; k++)
            {
                var (ci, cells) = Board[k];
                var color = t.Palette[ci];
                var headFirst = new Vector2[cells.Length];
                for (int j = 0; j < cells.Length; j++) headFirst[j] = CellPos2(cells[cells.Length - 1 - j].x, cells[cells.Length - 1 - j].y);
                var d = cells[^1] - cells[^2];
                int dir = d.x > 0 ? 1 : d.x < 0 ? 3 : d.y > 0 ? 2 : 0;
                var stroke = ArrowBoardView.CreateStroke(arrowBoard, headFirst, dir, Cell, color, 12 * Cell);
                var mat = LiveMat(t.Skin);
                stroke.material = mat;
                stroke.GlowPad = t.GlowPad;
                var len = stroke.BodyLength / Cell;
                var a = new LiveArrow { Stroke = stroke, Mat = mat, Cells = cells, Dir = d, Len = len };
                a.Cap = UIKit.Image(arrowBoard, "dot", new Vector2(0.5f, 0.5f), stroke.TailPoint, new Vector2(Cell * 0.6f, Cell * 0.6f),
                    t.Skin == "ArrowZipper" ? Color.Lerp(color, H("#B8BCC8"), 0.55f) : color);
                a.Face = Face.AddUI(a.Cap.transform, new Vector2(Cell * 0.44f, Cell * 0.44f), new Vector2(0, Cell * 0.02f));
                a.Face.InkFor(a.Cap.color);
                if (t.Skin == "ArrowTape") a.Roll = TapeRoll(a, color);
                liveArrows.Add(a);
                live.Add(new Live { Mat = mat, Init = m => m.SetFloat("_Len", len) });
                mat.SetFloat("_Len", len);
            }
            stepClock = 0f;
            stepCount = 0;
            UIKit.Label(root, "click an arrow to tap it   ·   tap the title = next page", 30, new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(1000, 50), UIKit.Ink);
        }

        private static Vector2 CellPos2(int c, int r) => new((c - 2.5f) * Cell, (3.5f - r) * Cell);

        private bool IsFree(LiveArrow a)
        {
            var head = a.Cells[^1];
            for (var p = head + a.Dir; p.x >= 0 && p.x < 6 && p.y >= 0 && p.y < 8; p += a.Dir)
                foreach (var o in liveArrows)
                {
                    if (o == a || o.Gone || o.Leaving) continue;
                    foreach (var c in o.Cells) if (c == p) return false;
                }
            return true;
        }

        private void Tap(LiveArrow a)
        {
            if (a.Gone || a.Leaving) return;
            if (IsFree(a)) { a.Leaving = true; a.GoT = 0f; }
            else a.BumpT = 0f;
        }

        private void AnimateLiveArrows(Page t)
        {
            var dt = frozen ? 0.016f : Time.deltaTime;
            if (released)
            {
                var c = Mathf.RoundToInt(finger.x / Cell + 2.5f);
                var r = Mathf.RoundToInt(3.5f - (finger.y + 60f) / Cell);
                foreach (var a in liveArrows)
                    if (Array.IndexOf(a.Cells, new Vector2Int(c, r)) >= 0) { Tap(a); break; }
            }
            // auto: every 1.1 s bump a blocked arrow (every other step) or send the first free one; reset when empty
            stepClock += dt;
            if (!manual && stepClock > 1.1f)
            {
                stepClock = 0f;
                stepCount++;
                LiveArrow pick = null;
                if (stepCount % 2 == 0)
                    foreach (var a in liveArrows) if (!a.Gone && !a.Leaving && !IsFree(a)) { pick = a; break; }
                if (pick == null) foreach (var a in liveArrows) if (!a.Gone && !a.Leaving && IsFree(a)) { pick = a; break; }
                if (pick != null) Tap(pick);
                else if (liveArrows.TrueForAll(a => a.Gone)) ResetArrows();
            }
            if (manual && released && liveArrows.TrueForAll(a => a.Gone)) ResetArrows();
            foreach (var a in liveArrows)
            {
                if (a.Gone) continue;
                a.Free = Mathf.MoveTowards(a.Free, IsFree(a) ? 1f : 0f, dt * 4f);
                if (a.Leaving) a.GoT += dt;
                a.BumpT += dt;
                var bump = a.BumpT < 0.5f ? Mathf.Sin(a.BumpT / 0.5f * Mathf.PI) : 0f;
                var m = a.Mat;
                var dirVec = new Vector2(a.Dir.x, -a.Dir.y);
                m.SetFloat("_Free", a.Free);
                var progress = 0f;
                switch (t.Skin)
                {
                    case "ArrowTrain":
                    case "ArrowAnts":
                        var fly = a.Leaving ? a.GoT : 0f;
                        a.Stroke.Advance = fly * fly * 1400f * 0.5f + fly * 160f;
                        a.Stroke.Shift = dirVec * 16f * bump;
                        m.SetFloat("_Bump", bump);
                        m.SetFloat("_Go", a.Leaving ? 1f : 0f);
                        a.March += dt * (a.Leaving ? 14f : 4f + 4f * (1f - a.Free));
                        m.SetFloat("_March", a.March);
                        if (a.Stroke.Advance > a.Stroke.BodyLength + 6 * Cell) a.Gone = true;
                        break;
                    case "ArrowTape":
                        progress = a.Leaving ? Mathf.Clamp01(a.GoT / 0.6f) : 0.3f * bump;
                        m.SetFloat("_Peel", progress);
                        if (a.Leaving && progress >= 1f) a.Gone = true;
                        break;
                    case "ArrowZipper":
                        progress = a.Leaving ? Mathf.Clamp01(a.GoT / 0.75f) : 0.06f * Mathf.Abs(Mathf.Sin(a.BumpT * 18f)) * bump;
                        m.SetFloat("_Unzip", progress);
                        if (a.Leaving && progress >= 1f) a.Gone = true;
                        break;
                }
                PlaceLiveCap(t, a, progress, bump, dirVec);
                a.Stroke.enabled = !a.Gone;
                a.Cap.gameObject.SetActive(!a.Gone);
            }
        }

        private void PlaceLiveCap(Page t, LiveArrow a, float progress, float bump, Vector2 dirVec)
        {
            switch (t.Skin)
            {
                case "ArrowTrain":                                   // the face rides the engine
                    a.Cap.enabled = false;
                    a.Cap.rectTransform.anchoredPosition = a.Stroke.HeadPoint - dirVec * Cell * 0.3f;
                    a.Cap.rectTransform.localScale = Vector3.one * 0.75f;
                    break;
                case "ArrowAnts":                                    // ants have their own heads
                    a.Cap.enabled = false;
                    a.Face.gameObject.SetActive(false);
                    break;
                case "ArrowTape":
                    // the roll rides the peel front, turning as it winds the tape up (and grows a little)
                    var along = Mathf.Max(0f, progress * (a.Len + 0.5f) - 0.12f) * Cell;
                    a.Cap.enabled = false;
                    a.Cap.rectTransform.anchoredPosition = a.Stroke.PointAt(along);
                    a.Cap.rectTransform.localScale = Vector3.one * (1f + 0.25f * progress);
                    if (a.Roll != null) a.Roll.localEulerAngles = new Vector3(0, 0, -along / (Cell * 0.33f) * Mathf.Rad2Deg);
                    break;
                default:                                             // zipper: the cap is the slider's pull tab
                    a.Cap.rectTransform.anchoredPosition = a.Stroke.PointAt(Mathf.Max(0f, progress * (a.Len + 0.2f) - 0.1f) * Cell)
                                                           + (Vector2)(UnityEngine.Random.insideUnitCircle * 4f * bump);
                    a.Cap.rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Sin(clock * 9f) * 8f * a.Free);
                    break;
            }
        }

        private Texture2D tapeRoll;

        // The tape roll: a tinted ring with an untinted core / outline on top (Art/Skins/tape_roll.png: base | details).
        private RectTransform TapeRoll(LiveArrow a, Color color)
        {
#if UNITY_EDITOR
            if (tapeRoll == null) tapeRoll = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/Skins/tape_roll.png");
#endif
            var roll = UIKit.Place(UIKit.Rect("Roll", a.Cap.transform), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * Cell * 0.7f);
            for (int k = 0; k < 2; k++)
            {
                var img = UIKit.Stretch(UIKit.Rect(k == 0 ? "Tape" : "Core", roll)).gameObject.AddComponent<RawImage>();
                img.texture = tapeRoll;
                img.uvRect = new Rect(k * 0.5f, 0f, 0.5f, 1f);
                img.color = k == 0 ? color : Color.white;
                img.raycastTarget = false;
            }
            a.Face.transform.SetAsLastSibling();
            a.Face.transform.localScale = Vector3.one * 0.8f;
            a.Face.InkFor(Color.white);
            return roll;
        }

        private void ResetArrows()
        {
            foreach (var a in liveArrows)
            {
                a.Gone = a.Leaving = false;
                a.GoT = -1f;
                a.BumpT = 99f;
                a.Stroke.Advance = 0f;
                a.Stroke.Shift = Vector2.zero;
                a.Stroke.enabled = true;
                a.Cap.gameObject.SetActive(true);
            }
        }
    }
}
