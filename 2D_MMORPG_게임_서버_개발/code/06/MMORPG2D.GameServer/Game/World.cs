using System.Collections.Concurrent;

namespace MMORPG2D.GameServer.Game;

public class World
{
    public static World Default { get; } = new(width: 800, height: 600);

    public int Width { get; }
    public int Height { get; }
    public const int CellSize = 32;
    public const int AoiCellSize = 128;

    public AoiGrid Aoi { get; } = new(AoiCellSize);

    private readonly ConcurrentDictionary<long, PlayerCharacter> _players = new();

    public World(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public bool Add(PlayerCharacter p)
    {
        if (!_players.TryAdd(p.CharacterId, p)) return false;
        Aoi.Add(p);
        return true;
    }

    public bool Remove(long characterId)
    {
        if (!_players.TryRemove(characterId, out var p)) return false;
        Aoi.Remove(p);
        return true;
    }

    public PlayerCharacter? Get(long characterId)
        => _players.TryGetValue(characterId, out var p) ? p : null;

    public IEnumerable<PlayerCharacter> All() => _players.Values;
    public int PlayerCount => _players.Count;
}
