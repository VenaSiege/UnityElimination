using System;
using Elimination;
using UnityEngine;

public class Piece : IDisposable {

    private static readonly string[] TEXTURE_NAMES = {
        "cake_0", "cake_1", "cake_2", "cake_3",
        "jewel_0", "jewel_1", "jewel_2", "jewel_3",
        "jewel_4", "jewel_5",
    };

    private static readonly Sprite[] SPRITES;

    public GameObject gameObject { get; private set; }

    public int X { get; private set; }

    public int Y { get; private set; }

    static Piece() {
        Debug.Assert(TEXTURE_NAMES.Length >= Board.PIECE_CATEGORY_COUNT);
        int count = TEXTURE_NAMES.Length;
        SPRITES = new Sprite[count];
        for (int i = 0; i < count; ++i) {
            SPRITES[i] = Resources.Load<Sprite>($"Images/Pieces/{TEXTURE_NAMES[i]}");
        }
    }

    public Piece(GameObject gameObject, int pieceCategory, bool isTouchable) {
        this.gameObject = gameObject;
        gameObject.name = $"piece_{pieceCategory}";
        gameObject.GetComponent<SpriteRenderer>().sprite = SPRITES[pieceCategory - 1];
        if (isTouchable) {
            // If the piece is touchable, add collider to it for use in RayCast.
            gameObject.AddComponent<BoxCollider2D>().isTrigger = true;
        }
    }

    public void SetXY(int x, int y) {
        X = x;
        Y = y;
    }

    public void Dispose() {
        if (gameObject) {
            UnityEngine.Object.Destroy(gameObject);
            gameObject = null;
        }
    }

}