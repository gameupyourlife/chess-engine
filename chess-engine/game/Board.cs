using System;
using System.Collections.Generic;

namespace chess_engine.game
{
    public enum PlayerColor
    {
        White,
        Black
    }

    public enum KastleRights
    {
        WhiteKingside,
        WhiteQueenside,
        BlackKingside,
        BlackQueenside
    }

    internal class MoveHistory
    {
        public Square From { get; set; }
        public Square To { get; set; }
        public Piece MovedPiece { get; set; } = null!;
        public Piece? CapturedPiece { get; set; }
        public Square? PreviousEnPassantSquare { get; set; }
        public List<KastleRights> PreviousCastleRights { get; set; } = [];
        public int PreviousHalfmoveClock { get; set; }
        public int PreviousFullmoveNumber { get; set; }
        public PlayerColor PreviousActiveColor { get; set; }
        public bool PreviousCheckState { get; set; }
        public PieceType? PromotedFrom { get; set; }
    }

    internal class Board
    {
        private static readonly AttackDetector _attackDetector = new();

        public List<List<Piece?>> ChessBoard { get; set; } = [];

        public PlayerColor ActiveColor { get; private set; } = PlayerColor.White;

        public List<KastleRights> CastleRights { get; private set; } =
        [
            KastleRights.WhiteKingside,
            KastleRights.WhiteQueenside,
            KastleRights.BlackKingside,
            KastleRights.BlackQueenside
        ];

        public (int, int)? EnPassantTargetSquare { get; private set; } = null;

        public int HalfmoveClock { get; private set; } = 0;
        public int FullmoveNumber { get; private set; } = 1;

        public PlayerColor OurColor = PlayerColor.White;

        public bool Check { get; set; } = false;

        private readonly Stack<MoveHistory> _moveHistoryStack = new();

        public bool IsFiftyMoveRuleDraw()
        {
            return HalfmoveClock >= 100;
        }

        public Board(string fenString)
        {
            LoadFromFEN(fenString);
        }

        public void UndoMove()
        {
            if (_moveHistoryStack.Count == 0)
            {
                throw new InvalidOperationException("No moves to undo");
            }

            MoveHistory lastMove = _moveHistoryStack.Pop();

            // Handle promotion undo - restore pawn
            if (lastMove.PromotedFrom.HasValue)
            {
                Piece pawn = new Piece(lastMove.PromotedFrom.Value, lastMove.MovedPiece.Color, lastMove.From);
                ChessBoard[lastMove.From.Row][lastMove.From.Col] = pawn;
            }
            else
            {
                // Restore the moved piece to its original position
                ChessBoard[lastMove.From.Row][lastMove.From.Col] = lastMove.MovedPiece;
                lastMove.MovedPiece.Position = lastMove.From;
            }

            // Handle castling undo
            if (lastMove.MovedPiece.Type == PieceType.King)
            {
                UndoCastling(lastMove);
            }

            // Handle en passant undo
            if (lastMove.MovedPiece.Type == PieceType.Pawn &&
                lastMove.PreviousEnPassantSquare != null &&
                lastMove.To == lastMove.PreviousEnPassantSquare)
            {
                // This was an en passant capture - restore the captured pawn
                int capturedPawnRow = lastMove.MovedPiece.Color == PlayerColor.White
                    ? lastMove.To.Row + 1
                    : lastMove.To.Row - 1;
                ChessBoard[capturedPawnRow][lastMove.To.Col] = lastMove.CapturedPiece;
                ChessBoard[lastMove.To.Row][lastMove.To.Col] = null;
            }
            else
            {
                // Normal move - restore captured piece (if any) to target square
                ChessBoard[lastMove.To.Row][lastMove.To.Col] = lastMove.CapturedPiece;
            }

            // Restore board state
            EnPassantTargetSquare = lastMove.PreviousEnPassantSquare;
            CastleRights = lastMove.PreviousCastleRights;
            HalfmoveClock = lastMove.PreviousHalfmoveClock;
            FullmoveNumber = lastMove.PreviousFullmoveNumber;
            ActiveColor = lastMove.PreviousActiveColor;
            Check = lastMove.PreviousCheckState;
        }

