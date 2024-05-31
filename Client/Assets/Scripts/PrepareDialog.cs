using Elimination.Command;
using UnityEngine;
using UnityEngine.UI;

public class PrepareDialog : MonoBehaviour {
    public Toggle toggleAgainstHuman;
    public Toggle toggleAgainstAI;
    public Button buttonReady;

    public GameObject messageForWaiting;

    private void Start() {
        buttonReady.onClick.AddListener(OnButtonReadyClick);
        messageForWaiting.SetActive(false);
    }

    private void OnDestroy() {
        buttonReady.onClick.RemoveListener(OnButtonReadyClick);
    }

    private async void OnButtonReadyClick() {
        buttonReady.enabled = false;
        toggleAgainstHuman.enabled = false;
        toggleAgainstAI.enabled = false;
        messageForWaiting.SetActive(true);
        await Client.Instance.SendAsync(new GamePrepare() {
            Ready = true,
            BattleAI = toggleAgainstAI.isOn,
        });
    }
}
