using MemoryPack;
using MMORPG2D.GameServer.Database;
using MMORPG2D.GameServer.Game;
using MMORPG2D.Shared;
using MMORPG2D.Shared.Packets;

namespace MMORPG2D.GameServer.Handlers;

public static class EnterGameHandler
{
    private static RedisWorker? _redisWorker;
    private static MySqlWorkerPool? _mySqlWorkerPool;

    public static void Init(RedisWorker redisWorker, MySqlWorkerPool mySqlWorkerPool)
    {
        _redisWorker = redisWorker;
        _mySqlWorkerPool = mySqlWorkerPool;
    }

    public static void Register(PacketHandler handler)
    {
        handler.Register(PacketId.ReqEnterGame, OnReqEnterGame);
        handler.Register(PacketId.ReqMove, MoveHandler.OnReqMove);
    }

    private static void OnReqEnterGame(GameSession s, byte[] body)
    {
        try
        {
            var req = MemoryPackSerializer.Deserialize<ReqEnterGame>(body)!;
            var redisWorker = _redisWorker
                ?? throw new InvalidOperationException("Redis worker is not initialized");

            if (!redisWorker.Post(
                    s,
                    new SessionAccountRequest(req.Token),
                    (session, result) => OnSessionAccountLoaded(session, req, result)))
            {
                s.SendPacket(PacketId.ResEnterGame, new ResEnterGame { ResultCode = 99 });
            }
        }
        catch (Exception ex)
        {
            Logger.Error("EnterGame dispatch failed", ex);
            s.SendPacket(PacketId.ResEnterGame, new ResEnterGame { ResultCode = 99 });
        }
    }

    private static void OnSessionAccountLoaded(
        GameSession s,
        ReqEnterGame req,
        SessionAccountResult result)
    {
        if (result.ResultCode != 0)
        {
            s.SendPacket(PacketId.ResEnterGame, new ResEnterGame { ResultCode = result.ResultCode });
            return;
        }

        var mySqlWorkerPool = _mySqlWorkerPool
            ?? throw new InvalidOperationException("MySQL worker pool is not initialized");

        if (!mySqlWorkerPool.Post(
                s,
                new CharacterLoadRequest(result.AccountId, req.CharacterId),
                (session, loadResult) => OnCharacterLoaded(session, loadResult)))
        {
            s.SendPacket(PacketId.ResEnterGame, new ResEnterGame { ResultCode = 99 });
        }
    }

    private static void OnCharacterLoaded(GameSession s, LoadCharacterResult result)
    {
        if (result.ResultCode != 0 || result.Character is not { } ch)
        {
            s.SendPacket(PacketId.ResEnterGame, new ResEnterGame { ResultCode = result.ResultCode });
            return;
        }

        var player = new PlayerCharacter(
            s,
            characterId: ch.CharacterId,
            accountId: ch.AccountId,
            name: ch.Name,
            x: ch.X,
            y: ch.Y,
            level: ch.Level,
            exp: ch.Exp,
            hp: ch.Hp,
            maxHp: ch.MaxHp,
            attack: ch.Attack,
            defense: ch.Defense);

        if (!World.Default.Add(player))
        {
            s.SendPacket(PacketId.ResEnterGame, new ResEnterGame { ResultCode = 3 });
            return;
        }

        s.AccountId = ch.AccountId;
        s.CharacterId = player.CharacterId;
        s.Player = player;

        s.SendPacket(PacketId.ResEnterGame, new ResEnterGame
        {
            ResultCode = 0,
            CharacterId = player.CharacterId,
            Name = player.Name,
            X = player.X,
            Y = player.Y,
        });

        Broadcaster.NotifyEnter(player);

        Logger.Info($"enter ok: {player.Name} hp={player.Hp}/{player.MaxHp} world={World.Default.PlayerCount}");
    }
}
