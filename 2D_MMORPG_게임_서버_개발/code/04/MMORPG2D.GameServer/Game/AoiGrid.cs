namespace MMORPG2D.GameServer.Game;

public readonly record struct CellId(int X, int Y);

/// <summary>
/// 좌표를 셀 단위로 묶어 두는 단순 격자 인덱스. 이동 시 셀이 바뀌면 갱신한다.
/// PacketDispatchWorker 단일 스레드에서만 호출한다는 전제로 lock 없이 사용한다.
/// </summary>
public class AoiGrid
{
    public int CellSize { get; }
    private readonly Dictionary<CellId, HashSet<PlayerCharacter>> _cells = new();

    public AoiGrid(int cellSize) { CellSize = cellSize; }

    public CellId ToCell(int px, int py)
        => new(px / CellSize, py / CellSize);

    public void Add(PlayerCharacter p)
    {
        var c = ToCell(p.X, p.Y);
        if (!_cells.TryGetValue(c, out var set))
            _cells[c] = set = new();
        set.Add(p);
    }

    public void Remove(PlayerCharacter p)
    {
        var c = ToCell(p.X, p.Y);
        if (_cells.TryGetValue(c, out var set))
            set.Remove(p);
    }

    public void UpdateCell(PlayerCharacter p, CellId oldCell, CellId newCell)
    {
        if (oldCell == newCell) return;
        if (_cells.TryGetValue(oldCell, out var oldSet)) oldSet.Remove(p);
        if (!_cells.TryGetValue(newCell, out var newSet))
            _cells[newCell] = newSet = new();
        newSet.Add(p);
    }

    /// <summary>(cx,cy) 셀과 8개 인접 셀의 사용자 스냅샷을 한 번에 복사해 돌려준다.</summary>
    public List<PlayerCharacter> Neighbors(int px, int py)
    {
        var (cx, cy) = ToCell(px, py);
        var result = new List<PlayerCharacter>();
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
            if (_cells.TryGetValue(new CellId(cx + dx, cy + dy), out var set))
                result.AddRange(set);
        return result;
    }
}
