namespace chess_engine.game
{


    public enum PieceType
    {
        Pawn,
        Rook,
        Knight,
        Bishop,
        Queen,
        King
    }

    internal class Piece
    {
        public PieceType Type { get; private set; }
        public PlayerColor Color { get; private set; }
        public (int, int) Position { get; set; }

        public List<(int, int)> ValidTargetPostions { get; set; }

        public Piece(PieceType type, PlayerColor color, (int, int) position)
        {
            Type = type;
            Color = color;
            Position = position;
        }

        public void CalculateValidTargetPositions(Board board, bool reUsePrecalculated = false)
        {
            ValidTargetPostions = new List<(int, int)>();

            switch (Type)
            {
                case PieceType.Pawn:
                    CalculatePawnMoves(board);
                    break;
                case PieceType.Rook:
                    CalculateRookMoves(board);
                    break;
                case PieceType.Knight:
                    CalculateKnightMoves(board);
                    break;
                case PieceType.Bishop:
                    CalculateBishopMoves(board);
                    break;
                case PieceType.Queen:
                    CalculateQueenMoves(board);
                    break;
                case PieceType.King:
                    CalculateKingMoves(board);
                    break;
                default:
                    throw new NotImplementedException($"Move calculation for {Type} not implemented.");
            }

        }

        /// <summary>
        /// ToDo: Implement castle
        /// ToDo: Implement promotion
        /// ToDO: En Passant make move handle -> remove captured pawn
        /// </summary>
        /// <param name="board"></param>

        private void CalculateKingMoves(Board board)
        {

            if (!SquareIsUnderAttack(Position, board))
            {
                if (board.CastleRights.Contains(KastleRights.WhiteKingside) && Color.Equals(PlayerColor.White) || board.CastleRights.Contains(KastleRights.BlackKingside) && Color.Equals(PlayerColor.Black))
                {
                    var targetPiece = board.ChessBoard[Position.Item1][Position.Item2 + 1];
                    var targetPiece2 = board.ChessBoard[Position.Item1][Position.Item2 + 2];
                    if (targetPiece == null && targetPiece2 == null && !SquareIsUnderAttack((Position.Item1, 5), board) && !SquareIsUnderAttack((Position.Item1, 6), board))
                    {
                        ValidTargetPostions.Add((Position.Item1, 7));
                    }
                }
                if (board.CastleRights.Contains(KastleRights.WhiteQueenside) && Color.Equals(PlayerColor.White) || board.CastleRights.Contains(KastleRights.BlackQueenside) && Color.Equals(PlayerColor.Black))
                {
                    var targetPiece = board.ChessBoard[Position.Item1][Position.Item2 - 1];
                    var targetPiece2 = board.ChessBoard[Position.Item1][Position.Item2 - 2];
                    if (targetPiece == null && targetPiece2 == null && !SquareIsUnderAttack((Position.Item1, 1), board) && !SquareIsUnderAttack((Position.Item1, 2), board))
                    {
                        ValidTargetPostions.Add((Position.Item1, 2));
                    }
                }
            }
            for (int dirRow = -1; dirRow <= 1; dirRow++)
            {
                for (int dirCol = -1; dirCol <= 1; dirCol++)
                {
                    if (dirRow == 0 && dirCol == 0)
                        continue; // Skip no-move
                    var targetPos = (Position.Item1 + dirRow, Position.Item2 + dirCol);
                    if (!IsWithinBounds(targetPos))
                        continue;

                    // Validate if the target square is not under attack
                    if (SquareIsUnderAttack(targetPos, board))
                        continue;

                    var targetPiece = board.ChessBoard[targetPos.Item1][targetPos.Item2];
                    if (targetPiece == null)
                    {
                        ValidTargetPostions.Add(targetPos);
                    }
                    else
                    {
                        if (targetPiece.Color != Color)
                        {
                            if (targetPiece.Type == PieceType.King)
                                continue; // Can't capture opposing king
                            ValidTargetPostions.Add(targetPos);
                            // Capture
                        }
                    }
                }
            }
        }

