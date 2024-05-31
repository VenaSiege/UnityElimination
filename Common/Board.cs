using System;
using System.Collections.Generic;
using System.Text;

namespace Elimination {

    public class Board {

        /// <summary>
        /// Number of categories for the pieces.
        /// Limited by client resources.
        /// </summary>
        public const int PIECE_CATEGORY_COUNT = 10;

        // Valid piece value is greater than zero.
        // So we define 0 to represent an empty cell.
        public const int EMPTY_CELL = 0;

        /// <summary>
        /// Least 3 connected pieces can be eliminated.
        /// </summary>
        public const int MIN_CONNECTED_COUNT_FOR_ELIMINATION = 3;

        private readonly Random _random = new Random();

        /// <summary>
        /// The width of the game board, representing the number of cells in the horizontal direction.
        /// </summary>
        public readonly int Width;

        /// <summary>
        /// The height of the game board, representing the number of cells in the vertical direction.
        /// </summary>
        public readonly int Height;

        private readonly int[,] _pieces;

        public Board(int width, int height) {
            Width = width;
            Height = height;
            _pieces = new int[width, height];
        }

        public Board(int width, int height, int[,] pieces) {
            Width = width;
            Height = height;
            _pieces = (int[,])pieces.Clone();
        }

        public int[,] ClonePieces() {
            return (int[,])this._pieces.Clone();
        }

        public void ResetRandom(int pieceCategoryCount) {
            int baseValue = _random.Next(PIECE_CATEGORY_COUNT);
            for (int y = 0; y < Height; ++y) {
                for (int x = 0; x < Width; ++x) {
                    // We increase the value because 0 represents an empty cell.
                    _pieces[x, y] = ((_random.Next(pieceCategoryCount) + baseValue) % PIECE_CATEGORY_COUNT) + 1;
                }
            }
        }

        public int GetPiece(int x, int y) {
            return _pieces[x, y];
        }

        public bool IsEqualTo(Board board) {
            if (this == board) return true;
            if (board == null) return false;
            if (this.Width != board.Width || this.Height != board.Height) return false;
            return Array.Equals(this._pieces, board._pieces);
        }

        #region Serialization

        public string SerializePieces() {
            StringBuilder sb = new StringBuilder(Width * Height * 2);
            for (int y = 0; y < Height; ++y) {
                for (int x = 0; x < Width; ++x) {
                    sb.Append(_pieces[x, y].ToString("x2") );
                }
            }
            return sb.ToString();
        }

        public static int[,] DeserializePieces(string s, int width, int height) {
            char[] array = s.ToCharArray();
            int[,] pieces = new int[width, height];
            int idx = 0;
            for (int y = 0; y < height; ++y) {
                for (int x = 0; x < width; ++x) {
                    if (idx >= array.Length - 1) {
                        throw new ArgumentException("Too short input string.");
                    }
                    char high = array[idx++];
                    char low = array[idx++];
                    pieces[x, y] = Utils.ConvertTwoHexCharToNumber(high, low);
                }
            }
            return pieces;
        }

        #endregion

        /// <summary>
        /// Is the cell coordinate valid ?
        /// </summary>
        /// <param name="x">X coordinate for cell.</param>
        /// <param name="y">Y coordinate for cell.</param>
        /// <returns>True if the corrdinate is valid.</returns>
        public bool IsCellCoordinateValid(int x, int y) {
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }

        public int CalcScore(int connectedPiecesCount) {
            if (connectedPiecesCount < MIN_CONNECTED_COUNT_FOR_ELIMINATION) {
                return 0;
            }
            int delta = connectedPiecesCount - MIN_CONNECTED_COUNT_FOR_ELIMINATION;
            double score = connectedPiecesCount * 100 * Math.Pow(1.5, delta);
            return (int)score;
        }

        /// <summary>
        /// Gets a list of positions connected to the specified coordinates.
        /// </summary>
        /// <param name="x">The horizontal coordinate of the position to find connections for.</param>
        /// <param name="y">The vertical coordinate of the position to find connections for.</param>
        /// <returns>
        /// A list of positions connected to the given coordinates.
        /// Returns null if the coordinates are invalid.
        /// </returns>
        public List<Pos> GetConnectedPosList(int x, int y) {
            if (!IsCellCoordinateValid(x, y) || _pieces[x, y] == EMPTY_CELL) {
                return null;
            }
            List<Pos> connectedPosList = new List<Pos>(12);
            VisitedPosRecord visited = new VisitedPosRecord();
            DFS(x, y, _pieces[x, y], connectedPosList, visited);
            return connectedPosList;
        }

