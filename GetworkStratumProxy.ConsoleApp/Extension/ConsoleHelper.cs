using System;

namespace GetworkStratumProxy.ConsoleApp.Extension
{
    public enum LogLevel
    {
        Debug,
        Information,
        Warning,
        Error,
        Success
    }

    public static class ConsoleHelper
    {
        private static object _lock = new object();

        public static bool IsVerbose { get; set; } = false;

        public static void Log(object subject, string message, LogLevel logLevel)
        {
            if (logLevel == LogLevel.Debug && !IsVerbose)
            {
                return;
            }

            lock (_lock)
            {
                var previousForegroundColour = Console.ForegroundColor;
                var foregroundColour = logLevel switch
                {
                    LogLevel.Debug => ConsoleColor.Cyan,
                    LogLevel.Information => ConsoleColor.White,
                    LogLevel.Warning => ConsoleColor.Yellow,
                    LogLevel.Error => ConsoleColor.Red,
                    LogLevel.Success => ConsoleColor.Green,
                    _ => ConsoleColor.White
                };

                Console.ForegroundColor = foregroundColour;
                Console.WriteLine($"[{DateTime.Now}] [{subject}] {message}");
                Console.ForegroundColor = previousForegroundColour;
            }
        }
    }
}
