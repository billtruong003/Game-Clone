using System;
using System.Collections;
using System.Collections.Generic;
using CasualGame.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CasualGame.ArrowOut
{
    /// <summary>
    /// Draws a <see cref="Board"/> with the white arrow tiles (tinted per color) and turns taps into arrow picks.
    /// Snakes are rebuilt from 5 tiles: head / body / corner / tail / single, authored pointing up.
    /// </summary>
    public class ArrowBoardView : MonoBehaviour, IPointerClickHandler
    {
        public static readonly Color[] Palette =
        {
            UIKit.Hex("#2E3A59"), UIKit.Hex("#2A9D8F"), UIKit.Hex("#8E5BB5"), UIKit.Hex("#E9A23B"),
        };
        public static readonly Color ErrorColor = UIKit.Hex("#E76F51");
        private static readonly Color GridColor = UIKit.Hex("#D9D2C5");
        private const float StepSeconds = 0.028f;

        public event Action<Arrow> Tapped;
        public float Cell { get; private set; }

        private Board board;
        private RectTransform layer;
        private readonly Dictionary<int, List<Image>> views = new();
        private Arrow hinted;

        public static ArrowBoardView Create(Transform parent, Vector2 offset, float cell)
        {
            var rt = UIKit.Place(UIKit.Rect("Board", parent), new Vector2(0.5f, 0.5f), offset, new Vector2(Board.Cols * cell, Board.Rows * cell));
            UIKit.AddImage(rt, (Sprite)null, new Color(1, 1, 1, 0)).raycastTarget = true; // catches taps between arrows
            var view = rt.gameObject.AddComponent<ArrowBoardView>();
            view.Cell = cell;
            return view;
        }

        public void Show(Board b)
        {
            board = b;
            views.Clear();
            hinted = null;
            foreach (Transform child in transform) Destroy(child.gameObject);
            for (int r = 0; r < Board.Rows; r++)
                for (int c = 0; c < Board.Cols; c++)
                    if (b.Mask == null || b.Mask[r, c])
                        UIKit.Image(transform, "grid_dot", new Vector2(0.5f, 0.5f), CellPos(new Pos(r, c)), new Vector2(32, 32), GridColor);
            layer = UIKit.Stretch(UIKit.Rect("Arrows", transform));
            int i = 0;
            foreach (var a in b.Arrows.Values) AddArrow(a, true, 0.02f * i++);
        }

        public Vector2 CellPos(Pos p) => new((p.C - (Board.Cols - 1) / 2f) * Cell, -(p.R - (Board.Rows - 1) / 2f) * Cell);

        public Vector3 WorldPos(Pos p) => transform.TransformPoint(CellPos(p));

        public void AddArrow(Arrow a, bool pop, float delay = 0f)
        {
            var imgs = new List<Image>(a.Cells.Length);
            for (int i = 0; i < a.Cells.Length; i++)
            {
                // +3 px overlap hides the bilinear seam where two tiles meet.
                var img = UIKit.Image(layer, null, new Vector2(0.5f, 0.5f), CellPos(a.Cells[i]), new Vector2(Cell + 3, Cell + 3), Palette[a.Color]);
                imgs.Add(img);
                if (pop)
                {
                    img.transform.localScale = Vector3.zero;
                    Tween.Scale(img.transform, Vector3.one, 0.22f, Ease.OutBack, delay);
                }
            }
            views[a.Id] = imgs;
            ApplyTiles(a.Cells, a.Dir, imgs);
        }

        private static void ApplyTiles(Pos[] cells, int dir, List<Image> imgs)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                var (tile, angle) = TileFor(cells, i, dir);
                imgs[i].sprite = ArtLibrary.Instance.Get(tile);
                imgs[i].rectTransform.localEulerAngles = new Vector3(0, 0, -angle);
            }
        }

        private static int DirBetween(Pos p, Pos q)
        {
            int dr = q.R - p.R, dc = q.C - p.C;
            return dr == -1 ? 0 : dc == 1 ? 1 : dr == 1 ? 2 : 3;
        }

        /// <summary>Which tile draws cell i of a snake, and its clockwise rotation.</summary>
        public static (string tile, float angle) TileFor(Pos[] cells, int i, int dir)
        {
            if (cells.Length == 1) return ("arrow_single", dir * 90);
            if (i == 0) return ("arrow_head", dir * 90);
            var a = DirBetween(cells[i], cells[i - 1]);
            if (i == cells.Length - 1) return ("arrow_tail", a * 90);
            var b = DirBetween(cells[i], cells[i + 1]);
            if ((a + 2) % 4 == b) return ("arrow_body", a % 2 == 0 ? 0 : 90);
            for (int k = 0; k < 4; k++)
            {
                int x = (2 + k) % 4, y = (1 + k) % 4;
                if ((a == x && b == y) || (a == y && b == x)) return ("arrow_corner", k * 90);
            }
            return ("arrow_body", 0);
        }

        /// <summary>Slides the arrow out along its own body and fades pieces that leave the board.</summary>
        public void AnimateExit(Arrow a)
        {
            if (!views.TryGetValue(a.Id, out var imgs)) return;
            views.Remove(a.Id);
            if (hinted == a) hinted = null;
            foreach (var img in imgs) Tween.Kill(img.transform);
            StartCoroutine(Slide(a, imgs));
        }

        private IEnumerator Slide(Arrow a, List<Image> imgs)
        {
            var wait = new WaitForSeconds(StepSeconds);
            int steps = Mathf.Max(Board.Rows, Board.Cols) + a.Cells.Length;
            for (int step = 1; step <= steps; step++)
            {
                var cells = Board.Slide(a, step);
                ApplyTiles(cells, a.Dir, imgs);
                for (int i = 0; i < cells.Length; i++)
                {
                    imgs[i].rectTransform.anchoredPosition = CellPos(cells[i]);
                    imgs[i].enabled = Board.Inside(cells[i].R, cells[i].C);
                }
                yield return wait;
            }
            foreach (var img in imgs) Destroy(img.gameObject);
        }

        public void Shake(Arrow a)
        {
            if (!views.TryGetValue(a.Id, out var imgs)) return;
            foreach (var img in imgs)
            {
                img.color = ErrorColor;
                var home = img.rectTransform.anchoredPosition;
                Tween.Kill(img.transform);
                Tween.Run(img.transform, 0.32f, k => img.rectTransform.anchoredPosition = home + new Vector2(Mathf.Sin(k * Mathf.PI * 6f) * 14f * (1f - k), 0f),
                    Ease.Linear, 0f, () =>
                    {
                        img.rectTransform.anchoredPosition = home;
                        img.color = Palette[a.Color];
                    });
            }
        }

        public void Hint(Arrow a)
        {
            ClearHint();
            if (!views.TryGetValue(a.Id, out var imgs)) return;
            hinted = a;
            foreach (var img in imgs)
            {
                var t = img.transform;
                Tween.Run(t, 3f, k => t.localScale = Vector3.one * (1f + 0.12f * Mathf.Abs(Mathf.Sin(k * Mathf.PI * 6f))), Ease.Linear, 0f, () => t.localScale = Vector3.one);
            }
        }

        public void ClearHint()
        {
            if (hinted == null || !views.TryGetValue(hinted.Id, out var imgs)) { hinted = null; return; }
            foreach (var img in imgs)
            {
                Tween.Kill(img.transform);
                img.transform.localScale = Vector3.one;
            }
            hinted = null;
        }

        public void RemoveInstant(Arrow a)
        {
            if (!views.TryGetValue(a.Id, out var imgs)) return;
            views.Remove(a.Id);
            foreach (var img in imgs) Tween.Scale(img.transform, Vector3.zero, 0.2f, Ease.InBack, 0f, () => Destroy(img.gameObject));
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (board == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, e.position, e.pressEventCamera, out var local);
            int c = Mathf.FloorToInt(local.x / Cell + Board.Cols / 2f);
            int r = Mathf.FloorToInt(-local.y / Cell + Board.Rows / 2f);
            if (!Board.Inside(r, c)) return;
            var id = board.Grid[r, c];
            if (id != -1) Tapped?.Invoke(board.Arrows[id]);
        }
    }
}
