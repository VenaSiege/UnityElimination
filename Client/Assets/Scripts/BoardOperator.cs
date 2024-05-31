using System.Collections.Generic;
using DG.Tweening;
using Elimination;
using Elimination.Command;
using UnityEngine;

public class BoardOperator {

    private readonly float _pieceMargin;

    private readonly Board _boardData;
    private readonly GameObject _goBoard;
    private readonly GameObject _goPieceTemplate;
    private readonly bool _myBoard;

    private readonly Dictionary<GameObject, Piece> _goToPieceMapping = new();
    private readonly Piece[,] _piecesByCoordinate;

    private readonly Vector3 _singlePieceWorldSize;

    public bool IsAnimationPlaying { get; private set; }

    public BoardOperator(Board boardData, GameObject goBoard, GameObject goPieceTemplate, bool isMyBoard) {
        _boardData = boardData;
        _goBoard = goBoard;
        _goPieceTemplate = goPieceTemplate;
        _myBoard = isMyBoard;
        _piecesByCoordinate = new Piece[boardData.Width, boardData.Height];

        Vector3 boardScale = _goBoard.transform.lossyScale;
        _singlePieceWorldSize = GetSpriteWorldSize(goPieceTemplate) * boardScale.x;
        _pieceMargin = 0.01f * boardScale.x;
    }

    private static Vector3 GetSpriteWorldSize(GameObject sprite) {
        return sprite.GetComponent<SpriteRenderer>().bounds.size;
    }

    public void Refresh() {

        // Size of all pieces, include margins.
        float cxPieceAndMargin = _singlePieceWorldSize.x + _pieceMargin;
        float cyPieceAndMargin = _singlePieceWorldSize.y + _pieceMargin;
        float allPiecesWidth = _boardData.Width * cxPieceAndMargin + _pieceMargin;

        // Margins between border and tiles.
        Vector3 boardSize = GetSpriteWorldSize(_goBoard);
        float leftMargin = (boardSize.x - allPiecesWidth) / 2f;

        // World position of board left bottom point.
        Vector3 boardLeftBottomPosition = _goBoard.GetComponent<Renderer>().bounds.min;

        float xOffset = boardLeftBottomPosition.x + leftMargin + _singlePieceWorldSize.x * 0.5f;
        float yOffset = _pieceMargin + boardLeftBottomPosition.y + _singlePieceWorldSize.y * 0.5f;
        float pieceZ = _goBoard.transform.position.z + (_myBoard ? -0.5f : 0.5f);
        for (int row = 0; row < _boardData.Height; ++row) {
            for (int col = 0; col < _boardData.Width; ++col) {
                int pieceCategory = _boardData.GetPiece(col, row);
                if (pieceCategory == Board.EMPTY_CELL) {
                    continue;
                }

                Vector3 pos = new() {
                    x = xOffset + col * cxPieceAndMargin,
                    y = yOffset + row * cyPieceAndMargin,
                    z = pieceZ,
                };
                GameObject go = Object.Instantiate(_goPieceTemplate, pos, Quaternion.identity, _goBoard.transform);

                Piece piece = new(go, pieceCategory, _myBoard); // Only my pieces can be touched.
                piece.SetXY(col, row);

                _piecesByCoordinate[col, row] = piece;
                _goToPieceMapping.Add(go, piece);
            }
        }
    }

    public void ClickPiece(GameObject go) {
        if (go && _goToPieceMapping.TryGetValue(go, out Piece piece)) {
            ClickPiece(piece.X, piece.Y);
        }
    }


    public void ClickPiece(int x, int y) {
        List<Board.Pos> connectedPosList = _boardData.GetConnectedPosList(x, y);
        if (connectedPosList == null || connectedPosList.Count < Board.MIN_CONNECTED_COUNT_FOR_ELIMINATION) {
            return;
        }
        List<Board.Moving> fallDownList = _boardData.RemoveAndCollapseDown(connectedPosList);

        if (_myBoard) {
            GameManager.Instance.AddMyScore(_boardData.CalcScore(connectedPosList.Count));
            Client.Instance.SendAsync(new GamePieceClick() { X = x, Y = y });

            // Set the state to 'Animation Playing' ensuring that any touch input is ignored.
            this.IsAnimationPlaying = true;
        }

        Sequence seq = DestroyAllConnectedPieces(connectedPosList);
        seq.OnComplete(() => FallDownPieces(fallDownList));
    }

    private Tween CreateFallDownAnimation(Transform ts, int distanceByCell) {
        const float TIME_FOR_MOVE_ONE_CELL = 0.2f;
        float duration = Mathf.Sqrt(distanceByCell) * TIME_FOR_MOVE_ONE_CELL;
        Vector3 pos = ts.position;
        pos.y -= distanceByCell * (_singlePieceWorldSize.y + _pieceMargin);
        return ts.DOMove(pos, duration).SetEase(Ease.InQuad);
    }

    private void FallDownPieces(List<Board.Moving> fallDownList) {
        Sequence seq = DOTween.Sequence();
        foreach (Board.Moving moving in fallDownList) {
            Board.Pos from = moving.From;
            Board.Pos to = moving.To;
            Piece p = _piecesByCoordinate[from.x, from.y];
            Debug.Assert(p != null, $"Piece {from} not found when fall down, data={_boardData.GetPiece(from.x, from.y)}");
            p.SetXY(to.x, to.y);
            _piecesByCoordinate[from.x, from.y] = null;
            _piecesByCoordinate[to.x, to.y] = p;
            seq.Insert(0f, CreateFallDownAnimation(p.gameObject.transform, from.y - to.y));
        }
        seq.OnComplete(() => {
            // Reset the state after animations are finished.
            this.IsAnimationPlaying = false;
        });
    }

    private Sequence DestroyAllConnectedPieces(List<Board.Pos> connectedPosList) {
        Sequence seq = DOTween.Sequence();
        Vector3 punch = new(0.5f, 0.5f, 0.5f);
        foreach (Board.Pos pos in connectedPosList) {
            Piece p = _piecesByCoordinate[pos.x, pos.y];
            Debug.Assert(p != null, $"Piece {pos} not found when delete connected.");
            Debug.Assert(p.X == pos.x && p.Y == pos.y, $"Coordination not equals, pos={pos}, xy={p.X},{p.Y}");
            _piecesByCoordinate[pos.x, pos.y] = null;
            _goToPieceMapping.Remove(p.gameObject);
            seq.Insert(0f,
                p.gameObject.transform.DOPunchScale(punch, 0.2f)
                    .SetEase(Ease.Flash)
                    .OnComplete(() => p.Dispose()));
        }
        return seq;
    }

#if false
        private static void PrintList<T>(List<T> list) {
            StringBuilder sb = new(1024);
            foreach (T t in list) {
                sb.Append(t.ToString()).Append(',');
            }
            Debug.Log(sb.ToString());
        }
#endif
}