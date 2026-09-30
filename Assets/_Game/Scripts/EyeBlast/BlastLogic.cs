using System;
using System.Collections.Generic;

namespace CasualGame.EyeBlast
{
    public sealed class Piece
    {
        public (int r, int c)[] Cells;
        public int Color;
        public int Width, Height; // in cells
    }

    /// <summary>Block Blast rules on an 8×8 grid: place pieces, full rows/columns clear, fair deals.</summary>
    public sealed class BlastBoard
    {
        public const int Size = 8;
        public const int Colors = 7;
        public readonly int[,] Grid = new int[Size, Size]; // 0 empty, 1..7 color

        private static (int, int)[] Line(int n, bool vertical)
        {
            var cells = new (int, int)[n];
            for (int i = 0; i < n; i++) cells[i] = vertical ? (i, 0) : (0, i);
            return cells;
        }

        private static (int, int)[] Square(int n)
        {
            var cells = new (int, int)[n * n];
            for (int i = 0; i < n * n; i++) cells[i] = (i / n, i % n);
            return cells;
        }

        public static readonly (int r, int c)[][] Shapes =
        {
            new[] { (0, 0) },
            Line(2, false), Line(2, true), Line(3, false), Line(3, true), Line(4, false), Line(4, true), Line(5, false), Line(5, true),
            Square(2), Square(3),
            new[] { (0, 0), (1, 0), (1, 1) }, new[] { (0, 0), (0, 1), (1, 0) }, new[] { (0, 0), (0, 1), (1, 1) }, new[] { (0, 1), (1, 0), (1, 1) },
            new[] { (0, 0), (1, 0), (2, 0), (2, 1) }, new[] { (0, 0), (0, 1), (0, 2), (1, 0) }, new[] { (0, 0), (0, 1), (1, 1), (2, 1) }, new[] { (0, 2), (1, 0), (1, 1), (1, 2) },
            new[] { (0, 0), (1, 0), (2, 0), (2, 1), (2, 2) }, new[] { (0, 0), (0, 1), (0, 2), (1, 0), (2, 0) }, new[] { (0, 0), (0, 1), (0, 2), (1, 2), (2, 2) }, new[] { (0, 2), (1, 2), (2, 0), (2, 1), (2, 2) },
            new[] { (0, 0), (0, 1), (0, 2), (1, 1) }, new[] { (0, 1), (1, 0), (1, 1), (2, 1) }, new[] { (0, 1), (1, 0), (1, 1), (1, 2) }, new[] { (0, 0), (1, 0), (1, 1), (2, 0) },
            new[] { (0, 1), (0, 2), (1, 0), (1, 1) }, new[] { (0, 0), (0, 1), (1, 1), (1, 2) }, new[] { (0, 0), (1, 0), (1, 1), (2, 1) }, new[] { (0, 1), (1, 0), (1, 1), (2, 0) },
        };

        public bool CanPlace(Piece p, int r0, int c0)
        {
            foreach (var (r, c) in p.Cells)
            {
                int rr = r0 + r, cc = c0 + c;
                if (rr < 0 || rr >= Size || cc < 0 || cc >= Size || Grid[rr, cc] != 0) return false;
            }
            return true;
        }

        public bool FitsAnywhere(Piece p)
        {
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    if (CanPlace(p, r, c)) return true;
            return false;
        }

        public void Place(Piece p, int r0, int c0)
        {
            foreach (var (r, c) in p.Cells) Grid[r0 + r, c0 + c] = p.Color;
        }

        /// <summary>Rows and columns that are full on the given grid (or on the grid plus a hypothetical placement).</summary>
        public void FullLines(List<int> rows, List<int> cols, Piece extra = null, int er = 0, int ec = 0)
        {
            rows.Clear();
            cols.Clear();
            bool Filled(int r, int c)
            {
                if (Grid[r, c] != 0) return true;
                if (extra == null) return false;
                foreach (var (pr, pc) in extra.Cells)
                    if (er + pr == r && ec + pc == c) return true;
                return false;
            }
            for (int i = 0; i < Size; i++)
            {
                bool row = true, col = true;
                for (int j = 0; j < Size; j++)
                {
                    row &= Filled(i, j);
                    col &= Filled(j, i);
                }
                if (row) rows.Add(i);
                if (col) cols.Add(i);
            }
        }

        public float FillRatio()
        {
            int n = 0;
            foreach (var v in Grid) if (v != 0) n++;
            return n / (float)(Size * Size);
        }

