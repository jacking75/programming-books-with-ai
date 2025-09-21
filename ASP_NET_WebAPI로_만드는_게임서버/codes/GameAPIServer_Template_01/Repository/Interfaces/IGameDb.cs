using GameAPIServer.Models;
using GameAPIServer.Models.DAO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace GameAPIServer.Repository.Interfaces;

public interface IGameDb
{
    public Task<ErrorCode> CreateAccount(string userID, string pw);
    public Task<(ErrorCode, Int64)> VerifyUser(string userID, string pw);


    public Task<GdbUserInfo> GetUserByUid(int uid);
    public Task<int> InsertUser(Int64 playerId, string nickname);
    
       
    
    public Task<int> InsertInitMoneyInfo(int uid);
    public Task<GdbUserMoneyInfo> GetUserMoneyById(int uid);


}