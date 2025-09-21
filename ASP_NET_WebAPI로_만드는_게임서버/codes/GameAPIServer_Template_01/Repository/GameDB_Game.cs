using System;
using System.Data;
using System.Threading.Tasks;
using GameAPIServer.Repository.Interfaces;
using GameAPIServer.Models.DAO;
using System.Collections.Concurrent;

namespace GameAPIServer.Repository;

public partial class GameDb : IGameDb
{
    ConcurrentDictionary<string, string> _users = new ConcurrentDictionary<string, string>();


    public async Task<ErrorCode> CreateAccount(string userID, string pw)
    {
        await Task.CompletedTask;

        _users.AddOrUpdate(userID, pw, (key, oldValue) => pw);
        return ErrorCode.None;
    }

    public async Task<(ErrorCode, Int64)> VerifyUser(string userID, string pw)
    {
        await Task.CompletedTask;

        _users.TryGetValue(userID, out var storedPassword);
        if (storedPassword is null)
        {
            return (ErrorCode.LoginFailUserNotExist, 0);
        }

        return (ErrorCode.None, 111);
    }

    public async Task<GdbUserInfo> GetUserByUid(int uid)
    {
        await Task.CompletedTask;

        return new GdbUserInfo { uid = uid, player_id = "123456789", nickname = "TestUser" };
    }
   
    public async Task<int> InsertUser(Int64 playerId, string nickname)
    {
        return await Task.FromResult(1); // Return 1 to indicate success
    }



    public async Task<int> InsertInitMoneyInfo(int uid)
    {
        return await Task.FromResult(1); // Return 1 to indicate success
    }

    public async Task<GdbUserMoneyInfo> GetUserMoneyById(int uid)
    {
        return await Task.FromResult(new GdbUserMoneyInfo { uid = uid, gold_medal = 1000 });
    }



}