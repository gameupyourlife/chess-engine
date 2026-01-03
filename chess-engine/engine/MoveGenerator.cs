namespace chess_engine.engine
{
    using chess_engine.game;

    /// <summary>
    /// Generates legal moves for a given board position.
    /// </summary>
    internal class MoveGenerator
    {
        /// <summary>
        /// Gets all legal moves for the specified color.
        /// </summary>
        public List<Move> GetAllLegalMoves(Board board, PlayerColor color)
        {
            var legalMoves = new List<Move>();

            for (int col = 0; col < BoardConstants.BoardSize; col++)
            {
                for (int row = 0; row < BoardConstants.BoardSize; row++)
                {
                    var piece = board.ChessBoard[row][col];
                    if (piece is null || piece.Color != color)
                        continue;

                    piece.CalculateValidTargetPositions(board);
                    AddLegalMovesForPiece(board, piece, legalMoves);
                }
            }

            return legalMoves;
        }

        private static void AddLegalMovesForPiece(Board board, Piece piece, List<Move> legalMoves)
        {
            foreach (var target in piece.ValidTargetPositions)
            {
                if (BoardConstants.IsPawnPromotion(piece, target))
                {
                    AddPromotionMoves(board, piece, target, legalMoves);
                }
                else
                {
                    AddMoveIfLegal(board, piece.Position, target, null, legalMoves);
                }
            }
        }

        private static void AddPromotionMoves(Board board, Piece piece, Square target, List<Move> legalMoves)
        {
            foreach (var promotionPiece in Move.PromotionPieces)
            {
                AddMoveIfLegal(board, piece.Position, target, promotionPiece, legalMoves);
            }
        }

        private static void AddMoveIfLegal(
            Board board,
            Square from,
            Square to,
            PieceType? promotionPiece,
            List<Move> legalMoves)
        {
            if (IsMoveLegal(board, from, to, promotionPiece))
            {
                legalMoves.Add(new Move(from, to, promotionPiece));
            }
        }

        /// <summary>
        /// Checks if a move is legal (doesn't leave the moving side in check).
        /// </summary>
        public static bool IsMoveLegal(Board board, Square from, Square to, PieceType? promotionPiece = null)
        {
            board.MakeMove(from, to, promotionPiece);

            bool isIllegal = board.Check;

            board.UndoMove();

            return !isIllegal;
        }
    }
}
