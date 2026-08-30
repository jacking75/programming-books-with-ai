using MMORPG2D.GameServer.Game;

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
    int Defense,
    Inventory Inventory);

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
    int Defense,
    IReadOnlyList<InventorySaveData> Inventory);

public readonly record struct InventorySaveData(int SlotIdx, int DefId, int Qty);

public sealed record MonsterLoadResult(
    int ResultCode,
    IReadOnlyList<MonsterData> Monsters,
    IReadOnlyList<MonsterSpawnData> Spawns);

public sealed record MonsterData(
    int Id,
    string Name,
    int MaxHp,
    int Attack,
    int Defense,
    int ExpReward,
    int MoveSpeed,
    int AggroRange,
    int RespawnSec);

public sealed record MonsterSpawnData(int MonsterId, int X, int Y);

public sealed record ItemDefsLoadResult(
    int ResultCode,
    IReadOnlyList<ItemDefData> Items,
    IReadOnlyList<DropData> Drops);

public sealed record ItemDefData(
    int Id,
    string Name,
    byte Type,
    byte EffectKind,
    int EffectAmt,
    int MaxStack);

public sealed record DropData(
    int MonsterId,
    int ItemDefId,
    int Chance,
    int QtyMin,
    int QtyMax);
