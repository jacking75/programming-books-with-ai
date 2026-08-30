using System.Text.RegularExpressions;
using MMORPG2D.ApiServer.Infrastructure;
using MMORPG2D.ApiServer.Models;
using MMORPG2D.Shared.Models;
using MySqlConnector;
using SqlKata.Execution;

namespace MMORPG2D.ApiServer.Services;

public class CharacterService
{
    public const int MaxCharactersPerAccount = 4;
    private static readonly Regex NameRx = new("^[가-힣A-Za-z0-9]{2,12}$", RegexOptions.Compiled);

    private readonly DbConnectionFactory _factory;
    public CharacterService(DbConnectionFactory f) { _factory = f; }

    public async Task<CreateResult> CreateAsync(long accountId, int worldId, string name)
    {
        if (string.IsNullOrWhiteSpace(name) || !NameRx.IsMatch(name))
            return CreateResult.BadName;

        using var db = _factory.Create();
        var count = await db.Query("characters")
            .Where("account_id", accountId).CountAsync<int>();
        if (count >= MaxCharactersPerAccount) return CreateResult.TooMany;

        try
        {
            var id = await db.Query("characters").InsertGetIdAsync<long>(new
            {
                account_id = accountId, world_id = worldId, name,
                level = 1, exp = 0L, pos_x = 400, pos_y = 300,
            });
            return CreateResult.Ok(id);
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            return CreateResult.NameTaken;
        }
    }

    public async Task<List<CharacterDto>> ListAsync(long accountId)
    {
        using var db = _factory.Create();
        var rows = await db.Query("characters")
            .Where("account_id", accountId).GetAsync<CharacterRow>();
        return rows.Select(r => new CharacterDto
        {
            Id = r.Id, WorldId = r.World_Id, Name = r.Name,
            Level = r.Level, Exp = r.Exp, PosX = r.Pos_X, PosY = r.Pos_Y
        }).ToList();
    }
}

public class CreateResult
{
    public bool Success { get; }
    public long CharacterId { get; }
    public string? Reason { get; }

    private CreateResult(bool ok, long id, string? reason)
    {
        Success = ok; CharacterId = id; Reason = reason;
    }
    public static CreateResult Ok(long id) => new(true, id, null);
    public static CreateResult BadName => new(false, 0, "bad_name");
    public static CreateResult TooMany => new(false, 0, "too_many");
    public static CreateResult NameTaken => new(false, 0, "name_taken");
}
