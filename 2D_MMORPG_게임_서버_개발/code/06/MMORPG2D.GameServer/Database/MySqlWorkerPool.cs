using System.Collections.Concurrent;
using MySqlConnector;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace MMORPG2D.GameServer.Database;

public sealed class MySqlWorkerPool : IDisposable
{
    private readonly BlockingCollection<Action<QueryFactory>> _jobs = new();
    private readonly List<MySqlWorker> _workers = new();
    private readonly PacketDispatchWorker _packetDispatcher;

    public MySqlWorkerPool(string connectionString, PacketDispatchWorker packetDispatcher, int workerCount)
    {
        if (workerCount < 2)
            throw new ArgumentOutOfRangeException(nameof(workerCount), "MySQL worker count must be 2 or more.");

        _packetDispatcher = packetDispatcher;

        for (var i = 0; i < workerCount; i++)
        {
            _workers.Add(new MySqlWorker(i, connectionString, _jobs));
        }
    }

    public bool Post(
        GameSession session,
        CharacterLoadRequest request,
        Action<GameSession, LoadCharacterResult> completed)
    {
        return Post(db =>
        {
            var result = Process(db, request);
            _packetDispatcher.Post(() => completed(session, result));
        });
    }

    public bool SavePlayer(PlayerSaveData player)
    {
        return Post(db =>
        {
            db.Query("characters").Where("id", player.CharacterId)
                .UpdateAsync(new
                {
                    pos_x = player.X,
                    pos_y = player.Y,
                    level = player.Level,
                    exp = player.Exp,
                    hp = player.Hp,
                    max_hp = player.MaxHp,
                    attack = player.Attack,
                    defense = player.Defense,
                })
                .GetAwaiter()
                .GetResult();

            Logger.Info($"saved {player.Name}: ({player.X},{player.Y}) lv{player.Level} hp={player.Hp}/{player.MaxHp}");
        });
    }

    public Task<MonsterLoadResult> LoadMonstersAsync()
    {
        var completion = new TaskCompletionSource<MonsterLoadResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!Post(db => completion.SetResult(LoadMonsters(db))))
        {
            completion.SetResult(new MonsterLoadResult(99, Array.Empty<MonsterData>(), Array.Empty<MonsterSpawnData>()));
        }

        return completion.Task;
    }

    private bool Post(Action<QueryFactory> job)
    {
        try
        {
            _jobs.Add(job);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static LoadCharacterResult Process(QueryFactory db, CharacterLoadRequest request)
    {
        try
        {
            var ch = db.Query("characters")
                .Where("id", request.CharacterId)
                .Where("account_id", request.AccountId)
                .FirstOrDefaultAsync<dynamic>()
                .GetAwaiter()
                .GetResult();

            if (ch == null)
                return new LoadCharacterResult(2);

            return new LoadCharacterResult(
                0,
                new CharacterData(
                    CharacterId: Convert.ToInt64(ch.id),
                    AccountId: request.AccountId,
                    Name: Convert.ToString(ch.name) ?? "",
                    X: Convert.ToInt32(ch.pos_x),
                    Y: Convert.ToInt32(ch.pos_y),
                    Level: Convert.ToInt32(ch.level),
                    Exp: Convert.ToInt64(ch.exp),
                    Hp: Convert.ToInt32(ch.hp),
                    MaxHp: Convert.ToInt32(ch.max_hp),
                    Attack: Convert.ToInt32(ch.attack),
                    Defense: Convert.ToInt32(ch.defense)));
        }
        catch (Exception ex)
        {
            Logger.Error("load character failed", ex);
            return new LoadCharacterResult(99);
        }
    }

    private static MonsterLoadResult LoadMonsters(QueryFactory db)
    {
        try
        {
            var monsters = db.Query("monsters")
                .GetAsync<dynamic>()
                .GetAwaiter()
                .GetResult()
                .Select(row => new MonsterData(
                    Id: Convert.ToInt32(row.id),
                    Name: Convert.ToString(row.name) ?? "",
                    MaxHp: Convert.ToInt32(row.max_hp),
                    Attack: Convert.ToInt32(row.attack),
                    Defense: Convert.ToInt32(row.defense),
                    ExpReward: Convert.ToInt32(row.exp_reward),
                    MoveSpeed: Convert.ToInt32(row.move_speed),
                    AggroRange: Convert.ToInt32(row.aggro_range),
                    RespawnSec: Convert.ToInt32(row.respawn_sec)))
                .ToArray();

            var spawns = db.Query("monster_spawns")
                .GetAsync<dynamic>()
                .GetAwaiter()
                .GetResult()
                .Select(row => new MonsterSpawnData(
                    MonsterId: Convert.ToInt32(row.monster_id),
                    X: Convert.ToInt32(row.x),
                    Y: Convert.ToInt32(row.y)))
                .ToArray();

            return new MonsterLoadResult(0, monsters, spawns);
        }
        catch (Exception ex)
        {
            Logger.Error("load monsters failed", ex);
            return new MonsterLoadResult(99, Array.Empty<MonsterData>(), Array.Empty<MonsterSpawnData>());
        }
    }

    public void Dispose()
    {
        _jobs.CompleteAdding();

        foreach (var worker in _workers)
        {
            worker.Dispose();
        }

        _jobs.Dispose();
    }

    private sealed class MySqlWorker : IDisposable
    {
        private readonly BlockingCollection<Action<QueryFactory>> _jobs;
        private readonly MySqlConnection _connection;
        private readonly QueryFactory _db;
        private readonly Thread _thread;

        public MySqlWorker(int index, string connectionString, BlockingCollection<Action<QueryFactory>> jobs)
        {
            _jobs = jobs;
            _connection = new MySqlConnection(connectionString);
            _connection.OpenAsync().GetAwaiter().GetResult();
            _db = new QueryFactory(_connection, new MySqlCompiler());
            _thread = new Thread(Run)
            {
                IsBackground = true,
                Name = $"mysql-worker-{index}"
            };
            _thread.Start();
        }

        private void Run()
        {
            foreach (var job in _jobs.GetConsumingEnumerable())
            {
                try
                {
                    job(_db);
                }
                catch (Exception ex)
                {
                    Logger.Error("MySQL job failed", ex);
                }
            }
        }

        public void Dispose()
        {
            _thread.Join(TimeSpan.FromSeconds(3));
            (_db as IDisposable)?.Dispose();
            _connection.Dispose();
        }
    }
}
