using System.Threading.Tasks.Dataflow;
using MySqlConnector;
using SqlKata.Compilers;
using SqlKata.Execution;
using SuperSocketLite.SocketBase.Logging;
using ServerClientCommon;


#pragma warning disable CS8618
#pragma warning disable CS0649


namespace OmokServer;

public class MYSQLWorker
{
    bool _isThreadRunning = false;
    List<System.Threading.Thread> _processThreadList = new List<System.Threading.Thread>();
    public Func<string, byte[], bool> NetSendFunc;
    BufferBlock<MemoryPackBinaryRequestInfo> _msgBuffer = new BufferBlock<MemoryPackBinaryRequestInfo>();

    Dictionary<int, Action<MemoryPackBinaryRequestInfo, QueryFactory>> _packetHandlerMap = new Dictionary<int, Action<MemoryPackBinaryRequestInfo, QueryFactory>>();
    private readonly ILog _logger;

    string db_connection;

    PKHDb _mysqlPacketHandler;

    UserManager _userMgr;
    RoomManager _roomMgr;


    public MYSQLWorker(ILog logger, ServerOption serverOpt)
    {
        this._logger = logger;
        db_connection = serverOpt.MySqlGameDb; // appsettings.json에서 가져온 연결 문자열 사용
        _userMgr = new UserManager(_logger);
        _mysqlPacketHandler = new PKHDb(_logger);
    }

    public void CreateAndStart()
    {
        //TODO: 간단하게 서버를 실행시키기 위해 임시적으로 데이터베이스 기능은 사용하지 않습니다.
        /*RegistPacketHandler();

        _isThreadRunning = true;

        for (int i = 0; i < 4; i++) // 스레드 4개 만든다고 가정
        {
            var thread = new System.Threading.Thread(this.Process);
            _processThreadList.Add(thread);
            thread.Start();
        }*/
    }

    public void Destory()
    {
        //TODO: 간단하게 서버를 실행시키기 위해 임시적으로 데이터베이스 기능은 사용하지 않습니다.

        /*_logger.Info("MYSQLWorker::Destory - begin");

        _isThreadRunning = false;
        _msgBuffer.Complete();

        foreach (var thread in _processThreadList)
        {
            thread.Join();
        }

        _logger.Info("MYSQLWorker::Destory - end");*/
    }

    public void InsertPacket(MemoryPackBinaryRequestInfo packet)
    {
        //TODO: 간단하게 서버를 실행시키기 위해 임시적으로 데이터베이스 기능은 사용하지 않습니다.
        //_msgBuffer.Post(packet);
    }

    void RegistPacketHandler()
    {
        //TODO: 간단하게 서버를 실행시키기 위해 임시적으로 데이터베이스 기능은 사용하지 않습니다.

        // mysql 용 패킷 핸들러 등록 
        /*PKHandler.NetSendFunc = NetSendFunc;
        PKHandler.DistributeInnerPacket = InsertPacket;

        _mysqlPacketHandler.Init(_userMgr, _roomMgr);
        _mysqlPacketHandler.RegistPacketHandler(_packetHandlerMap);

        Game.DistributeInnerPacket = InsertPacket;
        _logger.Info("DistributeInnerPacket is set in MYSQLWorker");*/
    }
    void Process()
    {
        //TODO: 간단하게 서버를 실행시키기 위해 임시적으로 데이터베이스 기능은 사용하지 않습니다.

        /*using (var connection = new MySqlConnection(db_connection))
        {
            connection.Open();
            var compiler = new MySqlCompiler();
            QueryFactory queryFactory = new QueryFactory(connection, compiler);

            try
            {
                while (_isThreadRunning)
                {
                    var packet = _msgBuffer.Receive();
                    if (packet == null)
                    {
                        continue;
                    }
                    var header = new MemoryPackPacketHeadInfo();
                    header.Read(packet.Data);

                    if (_packetHandlerMap.ContainsKey(header.Id))
                    {
                        _packetHandlerMap[header.Id](packet, queryFactory);
                    }
                }
            }
            catch (Exception ex)
            {
                if (_isThreadRunning)
                {
                    _logger.Error($"Error in MYSQL Worker Process: {ex}");
                }
            }
        }*/
    }
}
