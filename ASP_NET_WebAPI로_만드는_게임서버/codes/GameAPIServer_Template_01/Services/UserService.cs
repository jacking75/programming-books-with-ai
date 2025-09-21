using GameAPIServer.Models.DAO;
using GameAPIServer.Models.DTO;
using GameAPIServer.Repository.Interfaces;
using GameAPIServer.Servicies.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;


namespace GameAPIServer.Servicies;

public class UserService : IUserService
{
    readonly ILogger<UserService> _logger;
    readonly IGameDb _gameDb;


    public UserService(ILogger<UserService> logger, IGameDb gameDb)
    {
        _logger = logger;
        _gameDb = gameDb;
    }

    public async Task<(ErrorCode, GdbUserInfo)> GetUserInfo(int uid)
    {
        return (ErrorCode.None, await _gameDb.GetUserByUid(uid));
    }

    public async Task<(ErrorCode, GdbUserMoneyInfo)> GetUserMoneyInfo(int uid)
    {
        return (ErrorCode.None, await _gameDb.GetUserMoneyById(uid)); 
    }
       

    
}
