using System.Collections;
using System.Collections.Generic;
using CasualGame.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CasualGame.EyeBlast
{
    /// <summary>
    /// Eye Blast: drag pieces onto an 8×8 board; full rows/columns clear. Every block is a deadpan character.
    /// Pure gameplay screen: score, best, pause/settings, game over with rewarded revive.
    /// </summary>
    public class EyeBlastGame : MonoBehaviour
    {
        public static readonly Color[] BlockColors =
        {
            Color.clear, UIKit.Hex("#FF5A5F"), UIKit.Hex("#FF9F1C"), UIKit.Hex("#FFD23F"), UIKit.Hex("#3DDC97"),
            UIKit.Hex("#4EA8DE"), UIKit.Hex("#9B5DE5"), UIKit.Hex("#F15BB5"),
        };
        private static readonly Color Background = UIKit.Hex("#2B2F55");
        private static readonly Color SlotColor = UIKit.Hex("#252A4E");
        private static readonly Color GreyBlock = UIKit.Hex("#6B6F8E");
        private const float Cell = 116f;
        private const float TrayScale = 0.52f;
        private const float DragLift = 174f; // 1.5 cells above the finger (B1)
        private static readonly float[] TrayX = { -330f, 0f, 330f };
        private const float TrayY = 330f;
        private const float WorriedFill = 0.7f;

        private Canvas canvas;
        private RectTransform root, boardRect, trayRoot, playRoot;
        private readonly BlastBoard board = new();
        private readonly BlockView[,] blocks = new BlockView[BlastBoard.Size, BlastBoard.Size];
        private readonly Piece[] tray = new Piece[3];
        private readonly RectTransform[] trayViews = new RectTransform[3];
        private ImpactFrame impact;
        private readonly List<Image> ghosts = new();
        private readonly List<Image> glows = new();
        private readonly List<int> rows = new(), cols = new();
        private readonly HashSet<BlockView> previewing = new();
        private TextMeshProUGUI scoreText, bestText;
        private Image bestStar;
        private RectTransform tutorialHand;
        private TextMeshProUGUI tutorialText;
        private int score, shownScore, best, startBest, streak;
        private int dragging = -1;
        private bool playing, paused, revived, worried, bestToastShown;

        /// <summary>One block on screen: tinted body + outline + face.</summary>
        private class BlockView
        {
            public RectTransform Rect;
            public Image Body;
            public Face Face;
        }

        private void Start()
        {
            var cam = Camera.main;
            cam.backgroundColor = Background;
            canvas = UIKit.CreateCameraCanvas("EyeBlastUI", cam);
            root = UIKit.Stretch(UIKit.Rect("Safe", canvas.transform));
            root.gameObject.AddComponent<SafeArea>();
            AppEvents.Back += OnBack;
            AppEvents.Suspended += OnSuspended;
            if (!TryRestore()) NewGame();
            GameAudio.PlayMusic("music_blast");
        }

        private void OnDestroy()
        {
            AppEvents.Back -= OnBack;
            AppEvents.Suspended -= OnSuspended;
        }

        private void OnSuspended()
        {
            SaveRun();
            OpenPause();
        }

        // ---------------- run in progress (B14): board + tray + score + streak ----------------

        private const string RunKey = "blast.run";

        [System.Serializable]
        private class RunData
        {
            public int score, startBest, streak;
            public bool revived;
            public int[] grid;
            public PieceData[] tray = new PieceData[3];
        }

        [System.Serializable]
        private class PieceData { public int color; public int[] cells; } // r0, c0, r1, c1, …; empty = no piece

        private void SaveRun()
        {
            if (!playing && !paused) return; // lost: nothing to resume
            var d = new RunData { score = score, startBest = startBest, streak = streak, revived = revived, grid = new int[BlastBoard.Size * BlastBoard.Size] };
            for (int r = 0; r < BlastBoard.Size; r++)
                for (int c = 0; c < BlastBoard.Size; c++) d.grid[r * BlastBoard.Size + c] = board.Grid[r, c];
            for (int i = 0; i < 3; i++)
            {
                var p = tray[i];
                d.tray[i] = new PieceData { color = p?.Color ?? 0, cells = new int[p == null ? 0 : p.Cells.Length * 2] };
                if (p == null) continue;
                for (int k = 0; k < p.Cells.Length; k++) { d.tray[i].cells[k * 2] = p.Cells[k].r; d.tray[i].cells[k * 2 + 1] = p.Cells[k].c; }
            }
            SaveStore.SetJson(RunKey, d);
            SaveStore.Save();
        }

        private bool TryRestore()
        {
            if (!SaveStore.Has(RunKey)) return false;
            var d = SaveStore.GetJson<RunData>(RunKey);
            if (d.grid == null || d.grid.Length != BlastBoard.Size * BlastBoard.Size || d.tray == null || d.tray.Length != 3) { SaveStore.Delete(RunKey); return false; }
            NewGame(false);
            score = shownScore = d.score;
            startBest = d.startBest;
            best = Mathf.Max(best, score);
            streak = d.streak;
            revived = d.revived;
            for (int r = 0; r < BlastBoard.Size; r++)
                for (int c = 0; c < BlastBoard.Size; c++)
                {
                    var color = d.grid[r * BlastBoard.Size + c];
                    if (color <= 0 || color >= BlockColors.Length) continue;
                    board.Grid[r, c] = color;
                    blocks[r, c] = MakeBlock(boardRect, color, CellPos(r, c), Cell - 6);
                }
            for (int i = 0; i < 3; i++)
            {
                var pd = d.tray[i];
                if (pd == null || pd.cells == null || pd.cells.Length < 2) continue;
                var cells = new (int, int)[pd.cells.Length / 2];
                for (int k = 0; k < cells.Length; k++) cells[k] = (pd.cells[k * 2], pd.cells[k * 2 + 1]);
                tray[i] = BlastBoard.MakePiece(cells, Mathf.Clamp(pd.color, 1, BlockColors.Length - 1));
                trayViews[i] = BuildPieceView(tray[i], i);
            }
            if (tray[0] == null && tray[1] == null && tray[2] == null) Deal();
            RefreshScore();
            scoreText.text = score.ToString();
            RefreshTrayFits();
            worried = false;
            RefreshWorry();
            OpenPause();
            return true;
        }

        private void OnBack()
        {
            if (playing) OpenPause();
        }

        private void NewGame() => NewGame(true);

        private void NewGame(bool fresh)
        {
            if (fresh) SaveStore.Delete(RunKey);
            paused = false;
            Tween.Kill(this);
            StopAllCoroutines();
            if (playRoot != null) Destroy(playRoot.gameObject);
            System.Array.Clear(board.Grid, 0, board.Grid.Length);
            System.Array.Clear(blocks, 0, blocks.Length);
            System.Array.Clear(tray, 0, tray.Length);
            ghosts.Clear();
            glows.Clear();
            previewing.Clear();
            score = shownScore = streak = 0;
            dragging = -1;
            revived = worried = bestToastShown = false;
            best = startBest = SaveStore.GetInt("blast.best");

            playRoot = UIKit.Stretch(UIKit.Rect("Play", root));
            var top = new Vector2(0.5f, 1f);
            scoreText = UIKit.Label(playRoot, "0", 120, top, new Vector2(0, -130), new Vector2(700, 150), UIKit.Paper);
            bestText = UIKit.Label(playRoot, "", 46, top, new Vector2(30, -225), new Vector2(600, 70), UIKit.Hex("#B9BCE0"));
            bestStar = UIKit.Image(playRoot, "icon_star", top, new Vector2(-60, -222), new Vector2(52, 52));
            UIKit.IconButton(playRoot, "round_white", "icon_pause", OpenPause, new Vector2(1f, 1f), new Vector2(-100, -100), 116);

            var frameSize = Cell * BlastBoard.Size + 44;
            var frame = UIKit.Image(playRoot, "frame", new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(frameSize, frameSize), UIKit.Hex("#1A1D3A"));
            boardRect = UIKit.Place(UIKit.Rect("Board", frame.transform), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Cell * BlastBoard.Size, Cell * BlastBoard.Size));
            for (int r = 0; r < BlastBoard.Size; r++)
                for (int c = 0; c < BlastBoard.Size; c++)
                    UIKit.Image(boardRect, "block_fill", new Vector2(0.5f, 0.5f), CellPos(r, c), new Vector2(Cell - 6, Cell - 6), SlotColor);
            for (int i = 0; i < 25; i++)
            {
                var g = UIKit.Image(boardRect, "block_fill", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Cell - 6, Cell - 6), new Color(1, 1, 1, 0.35f));
                g.enabled = false;
                ghosts.Add(g);
            }
            // B3: rows / columns the drop would clear get a white glow frame
            for (int i = 0; i < 6; i++)
            {
                var g = UIKit.Image(boardRect, "round_rect", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0.9f));
                g.type = Image.Type.Sliced;
                g.fillCenter = false;
                g.enabled = false;
                glows.Add(g);
            }

            trayRoot = UIKit.Place(UIKit.Rect("Tray", playRoot), new Vector2(0.5f, 0f), new Vector2(0, TrayY), new Vector2(1000, 360));
            for (int i = 0; i < 3; i++)
            {
                var zone = UIKit.Place(UIKit.Rect("Slot" + i, trayRoot), new Vector2(0.5f, 0.5f), new Vector2(TrayX[i], 0), new Vector2(320, 360));
                UIKit.AddImage(zone, (Sprite)null, new Color(1, 1, 1, 0)).raycastTarget = true;
                var drag = zone.gameObject.AddComponent<DragZone>();
                int idx = i;
                drag.Pressed = () => OnPress(idx);
                drag.Began = () => { };
                drag.Moved = e => OnDrag(idx, e);
                drag.Ended = e => OnDragEnd(idx, e);
                drag.Released = () => OnRelease(idx);
            }

            RefreshScore();
            playing = true;
            if (!fresh) return;
            Deal();
            if (!SaveStore.GetBool("blast.tutorial", false)) ShowTutorial();
        }

        private static Vector2 CellPos(int r, int c) =>
            new((c - (BlastBoard.Size - 1) / 2f) * Cell, -(r - (BlastBoard.Size - 1) / 2f) * Cell);

        private BlockView MakeBlock(Transform parent, int color, Vector2 pos, float size)
        {
            var rt = UIKit.Place(UIKit.Rect("Block", parent), new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            var body = UIKit.AddImage(rt, "block_fill", BlockColors[color]);
            UIKit.Image(rt, "block_line", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            var face = Face.AddUI(rt, new Vector2(size * 0.66f, size * 0.66f), new Vector2(0, size * 0.04f));
            return new BlockView { Rect = rt, Body = body, Face = face };
        }

        // ---------------- tray ----------------

        private void Deal()
        {
            var pieces = board.Deal(() => Random.value * 0.99999f);
            for (int i = 0; i < 3; i++)
            {
                tray[i] = pieces[i];
                trayViews[i] = BuildPieceView(pieces[i], i);
            }
            RefreshTrayFits();
        }

        private RectTransform BuildPieceView(Piece p, int slot)
        {
            var rt = UIKit.Place(UIKit.Rect("Piece", trayRoot), new Vector2(0.5f, 0.5f), new Vector2(TrayX[slot], 0), new Vector2(p.Width * Cell, p.Height * Cell));
            rt.gameObject.AddComponent<CanvasGroup>();
            foreach (var (r, c) in p.Cells)
                MakeBlock(rt, p.Color, new Vector2((c - (p.Width - 1) / 2f) * Cell, -(r - (p.Height - 1) / 2f) * Cell), Cell - 6);
            rt.localScale = Vector3.zero;
            Tween.Scale(rt, Vector3.one * TrayScaleOf(p), 0.3f, Ease.OutBack, 0.06f * slot);
            return rt;
        }

        // small pieces read bigger in the tray; long ones still fit the 320 px slot
        private static float TrayScaleOf(Piece p) => Mathf.Min(0.72f, 290f / (Mathf.Max(p.Width, p.Height) * Cell));

        private float TrayScaleAt(int slot) => tray[slot] != null ? TrayScaleOf(tray[slot]) : TrayScale;

        private void Praise(string word)
        {
            var t = UIKit.Label(playRoot, word, 108, UIKit.Hex("#FFD23F"));
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            t.rectTransform.sizeDelta = new Vector2(1000, 150);
            t.rectTransform.anchoredPosition = new Vector2(0, -330);
            t.rectTransform.localEulerAngles = new Vector3(0, 0, -3);
            t.outlineWidth = 0.3f;
            t.outlineColor = UIKit.Ink;
            t.transform.localScale = Vector3.one * 0.4f;
            Tween.Scale(t.transform, Vector3.one, 0.25f, Ease.OutBack);
            Tween.Run(t, 0.35f, k => t.alpha = 1f - k, Ease.Linear, 0.9f, () => Destroy(t.gameObject));
        }

        // B9: a piece that fits nowhere right now is greyed out
        private void RefreshTrayFits()
        {
            for (int i = 0; i < 3; i++)
                if (tray[i] != null && trayViews[i] != null)
                    trayViews[i].GetComponent<CanvasGroup>().alpha = board.FitsAnywhere(tray[i]) ? 1f : 0.45f;
        }

        private void SetPieceFaces(int i, FaceId? face, float seconds = -1f)
        {
            if (trayViews[i] == null) return;
            foreach (var f in trayViews[i].GetComponentsInChildren<Face>())
                if (face.HasValue) f.React(face.Value, seconds);
                else f.ClearReaction();
        }

        // B1: the piece grows to full size on touch-down, before any drag
        private void OnPress(int i)
        {
            if (!playing || tray[i] == null || dragging != -1) return;
            dragging = i;
            HideTutorial();
            Tween.Kill(trayViews[i]);
            trayViews[i].SetParent(playRoot, true);
            trayViews[i].SetAsLastSibling();
            Tween.Scale(trayViews[i], Vector3.one, 0.12f, Ease.OutQuad);
            SetPieceFaces(i, FaceId.Shock); // lifted into the air: not happy about it
            GameAudio.Play("pick");
        }

        // EB2: pressed and released without dragging → back to the tray
        private void OnRelease(int i)
        {
            if (dragging == i) ReturnToTray(i, false);
        }

        private bool PointerToBoard(PointerEventData e, Piece p, out int r0, out int c0)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(boardRect, e.position, e.pressEventCamera, out var local);
            local.y += DragLift + p.Height * Cell / 2f; // the piece floats above the finger
            var topLeftX = local.x - p.Width * Cell / 2f + Cell / 2f;
            var topLeftY = local.y + p.Height * Cell / 2f - Cell / 2f;
            c0 = Mathf.RoundToInt(topLeftX / Cell + (BlastBoard.Size - 1) / 2f);
            r0 = Mathf.RoundToInt(-topLeftY / Cell + (BlastBoard.Size - 1) / 2f);
            return board.CanPlace(p, r0, c0);
        }

        private void OnDrag(int i, PointerEventData e)
        {
            if (!playing || dragging != i || tray[i] == null) return;
            var p = tray[i];
            RectTransformUtility.ScreenPointToLocalPointInRectangle(playRoot, e.position, e.pressEventCamera, out var local);
            var target = local + new Vector2(0, DragLift + p.Height * Cell / 2f) - playRoot.rect.size * (trayViews[i].anchorMin - new Vector2(0.5f, 0.5f));
            trayViews[i].anchoredPosition = Vector2.Lerp(trayViews[i].anchoredPosition, target, 0.6f);
            var ok = PointerToBoard(e, p, out var r0, out var c0);

            for (int k = 0; k < ghosts.Count; k++)
            {
                var show = ok && k < p.Cells.Length;
                ghosts[k].enabled = show;
                if (!show) continue;
                ghosts[k].rectTransform.anchoredPosition = CellPos(r0 + p.Cells[k].r, c0 + p.Cells[k].c);
                ghosts[k].color = new Color(BlockColors[p.Color].r, BlockColors[p.Color].g, BlockColors[p.Color].b, 0.4f);
            }
            PreviewLines(ok ? p : null, r0, c0);
        }

        // Lines the drop would clear: glow frame + those faces go smug before the player lets go.
        private void PreviewLines(Piece p, int r0, int c0)
        {
            var now = new HashSet<BlockView>();
            int g = 0;
            if (p != null)
            {
                board.FullLines(rows, cols, p, r0, c0);
                foreach (var r in rows)
                {
                    for (int c = 0; c < BlastBoard.Size; c++) if (blocks[r, c] != null) now.Add(blocks[r, c]);
                    if (g < glows.Count) Glow(glows[g++], new Vector2(0, CellPos(r, 0).y), new Vector2(Cell * BlastBoard.Size + 10, Cell + 10));
                }
                foreach (var c in cols)
                {
                    for (int r = 0; r < BlastBoard.Size; r++) if (blocks[r, c] != null) now.Add(blocks[r, c]);
                    if (g < glows.Count) Glow(glows[g++], new Vector2(CellPos(0, c).x, 0), new Vector2(Cell + 10, Cell * BlastBoard.Size + 10));
                }
            }
            for (; g < glows.Count; g++) glows[g].enabled = false;
            foreach (var b in previewing) if (!now.Contains(b) && b.Face != null) RestoreMood(b);
            foreach (var b in now) if (!previewing.Contains(b)) b.Face.React(FaceId.Smug, -1f);
            previewing.Clear();
            previewing.UnionWith(now);
        }

        private static void Glow(Image img, Vector2 pos, Vector2 size)
        {
            img.enabled = true;
            img.rectTransform.anchoredPosition = pos;
            img.rectTransform.sizeDelta = size;
            img.transform.SetAsLastSibling();
        }

        private void OnDragEnd(int i, PointerEventData e)
        {
            HidePreview();
            if (dragging != i) return;
            if (!playing || tray[i] == null) { ReturnToTray(i, false); return; }
            var p = tray[i];
            if (!PointerToBoard(e, p, out var r0, out var c0))
            {
                ReturnToTray(i, true);
                return;
            }
            dragging = -1;
            Destroy(trayViews[i].gameObject);
            trayViews[i] = null;
            tray[i] = null;
            Place(p, r0, c0);
            if (tray[0] == null && tray[1] == null && tray[2] == null) Deal();
            RefreshTrayFits();
            if (!AnyFits()) StartCoroutine(LoseSequence());
            else SaveRun();
        }

        private void HidePreview()
        {
            foreach (var g in ghosts) g.enabled = false;
            PreviewLines(null, 0, 0);
        }

        /// <summary>Sends the dragged piece home. <paramref name="invalid"/>: bounce + "blocked" sound + light haptic (B4).</summary>
        private void ReturnToTray(int i, bool invalid)
        {
            if (dragging == i) dragging = -1;
            HidePreview();
            if (trayViews[i] == null) return;
            trayViews[i].SetParent(trayRoot, true);
            Tween.Kill(trayViews[i]);
            Tween.Anchored(trayViews[i], new Vector2(TrayX[i], 0), invalid ? 0.28f : 0.18f, invalid ? Ease.OutBack : Ease.OutCubic);
            Tween.Scale(trayViews[i], Vector3.one * TrayScaleAt(i), 0.18f, Ease.OutQuad);
            SetPieceFaces(i, null);
            if (!invalid) return;
            SetPieceFaces(i, FaceId.Meh, 0.8f);
            GameAudio.Play("blocked");
            GameAudio.Haptic();
        }

        // B15: a piece being dragged always goes home when play stops (pause, game over)
        private void CancelDrag()
        {
            if (dragging != -1) ReturnToTray(dragging, false);
        }

        private bool AnyFits()
        {
            foreach (var p in tray) if (p != null && board.FitsAnywhere(p)) return true;
            return false;
        }

        // ---------------- placing & clearing ----------------

        private void Place(Piece p, int r0, int c0)
        {
            board.Place(p, r0, c0);
            foreach (var (r, c) in p.Cells)
            {
                var b = MakeBlock(boardRect, p.Color, CellPos(r0 + r, c0 + c), Cell - 6);
                b.Rect.localScale = Vector3.one * 0.7f;
                Tween.Scale(b.Rect, Vector3.one, 0.2f, Ease.OutBack);
                b.Face.React(FaceId.Grin, 0.6f);
                blocks[r0 + r, c0 + c] = b;
            }
            GameAudio.Play("place");
            PlaceDust(p, r0, c0);
            var gained = p.Cells.Length;

            board.FullLines(rows, cols);
            var lines = rows.Count + cols.Count;
            if (lines > 0)
            {
                streak++;
                var cleared = new HashSet<(int, int)>();
                foreach (var r in rows) for (int c = 0; c < BlastBoard.Size; c++) cleared.Add((r, c));
                foreach (var c in cols) for (int r = 0; r < BlastBoard.Size; r++) cleared.Add((r, c));
                // the board data clears now (so the next move / game-over check is right); the views animate after
                var views = new List<(BlockView b, float delay)>();
                foreach (var (r, c) in cleared)
                {
                    var b = TakeCell(r, c);
                    if (b != null) views.Add((b, (Mathf.Abs(r - r0) + Mathf.Abs(c - c0)) * 0.035f));
                }
                var perfect = board.Empty();
                gained += Mathf.RoundToInt(BlastBoard.LineScore(lines) * (1f + 0.5f * (streak - 1)));
                if (perfect) gained += 300;
                GameAudio.Play("clear", 1f + 0.08f * Mathf.Min(lines, 4));
                GameAudio.Haptic();
                var praise = lines >= 4 ? Loc.T("Unbelievable!", "Không thể tin nổi!") : lines == 3 ? Loc.T("Excellent!", "Xuất sắc!") : lines == 2 ? Loc.T("Great!", "Tuyệt!") : null;
                var combo = streak > 1 ? Loc.F("Combo x{0}", "Combo x{0}", streak) + "\n" : "";
                if (praise != null) Praise(praise); // the word sits above the board; the gain rises from the drop
                FloatText(combo + "+" + gained, boardRect.TransformPoint(CellPos(r0, c0)), 60);
                // started after the text exists, so the impact frame hides it too
                if (lines >= 2) StartCoroutine(BigClear(views, perfect));
                else
                {
                    foreach (var (b, delay) in views) AnimateClear(b, delay);
                    if (perfect) GameFx.Play("Combo_Nova", boardRect.position, 1.2f);
                }
            }
            else streak = 0;

            score += gained;
            if (score > best)
            {
                best = score;
                SaveStore.SetInt("blast.best", best); // G6: kept the moment it is beaten
                SaveStore.Save();
                if (!bestToastShown && startBest > 0)
                {
                    bestToastShown = true;
                    Toast.Show(playRoot, Loc.T("New best!", "Kỷ lục mới!"));
                }
            }
            RefreshScore();
            RefreshWorry();
        }

        // B10: a crowded board makes everyone nervous
        private void RefreshWorry()
        {
            var now = board.FillRatio() > WorriedFill;
            if (now == worried) return;
            worried = now;
            foreach (var b in blocks) if (b != null) RestoreMood(b);
        }

        private void RestoreMood(BlockView b)
        {
            if (worried) b.Face.React(FaceId.Shock, -1f);
            else b.Face.ClearReaction();
        }

        private BlockView TakeCell(int r, int c)
        {
            board.Grid[r, c] = 0;
            var b = blocks[r, c];
            blocks[r, c] = null;
            return b;
        }

        private void AnimateClear(BlockView b, float delay)
        {
            var color = b.Body.color;
            b.Face.React(FaceId.Grin, 5f);
            Tween.Scale(b.Rect, Vector3.one * 1.15f, 0.1f, Ease.OutQuad, delay, () =>
            {
                GameFx.Play(GameFx.Colored("Blast_BlockPop_{color}", color), b.Rect.position, 0.8f);
                Tween.Scale(b.Rect, Vector3.zero, 0.18f, Ease.InBack, 0f, () => Destroy(b.Rect.gameObject));
            });
        }

        // 2+ lines: a two-frame impact frame (blocks as silhouettes), then a big flash, a shake and the pops.
        private IEnumerator BigClear(List<(BlockView b, float delay)> views, bool perfect)
        {
            var subjects = new HashSet<Graphic>();
            foreach (var b in blocks) if (b != null) subjects.UnionWith(b.Rect.GetComponentsInChildren<Graphic>());
            foreach (var (b, _) in views) subjects.UnionWith(b.Rect.GetComponentsInChildren<Graphic>());
            for (int i = 0; i < trayViews.Length; i++)
                if (tray[i] != null && trayViews[i] != null) subjects.UnionWith(trayViews[i].GetComponentsInChildren<Graphic>());
            if (impact == null) impact = gameObject.AddComponent<ImpactFrame>();
            yield return impact.RunUI(Camera.main, canvas, subjects);
            GameFx.Play("Blast_MultiLine", boardRect.position, 1.3f);
            if (perfect) GameFx.Play("Combo_Nova", boardRect.position, 1.4f);
            Shake(0.25f);
            foreach (var (b, delay) in views) AnimateClear(b, delay);
        }

        // soft dust puff under the piece that was just placed
        private void PlaceDust(Piece p, int r0, int c0)
        {
            var sum = Vector3.zero;
            foreach (var (r, c) in p.Cells) sum += boardRect.TransformPoint(CellPos(r0 + r, c0 + c));
            GameFx.Play("Blast_Place", sum / p.Cells.Length, 0.7f);
        }

        private void Shake(float strength)
        {
            var home = playRoot.anchoredPosition;
            Tween.Run(playRoot, 0.25f, k => playRoot.anchoredPosition = home + Random.insideUnitCircle * 22f * strength * (1f - k) * 4f,
                Ease.Linear, 0f, () => playRoot.anchoredPosition = home);
        }

        // Praise / score text rising from the drop, kept inside the screen and clear of the score (EB-mockup fixes)
        private void FloatText(string text, Vector3 worldPos, float size)
        {
            var t = UIKit.Label(playRoot, text, size, UIKit.Paper);
            t.rectTransform.sizeDelta = new Vector2(760, size * 3.6f);
            t.rectTransform.position = worldPos;
            t.outlineWidth = 0.25f;
            t.outlineColor = UIKit.Ink;
            var start = t.rectTransform.anchoredPosition;
            var half = playRoot.rect.width / 2f - 400f;
            start.x = Mathf.Clamp(start.x, -half, half);
            start.y = Mathf.Min(start.y, playRoot.rect.height / 2f - 520f);
            Tween.Run(t, 0.9f, k =>
            {
                t.rectTransform.anchoredPosition = start + new Vector2(0, 140 * k);
                t.alpha = 1f - Mathf.Max(0f, k - 0.5f) * 2f;
            }, Ease.OutCubic, 0f, () => Destroy(t.gameObject));
        }

        private void RefreshScore()
        {
            var beaten = score > startBest && startBest > 0;
            bestText.text = beaten ? Loc.T("New best!", "Kỷ lục mới!") : Mathf.Max(best, score).ToString();
            var gold = UIKit.Hex("#FFD23F");
            bestText.color = beaten ? gold : UIKit.Hex("#B9BCE0");
            scoreText.color = beaten ? gold : UIKit.Paper;
            // the star sits just left of the centred best value, whatever its length (EB12: 7 digits never overlap)
            bestStar.rectTransform.anchoredPosition = new Vector2(30f - bestText.GetPreferredValues(bestText.text).x / 2f - 36f, -222f);
        }

        // M9-style count-up for the score
        private void LateUpdate()
        {
            if (scoreText == null || shownScore == score) return;
            var step = Mathf.Max(1, Mathf.CeilToInt((score - shownScore) * Mathf.Min(1f, Time.deltaTime * 10f)));
            shownScore = Mathf.Min(score, shownScore + step);
            scoreText.text = shownScore.ToString();
            if (shownScore == score) Tween.Punch(scoreText.transform, 0.12f, 0.2f);
        }

        // ---------------- tutorial ----------------

        private void ShowTutorial()
        {
            tutorialHand = UIKit.Hand(playRoot, new Vector2(0.5f, 0f), new Vector2(20, TrayY + 10));
            var from = tutorialHand.anchoredPosition;
            var to = from + new Vector2(0, 700);
            Tween.Run(tutorialHand, 60f, k =>
            {
                var t = Mathf.Repeat(k * 60f / 1.6f, 1f);
                tutorialHand.anchoredPosition = Vector2.Lerp(from, to, Tween.Evaluate(Ease.InOutSine, Mathf.Clamp01(t * 1.3f)));
            }, Ease.Linear);
            tutorialText = UIKit.Label(playRoot, Loc.T("Drag a block onto the board", "Kéo một khối lên bàn"), 44, new Vector2(0.5f, 0f), new Vector2(0, TrayY + 168), new Vector2(900, 70), UIKit.Paper);
        }

        private void HideTutorial()
        {
            if (tutorialHand == null) return;
            Destroy(tutorialHand.gameObject);
            tutorialHand = null;
            if (tutorialText != null) Destroy(tutorialText.gameObject);
            SaveStore.SetBool("blast.tutorial", true);
            SaveStore.Save();
        }

        // ---------------- end / pause ----------------

        // B11: the stuck pieces shake, the board greys row by row from the bottom, faces cry, then the card
        private IEnumerator LoseSequence()
        {
            playing = false;
            SaveStore.Delete(RunKey);
            CancelDrag();
            GameAudio.Play("lose");
            for (int i = 0; i < 3; i++)
            {
                if (trayViews[i] == null) continue;
                var v = trayViews[i];
                var home = v.anchoredPosition;
                Tween.Run(v, 0.4f, k => v.anchoredPosition = home + new Vector2(Mathf.Sin(k * Mathf.PI * 8f) * 18f * (1f - k), 0f), Ease.Linear, 0f, () => v.anchoredPosition = home);
            }
            yield return new WaitForSeconds(0.35f);
            for (int r = BlastBoard.Size - 1; r >= 0; r--)
            {
                for (int c = 0; c < BlastBoard.Size; c++)
                {
                    var b = blocks[r, c];
                    if (b == null) continue;
                    Tween.Color(b.Body, Color.Lerp(b.Body.color, GreyBlock, 0.75f), 0.2f);
                    b.Face.React(FaceId.Cry, -1f);
                }
                yield return new WaitForSeconds(0.075f);
            }
            best = SaveStore.SubmitBest("blast.best", score);
            if (score > startBest && score > 0) ReviewPrompt.GoodMoment();
            yield return new WaitForSeconds(0.25f);
            ShowGameOver();
        }

        private void ShowGameOver()
        {
            var newBest = score > startBest && score > 0;
            var p = Popup.Open(root, Loc.T("No room left!", "Hết chỗ rồi!"), 1050);
            p.Text(score.ToString(), 140, UIKit.Ink, 160);
            p.Text(newBest ? Loc.T("New best!", "Kỷ lục mới!") : Loc.F("Best {0}", "Kỷ lục {0}", best), 50, newBest ? UIKit.Hex("#E9A23B") : UIKit.Muted);
            p.Space(10);
            if (!revived) RewardedButton.Add(p, Loc.T("Revive", "Hồi sinh"), "blast_revive", Revive);
            p.Button("btn_green", Loc.T("Play again", "Chơi lại"), () => p.Close(() => Ads.OnBreak("blast_gameover", NewGame)), "icon_restart");
            p.Fit();
        }

        // B12: clears the three fullest rows/columns and deals a fresh tray.
        private void Revive()
        {
            revived = true;
            var cells = new HashSet<(int, int)>();
            foreach (var (isRow, index) in board.FullestLines(3))
                for (int j = 0; j < BlastBoard.Size; j++) cells.Add(isRow ? (index, j) : (j, index));
            foreach (var (r, c) in cells)
            {
                var b = TakeCell(r, c);
                if (b != null) AnimateClear(b, (r + c) * 0.02f);
            }
            foreach (var b in blocks)
            {
                if (b == null) continue;
                b.Body.color = BlockColors[ColorOf(b)];
                b.Face.ClearReaction();
            }
            for (int i = 0; i < 3; i++)
            {
                if (trayViews[i] != null) Destroy(trayViews[i].gameObject);
                tray[i] = null;
                trayViews[i] = null;
            }
            worried = false;
            RefreshWorry();
            Deal();
            playing = true;
        }

        private int ColorOf(BlockView b)
        {
            for (int r = 0; r < BlastBoard.Size; r++)
                for (int c = 0; c < BlastBoard.Size; c++)
                    if (blocks[r, c] == b) return board.Grid[r, c];
            return 1;
        }

        private void OpenPause()
        {
            if (!playing) return;
            CancelDrag();
            playing = false;
            paused = true;
            SaveRun();
            SettingsPopup.Show(root, () => { paused = false; playing = true; },
                (Loc.T("Play again", "Chơi lại"), "btn_green", "icon_restart", NewGame));
        }

        /// <summary>Forwards the pointer events of one tray slot. Only the first finger counts (G14 / EB4).</summary>
        private class DragZone : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            private static int owner = int.MinValue;
            public System.Action Pressed, Began, Released;
            public System.Action<PointerEventData> Moved, Ended;
            private bool dragged;

            public void OnPointerDown(PointerEventData e)
            {
                if (owner != int.MinValue) return;
                owner = e.pointerId;
                dragged = false;
                Pressed?.Invoke();
            }

            public void OnBeginDrag(PointerEventData e)
            {
                if (e.pointerId != owner) return;
                dragged = true;
                Began?.Invoke();
            }

            public void OnDrag(PointerEventData e)
            {
                if (e.pointerId == owner) Moved?.Invoke(e);
            }

            public void OnEndDrag(PointerEventData e)
            {
                if (e.pointerId != owner) return;
                owner = int.MinValue;
                Ended?.Invoke(e);
            }

            public void OnPointerUp(PointerEventData e)
            {
                if (e.pointerId != owner || dragged) return;
                owner = int.MinValue;
                Released?.Invoke();
            }

            private void OnDisable()
            {
                owner = int.MinValue;
            }
        }
    }
}
