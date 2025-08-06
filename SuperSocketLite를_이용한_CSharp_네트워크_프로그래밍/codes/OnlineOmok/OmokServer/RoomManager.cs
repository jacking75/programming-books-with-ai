using SuperSocketLite.SocketBase.Logging;

namespace OmokServer;
public class RoomManager
{
    List<Room> _roomsList = new List<Room>();
    //int _checkRoomInterval = 0;
    int _checkRoomIndex = 0;
    int _maxRoomCount = 0;
    private readonly SuperSocketLite.SocketBase.Logging.ILog _logger;

    //int checkTurn = 30000; // 30초
    public RoomManager(ILog logger)
    {
        _logger = logger;
    }
    public void CreateRooms(ServerOption serverOpt)
    {
        var maxRoomCount = serverOpt.RoomMaxCount;
        var startNumber = serverOpt.RoomStartNumber;
        var maxUserCount = serverOpt.RoomMaxUserCount;

        var checkRoomInterval = serverOpt.RoomIntervalMilliseconds;
        //_checkRoomInterval = 20000; 
        _maxRoomCount = 25; // serverOpt.maxRoomCount;
        _checkRoomIndex = 0;

        for (int i = 0; i < maxRoomCount; ++i)
        {
            var roomNumber = (startNumber + i);
            var room = new Room(_logger);
            room.Init(i, roomNumber, maxUserCount);

            _roomsList.Add(room);
        }
    }
    public List<Room> GetRoomsList()
    {
        return _roomsList;
    }

    public void CheckRoom()
    {
        if (_checkRoomIndex >= _roomsList.Count)
        {
            _checkRoomIndex = 0;
        }
        int EndIndex = _checkRoomIndex + _maxRoomCount;
        if(EndIndex > _roomsList.Count)
        {
            EndIndex = _roomsList.Count;
        }
        _logger.Debug("==CheckRoom 정상 진입");
        for (int i = _checkRoomIndex; i < EndIndex; i++)
        {
            DateTime cutTime = DateTime.Now;
            // 룸체크
            _roomsList[i].RoomCheck(cutTime); 

            
            // 턴체크
            _roomsList[i].TurnCheck(cutTime);
        }
        _checkRoomIndex += _maxRoomCount;
    }

    public void StartTurnTimer()
    {

    }
}
