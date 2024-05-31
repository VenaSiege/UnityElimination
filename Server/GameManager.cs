using Elimination;
using Elimination.Command;
using System.Diagnostics;

namespace EliminationServer {

    public class GameManager {

        private enum PlayerState {
            NOT_READY,
            WAITING,
        }

        public static readonly GameManager Instance = new GameManager();
        
        private readonly List<Room> _rooms = new(8);

        private readonly Dictionary<Client, PlayerState> _players = new();
        
        private GameManager() { }

        public void TearDown() {
            foreach (var room in _rooms) {
                room.Dispose();
            }
            _rooms.Clear();
        }

        /// <summary>
        /// Represents a new incoming player. Place it into a container and set its
        /// state to inactive until the GamePrepare message is received.
        /// </summary>
        /// <param name="client">The incoming player.</param>
        public void NewPlayerIncoming(Client client) {
            RepresentPlayerToNotReadyState(client);
        }

        private void RepresentPlayerToNotReadyState(Client player) {
            _players.Add(player, PlayerState.NOT_READY);
        }

        private void CreateRoomAndStartGame(Client playerA, Client? playerB) {
            Board board = new(10, 10);
            board.ResetRandom(5);
            _rooms.Add(Room.CreateAndStartGame(board, playerA, playerB));
        }

        private int FindRoomIndex(Client client) {
            for (int i = 0; i < _rooms.Count; i++) {
                Room room = _rooms[i];
                if (room.Contains(client)) {
                    return i;
                }
            }
            return -1;
        }

        //private Room? FindRoom(Client client) {
        //    int index = FindRoomIndex(client);
        //    return (index < 0) ? null : _rooms[index];
        //}

        public void CloseClient(Client client) {
            int index = FindRoomIndex(client);
            if (index >= 0) {
                Room room = _rooms[index];
                Client? opponent = room.PlayerExit(client);
                room.Dispose();
                _rooms.RemoveAt(index);
                if (opponent != null) {
                    _players.Add(opponent, PlayerState.NOT_READY);
                }
            }
            client.Dispose();
        }

        public void OnGamePrepare(Client client, GamePrepare gamePrepare) {
            var newState = gamePrepare.Ready ? PlayerState.WAITING : PlayerState.NOT_READY;
            if (_players.TryGetValue(client, out var state)) {
                // Start game now when request is battle against AI.
                if (newState == PlayerState.WAITING && gamePrepare.BattleAI) {
                    _players.Remove(client);
                    CreateRoomAndStartGame(client, null);
                    return;
                }
                // Update state and continue waiting for other human player.
                if (state != newState) {
                    _players[client] = newState;
                }
                return;
            }

            // Just put the player into container.
            if (newState != PlayerState.WAITING) {
                _players.Add(client, newState);
                return;
            }

            // Player request battle against other human player.
            // Find a human player to start game.
            foreach (var kv in _players) {
                if (kv.Value == PlayerState.WAITING) {
                    Client player = kv.Key;
                    _players.Remove(player);
                    CreateRoomAndStartGame(player, client);
                    return;
                }
            }

            // There isn't any other player is waiting.
            // Put this plyaer into container for waiting;
            _players.Add(client, newState);
        }

        public void OnGamePieceClick(Client client, GamePieceClick pieceClick) {
            int roomIndex = FindRoomIndex(client);
            if (roomIndex < 0) {
                Console.WriteLine($"Receive 'GamePieceClick' message from ${client}, but room not found.");
                return;
            }

            Room room = _rooms[roomIndex];
            room.OnGamePieceClick(client, pieceClick.X, pieceClick.Y);

            if (room.IsGameOver) {
                Client playerA = room.PlayerA;
                Client? playerB = room.PlayerB;
                room.Dispose();
                _rooms.RemoveAt(roomIndex);

                RepresentPlayerToNotReadyState(playerA);
                if (playerB != null) {
                    RepresentPlayerToNotReadyState(playerB);
                }
            }
        }
    }
}
