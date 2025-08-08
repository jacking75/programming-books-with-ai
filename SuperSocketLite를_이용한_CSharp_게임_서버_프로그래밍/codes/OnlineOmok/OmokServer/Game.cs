using MemoryPack;
using SuperSocketLite.SocketBase.Logging;
using ServerClientCommon;


#pragma warning disable CS8600
#pragma warning disable CS8602
#pragma warning disable CS8625
#pragma warning disable CS8618

namespace OmokServer;

public class Game
{
    private const int BoardSize = 19;
    private int[,] board = new int[BoardSize, BoardSize];
    private List<RoomUser> players;
    public bool IsGameStarted { get; private set; } = false;
    public static Func<string, byte[], bool> NetSendFunc;
    public static Action<MemoryPackBinaryRequestInfo> DistributeInnerPacket;

    private int turnSkipCount = 0;
    private const int MaxSkipCount = 6;

    private readonly SuperSocketLite.SocketBase.Logging.ILog _logger;

    public Game(List<RoomUser> players, Func<string, byte[], bool> netSendFunction, ILog logger)
    {
        this.players = players ?? throw new ArgumentNullException(nameof(players));
        NetSendFunc = netSendFunction ?? throw new ArgumentNullException(nameof(netSendFunction));
        InitializeBoard();
        this._logger = logger;
    }

    private void InitializeBoard()
    {
        Array.Clear(board, 0, BoardSize * BoardSize);
    }

    public void StartGame()
    {
        IsGameStarted = true;
        NotifyGameStart();
        _logger.Debug("Game has started.");
        turnSkipCount = 0;
    }

    public void SetTurnSkipCount1()
    {
        turnSkipCount++;
        _logger.Debug($"턴 스킵 + 1 , 현재 turnSkipCount : {turnSkipCount}");
    }

    public void IsGameTurnSkip6times()
    {
        if (turnSkipCount >= MaxSkipCount)
        {
            EndGameDueToTurnSkips();  // 연속 턴 스킵으로 게임 종료

        }
    }

    private void EndGameDueToTurnSkips()
    {
        foreach (var player in players)
        {
            var endGamePacket = new PKTNtfEndOmok { };
            var sendPacket = MemoryPackSerializer.Serialize(endGamePacket);
            MemoryPackPacketHeadInfo.Write(sendPacket, PACKETID.NTFEndOmok);
            // 모든 플레이어에게 게임 종료 통보
            BroadcastToAll(sendPacket);

            _logger.Debug($"Game ended. Winner: None = 턴넘김 6회로 무승부");
            
            // 무승부 결과 Mysql 업데이트
            UpdateDueGameData(player.UserID);
        }
        IsGameStarted = false;
        InitializeBoard(); // Reset the board
    }

    private void UpdateDueGameData(string userId)
    {
        var drawPacket = new PKTReqInDraw()
        {
            UserID = userId
        };
        
        var drawData = MemoryPackSerializer.Serialize(drawPacket);
        MemoryPackPacketHeadInfo.Write(drawData, PACKETID.ReqInUserWin);
        
        var memoryDrawPacket = new MemoryPackBinaryRequestInfo(null);
        memoryDrawPacket.Data = drawData;
        DistributeInnerPacket(memoryDrawPacket);

        _logger.Info($"PKTReqInDraw 보내기{userId}");
    }

    private void NotifyGameStart()
    {
        if (players == null || !players.Any())
            throw new InvalidOperationException("Players list is empty or not initialized.");

        if (NetSendFunc == null)
            throw new InvalidOperationException("Network send function is not set.");


        var random = new Random();
        int firstPlayerIndex = random.Next(players.Count);
        var firstPlayer = players[firstPlayerIndex];

        players[firstPlayerIndex].StoneColor = (int)StoneType.Black;
        players[(firstPlayerIndex+1)%2].StoneColor = (int)StoneType.White;


        var startPacket = new PKTNtfStartOmok
        {
            FirstUserID = firstPlayer.UserID
        };

        var sendPacket = MemoryPackSerializer.Serialize(startPacket);
        MemoryPackPacketHeadInfo.Write(sendPacket, PACKETID.NtfStartOmok);

        foreach (var player in players)
        {
            NetSendFunc(player.NetSessionID, sendPacket);
            _logger.Debug($"Notified {player.UserID} about game start.");
        }
    }

