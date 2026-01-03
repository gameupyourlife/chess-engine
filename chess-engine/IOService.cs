using chess_engine.commands;
using chess_engine.game;
using System;

namespace chess_engine
{
    internal enum InputCommand
    {
        UCI,
        DEBUG,
        ISREADY,
        SETOPTION,
        UCINEWGAME,
        GO,
        POSITION,
        QUIT,
        STOP,
    }

    internal class IOService
    {
        public string GetInput()
        {
            return Console.ReadLine() ?? string.Empty;
        }

        public InputCommand? ParseInput(string input)
        {
            var command = input.Split(' ')[0].ToUpper();
            return command switch
            {
                "UCI" => InputCommand.UCI,
                "DEBUG" => InputCommand.DEBUG,
                "ISREADY" => InputCommand.ISREADY,
                "SETOPTION" => InputCommand.SETOPTION,
                "UCINEWGAME" => InputCommand.UCINEWGAME,
                "GO" => InputCommand.GO,
                "POSITION" => InputCommand.POSITION,
                "QUIT" => InputCommand.QUIT,
                "STOP" => InputCommand.STOP,
                _ => null,
            };
        }

        public (InputCommand Command, string Input)? ProcessInput(string input)
        {
            var command = ParseInput(input);
            if (command == null)
            {
                Console.WriteLine("Unknown command");
                return null;
            }
            return (command.Value, input);
        }

        public void StartListening(CommandHandler handler)
        {
            while (true)
            {
                string input = GetInput();
                var processed = ProcessInput(input);

                if (processed is null)
                    continue;

                handler.HandleCommand(processed.Value.Command, processed.Value.Input);
            }
        }

        public void SendBestMove(((int, int) From, (int, int) To, PieceType? PromotionPiece) move)
        {
            string algebraic = PositionCommandHandler.ToAlgebraicNotation(move.From, move.To, move.PromotionPiece);
            Console.WriteLine($"bestmove {algebraic}");
        }

        public void SendOutput(string output)
        {
            Console.WriteLine(output);
        }
    }
}
