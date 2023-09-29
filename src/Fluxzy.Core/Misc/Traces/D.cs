using System;
using System.Text;

namespace Fluxzy.Misc.Traces
{
    /// <summary>
    /// A class that contains all the debug traces.
    /// </summary>
    internal static class D
    {
        private static readonly object Lock = new();

        public static bool EnableTracing { get; set; } = false; 


        static D()
        {
            EnableTracing = string.Equals(Environment.GetEnvironmentVariable("EnableTracing"), "1");
        }

        /// <summary>
        /// Trace everything, slow! 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="traceType"></param>
        public static void Trace(string message, TraceType traceType)
        {
            if (!EnableTracing)
                return;

            lock (Lock) {
                var consoleColor = Console.ForegroundColor;

                try
                {
                    // get Color 
                    var color = GetColor(traceType);
                    Console.WriteLine(message);
                }
                finally
                {
                    Console.ForegroundColor = consoleColor;
                }
            }

        }

        public static void TraceException(Exception ex, string? message = null)
        {
            var builder = new StringBuilder();

            if (!string.IsNullOrEmpty(message)) {
                builder.AppendLine($"{message}: {ex.Message}");
            }
            else
            {
                builder.AppendLine(ex.Message);
            }
            
            Trace(builder.ToString(), TraceType.Error);
        }

        public static void TraceWarning(string message)
        {
            Trace(message, TraceType.Error);
        }

        public static void TraceInfo(string message)
        {
            Trace(message, TraceType.Info);
        }

        private static ConsoleColor GetColor(TraceType traceType)
        {
            switch (traceType) {
                case TraceType.Info:
                    return ConsoleColor.Blue;
                case TraceType.Warning:
                    return ConsoleColor.Yellow;
                case TraceType.Error:
                    return ConsoleColor.Red;
                default:
                    return ConsoleColor.White;
            }
        }
    }

    public enum TraceType
    {
        Info,
        Warning,
        Error
    }
}