    public bool PlaceStone(int x, int y, int stoneType)
    {
        if (!IsGameStarted || x < 0 || y < 0 || x >= BoardSize || y >= BoardSize || board[x, y] != (int)StoneType.None)
        {
            _logger.Error("Invalid move or game not started.");
            return false;
        }
        board[x, y] = stoneType;
        _logger.Debug($"Stone placed at ({x}, {y}) with type {stoneType}.");


        if (CheckWin(x, y))
        {
            RoomUser winner = players.FirstOrDefault(p => p.StoneColor == stoneType);
            if (winner != null)
            {
                EndGame(winner);
                NotifyGameEnd(winner);
            }
        }
        return true;
    }

    private void NotifyGameEnd(RoomUser winner)
    {
        var endGamePacket = new PKTNtfEndOmok
        {
            WinUserID = winner.UserID
        };
        RoomUser loser = players.FirstOrDefault(p => p.UserID != winner.UserID);

        // Update in mysql UserGameData
        UpdateUserGameData(winner.UserID, loser.UserID);

        var sendPacket = MemoryPackSerializer.Serialize(endGamePacket);
        MemoryPackPacketHeadInfo.Write(sendPacket, PACKETID.NTFEndOmok);

        // 모든 플레이어에게 게임 종료 통보
        BroadcastToAll(sendPacket);
        _logger.Debug($"Game ended. Winner: {winner.UserID}");
    }

   

    private void UpdateUserGameData(string winnerId, string loserId)
    {
        // Update Winner - PKTReqInWin 만들기
        var winPacket = new PKTReqInWin() 
        { 
            WinUserID = winnerId 
        };
        var winData = MemoryPackSerializer.Serialize(winPacket);
        MemoryPackPacketHeadInfo.Write(winData, PACKETID.ReqInUserWin);
        var memoryWinPacket = new MemoryPackBinaryRequestInfo(null);
        memoryWinPacket.Data = winData;
        DistributeInnerPacket(memoryWinPacket);
        _logger.Info($"PKTReqInWin 보내기{loserId}");

        // Update Loser - PKTReqInLose 만들기
        var losePacket = new PKTReqInLose() { LoseUserID = loserId };
        var loseData = MemoryPackSerializer.Serialize(losePacket);
        MemoryPackPacketHeadInfo.Write(loseData, PACKETID.ReqInUserLose);
        var memoryLosePacket = new MemoryPackBinaryRequestInfo(null);
        memoryLosePacket.Data = loseData;
        DistributeInnerPacket(memoryLosePacket);
        _logger.Info($"PKTReqInLose 보내기{loserId}");
        //}
    }

    private void BroadcastToAll(byte[] packet)
    {
        foreach (var player in players)
        {
            NetSendFunc(player.NetSessionID, packet);
        }
    }

    private bool CheckWin(int x, int y)
    {
        int stoneType = board[x, y];
        return CheckDirection(x, y, 1, 0, stoneType) >= 5 ||   // 가로 방향
               CheckDirection(x, y, 0, 1, stoneType) >= 5 ||   // 세로 방향
               CheckDirection(x, y, 1, 1, stoneType) >= 5 ||   // 대각선 방향 (오른쪽 아래)
               CheckDirection(x, y, 1, -1, stoneType) >= 5;    // 반대 대각선 방향 (오른쪽 위)
    }
    private int CheckDirection(int x, int y, int dx, int dy, int stoneType)
    {
        int count = 1;
        // 정방향으로 세기
        count += CountInDirection(x, y, dx, dy, stoneType);
        // 역방향으로 세기
        count += CountInDirection(x, y, -dx, -dy, stoneType);
        return count;
    }

    private int CountInDirection(int x, int y, int dx, int dy, int stoneType)
    {
        int count = 0;
        int nx = x + dx;
        int ny = y + dy;
        while (nx >= 0 && nx < BoardSize && ny >= 0 && ny < BoardSize && board[nx, ny] == stoneType)
        {
            count++;
            nx += dx;
            ny += dy;
        }
        return count;
    }

    private void EndGame(RoomUser winner)
    {
        Console.WriteLine($"Player {winner.UserID} wins the game!");
        IsGameStarted = false;
        InitializeBoard(); // Reset the board
    }
}
public enum StoneType
{
    None = 0,
    Black = 1,
    White = 2
}