        private void UndoCastling(MoveHistory lastMove)
        {
            int columnDiff = lastMove.To.Col - lastMove.From.Col;

            // Undo kingside castle
            if (columnDiff == 2)
            {
                Piece rook = ChessBoard[lastMove.From.Row][5]
                    ?? throw new Exception("Rook not found for undoing castling");
                ChessBoard[lastMove.From.Row][5] = null;
                ChessBoard[lastMove.From.Row][7] = rook;
                rook.Position = new Square(lastMove.From.Row, 7);
            }
            // Undo queenside castle
            else if (columnDiff == -2)
            {
                Piece rook = ChessBoard[lastMove.From.Row][3]
                    ?? throw new Exception("Rook not found for undoing castling");
                ChessBoard[lastMove.From.Row][3] = null;
                ChessBoard[lastMove.From.Row][0] = rook;
                rook.Position = new Square(lastMove.From.Row, 0);
            }
        }

        public void MakeMove(Square from, Square to, PieceType? promotionPiece = null)
        {
            Piece? movedPiece = ChessBoard[from.Row][from.Col];
            if(movedPiece == null)    
                throw new Exception($"Tried to move from a field with no Piece {from.ToString()} {to.ToString()}");
            Piece? capturedPiece = ChessBoard[to.Row][to.Col];

            MoveHistory history = new()
            {
                From = from,
                To = to,
                MovedPiece = movedPiece,
                CapturedPiece = capturedPiece,
                PreviousEnPassantSquare = EnPassantTargetSquare.HasValue
                    ? new Square(EnPassantTargetSquare.Value.Item1, EnPassantTargetSquare.Value.Item2)
                    : null,
                PreviousCastleRights = new List<KastleRights>(CastleRights),
                PreviousHalfmoveClock = HalfmoveClock,
                PreviousFullmoveNumber = FullmoveNumber,
                PreviousActiveColor = ActiveColor,
                PreviousCheckState = Check,
                PromotedFrom = null
            };

            _moveHistoryStack.Push(history);

            // Handle special moves
            HandleCastling(movedPiece, from, to);
            UpdateCastlingRights(movedPiece, capturedPiece, from, to);
            capturedPiece = HandleEnPassant(movedPiece, to, capturedPiece, history);
            UpdateEnPassantSquare(movedPiece, from, to);

            // Move the piece
            ChessBoard[from.Row][from.Col] = null;
            movedPiece.Position = to;
            ChessBoard[to.Row][to.Col] = movedPiece;

            // Handle pawn promotion
            HandlePromotion(movedPiece, to, promotionPiece, history);

            // Update clocks
            UpdateClocks(movedPiece, capturedPiece);

            // Switch active color
            ActiveColor = ActiveColor == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;

            // Update check status
            UpdateCheckStatus();
        }

        // Backward compatibility overload
        public void MakeMove((int, int) from, (int, int) to, PieceType? promotionPiece = null)
        {
            MakeMove(new Square(from.Item1, from.Item2), new Square(to.Item1, to.Item2), promotionPiece);
        }

        private void HandleCastling(Piece movedPiece, Square from, Square to)
        {
            if (movedPiece.Type != PieceType.King)
                return;

            int columnDiff = to.Col - from.Col;

            // Kingside castle
            if (columnDiff == 2)
            {
                Piece rook = ChessBoard[from.Row][7]
                    ?? throw new Exception("Rook not found for castling");
                ChessBoard[from.Row][7] = null;
                ChessBoard[from.Row][5] = rook;
                rook.Position = new Square(from.Row, 5);
            }
            // Queenside castle
            else if (columnDiff == -2)
            {
                Piece rook = ChessBoard[from.Row][0]
                    ?? throw new Exception("Rook not found for castling");
                ChessBoard[from.Row][0] = null;
                ChessBoard[from.Row][3] = rook;
                rook.Position = new Square(from.Row, 3);
            }
        }

