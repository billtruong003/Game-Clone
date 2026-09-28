using System;
using System.Collections.Generic;

namespace CasualGame.ArrowOut
{
    public readonly struct Pos : IEquatable<Pos>
    {
        public readonly int R, C;
        public Pos(int r, int c) { R = r; C = c; }
        public bool Equals(Pos o) => R == o.R && C == o.C;
        public override bool Equals(object o) => o is Pos p && Equals(p);
        public override int GetHashCode() => R * 31 + C;
        public override string ToString() => $"({R},{C})";
    }

    /// <summary>A snake-like arrow. Cells[0] is the head; it exits by sliding forward along Dir with the body following.</summary>
    public sealed class Arrow
    {
        public int Id;
        public Pos[] Cells;
        public int Dir; // 0 up, 1 right, 2 down, 3 left
        public int Color;
        public Pos Head => Cells[0];
    }

    public struct Difficulty
    {
        public int Stage, MinLen, MaxLen, Colors, MinArrows;
        public float TwoChance;
    }

    /// <summary>
    /// Arrow Out rules, engine-free so they are unit-testable.
    /// Deadlock guarantee: edge A→B whenever any cell of B lies anywhere on A's head path. A new arrow is only
    /// placed if that graph stays acyclic; then some arrow is always free and removing arrows can never create a cycle.
    /// </summary>
    public sealed class Board
    {
        public const int Cols = 7, Rows = 9;
        public static readonly int[] DX = { 0, 1, 0, -1 };
        public static readonly int[] DY = { -1, 0, 1, 0 };

        public readonly int[,] Grid = new int[Rows, Cols]; // arrow id, -1 = empty
        public readonly Dictionary<int, Arrow> Arrows = new();
        public readonly bool[,] Mask; // level mode play area; null = whole board
        public int NextId;

        public Board(bool[,] mask = null)
        {
            Mask = mask;
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++) Grid[r, c] = -1;
        }

        public static bool Inside(int r, int c) => r >= 0 && r < Rows && c >= 0 && c < Cols;
        public bool Usable(int r, int c) => Grid[r, c] == -1 && (Mask == null || Mask[r, c]);
        public int Count => Arrows.Count;

        public static bool[,] CenteredMask(int w, int h)
        {
            var m = new bool[Rows, Cols];
            int c0 = (Cols - w) / 2, r0 = (Rows - h) / 2;
            for (int r = r0; r < r0 + h; r++)
                for (int c = c0; c < c0 + w; c++) m[r, c] = true;
            return m;
        }

        /// <summary>Id of the first arrow in the head's path to the edge, or -1 if clear.</summary>
        public int FirstBlocker(Arrow a)
        {
            int r = a.Head.R + DY[a.Dir], c = a.Head.C + DX[a.Dir];
            for (; Inside(r, c); r += DY[a.Dir], c += DX[a.Dir])
                if (Grid[r, c] != -1) return Grid[r, c];
            return -1;
        }

        public bool IsFree(Arrow a) => FirstBlocker(a) == -1;

        public void FreeArrows(List<Arrow> into)
        {
            into.Clear();
            foreach (var a in Arrows.Values)
                if (IsFree(a)) into.Add(a);
        }

        public bool HasCycle()
        {
            var state = new Dictionary<int, byte>(Arrows.Count); // 1 on stack, 2 done
            foreach (var a in Arrows.Values)
                if (Visit(a, state)) return true;
            return false;
        }

        private bool Visit(Arrow a, Dictionary<int, byte> state)
        {
            if (state.TryGetValue(a.Id, out var s)) return s == 1;
            state[a.Id] = 1;
            int r = a.Head.R + DY[a.Dir], c = a.Head.C + DX[a.Dir];
            int lastSeen = -1;
            for (; Inside(r, c); r += DY[a.Dir], c += DX[a.Dir])
            {
                var id = Grid[r, c];
                if (id == -1 || id == lastSeen) continue;
                lastSeen = id;
                if (Visit(Arrows[id], state)) return true;
            }
            state[a.Id] = 2;
            return false;
        }

        public void Place(Arrow a)
        {
            Arrows[a.Id] = a;
            foreach (var p in a.Cells) Grid[p.R, p.C] = a.Id;
        }

        public Arrow Remove(int id)
        {
            var a = Arrows[id];
            foreach (var p in a.Cells) Grid[p.R, p.C] = -1;
            Arrows.Remove(id);
            return a;
        }

        public List<Pos> EmptyCells()
        {
            var list = new List<Pos>();
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    if (Usable(r, c)) list.Add(new Pos(r, c));
            return list;
        }

