namespace chess_engine.game
{
    /// <summary>
    /// Detects attacks on squares from enemy pieces.
    /// </summary>
    internal class AttackDetector
    {
        public bool IsSquareUnderAttack(Square position, Board board, PlayerColor defendingColor)
        {
            return IsAttackedByKnight(position, board, defendingColor) ||
                   IsAttackedBySlider(position, board, defendingColor, Direction.Orthogonal, PieceType.Rook) ||
                   IsAttackedBySlider(position, board, defendingColor, Direction.Diagonal, PieceType.Bishop) ||
                   IsAttackedByKing(position, board, defendingColor) ||
                   IsAttackedByPawn(position, board, defendingColor);
        }

        private static bool IsAttackedByKnight(Square position, Board board, PlayerColor defendingColor)
        {
            foreach (var move in Direction.KnightMoves)
            {
                var targetPos = position + move;
                if (!targetPos.IsWithinBounds())
                    continue;

                var piece = board.ChessBoard[targetPos.Row][targetPos.Col];
                if (piece != null && piece.Color != defendingColor && piece.Type == PieceType.Knight)
                    return true;
            }
            return false;
        }

        private static bool IsAttackedBySlider(Square position, Board board, PlayerColor defendingColor,
            Direction[] directions, PieceType sliderType)
        {
            foreach (var direction in directions)
            {
                for (int step = 1; step < BoardConstants.BoardSize; step++)
                {
                    var targetPos = new Square(
                        position.Row + direction.RowDelta * step,
                        position.Col + direction.ColDelta * step);

                    if (!targetPos.IsWithinBounds())
                        break;

                    var piece = board.ChessBoard[targetPos.Row][targetPos.Col];
                    if (piece == null)
                        continue;

                    if (piece.Color != defendingColor)
                    {
                        if (piece.Type == sliderType || piece.Type == PieceType.Queen)
                            return true;

                        if (piece.Type == PieceType.King && step == 1)
                            return true;
                    }
                    break; // Blocked by any piece
                }
            }
            return false;
        }

        private static bool IsAttackedByKing(Square position, Board board, PlayerColor defendingColor)
        {
            foreach (var direction in Direction.All)
            {
                var targetPos = position + direction;
                if (!targetPos.IsWithinBounds())
                    continue;

                var piece = board.ChessBoard[targetPos.Row][targetPos.Col];
                if (piece != null && piece.Color != defendingColor && piece.Type == PieceType.King)
                    return true;
            }
            return false;
        }

        private static bool IsAttackedByPawn(Square position, Board board, PlayerColor defendingColor)
        {
            // Pawns attack diagonally toward the defending side
            int pawnDirection = defendingColor == PlayerColor.White ? -1 : 1;
            var pawnAttackDirections = new Direction[]
            {
                new(pawnDirection, -1),
                new(pawnDirection, 1)
            };

            foreach (var direction in pawnAttackDirections)
            {
                var targetPos = position + direction;
                if (!targetPos.IsWithinBounds())
                    continue;

                var piece = board.ChessBoard[targetPos.Row][targetPos.Col];
                if (piece != null && piece.Color != defendingColor && piece.Type == PieceType.Pawn)
                    return true;
            }
            return false;
        }
    }
}
