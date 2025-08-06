using SuperSocketLite.SocketBase.Logging;
using ServerClientCommon;


#pragma warning disable CS0649
#pragma warning disable CS8618
#pragma warning disable CS8600
#pragma warning disable CS8603


namespace OmokServer;

public class UserManager
{
    int _maxUserCount; // 200
    UInt64 _userSequenceNumber = 0;
    int _checkUserIndex = 0;
    int _maxUserGroupCount = 40; // 200/4
    private readonly SuperSocketLite.SocketBase.Logging.ILog _logger;

    Dictionary<string, User> _userMap = new Dictionary<string, User>();
    List<User> _userList = new List<User>();

    public UserManager(ILog logger)
    {
        _logger = logger;
    }

    public void Init(int maxUserCount)
    {
        _maxUserCount = maxUserCount;

        for (int i = 0; i < _maxUserCount; ++i)
        {
            _userList.Add(new User());
        }
    }

    public ERROR_CODE AddUser(string userID, string sessionID)
    {
        if(IsFullUserCount())
        {
            return ERROR_CODE.LOGIN_FULL_USER_COUNT;
        }

        if (_userMap.ContainsKey(sessionID)) // 중복 체크
        {
            return ERROR_CODE.ADD_USER_DUPLICATION;
        }

        // _userList에서 빈 User 객체를 찾는다.
        var emptyUser = _userList.FirstOrDefault(u => u.GetSequenceNumber() == 0);
        if (emptyUser == null)
        {
            return ERROR_CODE.LOGIN_FULL_USER_COUNT; // 위에서 체크하고 있긴한디
        }

        ++_userSequenceNumber;
        
        emptyUser.Set(_userSequenceNumber, sessionID, userID);
        _userMap.Add(sessionID, emptyUser);

        return ERROR_CODE.NONE;
    }

    public ERROR_CODE RemoveUser(string sessionID)
    {
        if(_userMap.Remove(sessionID) == false)
        {
            return ERROR_CODE.REMOVE_USER_SEARCH_FAILURE_USER_ID;
        }
        // _userList에서 해당 User 객체를 찾아 초기화
        var userInList = _userList.FirstOrDefault(u => u.GetSessionID() == sessionID);
        if (userInList != null)
        {
            userInList.Reset(); // Reset 메소드는 User 클래스에 정의해야 합니다.
        }

        // _userMap에서 사용자 제거
        _userMap.Remove(sessionID);

        return ERROR_CODE.NONE;
    }

    public void CheckUser()
    {
        if (_checkUserIndex >= _userList.Count)
        {
            _checkUserIndex = 0;
        }
        int EndIndex = _checkUserIndex + _maxUserGroupCount;
        if (EndIndex > _userList.Count)
        {
            EndIndex = _userList.Count;
        }
        _logger.Debug("==CheckUser 정상 진입");
        for (int i = _checkUserIndex; i < EndIndex; i++)
        {
            DateTime cutTime = DateTime.Now;
            // 유저 상태 체크
            _userList[i].UserCheck(cutTime);
        }
        _checkUserIndex += _maxUserGroupCount;
    }


    public User GetUser(string sessionID)
    {
        User user = null;
        _userMap.TryGetValue(sessionID, out user);
        return user;
    }

    bool IsFullUserCount()
    {
        return _maxUserCount <= _userMap.Count();
     }

    public Dictionary<string, User> GetUserMap()
    {
        return _userMap;
    }
            
}

public class User
{
    private UserManager _userManager;
    UInt64 SequenceNumber = 0; // 고유번호
    string SessionID; // supersocket의 세션 아이디
   
    public int RoomNumber { get; private set; } = -1;
    string UserID;
    public DateTime LoginTime;
    //bool IsReady = false;

    public void Set(UInt64 sequence, string sessionID, string userID)
    {
        SequenceNumber = sequence;
        SessionID = sessionID;
        UserID = userID;
        //IsReady = false;
    }                   
    
    public bool IsConfirm(string netSessionID)
    {
        return SessionID == netSessionID;
    }

    public string ID()
    {
        return UserID;
    }

    public void EnteredRoom(int roomNumber)
    {
        RoomNumber = roomNumber;
    }

    public void LeaveRoom()
    {
        RoomNumber = -1;
    }

    public UInt64 GetSequenceNumber() { return SequenceNumber; }

    public string GetSessionID() { return SessionID; }

    public bool IsStateLogin() { return SequenceNumber != 0; }

    public bool IsStateRoom() { return RoomNumber != -1; }
    public void Reset()
    {
        SequenceNumber = 0;
        SessionID = string.Empty;
        UserID = string.Empty;
        RoomNumber = -1;
        //IsReady = false;
    }

    internal void UserCheck(DateTime cutTime)
    {
        if(!IsStateLogin()) { return; } // 로그인 상태가 아니면 체크 X
        if ((cutTime - LoginTime).TotalSeconds > 2.5) // 2.5초
        {
            //_logger.LogDebug("==시간 초과로 접속 종료");
            MainServer.MainLogger.Debug("==시간 초과로 접속 종료");
            // 로그 아웃 처리
            if (_userManager != null) { _userManager.RemoveUser(SessionID); }
            // OnClose 호출

        }
    }
}

