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
    public Guid Id { get; } = Guid.NewGuid();
    public bool IsConnected => _socket?.Connected ?? false;
    public EndPoint RemoteEndPoint => _socket?.RemoteEndPoint;

    private Socket _socket;
    private readonly Action<TcpSession> _onDisconnected;
    private readonly Action<ISession, byte[]> _onPacketReceived;
    private readonly IMessageFramer _messageFramer;
    private readonly IPacketProcessor _packetProcessor;

    internal TcpSession(Socket socket, Action<TcpSession> onDisconnected, Action<ISession, byte[]> onPacketReceived, IPacketProcessor packetProcessor)
    {
        _socket = socket;
        _onDisconnected = onDisconnected;
        _onPacketReceived = onPacketReceived;
        _packetProcessor = packetProcessor;

        // 각 세션은 고유한 MessageFramer 인스턴스를 가집니다.
        _messageFramer = new LengthPrefixedMessageFramer();
        _messageFramer.MessageReceived += OnMessageReceived;
    }

    private void OnMessageReceived(byte[] data)
    {
        _onPacketReceived(this, data);
    }

    internal void ProcessReceive(byte[] buffer, int offset, int count)
    {
        // MessageFramer를 통해 메시지를 파싱합니다.
        _messageFramer.ProcessReceivedData(new ArraySegment<byte>(buffer, offset, count));
    }

    public void Send(IPacket packet)
    {
        if (!IsConnected)
        {
            return;
        }

        byte[] packetData = _packetProcessor.SerializePacket(packet);
        byte[] framedData = _messageFramer.FrameMessage(packetData);

        // Send를 위한 새로운 SocketAsyncEventArgs를 생성하여 비동기 전송
        var sendArgs = new SocketAsyncEventArgs();
        sendArgs.SetBuffer(framedData, 0, framedData.Length);
        sendArgs.Completed += IO_Completed; // 전송 완료 후 콜백
        sendArgs.UserToken = this;

        try
        {
            bool willRaiseEvent = _socket.SendAsync(sendArgs);
            if (!willRaiseEvent)
            {
                ProcessSend(sendArgs);
            }
        }
        catch (ObjectDisposedException)
        {
            // 소켓이 이미 닫혔을 경우
            Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Send Error: {ex.Message}");
            Close();
        }
    }

    internal void ProcessSend(SocketAsyncEventArgs e)
    {
        if (e.SocketError != SocketError.Success)
        {
            Close();
        }

        // Send를 위해 사용한 SocketAsyncEventArgs는 재사용하지 않고 Dispose
        e.Dispose();
    }

    public void Disconnect()
    {
        Close();
    }
    
    internal void Close()
    {
        if (_socket == null) return;
        
        // 연결 종료 콜백 호출
        _onDisconnected(this);
        
        try
        {
            _socket.Shutdown(SocketShutdown.Both);
        }
        catch (Exception) { /* 이미 닫혔을 수 있음 */ }
        finally
        {
            _socket.Close();
            _socket = null;
        }
    }

    // SendAsync 완료 시 호출되는 콜백
    private void IO_Completed(object sender, SocketAsyncEventArgs e)
    {
        var session = (TcpSession)e.UserToken;
        switch (e.LastOperation)
        {
            case SocketAsyncOperation.Send:
                session.ProcessSend(e);
                break;
            default:
                throw new ArgumentException("The last operation completed on the socket was not a send");
        }
    }
}