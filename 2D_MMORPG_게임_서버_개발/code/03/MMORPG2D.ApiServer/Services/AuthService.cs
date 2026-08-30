using CloudStructures.Structures;
using MMORPG2D.ApiServer.Infrastructure;
using MMORPG2D.ApiServer.Models;
using MySqlConnector;
using SqlKata;
using SqlKata.Execution;

namespace MMORPG2D.ApiServer.Services;

public class AuthService
{
    public static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(10);

    private readonly DbConnectionFactory _factory;
    private readonly RedisProvider _redis;
    private readonly PasswordHasher _hasher;

    public AuthService(DbConnectionFactory f, RedisProvider r, PasswordHasher h)
    {
        _factory = f; _redis = r; _hasher = h;
    }

    public async Task<RegisterResult> RegisterAsync(string loginId, string pw)
    {
        if (string.IsNullOrWhiteSpace(loginId) || loginId.Length < 3) return RegisterResult.BadInput;
        if (string.IsNullOrWhiteSpace(pw) || pw.Length < 4) return RegisterResult.BadInput;

        using var db = _factory.Create();

        // 빠른 사전 검사 — UNIQUE 위반 BCrypt 헛발질을 줄인다.
        var exists = await db.Query("users").Where("login_id", loginId).ExistsAsync();
        if (exists) return RegisterResult.Conflict;

        var hash = _hasher.Hash(pw);
        try
        {
            await db.Query("users").InsertAsync(new
            {
                login_id = loginId,
                pw_hash = hash,
            });
            return RegisterResult.Ok;
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            return RegisterResult.Conflict;
        }
    }

    public async Task<LoginResult> LoginAsync(string loginId, string pw)
    {
        using var db = _factory.Create();
        var user = await db.Query("users")
            .Where("login_id", loginId)
            .FirstOrDefaultAsync<UserRow>();

        if (user == null || !_hasher.Verify(pw, user.Pw_Hash))
            return LoginResult.Fail();

        var token = Guid.NewGuid().ToString("N");
        var sess = new RedisString<long>(_redis.Conn, $"sess:{token}", SessionTtl);
        await sess.SetAsync(user.Id);

        await db.Query("users").Where("id", user.Id)
            .UpdateAsync(new { last_login_at = DateTime.UtcNow });

        return LoginResult.Ok(token, user.Id);
    }
}

public enum RegisterResult { Ok, Conflict, BadInput }

public class LoginResult
{
    public bool Success { get; }
    public string? Token { get; }
    public long AccountId { get; }

    private LoginResult(bool ok, string? token, long id)
    {
        Success = ok; Token = token; AccountId = id;
    }
    public static LoginResult Ok(string token, long id) => new(true, token, id);
    public static LoginResult Fail() => new(false, null, 0);
}
