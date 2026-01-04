namespace chess_engine.game
{
    /// <summary>
    /// Constants for chess board dimensions and positions.
    /// </summary>
    internal static class BoardConstants
    {
        public const int BoardSize = 8;

        public const int WhitePromotionRank = 0;
        public const int BlackPromotionRank = 7;

        public const int WhitePawnStartRank = 6;
        public const int BlackPawnStartRank = 1;

        public const int SearchDepth = 2 * 2; // 4 full moves (8 plies)

        public static int GetPromotionRank(PlayerColor color) =>
            color == PlayerColor.White ? WhitePromotionRank : BlackPromotionRank;

        public static bool IsPawnPromotion(Piece piece, Square targetPosition)
        {
            if (piece.Type != PieceType.Pawn)
                return false;

            return (piece.Color == PlayerColor.White && targetPosition.Row == WhitePromotionRank) ||
                   (piece.Color == PlayerColor.Black && targetPosition.Row == BlackPromotionRank);
        }

        // Overload for tuple compatibility
        public static bool IsPawnPromotion(Piece piece, (int Row, int Col) targetPosition) =>
            IsPawnPromotion(piece, new Square(targetPosition.Row, targetPosition.Col));
    }
}
