using chess_engine.game;

namespace chess_engine.engine
{
    internal class Evaluator
    {
        private readonly MoveGenerator _moveGenerator = new();
        private static readonly AttackDetector _attackDetector = new();
        private readonly TranspositionTable _transpositionTable = new();
        
        public int NumOfVisitedNodes { get; private set; } = 0;
        public int NumOfAdvancedDeapthNodes { get; private set; } = 0;
        public int TranspositionHits => _transpositionTable.Hits;
        public int TranspositionStores => _transpositionTable.Stores;

        /// <summary>
        /// Clears the transposition table. Call this when starting a new game.
        /// </summary>
        public void ClearTranspositionTable() => _transpositionTable.Clear();

        /// <summary>
        /// Resets search statistics for a new search.
        /// </summary>
        public void ResetStats()
        {
            NumOfVisitedNodes = 0;
            NumOfAdvancedDeapthNodes = 0;
            _transpositionTable.ResetStats();
        }

        public int EvaluatePosition(Board board, PlayerColor evaluatingColor)
        {
            int totalScore = 0;
            int queenCount = 0;
            int minorMajorCount = 0;
            Piece? ourKing = null;
            Piece? enemyKing = null;

            // Single pass: calculate piece values, position scores, and gather endgame info
            for (int rank = 0; rank < BoardConstants.BoardSize; rank++)
            {
                for (int file = 0; file < BoardConstants.BoardSize; file++)
                {
                    var piece = board.ChessBoard[rank][file];
                    if (piece == null) continue;

                    // Track for endgame detection
                    if (piece.Type == PieceType.Queen) queenCount++;
                    if (piece.Type == PieceType.Queen || piece.Type == PieceType.Rook ||
                        piece.Type == PieceType.Knight || piece.Type == PieceType.Bishop)
                        minorMajorCount++;

                    int pieceValue = GetPieceValue(piece.Type);

                    // Handle kings separately (need endgame status for their position score)
                    if (piece.Type == PieceType.King)
                    {
                        if (piece.Color == evaluatingColor)
                            ourKing = piece;
                        else
                            enemyKing = piece;
                        
                        totalScore += piece.Color == evaluatingColor ? pieceValue : -pieceValue;
                        continue;
                    }

                    int positionalScore = CalculatePositionScoreNonKing(piece, board);
                    int combinedScore = pieceValue + positionalScore;

                    totalScore += piece.Color == evaluatingColor ? combinedScore : -combinedScore;
                }
            }

            // Now calculate king scores with endgame knowledge
            bool isEndgame = queenCount == 0 || minorMajorCount <= 3;
            
            if (ourKing != null)
            {
                totalScore += CalculateKingPositionScore(ourKing, isEndgame);
            }
            
            if (enemyKing != null)
            {
                totalScore -= CalculateKingPositionScore(enemyKing, isEndgame);

                // Check bonus for enemy king
                if (_attackDetector.IsSquareUnderAttack(enemyKing.Position, board, enemyKing.Color))
                {
                    totalScore += 70;
                }
            }

            return totalScore;
        }

