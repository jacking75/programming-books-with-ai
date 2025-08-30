using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetServerLib;

namespace ChatServer;


public class ChattingServer
{
    private readonly PacketProcessor _packetProcessor;
    private readonly TcpNetworkServer _networkServer;
    private readonly PacketHandler _packetHandler;
    private readonly DBHandler _dbHandler;
    private readonly UserManager _userManager = new();

    private Thread _packetWorkThread;
    private Thread _dbWorkThread;
    private volatile bool _isRunning = false;

    public ChattingServer()
    {
        _packetProcessor = new PacketProcessor();
        _dbHandler = new DBHandler(_packetProcessor, EnqueuePacketFromAnotherThread);
        _packetHandler = new PacketHandler(_packetProcessor, _dbHandler, _userManager);

        _networkServer = new TcpNetworkServer(
            () => new LengthPrefixedMessageFramer(),
            _packetProcessor,
            _packetHandler.EnqueuePacket
        );

        _networkServer.SessionConnected += _userManager.OnSessionConnected;
        _networkServer.SessionDisconnected += _userManager.OnSessionDisconnected;

        RegisterPackets();
        RegisterPacketHandlers();
    }

    private void EnqueuePacketFromAnotherThread(ISession session, IPacket packet)
    {
        byte[] data = _packetProcessor.SerializePacket(packet);
        _packetHandler.EnqueuePacket(session, data);
    }

    private void RegisterPackets()
    {
        _packetProcessor.RegisterPacket<ChatReq>();
        _packetProcessor.RegisterPacket<ChatNtf>();
        _packetProcessor.RegisterPacket<UserInfoReq>();
        _packetProcessor.RegisterPacket<InternalUserInfoRes>();
    }

    private void RegisterPacketHandlers()
    {
        _packetProcessor.RegisterPacketHandler<ChatReq>(_packetHandler.HandleChatReq);
        _packetProcessor.RegisterPacketHandler<UserInfoReq>(_packetHandler.HandleUserInfoReq);
        _packetProcessor.RegisterPacketHandler<InternalUserInfoRes>(_packetHandler.HandleInternalUserInfoRes);
    }

    public void Start()
    {
        _isRunning = true;
        _dbWorkThread = new Thread(_dbHandler.Process);
        _dbWorkThread.Start();

        _packetWorkThread = new Thread(_packetHandler.Process);
        _packetWorkThread.Start();

        _networkServer.Start(7777);
    }

    public void Stop()
    {
        if (!_isRunning) return;
        _isRunning = false;

        _packetHandler.Stop();
        _dbHandler.Stop();

        _networkServer.Stop();

        _packetWorkThread?.Join();
        _dbWorkThread?.Join();
    }
}
