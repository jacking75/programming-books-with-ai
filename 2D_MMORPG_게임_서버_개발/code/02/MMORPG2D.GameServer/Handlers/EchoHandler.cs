using MemoryPack;
using MMORPG2D.Shared;
using MMORPG2D.Shared.Packets;

namespace MMORPG2D.GameServer.Handlers;

public static class EchoHandler
{
    public static void Register(PacketHandler handler)
    {
        handler.Register(PacketId.ReqEcho, OnReqEcho);
    }

    private static void OnReqEcho(GameSession s, byte[] body)
    {
        var req = MemoryPackSerializer.Deserialize<ReqEcho>(body)
            ?? throw new InvalidOperationException("body is null");

        Logger.Info($"dispatch ReqEcho from {s.SessionID} (msg='{req.Message}')");

        var res = new ResEcho
        {
            Message = req.Message,
            ServerTimeUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        s.SendPacket(PacketId.ResEcho, res);
    }
}
