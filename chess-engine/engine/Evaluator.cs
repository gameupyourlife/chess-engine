using chess_engine.game;
using System.Collections;
using System.Collections.Generic;

namespace chess_engine.engine
{
    internal class Evaluator
    {
        private readonly MoveGenerator _moveGenerator = new();
        public int NumOfVisitedNodes { get; private set; } = 0;

        public int EvaluatePosition(Board board, PlayerColor evaluatingColor)
        {
            int totalScore = 0;
            totalScore += EvalutePieceValues(board, evaluatingColor);
            totalScore += EvaluatePiecePositionScores(board, evaluatingColor);
            totalScore += EvaluateEnemyKingInCheck(board, evaluatingColor);


            return totalScore;
        }

        private int EvaluateEnemyKingInCheck(Board board, PlayerColor evaluatingColor)
        {
            foreach (var row in board.ChessBoard)
            {
                foreach (var piece in row)
                {
                    if (piece != null && piece.Type == PieceType.King && piece.Color != evaluatingColor)
                    {
                        return piece.SquareIsUnderAttack(piece.Position, board) ? 70 : 0;
                    }
                }
            }
            return 0;
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

        [Obsolete("EvaluateWithDepth is deprecated, please use EvaluateWithDepthAndPV instead.")]
        /// <summary>
        /// 
        /// </summary>
        /// <depr
        /// <param name="board"></param>
        /// <param name="depth"></param>
        /// <param name="colorToMove"></param>
        /// <param name="alpha"></param>
        /// <param name="beta"></param>
        /// <returns></returns>
        public int EvaluateWithDepth(Board board, int depth, PlayerColor ourColor, PlayerColor colorToMove, int alpha, int beta)
        {
            NumOfVisitedNodes ++;
            if (depth == 0)
            {
                return EvaluatePosition(board, ourColor);
            }

            var moves = _moveGenerator.GetAllLegalMoves(board, colorToMove);

            if (moves.Count == 0)
            {
                return EvaluateTerminalPosition(board, ourColor);
            }

            // Maximising if colorToMove is ourColor
            if (ourColor == colorToMove)
            {
                int maxEval = int.MinValue;
                PlayerColor opponentColor = GetOpponentColor(colorToMove);

                foreach (var move in moves)
                {
                    board.MakeMove(move.From, move.To, move.PromotionPiece);

                    if(board.Check)
                    {
                        board.UndoMove();
                        continue;
                    }

                    // Recursively evaluate opponent's best response (negate the result)
                    int eval = EvaluateWithDepth(board, depth - 1, ourColor, opponentColor, alpha, beta);


                    board.UndoMove();

                    maxEval = Math.Max(maxEval, eval);

                    if (maxEval >= beta)
                        break; // Beta cutoff

                    alpha = Math.Max(alpha, maxEval);
                }
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

                    // Recursively evaluate opponent's best response (negate the result)
                    int eval = EvaluateWithDepth(board, depth - 1, ourColor, opponentColor, alpha, beta);


                    board.UndoMove();

                    minEval = Math.Min(minEval, eval);

                    if (minEval <= alpha)
                        break; // Alpha cutoff

                    beta = Math.Min(beta, minEval);
                }
                return minEval;
            }
        }

        const int MaxDepth = BoardConstants.SearchDepth + 4;
        public (int Value, List<Move> PrincipalVariation) EvaluateWithDepthAndPV(Board board, int depth, int numOfMovesPlayed, PlayerColor ourColor,
            PlayerColor colorToMove, int alpha, int beta)
        {
            NumOfVisitedNodes ++;
            if (depth == 0)
            {
                int eval = EvaluatePosition(board, ourColor);

                if (numOfMovesPlayed >= MaxDepth)
                {
                    return (eval, new List<Move>());
                }

                if (eval > alpha - board.FullmoveNumber * 10 / MaxDepth && eval < beta + board.FullmoveNumber * 10 / MaxDepth)
                {
                    var(newEval, pv) = EvaluateWithDepthAndPV(board, 2, numOfMovesPlayed, ourColor, colorToMove, alpha, beta);
                    return (newEval, pv);
                }

                return (eval, new List<Move>());
            }

            var moves = _moveGenerator.GetAllLegalMoves(board, colorToMove);

            if (moves.Count == 0)
            {
                return (EvaluateTerminalPosition(board, ourColor), new List<Move>());
            }

            // Maximising if colorToMove is ourColor
            if (ourColor == colorToMove)
            {
                int maxEval = int.MinValue;
                List<Move> bestVariation = new List<Move>();
                PlayerColor opponentColor = GetOpponentColor(colorToMove);

                foreach (var move in moves)
                {
                    board.MakeMove(move.From, move.To, move.PromotionPiece);

                    if(board.Check)
                    {
                        board.UndoMove();
                        continue;
                    }

                    // Recursively evaluate opponent's best response
                    var (eval, pv) = EvaluateWithDepthAndPV(board, depth - 1, numOfMovesPlayed + 1, ourColor, opponentColor, alpha, beta);

                    board.UndoMove();

                    if (eval > maxEval)
                    {
                        maxEval = eval;
                        bestVariation = new List<Move> { move };
                        bestVariation.AddRange(pv);
                    }

                    if (maxEval >= beta)
                        break; // Beta cutoff

                    alpha = Math.Max(alpha, maxEval);
                }
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

                    // Recursively evaluate opponent's best response
                    var (eval, pv) = EvaluateWithDepthAndPV(board, depth - 1, numOfMovesPlayed + 1, ourColor, opponentColor, alpha, beta);

                    board.UndoMove();

                    if (eval < minEval)
                    {
                        minEval = eval;
                        bestVariation = new List<Move> { move };
                        bestVariation.AddRange(pv);
                    }

                    if (minEval <= alpha)
                        break; // Alpha cutoff

                    beta = Math.Min(beta, minEval);
                }
                return (minEval, bestVariation);
            }
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
