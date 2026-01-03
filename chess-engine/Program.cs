using chess_engine.game;
using chess_engine.commands;
using System;

namespace chess_engine
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Set up global exception handlers
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;

            try
            {
                IOService ioService = new IOService();
                CommandHandler commandHandler = new CommandHandler(ioService);
                ioService.StartListening(commandHandler);
            }
            catch (Exception ex)
            {
                Logger.LogFatalException(ex);
                Environment.Exit(1);
            }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                Logger.LogFatalException(exception);
            }
            else
            {
                Logger.LogError($"Unhandled non-exception object: {e.ExceptionObject}");
            }

            // If terminating, exit gracefully
            if (e.IsTerminating)
            {
                Logger.LogError("Application is terminating due to unhandled exception");
                Environment.Exit(1);
            }
        }

        private static void OnFirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            // Only log severe exceptions at first chance to avoid noise
            // You can uncomment this for debugging purposes
            // Logger.LogException(e.Exception, "First Chance");
        }
    }
}
