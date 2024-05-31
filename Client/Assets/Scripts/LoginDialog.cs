using System;
using System.Net;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class LoginDialog : MonoBehaviour {

    public ClientIO clientIO;
    public InputField inputUserName;
    public InputField inputPassword;
    public InputField inputIPAddress;
    public Button buttonLogin;
    public Text toast;
    
    public GameObject loginInfoDialog;
    public GameObject messageDialog;
    private Text _messageDialogText;

    private float _yEndOfToast;

    private void Start() {
        _messageDialogText = messageDialog.GetComponentInChildren<Text>();
        _yEndOfToast = toast.rectTransform.localPosition.y + 200f;
        buttonLogin.onClick.AddListener(OnButtonLoginClick);
        GameManager.Reset();
    }

    private async void OnButtonLoginClick() {
        string userName = inputUserName.text;
        if (string.IsNullOrEmpty(userName)) {
            ShowToast("请输入用户名。");
            return;
        }
        string password = inputPassword.text;
        if (string.IsNullOrEmpty(password)) {
            ShowToast("请输入密码。");
            return;
        }

        string ip = inputIPAddress.text;
        if (string.IsNullOrEmpty(ip)) {
            ShowToast("请输入IP地址。");
            return;
        }

        //string IPAddressTest = "192.168.31.125";//测试用IP

        IPAddress iPAddress;

        bool isIPAddressValid = IPAddress.TryParse(ip, out iPAddress);
        if(isIPAddressValid == false) {
            ShowToast("IP地址无效。");
            return;
        }

        ShowMessageDialog("正在登录到服务器......");

        try {
            await Client.CreateInstance().LoginAsync(
                new IPEndPoint(iPAddress, 20678), // FIXME: Server address
                new Client.LoginData(userName, password));
            clientIO.enabled = true;
            DontDestroyOnLoad(clientIO);
        } catch (Exception e) {
            Debug.LogWarning(e);
            ShowToast(e.Message);
        } finally {
            HideMessageDialog();
        }
    }

    private void ShowToast(string msg) {                //显示提示信息
        GameObject newToast = Instantiate(toast.gameObject, toast.transform.parent);
        Text text = newToast.GetComponent<Text>();
        text.text = msg;
        newToast.SetActive(true);
        Sequence seq = DOTween.Sequence();

        const float DURATION = 1.5f;
        seq.Append(text.rectTransform.DOLocalMoveY(_yEndOfToast, DURATION).SetEase(Ease.OutQuad));
        seq.Insert(1f, text.DOFade(0f, DURATION - 1f));
        seq.OnComplete(() => {
            Destroy(newToast);
        });
    }

    private void ShowMessageDialog(string message) {
        _messageDialogText.text = message;
        messageDialog.SetActive(true);
    }

    private void HideMessageDialog() {
        messageDialog.SetActive(false);
    }
}