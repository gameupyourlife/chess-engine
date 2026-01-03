namespace chess_engine.game
{
    /// <summary>
    /// Represents a chess move with source, destination, and optional promotion piece.
    /// </summary>
    internal readonly record struct Move(
        Square From,
        Square To,
        PieceType? PromotionPiece = null)
    {
        public static readonly PieceType[] PromotionPieces =
        [
            PieceType.Queen,
            PieceType.Rook,
            PieceType.Bishop,
            PieceType.Knight
        ];

        public bool IsPromotion => PromotionPiece.HasValue;

        // Constructor overload for tuple compatibility
        public Move((int, int) from, (int, int) to, PieceType? promotionPiece = null)
            : this(new Square(from.Item1, from.Item2), new Square(to.Item1, to.Item2), promotionPiece)
        {
        }
    }
}
