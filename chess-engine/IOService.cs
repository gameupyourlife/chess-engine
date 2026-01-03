using chess_engine.commands;
using chess_engine.game;
using System;
using System.Collections.Generic;
using System.Text;

namespace chess_engine
{
    enum InputCommand
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

        public IOService() { }

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

        public (InputCommand, string)? ProcessInput(string input)
        {
            var command = ParseInput(input);
            if (command == null)
            {
                Console.WriteLine("Unknown command");
                return null;
            }
            return ((InputCommand, string)?)(command, input);
        }

        public void StartListening(CommandHandler handler)
        {
            
            while (true)
            {
                string input = GetInput();
                (InputCommand, string)? processed = ProcessInput(input);

                if (processed is null) continue;

                handler.HandleCommand(processed.Value.Item1, processed.Value.Item2);
            }
        }

        public void SendBestMove(((int, int), (int, int), PieceType?) move)
        {
            string fromAlgebraic = PositionCommandHandler.ConvertCoordinatesToAlgebraic(move.Item1, move.Item2, move.Item3);
            Console.WriteLine($"bestmove {fromAlgebraic}");
        }

        public void SendOutput(string output)
        {
            Console.WriteLine(output);
        }
    }
}