        private void UpdateCastlingRights(Piece movedPiece, Piece? capturedPiece, Square from, Square to)
        {
            // King moves - lose all castling rights for that color
            if (movedPiece.Type == PieceType.King)
            {
                RemoveCastlingRights(movedPiece.Color);
            }

            // Rook moves
            if (movedPiece.Type == PieceType.Rook)
            {
                RemoveRookCastlingRights(movedPiece.Color, from.Col);
            }

            // Rook captured
            if (capturedPiece?.Type == PieceType.Rook)
            {
                RemoveRookCastlingRights(capturedPiece.Color, to.Col);
            }
        }

        private void RemoveCastlingRights(PlayerColor color)
        {
            if (color == PlayerColor.White)
            {
                CastleRights.Remove(KastleRights.WhiteKingside);
                CastleRights.Remove(KastleRights.WhiteQueenside);
            }
            else
            {
                CastleRights.Remove(KastleRights.BlackKingside);
                CastleRights.Remove(KastleRights.BlackQueenside);
            }
        }

        private void RemoveRookCastlingRights(PlayerColor color, int column)
        {
            if (color == PlayerColor.White)
            {
                if (column == 0) CastleRights.Remove(KastleRights.WhiteQueenside);
                if (column == 7) CastleRights.Remove(KastleRights.WhiteKingside);
            }
            else
            {
                if (column == 0) CastleRights.Remove(KastleRights.BlackQueenside);
                if (column == 7) CastleRights.Remove(KastleRights.BlackKingside);
            }
        }

        private Piece? HandleEnPassant(Piece movedPiece, Square to, Piece? capturedPiece, MoveHistory history)
        {
            if (movedPiece.Type != PieceType.Pawn || EnPassantTargetSquare == null)
                return capturedPiece;

            if (to.Row != EnPassantTargetSquare.Value.Item1 || to.Col != EnPassantTargetSquare.Value.Item2)
                return capturedPiece;

            // Capture the pawn that is not on the target square
            int capturedPawnRow = movedPiece.Color == PlayerColor.White ? to.Row + 1 : to.Row - 1;
            capturedPiece = ChessBoard[capturedPawnRow][to.Col];
            ChessBoard[capturedPawnRow][to.Col] = null;
            history.CapturedPiece = capturedPiece;

            return capturedPiece;
        }

        private void UpdateEnPassantSquare(Piece movedPiece, Square from, Square to)
        {
            EnPassantTargetSquare = null;

            if (movedPiece.Type == PieceType.Pawn && Math.Abs(to.Row - from.Row) == 2)
            {
                EnPassantTargetSquare = ((from.Row + to.Row) / 2, from.Col);
            }
        }

        private void HandlePromotion(Piece movedPiece, Square to, PieceType? promotionPiece, MoveHistory history)
        {
            if (movedPiece.Type != PieceType.Pawn)
                return;

            int promotionRank = BoardConstants.GetPromotionRank(movedPiece.Color);
            if (to.Row != promotionRank)
                return;

            history.PromotedFrom = PieceType.Pawn;
            PieceType newPieceType = promotionPiece ?? PieceType.Queen;
            Piece promotedPiece = new Piece(newPieceType, movedPiece.Color, to);
            ChessBoard[to.Row][to.Col] = promotedPiece;
        }

        private void UpdateClocks(Piece movedPiece, Piece? capturedPiece)
        {
            // Reset halfmove clock on pawn move or capture
            if (movedPiece.Type == PieceType.Pawn || capturedPiece != null)
            {
                HalfmoveClock = 0;
            }
            else
            {
                HalfmoveClock++;
            }

            // Increment fullmove number after Black's move
            if (ActiveColor == PlayerColor.Black)
            {
                FullmoveNumber++;
            }
        }

