using Microsoft.Extensions.Configuration;
using MMORPG2D.GameServer.Database;
using MMORPG2D.GameServer.Game;
using MMORPG2D.GameServer.Handlers;
using SuperSocketLite.SocketBase;
using SuperSocketLite.SocketBase.Config;
using SuperSocketLite.SocketBase.Protocol;

namespace MMORPG2D.GameServer;

public class GameServer : AppServer<GameSession, GameRequestInfo>
{
    private PacketDispatchWorker? _packetWorker;
    private MySqlWorkerPool? _mySqlWorkerPool;
    private RedisWorker? _redisWorker;

    public GameServer()
        : base(new DefaultReceiveFilterFactory<GamePacketFilter, GameRequestInfo>())
    {
        NewSessionConnected += HandleSessionConnected;
        SessionClosed += HandleSessionClosed;
    }

    public static async Task RunFromAppSettingsAsync()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var server = new GameServer();
        await server.RunAsync(config);
    }

    public async Task RunAsync(IConfiguration config)
    {
        var ip = config["GameServer:Ip"] ?? "Any";
        var port = int.Parse(config["GameServer:Port"] ?? "7777");
        var name = config["GameServer:Name"] ?? "GameServer";
        var dbConn = config["Database:ConnectionString"]!;
        var redisConn = config["Redis:ConnectionString"]!;
        var mySqlWorkerCount = Math.Max(2, int.Parse(config["Database:WorkerCount"] ?? "4"));

        var handler = new PacketHandler();
        EchoHandler.Register(handler);

        _packetWorker = new PacketDispatchWorker(handler);
        _redisWorker = new RedisWorker(redisConn, _packetWorker);
        _mySqlWorkerPool = new MySqlWorkerPool(dbConn, _packetWorker, mySqlWorkerCount);

        MonsterManager.Instance.PacketWorker = _packetWorker;
        MonsterManager.Instance.LoadAndStart(await _mySqlWorkerPool.LoadMonstersAsync());

        EnterGameHandler.Init(_redisWorker, _mySqlWorkerPool);
        EnterGameHandler.Register(handler);
        AttackHandler.Init(_packetWorker);
        AttackHandler.Register(handler);

        var ok = Setup(
            new RootConfig(),
            new ServerConfig
            {
                Name = name,
                Ip = ip,
                Port = port,
                Mode = SuperSocketLite.SocketBase.SocketMode.Tcp,
                MaxConnectionNumber = 1000,
                MaxRequestLength = 1024 * 16,
            });

        if (!ok)
        {
            Logger.Error("server setup failed");
            DisposeWorkers();
            return;
        }

        NewRequestReceived += OnNewRequestReceived;

        if (!Start())
        {
            Logger.Error("server start failed");
            DisposeWorkers();
            return;
        }

        Logger.Info($"server started on {ip}:{port}. MySQL workers={mySqlWorkerCount}. press Ctrl+C to quit.");

        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            stopped.TrySetResult();
        };

        await stopped.Task;
        Stop();
        DisposeWorkers();
    }

    private void HandleSessionConnected(GameSession session)
    {
        Logger.Info($"[+] {session.SessionID} from {session.RemoteEndPoint}");
    }

    private void HandleSessionClosed(GameSession session, CloseReason reason)
    {
        var posted = _packetWorker?.Post(() => CloseSessionOnPacketThread(session, reason)) ?? false;
        if (!posted)
        {
            Logger.Warn($"session close skipped: packet worker is not available ({session.SessionID})");
        }
    }

    private void OnNewRequestReceived(GameSession session, GameRequestInfo request)
    {
        if (_packetWorker?.Post(session, request) != true)
        {
            Logger.Warn($"packet dropped: worker is not available ({request.PacketId})");
        }
    }

    private void CloseSessionOnPacketThread(GameSession session, CloseReason reason)
    {
        if (session.Player is { } player)
        {
            Broadcaster.NotifyLeave(player);
            World.Default.Remove(player.CharacterId);

            var saveData = new PlayerSaveData(
                player.CharacterId,
                player.Name,
                player.X,
                player.Y,
                player.Level,
                player.Exp,
                player.Hp,
                player.MaxHp,
                player.Attack,
                player.Defense);

            if (_mySqlWorkerPool?.SavePlayer(saveData) != true)
            {
                Logger.Warn($"save skipped for {player.Name}: MySQL worker pool is not available");
            }
        }

        Logger.Info($"[-] {session.SessionID} closed ({reason})");
    }

    private void DisposeWorkers()
    {
        _mySqlWorkerPool?.Dispose();
        _redisWorker?.Dispose();
        _packetWorker?.Dispose();

        _mySqlWorkerPool = null;
        _redisWorker = null;
        _packetWorker = null;
    }
}