        private List<Piece> GetAttackers((int, int) position, Board board)
        {
            List<Piece> attackers = new List<Piece>();

            // Check Knight attacks
            var knightMoves = new List<(int, int)>
            {
                (2, 1), (1, 2), (-1, 2), (-2, 1),
                (-2, -1), (-1, -2), (1, -2), (2, -1)
            };

            foreach (var move in knightMoves)
            {
                var targetPos = (position.Item1 + move.Item1, position.Item2 + move.Item2);
                if (IsWithinBounds(targetPos))
                {
                    var targetPiece = board.ChessBoard[targetPos.Item1][targetPos.Item2];
                    if (targetPiece != null && targetPiece.Color != Color && targetPiece.Type == PieceType.Knight)
                    {
                        attackers.Add(targetPiece);
                    }
                }
            }

            // Check straight lines
            var straightDirections = new List<(int, int)>
            {
                (1, 0), (-1, 0), (0, 1), (0, -1)
            };

            foreach (var direction in straightDirections)
            {
                int step = 1;
                while (true)
                {
                    var targetPos = (position.Item1 + step * direction.Item1, position.Item2 + step * direction.Item2);
                    if (!IsWithinBounds(targetPos))
                        break;
                    var targetPiece = board.ChessBoard[targetPos.Item1][targetPos.Item2];
                    if (targetPiece != null)
                    {
                        if (targetPiece.Color != Color)
                        {
                            if (targetPiece.Type == PieceType.Rook || targetPiece.Type == PieceType.Queen)
                            {
                                attackers.Add(targetPiece);
                            }
                            else if (targetPiece.Type == PieceType.King && step == 1)
                            {
                                attackers.Add(targetPiece);
                            }
                        }
                        break; // Blocked by any piece
                    }
                    step++;
                }
            }

            // Check diagonals
            var diagonalDirections = new List<(int, int)>
            {
                (1, 1), (1, -1), (-1, 1), (-1, -1)
            };

            foreach (var direction in diagonalDirections)
            {
                int step = 1;
                while (true)
                {
                    var targetPos = (position.Item1 + step * direction.Item1, position.Item2 + step * direction.Item2);
                    if (!IsWithinBounds(targetPos))
                        break;
                    var targetPiece = board.ChessBoard[targetPos.Item1][targetPos.Item2];
                    if (targetPiece != null)
                    {
                        if (targetPiece.Color != Color)
                        {
                            if (targetPiece.Type == PieceType.Bishop || targetPiece.Type == PieceType.Queen)
                            {
                                attackers.Add(targetPiece);
                            }
                            else if (targetPiece.Type == PieceType.King && step == 1)
                            {
                                attackers.Add(targetPiece);
                            }
                            else if (targetPiece.Type == PieceType.Pawn)
                            {
                                // Pawns attack diagonally
                                if ((Color == PlayerColor.White && direction.Item1 == -1) ||
                                    (Color == PlayerColor.Black && direction.Item1 == 1))
                                {
                                    if (step == 1)
                                        attackers.Add(targetPiece);
                                }
                            }
                        }
                        break; // Blocked by any piece
                    }
                    step++;
                }
            }

            return attackers;
        }