        private void UpdateCheckStatus()
        {
            // After a move, ActiveColor has already been switched to the opponent.
            // We need to check if the side that just moved left their king in check (illegal move).
            // The side that just moved is the opposite of the current ActiveColor.
            PlayerColor sideJustMoved = ActiveColor == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;
            
            for (int row = 0; row < BoardConstants.BoardSize; row++)
            {
                for (int col = 0; col < BoardConstants.BoardSize; col++)
                {
                    var piece = ChessBoard[row][col];
                    if (piece?.Type == PieceType.King && piece.Color == sideJustMoved)
                    {
                        Check = _attackDetector.IsSquareUnderAttack(piece.Position, this, sideJustMoved);
                        return;
                    }
                }
            }
        }

        private void LoadFromFEN(string fenString)
        {
            string[] parts = fenString.Split(' ');
            if (parts.Length < 6)
            {
                throw new ArgumentException("Invalid FEN string");
            }

            ParsePieces(parts[0]);
            ParseActiveColor(parts[1]);
            ParseCastlingRights(parts[2]);
            ParseEnPassant(parts[3]);
            ParseHalfmoveClock(parts[4]);
            ParseFullmoveNumber(parts[5]);
        }

        private void ParseCastlingRights(string castling)
        {
            CastleRights.Clear();
            if (castling.Contains('K')) CastleRights.Add(KastleRights.WhiteKingside);
            if (castling.Contains('Q')) CastleRights.Add(KastleRights.WhiteQueenside);
            if (castling.Contains('k')) CastleRights.Add(KastleRights.BlackKingside);
            if (castling.Contains('q')) CastleRights.Add(KastleRights.BlackQueenside);
        }

        private void ParseEnPassant(string enPassant)
        {
            if (enPassant == "-")
            {
                EnPassantTargetSquare = null;
            }
            else
            {
                int file = enPassant[0] - 'a';
                int rank = enPassant[1] - '1';
                EnPassantTargetSquare = (file, rank);
            }
        }

        private void ParseHalfmoveClock(string halfmoves)
        {
            HalfmoveClock = int.Parse(halfmoves);
        }

        private void ParseFullmoveNumber(string fullmoves)
        {
            FullmoveNumber = int.Parse(fullmoves);
        }

        private void ParseActiveColor(string color)
        {
            ActiveColor = color == "b" ? PlayerColor.Black : PlayerColor.White;
        }

        private void ParsePieces(string piecePlacement)
        {
            string[] rows = piecePlacement.Split('/');
            if (rows.Length != BoardConstants.BoardSize)
            {
                throw new ArgumentException("Invalid FEN string: incorrect number of rows");
            }

            ChessBoard.Clear();
            for (int row = 0; row < rows.Length; row++)
            {
                List<Piece?> rowPieces = [.. Enumerable.Repeat<Piece?>(null, BoardConstants.BoardSize)];

                int column = 0;
                foreach (char c in rows[row])
                {
                    if (char.IsDigit(c))
                    {
                        column += c - '0';
                    }
                    else
                    {
                        PlayerColor color = char.IsUpper(c) ? PlayerColor.White : PlayerColor.Black;
                        PieceType type = ParsePieceType(c);
                        rowPieces[column] = new Piece(type, color, (row, column));
                        column++;
                    }
                }
                ChessBoard.Add(rowPieces);
            }
        }

        private static PieceType ParsePieceType(char c)
        {
            return char.ToLower(c) switch
            {
                'p' => PieceType.Pawn,
                'r' => PieceType.Rook,
                'n' => PieceType.Knight,
                'b' => PieceType.Bishop,
                'q' => PieceType.Queen,
                'k' => PieceType.King,
                _ => throw new ArgumentException($"Invalid piece character: {c}")
            };
        }
    }
}
