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
    /// Draws a <see cref="Board"/> with one continuous stroke per arrow (<see cref="ArrowStroke"/>, tinted per colour)
    /// and turns taps into arrow picks. Every arrow wears a deadpan face on its round tail, so it gets yanked out
    /// backwards when it flies (panic).
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
        // flight, in cells per second: a quick start that keeps speeding up reads as a yank, not a conveyor
        private const float StartSpeed = 14f, TopSpeed = 46f, Acceleration = 120f;
        private const float InkTrailTime = 0.45f, InkWidth = 0.3f; // ink width as a fraction of a cell
        private const float CapSize = 0.6f, FaceSize = 0.44f; // fractions of a cell
        private static Material inkMaterial;

        // zoom-to-fit cap: past this a tiny level's arrows look bloated next to the HUD
        private const float MaxZoom = 1.4f;

        public event Action<Arrow> Tapped;
        public float Cell { get; private set; }
        /// <summary>How much the board is scaled up to fit a small level (1 = whole 7×9 grid).</summary>
        public float Zoom { get; private set; } = 1f;
        private Rect playArea;
        private Vector2? home;

        private class View
        {
            public ArrowStroke Stroke;
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
            // own canvas: ~20 blinking faces, hint pulses and slides re-batch only the board, not the HUD around it
            rt.gameObject.AddComponent<Canvas>();
            rt.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            UIKit.AddImage(rt, (Sprite)null, new Color(1, 1, 1, 0)).raycastTarget = true; // catches taps between arrows
            var view = rt.gameObject.AddComponent<ArrowBoardView>();
            view.Cell = cell;
            return view;
        }

        /// <summary>
        /// Draws the board. With <paramref name="fit"/> (levels: nothing spawns later) the view zooms in on the cells
        /// the puzzle uses, up to <see cref="MaxZoom"/>, so a small level fills the play area instead of sitting in a
        /// corner of the 7×9 grid; arrows then fade and puff where that area ends.
        /// </summary>
        public void Show(Board b, bool fit = false)
        {
            board = b;
            views.Clear();
            hintDots.Clear();
            hinted = null;
            foreach (Transform child in transform) Destroy(child.gameObject);
            int r0 = 0, r1 = Board.Rows - 1, c0 = 0, c1 = Board.Cols - 1;
            if (fit) ContentBounds(b, out r0, out r1, out c0, out c1);
            var lo = CellPos(new Pos(r1, c0)) - Vector2.one * Cell * 0.5f;
            var hi = CellPos(new Pos(r0, c1)) + Vector2.one * Cell * 0.5f;
            playArea = Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y);
            Zoom = Mathf.Clamp(Mathf.Min(Board.Cols * Cell / playArea.width, Board.Rows * Cell / playArea.height), 1f, MaxZoom);
            var rt = (RectTransform)transform;
            if (home == null) home = rt.anchoredPosition;
            rt.localScale = Vector3.one * Zoom;
            rt.anchoredPosition = home.Value - playArea.center * Zoom;
            for (int r = r0; r <= r1; r++)
                for (int c = c0; c <= c1; c++)
                    if (b.Mask == null || b.Mask[r, c])
                        UIKit.Image(transform, "grid_dot", new Vector2(0.5f, 0.5f), CellPos(new Pos(r, c)), new Vector2(32, 32), GridColor);
            hintLayer = UIKit.Stretch(UIKit.Rect("Hint", transform));
            layer = UIKit.Stretch(UIKit.Rect("Arrows", transform));
            int i = 0;
            foreach (var a in b.Arrows.Values) AddArrow(a, true, 0.02f * i++);
        }

        // the cells the puzzle uses: its mask when it has a shape, else the arrows' bounding box
        private static void ContentBounds(Board b, out int r0, out int r1, out int c0, out int c1)
        {
            r0 = c0 = int.MaxValue;
            r1 = c1 = int.MinValue;
            for (int r = 0; r < Board.Rows; r++)
                for (int c = 0; c < Board.Cols; c++)
                    if (b.Mask != null ? b.Mask[r, c] : b.Grid[r, c] != -1)
                    {
                        r0 = Mathf.Min(r0, r); r1 = Mathf.Max(r1, r);
                        c0 = Mathf.Min(c0, c); c1 = Mathf.Max(c1, c);
                    }
            if (r0 > r1) { r0 = c0 = 0; r1 = Board.Rows - 1; c1 = Board.Cols - 1; }
        }

        /// <summary>Centre of the visible play area, in board-local units.</summary>
        public Vector2 PlayCentre => playArea.center;

        public Vector2 CellPos(Pos p) => new((p.C - (Board.Cols - 1) / 2f) * Cell, -(p.R - (Board.Rows - 1) / 2f) * Cell);

        public Vector3 WorldPos(Pos p) => transform.TransformPoint(CellPos(p));

        public void AddArrow(Arrow a, bool pop, float delay = 0f)
        {
            var cells = new Vector2[a.Cells.Length];
            for (int i = 0; i < cells.Length; i++) cells[i] = CellPos(a.Cells[i]);
            var v = new View { Stroke = CreateStroke(layer, cells, a.Dir, Cell, Palette[a.Color], RunOut(a)) };
            v.Stroke.FadeRect = playArea;
            // a one-cell arrow shares its cell with the chevron: a smaller cap
            var capScale = a.Cells.Length == 1 ? 0.8f : 1f;
            v.Cap = UIKit.Image(layer, "dot", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Cell * CapSize, Cell * CapSize) * capScale, Palette[a.Color]);
            v.Face = Face.AddUI(v.Cap.transform, new Vector2(Cell * FaceSize, Cell * FaceSize) * capScale, new Vector2(0, Cell * 0.02f));
            v.Face.InkFor(Palette[a.Color]);
            views[a.Id] = v;
            PlaceCap(v);
            if (!pop) return;
            var stroke = v.Stroke;
            stroke.WidthScale = 0f;
            Tween.Run(stroke, 0.22f, k => stroke.WidthScale = k, Ease.OutBack, delay);
            v.Cap.transform.localScale = Vector3.zero;
            Tween.Scale(v.Cap.transform, Vector3.one, 0.22f, Ease.OutBack, delay);
        }

        // track past the head: far enough for the whole body to leave the board and fade
        private float RunOut(Arrow a) => (Mathf.Max(Board.Rows, Board.Cols) + a.Cells.Length + 3) * Cell;

        /// <summary>One arrow as a continuous stroke; cell centres head first, in the parent's local space.</summary>
        public static ArrowStroke CreateStroke(Transform parent, Vector2[] headFirst, int dir, float cell, Color color, float runOut = 0f)
        {
            var rt = UIKit.Stretch(UIKit.Rect("Arrow", parent));
            var stroke = rt.gameObject.AddComponent<ArrowStroke>();
            stroke.raycastTarget = false;
            stroke.color = color;
            stroke.SetPath(headFirst, dir, cell, runOut);
            return stroke;
        }

        // The face rides the round tail end, and fades with it past the board edge.
        private static void PlaceCap(View v)
        {
            var tail = v.Stroke.TailPoint;
            v.Cap.rectTransform.anchoredPosition = tail;
            var c = v.Cap.color;
            c.a = v.Stroke.FadeAt(tail);
            v.Cap.color = c;
            v.Face.gameObject.SetActive(c.a > 0.5f);
        }

        // Stops any running tween on the arrow and puts it back at full size where it belongs (A3: tapping an arrow
        // that is still growing in used to freeze it small).
        private static void Settle(Arrow a, View v)
        {
            Tween.Kill(v.Stroke);
            Tween.Kill(v.Stroke.transform);
            v.Stroke.WidthScale = 1f;
            v.Stroke.Shift = Vector2.zero;
            v.Stroke.Advance = 0f;
            v.Stroke.color = Palette[a.Color];
            Tween.Kill(v.Cap.transform);
            v.Cap.transform.localScale = Vector3.one;
            v.Cap.color = Palette[a.Color];
            PlaceCap(v);
        }

        /// <summary>Slides the arrow out along its own body, speeding up, and fades it past the board edge.</summary>
        public void AnimateExit(Arrow a)
        {
            if (!views.TryGetValue(a.Id, out var v)) return;
            if (hinted == a) ClearHint();
            views.Remove(a.Id);
            Settle(a, v);
            v.Face.React(FaceId.Panic, -1f);
            StartCoroutine(Slide(v));
        }

        private IEnumerator Slide(View v)
        {
            var stroke = v.Stroke;
            var ink = CreateInkTrail(stroke.color, stroke.transform.TransformPoint(stroke.TailPoint));
            float speed = StartSpeed * Cell, max = stroke.BodyLength + (Mathf.Max(Board.Rows, Board.Cols) + 3) * Cell;
            bool puffed = false;
            while (stroke.Advance < max)
            {
                yield return null;
                speed = Mathf.Min(speed + Acceleration * Cell * Time.deltaTime, TopSpeed * Cell);
                stroke.Advance = Mathf.Min(stroke.Advance + speed * Time.deltaTime, max);
                PlaceCap(v);
                // the ink is laid by the TAIL, so it never covers the arrow's own body
                ink.transform.position = stroke.transform.TransformPoint(stroke.TailPoint);
                var head = stroke.HeadPoint;
                if (!puffed && OutsideBoard(head))
                {
                    puffed = true;
                    GameFx.Play("Land_Poof", stroke.transform.TransformPoint(head), WorldCell() * 0.35f);
                }
                if (stroke.Advance > stroke.BodyLength && stroke.FadeAt(stroke.TailPoint) <= 0f) break;
            }
            Destroy(stroke.gameObject);
            Destroy(v.Cap.gameObject);
            ink.emitting = false;
            inks.Remove(ink.gameObject);
            Destroy(ink.gameObject, InkTrailTime + 0.1f);
        }

        private bool OutsideBoard(Vector2 p) => !playArea.Contains(p);

        private float WorldCell() => (WorldPos(new Pos(0, 1)) - WorldPos(new Pos(0, 0))).magnitude;

        // Ink brush stroke (InkBrush shader): bristles along the stroke, dries out from the tail end.
        private TrailRenderer CreateInkTrail(Color color, Vector3 start)
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
            trail.startColor = trail.endColor = color;
            var canvas = GetComponentInParent<Canvas>()?.rootCanvas; // the board has its own nested canvas; sorting comes from the root
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
            var tint = costsHeart ? ErrorColor : Palette[a.Color];
            var stroke = v.Stroke;
            Tween.Run(stroke.transform, 0.34f, k =>
            {
                // out fast, back with a little spring
                var t = k < 0.35f ? Tween.Evaluate(Ease.OutQuad, k / 0.35f) : 1f - Tween.Evaluate(Ease.OutBack, (k - 0.35f) / 0.65f);
                stroke.Shift = push * t;
                stroke.color = Color.Lerp(Palette[a.Color], tint, Mathf.Sin(k * Mathf.PI));
                PlaceCap(v);
                v.Cap.color = stroke.color;
            }, Ease.Linear, 0f, () => { if (views.ContainsKey(a.Id)) Settle(a, v); });

            if (!views.TryGetValue(blocker.Id, out var b)) return;
            b.Face.React(FaceId.Meh, 0.9f);
            Tween.Run(b.Cap.transform, 0.3f, k =>
            {
                var flash = k < 0.12f ? 0f : Mathf.Sin((k - 0.12f) / 0.88f * Mathf.PI);
                b.Stroke.color = Color.Lerp(Palette[blocker.Color], Color.white, flash * 0.85f);
                b.Cap.color = b.Stroke.color;
            }, Ease.Linear, 0.1f, () => { b.Stroke.color = Palette[blocker.Color]; b.Cap.color = Palette[blocker.Color]; });
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
                var inside = Board.Inside(r, c) && playArea.Contains(CellPos(new Pos(r, c)));
                var dot = UIKit.Image(hintLayer, "dot", new Vector2(0.5f, 0.5f), CellPos(new Pos(r, c)), new Vector2(Cell * 0.16f, Cell * 0.16f), Palette[a.Color]);
                dot.color = new Color(dot.color.r, dot.color.g, dot.color.b, inside ? 0.9f : 0.45f);
                hintDots.Add(dot);
                if (!inside) break;
            }
            var stroke = v.Stroke;
            Tween.Loop(stroke, s => stroke.WidthScale = 1f + 0.3f * Mathf.Abs(Mathf.Sin(s * Mathf.PI * 1.2f)));
        }

        public void ClearHint()
        {
            foreach (var d in hintDots) if (d != null) Destroy(d.gameObject);
            hintDots.Clear();
            if (hinted == null || !views.TryGetValue(hinted.Id, out var v)) { hinted = null; return; }
            Tween.Kill(v.Stroke);
            v.Stroke.WidthScale = 1f;
            v.Face.ClearReaction();
            hinted = null;
        }

        public void RemoveInstant(Arrow a)
        {
            if (!views.TryGetValue(a.Id, out var v)) return;
            views.Remove(a.Id);
            if (hinted == a) ClearHint();
            var stroke = v.Stroke;
            Tween.Kill(stroke);
            Tween.Kill(stroke.transform);
            Tween.Run(stroke, 0.2f, k => stroke.WidthScale = 1f - k, Ease.InBack, 0f, () => Destroy(stroke.gameObject));
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
