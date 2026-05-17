using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using PvPGameServer.Ecs;
using PvPGameServer.Ecs.Systems;


namespace PvPGameServer;

class PacketProcessor
{
    bool _isThreadRunning = false;
    System.Threading.Thread _processThread = null;

    public Func<string, byte[], bool> NetSendFunc;
    
    //receive쪽에서 처리하지 않아도 Post에서 블럭킹 되지 않는다. 
    //BufferBlock<T>(DataflowBlockOptions) 에서 DataflowBlockOptions의 BoundedCapacity로 버퍼 가능 수 지정. BoundedCapacity 보다 크게 쌓이면 블럭킹 된다
    BufferBlock<MemoryPackBinaryRequestInfo> _packetBuffer = new ();

    readonly EntityWorld _world = new();
    PlayerSystem _playerSystem;
    RoomSystem _roomSystem;

    Dictionary<int, Action<MemoryPackBinaryRequestInfo>> _packetHandlerDict = new ();
    PKHCommon _commonPacketHandler = new ();
    PKHGame _gamePacketHandler = new ();
    PKHRoom _roomPacketHandler = new ();

            
    public void CreateAndStart(ServerOption serverOpt)
    {
        var maxUserCount = serverOpt.RoomMaxCount * serverOpt.RoomMaxUserCount;

        _playerSystem = new PlayerSystem(_world);
        _playerSystem.Initialize(maxUserCount);

        _roomSystem = new RoomSystem(_world);
        _roomSystem.InitializeRooms(serverOpt.RoomMaxCount, serverOpt.RoomStartNumber, serverOpt.RoomMaxUserCount);
        
        RegistPacketHandler();

        _isThreadRunning = true;
        _processThread = new System.Threading.Thread(this.Process);
        _processThread.Start();
    }
    
    public void Destory()
    {
        MainServer.s_MainLogger.Info("PacketProcessor::Destory - begin");

        _isThreadRunning = false;
        _packetBuffer.Complete();

        _processThread.Join();

        _world.Clear();

        MainServer.s_MainLogger.Info("PacketProcessor::Destory - end");
    }
          
    public void InsertPacket(MemoryPackBinaryRequestInfo data)
    {
        _packetBuffer.Post(data);
    }

    
    void RegistPacketHandler()
    {
        PKHandler.NetSendFunc = NetSendFunc;
        PKHandler.DistributeInnerPacket = InsertPacket;
        _commonPacketHandler.Init(_playerSystem, _roomSystem);
        _commonPacketHandler.RegistPacketHandler(_packetHandlerDict);                

        _gamePacketHandler.Init(_playerSystem, _roomSystem);
        _gamePacketHandler.RegistPacketHandler(_packetHandlerDict);
        
        _roomPacketHandler.Init(_playerSystem, _roomSystem);
        _roomPacketHandler.SetGameCoordinator(_gamePacketHandler);
        _roomPacketHandler.RegistPacketHandler(_packetHandlerDict);
    }

    void Process()
    {
        while (_isThreadRunning)
        {
            try
            {
                var packet = _packetBuffer.Receive();

                var header = new MemoryPackPacketHeader();
                header.Read(packet.Data);

                if (_packetHandlerDict.ContainsKey(header.Id))
                {
                    _packetHandlerDict[header.Id](packet);
                }
                /*else
                {
                    System.Diagnostics.Debug.WriteLine("세션 번호 {0}, PacketID {1}, 받은 데이터 크기: {2}", packet.SessionID, packet.PacketID, packet.BodyData.Length);
                }*/
            }
            catch (Exception ex)
            {
                if (_isThreadRunning)
                {
                    MainServer.s_MainLogger.Error(ex.ToString());
                }
            }
        }
    }


}
