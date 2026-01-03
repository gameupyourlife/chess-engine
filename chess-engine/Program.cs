using chess_engine.game;
using chess_engine.commands;

namespace chess_engine
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IOService ioService = new IOService();
            CommandHandler commandHandler = new CommandHandler(ioService);
            ioService.StartListening(commandHandler);


        }
    }
}
