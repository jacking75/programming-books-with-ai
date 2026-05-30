// code/ch07/CsvLogWriter/CsvLogWriter.cs
using System.Text;

namespace GameCsvWriter;

/// <summary>
/// 고성능 CSV 로그 라이터.
/// StreamWriter 기반, RFC 4180 이스케이프 처리.
/// </summary>
public sealed class CsvLogWriter : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly object _lock = new();
    private bool _headerWritten;

    public CsvLogWriter(string filePath, bool append = true)
    {
        // 파일 디렉터리가 없으면 생성
        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        bool fileExists = File.Exists(filePath) && new FileInfo(filePath).Length > 0;

        _writer = new StreamWriter(
            new FileStream(filePath, append ? FileMode.Append : FileMode.Create,
                FileAccess.Write, FileShare.Read, 65536),
            Encoding.UTF8,
            leaveOpen: false);

        // 파일이 이미 존재하고 내용이 있으면 헤더를 다시 쓰지 않는다
        _headerWritten = fileExists && append;
    }

    /// <summary>헤더 행을 기록한다 (최초 1회만).</summary>
    public void WriteHeader(IEnumerable<string> columns)
    {
        lock (_lock)
        {
            if (_headerWritten) return;
            _writer.WriteLine(JoinCsvRow(columns));
            _headerWritten = true;
        }
    }

    /// <summary>데이터 행을 기록한다 (스레드 안전).</summary>
    public void WriteRow(IEnumerable<string?> values)
    {
        string line = JoinCsvRow(values.Select(v => v ?? ""));
        lock (_lock)
        {
            _writer.WriteLine(line);
        }
    }

    /// <summary>버퍼를 디스크에 즉시 쓴다.</summary>
    public void Flush()
    {
        lock (_lock) { _writer.Flush(); }
    }

    /// <summary>값들을 CSV 행으로 조합한다 (RFC 4180 이스케이프).</summary>
    private static string JoinCsvRow(IEnumerable<string> values)
    {
        var sb = new StringBuilder();
        bool first = true;
        foreach (string v in values)
        {
            if (!first) sb.Append(',');
            first = false;
            sb.Append(EscapeCsvField(v));
        }
        return sb.ToString();
    }

    /// <summary>단일 CSV 필드를 RFC 4180에 맞게 이스케이프한다.</summary>
    public static string EscapeCsvField(string value)
    {
        // 특수 문자가 없으면 그대로 반환 (가장 흔한 케이스, 빠른 경로)
        if (!value.Contains(',') && !value.Contains('"') &&
            !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        // 큰따옴표 이스케이프 후 전체를 큰따옴표로 감싼다
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    public void Dispose() => _writer.Dispose();
}
