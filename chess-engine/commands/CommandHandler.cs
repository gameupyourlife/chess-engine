using chess_engine.engine;
using chess_engine.game;
using System;
using System.Collections.Generic;

namespace chess_engine.commands
{
    internal class CommandHandler
    {
        public Board? Board { get; set; }

        private readonly IOService _ioService;
        private readonly Evaluator _evaluator = new();
        private readonly MoveGenerator _moveGenerator = new();

        public CommandHandler(IOService ioService)
        {
            _ioService = ioService;
        }

        public void HandleCommand(InputCommand command, string input)
        {
            switch (command)
            {
                case InputCommand.UCI:
                    HandleUciCommand();
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
                    HandleGoCommand();
                    break;
                default:
                    Console.WriteLine($"Command {command} received but not implemented yet.");
                    break;
            }
        }

        private static void HandleUciCommand()
        {
            Console.WriteLine("id name DCMachine");
            Console.WriteLine("id author Daniel and Cedric");
            Console.WriteLine("uciok");
        }

        private void HandleGoCommand()
        {
            if (Board is null)
            {
                Console.WriteLine("info string No position set");
                return;
            }

            Board.OurColor = Board.ActiveColor;

            var evaluatedMoves = EvaluateAllMoves();

            if (evaluatedMoves.Count == 0)
            {
                Console.WriteLine("info string No legal moves available");
                return;
            }

            var bestMove = FindBestMove(evaluatedMoves);
            _ioService.SendBestMove((bestMove.Move.From, bestMove.Move.To, bestMove.Move.PromotionPiece));
        }

        private List<(Move Move, int Value)> EvaluateAllMoves()
        {
            var evaluatedMoves = new List<(Move Move, int Value)>();
            var legalMoves = _moveGenerator.GetAllLegalMoves(Board!, Board!.OurColor);

            foreach (var move in legalMoves)
            {
                Board!.MakeMove(move.From, move.To, move.PromotionPiece);

                // Evaluate from opponent's perspective (they will move next), then negate
                var opponentColor = Board.OurColor == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;
                int value = -_evaluator.EvaluateWithDepth(Board, 4, opponentColor, int.MinValue, int.MaxValue);

                Console.WriteLine("{0} {1}", move.ToString(), value);

                evaluatedMoves.Add((move, value));

                Board.UndoMove();
            }

            return evaluatedMoves;
        }

        private static (Move Move, int Value) FindBestMove(List<(Move Move, int Value)> evaluatedMoves)
        {
            var bestMove = evaluatedMoves[0];

            foreach (var move in evaluatedMoves)
            {
                if (move.Value > bestMove.Value)
                {
                    bestMove = move;
                }
            }

            return bestMove;
        }
    }
}
