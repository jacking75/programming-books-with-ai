// 파일: samples/src/Common/ConsoleHelpers.cs
// 모든 장 예제가 사용하는 공용 콘솔 헬퍼.

using System.Globalization;

namespace AsyncAwaitLab.Common;

public static class ConsoleHelpers
{
    private static readonly object _lock = new();
    private static readonly long _start = Stopwatch.GetTimestamp();

    public static void Log(string message)
    {
        var elapsed = Stopwatch.GetElapsedTime(_start);
        var ms = elapsed.TotalMilliseconds.ToString("0000.0", CultureInfo.InvariantCulture);
        var tid = Environment.CurrentManagedThreadId.ToString("00");
        lock (_lock)
        {
            Console.WriteLine($"[{ms} ms][T#{tid}] {message}");
        }
    }

    public static void Banner(string title)
    {
        var line = new string('=', Math.Max(20, title.Length + 8));
        Console.WriteLine();
        Console.WriteLine(line);
        Console.WriteLine($"   {title}");
        Console.WriteLine(line);
    }

    public static void Pause(string prompt = "Enter를 누르면 다음으로...")
    {
        if (!Environment.UserInteractive || Console.IsInputRedirected) return;
        Console.Write(prompt);
        Console.ReadLine();
    }
}