        /// <summary>
        /// Places one arrow without creating a deadlock, longest allowed length first, shorter when space is tight.
        /// Returns null only when not even a 1-cell arrow fits.
        /// </summary>
        public Arrow Spawn(Func<float> rand, int minLen, int maxLen, int colors)
        {
            int want = minLen + (int)(rand() * (maxLen - minLen + 1));
            int color = (int)(rand() * colors);
            var dirs = new[] { 0, 1, 2, 3 };
            for (int len = want; len >= 1; len--)
            {
                var cells = EmptyCells();
                Shuffle(cells, rand);
                foreach (var head in cells)
                {
                    Shuffle(dirs, rand);
                    foreach (var dir in dirs)
                    {
                        var body = GrowBody(head, dir, len, rand);
                        if (body == null) continue;
                        var a = new Arrow { Id = NextId, Cells = body, Dir = dir, Color = color };
                        Place(a);
                        if (!HasCycle())
                        {
                            NextId++;
                            return a;
                        }
                        Remove(a.Id);
                    }
                }
            }
            return null;
        }

        // Random walk backwards from the head; the cell right behind the head fixes the arrow's direction.
        private Pos[] GrowBody(Pos head, int dir, int len, Func<float> rand)
        {
            var cells = new List<Pos>(len) { head };
            bool Ahead(int r, int c) => dir % 2 == 0 ? c == head.C && (r - head.R) * DY[dir] > 0 : r == head.R && (c - head.C) * DX[dir] > 0;
            bool Ok(int r, int c) => Inside(r, c) && Usable(r, c) && !cells.Contains(new Pos(r, c)) && !Ahead(r, c);

            if (len > 1)
            {
                int r = head.R - DY[dir], c = head.C - DX[dir];
                if (!Ok(r, c)) return null;
                cells.Add(new Pos(r, c));
            }
            var order = new[] { 0, 1, 2, 3 };
            while (cells.Count < len)
            {
                var last = cells[cells.Count - 1];
                Shuffle(order, rand);
                var found = false;
                foreach (var d in order)
                {
                    int r = last.R + DY[d], c = last.C + DX[d];
                    if (!Ok(r, c)) continue;
                    cells.Add(new Pos(r, c));
                    found = true;
                    break;
                }
                if (!found) return null;
            }
            return cells.ToArray();
        }

        /// <summary>Cells the arrow occupies after sliding <paramref name="step"/> cells forward (may be off-board).</summary>
        public static Pos[] Slide(Arrow a, int step)
        {
            var result = new Pos[a.Cells.Length];
            int i = 0;
            for (int k = step; k >= 1 && i < result.Length; k--)
                result[i++] = new Pos(a.Head.R + DY[a.Dir] * k, a.Head.C + DX[a.Dir] * k);
            for (int j = 0; i < result.Length; j++) result[i++] = a.Cells[j];
            return result;
        }

