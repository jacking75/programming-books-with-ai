namespace GameLogAnalyzer.Pipeline;

/// <summary>
/// data/ 디렉토리를 스캔하여 JSONL 파일 목록을 반환한다.
/// </summary>
public class FileScanner
{
    private readonly string _dataDir;

    public FileScanner(string dataDir)
    {
        _dataDir = dataDir;
    }

    /// <summary>
    /// logs_yyyy-MM-dd.jsonl 패턴에 맞는 파일을 날짜 순으로 반환한다.
    /// </summary>
    public IReadOnlyList<FileInfo> ScanLogFiles()
    {
        if (!Directory.Exists(_dataDir))
            return [];

        return Directory
            .GetFiles(_dataDir, "logs_*.jsonl")
            .Select(f => new FileInfo(f))
            .OrderBy(f => f.Name)
            .ToList();
    }
}
