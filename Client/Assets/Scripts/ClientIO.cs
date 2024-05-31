using System;
using Elimination.Command;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientIO : MonoBehaviour {
    private async void Start() {
        for (;;) {
            try {
                Client client = Client.Instance;
                Cmd cmd = await client.ReceiveAndProcessSingleCommandAsync();
                GameManager.Instance.OnServerCommand(client, cmd);
            } catch (Exception e) {
                if (e is not OperationCanceledException) {
                    Debug.LogWarning(e);
                }
                break;
            }
        }
        SceneManager.LoadSceneAsync("Login");
    }

    private void OnDestroy() {
        Client.CreateInstance();
    }
}
