using System;
using System.Collections.Generic;
using System.Text;

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
        public (int, int) From { get; set; }
        public (int, int) To { get; set; }
        public Piece MovedPiece { get; set; }
        public Piece? CapturedPiece { get; set; }
        public (int, int)? PreviousEnPassantSquare { get; set; }
        public List<KastleRights> PreviousCastleRights { get; set; }
        public int PreviousHalfmoveClock { get; set; }
        public int PreviousFullmoveNumber { get; set; }
        public PlayerColor PreviousActiveColor { get; set; }
        public bool PreviousCheckState { get; set; }
        public PieceType? PromotedFrom { get; set; }
    }

    internal class Board
    {
        //public List<Piece> Pieces { get; private set; } = new List<Piece>();
        public List<List<Piece?>> ChessBoard {  get; set; } = new List<List<Piece?>>();

        public PlayerColor ActiveColor { get; private set; } = PlayerColor.White;

        public List<KastleRights> CastleRights { get; private set; } = new List<KastleRights>()
        {
            KastleRights.WhiteKingside,
            KastleRights.WhiteQueenside,
            KastleRights.BlackKingside,
            KastleRights.BlackQueenside
        };

        public (int, int)? EnPassantTargetSquare { get; private set; } = null;

        public int HalfmoveClock { get; private set; } = 0;
        public int FullmoveNumber { get; private set; } = 1;

        public PlayerColor OurColor = PlayerColor.White;

        public bool Check { get; set; } = false;

        private Stack<MoveHistory> moveHistoryStack = new Stack<MoveHistory>();

        public Board(string fenString)
        {
            LoadFromFEN(fenString);

        }

        public void UndoMove()
        {
            if (moveHistoryStack.Count == 0)
            {
                throw new InvalidOperationException("No moves to undo");
            }

            MoveHistory lastMove = moveHistoryStack.Pop();

            // Handle promotion undo - restore pawn
            if (lastMove.PromotedFrom.HasValue)
            {
                Piece pawn = new Piece(lastMove.PromotedFrom.Value, lastMove.MovedPiece.Color, lastMove.From);
                ChessBoard[lastMove.From.Item1][lastMove.From.Item2] = pawn;
            }
            else
            {
                // Restore the moved piece to its original position
                ChessBoard[lastMove.From.Item1][lastMove.From.Item2] = lastMove.MovedPiece;
                lastMove.MovedPiece.Position = lastMove.From;
            }

            // Handle castling undo
            if (lastMove.MovedPiece.Type == PieceType.King)
            {
                int columnDiff = lastMove.To.Item2 - lastMove.From.Item2;
                
                // Undo kingside castle
                if (columnDiff == 2)
                {
                    Piece rook = ChessBoard[lastMove.From.Item1][5] ?? throw new Exception("Rook not found for undoing castling");
                    ChessBoard[lastMove.From.Item1][5] = null;
                    ChessBoard[lastMove.From.Item1][7] = rook;
                    rook.Position = (lastMove.From.Item1, 7);
                }
                // Undo queenside castle
                else if (columnDiff == -2)
                {
                    Piece rook = ChessBoard[lastMove.From.Item1][3] ?? throw new Exception("Rook not found for undoing castling");
                    ChessBoard[lastMove.From.Item1][3] = null;
                    ChessBoard[lastMove.From.Item1][0] = rook;
                    rook.Position = (lastMove.From.Item1, 0);
                }
            }

            // Handle en passant undo
            if (lastMove.MovedPiece.Type == PieceType.Pawn && 
                lastMove.PreviousEnPassantSquare != null && 
                lastMove.To == lastMove.PreviousEnPassantSquare)
            {
                // This was an en passant capture
                // Restore the captured pawn to its actual position
                int capturedPawnRow = lastMove.MovedPiece.Color == PlayerColor.White ? lastMove.To.Item1 + 1 : lastMove.To.Item1 - 1;
                ChessBoard[capturedPawnRow][lastMove.To.Item2] = lastMove.CapturedPiece;
                ChessBoard[lastMove.To.Item1][lastMove.To.Item2] = null;
            }
            else
            {
                // Normal move - restore captured piece (if any) to target square
                ChessBoard[lastMove.To.Item1][lastMove.To.Item2] = lastMove.CapturedPiece;
            }

            // Restore board state
            EnPassantTargetSquare = lastMove.PreviousEnPassantSquare;
            CastleRights = lastMove.PreviousCastleRights;
            HalfmoveClock = lastMove.PreviousHalfmoveClock;
            FullmoveNumber = lastMove.PreviousFullmoveNumber;
            ActiveColor = lastMove.PreviousActiveColor;
            Check = lastMove.PreviousCheckState;
        }

        public void MakeMove((int, int) from, (int, int) to, PieceType? promotionPiece = null)
        {
            Piece movedPiece = ChessBoard[from.Item1][from.Item2] ?? throw new Exception("Tried to move from a field with no Piece");
            Piece? capturedPiece = ChessBoard[to.Item1][to.Item2];

            MoveHistory history = new MoveHistory
            {
                From = from,
                To = to,
                MovedPiece = movedPiece,
                CapturedPiece = capturedPiece,
                PreviousEnPassantSquare = EnPassantTargetSquare,
                PreviousCastleRights = new List<KastleRights>(CastleRights),
                PreviousHalfmoveClock = HalfmoveClock,
                PreviousFullmoveNumber = FullmoveNumber,
                PreviousActiveColor = ActiveColor,
                PreviousCheckState = Check,
                PromotedFrom = null
            };

            moveHistoryStack.Push(history);

            // Handle castling
            if (movedPiece.Type == PieceType.King)
            {
                int columnDiff = to.Item2 - from.Item2;
                
                // Kingside castle
                if (columnDiff == 2)
                {
                    // Move the rook
                    Piece rook = ChessBoard[from.Item1][7] ?? throw new Exception("Rook not found for castling");
                    ChessBoard[from.Item1][7] = null;
                    ChessBoard[from.Item1][5] = rook;
                    rook.Position = (from.Item1, 5);
                }
                // Queenside castle
                else if (columnDiff == -2)
                {
                    // Move the rook
                    Piece rook = ChessBoard[from.Item1][0] ?? throw new Exception("Rook not found for castling");
                    ChessBoard[from.Item1][0] = null;
                    ChessBoard[from.Item1][3] = rook;
                    rook.Position = (from.Item1, 3);
                }

                // Remove castling rights for this color
                if (movedPiece.Color == PlayerColor.White)
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

            // Remove castling rights if rook moves or is captured
            if (movedPiece.Type == PieceType.Rook)
            {
                if (movedPiece.Color == PlayerColor.White)
                {
                    if (from.Item2 == 0) CastleRights.Remove(KastleRights.WhiteQueenside);
                    if (from.Item2 == 7) CastleRights.Remove(KastleRights.WhiteKingside);
                }
                else
                {
                    if (from.Item2 == 0) CastleRights.Remove(KastleRights.BlackQueenside);
                    if (from.Item2 == 7) CastleRights.Remove(KastleRights.BlackKingside);
                }
            }

            // Remove castling rights if rook is captured
            if (capturedPiece != null && capturedPiece.Type == PieceType.Rook)
            {
                if (capturedPiece.Color == PlayerColor.White)
                {
                    if (to.Item2 == 0) CastleRights.Remove(KastleRights.WhiteQueenside);
                    if (to.Item2 == 7) CastleRights.Remove(KastleRights.WhiteKingside);
                }
                else
                {
                    if (to.Item2 == 0) CastleRights.Remove(KastleRights.BlackQueenside);
                    if (to.Item2 == 7) CastleRights.Remove(KastleRights.BlackKingside);
                }
            }

            // Handle en passant capture
            bool isEnPassantCapture = false;
            if (movedPiece.Type == PieceType.Pawn && EnPassantTargetSquare != null && to == EnPassantTargetSquare)
            {
                isEnPassantCapture = true;
                // Capture the pawn that is not on the target square
                int capturedPawnRow = movedPiece.Color == PlayerColor.White ? to.Item1 + 1 : to.Item1 - 1;
                capturedPiece = ChessBoard[capturedPawnRow][to.Item2];
                ChessBoard[capturedPawnRow][to.Item2] = null;
                // Store the actual captured piece in history for undo
                history.CapturedPiece = capturedPiece;
            }

            // Clear en passant square
            EnPassantTargetSquare = null;

            // Set new en passant square if pawn moved two squares
            if (movedPiece.Type == PieceType.Pawn && Math.Abs(to.Item1 - from.Item1) == 2)
            {
                EnPassantTargetSquare = ((from.Item1 + to.Item1) / 2, from.Item2);
            }

            // Move the piece
            ChessBoard[from.Item1][from.Item2] = null;
            movedPiece.Position = to;
            ChessBoard[to.Item1][to.Item2] = movedPiece;

            // Handle pawn promotion
            if (movedPiece.Type == PieceType.Pawn)
            {
                int promotionRank = movedPiece.Color == PlayerColor.White ? 0 : 7;
                if (to.Item1 == promotionRank)
                {
                    history.PromotedFrom = PieceType.Pawn;
                    PieceType newPieceType = promotionPiece ?? PieceType.Queen; // Default to queen
                    Piece promotedPiece = new Piece(newPieceType, movedPiece.Color, to);
                    ChessBoard[to.Item1][to.Item2] = promotedPiece;
                }
            }

            // Update halfmove clock
            if (movedPiece.Type == PieceType.Pawn || capturedPiece != null)
            {
                HalfmoveClock = 0;
            }
            else
            {
                HalfmoveClock++;
            }

            // Update fullmove number
            if (ActiveColor == PlayerColor.Black)
            {
                FullmoveNumber++;
            }

            // Switch active color
            ActiveColor = ActiveColor == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;

            // Check if the king is in check
            foreach (var field in ChessBoard)
            {
                foreach (var piece in field)
                {
                    if (piece is null || piece.Type != PieceType.King || piece.Color != OurColor) continue;
                    Check = piece.SquareIsUnderAttack(piece.Position, this);
                    return;
                }
            }
        }

        private void LoadFromFEN(string fenString)
        {
            string[] parts = fenString.Split(' ');
            if (parts.Length < 1)
            {
                throw new ArgumentException("Invalid FEN string");
            }



            HandlePieces(parts[0]);
            HandlePlayerColor(parts[1]);
            HandleCastle(parts[2]);
            HandleEnPassant(parts[3]);
            HandleHalfTurns(parts[4]);
            HandleFullTurns(parts[5]);
        }

        private void HandleCastle(string v)
        {
            CastleRights.Clear();
            if (v.Contains('K'))
            {
                CastleRights.Add(KastleRights.WhiteKingside);
            }
            if (v.Contains('Q'))
            {
                CastleRights.Add(KastleRights.WhiteQueenside);
            }
            if (v.Contains('k'))
            {
                CastleRights.Add(KastleRights.BlackKingside);
            }
            if (v.Contains('q'))
            {
                CastleRights.Add(KastleRights.BlackQueenside);
            }
        }

        private void HandleEnPassant(string v)
        {
            if (v == "-")
            {
                EnPassantTargetSquare = null;
            }
            else
            {
                char fileChar = v[0];
                char rankChar = v[1];
                int file = fileChar - 'a';
                int rank = rankChar - '1';
                EnPassantTargetSquare = (file, rank);
            }
        }

        private void HandleHalfTurns(string v)
        {
            int halfmoves = int.Parse(v);
            HalfmoveClock = halfmoves;
        }

        private void HandleFullTurns(string v)
        {
            int fullMoves = int.Parse(v);
            FullmoveNumber = fullMoves;
        }

        private void HandlePlayerColor(string v)
        {
            if (v == "b")
            {
                ActiveColor = PlayerColor.Black;
            }
            else
            {
                ActiveColor = PlayerColor.White;
            }
            }

        private void HandlePieces(string line)
        {
            string[] rows = line.Split('/');
            if (rows.Length != 8)
            {
                throw new ArgumentException("Invalid FEN string: incorrect number of rows");
            }

            ChessBoard.Clear();
            for (int row = 0; row < rows.Length; row++)
            {
                List<Piece?> rowPieces = [.. Enumerable.Repeat<Piece?>(null, 8)];

                int column = 0;
                foreach (char c in rows[row])
                {
                    if (char.IsDigit(c))
                    {
                        column += (c - '0');
                    }
                    else
                    {
                        PlayerColor color = char.IsUpper(c) ? PlayerColor.White : PlayerColor.Black;
                        PieceType type = c.ToString().ToLower() switch
                        {
                            "p" => PieceType.Pawn,
                            "r" => PieceType.Rook,
                            "n" => PieceType.Knight,
                            "b" => PieceType.Bishop,
                            "q" => PieceType.Queen,
                            "k" => PieceType.King,
                            _ => throw new ArgumentException($"Invalid piece character: {c}")
                        };
                        rowPieces[column] = new Piece(type, color, (row, column));
                        column++;
                    }
                }
                ChessBoard.Add(rowPieces);
            }
        }
    }
}
