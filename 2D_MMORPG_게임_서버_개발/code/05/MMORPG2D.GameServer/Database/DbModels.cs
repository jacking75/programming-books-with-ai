namespace MMORPG2D.GameServer.Database;

public sealed record SessionAccountRequest(string Token);

public sealed record SessionAccountResult(int ResultCode, long AccountId = 0);

public sealed record CharacterLoadRequest(long AccountId, long CharacterId);

public sealed record CharacterData(
    long CharacterId,
    long AccountId,
    string Name,
    int X,
    int Y,
    int Level,
    long Exp,
    int Hp,
    int MaxHp,
    int Attack,
    int Defense);

public sealed record LoadCharacterResult(int ResultCode, CharacterData? Character = null);

public readonly record struct PlayerSaveData(
    long CharacterId,
    string Name,
    int X,
    int Y,
    int Level,
    long Exp,
    int Hp,
    int MaxHp,
    int Attack,
    int Defense);
