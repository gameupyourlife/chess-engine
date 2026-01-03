using chess_engine.game;
using System;
using System.Diagnostics;

namespace chess_engine.commands
{
    internal static class PositionCommandHandler
    {
        private const string StartPositionFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        public static Board HandlePositionCommand(string input)
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                throw new ArgumentException("Invalid position command");
            }

            int moveIndex = Array.IndexOf(parts, "moves");
            var board = ParseBoardPosition(parts);

            if (moveIndex != -1)
            {
                ApplyMoves(parts[(moveIndex + 1)..], board);
            }

            return board;
        }

        private static Board ParseBoardPosition(string[] parts)
        {
            string positionType = parts[1];

            if (positionType == "startpos")
            {
                Debug.WriteLine("Position set to starting position");
                return new Board(StartPositionFen);
            }

            if (positionType.StartsWith("fen"))
            {
                string fen = string.Join(' ', parts, 2, parts.Length - 2);
                Debug.WriteLine($"Position set to FEN: {fen}");
                return new Board(fen);
            }

            throw new ArgumentException("Invalid position type");
        }

        private static void ApplyMoves(string[] moves, Board board)
        {
            foreach (var moveNotation in moves)
            {
                var move = ParseMove(moveNotation);
                board.MakeMove(move.From, move.To, move.PromotionPiece);
            }
        }

        public static Move ParseMove(string notation)
        {
            if (notation.Length < 4 || notation.Length > 5)
            {
                throw new ArgumentException($"Invalid move format: {notation}");
            }

            var from = ParseSquare(notation[0], notation[1]);
            var to = ParseSquare(notation[2], notation[3]);
            PieceType? promotionPiece = notation.Length == 5 ? ParsePromotionPiece(notation[4]) : null;

            return new Move(from, to, promotionPiece);
        }

        private static (int Row, int Col) ParseSquare(char file, char rank)
        {
            int col = file - 'a';
            int row = 8 - (rank - '0');
            return (row, col);
        }

        private static PieceType ParsePromotionPiece(char piece)
        {
            return piece switch
            {
                'q' => PieceType.Queen,
                'r' => PieceType.Rook,
                'b' => PieceType.Bishop,
                'n' => PieceType.Knight,
                _ => throw new ArgumentException($"Invalid promotion piece: {piece}")
            };
        }

        public static string ToAlgebraicNotation(Move move)
        {
            return ToAlgebraicNotation(move.From, move.To, move.PromotionPiece);
        }

        public static string ToAlgebraicNotation((int Row, int Col) from, (int Row, int Col) to, PieceType? promotionPiece = null)
        {
            char fromFile = (char)('a' + from.Col);
            char fromRank = (char)('8' - from.Row);
            char toFile = (char)('a' + to.Col);
            char toRank = (char)('8' - to.Row);

            string result = $"{fromFile}{fromRank}{toFile}{toRank}";

            if (promotionPiece.HasValue)
            {
                result += GetPromotionChar(promotionPiece.Value);
            }

            return result;
        }

        private static char GetPromotionChar(PieceType pieceType)
        {
            return pieceType switch
            {
                PieceType.Queen => 'q',
                PieceType.Rook => 'r',
                PieceType.Bishop => 'b',
                PieceType.Knight => 'n',
                _ => throw new ArgumentException($"Invalid promotion piece type: {pieceType}")
            };
        }

        // Keep for backward compatibility
        public static ((int, int), (int, int), PieceType?) ConvertAlgebraicToCoordinates(string move)
        {
            var parsed = ParseMove(move);
            return (parsed.From, parsed.To, parsed.PromotionPiece);
        }

        public static string ConvertCoordinatesToAlgebraic((int, int) from, (int, int) to, PieceType? promotionPiece = null)
        {
            return ToAlgebraicNotation(from, to, promotionPiece);
        }
    }
}
