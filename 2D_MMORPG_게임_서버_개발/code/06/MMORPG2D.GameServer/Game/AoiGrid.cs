namespace MMORPG2D.GameServer.Game;

public readonly record struct CellId(int X, int Y);

/// <summary>
/// 좌표를 셀 단위로 묶어 두는 단순 격자 인덱스. 이동 시 셀이 바뀌면 갱신한다.
/// 학습용으로 단일 lock 으로 보호. 8주차에서 lock-free 자료구조로 옮긴다.
/// </summary>
public class AoiGrid
{
    public int CellSize { get; }
    private readonly Dictionary<CellId, HashSet<PlayerCharacter>> _cells = new();
    private readonly object _lock = new();

    public AoiGrid(int cellSize) { CellSize = cellSize; }

    public CellId ToCell(int px, int py)
        => new(px / CellSize, py / CellSize);

    public void Add(PlayerCharacter p)
    {
        var c = ToCell(p.X, p.Y);
        lock (_lock)
        {
            if (!_cells.TryGetValue(c, out var set))
                _cells[c] = set = new();
            set.Add(p);
        }
    }

    public void Remove(PlayerCharacter p)
    {
        var c = ToCell(p.X, p.Y);
        lock (_lock)
        {
            if (_cells.TryGetValue(c, out var set))
                set.Remove(p);
        }
    }

    public void UpdateCell(PlayerCharacter p, CellId oldCell, CellId newCell)
    {
        if (oldCell == newCell) return;
        lock (_lock)
        {
            if (_cells.TryGetValue(oldCell, out var oldSet)) oldSet.Remove(p);
            if (!_cells.TryGetValue(newCell, out var newSet))
                _cells[newCell] = newSet = new();
            newSet.Add(p);
        }
    }

    /// <summary>(cx,cy) 셀과 8개 인접 셀의 사용자 스냅샷을 한 번에 복사해 돌려준다.</summary>
    public List<PlayerCharacter> Neighbors(int px, int py)
    {
        var (cx, cy) = ToCell(px, py);
        var result = new List<PlayerCharacter>();
        lock (_lock)
        {
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
                if (_cells.TryGetValue(new CellId(cx + dx, cy + dy), out var set))
                    result.AddRange(set);
        }
        return result;
    }
}
