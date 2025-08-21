using ArcXray.Contracts;

namespace ArcXray.Cli.Loggers
{
    internal class ConsoleLogger : ILogger
    {
        public void Error(string message)
        {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {message}");
            Console.ForegroundColor = prevColor;
        }

        public void Info(string message)
        {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"[INFO] {message}");
            Console.ForegroundColor = prevColor;
        }

        public void Warn(string message)
        {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[WARN] {message}");
            Console.ForegroundColor = prevColor;
        }

        public void Debug(string message)
        {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"[DEBUG] {message}");
            Console.ForegroundColor = prevColor;
        }

        public void Log(string message)
        {
            Console.WriteLine(message);
        }
    }

}