        /// <summary>
        /// Performs Depth-First Search (DFS) to find positions connected to the specified coordinates
        /// with the given target value. Populates the connected positions into the provided list.
        /// </summary>
        /// <param name="x">The horizontal coordinate of the starting position.</param>
        /// <param name="y">The vertical coordinate of the starting position.</param>
        /// <param name="targetValue">The target value to find in connected positions.</param>
        /// <param name="connectedIndices">List to store connected positions.</param>
        /// <param name="visited">Record of visited positions to avoid revisiting.</param>
        private void DFS(int x, int y, int targetValue, List<Pos> connectedIndices, VisitedPosRecord visited) {
            if (!IsCellCoordinateValid(x, y) || _pieces[x, y] != targetValue || visited.Contains(x, y)) {
                return;
            }
            visited.Add(x, y);
            connectedIndices.Add(new Pos(x, y));
            DFS(x - 1, y, targetValue, connectedIndices, visited);
            DFS(x + 1, y, targetValue, connectedIndices, visited);
            DFS(x, y - 1, targetValue, connectedIndices, visited);
            DFS(x, y + 1, targetValue, connectedIndices, visited);
        }

        /// <summary>
        /// Tries to find a position that can be clicked for elimination.
        /// </summary>
        /// <param name="pos">When successful, contains the first position of the solution; otherwise, set to (-1, -1).</param>
        /// <returns>True if a solution is found, otherwise false.</returns>
        public bool TryFindSolution(out Pos pos) {
            VisitedPosRecord visitedIncorrectList = new VisitedPosRecord();
            for (int x = 0; x < Width; x++) {
                for (int y = Height - 1; y >= 0; --y) {
                    if (_pieces[x, y] == EMPTY_CELL) {
                        continue;
                    }
                    if (visitedIncorrectList.Contains(x, y)) {
                        continue;
                    }
                    List<Pos> connectedPosList = GetConnectedPosList(x, y);
                    if (connectedPosList == null) {
                        continue;
                    }
                    if (connectedPosList.Count >= MIN_CONNECTED_COUNT_FOR_ELIMINATION) {
                        pos = connectedPosList[0];
                        return true;
                    }
                    foreach (Pos p in connectedPosList) {
                        visitedIncorrectList.Add(p.x, p.y);
                    }
                }
            }
            pos = new Pos(-1, -1);
            return false;
        }

        /// <summary>
        /// Removes and collapses the specified list of positions vertically.
        /// </summary>
        /// <param name="listNeedRemove">The list of positions to be removed.</param>
        /// <returns>
        /// A list of movings representing the cells that have fallen down after removal and collapse.
        /// Returns null if the listNeedRemove is null or empty.
        /// </returns>
        public List<Moving> RemoveAndCollapseDown(IReadOnlyCollection<Pos> listNeedRemove) {
            if (listNeedRemove == null || listNeedRemove.Count == 0) {
                return null;
            }

            // Set the cell to empty and determine the horizontal coordinate range of the removed positions.
            int minX = Width - 1;
            int maxX = 0;
            foreach (Pos pos in listNeedRemove) {
                int x = pos.x;
                int y = pos.y;
                _pieces[x, y] = EMPTY_CELL;
                if (x < minX) {
                    minX = x;
                } 
                if (x > maxX) {
                    maxX = x;
                }
            }

            List<Moving> listMoving = new List<Moving>((maxX - minX + 1) * Height);
            for (int x = minX; x <= maxX; ++x) {
                FallDown(x, listMoving);
            }
            return listMoving;
        }

        private void FallDown(int x, List<Moving> movingList) {
            int src = 0;
            int dst = 0;
            while (src < Height) {
                if (_pieces[x, src] != EMPTY_CELL) {
                    if (dst != src) {
                        _pieces[x, dst] = _pieces[x, src];
                        movingList.Add(new Moving(new Pos(x, src), new Pos(x, dst)));
                    }
                    ++dst;
                }
                ++src;
            }
            for (; dst < Height; ++dst) {
                _pieces[x, dst] = EMPTY_CELL;
            }
        }

        private class VisitedPosRecord {
            private readonly HashSet<int> _visited = new HashSet<int>();

            private static int xy2key(int x, int y) {
                return (x << 16) | y;
            }

            public void Add(int x, int y) {
                _visited.Add(xy2key(x, y));
            }

            public bool Contains(int x, int y) {
                return _visited.Contains(xy2key(x, y));
            }
        }

        public readonly struct Pos {
            public readonly int x;
            public readonly int y;
            public Pos(int x, int y) {
                this.x = x;
                this.y = y;
            }
            public override string ToString() {
                return $"[{x},{y}]";
            }
        }

        public readonly struct Moving {
            public readonly Pos From;
            public readonly Pos To;
            public Moving(Pos from, Pos to) {
                this.From = from;
                this.To = to;
            }
            public override string ToString() {
                return $"({From} -> {To})";
            }
        }
    }
}
