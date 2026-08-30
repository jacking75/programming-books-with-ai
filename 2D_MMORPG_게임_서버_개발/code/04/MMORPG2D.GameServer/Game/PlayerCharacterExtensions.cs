using MMORPG2D.Shared.Packets;

namespace MMORPG2D.GameServer.Game;

public static class PlayerCharacterExtensions
{
    public static PlayerSnapshot ToSnapshot(this PlayerCharacter p) => new()
    {
        Id = p.CharacterId,
        Name = p.Name,
        X = p.X,
        Y = p.Y,
        Level = p.Level,
    };
}
