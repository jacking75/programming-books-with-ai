using System.Collections.Concurrent;

namespace MMORPG2D.GameServer.Game;

/// <summary>
/// 한 월드 안의 PlayerCharacter 들을 들고 있는다.
/// 4주차에서 브로드캐스트, 6주차에서 몬스터, 7주차에서 아이템이 같이 들어온다.
/// </summary>
public class World
{
    public static World Default { get; } = new(width: 800, height: 600);

    public int Width { get; }
    public int Height { get; }
    public const int CellSize = 32;

    private readonly ConcurrentDictionary<long, PlayerCharacter> _players = new();

    public World(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public bool Add(PlayerCharacter p)   => _players.TryAdd(p.CharacterId, p);
    public bool Remove(long characterId) => _players.TryRemove(characterId, out _);

    public PlayerCharacter? Get(long characterId)
        => _players.TryGetValue(characterId, out var p) ? p : null;

    public IEnumerable<PlayerCharacter> All() => _players.Values;
    public int PlayerCount => _players.Count;
}