        public bool Empty()
        {
            foreach (var v in Grid) if (v != 0) return false;
            return true;
        }

        public static int LineScore(int lines) => 10 * lines * (lines + 1) / 2;

        public static Piece MakePiece((int r, int c)[] shape, int color)
        {
            int w = 0, h = 0;
            foreach (var (r, c) in shape) { w = Math.Max(w, c + 1); h = Math.Max(h, r + 1); }
            return new Piece { Cells = shape, Color = color, Width = w, Height = h };
        }

        /// <summary>
        /// Three random pieces, re-rolled (up to 8 times) until all three can be placed in at least one order (counting
        /// the lines they clear on the way). Small pieces get likelier on a crowded board.
        /// </summary>
        public Piece[] Deal(Func<float> rand)
        {
            var crowded = FillRatio() > 0.6f;
            Piece Roll()
            {
                float total = 0;
                foreach (var s in Shapes) total += crowded && s.Length <= 3 ? 3 : 1;
                var x = rand() * total;
                foreach (var s in Shapes)
                {
                    x -= crowded && s.Length <= 3 ? 3 : 1;
                    if (x < 0) return MakePiece(s, 1 + (int)(rand() * Colors));
                }
                return MakePiece(Shapes[0], 1);
            }

            var pieces = new Piece[3];
            for (int attempt = 0; attempt < 8; attempt++)
            {
                for (int i = 0; i < 3; i++) pieces[i] = Roll();
                if (AllPlaceable(pieces)) return pieces;
            }
            // fallback: the smallest pieces, which fit whenever the board has room at all
            for (int i = 0; i < 3; i++) pieces[i] = MakePiece(Shapes[0], 1 + (int)(rand() * Colors));
            return pieces;
        }

        /// <summary>True if some order places every piece (line clears between placements included). Search is capped.</summary>
        public bool AllPlaceable(IList<Piece> pieces)
        {
            int budget = 6000;
            return Search((int[,])Grid.Clone(), pieces, new bool[pieces.Count], ref budget);
        }

        private static bool Search(int[,] grid, IList<Piece> pieces, bool[] used, ref int budget)
        {
            bool any = false;
            for (int i = 0; i < pieces.Count; i++)
            {
                if (used[i] || pieces[i] == null) continue;
                any = true;
                var p = pieces[i];
                for (int r = 0; r <= Size - p.Height; r++)
                    for (int c = 0; c <= Size - p.Width; c++)
                    {
                        if (!Fits(grid, p, r, c)) continue;
                        if (--budget < 0) return false;
                        var next = (int[,])grid.Clone();
                        foreach (var (pr, pc) in p.Cells) next[r + pr, c + pc] = p.Color;
                        ClearFull(next);
                        used[i] = true;
                        var ok = Search(next, pieces, used, ref budget);
                        used[i] = false;
                        if (ok) return true;
                    }
            }
            return !any;
        }

        private static bool Fits(int[,] grid, Piece p, int r0, int c0)
        {
            foreach (var (r, c) in p.Cells)
                if (grid[r0 + r, c0 + c] != 0) return false;
            return true;
        }

        private static void ClearFull(int[,] grid)
        {
            Span<bool> rows = stackalloc bool[Size], cols = stackalloc bool[Size];
            for (int i = 0; i < Size; i++)
            {
                bool row = true, col = true;
                for (int j = 0; j < Size; j++) { row &= grid[i, j] != 0; col &= grid[j, i] != 0; }
                rows[i] = row;
                cols[i] = col;
            }
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    if (rows[r] || cols[c]) grid[r, c] = 0;
        }

        /// <summary>The <paramref name="count"/> fullest rows/columns (revive clears them).</summary>
        public List<(bool row, int index)> FullestLines(int count)
        {
            var all = new List<(bool row, int index, int filled)>();
            for (int i = 0; i < Size; i++)
            {
                int r = 0, c = 0;
                for (int j = 0; j < Size; j++) { if (Grid[i, j] != 0) r++; if (Grid[j, i] != 0) c++; }
                all.Add((true, i, r));
                all.Add((false, i, c));
            }
            all.Sort((x, y) => y.filled.CompareTo(x.filled));
            var result = new List<(bool, int)>();
            foreach (var l in all)
            {
                if (result.Count == count || l.filled == 0) break;
                result.Add((l.row, l.index));
            }
            return result;
        }
    }
}
