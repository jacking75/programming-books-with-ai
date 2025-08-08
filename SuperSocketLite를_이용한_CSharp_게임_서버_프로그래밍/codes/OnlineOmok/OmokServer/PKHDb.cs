using MemoryPack;
using SqlKata.Execution;
using SuperSocketLite.SocketBase.Logging;
using ServerClientCommon;


#pragma warning disable CS8602

namespace OmokServer;

internal class PKHDb : PKHandler
{
    //private QueryFactory _queryFactory;
    private readonly SuperSocketLite.SocketBase.Logging.ILog _logger;

    public PKHDb(ILog logger) : base(logger)
    {
        this._logger = logger;
    }

    public void RegistPacketHandler(Dictionary<int, Action<MemoryPackBinaryRequestInfo, QueryFactory>> packetHandlerMap)
    {
        packetHandlerMap.Add((int)PACKETID.ReqInUserWin, (info, factory) => RequestInUserWin(factory, info));
        packetHandlerMap.Add((int)PACKETID.ReqInUserLose, (info, factory) => RequestInUserLose(factory, info));
        packetHandlerMap.Add((int)PACKETID.ReqInUserDraw, (info, factory) => RequestInUserDraw(factory, info));
    }

    private void RequestInUserWin(QueryFactory _queryFactory, MemoryPackBinaryRequestInfo info)
    {
        var winPacket = MemoryPackSerializer.Deserialize<PKTReqInWin>(info.Data);
        string winnerId = winPacket.WinUserID;

        _logger.Debug($"Processing win for user ID: {winnerId}");

        var result = _queryFactory.Query("UserGameData")
            .Where("UserId", "=", winnerId)
            .Increment("Win", 1);

        // Check the result and log accordingly
        if (result > 0)
        {
            
            _logger.Info($"Successfully updated win count for user ID: {winnerId}");
        }
        else
        {
            _logger.Error($"Failed to update win count for user ID: {winnerId}");
        }
    }

    private void RequestInUserLose(QueryFactory _queryFactory, MemoryPackBinaryRequestInfo info)
    {
        var losePacket = MemoryPackSerializer.Deserialize<PKTReqInLose>(info.Data);
        string loserId = losePacket.LoseUserID;

        _logger.Debug($"Processing win for user ID: {loserId}");

        var result = _queryFactory.Query("UserGameData")
            .Where("UserId", "=", loserId)
            .Increment("Lose", 1);

        // Check the result and log accordingly
        if (result > 0)
        {
            _logger.Info($"Successfully updated lose count for user ID: {loserId}");
        }
        else
        {
            _logger.Error($"Failed to update lose count for user ID: {loserId}");
        }
    }


    private void RequestInUserDraw(QueryFactory _queryFactory, MemoryPackBinaryRequestInfo info)
    {
        var Packet = MemoryPackSerializer.Deserialize<PKTReqInDraw>(info.Data);
        string userId = Packet.UserID;

        _logger.Debug($"Processing win for user ID: {userId}");

        var result = _queryFactory.Query("UserGameData")
            .Where("UserId", "=", userId)
            .Increment("Draw", 1);

        // Check the result and log accordingly
        if (result > 0)
        {
            _logger.Info($"Successfully updated draw count for user ID: {userId}");
        }
        else
        {
            _logger.Error($"Failed to update draw count for user ID: {userId}");
        }
    }

    private void RequestInMysql(QueryFactory _queryFactory, MemoryPackBinaryRequestInfo requestData)
    {
        _logger.Debug("RequestInMysql");
    }

}
