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