        public static void Shuffle<T>(IList<T> list, Func<float> rand)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = (int)(rand() * (i + 1));
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }

    public static class Rng
    {
        /// <summary>Deterministic PRNG (mulberry32): same seed → same spawn sequence (daily challenge, baked levels).</summary>
        public static Func<float> Seeded(int seed)
        {
            uint s = (uint)seed;
            return () =>
            {
                s += 0x6D2B79F5u;
                uint t = s;
                t = (t ^ (t >> 15)) * (t | 1u);
                t ^= t + (t ^ (t >> 7)) * (t | 61u);
                return ((t ^ (t >> 14)) >> 8) / 16777216f;
            };
        }

        public static Func<float> Unity() => () => UnityEngine.Random.value * 0.99999f;

        public static int DailySeed(DateTime d) => d.Year * 10000 + d.Month * 100 + d.Day;
    }

    /// <summary>Endless mode: stages, color combo, breather turns, top-up. Tuned against bots (see tests).</summary>
    public sealed class EndlessRun
    {
        public const int StartArrows = 10;
        public const int ComboMax = 5;
        public const int BreatherCombo = 3;

        // Stage thresholds in arrows cleared.
        private static readonly int[] StageAt = { 0, 10, 30, 60, 100, 150 };
        private static readonly Difficulty[] Stages =
        {
            new() { MinLen = 1, MaxLen = 2, Colors = 2, TwoChance = 0f, MinArrows = 6 },
            new() { MinLen = 1, MaxLen = 3, Colors = 3, TwoChance = 0.1f, MinArrows = 7 },
            new() { MinLen = 2, MaxLen = 4, Colors = 3, TwoChance = 0.2f, MinArrows = 8 },
            new() { MinLen = 2, MaxLen = 5, Colors = 3, TwoChance = 0.3f, MinArrows = 9 },
            new() { MinLen = 3, MaxLen = 6, Colors = 4, TwoChance = 0.35f, MinArrows = 10 },
            new() { MinLen = 3, MaxLen = 7, Colors = 4, TwoChance = 0.45f, MinArrows = 11 },
        };

        public readonly Board Board = new();
        public readonly Func<float> Rand;
        public int Cleared, Score, Combo, ComboColor = -1;
        public bool Over;

        public EndlessRun(Func<float> rand)
        {
            Rand = rand;
            var d = DifficultyAt(0);
            for (int i = 0; i < StartArrows; i++) Board.Spawn(rand, d.MinLen, d.MaxLen, d.Colors);
        }

        public static Difficulty DifficultyAt(int cleared)
        {
            int stage = 0;
            while (stage + 1 < StageAt.Length && cleared >= StageAt[stage + 1]) stage++;
            var d = Stages[stage];
            d.Stage = stage;
            return d;
        }

        public struct ExitResult
        {
            public Arrow Arrow;
            public int Gained, Combo;
            public bool StageUp;
            public List<Arrow> Spawned;
        }

        /// <summary>Applies a successful exit of a free arrow: score, combo, spawns.</summary>
        public ExitResult Exit(int id)
        {
            var before = DifficultyAt(Cleared).Stage;
            var arrow = Board.Remove(id);
            Combo = arrow.Color == ComboColor ? Math.Min(Combo + 1, ComboMax) : 1;
            ComboColor = arrow.Color;
            var gained = arrow.Cells.Length * Combo;
            Score += gained;
            Cleared++;

            var d = DifficultyAt(Cleared);
            var spawned = new List<Arrow>();
            if (Combo < BreatherCombo)
            {
                var count = Rand() < d.TwoChance ? 2 : 1;
                for (int i = 0; i < count; i++)
                {
                    var a = Board.Spawn(Rand, d.MinLen, d.MaxLen, d.Colors);
                    if (a != null) spawned.Add(a);
                    else Over = true;
                }
            }
            while (!Over && Board.Count < d.MinArrows)
            {
                var a = Board.Spawn(Rand, d.MinLen, d.MaxLen, d.Colors);
                if (a == null) break;
                spawned.Add(a);
            }
            return new ExitResult { Arrow = arrow, Gained = gained, Combo = Combo, StageUp = d.Stage > before, Spawned = spawned };
        }

        public void Miss()
        {
            Combo = 0;
            ComboColor = -1;
        }
    }

    // ---------- baked levels ----------

    [Serializable]
    public class ArrowData
    {
        public int dir;
        public int color;
        public int[] cells; // r0, c0, r1, c1, …
    }

    [Serializable]
    public class LevelData
    {
        public int n, w, h;
        public float coverage;
        public float difficulty; // expected wrong taps for a random tapper
        public ArrowData[] arrows;

        public Board ToBoard()
        {
            var b = new Board(Board.CenteredMask(w, h));
            foreach (var a in arrows)
            {
                var cells = new Pos[a.cells.Length / 2];
                for (int i = 0; i < cells.Length; i++) cells[i] = new Pos(a.cells[i * 2], a.cells[i * 2 + 1]);
                b.Place(new Arrow { Id = b.NextId++, Cells = cells, Dir = a.dir, Color = a.color });
            }
            return b;
        }

        public static LevelData FromBoard(int n, int w, int h, Board b, float coverage)
        {
            var list = new List<ArrowData>();
            foreach (var a in b.Arrows.Values)
            {
                var cells = new int[a.Cells.Length * 2];
                for (int i = 0; i < a.Cells.Length; i++) { cells[i * 2] = a.Cells[i].R; cells[i * 2 + 1] = a.Cells[i].C; }
                list.Add(new ArrowData { dir = a.Dir, color = a.Color, cells = cells });
            }
            return new LevelData { n = n, w = w, h = h, coverage = coverage, arrows = list.ToArray() };
        }

        /// <summary>Expected wrong taps if the player tapped a random arrow until one flies, averaged over random solve orders.</summary>
        public float GuessMistakes(Func<float> rand, int samples = 24)
        {
            double total = 0;
            var free = new List<Arrow>();
            for (int s = 0; s < samples; s++)
            {
                var b = ToBoard();
                while (b.Count > 0)
                {
                    b.FreeArrows(free);
                    total += (double)(b.Count - free.Count) / free.Count;
                    b.Remove(free[(int)(rand() * free.Count)].Id);
                }
            }
            return (float)(total / samples);
        }
    }

    [Serializable]
    public class LevelPack
    {
        public LevelData[] levels;
    }
}
