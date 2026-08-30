namespace MMORPG2D.GameServer;

/// <summary>
/// 1주차에서는 Console.WriteLine 한 줄짜리 단순 로거로 시작한다.
/// 8주차에서 ZLogger 기반 비동기 로거로 교체한다.
/// </summary>
public static class Logger
{
    public static void Info(string msg)
        => Write("INFO", ConsoleColor.Gray, msg);

    public static void Warn(string msg)
        => Write("WARN", ConsoleColor.Yellow, msg);

    public static void Error(string msg, Exception? ex = null)
        => Write("ERR ", ConsoleColor.Red, ex == null ? msg : $"{msg} :: {ex}");

    private static void Write(string level, ConsoleColor color, string msg)
    {
        var line = $"[{DateTime.UtcNow:HH:mm:ss.fff}][{level}] {msg}";
        lock (typeof(Logger))
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(line);
            Console.ForegroundColor = prev;
        }
    }
}
