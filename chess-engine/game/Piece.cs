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
        private static readonly AttackDetector _attackDetector = new();

        public PieceType Type { get; private set; }
        public PlayerColor Color { get; private set; }
        public Square Position { get; set; }

        public List<Square> ValidTargetPositions { get; private set; } = [];

        // Keep for backward compatibility
        public List<(int, int)> ValidTargetPostions
        {
            get => ValidTargetPositions.Select(s => (s.Row, s.Col)).ToList();
            set => ValidTargetPositions = value.Select(t => new Square(t.Item1, t.Item2)).ToList();
        }

        public Piece(PieceType type, PlayerColor color, (int, int) position)
        {
            Type = type;
            Color = color;
            Position = new Square(position.Item1, position.Item2);
        }

        public void CalculateValidTargetPositions(Board board, bool reUsePrecalculated = false)
        {
            ValidTargetPositions = Type switch
            {
                PieceType.Pawn => CalculatePawnMoves(board),
                PieceType.Rook => CalculateSlidingMoves(board, Direction.Orthogonal),
                PieceType.Knight => CalculateKnightMoves(board),
                PieceType.Bishop => CalculateSlidingMoves(board, Direction.Diagonal),
                PieceType.Queen => CalculateSlidingMoves(board, Direction.All),
                PieceType.King => CalculateKingMoves(board),
                _ => throw new NotImplementedException($"Move calculation for {Type} not implemented.")
            };
        }

        public int CalculatePositionScore(Board board)
        {
            return Type switch
            {
                PieceType.Pawn => CalculatePawnPositionScore(board),
                PieceType.Knight => CalculateKnightPositionScore(board),
                PieceType.King => CalculateKingPositonScore(board),
                _ => 0
            };
        }

        private int CalculateKingPositonScore(Board board)
        {
            return 0;
        }

        private int CalculateKnightPositionScore(Board board)
        {
            return 0;
        }

        private int CalculatePawnPositionScore(Board board)
        {
            return 0;
            int direction = Color == PlayerColor.White ? -1 : 1;
            var captureDirections = new[] { new Direction(direction, -1), new Direction(direction, 1) };

            foreach (var captureDir in captureDirections)
            {
                var protectionPos = Position + captureDir;

                if (!protectionPos.IsWithinBounds())
                    continue;

                var targetPiece = board.ChessBoard[protectionPos.Row][protectionPos.Col];

                // Normal capture
                if (targetPiece != null && targetPiece.Color == Color)
                {
                    
                }
            }

            return 0;
        }






        

        public bool SquareIsUnderAttack((int, int) position, Board board)
        {
            return _attackDetector.IsSquareUnderAttack(new Square(position.Item1, position.Item2), board, Color);
        }

        private List<Square> CalculateSlidingMoves(Board board, Direction[] directions)
        {
            var moves = new List<Square>();

            foreach (var direction in directions)
            {
                for (int step = 1; step < BoardConstants.BoardSize; step++)
                {
                    var targetPos = new Square(
                        Position.Row + direction.RowDelta * step,
                        Position.Col + direction.ColDelta * step);

                    if (!targetPos.IsWithinBounds())
                        break;

                    var targetPiece = board.ChessBoard[targetPos.Row][targetPos.Col];
                    if (targetPiece == null)
                    {
                        moves.Add(targetPos);
                    }
                    else
                    {
                        if (targetPiece.Color != Color)
                            moves.Add(targetPos);
                        break; // Blocked by any piece
                    }
                }
            }

            return moves;
        }

        private List<Square> CalculateKnightMoves(Board board)
        {
            var moves = new List<Square>();

            foreach (var move in Direction.KnightMoves)
            {
                var targetPos = Position + move;
                if (!targetPos.IsWithinBounds())
                    continue;

                var targetPiece = board.ChessBoard[targetPos.Row][targetPos.Col];
                if (targetPiece == null || targetPiece.Color != Color)
                {
                    moves.Add(targetPos);
                }
            }

            return moves;
        }

        private List<Square> CalculateKingMoves(Board board)
        {
            var moves = new List<Square>();

            // Normal king moves
            foreach (var direction in Direction.All)
            {
                var targetPos = Position + direction;
                if (!targetPos.IsWithinBounds())
                    continue;

                if (_attackDetector.IsSquareUnderAttack(targetPos, board, Color))
                    continue;

                var targetPiece = board.ChessBoard[targetPos.Row][targetPos.Col];
                if (targetPiece == null)
                {
                    moves.Add(targetPos);
                }
                else if (targetPiece.Color != Color && targetPiece.Type != PieceType.King)
                {
                    moves.Add(targetPos);
                }
            }

            // Castling
            if (!_attackDetector.IsSquareUnderAttack(Position, board, Color))
            {
                AddCastlingMoves(board, moves);
            }

            return moves;
        }

        private void AddCastlingMoves(Board board, List<Square> moves)
        {
            // Kingside castling
            if (CanCastleKingside(board))
            {
                var passThroughSquare = new Square(Position.Row, 5);
                var targetSquare = new Square(Position.Row, 6);

                if (IsPathClearForCastling(board, passThroughSquare, targetSquare))
                {
                    moves.Add(new Square(Position.Row, 6));
                }
            }

            // Queenside castling
            if (CanCastleQueenside(board))
            {
                var passThroughSquare1 = new Square(Position.Row, 3);
                var passThroughSquare2 = new Square(Position.Row, 2);
                var extraSquare = new Square(Position.Row, 1);

                if (IsPathClearForCastling(board, passThroughSquare1, passThroughSquare2) &&
                    board.ChessBoard[extraSquare.Row][extraSquare.Col] == null)
                {
                    moves.Add(new Square(Position.Row, 2));
                }
            }
        }

        private bool CanCastleKingside(Board board)
        {
            return (Color == PlayerColor.White && board.CastleRights.Contains(KastleRights.WhiteKingside)) ||
                   (Color == PlayerColor.Black && board.CastleRights.Contains(KastleRights.BlackKingside));
        }

        private bool CanCastleQueenside(Board board)
        {
            return (Color == PlayerColor.White && board.CastleRights.Contains(KastleRights.WhiteQueenside)) ||
                   (Color == PlayerColor.Black && board.CastleRights.Contains(KastleRights.BlackQueenside));
        }

        private bool IsPathClearForCastling(Board board, params Square[] squares)
        {
            foreach (var square in squares)
            {
                if (board.ChessBoard[square.Row][square.Col] != null)
                    return false;

                if (_attackDetector.IsSquareUnderAttack(square, board, Color))
                    return false;
            }
            return true;
        }

        private List<Square> CalculatePawnMoves(Board board)
        {
            var moves = new List<Square>();
            int direction = Color == PlayerColor.White ? -1 : 1;
            int startRow = Color == PlayerColor.White
                ? BoardConstants.WhitePawnStartRank
                : BoardConstants.BlackPawnStartRank;

            // Forward moves
            AddPawnForwardMoves(board, moves, direction, startRow);

            // Captures
            AddPawnCaptures(board, moves, direction);

            return moves;
        }

        private void AddPawnForwardMoves(Board board, List<Square> moves, int direction, int startRow)
        {
            var forwardPos = new Square(Position.Row + direction, Position.Col);

            if (!forwardPos.IsWithinBounds() || board.ChessBoard[forwardPos.Row][forwardPos.Col] != null)
                return;

            moves.Add(forwardPos);

            // Double move from starting position
            if (Position.Row == startRow)
            {
                var doubleForwardPos = new Square(Position.Row + 2 * direction, Position.Col);
                if (board.ChessBoard[doubleForwardPos.Row][doubleForwardPos.Col] == null)
                {
                    moves.Add(doubleForwardPos);
                }
            }
        }

        private void AddPawnCaptures(Board board, List<Square> moves, int direction)
        {
            var captureDirections = new[] { new Direction(direction, -1), new Direction(direction, 1) };

            foreach (var captureDir in captureDirections)
            {
                var capturePos = Position + captureDir;

                if (!capturePos.IsWithinBounds())
                    continue;

                var targetPiece = board.ChessBoard[capturePos.Row][capturePos.Col];

                // Normal capture
                if (targetPiece != null && targetPiece.Color != Color)
                {
                    moves.Add(capturePos);
                }

                // En passant
                if (board.EnPassantTargetSquare != null &&
                    capturePos.Row == board.EnPassantTargetSquare.Value.Item1 &&
                    capturePos.Col == board.EnPassantTargetSquare.Value.Item2)
                {
                    moves.Add(capturePos);
                }
            }
        }
    }
}
