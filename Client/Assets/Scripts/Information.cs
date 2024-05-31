using Elimination.Command;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Information : MonoBehaviour {

    public Text opponentName;
    public Text opponentScore;

    public Text myName;
    public Text myScore;

    public GameObject gameOverDialog;
    public Text winnerName;
    public Text loserName;
    public Button buttonCloseGameOverDialog;

    private void Start() {
        myName.text = $"{GameManager.Instance.MyName}: ";
        opponentName.text = $"{GameManager.Instance.OpponentName}: ";
        myScore.text = ScoreToString(0);
        opponentScore.text = ScoreToString(0);

        Beacon.Instance.CommandPublisher += this.OnCommand;
        Beacon.Instance.MyScoreChangeEvent += this.OnMyScoreChange;
    }

    private void OnDestroy() {
        Beacon.Instance.CommandPublisher -= this.OnCommand;
        Beacon.Instance.MyScoreChangeEvent -= this.OnMyScoreChange;
        buttonCloseGameOverDialog.onClick.RemoveAllListeners();
    }

    private void OnCommand(object sender, Cmd cmd) {
        switch (cmd) {
        case GamePieceClick gpc:
            opponentScore.text = ScoreToString(gpc.TotalScore);
            break;
        case GameOver go:
            ShowGameOverDialog(go);
            break;
        }
    }

    private void ShowGameOverDialog(GameOver go) {
        winnerName.text = GetPlayerName(go.Winner);
        loserName.text = GetPlayerName(go.Loser);
        buttonCloseGameOverDialog.onClick.RemoveAllListeners();
        buttonCloseGameOverDialog.onClick.AddListener(() => {
            SceneManager.LoadSceneAsync("Prepare");
        });
        gameOverDialog.SetActive(true);
    }

    private void OnMyScoreChange(object sender, int score) {
        myScore.text = ScoreToString(score);
    }

    private static string ScoreToString(int score) {
        return score.ToString("N0");
    }

    private static string GetPlayerName(string name) => string.IsNullOrEmpty(name) ? "(AI)" : name;
}