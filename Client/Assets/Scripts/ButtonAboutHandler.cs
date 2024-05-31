
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonAboutHandler : MonoBehaviour, IPointerClickHandler {

    public DialogHandler dialogHandler;
    public GameObject dialogAbout;

    public void OnPointerClick(PointerEventData eventData) {
        dialogHandler.ShowDialog(dialogAbout);
    }
}