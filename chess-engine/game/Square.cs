namespace chess_engine.game
{
    /// <summary>
    /// Represents a position on the chess board.
    /// </summary>
    internal readonly record struct Square(int Row, int Col)
    {
        public static Square operator +(Square square, Direction direction) =>
            new(square.Row + direction.RowDelta, square.Col + direction.ColDelta);

        public static Square operator *(Square square, int multiplier) =>
            new(square.Row * multiplier, square.Col * multiplier);

        public bool IsWithinBounds() =>
            Row >= 0 && Row < BoardConstants.BoardSize &&
            Col >= 0 && Col < BoardConstants.BoardSize;

        public static implicit operator Square((int Row, int Col) tuple) => new(tuple.Row, tuple.Col);
        public static implicit operator (int, int)(Square square) => (square.Row, square.Col);

        public override string ToString() => $"({Row}, {Col})";
    }

    /// <summary>
    /// Represents a direction of movement on the board.
    /// </summary>
    internal readonly record struct Direction(int RowDelta, int ColDelta)
    {
        public static Direction operator *(Direction direction, int multiplier) =>
            new(direction.RowDelta * multiplier, direction.ColDelta * multiplier);

        public static readonly Direction North = new(-1, 0);
        public static readonly Direction South = new(1, 0);
        public static readonly Direction East = new(0, 1);
        public static readonly Direction West = new(0, -1);
        public static readonly Direction NorthEast = new(-1, 1);
        public static readonly Direction NorthWest = new(-1, -1);
        public static readonly Direction SouthEast = new(1, 1);
        public static readonly Direction SouthWest = new(1, -1);

        public static readonly Direction[] Orthogonal = [North, South, East, West];
        public static readonly Direction[] Diagonal = [NorthEast, NorthWest, SouthEast, SouthWest];
        public static readonly Direction[] All = [North, South, East, West, NorthEast, NorthWest, SouthEast, SouthWest];
    
        public static readonly Direction[] KnightMoves =
        [
            new(2, 1), new(1, 2), new(-1, 2), new(-2, 1),
            new(-2, -1), new(-1, -2), new(1, -2), new(2, -1)
        ];
    }
}
