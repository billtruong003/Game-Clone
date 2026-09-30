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
    /// Every arrow wears a deadpan face on its round tail, so it gets yanked out backwards when it flies (panic).
    /// </summary>
    public class ArrowBoardView : MonoBehaviour, IPointerClickHandler
    {
        // A15: the four colours differ in brightness too (dark navy, mid purple, mid-light teal, light amber)
        public static readonly Color[] Palette =
        {
            UIKit.Hex("#2E3A59"), UIKit.Hex("#35B09F"), UIKit.Hex("#7B4FAE"), UIKit.Hex("#EDA93C"),
        };
        // error red far from the amber arrows
        public static readonly Color ErrorColor = UIKit.Hex("#D6334A");
        private static readonly Color GridColor = UIKit.Hex("#D9D2C5");
        private const float StepSeconds = 0.028f;
        private const float InkTrailTime = 0.45f, InkWidth = 0.3f; // ink width as a fraction of a cell
        private const float CapSize = 0.6f, FaceSize = 0.44f; // fractions of a cell
        private static Material inkMaterial;

        public event Action<Arrow> Tapped;
        public float Cell { get; private set; }

        private class View
        {
            public List<Image> Tiles;
            public Image Cap;
            public Face Face;
        }

        private Board board;
        private RectTransform layer, hintLayer;
        private readonly Dictionary<int, View> views = new();
        private readonly List<GameObject> inks = new();
        private readonly List<Image> hintDots = new();
        private Arrow hinted;

        public Arrow Hinted => hinted;

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
            hintDots.Clear();
            hinted = null;
            foreach (Transform child in transform) Destroy(child.gameObject);
            for (int r = 0; r < Board.Rows; r++)
                for (int c = 0; c < Board.Cols; c++)
                    if (b.Mask == null || b.Mask[r, c])
                        UIKit.Image(transform, "grid_dot", new Vector2(0.5f, 0.5f), CellPos(new Pos(r, c)), new Vector2(32, 32), GridColor);
            hintLayer = UIKit.Stretch(UIKit.Rect("Hint", transform));
            layer = UIKit.Stretch(UIKit.Rect("Arrows", transform));
            int i = 0;
            foreach (var a in b.Arrows.Values) AddArrow(a, true, 0.02f * i++);
        }

        public Vector2 CellPos(Pos p) => new((p.C - (Board.Cols - 1) / 2f) * Cell, -(p.R - (Board.Rows - 1) / 2f) * Cell);

        public Vector3 WorldPos(Pos p) => transform.TransformPoint(CellPos(p));

        public void AddArrow(Arrow a, bool pop, float delay = 0f)
        {
            var v = new View { Tiles = new List<Image>(a.Cells.Length) };
            for (int i = 0; i < a.Cells.Length; i++)
            {
                // +3 px overlap hides the bilinear seam where two tiles meet.
                var img = UIKit.Image(layer, null, new Vector2(0.5f, 0.5f), CellPos(a.Cells[i]), new Vector2(Cell + 3, Cell + 3), Palette[a.Color]);
                v.Tiles.Add(img);
            }
            // a one-cell arrow shares its cell with the chevron: a smaller cap, pushed further back
            var capScale = a.Cells.Length == 1 ? 0.8f : 1f;
            v.Cap = UIKit.Image(layer, "dot", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Cell * CapSize, Cell * CapSize) * capScale, Palette[a.Color]);
            v.Face = Face.AddUI(v.Cap.transform, new Vector2(Cell * FaceSize, Cell * FaceSize) * capScale, new Vector2(0, Cell * 0.02f));
            views[a.Id] = v;
            ApplyTiles(a.Cells, a.Dir, v);
            if (!pop) return;
            foreach (var img in v.Tiles) PopIn(img.transform, delay);
            PopIn(v.Cap.transform, delay);
        }

        private static void PopIn(Transform t, float delay)
        {
            t.localScale = Vector3.zero;
            Tween.Scale(t, Vector3.one, 0.22f, Ease.OutBack, delay);
        }

        private void ApplyTiles(Pos[] cells, int dir, View v)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                var (tile, angle) = TileFor(cells, i, dir);
                v.Tiles[i].sprite = ArtLibrary.Instance.Get(tile);
                v.Tiles[i].rectTransform.localEulerAngles = new Vector3(0, 0, -angle);
            }
            PlaceCap(cells, dir, v);
        }

        // The face sits on the round end: the tail cell's centre, or the back of a one-cell arrow.
        private void PlaceCap(Pos[] cells, int dir, View v)
        {
            var tail = v.Tiles[^1].rectTransform.anchoredPosition;
            if (cells.Length == 1) tail -= new Vector2(Board.DX[dir], -Board.DY[dir]) * Cell * 0.38f;
            v.Cap.rectTransform.anchoredPosition = tail;
            v.Cap.enabled = v.Tiles[^1].enabled;
            v.Face.gameObject.SetActive(v.Cap.enabled);
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

        // Stops any running tween on the arrow and puts it back at full size where it belongs (A3: tapping an arrow
        // that is still growing in used to freeze it small).
        private void Settle(Arrow a, View v)
        {
            for (int i = 0; i < v.Tiles.Count; i++)
            {
                Tween.Kill(v.Tiles[i].transform);
                v.Tiles[i].transform.localScale = Vector3.one;
                v.Tiles[i].rectTransform.anchoredPosition = CellPos(a.Cells[i]);
                v.Tiles[i].color = Palette[a.Color];
            }
            Tween.Kill(v.Cap.transform);
            v.Cap.transform.localScale = Vector3.one;
            v.Cap.color = Palette[a.Color];
            PlaceCap(a.Cells, a.Dir, v);
        }

        /// <summary>Slides the arrow out along its own body and fades pieces that leave the board.</summary>
        public void AnimateExit(Arrow a)
        {
            if (!views.TryGetValue(a.Id, out var v)) return;
            if (hinted == a) ClearHint();
            views.Remove(a.Id);
            Settle(a, v);
            v.Face.React(FaceId.Panic, -1f);
            StartCoroutine(Slide(a, v));
        }

        private IEnumerator Slide(Arrow a, View v)
        {
            var wait = new WaitForSeconds(StepSeconds);
            int steps = Mathf.Max(Board.Rows, Board.Cols) + a.Cells.Length;
            var ink = CreateInkTrail(a, v.Tiles[^1].transform.position);
            bool puffed = false;
            for (int step = 1; step <= steps; step++)
            {
                var cells = Board.Slide(a, step);
                for (int i = 0; i < cells.Length; i++)
                {
                    v.Tiles[i].rectTransform.anchoredPosition = CellPos(cells[i]);
                    v.Tiles[i].enabled = Board.Inside(cells[i].R, cells[i].C);
                }
                ApplyTiles(cells, a.Dir, v);
                // the ink is laid by the TAIL, so it never covers the arrow's own body
                ink.transform.position = v.Tiles[^1].transform.position;
                if (!puffed && !Board.Inside(cells[0].R, cells[0].C))
                {
                    puffed = true;
                    var edge = (WorldPos(Board.Slide(a, step - 1)[0]) + WorldPos(cells[0])) * 0.5f;
                    GameFx.Play("Land_Poof", edge, WorldCell() * 0.35f);
                }
                yield return wait;
            }
            foreach (var img in v.Tiles) Destroy(img.gameObject);
            Destroy(v.Cap.gameObject);
            ink.emitting = false;
            inks.Remove(ink.gameObject);
            Destroy(ink.gameObject, InkTrailTime + 0.1f);
        }

        private float WorldCell() => (WorldPos(new Pos(0, 1)) - WorldPos(new Pos(0, 0))).magnitude;

        // Ink brush stroke (InkBrush shader): bristles along the stroke, dries out from the tail end.
        private TrailRenderer CreateInkTrail(Arrow a, Vector3 start)
        {
            if (inkMaterial == null)
            {
                inkMaterial = new Material(Shader.Find("CasualGame/InkBrush"));
                inkMaterial.SetFloat("_Length", WorldCell() * 6f);
            }
            var go = new GameObject("Ink");
            go.transform.position = start;
            inks.Add(go);
            var trail = go.AddComponent<TrailRenderer>();
            trail.sharedMaterial = inkMaterial;
            trail.time = InkTrailTime;
            trail.widthMultiplier = WorldCell() * InkWidth;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.2f);
            trail.textureMode = LineTextureMode.Stretch;
            trail.minVertexDistance = 0.04f;
            trail.numCornerVertices = 3;
            trail.numCapVertices = 2;
            trail.startColor = trail.endColor = Palette[a.Color];
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                trail.sortingLayerID = canvas.sortingLayerID;
                trail.sortingOrder = canvas.sortingOrder + 1; // over the paper card
            }
            return trail;
        }

        // AO7: leaving the screen mid-flight must not leave ink strokes behind
        private void OnDestroy()
        {
            foreach (var go in inks) if (go != null) Destroy(go);
            inks.Clear();
        }

        /// <summary>
        /// A2 blocked tap: the arrow lunges forward until it bumps the blocker, springs back, the blocker flashes white.
        /// </summary>
        public void Lunge(Arrow a, Arrow blocker, bool costsHeart)
        {
            if (!views.TryGetValue(a.Id, out var v)) return;
            Settle(a, v);
            v.Face.React(FaceId.Shock, 0.9f);
            // distance to the blocker's first cell along the head's path (in cells, minus a small gap)
            int gap = 0;
            for (int r = a.Head.R + Board.DY[a.Dir], c = a.Head.C + Board.DX[a.Dir]; Board.Inside(r, c) && board.Grid[r, c] != blocker.Id; r += Board.DY[a.Dir], c += Board.DX[a.Dir]) gap++;
            var push = new Vector2(Board.DX[a.Dir], -Board.DY[a.Dir]) * Cell * (gap + 0.3f);
            var homes = new Vector2[v.Tiles.Count];
            for (int i = 0; i < homes.Length; i++) homes[i] = CellPos(a.Cells[i]);
            var capHome = v.Cap.rectTransform.anchoredPosition;
            var tint = costsHeart ? ErrorColor : Palette[a.Color];
            Tween.Run(v.Tiles[0].transform, 0.34f, k =>
            {
                // out fast, back with a little spring
                var t = k < 0.35f ? Tween.Evaluate(Ease.OutQuad, k / 0.35f) : 1f - Tween.Evaluate(Ease.OutBack, (k - 0.35f) / 0.65f);
                for (int i = 0; i < homes.Length; i++)
                {
                    v.Tiles[i].rectTransform.anchoredPosition = homes[i] + push * t;
                    v.Tiles[i].color = Color.Lerp(Palette[a.Color], tint, Mathf.Sin(k * Mathf.PI));
                }
                v.Cap.rectTransform.anchoredPosition = capHome + push * t;
                v.Cap.color = v.Tiles[0].color;
            }, Ease.Linear, 0f, () => { if (views.ContainsKey(a.Id)) Settle(a, v); });

            if (!views.TryGetValue(blocker.Id, out var b)) return;
            b.Face.React(FaceId.Meh, 0.9f);
            Tween.Run(b.Cap.transform, 0.3f, k =>
            {
                var flash = k < 0.12f ? 0f : Mathf.Sin((k - 0.12f) / 0.88f * Mathf.PI);
                foreach (var img in b.Tiles) img.color = Color.Lerp(Palette[blocker.Color], Color.white, flash * 0.85f);
                b.Cap.color = b.Tiles[0].color;
            }, Ease.Linear, 0.1f, () => { foreach (var img in b.Tiles) img.color = Palette[blocker.Color]; b.Cap.color = Palette[blocker.Color]; });
        }

        /// <summary>A4: dotted flight path from the head to the edge, in the arrow's colour, and a gentle pulse.</summary>
        public void Hint(Arrow a)
        {
            ClearHint();
            if (!views.TryGetValue(a.Id, out var v)) return;
            hinted = a;
            v.Face.React(FaceId.Smug, -1f);
            for (int r = a.Head.R + Board.DY[a.Dir], c = a.Head.C + Board.DX[a.Dir]; ; r += Board.DY[a.Dir], c += Board.DX[a.Dir])
            {
                var inside = Board.Inside(r, c);
                var dot = UIKit.Image(hintLayer, "dot", new Vector2(0.5f, 0.5f), CellPos(new Pos(r, c)), new Vector2(Cell * 0.16f, Cell * 0.16f), Palette[a.Color]);
                dot.color = new Color(dot.color.r, dot.color.g, dot.color.b, inside ? 0.9f : 0.45f);
                hintDots.Add(dot);
                if (!inside) break;
            }
            foreach (var img in v.Tiles)
            {
                var t = img.transform;
                Tween.Run(t, 30f, k => t.localScale = Vector3.one * (1f + 0.08f * Mathf.Abs(Mathf.Sin(k * 30f * Mathf.PI * 1.2f))), Ease.Linear, 0f, () => t.localScale = Vector3.one);
            }
        }

        public void ClearHint()
        {
            foreach (var d in hintDots) if (d != null) Destroy(d.gameObject);
            hintDots.Clear();
            if (hinted == null || !views.TryGetValue(hinted.Id, out var v)) { hinted = null; return; }
            foreach (var img in v.Tiles)
            {
                Tween.Kill(img.transform);
                img.transform.localScale = Vector3.one;
            }
            v.Face.ClearReaction();
            hinted = null;
        }

        public void RemoveInstant(Arrow a)
        {
            if (!views.TryGetValue(a.Id, out var v)) return;
            views.Remove(a.Id);
            if (hinted == a) ClearHint();
            foreach (var img in v.Tiles) { Tween.Kill(img.transform); Tween.Scale(img.transform, Vector3.zero, 0.2f, Ease.InBack, 0f, () => Destroy(img.gameObject)); }
            Tween.Kill(v.Cap.transform);
            Tween.Scale(v.Cap.transform, Vector3.zero, 0.2f, Ease.InBack, 0f, () => Destroy(v.Cap.gameObject));
        }

        /// <summary>Every face on the board at once (lose: cry; win: grin).</summary>
        public void AllFaces(FaceId face, float seconds = -1f)
        {
            foreach (var v in views.Values) v.Face.React(face, seconds);
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