        public bool SquareIsUnderAttack((int, int) position, Board board)
        {
            // Check Knight attacks
            var knightMoves = new List<(int, int)>
            {
                (2, 1), (1, 2), (-1, 2), (-2, 1),
                (-2, -1), (-1, -2), (1, -2), (2, -1)
            };

            foreach (var move in knightMoves)
            {
                var targetPos = (position.Item1 + move.Item1, position.Item2 + move.Item2);
                if (IsWithinBounds(targetPos))
                {
                    var targetPiece = board.ChessBoard[targetPos.Item1][targetPos.Item2];
                    if (targetPiece != null && targetPiece.Color != Color && targetPiece.Type == PieceType.Knight)
                    {
                        return true;
                    }
                }
            }

            // Check straight lines
            var straightDirections = new List<(int, int)>
            {
                (1, 0), (-1, 0), (0, 1), (0, -1)
            };

            foreach (var direction in straightDirections)
            {
                int step = 1;
                while (true)
                {
                    var targetPos = (position.Item1 + step * direction.Item1, position.Item2 + step * direction.Item2);
                    if (!IsWithinBounds(targetPos))
                        break;
                    var targetPiece = board.ChessBoard[targetPos.Item1][targetPos.Item2];
                    if (targetPiece != null)
                    {
                        if (targetPiece.Color != Color)
                        {
                            if (targetPiece.Type == PieceType.Rook || targetPiece.Type == PieceType.Queen)
                            {
                                return true;
                            }
                            if (targetPiece.Type == PieceType.King && step == 1)
                            {
                                return true;
                            }
                        }
                        break; // Blocked by any piece
                    }
                    step++;
                }
            }

            // Check diagonals
            var diagonalDirections = new List<(int, int)>
            {
                (1, 1), (1, -1), (-1, 1), (-1, -1)
            };

            foreach (var direction in diagonalDirections)
            {
                int step = 1;
                while (true)
                {
                    var targetPos = (position.Item1 + step * direction.Item1, position.Item2 + step * direction.Item2);
                    if (!IsWithinBounds(targetPos))
                        break;
                    var targetPiece = board.ChessBoard[targetPos.Item1][targetPos.Item2];
                    if (targetPiece != null)
                    {
                        if (targetPiece.Color != Color)
                        {
                            if (targetPiece.Type == PieceType.Bishop || targetPiece.Type == PieceType.Queen)
                            {
                                return true;
                            }
                            if (targetPiece.Type == PieceType.King && step == 1)
                            {
                                return true;
                            }
                            if (targetPiece.Type == PieceType.Pawn)
                            {
                                // Pawns attack diagonally
                                if ((Color == PlayerColor.White && direction.Item1 == -1) ||
                                    (Color == PlayerColor.Black && direction.Item1 == 1))
                                {
                                    if (step == 1)
                                        return true;
                                }
                            }
                        }
                        break; // Blocked by any piece
                    }
                    step++;
                }
            }

            return false;
        }

        private void CalculateQueenMoves(Board board)
        {
            for (int dirRow = -1; dirRow <= 1; dirRow++)
            {
                for (int dirCol = -1; dirCol <= 1; dirCol++)
                {
                    if (dirRow == 0 && dirCol == 0)
                        continue; // Skip no-move
                    int step = 1;
                    while (true)
                    {
                        var targetPos = (Position.Item1 + step * dirRow, Position.Item2 + step * dirCol);
                        if (!IsWithinBounds(targetPos))
                            break;
                        var targetPiece = board.ChessBoard[targetPos.Item1][targetPos.Item2];
                        if (targetPiece == null)
                        {
                            ValidTargetPostions.Add(targetPos);
                        }
                        else
                        {
                            if (targetPiece.Color != Color)
                            {
                                ValidTargetPostions.Add(targetPos);
                                // Capture
                            }
                            break; // Blocked by any piece
                        }
                        step++;
                    }
                }
            }
        }

        private void CalculateBishopMoves(Board board)
        {
            for (int dirRow = -1; dirRow <= 1; dirRow++)
            {
                for (int dirCol = -1; dirCol <= 1; dirCol++)
                {
                    if (Math.Abs(dirRow) + Math.Abs(dirCol) != 2)
                        continue; // Skip non-diagonals
                    int step = 1;
                    while (true)
                    {
                        var targetPos = (Position.Item1 + step * dirRow, Position.Item2 + step * dirCol);
                        if (!IsWithinBounds(targetPos))
                            break;
                        var targetPiece = board.ChessBoard[targetPos.Item1][targetPos.Item2];
                        if (targetPiece == null)
                        {
                            ValidTargetPostions.Add(targetPos);
                        }
                        else
                        {
                            if (targetPiece.Color != Color)
                            {
                                ValidTargetPostions.Add(targetPos);
                                // Capture
                            }
                            break; // Blocked by any piece
                        }
                        step++;
                    }
                }
            }
        }

