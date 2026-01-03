using chess_engine.engine;
using chess_engine.game;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace chess_engine.commands
{
    internal class CommandHandler
    {
        public Board? Board { get; set; }

        private IOService iOService;
        private Evaluator evaluator = new Evaluator();

        public CommandHandler(IOService iOService)
        {
            this.iOService = iOService;
        }


        public void HandleCommand(InputCommand command, string input)
        {
            switch (command)
            {
                case InputCommand.UCI:
                    Console.WriteLine("id name DCMachine");
                    Console.WriteLine("id author Daniel and Cedric");
                    Console.WriteLine("uciok");
                    break;
                case InputCommand.ISREADY:
                    Console.WriteLine("readyok");
                    break;
                case InputCommand.QUIT:
                    Environment.Exit(0);
                    break;
                case InputCommand.POSITION:
                    Board = PositionCommandHandler.HandlePositionCommand(input);
                    break;
                case InputCommand.GO:

                    var valueMoves = new List<((int, int) from, (int, int) to, PieceType? promotionPiece, int value)>();
                    Board.OurColor = Board.ActiveColor;

                    for (int col =0; col<8; col++)
                    {
                        for(int row=0; row<8; row++)
                        {
                            var piece = Board?.ChessBoard[row][col];
                            if (piece is null || piece.Color != Board.OurColor) continue;
                            piece.CalculateValidTargetPositions(Board);

                            foreach (var target in piece.ValidTargetPostions)
                            {
                                // Check if this is a pawn promotion
                                bool isPromotion = piece.Type == PieceType.Pawn && 
                                                  ((piece.Color == PlayerColor.White && target.Item1 == 0) ||
                                                   (piece.Color == PlayerColor.Black && target.Item1 == 7));

                                if (isPromotion)
                                {
                                    // Generate all four promotion options
                                    var promotionPieces = new[] { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight };
                                    
                                    foreach (var promotionPiece in promotionPieces)
                                    {
                                        bool wasInCheck = Board.Check;
                                        var oldPos = piece.Position;
                                        Board.MakeMove(piece.Position, target, promotionPiece);
                                        
                                        // Check if move is illegal (leaves us in check)
                                        if(Board.Check && wasInCheck)
                                        {
                                            Board.UndoMove();
                                            continue;
                                        }

                                        // Evaluate position with 5-move depth
                                        int value = evaluator.EvaluateWithDepth(Board, 5, false, int.MinValue, int.MaxValue);

                                        valueMoves.Add((oldPos, target, promotionPiece, value));

                                        Board.UndoMove();

                                        Console.WriteLine($"{PositionCommandHandler.ConvertCoordinatesToAlgebraic(oldPos, target, promotionPiece)} {value}");
                                    }
                                }
                                else
                                {
                                    bool wasInCheck = Board.Check;
                                    var oldPos = piece.Position;
                                    Board.MakeMove(piece.Position, target);
                                    
                                    // Check if move is illegal (leaves us in check)
                                    if(Board.Check && wasInCheck)
                                    {
                                        Board.UndoMove();
                                        continue;
                                    }

                                    // Evaluate position with 5-move depth
                                    int value = evaluator.EvaluateWithDepth(Board, 5, false, int.MinValue, int.MaxValue);

                                    valueMoves.Add((oldPos, target, null, value));

                                    Board.UndoMove();

                                    Console.WriteLine($"{PositionCommandHandler.ConvertCoordinatesToAlgebraic(oldPos, target)} {value}");
                                }
                            }
                        }
                    }

                    // Find max value move
                    var currentMax = valueMoves[0];
                    foreach (var move in valueMoves)
                    {
                        if (move.value > currentMax.value)
                        {
                            currentMax = move;
                        }
                    }

                    iOService.SendBestMove((currentMax.from, currentMax.to, currentMax.promotionPiece));

                    break;
                default:
                    Console.WriteLine($"Command {command} received but not implemented yet.");
                    break;
            }
        }
    }
}
