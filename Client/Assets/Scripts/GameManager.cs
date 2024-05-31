using System;
using System.IO;
using Elimination;
using Elimination.Command;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager {
    public static GameManager Instance { get; private set; }

    private bool _loggedIn;

    public string MyName { get; private set; }
    public int MyScore { get; private set; }
    public Board MyBoard { get; private set; }

    public string OpponentName { get; private set; }
    public int OpponentScore { get; private set; }
    public Board OpponentBoard { get; private set; }

    public bool IsMyBoardAnimationPlaying => _boardOperatorMine?.IsAnimationPlaying ?? false;

    public BoardOperator MyBoardOperator => _boardOperatorMine;
    private BoardOperator _boardOperatorMine;

    private BoardOperator _boardOperatorOpponent;

    // We set the constructor to private because it's a singleton.
    private GameManager() { }

    public static void Reset() {
        Instance = new GameManager();
    }

    public void InitBoardOperator(GameObject goMyBoard, GameObject goOpponentBoard, GameObject pieceTemplate) {
        _boardOperatorMine = new BoardOperator(MyBoard, goMyBoard, pieceTemplate, true);
        _boardOperatorMine.Refresh();
        _boardOperatorOpponent = new BoardOperator(OpponentBoard, goOpponentBoard, pieceTemplate, false);
        _boardOperatorOpponent.Refresh();
    }

    /// <summary>
    /// Invoked on a command received from server.
    /// </summary>
    /// <param name="client">Client object.</param>
    /// <param name="cmd">The command.</param>
    public void OnServerCommand(Client client, Cmd cmd) {
        switch (cmd) {
        case LoginResponse loginResponse:
            ProcessLoginResponse(loginResponse);
            break;
        case GameStart gameStart:
            ProcessGameStartCmd(gameStart);
            break;
        case GamePieceClick gpc:
            ProcessGamePieceClick(gpc);
            break;
        case GameOver gameOver:
            ProcessGameOver(gameOver);
            break;
        }
        Beacon.Instance.PublishCommand(client, cmd);
    }

    private void ProcessLoginResponse(LoginResponse response) {
        if (_loggedIn) {
            throw new InvalidOperationException("Already login succeeded.");
        }
        switch (response.Code) {
        case 200:
            Debug.Log("Login to server succeeded.");
            break;
        case 201:
            Debug.Log("Register new account succeeded.");
            break;
        default:
            throw new IOException($"Login failed, the status code is {response.Code}");
        }
        this._loggedIn = true;
        this.MyName = response.UserName;

        SceneManager.LoadScene("Prepare");
    }

    private void ProcessGameStartCmd(GameStart gameStart) {
        int[,] pieces = Board.DeserializePieces(gameStart.Pieces, gameStart.BoardWidth, gameStart.BoardHeight);
        MyBoard = new Board(gameStart.BoardWidth, gameStart.BoardHeight, pieces);
        OpponentBoard = new Board(gameStart.BoardWidth, gameStart.BoardHeight, pieces);

        if (gameStart.PlayerA == this.MyName) {
            this.OpponentName = gameStart.PlayerB;
        } else {
            this.OpponentName = gameStart.PlayerA;
        }
        if (string.IsNullOrEmpty(OpponentName)) {
            OpponentName = "(AI)";
        }
        MyScore = OpponentScore = 0;

        SceneManager.LoadScene("Game");
    }


    private void ProcessGamePieceClick(GamePieceClick gpc) {
        _boardOperatorOpponent.ClickPiece(gpc.X, gpc.Y);
    }

    private void ProcessGameOver(GameOver gameOver) {
        Debug.Log($"Game over, winner is '{gameOver.Winner}', loser is '{gameOver.Loser}'");
    }

    public void AddMyScore(int value) {
        this.MyScore += value;
        Beacon.Instance.PublishMyScoreChange(this, this.MyScore);
    }
}