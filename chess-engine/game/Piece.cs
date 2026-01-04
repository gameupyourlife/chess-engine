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

        public int GetPieceValue()
        {
            return Type switch
            {
                PieceType.Pawn => 100,
                PieceType.Knight => 320,
                PieceType.Bishop => 330,
                PieceType.Rook => 500,
                PieceType.Queen => 900,
                PieceType.King => 2000000,
                _ => 0,
            };
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
                PieceType.Bishop => CalculateBishopPositionScore(board),
                PieceType.Rook => CalculateRookPositionScore(board),
                PieceType.Queen => CalculateQueenPositionScore(board),
                PieceType.King => CalculateKingPositonScore(board),
                _ => 0
            };
        }

        private int CalculateKingPositonScore(Board board)
        {
            var pieceChessBoardScores = new int[][]
            {
                [-30, -40, -40, -50, -50, -40, -40, -30],
                [-30, -40, -40, -50, -50, -40, -40, -30],
                [-30, -40, -40, -50, -50, -40, -40, -30],
                [-30, -40, -40, -50, -50, -40, -40, -30],
                [-20, -30, -30, -40, -40, -30, -30, -20],
                [-10, -20, -20, -20, -20, -20, -20, -10],
                [ 20,  20,   0,   0,   0,   0,  20,  20],
                [ 20,  30,  10,   0,   0,  10,  30,  20]
            };

            // Late game king heatmap: encourage centralization and activity
            var pieceChessBoardScoresLategame = new int[][]
            {
                new int[] { -50, -40, -30, -20, -20, -30, -40, -50 },
                new int[] { -30, -20, -10,   0,   0, -10, -20, -30 },
                new int[] { -30, -10,  20,  30,  30,  20, -10, -30 },
                new int[] { -30, -10,  30,  40,  40,  30, -10, -30 },
                new int[] { -30, -10,  30,  40,  40,  30, -10, -30 },
                new int[] { -30, -10,  20,  30,  30,  20, -10, -30 },
                new int[] { -30, -30,   0,   0,   0,   0, -30, -30 },
                new int[] { -50, -30, -30, -30, -30, -30, -30, -50 }
            };

            int colorCorrectedRow = Color == PlayerColor.White ? Position.Row : 7 - Position.Row;
            bool isEndgame = IsEndgame(board);
            var table = isEndgame ? pieceChessBoardScoresLategame : pieceChessBoardScores;
            return table[colorCorrectedRow][Position.Col];
        }

        // Simple endgame detection: true if both sides have no queens or only minor pieces left
        private bool IsEndgame(Board board)
        {
            int queenCount = 0;
            int minorMajorCount = 0;
            for (int r = 0; r < BoardConstants.BoardSize; r++)
            {
                for (int c = 0; c < BoardConstants.BoardSize; c++)
                {
                    var piece = board.ChessBoard[r][c];
                    if (piece == null) continue;
                    if (piece.Type == PieceType.Queen) queenCount++;
                    if (piece.Type == PieceType.Queen || piece.Type == PieceType.Rook || piece.Type == PieceType.Knight || piece.Type == PieceType.Bishop)
                        minorMajorCount++;
                }
            }
            // Endgame if no queens or only one major piece left
            return queenCount == 0 || minorMajorCount <= 3;
        }

        private int CalculateQueenPositionScore(Board board)
        {
            var pieceChessBoardScores = new int[][]
            {
                [-20, -10, -10,  -5,  -5, -10, -10, -20],
                [-10,   0,   0,   0,   0,   0,   0, -10],
                [-10,   0,   5,   5,   5,   5,   0, -10],
                [ -5,   0,   5,   5,   5,   5,   0,  -5],
                [ -5,   0,   5,   5,   5,   5,   0,  -5],
                [-10,   5,   5,   5,   5,   5,   0, -10],
                [-10,   0,   0,   0,   0,   0,   0, -10],
                [-20, -10, -10,  -5,  -5, -10, -10, -20]
            };

            int colorCorrectedRow = Color == PlayerColor.White ? Position.Row : 7 - Position.Row;
            return pieceChessBoardScores[colorCorrectedRow][Position.Col];
        }

        private int CalculateRookPositionScore(Board board)
        {
            var pieceChessBoardScores = new int[][]
            {
                [ 5,  5,  5,  5,  5,  5,  5,  5],
                [ 5, 10, 10, 10, 10, 10, 10,  5],
                [-5,  0,  0,  0,  0,  0,  0, -5],
                [-5,  0,  0,  0,  0,  0,  0, -5],
                [-5,  0,  0,  0,  0,  0,  0, -5],
                [-5,  0,  0,  0,  0,  0,  0, -5],
                [-5,  0,  0,  0,  0,  0,  0, -5],
                [ 0,  0,  0,  5,  5,  0,  0,  0]
            };

            int colorCorrectedRow = Color == PlayerColor.White ? Position.Row : 7 - Position.Row;
            return pieceChessBoardScores[colorCorrectedRow][Position.Col];
        }

        private int CalculateBishopPositionScore(Board board)
        {
            var pieceChessBoardScores = new int[][]
            {
                [-20, -10, -10, -10, -10, -10, -10, -20],
                [-10,   0,   0,   0,   0,   0,   0, -10],
                [-10,   0,   5,  10,  10,   5,   0, -10],
                [-10,   5,   5,  10,  10,   5,   5, -10],
                [-10,   0,  10,  10,  10,  10,   0, -10],
                [-10,  10,  10,  10,  10,  10,  10, -10],
                [-10,  20,   0,   5,   5,   0,  20, -10],
                [-20, -10, -10, -10, -10, -10, -10, -20]
            };

            int colorCorrectedRow = Color == PlayerColor.White ? Position.Row : 7 - Position.Row;
            return pieceChessBoardScores[colorCorrectedRow][Position.Col];
        }

        private int CalculatePawnPositionScore(Board board)
        {
            int score = 0;
            int direction = Color == PlayerColor.White ? -1 : 1;
            var captureDirections = new[] { new Direction(direction, -1), new Direction(direction, 1) };

            foreach (var captureDir in captureDirections)
            {
                var protectionPos = Position + captureDir;

                if (!protectionPos.IsWithinBounds())
                    continue;

                var targetPiece = board.ChessBoard[protectionPos.Row][protectionPos.Col];

                if (targetPiece != null && targetPiece.Color == Color)
                {
                    score += targetPiece.Type switch
                    {
                        PieceType.King => 0,
                        PieceType.Pawn => (int)Math.Round(targetPiece.GetPieceValue() * 0.05),
                        _ => (int)Math.Round(targetPiece.GetPieceValue() * 0.01),
                    };
                }
            }

            var pieceChessBoardScores = new int[][]
            {
                [60, 60, 60, 60, 60, 60, 60, 60],
                [ 5,  6,  7,  8,  8,  7,  6,  5],
                [ 4,  5,  6,  7,  7,  6,  5,  4],
                [ 3, 4,10,20,20,10,4,3],
                [ 2, 3, 10, 20, 20, 10, 3, 2],
                [ 1, 2, 7, 7, 7,7,2,1],
                [ 0, 0,0,0,0,0,0,0],
                [ 0, 0,0,0,0,0,0,0],
            };

            int colorCorrectedRow = Color == PlayerColor.White ? Position.Row : 7 - Position.Row;
            score += pieceChessBoardScores[colorCorrectedRow][Position.Col];


            return score;
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
        
        private int CalculateKnightPositionScore(Board board)
        {
            int score = 0;
            
            // Protection bonus
            foreach (var move in Direction.KnightMoves)
            {
                var targetPos = Position + move;
                if (!targetPos.IsWithinBounds())
                    continue;

                var targetPiece = board.ChessBoard[targetPos.Row][targetPos.Col];
                if (targetPiece != null && targetPiece.Color == Color)
                {
                    score += targetPiece.Type switch
                    {
                        PieceType.King => 0,
                        _ => (int)Math.Round(targetPiece.GetPieceValue() * 0.01),
                    };
                }
            }

            // Position heatmap
            var pieceChessBoardScores = new int[][]
            {
                [-50, -40, -30, -30, -30, -30, -40, -50],
                [-40, -20,   0,   0,   0,   0, -20, -40],
                [-30,   0,  10,  15,  15,  10,   0, -30],
                [-30,   5,  15,  20,  20,  15,   5, -30],
                [-30,   0,  15,  20,  20,  15,   0, -30],
                [-30,   5,  10,  15,  15,  10,   5, -30],
                [-40, -20,   0,   5,   5,   0, -20, -40],
                [-50, -40, -30, -30, -30, -30, -40, -50]
            };

            int colorCorrectedRow = Color == PlayerColor.White ? Position.Row : 7 - Position.Row;
            score += pieceChessBoardScores[colorCorrectedRow][Position.Col];

            return score;
        }
    }
}
