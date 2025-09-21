using GameAPIServer.Repository.Interfaces;
using GameAPIServer.Servicies.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Threading.Tasks;
using System.Transactions;

namespace GameAPIServer.Servicies;

public class GameService :IGameService
{
    readonly ILogger<GameService> _logger;
    readonly IGameDb _gameDb;
    readonly IMasterDb _masterDb;


    public GameService(ILogger<GameService> logger, IGameDb gameDb, IMasterDb masterDb)
    {
        _logger = logger;
        _gameDb = gameDb;
        _masterDb = masterDb;
    }


    public async Task<(ErrorCode, int)> InitNewUserGameData(Int64 playerId, string nickname)
    {
        var (errorCode, uid) = await CreateUserAsync(playerId, nickname);
        if (errorCode != ErrorCode.None)
        {
            return (errorCode, 0);
        }


        var rowCount = await _gameDb.InsertInitMoneyInfo(uid);
        if (rowCount != 1)
        {
            return (ErrorCode.InitNewUserGameDataFailMoney, 0);
        }

        return (ErrorCode.None, uid);
    }

    async Task<(ErrorCode,int)> CreateUserAsync(Int64 playerId, string nickname)
    {
        await Task.CompletedTask;
        return (ErrorCode.None, await _gameDb.InsertUser(playerId, nickname));
    }

}
