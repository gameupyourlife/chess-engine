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

        public static int SearchDepth { get; set; } = 2 * 2;
        public static int MaxSearchDepth { get; set; } = SearchDepth + 2;

        public const bool EnablePawnStructureEvaluation = true;
        public const bool EnablePenalizeDoubledPawns = true;

        public const bool EnableCoverOwnPiecesEvaluation = true;

        // Not implemeted yet
        //public const bool EnableKingSafetyEvaluation = true;
        //public const bool EnableMobilityEvaluation = true;
        public const bool EnableEndgameEvaluation = true;

        public static int AggressionOfQuiniensearch { get; set; } = 5;
        public static int MovesLengthIncreaseForQuiniensearch { get; set; } = 2;

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
