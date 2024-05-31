using Elimination;
using static Elimination.Board;

namespace Test {

    [TestFixture]
    public class TestBoard {

        #region The board data
        /* 
         * Graph Representation:
         * The graph is a 10x10 grid, where the left bottom is (x=0, y=0), right top is (x=9, y=9), and right bottom is (x=9, y=0).
         * Each cell in the grid is represented by a numerical value.
         * 
         * Grid Configuration:
            5,1,5,3,4,5,5,5,2,3
            4,5,5,1,2,3,4,3,2,1
            2,3,5,5,2,1,2,3,2,5
            3,1,5,5,1,5,5,1,2,3
            5,5,5,1,1,1,4,5,2,1
            2,3,4,5,1,2,2,3,2,5
            5,1,2,3,1,5,5,3,2,3
            4,5,5,1,2,3,4,5,2,1
            2,5,4,2,5,4,2,3,2,4
            2,1,2,2,4,4,4,1,2,4
         * 
         * To automatically generate the array of test pieces, open the TestBoard.xlsx file,
         * then export it as a CSV file named TestBoard.csv.
         * Next, execute the BuildTestBoardSrc.py script, which will output the pieces defined
         * in C# syntax. Copy and paste these definitions into here.
         */
        private static readonly int[,] PIECES = {
            {2, 2, 4, 5, 2, 5, 3, 2, 4, 5},
            {1, 5, 5, 1, 3, 5, 1, 3, 5, 1},
            {2, 4, 5, 2, 4, 5, 5, 5, 5, 5},
            {2, 2, 1, 3, 5, 1, 5, 5, 1, 3},
            {4, 5, 2, 1, 1, 1, 1, 2, 2, 4},
            {4, 4, 3, 5, 2, 1, 5, 1, 3, 5},
            {4, 2, 4, 5, 2, 4, 5, 2, 4, 5},
            {1, 3, 5, 3, 3, 5, 1, 3, 3, 5},
            {2, 2, 2, 2, 2, 2, 2, 2, 2, 2},
            {4, 4, 1, 3, 5, 1, 3, 5, 1, 3},
        };
        #endregion

        private Board _board;

        [SetUp]
        public void SetUp() {
            _board = new Board(PIECES.GetLength(0), PIECES.GetLength(1), PIECES);
        }

        [Test]
        public void TestConnectedList_1() {
            int[,] EXPECTED_REMOVES = {
                { 0, 5 }, { 1, 5 }, { 2, 5 }, { 2, 6 }, { 3, 6 },
                { 2, 7 }, { 3, 7 }, { 1, 8 }, { 2, 8 }, { 2, 9 },
            };
            int[,] EXPECTED_MOVINGS = {
                { 0, 6, 0, 5 }, { 0, 7, 0, 6 }, { 0, 8, 0, 7 }, { 0, 9, 0, 8 },
                { 1, 6, 1, 5 }, { 1, 7, 1, 6 }, { 1, 9, 1, 7 },
                { 3, 8, 3, 6 }, { 3, 9, 3, 7 },
            };
            TestConnectedList(new Pos(0, 5), EXPECTED_REMOVES, EXPECTED_MOVINGS);
        }

        [Test]
        public void TestConnectedList_2() {
            int[,] EXPECTED_REMOVES = {
                { 1, 1 }, { 1, 2 }, { 2, 2 },
            };
            int[,] EXPECTED_MOVINGS = {
                { 1, 3, 1, 1 }, { 1, 4, 1, 2 }, { 1, 5,  1, 3 }, { 1, 6, 1, 4 },
                { 1, 7, 1, 5 }, { 1, 8, 1, 6 }, { 1, 9, 1, 7 },
                { 2, 3, 2, 2 }, { 2, 4, 2, 3 }, { 2, 5,  2, 4 }, { 2, 6, 2, 5 },
                { 2, 7, 2, 6 }, { 2, 8, 2, 7 }, { 2, 9, 2, 8 },
            };
            TestConnectedList(new Pos(1, 1), EXPECTED_REMOVES, EXPECTED_MOVINGS);
        }

