using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using GameAPIServer.Repository.Interfaces;
using GameAPIServer.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameAPIServer.Repository;

public class MasterDb : IMasterDb
{
    
    readonly ILogger<MasterDb> _logger;
    readonly IGameDb _gameDb;
    
    public VersionDAO _version { get; set; }
    public List<AttendanceRewardData> _attendanceRewardList { get; set; }    
    public List<GachaRewardData> _gachaRewardList { get; set; }
    public List<ItemLevelData> _itemLevelList { get; set; }


    public MasterDb(ILogger<MasterDb> logger, IGameDb gameDb)
    {
        _logger = logger;
 
        Open();

        _gameDb = gameDb;

    }

    public void Dispose()
    {
        Close();
    }

    public async Task<bool> Load()
    {
        await Task.CompletedTask;

        return true;
    }
            
    void Open()
    {
    }

    void Close()
    {
    }

}