        private void CalculateKnightMoves(Board board)
        {
            var knightMoves = new List<(int, int)>
            {
                (2, 1), (1, 2), (-1, 2), (-2, 1),
                (-2, -1), (-1, -2), (1, -2), (2, -1)
            };

            foreach (var move in knightMoves)
            {
                var targetPos = (Position.Item1 + move.Item1, Position.Item2 + move.Item2);
                if (IsWithinBounds(targetPos))
                {
                    var targetPiece = board.ChessBoard[targetPos.Item1][targetPos.Item2];
                    if (targetPiece == null)
                    {
                        ValidTargetPostions.Add(targetPos);
                    }
                    else if (targetPiece != null && targetPiece.Color != Color)
                    {
                        ValidTargetPostions.Add(targetPos);
                    }
                }
            }
        }

        private void CalculateRookMoves(Board board)
        {
            for (int dirRow = -1; dirRow <= 1; dirRow++)
            {
                for (int dirCol = -1; dirCol <= 1; dirCol++)
                {
                    if (Math.Abs(dirRow) + Math.Abs(dirCol) != 1)
                        continue; // Skip diagonals and no-move
                    int step = 1;
                    while (true)
                    {
                        var targetPos = (Position.Item1 + step * dirRow, Position.Item2 + step * dirCol);
                        if (!IsWithinBounds(targetPos))
                            break;
                        var targetPiece = board.ChessBoard[targetPos.Item1][targetPos.Item2];
                        if (targetPiece == null)
                        {
                            ValidTargetPostions.Add(targetPos);
                        }
                        else
                        {
                            if (targetPiece.Color != Color)
                            {
                                ValidTargetPostions.Add(targetPos);
                                // Capture
                            }
                            break; // Blocked by any piece
                        }
                        step++;
                    }
                }
            }
        }

        private void CalculatePawnMoves(Board board)
        {
            // Pawns move differently based on color
            int direction = Color == PlayerColor.White ? -1 : 1;
            int startRow = Color == PlayerColor.White ? 6 : 1;

            // Single square move
            var forwardPos = (Position.Item1 + direction, Position.Item2);
            if (IsWithinBounds(forwardPos) && board.ChessBoard[forwardPos.Item1][forwardPos.Item2] == null)
            {
                ValidTargetPostions.Add(forwardPos);
                // Double square move from starting position
                if (Position.Item1 == startRow)
                {
                    var doubleForwardPos = (Position.Item1 + 2 * direction, Position.Item2);
                    if (board.ChessBoard[doubleForwardPos.Item1][doubleForwardPos.Item2] == null)
                    {
                        ValidTargetPostions.Add(doubleForwardPos);
                    }
                }
            }

            // Captures
            var captureOffsets = new List<(int, int)> { (direction, -1), (direction, 1) };
            foreach (var offset in captureOffsets)
            {
                var capturePos = (Position.Item1 + offset.Item1, Position.Item2 + offset.Item2);
                if (IsWithinBounds(capturePos))
                {
                    var targetPiece = board.ChessBoard[capturePos.Item1][capturePos.Item2];
                    if (targetPiece != null && targetPiece.Color != Color)
                    {
                        ValidTargetPostions.Add(capturePos);
                    }

                    if (board.EnPassantTargetSquare != null &&
                       capturePos == board.EnPassantTargetSquare)
                    {
                        ValidTargetPostions.Add(capturePos);
                    }
                }
            }
        }

        private bool IsWithinBounds((int, int) position)
        {
            return position.Item1 >= 0 && position.Item1 < 8 && position.Item2 >= 0 && position.Item2 < 8;
        }
    }
}
