using chess_engine.commands;
using System.Collections.Generic;
using System.Diagnostics;

namespace chess_engine.engine
{
    internal class Evaluator
    {
        public int EvaluatePosition(game.Board board, bool previeueslyInCheck)
        {
            if (board.Check && previeueslyInCheck)
            {
                return int.MinValue;
            }

            int score = 0;
            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 0; file < 8; file++)
                {
                    var piece = board.ChessBoard[rank][file];
                    if (piece != null)
                    {
                        int pieceValue = GetPieceValue(piece.Type);
                        score += piece.Color == board.OurColor ? pieceValue : -pieceValue;
                    }
                }
            }
            return score;
        }

        public int EvaluateWithDepth(game.Board board, int depth, bool isMaximizingPlayer, int alpha, int beta)
        {
            if (depth == 0)
            {
                return EvaluatePosition(board, false);
            }

            var moves = GetAllLegalMoves(board, isMaximizingPlayer ? board.OurColor : (board.OurColor == game.PlayerColor.White ? game.PlayerColor.Black : game.PlayerColor.White));

            if (moves.Count == 0)
            {
                // No legal moves - checkmate or stalemate
                if (board.Check)
                {
                    // Checkmate - return a very bad score for the side in check
                    return isMaximizingPlayer ? -1000000 : 1000000;
                }
                else
                {
                    // Stalemate
                    return 0;
                }
            }

            if (isMaximizingPlayer)
            {
                int maxEval = int.MinValue;
                foreach (var move in moves)
                {
                    Console.WriteLine(PositionCommandHandler.ConvertCoordinatesToAlgebraic(move.from, move.to));

                    board.MakeMove(move.from, move.to, move.promotionPiece);
                    int eval = EvaluateWithDepth(board, depth - 1, false, alpha, beta);
                    board.UndoMove();
                    
                    maxEval = System.Math.Max(maxEval, eval);
                    alpha = System.Math.Max(alpha, eval);
                    
                    if (beta <= alpha)
                        break; // Beta cutoff
                }
                return maxEval;
            }
            else
            {
                int minEval = int.MaxValue;
                foreach (var move in moves)
                {
                    board.MakeMove(move.from, move.to, move.promotionPiece);
                    int eval = EvaluateWithDepth(board, depth - 1, true, alpha, beta);
                    board.UndoMove();
                    
                    minEval = System.Math.Min(minEval, eval);
                    beta = System.Math.Min(beta, eval);
                    
                    if (beta <= alpha)
                        break; // Alpha cutoff
                }
                return minEval;
            }
        }

        private List<((int, int) from, (int, int) to, game.PieceType? promotionPiece)> GetAllLegalMoves(game.Board board, game.PlayerColor color)
        {
            var legalMoves = new List<((int, int) from, (int, int) to, game.PieceType? promotionPiece)>();

            for (int col = 0; col < 8; col++)
            {
                for (int row = 0; row < 8; row++)
                {
                    var piece = board.ChessBoard[row][col];
                    if (piece is null || piece.Color != color) continue;
                    
                    piece.CalculateValidTargetPositions(board);

                    foreach (var target in piece.ValidTargetPostions)
                    {
                        // Check if this is a pawn promotion
                        bool isPromotion = piece.Type == game.PieceType.Pawn && 
                                          ((piece.Color == game.PlayerColor.White && target.Item1 == 0) ||
                                           (piece.Color == game.PlayerColor.Black && target.Item1 == 7));

                        if (isPromotion)
                        {
                            // Generate all four promotion options
                            var promotionPieces = new[] { game.PieceType.Queen, game.PieceType.Rook, game.PieceType.Bishop, game.PieceType.Knight };
                            
                            foreach (var promotionPiece in promotionPieces)
                            {
                                bool wasInCheck = board.Check;
                                board.MakeMove(piece.Position, target, promotionPiece);
                                
                                // Check if move leaves us in check (illegal)
                                bool isIllegal = board.Check && wasInCheck;
                                
                                board.UndoMove();
                                
                                if (!isIllegal)
                                {
                                    legalMoves.Add((piece.Position, target, promotionPiece));
                                }
                            }
                        }
                        else
                        {
                            bool wasInCheck = board.Check;
                            board.MakeMove(piece.Position, target);
                            
                            // Check if move leaves us in check (illegal)
                            bool isIllegal = board.Check && wasInCheck;
                            
                            board.UndoMove();
                            
                            if (!isIllegal)
                            {
                                legalMoves.Add((piece.Position, target, null));
                            }
                        }
                    }
                }
            }

            return legalMoves;
        }

        private int GetPieceValue(game.PieceType pieceType)
        {
            return pieceType switch
            {
                game.PieceType.Pawn => 100,
                game.PieceType.Knight => 320,
                game.PieceType.Bishop => 330,
                game.PieceType.Rook => 500,
                game.PieceType.Queen => 900,
                game.PieceType.King => 2000000,
                _ => 0,
            };
        }
    }
}
