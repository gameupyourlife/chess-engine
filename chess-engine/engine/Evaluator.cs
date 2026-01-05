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
            int pieceCount = 0;
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
                    pieceCount++;

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
            bool isEndgame = queenCount == 0 || minorMajorCount <= 6;

            if (BoardConstants.EnableEndgameEvaluation && isEndgame)
            {
                totalScore += 10; // Small bonus for reaching endgame
                BoardConstants.SearchDepth = Math.Max(BoardConstants.SearchDepth, 6); // Increase search depth in endgame
                BoardConstants.MaxSearchDepth = BoardConstants.SearchDepth + 2;
                BoardConstants.MaxSearchDepth = Math.Max(BoardConstants.MaxSearchDepth, 16 - pieceCount);
                
                // Recalculate pawn scores with endgame evaluation
                for (int rank = 0; rank < BoardConstants.BoardSize; rank++)
                {
                    for (int file = 0; file < BoardConstants.BoardSize; file++)
                    {
                        var piece = board.ChessBoard[rank][file];
                        if (piece?.Type == PieceType.Pawn)
                        {
                            int endgamePawnBonus = CalculateEndgamePawnBonus(piece, board, ourKing, enemyKing);
                            totalScore += piece.Color == evaluatingColor ? endgamePawnBonus : -endgamePawnBonus;
                        }
                    }
                }
            }

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
                    if(isEndgame)
                        totalScore += 120;
                    else
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
            [80, 80, 80, 80, 80, 80, 80, 80],  // 7th rank - huge bonus for advanced pawns
            [25, 30, 35, 40, 40, 35, 30, 25],  // 6th rank - strong push bonus
            [15, 20, 25, 30, 30, 25, 20, 15],  // 5th rank - good advancement
            [ 8, 12, 20, 30, 30, 20, 12,  8],  // 4th rank - center control
            [ 5,  8, 15, 25, 25, 15,  8,  5],  // 3rd rank - moderate push
            [ 2,  4,  8, 10, 10,  8,  4,  2],  // 2nd rank - small bonus
            [ 0,  0,  0, -5, -5,  0,  0,  0],  // starting rank - slight penalty for not moving
            [ 0,  0,  0,  0,  0,  0,  0,  0],  // 1st rank (should never happen)
        ];

        private static readonly int[][] KnightTable =
        [
            [-50, -40, -30, -30, -30, -30, -40, -50],
            [-40, -20,   0,   0,   0,   0, -20, -40],
            [-30,   0,  10,  15,  15,  10,   0, -30],
            [-30,   5,  15,  25,  25,  15,   5, -30],
            [-30,   0,  15,  25,  25,  15,   0, -30],
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

            if (BoardConstants.EnableCoverOwnPiecesEvaluation)
            {
                foreach (var direction in Direction.Orthogonal)
                {
                    for (int step = 1; step < BoardConstants.BoardSize; step++)
                    {
                        int targetRow = piece.Position.Row + direction.RowDelta * step;
                        int targetCol = piece.Position.Col + direction.ColDelta * step;

                        if (targetRow < 0 || targetRow >= BoardConstants.BoardSize ||
                            targetCol < 0 || targetCol >= BoardConstants.BoardSize)
                            break;

                        var targetPiece = board.ChessBoard[targetRow][targetCol];
                        if (targetPiece != null && targetPiece.Color == piece.Color && targetPiece.Type != PieceType.King)
                        {
                            score += GetPieceValue(targetPiece.Type) / 100;
                        }
                    }
                }
            }

            int colorCorrectedRow = piece.Color == PlayerColor.White ? piece.Position.Row : 7 - piece.Position.Row;
            return score + RookTable[colorCorrectedRow][piece.Position.Col];
        }

        private static int CalculateBishopPositionScore(Piece piece, Board board)
        {
            int score = 0;

            if (BoardConstants.EnableCoverOwnPiecesEvaluation)
            {
                foreach (var direction in Direction.Diagonal)
                {
                    for (int step = 1; step < BoardConstants.BoardSize; step++)
                    {
                        int targetRow = piece.Position.Row + direction.RowDelta * step;
                        int targetCol = piece.Position.Col + direction.ColDelta * step;

                        if (targetRow < 0 || targetRow >= BoardConstants.BoardSize ||
                            targetCol < 0 || targetCol >= BoardConstants.BoardSize)
                            break;

                        var targetPiece = board.ChessBoard[targetRow][targetCol];
                        if (targetPiece != null && targetPiece.Color == piece.Color && targetPiece.Type != PieceType.King)
                        {
                            score += GetPieceValue(targetPiece.Type) / 100;
                        }
                    }
                }
            }

            int colorCorrectedRow = piece.Color == PlayerColor.White ? piece.Position.Row : 7 - piece.Position.Row;
            return score + BishopTable[colorCorrectedRow][piece.Position.Col];
        }

        private static int CalculateKnightPositionScore(Piece piece, Board board)
        {
            int score = 0;

            if (BoardConstants.EnableCoverOwnPiecesEvaluation)
            {
                foreach (var move in Direction.KnightMoves)
                {
                    int targetRow = piece.Position.Row + move.RowDelta;
                    int targetCol = piece.Position.Col + move.ColDelta;

                    if (targetRow < 0 || targetRow >= BoardConstants.BoardSize ||
                        targetCol < 0 || targetCol >= BoardConstants.BoardSize)
                        continue;

                    var targetPiece = board.ChessBoard[targetRow][targetCol];
                    if (targetPiece != null && targetPiece.Color == piece.Color && targetPiece.Type != PieceType.King)
                    {
                        score += GetPieceValue(targetPiece.Type) / 100;
                    }
                }
            }

            int colorCorrectedRow = piece.Color == PlayerColor.White ? piece.Position.Row : 7 - piece.Position.Row;
            return score + KnightTable[colorCorrectedRow][piece.Position.Col];
        }

        private static int CalculatePawnPositionScore(Piece piece, Board board)
        {
            int score = 0;
            int direction = piece.Color == PlayerColor.White ? -1 : 1;

            // Check both diagonal protection directions
            int[] colOffsets = [-1, 1];
            foreach (int colOffset in colOffsets)
            {
                int targetRow = piece.Position.Row + direction;
                int targetCol = piece.Position.Col + colOffset;

                if (targetRow < 0 || targetRow >= BoardConstants.BoardSize ||
                    targetCol < 0 || targetCol >= BoardConstants.BoardSize)
                    continue;

                var targetPiece = board.ChessBoard[targetRow][targetCol];
                if (targetPiece != null && targetPiece.Color == piece.Color && targetPiece.Type != PieceType.King)
                {
                    if (BoardConstants.EnableCoverOwnPiecesEvaluation)
                    {
                        score += GetPieceValue(targetPiece.Type) / 100;
                    }
                    if (BoardConstants.EnablePawnStructureEvaluation && targetPiece.Type == PieceType.Pawn)
                    {
                        score += 9; // Bonus for pawn chains
                    }
                }
            }

            if(BoardConstants.EnablePenalizeDoubledPawns)
            {
                // Penalize doubled pawns
                for (int row = 0; row < BoardConstants.BoardSize; row++)
                {
                    if (row == piece.Position.Row) continue;
                    var targetPiece = board.ChessBoard[row][piece.Position.Col];
                    if (targetPiece != null && targetPiece.Color == piece.Color && targetPiece.Type == PieceType.Pawn)
                    {
                        score -= 15; // Penalty for doubled pawns
                    }
                }
            }

            int colorCorrectedRow = piece.Color == PlayerColor.White ? piece.Position.Row : 7 - piece.Position.Row;
            return score + PawnTable[colorCorrectedRow][piece.Position.Col];
        }

        #region Endgame Pawn Evaluation

        /// <summary>
        /// Calculates endgame-specific bonuses for pawns including passed pawns, king proximity, etc.
        /// </summary>
        private static int CalculateEndgamePawnBonus(Piece pawn, Board board, Piece? ourKing, Piece? enemyKing)
        {
            int bonus = 0;
            
            // Check if this is a passed pawn
            if (IsPassedPawn(pawn, board))
            {
                int passedPawnBonus = CalculatePassedPawnBonus(pawn, board, ourKing, enemyKing);
                bonus += passedPawnBonus;
            }
            else
            {
                // Even non-passed pawns benefit from pushing in endgame
                int advancement = pawn.Color == PlayerColor.White 
                    ? (BoardConstants.WhitePawnStartRank - pawn.Position.Row)
                    : (pawn.Position.Row - BoardConstants.BlackPawnStartRank);
                
                // Encourage pushing pawns forward in endgame
                bonus += advancement * 5;
            }
            
            // Penalize isolated pawns in endgame (no friendly pawns on adjacent files)
            if (IsIsolatedPawn(pawn, board))
            {
                bonus -= 20;
            }
            
            // Penalize backward pawns in endgame
            if (IsBackwardPawn(pawn, board))
            {
                bonus -= 15;
            }
            
            return bonus;
        }

        /// <summary>
        /// Checks if a pawn is a passed pawn (no enemy pawns blocking its path to promotion)
        /// </summary>
        private static bool IsPassedPawn(Piece pawn, Board board)
        {
            if (pawn.Type != PieceType.Pawn) return false;
            
            int direction = pawn.Color == PlayerColor.White ? -1 : 1;
            PlayerColor enemyColor = pawn.Color == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;
            
            // Check three files: current file and both adjacent files
            for (int fileOffset = -1; fileOffset <= 1; fileOffset++)
            {
                int checkFile = pawn.Position.Col + fileOffset;
                if (checkFile < 0 || checkFile >= BoardConstants.BoardSize) continue;
                
                // Check all ranks from current position to promotion rank
                int currentRank = pawn.Position.Row + direction;
                while (currentRank >= 0 && currentRank < BoardConstants.BoardSize)
                {
                    var piece = board.ChessBoard[currentRank][checkFile];
                    if (piece?.Type == PieceType.Pawn && piece.Color == enemyColor)
                    {
                        return false; // Enemy pawn blocking
                    }
                    currentRank += direction;
                }
            }
            
            return true;
        }

        /// <summary>
        /// Calculates bonus for passed pawns based on advancement and king proximity
        /// </summary>
        private static int CalculatePassedPawnBonus(Piece pawn, Board board, Piece? ourKing, Piece? enemyKing)
        {
            int bonus = 0;
            
            // Base bonus for being a passed pawn
            bonus += 50;
            
            // Calculate how far the pawn has advanced (0-6 for non-promoted pawns)
            int advancement = pawn.Color == PlayerColor.White 
                ? (BoardConstants.WhitePawnStartRank - pawn.Position.Row)
                : (pawn.Position.Row - BoardConstants.BlackPawnStartRank);
            
            // Exponential bonus for advancement - passed pawns become much more valuable as they advance
            bonus += advancement * advancement * 10;
            
            // Extra bonus for pawns very close to promotion
            if (advancement >= 5)
            {
                bonus += 100; // 7th rank bonus
            }
            if (advancement >= 6)
            {
                bonus += 200; // 8th rank (about to promote) - huge bonus
            }
            
            // King proximity evaluation
            if (ourKing != null)
            {
                int ourKingDistance = Math.Abs(ourKing.Position.Row - pawn.Position.Row) + 
                                     Math.Abs(ourKing.Position.Col - pawn.Position.Col);
                
                // Bonus if our king is close to support the passed pawn
                bonus += Math.Max(0, 7 - ourKingDistance) * 5;
            }
            
            if (enemyKing != null)
            {
                int enemyKingDistance = Math.Abs(enemyKing.Position.Row - pawn.Position.Row) + 
                                       Math.Abs(enemyKing.Position.Col - pawn.Position.Col);
                
                // Bonus if enemy king is far from stopping the passed pawn
                bonus += Math.Min(enemyKingDistance * 8, 50);
                
                // Calculate the "square rule" - can the enemy king catch the pawn?
                if (!CanKingCatchPawn(pawn, enemyKing, board))
                {
                    // Huge bonus if the pawn will promote before the king can catch it
                    bonus += 150;
                }
            }
            
            // Protected passed pawn bonus
            if (IsPawnProtected(pawn, board))
            {
                bonus += 30;
            }
            
            // Bonus for unstoppable passed pawns (no pieces can block)
            if (IsUnstoppablePassedPawn(pawn, board, enemyKing))
            {
                bonus += 300; // Almost winning
            }
            
            return bonus;
        }

        /// <summary>
        /// Checks if a pawn is isolated (no friendly pawns on adjacent files)
        /// </summary>
        private static bool IsIsolatedPawn(Piece pawn, Board board)
        {
            if (pawn.Type != PieceType.Pawn) return false;
            
            // Check adjacent files for friendly pawns
            for (int fileOffset = -1; fileOffset <= 1; fileOffset += 2) // -1 and +1
            {
                int checkFile = pawn.Position.Col + fileOffset;
                if (checkFile < 0 || checkFile >= BoardConstants.BoardSize) continue;
                
                for (int rank = 0; rank < BoardConstants.BoardSize; rank++)
                {
                    var piece = board.ChessBoard[rank][checkFile];
                    if (piece?.Type == PieceType.Pawn && piece.Color == pawn.Color)
                    {
                        return false; // Found a friendly pawn on adjacent file
                    }
                }
            }
            
            return true;
        }

        /// <summary>
        /// Checks if a pawn is backward (can't safely advance and is behind friendly pawns)
        /// </summary>
        private static bool IsBackwardPawn(Piece pawn, Board board)
        {
            if (pawn.Type != PieceType.Pawn) return false;
            
            int direction = pawn.Color == PlayerColor.White ? -1 : 1;
            
            // Check if pawn can advance safely
            int nextRank = pawn.Position.Row + direction;
            if (nextRank < 0 || nextRank >= BoardConstants.BoardSize) return false;
            
            // If square in front is occupied by any piece, not backward
            if (board.ChessBoard[nextRank][pawn.Position.Col] != null)
            {
                return false;
            }
            
            // Check if advancing would be attacked by enemy pawns
            PlayerColor enemyColor = pawn.Color == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;
            for (int fileOffset = -1; fileOffset <= 1; fileOffset += 2)
            {
                int checkFile = pawn.Position.Col + fileOffset;
                if (checkFile < 0 || checkFile >= BoardConstants.BoardSize) continue;
                
                var piece = board.ChessBoard[nextRank][checkFile];
                if (piece?.Type == PieceType.Pawn && piece.Color == enemyColor)
                {
                    // Would be attacked, now check if it's unsupported
                    if (!IsPawnProtected(pawn, board))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        /// <summary>
        /// Checks if a pawn is protected by another friendly pawn
        /// </summary>
        private static bool IsPawnProtected(Piece pawn, Board board)
        {
            int direction = pawn.Color == PlayerColor.White ? 1 : -1; // Opposite of movement direction
            
            for (int fileOffset = -1; fileOffset <= 1; fileOffset += 2)
            {
                int checkFile = pawn.Position.Col + fileOffset;
                int checkRank = pawn.Position.Row + direction;
                
                if (checkFile < 0 || checkFile >= BoardConstants.BoardSize ||
                    checkRank < 0 || checkRank >= BoardConstants.BoardSize)
                    continue;
                
                var piece = board.ChessBoard[checkRank][checkFile];
                if (piece?.Type == PieceType.Pawn && piece.Color == pawn.Color)
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Determines if the enemy king can catch the passed pawn before it promotes (square rule)
        /// </summary>
        private static bool CanKingCatchPawn(Piece pawn, Piece enemyKing, Board board)
        {
            int promotionRank = pawn.Color == PlayerColor.White 
                ? BoardConstants.WhitePromotionRank 
                : BoardConstants.BlackPromotionRank;
            
            int pawnDistanceToPromotion = Math.Abs(pawn.Position.Row - promotionRank);
            
            // If pawn hasn't moved yet, it gets an extra square
            int pawnStartRank = pawn.Color == PlayerColor.White 
                ? BoardConstants.WhitePawnStartRank 
                : BoardConstants.BlackPawnStartRank;
            
            if (pawn.Position.Row == pawnStartRank)
            {
                pawnDistanceToPromotion -= 1;
            }
            
            // Calculate king's distance to the promotion square
            int kingDistanceToPromotionSquare = Math.Max(
                Math.Abs(enemyKing.Position.Row - promotionRank),
                Math.Abs(enemyKing.Position.Col - pawn.Position.Col)
            );
            
            // The square rule: if king can't reach the promotion square or square in front of pawn, it can't catch it
            // Add 1 if it's not the king's turn to move
            return kingDistanceToPromotionSquare <= pawnDistanceToPromotion;
        }

        /// <summary>
        /// Checks if a passed pawn is unstoppable (enemy has no way to stop it from promoting)
        /// </summary>
        private static bool IsUnstoppablePassedPawn(Piece pawn, Board board, Piece? enemyKing)
        {
            if (!IsPassedPawn(pawn, board))
                return false;
            
            // If no enemy king, pawn is unstoppable
            if (enemyKing == null)
                return true;
            
            // Check if enemy king can catch the pawn
            if (CanKingCatchPawn(pawn, enemyKing, board))
                return false;
            
            // Check if there are any enemy pieces that could potentially block
            int promotionRank = pawn.Color == PlayerColor.White 
                ? BoardConstants.WhitePromotionRank 
                : BoardConstants.BlackPromotionRank;
            int direction = pawn.Color == PlayerColor.White ? -1 : 1;
            PlayerColor enemyColor = pawn.Color == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;
            
            // Check path to promotion
            int currentRank = pawn.Position.Row + direction;
            while (currentRank != promotionRank)
            {
                // Check if any enemy piece can reach this square before the pawn
                for (int rank = 0; rank < BoardConstants.BoardSize; rank++)
                {
                    for (int file = 0; file < BoardConstants.BoardSize; file++)
                    {
                        var piece = board.ChessBoard[rank][file];
                        if (piece != null && piece.Color == enemyColor && piece.Type != PieceType.King)
                        {
                            // For simplicity, only check queens and rooks on same file/rank
                            if (piece.Type == PieceType.Queen || piece.Type == PieceType.Rook)
                            {
                                if (piece.Position.Col == pawn.Position.Col || 
                                    piece.Position.Row == currentRank)
                                {
                                    return false; // Could potentially block
                                }
                            }
                            // Bishops on same diagonal
                            if (piece.Type == PieceType.Bishop || piece.Type == PieceType.Queen)
                            {
                                int rankDiff = Math.Abs(piece.Position.Row - currentRank);
                                int fileDiff = Math.Abs(piece.Position.Col - pawn.Position.Col);
                                if (rankDiff == fileDiff)
                                {
                                    return false; // Could potentially block
                                }
                            }
                        }
                    }
                }
                currentRank += direction;
            }
            
            return true;
        }

        #endregion

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

                if (numOfMovesPlayed >= BoardConstants.MaxSearchDepth)
                {
                    var nextMoves = _moveGenerator.GetAllLegalMoves(board, colorToMove);

                    if (nextMoves.Count == 0)
                    {
                        int terminalEval = EvaluateTerminalPosition(board, ourColor);
                        _transpositionTable.Store(hash, terminalEval, depth, TTBoundType.Exact, null);
                        return (terminalEval, new List<Move>());
                    }

                    _transpositionTable.Store(hash, eval, depth, TTBoundType.Exact, null);
                    return (eval, new List<Move>());
                }

                // If eval shows this is a good move, relative to current best move, extend depth
                if (eval > beta - board.FullmoveNumber * BoardConstants.AggressionOfQuiniensearch || eval < alpha + board.FullmoveNumber * BoardConstants.AggressionOfQuiniensearch)
                {
                    NumOfAdvancedDeapthNodes++;
                    return EvaluateWithDepthAndPV(board, BoardConstants.MovesLengthIncreaseForQuiniensearch, numOfMovesPlayed, ourColor, colorToMove, alpha, beta);
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
