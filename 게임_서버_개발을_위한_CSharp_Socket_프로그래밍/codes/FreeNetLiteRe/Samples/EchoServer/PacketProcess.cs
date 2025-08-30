using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using FreeNetLite;


namespace EchoServer;

class PacketProcess(NetworkService netService)
{
    Dictionary<ulong, GameUser> _userList = new();
    CancellationTokenSource _cancellationSource = new();
    Thread _logicThread;
    IPacketDispatcher _packetDispatcher = netService.PacketDispatcher;


    public void Start()
    {
        _logicThread = new Thread(Run)
        {
            IsBackground = true,
            Name = "PacketProcessThread"
        };

        _logicThread.Start();
    }

    public void Stop()
    {
        _cancellationSource.Cancel();
        _logicThread.Join();
    }

    private void Run()
    {
        while (!_cancellationSource.IsCancellationRequested)
        {
            var packetQueue = _packetDispatcher.DispatchAll();
            if (packetQueue.Count > 0)
            {
                while (packetQueue.TryDequeue(out var packet))
                {
                    OnMessage(packet);
                }
            }
            else
            {
                Thread.Sleep(1);
            }
        }
    }

    private void OnMessage(Packet packet)
    {
        var protocol = (ProtocolId)packet.Id;
        switch (protocol)
        {
            case ProtocolId.Echo:
                {
                    // BodyData를 ReadOnlySpan<byte>으로 안전하게 처리합니다.
                    if (packet.BodyData.HasValue)
                    {
                        var bodySpan = packet.BodyData.Value.Span;

                        var requestPkt = new EchoPacket();
                        requestPkt.Decode(bodySpan);
                        Console.WriteLine($"Received: {requestPkt.Data}");

                        var responsePkt = new EchoPacket { Data = requestPkt.Data };
                        var packetData = responsePkt.ToPacket(ProtocolId.Echo, null);
                        packet.Owner.Send(new ArraySegment<byte>(packetData));
                    }
                    break;
                }
            default:
                if (!OnSystemPacket(packet))
                {
                    Console.WriteLine($"Unknown protocol id: {protocol}");
                }
                break;
        }
    }

    private bool OnSystemPacket(Packet packet)
    {
        switch (packet.Id)
        {
            case NetworkDefine.SysNtfConnected:
                Console.WriteLine($"SYS_NTF_CONNECTED: {packet.Owner.UniqueId}");
                _userList.Add(packet.Owner.UniqueId, new GameUser(packet.Owner));
                return true;

            case NetworkDefine.SysNtfClosed:
                Console.WriteLine($"SYS_NTF_CLOSED: {packet.Owner.UniqueId}");
                _userList.Remove(packet.Owner.UniqueId);
                return true;
        }
        return false;
    }
}