using chess_engine.game;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace chess_engine.commands
{
    internal static class PositionCommandHandler
    {
        public static Board HandlePositionCommand(string input)
        {
            // Example input: "position startpos moves e2e4 e7e5"
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                throw new ArgumentException("Invalid position command");
            }
            int moveIndex = Array.IndexOf(parts, "moves");
            if (moveIndex == -1)
            {
                // No moves provided, just set the position
                return ApplyFenString(parts);
            }
            else
            {
                // Set position and apply moves
                var board = ApplyFenString(parts);
                board = ApplyPositionMoves(parts[(moveIndex + 1)..], board);
                return board;
            }
        }

        private static Board ApplyPositionMoves(string[] moves, Board board)
        {
            foreach (var move in moves)
            {
                var (from, to, promotionPiece) = ConvertAlgebraicToCoordinates(move);
                board.MakeMove(from, to, promotionPiece);
            }

            return board;
        }

        public static ((int, int), (int, int), PieceType?) ConvertAlgebraicToCoordinates(string move)
        {
            // Example move: "e2e4" or "e7e8q" (with promotion)
            if (move.Length < 4 || move.Length > 5)
            {
                throw new ArgumentException("Invalid move format");
            }
            int fromFile = move[0] - 'a';
            int fromRank = 8 - (move[1] - '0');
            int toFile = move[2] - 'a';
            int toRank = 8 - (move[3] - '0');
            
            PieceType? promotionPiece = null;
            if (move.Length == 5)
            {
                promotionPiece = move[4] switch
                {
                    'q' => PieceType.Queen,
                    'r' => PieceType.Rook,
                    'b' => PieceType.Bishop,
                    'n' => PieceType.Knight,
                    _ => throw new ArgumentException($"Invalid promotion piece: {move[4]}")
                };
            }
            
            return ((fromRank, fromFile), (toRank, toFile), promotionPiece);
        }

        public static string ConvertCoordinatesToAlgebraic((int, int) from, (int, int) to, PieceType? promotionPiece = null)
        {
            // Example coordinates: ((6, 4), (4, 4)) -> "e2e4"
            // With promotion: ((1, 4), (0, 4), PieceType.Queen) -> "e7e8q"
            char fromFile = (char)('a' + from.Item2);
            char fromRank = (char)('8' - from.Item1);
            char toFile = (char)('a' + to.Item2);
            char toRank = (char)('8' - to.Item1);
            
            string result = $"{fromFile}{fromRank}{toFile}{toRank}";
            
            if (promotionPiece.HasValue)
            {
                char promotionChar = promotionPiece.Value switch
                {
                    PieceType.Queen => 'q',
                    PieceType.Rook => 'r',
                    PieceType.Bishop => 'b',
                    PieceType.Knight => 'n',
                    _ => throw new ArgumentException($"Invalid promotion piece type: {promotionPiece.Value}")
                };
                result += promotionChar;
            }
            
            return result;
        }

        private static Board ApplyFenString(string[] parts)
        {
            string positionType = parts[1];
            if (positionType == "startpos")
            {
                // Set to starting position
                Debug.WriteLine("Position set to starting position");
                return new Board("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
            }
            else if (positionType.StartsWith("fen"))
            {
                // Set to FEN position
                string fen = string.Join(' ', parts, 2, parts.Length - 2);
                Debug.WriteLine($"Position set to FEN: {fen}");
                return new Board(fen);
            }
            else
            {
                throw new ArgumentException("Invalid position type");
            }
        }
    }
}
