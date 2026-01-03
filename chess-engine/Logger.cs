using System;

namespace chess_engine
{
    /// <summary>
    /// Simple logger for console output with different log levels.
    /// </summary>
    internal static class Logger
    {
        public static void LogInfo(string message)
        {
            Console.WriteLine($"info string {message}");
        }

        public static void LogError(string message)
        {
            Console.WriteLine($"info string ERROR: {message}");
        }

        public static void LogException(Exception exception, string context = "")
        {
            var contextPrefix = string.IsNullOrEmpty(context) ? "" : $"{context}: ";
            Console.WriteLine($"info string EXCEPTION: {contextPrefix}{exception.GetType().Name}");
            Console.WriteLine($"info string Message: {exception.Message}");
            
            if (exception.StackTrace != null)
            {
                var stackLines = exception.StackTrace.Split('\n');
                foreach (var line in stackLines.Take(5)) // Only log first 5 stack trace lines
                {
                    Console.WriteLine($"info string   {line.Trim()}");
                }
            }

            // Log inner exception if present
            if (exception.InnerException != null)
            {
                Console.WriteLine($"info string Inner Exception: {exception.InnerException.GetType().Name}");
                Console.WriteLine($"info string Inner Message: {exception.InnerException.Message}");
            }
        }

        public static void LogFatalException(Exception exception)
        {
            Console.WriteLine("info string ===== FATAL UNHANDLED EXCEPTION =====");
            LogException(exception, "Fatal Error");
            Console.WriteLine("info string ======================================");
        }
    }
}