        private static int GetPieceValue(PieceType type)
        {
            return type switch
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

        private static int CalculatePositionScoreNonKing(Piece piece, Board board)
        {
            return piece.Type switch
            {
                PieceType.Pawn => CalculatePawnPositionScore(piece, board),
                PieceType.Knight => CalculateKnightPositionScore(piece, board),
                PieceType.Bishop => CalculateBishopPositionScore(piece, board),
                PieceType.Rook => CalculateRookPositionScore(piece, board),
                PieceType.Queen => CalculateQueenPositionScore(piece),
                _ => 0
            };
        }

        #region Piece-Square Tables (Static for Performance)
        
        private static readonly int[][] PawnTable =
        [
            [60, 60, 60, 60, 60, 60, 60, 60],
            [ 5,  6,  7,  8,  8,  7,  6,  5],
            [ 4,  5,  6,  7,  7,  6,  5,  4],
            [ 3,  4, 10, 20, 20, 10,  4,  3],
            [ 2,  3, 10, 20, 20, 10,  3,  2],
            [ 1,  2,  7,  7,  7,  7,  2,  1],
            [ 0,  0,  0,  0,  0,  0,  0,  0],
            [ 0,  0,  0,  0,  0,  0,  0,  0],
        ];

        private static readonly int[][] KnightTable =
        [
            [-50, -40, -30, -30, -30, -30, -40, -50],
            [-40, -20,   0,   0,   0,   0, -20, -40],
            [-30,   0,  10,  15,  15,  10,   0, -30],
            [-30,   5,  15,  20,  20,  15,   5, -30],
            [-30,   0,  15,  20,  20,  15,   0, -30],
            [-30,   5,  10,  15,  15,  10,   5, -30],
            [-40, -20,   0,   5,   5,   0, -20, -40],
            [-50, -40, -30, -30, -30, -30, -40, -50]
        ];

        private static readonly int[][] BishopTable =
        [
            [-20, -10, -10, -10, -10, -10, -10, -20],
            [-10,   0,   0,   0,   0,   0,   0, -10],
            [-10,   0,   5,  10,  10,   5,   0, -10],
            [-10,   5,   5,  10,  10,   5,   5, -10],
            [-10,   0,  10,  10,  10,  10,   0, -10],
            [-10,  10,  10,  10,  10,  10,  10, -10],
            [-10,  20,   0,   5,   5,   0,  20, -10],
            [-20, -10, -10, -10, -10, -10, -10, -20]
        ];

        private static readonly int[][] RookTable =
        [
            [ 5,  5,  5,  5,  5,  5,  5,  5],
            [ 5, 10, 10, 10, 10, 10, 10,  5],
            [-5,  0,  0,  0,  0,  0,  0, -5],
            [-5,  0,  0,  0,  0,  0,  0, -5],
            [-5,  0,  0,  0,  0,  0,  0, -5],
            [-5,  0,  0,  0,  0,  0,  0, -5],
            [-5,  0,  0,  0,  0,  0,  0, -5],
            [ 0,  0,  0,  5,  5,  0,  0,  0]
        ];

        private static readonly int[][] QueenTable =
        [
            [-20, -10, -10,  -5,  -5, -10, -10, -20],
            [-10,   0,   0,   0,   0,   0,   0, -10],
            [-10,   0,   5,   5,   5,   5,   0, -10],
            [ -5,   0,   5,   5,   5,   5,   0,  -5],
            [ -5,   0,   5,   5,   5,   5,   0,  -5],
            [-10,   5,   5,   5,   5,   5,   0, -10],
            [-10,   0,   0,   0,   0,   0,   0, -10],
            [-20, -10, -10,  -5,  -5, -10, -10, -20]
        ];

        private static readonly int[][] KingTableMiddlegame =
        [
            [-30, -40, -40, -50, -50, -40, -40, -30],
            [-30, -40, -40, -50, -50, -40, -40, -30],
            [-30, -40, -40, -50, -50, -40, -40, -30],
            [-30, -40, -40, -50, -50, -40, -40, -30],
            [-20, -30, -30, -40, -40, -30, -30, -20],
            [-10, -20, -20, -20, -20, -20, -20, -10],
            [ 20,  20,   0,   0,   0,   0,  20,  20],
            [ 20,  30,  10,   0,   0,  10,  30,  20]
        ];

        private static readonly int[][] KingTableEndgame =
        [
            [-50, -40, -30, -20, -20, -30, -40, -50],
            [-30, -20, -10,   0,   0, -10, -20, -30],
            [-30, -10,  20,  30,  30,  20, -10, -30],
            [-30, -10,  30,  40,  40,  30, -10, -30],
            [-30, -10,  30,  40,  40,  30, -10, -30],
            [-30, -10,  20,  30,  30,  20, -10, -30],
            [-30, -30,   0,   0,   0,   0, -30, -30],
            [-50, -30, -30, -30, -30, -30, -30, -50]
        ];

        #endregion

        private static int CalculateKingPositionScore(Piece piece, bool isEndgame)
        {
            int colorCorrectedRow = piece.Color == PlayerColor.White ? piece.Position.Row : 7 - piece.Position.Row;
            var table = isEndgame ? KingTableEndgame : KingTableMiddlegame;
            return table[colorCorrectedRow][piece.Position.Col];
        }

        private static int CalculateQueenPositionScore(Piece piece)
        {
            int colorCorrectedRow = piece.Color == PlayerColor.White ? piece.Position.Row : 7 - piece.Position.Row;
            return QueenTable[colorCorrectedRow][piece.Position.Col];
        }

        private static int CalculateRookPositionScore(Piece piece, Board board)
        {
            int score = 0;

            //foreach (var direction in Direction.Orthogonal)
            //{
            //    for (int step = 1; step < BoardConstants.BoardSize; step++)
            //    {
            //        int targetRow = piece.Position.Row + direction.RowDelta * step;
            //        int targetCol = piece.Position.Col + direction.ColDelta * step;

            //        if (targetRow < 0 || targetRow >= BoardConstants.BoardSize ||
            //            targetCol < 0 || targetCol >= BoardConstants.BoardSize)
            //            break;

            //        var targetPiece = board.ChessBoard[targetRow][targetCol];
            //        if (targetPiece != null && targetPiece.Color == piece.Color && targetPiece.Type != PieceType.King)
            //        {
            //            score += GetPieceValue(targetPiece.Type) / 100;
            //        }
            //    }
            //}

            int colorCorrectedRow = piece.Color == PlayerColor.White ? piece.Position.Row : 7 - piece.Position.Row;
            return score + RookTable[colorCorrectedRow][piece.Position.Col];
        }

        private static int CalculateBishopPositionScore(Piece piece, Board board)
        {
            int score = 0;

            //foreach (var direction in Direction.Diagonal)
            //{
            //    for (int step = 1; step < BoardConstants.BoardSize; step++)
            //    {
            //        int targetRow = piece.Position.Row + direction.RowDelta * step;
            //        int targetCol = piece.Position.Col + direction.ColDelta * step;

            //        if (targetRow < 0 || targetRow >= BoardConstants.BoardSize ||
            //            targetCol < 0 || targetCol >= BoardConstants.BoardSize)
            //            break;

            //        var targetPiece = board.ChessBoard[targetRow][targetCol];
            //        if (targetPiece != null && targetPiece.Color == piece.Color && targetPiece.Type != PieceType.King)
            //        {
            //            score += GetPieceValue(targetPiece.Type) / 100;
            //        }
            //    }
            //}

            int colorCorrectedRow = piece.Color == PlayerColor.White ? piece.Position.Row : 7 - piece.Position.Row;
            return score + BishopTable[colorCorrectedRow][piece.Position.Col];
        }

        private static int CalculateKnightPositionScore(Piece piece, Board board)
        {
            int score = 0;

            //foreach (var move in Direction.KnightMoves)
            //{
            //    int targetRow = piece.Position.Row + move.RowDelta;
            //    int targetCol = piece.Position.Col + move.ColDelta;

            //    if (targetRow < 0 || targetRow >= BoardConstants.BoardSize ||
            //        targetCol < 0 || targetCol >= BoardConstants.BoardSize)
            //        continue;

            //    var targetPiece = board.ChessBoard[targetRow][targetCol];
            //    if (targetPiece != null && targetPiece.Color == piece.Color && targetPiece.Type != PieceType.King)
            //    {
            //        score += GetPieceValue(targetPiece.Type) / 100;
            //    }
            //}

            int colorCorrectedRow = piece.Color == PlayerColor.White ? piece.Position.Row : 7 - piece.Position.Row;
            return score + KnightTable[colorCorrectedRow][piece.Position.Col];
        }

        private static int CalculatePawnPositionScore(Piece piece, Board board)
        {
            int score = 0;
            int direction = piece.Color == PlayerColor.White ? -1 : 1;

            // Check both diagonal protection directions
            //int[] colOffsets = [-1, 1];
            //foreach (int colOffset in colOffsets)
            //{
            //    int targetRow = piece.Position.Row + direction;
            //    int targetCol = piece.Position.Col + colOffset;

            //    if (targetRow < 0 || targetRow >= BoardConstants.BoardSize ||
            //        targetCol < 0 || targetCol >= BoardConstants.BoardSize)
            //        continue;

            //    var targetPiece = board.ChessBoard[targetRow][targetCol];
            //    if (targetPiece != null && targetPiece.Color == piece.Color && targetPiece.Type != PieceType.King)
            //    {
            //        int pieceValue = GetPieceValue(targetPiece.Type);
            //        score += targetPiece.Type == PieceType.Pawn ? pieceValue / 20 : pieceValue / 100;
            //    }
            //}

            int colorCorrectedRow = piece.Color == PlayerColor.White ? piece.Position.Row : 7 - piece.Position.Row;
            return score + PawnTable[colorCorrectedRow][piece.Position.Col];
        }

        [Obsolete("EvaluateWithDepth is deprecated, please use EvaluateWithDepthAndPV instead.")]
        public int EvaluateWithDepth(Board board, int depth, PlayerColor ourColor, PlayerColor colorToMove, int alpha, int beta)
        {
            NumOfVisitedNodes++;

            // Check for 50-move rule draw immediately
            if (board.IsFiftyMoveRuleDraw())
            {
                return 0;
            }

            // Check for 3-fold repetition draw immediately
            if (board.IsThreefoldRepetition())
            {
                return 0;
            }
            
            // Compute position hash for transposition table lookup
            ulong hash = ZobristHash.ComputeHash(board);
            
            // Check transposition table
            Move? ttBestMove = null;
            if (_transpositionTable.TryGet(hash, out TTEntry ttEntry) && ttEntry.Depth >= depth)
            {
                ttBestMove = ttEntry.BestMove;
                switch (ttEntry.Bound)
                {
                    case TTBoundType.Exact:
                        return ttEntry.Score;
                    case TTBoundType.LowerBound:
                        if (ttEntry.Score >= beta) return ttEntry.Score;
                        alpha = Math.Max(alpha, ttEntry.Score);
                        break;
                    case TTBoundType.UpperBound:
                        if (ttEntry.Score <= alpha) return ttEntry.Score;
                        beta = Math.Min(beta, ttEntry.Score);
                        break;
                }
            }

            if (depth == 0)
            {
                int eval = EvaluatePosition(board, ourColor);
                _transpositionTable.Store(hash, eval, depth, TTBoundType.Exact, null);
                return eval;
            }

            var moves = _moveGenerator.GetAllLegalMoves(board, colorToMove);

            if (moves.Count == 0)
            {
                int terminalEval = EvaluateTerminalPosition(board, ourColor);
                _transpositionTable.Store(hash, terminalEval, depth, TTBoundType.Exact, null);
                return terminalEval;
            }

            // Order moves: try TT best move first if available
            if (ttBestMove.HasValue)
            {
                OrderMovesWithTTMove(moves, ttBestMove.Value);
            }

            int originalAlpha = alpha;
            Move? bestMove = null;

            // Maximising if colorToMove is ourColor
            if (ourColor == colorToMove)
            {
                int maxEval = int.MinValue;
                PlayerColor opponentColor = GetOpponentColor(colorToMove);

                foreach (var move in moves)
                {
                    board.MakeMove(move.From, move.To, move.PromotionPiece);

                    if (board.Check)
                    {
                        board.UndoMove();
                        continue;
                    }

                    int eval = EvaluateWithDepth(board, depth - 1, ourColor, opponentColor, alpha, beta);
                    board.UndoMove();

                    if (eval > maxEval)
                    {
                        maxEval = eval;
                        bestMove = move;
                    }

                    if (maxEval >= beta)
                        break;

                    alpha = Math.Max(alpha, maxEval);
                }

                TTBoundType boundType = maxEval <= originalAlpha ? TTBoundType.UpperBound
                    : maxEval >= beta ? TTBoundType.LowerBound
                    : TTBoundType.Exact;
                _transpositionTable.Store(hash, maxEval, depth, boundType, bestMove);

                return maxEval;
            }
            else
            {
                int minEval = int.MaxValue;
                PlayerColor opponentColor = GetOpponentColor(colorToMove);

                foreach (var move in moves)
                {
                    board.MakeMove(move.From, move.To, move.PromotionPiece);

                    if (board.Check)
                    {
                        board.UndoMove();
                        continue;
                    }

                    int eval = EvaluateWithDepth(board, depth - 1, ourColor, opponentColor, alpha, beta);
                    board.UndoMove();

                    if (eval < minEval)
                    {
                        minEval = eval;
                        bestMove = move;
                    }

                    if (minEval <= alpha)
                        break;

                    beta = Math.Min(beta, minEval);
                }

                TTBoundType boundType = minEval >= beta ? TTBoundType.LowerBound
                    : minEval <= originalAlpha ? TTBoundType.UpperBound
                    : TTBoundType.Exact;
                _transpositionTable.Store(hash, minEval, depth, boundType, bestMove);

                return minEval;
            }
        }

        private const int MaxDepth = BoardConstants.SearchDepth + 2;
        public (int Value, List<Move> PrincipalVariation) EvaluateWithDepthAndPV(Board board, int depth, int numOfMovesPlayed, PlayerColor ourColor,
            PlayerColor colorToMove, int alpha, int beta)
        {
            NumOfVisitedNodes++;

            // Check for 50-move rule draw immediately
            if (board.IsFiftyMoveRuleDraw())
            {
                return (0, new List<Move>());
            }

            // Check for 3-fold repetition draw immediately
            if (board.IsThreefoldRepetition())
            {
                return (0, new List<Move>());
            }

            // Compute position hash for transposition table lookup
            ulong hash = ZobristHash.ComputeHash(board);

            // Check transposition table for cutoffs
            Move? ttBestMove = null;
            if (_transpositionTable.TryGet(hash, out TTEntry ttEntry))
            {
                ttBestMove = ttEntry.BestMove;
                
                if (ttEntry.Depth >= depth)
                {
                    switch (ttEntry.Bound)
                    {
                        case TTBoundType.Exact:
                            var exactPv = ttBestMove.HasValue ? new List<Move> { ttBestMove.Value } : new List<Move>();
                            return (ttEntry.Score, exactPv);
                        case TTBoundType.LowerBound:
                            if (ttEntry.Score >= beta)
                            {
                                var lowerPv = ttBestMove.HasValue ? new List<Move> { ttBestMove.Value } : new List<Move>();
                                return (ttEntry.Score, lowerPv);
                            }
                            alpha = Math.Max(alpha, ttEntry.Score);
                            break;
                        case TTBoundType.UpperBound:
                            if (ttEntry.Score <= alpha)
                                return (ttEntry.Score, new List<Move>());
                            beta = Math.Min(beta, ttEntry.Score);
                            break;
                    }
                }
            }

            if (depth == 0)
            {
                int eval = EvaluatePosition(board, ourColor);

                if (numOfMovesPlayed >= MaxDepth)
                {
                    _transpositionTable.Store(hash, eval, depth, TTBoundType.Exact, null);
                    return (eval, new List<Move>());
                }

                // If eval shows this is a good move, relative to current best move, extend depth
                if (eval > beta - board.FullmoveNumber * 5 || eval < alpha + board.FullmoveNumber * 5)
                {
                    NumOfAdvancedDeapthNodes++;
                    return EvaluateWithDepthAndPV(board, 2, numOfMovesPlayed, ourColor, colorToMove, alpha, beta);
                }

                _transpositionTable.Store(hash, eval, depth, TTBoundType.Exact, null);
                return (eval, new List<Move>());
            }

            var moves = _moveGenerator.GetAllLegalMoves(board, colorToMove);

            if (moves.Count == 0)
            {
                int terminalEval = EvaluateTerminalPosition(board, ourColor);
                _transpositionTable.Store(hash, terminalEval, depth, TTBoundType.Exact, null);
                return (terminalEval, new List<Move>());
            }

            // Order moves: try TT best move first
            if (ttBestMove.HasValue)
            {
                moves = OrderMovesWithTTMove(moves, ttBestMove.Value);
            }

            int originalAlpha = alpha;
            Move? bestMove = null;

            // Maximising if colorToMove is ourColor
            if (ourColor == colorToMove)
            {
                int maxEval = int.MinValue;
                List<Move> bestVariation = new List<Move>();
                PlayerColor opponentColor = GetOpponentColor(colorToMove);

                foreach (var move in moves)
                {
                    board.MakeMove(move.From, move.To, move.PromotionPiece);

                    if (board.Check)
                    {
                        board.UndoMove();
                        continue;
                    }

                    var (eval, pv) = EvaluateWithDepthAndPV(board, depth - 1, numOfMovesPlayed + 1, ourColor, opponentColor, alpha, beta);
                    board.UndoMove();

                    if (eval > maxEval)
                    {
                        maxEval = eval;
                        bestMove = move;
                        bestVariation = [move, .. pv];
                    }

                    if (maxEval >= beta)
                        break;

                    alpha = Math.Max(alpha, maxEval);
                }

                TTBoundType boundType = maxEval <= originalAlpha ? TTBoundType.UpperBound
                    : maxEval >= beta ? TTBoundType.LowerBound
                    : TTBoundType.Exact;
                _transpositionTable.Store(hash, maxEval, depth, boundType, bestMove);

                return (maxEval, bestVariation);
            }
            else
            {
                int minEval = int.MaxValue;
                List<Move> bestVariation = new List<Move>();
                PlayerColor opponentColor = GetOpponentColor(colorToMove);

                foreach (var move in moves)
                {
                    board.MakeMove(move.From, move.To, move.PromotionPiece);

                    if (board.Check)
                    {
                        board.UndoMove();
                        continue;
                    }

                    var (eval, pv) = EvaluateWithDepthAndPV(board, depth - 1, numOfMovesPlayed + 1, ourColor, opponentColor, alpha, beta);
                    board.UndoMove();

                    if (eval < minEval)
                    {
                        minEval = eval;
                        bestMove = move;
                        bestVariation = [move, .. pv];
                    }

                    if (minEval <= alpha)
                        break;

                    beta = Math.Min(beta, minEval);
                }

                TTBoundType boundType = minEval >= beta ? TTBoundType.LowerBound
                    : minEval <= originalAlpha ? TTBoundType.UpperBound
                    : TTBoundType.Exact;
                _transpositionTable.Store(hash, minEval, depth, boundType, bestMove);

                return (minEval, bestVariation);
            }
        }

        private static List<Move> OrderMovesWithTTMove(List<Move> moves, Move ttMove)
        {
            for (int i = 0; i < moves.Count; i++)
            {
                if (moves[i].From == ttMove.From && moves[i].To == ttMove.To && 
                    moves[i].PromotionPiece == ttMove.PromotionPiece)
                {
                    if (i > 0)
                    {
                        (moves[0], moves[i]) = (moves[i], moves[0]);
                    }
                    break;
                }
            }
            return moves;
        }

        private static PlayerColor GetOpponentColor(PlayerColor color)
        {
            return color == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;
        }

        private static int EvaluateTerminalPosition(Board board, PlayerColor colorToMove)
        {
            // Check for 50-move rule draw
            if (board.IsFiftyMoveRuleDraw())
            {
                return 0;
            }

            // Check for 3-fold repetition draw
            if (board.IsThreefoldRepetition())
            {
                return 0;
            }

            // Checkmate
            if (board.Check)
            {
                return -1000000;
            }

            // Stalemate
            return 0;
        }
    }
}
