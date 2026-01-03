using chess_engine.game;

namespace chess_engine.engine
{
    internal class Evaluator
    {
        private readonly MoveGenerator _moveGenerator = new();

        public int EvaluatePosition(Board board, PlayerColor evaluatingColor)
        {
            int totalScore = 0;
            totalScore += EvalutePieceValues(board, evaluatingColor);
            totalScore += EvaluatePiecePositionScores(board, evaluatingColor);


            return totalScore;
        }

        private int EvaluatePiecePositionScores(Board board, PlayerColor evaluatingColor)
        {
            int score = 0;
            for (int rank = 0; rank < BoardConstants.BoardSize; rank++)
            {
                for (int file = 0; file < BoardConstants.BoardSize; file++)
                {
                    var piece = board.ChessBoard[rank][file];
                    if (piece != null)
                    {
                        int positionalScore = piece.CalculatePositionScore(board);
                        if (positionalScore != 0)
                        {

                        }
                        score += piece.Color == evaluatingColor ? positionalScore : -positionalScore;
                    }
                }
            }
            return score;
        }

        private int EvalutePieceValues(Board board, PlayerColor evaluatingColor)
        {
            int score = 0;

            for (int rank = 0; rank < BoardConstants.BoardSize; rank++)
            {
                for (int file = 0; file < BoardConstants.BoardSize; file++)
                {
                    var piece = board.ChessBoard[rank][file];
                    if (piece != null)
                    {
                        int pieceValue = piece.GetPieceValue();
                        score += piece.Color == evaluatingColor ? pieceValue : -pieceValue;
                    }
                }
            }
            return score;
        }

        public int EvaluateWithDepth(Board board, int depth, PlayerColor colorToMove, int alpha, int beta)
        {
            if (depth == 0)
            {
                return EvaluatePosition(board, colorToMove);
            }

            var moves = _moveGenerator.GetAllLegalMoves(board, colorToMove);

            if (moves.Count == 0)
            {
                return EvaluateTerminalPosition(board, colorToMove);
            }

            int maxEval = int.MinValue;
            PlayerColor opponentColor = GetOpponentColor(colorToMove);

            foreach (var move in moves)
            {
                board.MakeMove(move.From, move.To, move.PromotionPiece);
                
                // Recursively evaluate opponent's best response (negate the result)
                int eval = -EvaluateWithDepth(board, depth - 1, opponentColor, -beta, -alpha);
                
                board.UndoMove();

                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);

                //if (beta <= alpha)
                    //break; // Beta cutoff
            }

            return maxEval;
        }

        private static PlayerColor GetOpponentColor(PlayerColor color)
        {
            return color == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;
        }

        private static int EvaluateTerminalPosition(Board board, PlayerColor colorToMove)
        {
            if (board.Check)
            {
                // Checkmate - very bad score for the side that has no moves
                return -1000000;
            }

            // Stalemate
            return 0;
        }
    }
}
