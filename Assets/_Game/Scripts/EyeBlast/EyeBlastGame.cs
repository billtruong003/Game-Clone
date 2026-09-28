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
    /// Eye Blast: drag pieces onto an 8×8 board; full rows/columns clear. Every block is an emoji character.
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
        private static readonly Color SlotColor = UIKit.Hex("#232748");
        private const float Cell = 116f;
        private const float TrayScale = 0.52f;
        private const float DragLift = 230f;
        private static readonly float[] TrayX = { -330f, 0f, 330f };
        private const float TrayY = 330f;

        private Canvas canvas;
        private RectTransform root, boardRect, trayRoot, playRoot;
        private readonly BlastBoard board = new();
        private readonly BlockView[,] blocks = new BlockView[BlastBoard.Size, BlastBoard.Size];
        private readonly Piece[] tray = new Piece[3];
        private readonly RectTransform[] trayViews = new RectTransform[3];
        private ImpactFrame impact;
        private readonly List<Image> ghosts = new();
        private readonly List<int> rows = new(), cols = new();
        private readonly HashSet<BlockView> previewing = new();
        private TextMeshProUGUI scoreText, bestText;
        private int score, best, streak;
        private bool playing, revived;

        /// <summary>One block on screen: tinted body + outline + animated emoji face.</summary>
        private class BlockView
        {
            public RectTransform Rect;
            public EmojiFace Face;
        }

        private void Start()
        {
            var cam = Camera.main;
            cam.backgroundColor = Background;
            canvas = UIKit.CreateCameraCanvas("EyeBlastUI", cam);
            root = UIKit.Stretch(UIKit.Rect("Safe", canvas.transform));
            root.gameObject.AddComponent<SafeArea>();
            NewGame();
            GameAudio.PlayMusic("music_blast");
        }

        private void NewGame()
        {
            if (playRoot != null) Destroy(playRoot.gameObject);
            System.Array.Clear(board.Grid, 0, board.Grid.Length);
            System.Array.Clear(blocks, 0, blocks.Length);
            ghosts.Clear();
            previewing.Clear();
            score = streak = 0;
            revived = false;
            best = SaveStore.GetInt("blast.best");

            playRoot = UIKit.Stretch(UIKit.Rect("Play", root));
            var top = new Vector2(0.5f, 1f);
            scoreText = UIKit.Label(playRoot, "0", 120, top, new Vector2(0, -130), new Vector2(700, 150), UIKit.Paper);
            bestText = UIKit.Label(playRoot, "", 46, top, new Vector2(0, -225), new Vector2(700, 70), UIKit.Hex("#AEB3D9"));
            UIKit.Image(playRoot, "icon_star", top, new Vector2(-110, -222), new Vector2(60, 60));
            UIKit.IconButton(playRoot, "round_white", "icon_pause", OpenPause, new Vector2(1f, 1f), new Vector2(-100, -100), 116);
            if (Application.CanStreamedLevelBeLoaded("Hub"))
                UIKit.IconButton(playRoot, "round_white", "icon_home", () => SceneFlow.Load("Hub"), new Vector2(0f, 1f), new Vector2(100, -100), 116);

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

            trayRoot = UIKit.Place(UIKit.Rect("Tray", playRoot), new Vector2(0.5f, 0f), new Vector2(0, TrayY), new Vector2(1000, 360));
            for (int i = 0; i < 3; i++)
            {
                var zone = UIKit.Place(UIKit.Rect("Slot" + i, trayRoot), new Vector2(0.5f, 0.5f), new Vector2(TrayX[i], 0), new Vector2(320, 360));
                UIKit.AddImage(zone, (Sprite)null, new Color(1, 1, 1, 0)).raycastTarget = true;
                var drag = zone.gameObject.AddComponent<DragZone>();
                int idx = i;
                drag.Began = () => OnDragBegin(idx);
                drag.Moved = e => OnDrag(idx, e);
                drag.Ended = e => OnDragEnd(idx, e);
            }

            RefreshScore();
            Deal();
            playing = true;
        }

        private static Vector2 CellPos(int r, int c) =>
            new((c - (BlastBoard.Size - 1) / 2f) * Cell, -(r - (BlastBoard.Size - 1) / 2f) * Cell);

        private BlockView MakeBlock(Transform parent, int color, Vector2 pos, float size)
        {
            var rt = UIKit.Place(UIKit.Rect("Block", parent), new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            UIKit.AddImage(rt, "block_fill", BlockColors[color]);
            UIKit.Image(rt, "block_line", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            var faceImg = UIKit.Image(rt, null, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.81f, size * 0.81f));
            var face = faceImg.gameObject.AddComponent<EmojiFace>();
            face.SetRandom();
            return new BlockView { Rect = rt, Face = face };
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
        }

        private RectTransform BuildPieceView(Piece p, int slot)
        {
            var rt = UIKit.Place(UIKit.Rect("Piece", trayRoot), new Vector2(0.5f, 0.5f), new Vector2(TrayX[slot], 0), new Vector2(p.Width * Cell, p.Height * Cell));
            foreach (var (r, c) in p.Cells)
                MakeBlock(rt, p.Color, new Vector2((c - (p.Width - 1) / 2f) * Cell, -(r - (p.Height - 1) / 2f) * Cell), Cell - 6);
            rt.localScale = Vector3.zero;
            Tween.Scale(rt, Vector3.one * TrayScale, 0.3f, Ease.OutBack, 0.06f * slot);
            return rt;
        }

        private void OnDragBegin(int i)
        {
            if (!playing || tray[i] == null) return;
            Tween.Kill(trayViews[i]);
            trayViews[i].SetParent(playRoot, true);
            trayViews[i].SetAsLastSibling();
            Tween.Scale(trayViews[i], Vector3.one, 0.12f, Ease.OutQuad);
            GameAudio.Play("pick");
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
            if (!playing || tray[i] == null) return;
            var p = tray[i];
            RectTransformUtility.ScreenPointToLocalPointInRectangle(playRoot, e.position, e.pressEventCamera, out var local);
            trayViews[i].anchoredPosition = local + new Vector2(0, DragLift + p.Height * Cell / 2f) - playRoot.rect.size * (trayViews[i].anchorMin - new Vector2(0.5f, 0.5f));
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

        // Faces on lines that the drop would clear light up ("starstruck") before the player lets go.
        private void PreviewLines(Piece p, int r0, int c0)
        {
            var now = new HashSet<BlockView>();
            if (p != null)
            {
                board.FullLines(rows, cols, p, r0, c0);
                foreach (var r in rows) for (int c = 0; c < BlastBoard.Size; c++) if (blocks[r, c] != null) now.Add(blocks[r, c]);
                foreach (var c in cols) for (int r = 0; r < BlastBoard.Size; r++) if (blocks[r, c] != null) now.Add(blocks[r, c]);
            }
            foreach (var b in previewing) if (!now.Contains(b) && b.Face != null) b.Face.ClearReaction();
            foreach (var b in now) if (!previewing.Contains(b)) b.Face.React("starstruck", -1f);
            previewing.Clear();
            previewing.UnionWith(now);
        }

        private void OnDragEnd(int i, PointerEventData e)
        {
            foreach (var g in ghosts) g.enabled = false;
            PreviewLines(null, 0, 0);
            if (!playing || tray[i] == null) return;
            var p = tray[i];
            if (!PointerToBoard(e, p, out var r0, out var c0))
            {
                trayViews[i].SetParent(trayRoot, true);
                Tween.Anchored(trayViews[i], new Vector2(TrayX[i], 0), 0.18f);
                Tween.Scale(trayViews[i], Vector3.one * TrayScale, 0.18f, Ease.OutQuad);
                return;
            }
            Destroy(trayViews[i].gameObject);
            tray[i] = null;
            Place(p, r0, c0);
            if (tray[0] == null && tray[1] == null && tray[2] == null) Deal();
            if (!AnyFits()) GameOver();
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
                Toast(streak > 1 ? $"Combo x{streak}\n+{gained}" : $"+{gained}", boardRect.TransformPoint(CellPos(r0, c0)));
                // started after the toast exists, so the impact frame hides it too
                if (lines >= 2) StartCoroutine(BigClear(views, perfect));
                else
                {
                    foreach (var (b, delay) in views) AnimateClear(b, delay);
                    if (perfect) GameFx.Play("Combo_Nova", boardRect.position, 1.2f);
                }
            }
            else streak = 0;

            score += gained;
            RefreshScore();
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
            var color = b.Rect.GetComponent<Image>().color;
            b.Face.React("laugh", 5f);
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
            foreach (var t in trayViews) if (t != null) subjects.UnionWith(t.GetComponentsInChildren<Graphic>());
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

        private void Toast(string text, Vector3 worldPos)
        {
            var t = UIKit.Label(playRoot, text, 64, UIKit.Paper);
            t.rectTransform.sizeDelta = new Vector2(600, 180);
            t.rectTransform.position = worldPos;
            t.outlineWidth = 0.25f;
            t.outlineColor = UIKit.Ink;
            var start = t.rectTransform.anchoredPosition;
            Tween.Run(t, 0.9f, k =>
            {
                t.rectTransform.anchoredPosition = start + new Vector2(0, 160 * k);
                t.alpha = 1f - Mathf.Max(0f, k - 0.5f) * 2f;
            }, Ease.OutCubic, 0f, () => Destroy(t.gameObject));
        }

        private void RefreshScore()
        {
            scoreText.text = score.ToString();
            bestText.text = Mathf.Max(best, score).ToString();
        }

        // ---------------- end / pause ----------------

        private void GameOver()
        {
            playing = false;
            GameAudio.Play("lose");
            foreach (var b in blocks) b?.Face.React("cry", -1f);
            best = SaveStore.SubmitBest("blast.best", score);
            Tween.Delay(this, 0.7f, () =>
            {
                Popup p = null;
                p = Popup.Open(root, "Hết chỗ rồi!", 1050);
                p.Text(score.ToString(), 140, UIKit.Ink, 160);
                p.Text(score >= best && score > 0 ? "Kỷ lục mới!" : $"Kỷ lục {best}", 50, UIKit.Muted);
                p.Space(10);
                if (!revived)
                    p.Button("btn_blue", "Hồi sinh", () => p.Close(() => Ads.ShowRewarded("blast_revive", ok => { if (ok) Revive(); })), "icon_ad");
                p.Button("btn_green", "Chơi lại", () => p.Close(() => Ads.OnBreak("blast_gameover", NewGame)), "icon_restart");
                p.Fit();
            });
        }

        // Clears the middle 4×4 and deals three fresh pieces (at least one always fits).
        private void Revive()
        {
            revived = true;
            for (int r = 2; r < 6; r++)
                for (int c = 2; c < 6; c++)
                {
                    var b = TakeCell(r, c);
                    if (b != null) AnimateClear(b, (r + c) * 0.02f);
                }
            foreach (var b in blocks) b?.Face.ClearReaction();
            for (int i = 0; i < 3; i++)
            {
                if (trayViews[i] != null) Destroy(trayViews[i].gameObject);
                tray[i] = null;
            }
            Deal();
            playing = true;
        }

        private void OpenPause()
        {
            if (!playing) return;
            playing = false;
            SettingsPopup.Show(root, () => playing = true,
                ("Chơi lại", "btn_green", "icon_restart", NewGame),
                ("Về menu", "btn_white", "icon_home", () => SceneFlow.Load(Application.CanStreamedLevelBeLoaded("Hub") ? "Hub" : "EyeBlast")));
        }

        /// <summary>Forwards drag events of one tray slot.</summary>
        private class DragZone : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            public System.Action Began;
            public System.Action<PointerEventData> Moved, Ended;
            public void OnBeginDrag(PointerEventData e) => Began?.Invoke();
            public void OnDrag(PointerEventData e) => Moved?.Invoke(e);
            public void OnEndDrag(PointerEventData e) => Ended?.Invoke(e);
        }
    }
}
