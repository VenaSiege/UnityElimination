using UnityEngine;

public class MainController : MonoBehaviour {

    /// <summary>
    /// The board of opponent.
    /// </summary>
    public GameObject opponentBoard;

    /// <summary>
    /// The board of mine.
    /// </summary>
    public GameObject myBoard;

    private Camera _mainCamera;

    private GameObject _pieceBox;

    private void Start() {
        _mainCamera = Camera.main;
        GameManager.Instance.InitBoardOperator(myBoard, opponentBoard, Resources.Load<GameObject>("Prefabs/piece_box"));
    }

    private void Update() {
        if (!GameManager.Instance.IsMyBoardAnimationPlaying) {
            CheckTouch();
        }
    }

    private void CheckTouch() {
        if (!CheckTouch(out Vector3 pos)) {
            return;
        }
        RaycastHit2D hit = Physics2D.Raycast(_mainCamera.ScreenToWorldPoint(pos), Vector2.zero);
        Collider2D hitCollider = hit.collider;
        if (!hitCollider) {
            return;
        }
        GameManager.Instance.MyBoardOperator.ClickPiece(hitCollider.gameObject);
    }

    private static bool CheckTouch(out Vector3 pos) {
        if (Input.GetMouseButtonDown(0)) {
            pos = Input.mousePosition;
            return true;
        }
        foreach (Touch touch in Input.touches) {
            if (touch.phase == TouchPhase.Began) {
                pos = touch.position;
                return true;
            }
        }
        pos = Vector3.zero;
        return false;
    }

}