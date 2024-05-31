using Elimination;
using Elimination.Command;
using System.Diagnostics;

namespace EliminationServer {
    public class Room : IDisposable {

        /// <summary>
        /// Does the game has been started?
        /// </summary>
        private bool _started;

        /// <summary>
        /// The first player, it's definitely a human player.
        /// </summary>
        public Client PlayerA {
            get {
                Debug.Assert(_boardA.Player != null);
                return _boardA.Player;
            }
        }

        /// <summary>
        /// The second player, may be null when it's an AI player.
        /// </summary>
        public Client? PlayerB => _boardB.Player;

        /// <summary>
        /// The board of the first player.
        /// </summary>
        private readonly BoardEx _boardA;

        /// <summary>
        /// The board of the second player.
        /// </summary>
        private readonly BoardEx _boardB;

        /// <summary>
        /// Is the game over?
        /// </summary>
        public bool IsGameOver => _boardA.IsOver && _boardB.IsOver;
        
        /// <summary>
        /// When the second player is an AI, there is a timer to automatically play the game.
        /// </summary>
        private Timer? _timerAI;

        public static Room CreateAndStartGame(Board board, Client playerA, Client? playerB) {
            Room room = new Room(board, playerA, playerB);
            room.StartGame();
            return room;
        }

        private Room(Board board, Client playerA, Client? playerB) {
            _boardA = new(playerA, board);
            _boardB = new(playerB, board);
        }

        public void Dispose() {
            if (_timerAI != null) {
                _timerAI.Dispose();
                _timerAI = null;
            }
        }

        public bool Contains(Client client) {
            return PlayerA == client || PlayerB == client;
        }

        private void StartGame() {
            Debug.Assert(!_started);
            _started = true;

            GameStart gs = new() {
                BoardWidth = _boardA.Width,
                BoardHeight = _boardA.Height,
                Pieces = _boardA.SerializePieces(),
                PlayerA = PlayerA.UserName,
                PlayerB = PlayerB?.UserName,
            };

            SendCmdToPlayer(PlayerA, gs);
            SendCmdToPlayer(PlayerB, gs);

            Debug.Assert(PlayerA != null);
            if (PlayerB == null) {
                this._timerAI = new(OnTimerAI, this, 1000, 1000);
            }
        }

        private static void SendCmdToPlayer(Client? player, Cmd cmd) {
            if (player == null) {
                return;
            }
            try {
                player.Send(cmd);
            } catch (IOException) {
                WorkQueue.Instance.Post(() => {
                    GameManager.Instance.CloseClient(player);
                });
                throw;
            }
        }

        private void OnTimerAI(object? state) {
            WorkQueue.Instance.Post(() => {
                if (!_boardB.TryFindSolution(out var pos)) {
                    _boardB.IsOver = true;
                    if (_timerAI != null) {
                        _timerAI.Dispose();
                        _timerAI = null;
                    }
                    return;
                }
                this.OnGamePieceClick(PlayerB, pos.x, pos.y);
            });
        }

        private Client? GetOpponent(Client? who) {
            if (who == PlayerA) {
                return PlayerB;
            } else if (who == PlayerB) {
                return PlayerA;
            } else {
                Debug.Assert(false, $"{who} is not in the room.");
                return null;
            }
        }

        private void GetBoard(Client? owner, out BoardEx myBoard, out BoardEx opponentBoard) {
            if (owner == PlayerA) {
                myBoard = _boardA;
                opponentBoard = _boardB;
            } else if (owner == PlayerB) {
                myBoard = _boardB;
                opponentBoard = _boardA;
            } else {
                throw new ApplicationException($"{owner} is not in the room.");
            }
        }

        public void OnGamePieceClick(Client? who, int x, int y) {
            GetBoard(who, out BoardEx myBoard, out BoardEx opponentBoard);
            if (myBoard.IsOver) {
                return;
            }

            Client? opponent = GetOpponent(who);

            List<Board.Pos> connectedList = myBoard.GetConnectedPosList(x, y);
            if (connectedList == null || connectedList.Count < Board.MIN_CONNECTED_COUNT_FOR_ELIMINATION) {
                return;
            }
            myBoard.RemoveAndCollapseDown(connectedList);

            int thisScore = myBoard.CalcScore(connectedList.Count);
            myBoard.Score += thisScore;

            if (opponent != null) {
                // Notify opponent if it's a human player.
                GamePieceClick gpc = new GamePieceClick {
                    Player = who?.UserName,
                    X = x,
                    Y = y,
                    ThisScore = thisScore,
                    TotalScore = myBoard.Score,
                };
                opponent.Send(gpc);
            }

            // If no solution exists, set set the game over flag.
            if (!myBoard.TryFindSolution(out var _)) {
                myBoard.IsOver = true;
                if (opponentBoard.IsOver) {
                    // The current round of the game is over.
                    GameOver go = new GameOver();
                    if (myBoard.Score > opponentBoard.Score) {
                        go.Winner = myBoard.PlayerName;
                        go.Loser = opponentBoard.PlayerName;
                    } else if (myBoard.Score < opponentBoard.Score) {
                        go.Winner = opponentBoard.PlayerName;
                        go.Loser = myBoard.PlayerName;
                    } else {
                        go.Winner = go.Loser = null;
                    }
                    PlayerA?.Send(go);
                    PlayerB?.Send(go);
                }
            }
        }

        public Client? PlayerExit(Client who) {
            GetBoard(who, out BoardEx myBoard, out BoardEx opponentBoard);
            Client? opponent = opponentBoard.Player;
            if (opponent != null) {
                // TODO:
                //      1. Notify this player that your opponent has beed exit.
                //      2. Create a new room and move this player into.
            }
            return opponent;
        }


        private class BoardEx : Board {

            public Client? Player { get; set; }

            /// <summary>
            /// Is no solution exists in this board?
            /// </summary>
            public bool IsOver { get; set; }

            /// <summary>
            /// The score of this board/player.
            /// </summary>
            public int Score { get; set; }

            public string? PlayerName => Player?.UserName;

            public BoardEx(Client? player, Board board)
                : base(board.Width, board.Height, board.ClonePieces()) {
                this.Player = player;
            }
        }
    }
}
