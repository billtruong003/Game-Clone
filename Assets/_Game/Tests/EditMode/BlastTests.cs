using CasualGame.EyeBlast;
using NUnit.Framework;

namespace CasualGame.Tests
{
    public class BlastTests
    {
        private static Piece P(params (int, int)[] cells) => BlastBoard.MakePiece(cells, 1);

        [Test]
        public void EmptyBoardPlacesAnyDeal()
        {
            var b = new BlastBoard();
            Assert.IsTrue(b.AllPlaceable(new[] { P((0, 0), (1, 1)), P((0, 0), (0, 1), (0, 2), (0, 3), (0, 4)), P((0, 0), (1, 0), (2, 0)) }));
        }

        [Test]
        public void OrderMattersThroughLineClears()
        {
            // row 0 full except the last cell; a 1×1 there clears the row and makes room for a 1×5 afterwards
            var b = new BlastBoard();
            for (int r = 0; r < BlastBoard.Size; r++)
                for (int c = 0; c < BlastBoard.Size; c++)
                    b.Grid[r, c] = 1;
            b.Grid[0, 7] = 0;
            Assert.IsTrue(b.AllPlaceable(new[] { P((0, 0), (0, 1), (0, 2), (0, 3), (0, 4)), P((0, 0)) }));
            Assert.IsFalse(b.AllPlaceable(new[] { P((0, 0), (0, 1)) }));
        }

        [Test]
        public void DealIsAlwaysFullyPlaceable()
        {
            var b = new BlastBoard();
            var rand = new System.Random(3);
            for (int i = 0; i < 40; i++)
            {
                for (int r = 0; r < BlastBoard.Size; r++)
                    for (int c = 0; c < BlastBoard.Size; c++)
                        b.Grid[r, c] = rand.NextDouble() < 0.45 ? 1 : 0;
                var deal = b.Deal(() => (float)rand.NextDouble() * 0.99999f);
                bool anyRoom = false;
                foreach (var v in b.Grid) if (v == 0) anyRoom = true;
                if (anyRoom) Assert.IsTrue(b.AllPlaceable(deal) || !b.FitsAnywhere(deal[0]), "deal " + i);
            }
        }

        // The bitboard search must answer exactly like the plain grid search it replaced (same order, same budget).
        [Test]
        public void BitboardSearchMatchesGridSearch()
        {
            var b = new BlastBoard();
            var rand = new System.Random(11);
            for (int i = 0; i < 500; i++)
            {
                var fill = 0.3 + 0.5 * rand.NextDouble();
                for (int r = 0; r < BlastBoard.Size; r++)
                    for (int c = 0; c < BlastBoard.Size; c++)
                        b.Grid[r, c] = rand.NextDouble() < fill ? 1 : 0;
                var pieces = new Piece[3];
                for (int k = 0; k < 3; k++) pieces[k] = BlastBoard.MakePiece(BlastBoard.Shapes[rand.Next(BlastBoard.Shapes.Length)], 1);
                int budget = 6000;
                var expected = GridSearch((int[,])b.Grid.Clone(), pieces, new bool[3], ref budget);
                Assert.AreEqual(expected, b.AllPlaceable(pieces), "board " + i);
            }
        }

        private static bool GridSearch(int[,] grid, Piece[] pieces, bool[] used, ref int budget)
        {
            const int n = BlastBoard.Size;
            bool any = false;
            for (int i = 0; i < pieces.Length; i++)
            {
                if (used[i]) continue;
                any = true;
                var p = pieces[i];
                for (int r = 0; r <= n - p.Height; r++)
                    for (int c = 0; c <= n - p.Width; c++)
                    {
                        bool fits = true;
                        foreach (var (pr, pc) in p.Cells) fits &= grid[r + pr, c + pc] == 0;
                        if (!fits) continue;
                        if (--budget < 0) return false;
                        var next = (int[,])grid.Clone();
                        foreach (var (pr, pc) in p.Cells) next[r + pr, c + pc] = 1;
                        var rows = new bool[n];
                        var cols = new bool[n];
                        for (int k = 0; k < n; k++)
                        {
                            bool row = true, col = true;
                            for (int j = 0; j < n; j++) { row &= next[k, j] != 0; col &= next[j, k] != 0; }
                            rows[k] = row;
                            cols[k] = col;
                        }
                        for (int y = 0; y < n; y++)
                            for (int x = 0; x < n; x++)
                                if (rows[y] || cols[x]) next[y, x] = 0;
                        used[i] = true;
                        var ok = GridSearch(next, pieces, used, ref budget);
                        used[i] = false;
                        if (ok) return true;
                    }
            }
            return !any;
        }

        [Test]
        public void FullestLinesPicksTheMostFilled()
        {
            var b = new BlastBoard();
            for (int c = 0; c < 7; c++) b.Grid[2, c] = 1;
            for (int r = 0; r < 6; r++) b.Grid[r, 5] = 1;
            var lines = b.FullestLines(2);
            Assert.AreEqual((true, 2), lines[0]);
            Assert.AreEqual((false, 5), lines[1]);
        }
    }
}
