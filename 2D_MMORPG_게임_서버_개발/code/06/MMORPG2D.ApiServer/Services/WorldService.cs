using MMORPG2D.ApiServer.Infrastructure;
using MMORPG2D.ApiServer.Models;
using MMORPG2D.Shared.Models;
using SqlKata.Execution;

namespace MMORPG2D.ApiServer.Services;

public class WorldService
{
    private readonly DbConnectionFactory _factory;
    public WorldService(DbConnectionFactory f) { _factory = f; }

    public async Task<List<WorldDto>> ListAsync()
    {
        using var db = _factory.Create();
        var rows = await db.Query("worlds").GetAsync<WorldRow>();
        return rows.Select(r => new WorldDto
        {
            Id = r.Id, Name = r.Name, IsOpen = r.Is_Open, Capacity = r.Capacity
        }).ToList();
    }
}
