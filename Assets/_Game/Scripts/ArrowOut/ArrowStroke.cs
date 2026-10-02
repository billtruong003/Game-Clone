using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CasualGame.ArrowOut
{
    /// <summary>
    /// One arrow drawn as a single continuous stroke (UI mesh): straight runs, quarter-circle bends of half a cell,
    /// a chevron head with round ends and a 1-px soft edge so it reads smooth at any size. Because the body is one
    /// path, the arrow can slide out along its own track continuously (<see cref="Advance"/>), bends included, which
    /// the old one-sprite-per-cell drawing could only do cell by cell, and it never shows seams between cells.
    /// Geometry follows the old tiles: line 0.21 cell wide, head line ending 0.23 cell past the head centre,
    /// chevron arms 0.41 cell at 135°.
    /// UVs for shaders (theme lab): uv0.x = distance from the tail along the stroke (cells), uv0.y = across the line
    /// (±1 at the line edge, beyond it in the rim / glow pad); uv1.x = 0 at the tail .. 1 at the head, uv1.y = 1 on the
    /// chevron. The default UI shader ignores them (white texture), so the normal look is unchanged.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ArrowStroke : MaskableGraphic
    {
        private const float Width = 0.208f, HeadReach = 0.234f, ApexReach = 0.266f, ArmLength = 0.41f, SingleBack = 0.344f;
        private const float Feather = 1.3f; // soft edge, in canvas pixels
        private const int ArcSteps = 8, CapSteps = 14;

        private struct Prim { public bool arc; public Vector2 a, b, c; public float r, a0, sweep, len; }

        private readonly List<Prim> prims = new();
        private float cell, bodyLength, totalLength;
        private Vector2 dir;
        private float advance, widthScale = 1f;
        private Vector2 shift;
        private Rect fadeRect;

        /// <summary>How far the arrow has slid forward along its track (canvas units).</summary>
        public float Advance { get => advance; set { advance = value; SetVerticesDirty(); } }

        /// <summary>Rigid offset (the lunge).</summary>
        public Vector2 Shift { get => shift; set { shift = value; SetVerticesDirty(); } }

        /// <summary>Line width multiplier (grow-in, hint pulse).</summary>
        public float WidthScale { get => widthScale; set { widthScale = value; SetVerticesDirty(); } }

        /// <summary>
        /// Extra geometry around the line for glow / halo shaders, in cells. 0 = the normal 1-px soft rim. Above 0 the rim
        /// is that wide and fully opaque: the material draws the line edge itself from uv0.y (|y| = 1).
        /// </summary>
        public float GlowPad { get => glowPad; set { glowPad = value; SetVerticesDirty(); } }
        private float glowPad;

        /// <summary>The visible play area (local): past it the stroke fades out over 1.2 cells. Zero size = never fades.</summary>
        public Rect FadeRect { get => fadeRect; set { fadeRect = value; SetVerticesDirty(); } }

        /// <summary>Path length still on the track behind the head; the arrow is gone once Advance passes it.</summary>
        public float BodyLength => bodyLength;

        /// <summary>The board cell size the path was laid out with (canvas units).</summary>
        public float CellSize => cell;

        /// <summary>
        /// The arrow's cells, head first (as in Arrow.Cells), as local positions, its direction (Board 0 up, 1 right,
        /// 2 down, 3 left) and the cell size. The track continues straight past the head for <paramref name="runOut"/>.
        /// </summary>
        public void SetPath(IReadOnlyList<Vector2> headFirst, int boardDir, float cellSize, float runOut)
        {
            cell = cellSize;
            dir = new Vector2(Board.DX[boardDir], -Board.DY[boardDir]);
            var pts = new List<Vector2>(headFirst.Count + 2);
            for (int i = headFirst.Count - 1; i >= 0; i--) pts.Add(headFirst[i]); // tail → head
            if (pts.Count == 1) pts.Insert(0, pts[0] - dir * cell * SingleBack);
            var tip = pts[^1] + dir * cell * HeadReach;
            pts.Add(tip);
            prims.Clear();
            BuildFilleted(pts, cell * 0.5f);
            bodyLength = Length();
            // run-out: straight on from the tip
            AddLine(tip, tip + dir * runOut);
            totalLength = Length();
            SetVerticesDirty();
        }

        /// <summary>Where the round tail end is right now (local, shift included).</summary>
        public Vector2 TailPoint => Sample(advance, out _) + shift;

        /// <summary>Where the head tip is right now (local, shift included).</summary>
        public Vector2 HeadPoint => Sample(advance + bodyLength, out _) + shift;

        /// <summary>A point on the body, <paramref name="fromTail"/> canvas units ahead of the round tail end (local, shift included).</summary>
        public Vector2 PointAt(float fromTail) => Sample(advance + fromTail, out _) + shift;

        /// <summary>0..1 fade at a local point (1 inside the board, 0 past 1.2 cells outside).</summary>
        public float FadeAt(Vector2 p)
        {
            if (fadeRect.width <= 0f) return 1f;
            var d = p - fadeRect.center;
            var outside = Mathf.Max(Mathf.Abs(d.x) - fadeRect.width * 0.5f, Mathf.Abs(d.y) - fadeRect.height * 0.5f, 0f);
            return 1f - Mathf.Clamp01(outside / (cell * 1.2f));
        }

        // ---------------- path ----------------

        private void BuildFilleted(List<Vector2> pts, float radius)
        {
            var cursor = pts[0];
            for (int i = 1; i < pts.Count - 1; i++)
            {
                Vector2 inDir = (pts[i] - pts[i - 1]).normalized, outDir = (pts[i + 1] - pts[i]).normalized;
                if (Vector2.Dot(inDir, outDir) > 0.999f) continue; // straight through
                var start = pts[i] - inDir * radius;
                var end = pts[i] + outDir * radius;
                AddLine(cursor, start);
                // quarter circle from start to end; its centre sits on the inside of the turn
                var turn = Mathf.Sign(inDir.x * outDir.y - inDir.y * outDir.x); // +1 counter-clockwise
                var centre = start + new Vector2(-inDir.y, inDir.x) * radius * turn;
                var a0 = Mathf.Atan2(start.y - centre.y, start.x - centre.x);
                prims.Add(new Prim { arc = true, c = centre, r = radius, a0 = a0, sweep = turn * Mathf.PI * 0.5f, len = radius * Mathf.PI * 0.5f });
                cursor = end;
            }
            AddLine(cursor, pts[^1]);
        }

        private void AddLine(Vector2 a, Vector2 b)
        {
            var len = (b - a).magnitude;
            if (len > 0.0001f) prims.Add(new Prim { a = a, b = b, len = len });
        }

        private float Length()
        {
            float l = 0f;
            foreach (var p in prims) l += p.len;
            return l;
        }

        private Vector2 Sample(float s, out Vector2 tangent)
        {
            s = Mathf.Clamp(s, 0f, totalLength);
            for (int i = 0; i < prims.Count; i++)
            {
                var p = prims[i];
                if (s <= p.len || i == prims.Count - 1)
                {
                    var t = p.len > 0f ? Mathf.Clamp01(s / p.len) : 0f;
                    if (!p.arc)
                    {
                        tangent = (p.b - p.a) / p.len;
                        return Vector2.Lerp(p.a, p.b, t);
                    }
                    var ang = p.a0 + p.sweep * t;
                    var radial = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                    tangent = new Vector2(-radial.y, radial.x) * Mathf.Sign(p.sweep);
                    return p.c + radial * p.r;
                }
                s -= p.len;
            }
            tangent = dir;
            return prims.Count > 0 ? prims[^1].b : Vector2.zero;
        }

        // ---------------- mesh ----------------

        private readonly List<Vector2> pathPts = new();
        private readonly List<Vector2> pathTan = new();
        private readonly List<float> pathS = new();
        private float meshS0;
        private float Rim => glowPad > 0f ? glowPad * cell : Feather;
        private float RimAlpha => glowPad > 0f ? 1f : 0f;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (prims.Count == 0 || widthScale <= 0.001f) return;
            float s0 = advance, s1 = advance + bodyLength;
            pathPts.Clear();
            pathTan.Clear();
            pathS.Clear();
            meshS0 = s0;
            float acc = 0f;
            foreach (var p in prims)
            {
                float a = acc, b = acc + p.len;
                acc = b;
                if (b < s0 || a > s1) continue;
                float from = Mathf.Max(a, s0), to = Mathf.Min(b, s1);
                int steps = p.arc ? Mathf.Max(2, Mathf.CeilToInt(ArcSteps * (to - from) / p.len)) : 1;
                for (int k = pathPts.Count == 0 ? 0 : 1; k <= steps; k++)
                {
                    var sk = Mathf.Lerp(from, to, k / (float)steps);
                    var pt = Sample(sk, out var tan);
                    pathPts.Add(pt + shift);
                    pathTan.Add(tan);
                    pathS.Add(sk);
                }
            }
            if (pathPts.Count < 2) return;
            var half = cell * Width * 0.5f * widthScale;
            Strip(vh, pathPts, pathTan, half, false);
            // chevron: two round-ended arms from the apex, plus round ends where they meet
            var head = pathPts[^1];
            var apex = head + dir * cell * (ApexReach - HeadReach);
            for (int side = -1; side <= 1; side += 2)
            {
                var armDir = Rotate(-dir, 45f * side);
                var end = apex + armDir * cell * ArmLength;
                Capsule(vh, apex, end, half);
            }
            Disc(vh, apex, half);
        }

        private static Vector2 Rotate(Vector2 v, float deg)
        {
            var r = deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        private Color32 Col(Vector2 p, float alpha)
        {
            var c = color;
            c.a *= alpha * FadeAt(p);
            return c;
        }

        private void Vert(VertexHelper vh, Vector2 pos, Color32 col, float along, float across, float t, float head)
        {
            var v = UIVertex.simpleVert;
            v.position = pos;
            v.color = col;
            v.uv0 = new Vector4(along, across, 0f, 0f);
            v.uv1 = new Vector4(t, head, 0f, 0f);
            vh.AddVert(v);
        }

        // A line strip with a rim: four vertices per point (rim, edge, edge, rim). The body's points carry their path
        // position (pathS); the chevron arms (head) measure along from the apex.
        private void Strip(VertexHelper vh, List<Vector2> pts, List<Vector2> tans, float half, bool head)
        {
            int start = vh.currentVertCount;
            var rimAcross = (half + Rim) / Mathf.Max(0.001f, half);
            for (int i = 0; i < pts.Count; i++)
            {
                var n = new Vector2(-tans[i].y, tans[i].x);
                var p = pts[i];
                float along, t;
                if (head) { along = bodyLength / cell + (p - pts[0]).magnitude / cell; t = 1f; }
                else { along = (pathS[i] - meshS0) / cell; t = bodyLength > 0f ? (pathS[i] - meshS0) / bodyLength : 0f; }
                float h = head ? 1f : 0f;
                Vert(vh, p + n * (half + Rim), Col(p, RimAlpha), along, rimAcross, t, h);
                Vert(vh, p + n * half, Col(p, 1f), along, 1f, t, h);
                Vert(vh, p - n * half, Col(p, 1f), along, -1f, t, h);
                Vert(vh, p - n * (half + Rim), Col(p, RimAlpha), along, -rimAcross, t, h);
            }
            for (int i = 0; i < pts.Count - 1; i++)
            {
                int a = start + i * 4, b = a + 4;
                for (int k = 0; k < 3; k++) { vh.AddTriangle(a + k, b + k, b + k + 1); vh.AddTriangle(a + k, b + k + 1, a + k + 1); }
            }
        }

        private void Capsule(VertexHelper vh, Vector2 a, Vector2 b, float half)
        {
            var t = (b - a).normalized;
            pathPts.Clear();
            pathTan.Clear();
            pathPts.Add(a); pathTan.Add(t);
            pathPts.Add(b); pathTan.Add(t);
            Strip(vh, pathPts, pathTan, half, true);
            Disc(vh, b, half);
        }

        private void Disc(VertexHelper vh, Vector2 c, float r)
        {
            int centre = vh.currentVertCount;
            var along = bodyLength / cell;
            var rimAcross = (r + Rim) / Mathf.Max(0.001f, r);
            Vert(vh, c, Col(c, 1f), along, 0f, 1f, 1f);
            for (int i = 0; i <= CapSteps; i++)
            {
                var ang = i * Mathf.PI * 2f / CapSteps;
                var d = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                Vert(vh, c + d * r, Col(c, 1f), along, 1f, 1f, 1f);
                Vert(vh, c + d * (r + Rim), Col(c, RimAlpha), along, rimAcross, 1f, 1f);
            }
            for (int i = 0; i < CapSteps; i++)
            {
                int e0 = centre + 1 + i * 2, e1 = e0 + 2;
                vh.AddTriangle(centre, e0, e1);
                vh.AddTriangle(e0, e0 + 1, e1 + 1);
                vh.AddTriangle(e0, e1 + 1, e1);
            }
        }
    }
}