        [Test]
        public void TestConnectedList_3() {
            int[,] EXPECTED_REMOVES = {
                { 8, 0 }, { 8, 1 }, { 8, 2 }, { 8, 3 }, { 8, 4  }, { 8, 5 },
                { 8, 6 }, { 8, 7 }, { 8, 8 }, { 8, 9 },
            };
            int[,] EXPECTED_MOVINGS = { };
            TestConnectedList(new Pos(8, 9), EXPECTED_REMOVES, EXPECTED_MOVINGS);
        }

        private void TestConnectedList(Pos clickPos, int[,] expectedRemoves, int[,] expectedMovings) {
            List<Pos> removes = _board.GetConnectedPosList(clickPos.x, clickPos.y);
            //PrintList(removes);
            Assert.That(removes.Count, Is.EqualTo(expectedRemoves.GetLength(0)));
            Assert.That(removes, Is.Unique);
            for (int i = 0; i < expectedRemoves.GetLength(0); ++i) {
                int x = expectedRemoves[i, 0];
                int y = expectedRemoves[i, 1];
                Assert.That(removes, Does.Contain(new Pos(x, y)));
            }

            List<Moving> movings = _board.RemoveAndCollapseDown(removes);
            //PrintList(movings);
            Assert.That(movings, Has.Count.EqualTo(expectedMovings.GetLength(0)));
            Assert.That(movings, Is.Unique);
            for (int i = 0; i < expectedMovings.GetLength(0); i++) {
                int xFrom = expectedMovings[i, 0];
                int yFrom = expectedMovings[i, 1];
                int xTo = expectedMovings[i, 2];
                int yTo = expectedMovings[i, 3];
                Moving moving = new Moving(new Pos(xFrom, yFrom), new Pos(xTo, yTo));
                Assert.That(movings.Contains(moving), Is.True, moving.ToString());
            }

            // Now, we'll assert that all cells above the moved ones are empty.

            Dictionary<int, int> coordinate = new();
            foreach (Moving moving in movings) {
                int x = moving.From.x;
                int y = moving.From.y;
                int yTo = moving.To.y;
                Assert.That(x, Is.EqualTo(moving.To.x));
                Assert.That(y, Is.GreaterThan(yTo));
                Assert.That(yTo, Is.GreaterThanOrEqualTo(0));
                Assert.That(y, Is.LessThan(_board.Height));
                if (!coordinate.TryAdd(x, yTo)) {
                    if (yTo > coordinate[x]) {
                        coordinate[x] = yTo;
                    }
                }
            }

            // Now, the Y value is the max coordinate of the non-empty cell.
            foreach (var kv in coordinate) {
                int x = kv.Key;
                Assert.That(_board.GetPiece(x, kv.Value), Is.Not.EqualTo(EMPTY_CELL));
                for (int y = kv.Value + 1; y < _board.Height; y++) {
                    Assert.That(_board.GetPiece(x, y), Is.EqualTo(EMPTY_CELL));
                }
            }            
        }

        [Test]
        public void TestFindSolution() {
            int[,] PIECES = {
                { 0, 1, 2, 3, 4, },
                { 9, 8, 2, 2, 0, },
                { 8, 7, 6, 5, 4, },
                { 7, 6, 5, 4, 3, },
                { 6, 5, 4, 3, 2, },
            };
            Board board = new Board(PIECES.GetLength(0), PIECES.GetLength(1), PIECES);
            Assert.That(board.TryFindSolution(out Pos pos), Is.True);
            var connectedList = board.GetConnectedPosList(pos.x, pos.y);
            Assert.That(connectedList.Count, Is.GreaterThanOrEqualTo(Board.MIN_CONNECTED_COUNT_FOR_ELIMINATION));
            foreach (Pos p in connectedList) {
                Assert.That(board.GetPiece(pos.x, pos.y), Is.EqualTo(2));
            }            
        }

        private void PrintList<T>(List<T> list) {
            foreach (T t in list) {
                Console.Write(t.ToString());
                Console.Write(", ");
            }
            Console.WriteLine();
        }
    }
}
