using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetServerLib;


public class TcpSession : ISession
{
    private readonly TcpClient _tcpClient;
    private readonly NetworkStream _networkStream;
    private readonly IMessageFramer _messageFramer;
    private readonly IPacketProcessor _packetProcessor;
    private readonly Action<ISession, byte[]> _onPacketReceived;
    private Thread _receiveThread;
    private volatile bool _isDisconnecting = false;

    public Guid Id { get; } = Guid.NewGuid();
    public bool IsConnected => _tcpClient?.Connected ?? false;
    public EndPoint RemoteEndPoint => _tcpClient?.Client?.RemoteEndPoint;

    public event Action<IPacket> PacketReceived; // 이 이벤트는 더 이상 직접 사용되지 않음
    public event Action Disconnected;

    public TcpSession(TcpClient tcpClient, IMessageFramer messageFramer, IPacketProcessor packetProcessor, Action<ISession, byte[]> onPacketReceived)
    {
        _tcpClient = tcpClient;
        _networkStream = tcpClient.GetStream();
        _messageFramer = messageFramer;
        _packetProcessor = packetProcessor;
        _onPacketReceived = onPacketReceived;

        _messageFramer.MessageReceived += OnMessageReceived;
    }
    
    // 이 메서드는 이제 패킷을 중앙 큐로 보냄
    private void OnMessageReceived(byte[] data)
    {
        if (_isDisconnecting) return;
        _onPacketReceived(this, data);
    }

    public void Start()
    {
        _receiveThread = new Thread(ReceiveLoop);
        _receiveThread.Start();
    }

    private void ReceiveLoop()
    {
        var receiveBuffer = new byte[8192];
        try
        {
            while (!_isDisconnecting && IsConnected)
            {
                int bytesRead = _networkStream.Read(receiveBuffer, 0, receiveBuffer.Length);
                if (bytesRead == 0)
                {
                    break; // 원격 호스트가 연결을 종료
                }
                _messageFramer.ProcessReceivedData(new ArraySegment<byte>(receiveBuffer, 0, bytesRead));
            }
        }
        catch (IOException ex) when (ex.InnerException is SocketException)
        {
             /* 소켓 강제 종료 */
        }
        catch (Exception ex)
        {
            if (!_isDisconnecting)
            {
                 Console.WriteLine($"[{Id}] Receive loop error: {ex.Message}");
            }
        }
        finally
        {
            Disconnect();
        }
    }

    public void Send(IPacket packet)
    {
        if (!IsConnected || _isDisconnecting)
            return;

        try
        {
            byte[] packetData = _packetProcessor.SerializePacket(packet);
            byte[] framedData = _messageFramer.FrameMessage(packetData);
            _networkStream.Write(framedData, 0, framedData.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{Id}] Send error: {ex.Message}");
            Disconnect();
        }
    }

    public void Disconnect()
    {
        if (_isDisconnecting) return;
        _isDisconnecting = true;
        
        _tcpClient?.Close();
        _networkStream?.Dispose();

        Disconnected?.Invoke();
    }
}