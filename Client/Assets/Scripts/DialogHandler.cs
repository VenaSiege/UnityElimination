using UnityEngine;
using UnityEngine.EventSystems;

public class DialogHandler : MonoBehaviour, IPointerDownHandler {
    public void OnPointerDown(PointerEventData eventData) {
        foreach (Transform child in transform) {
            child.gameObject.SetActive(false);
        }
        this.gameObject.SetActive(false);
    }

    public void ShowDialog(GameObject dialog) {
        this.gameObject.SetActive(true);
        dialog.SetActive(true);
    }
}