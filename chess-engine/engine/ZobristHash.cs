using chess_engine.game;

namespace chess_engine.engine
{
    /// <summary>
    /// Zobrist hashing for efficient position identification in transposition tables.
    /// </summary>
    internal static class ZobristHash
    {
        // Random numbers for each piece type, color, and square
        // [pieceType (6)] [color (2)] [square (64)]
        private static readonly ulong[,,] PieceSquareKeys = new ulong[6, 2, 64];
        
        // Random number for side to move (XOR when black to move)
        private static readonly ulong SideToMoveKey;
        
        // Random numbers for castling rights [4 possible rights]
        private static readonly ulong[] CastlingKeys = new ulong[4];
        
        // Random numbers for en passant file [8 files, 0 = no en passant]
        private static readonly ulong[] EnPassantKeys = new ulong[8];

        static ZobristHash()
        {
            var random = new Random(12345); // Fixed seed for reproducibility
            
            // Initialize piece-square keys
            for (int pieceType = 0; pieceType < 6; pieceType++)
            {
                for (int color = 0; color < 2; color++)
                {
                    for (int square = 0; square < 64; square++)
                    {
                        PieceSquareKeys[pieceType, color, square] = NextULong(random);
                    }
                }
            }
            
            SideToMoveKey = NextULong(random);
            
            for (int i = 0; i < 4; i++)
            {
                CastlingKeys[i] = NextULong(random);
            }
            
            for (int i = 0; i < 8; i++)
            {
                EnPassantKeys[i] = NextULong(random);
            }
        }

        private static ulong NextULong(Random random)
        {
            byte[] buffer = new byte[8];
            random.NextBytes(buffer);
            return BitConverter.ToUInt64(buffer, 0);
        }

        /// <summary>
        /// Computes the Zobrist hash for the entire board position.
        /// </summary>
        public static ulong ComputeHash(Board board)
        {
            ulong hash = 0;

            // Hash pieces
            for (int row = 0; row < BoardConstants.BoardSize; row++)
            {
                for (int col = 0; col < BoardConstants.BoardSize; col++)
                {
                    var piece = board.ChessBoard[row][col];
                    if (piece != null)
                    {
                        hash ^= GetPieceSquareKey(piece.Type, piece.Color, row, col);
                    }
                }
            }

            // Hash side to move
            if (board.ActiveColor == PlayerColor.Black)
            {
                hash ^= SideToMoveKey;
            }

            // Hash castling rights
            foreach (var right in board.CastleRights)
            {
                hash ^= CastlingKeys[(int)right];
            }

            // Hash en passant
            if (board.EnPassantTargetSquare.HasValue)
            {
                hash ^= EnPassantKeys[board.EnPassantTargetSquare.Value.Item2];
            }

            return hash;
        }

        /// <summary>
        /// Gets the key for a piece on a specific square.
        /// </summary>
        public static ulong GetPieceSquareKey(PieceType pieceType, PlayerColor color, int row, int col)
        {
            int square = row * 8 + col;
            return PieceSquareKeys[(int)pieceType, (int)color, square];
        }

        /// <summary>
        /// Gets the side to move key (for incremental updates).
        /// </summary>
        public static ulong GetSideToMoveKey() => SideToMoveKey;

        /// <summary>
        /// Gets the castling key for a specific right.
        /// </summary>
        public static ulong GetCastlingKey(KastleRights right) => CastlingKeys[(int)right];

        /// <summary>
        /// Gets the en passant key for a specific file.
        /// </summary>
        public static ulong GetEnPassantKey(int file) => EnPassantKeys[file];
    }
}
