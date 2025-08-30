#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;


namespace FreeNetLite;

public class NetworkService(ServerOption serverOption, IPacketDispatcher packetDispatcher)
{
    private readonly SocketAsyncEventArgsPool _receiveEventArgsPool = new();
    private readonly SocketAsyncEventArgsPool _sendEventArgsPool = new();
    private readonly Listener _clientListener = new();
    
    private ulong _sequenceId;
    private long _connectedSessionCount;

    public IPacketDispatcher PacketDispatcher { get; } = packetDispatcher;
    public ServerOption ServerOpt { get; } = serverOption;
    public long ConnectedSessionCount => Interlocked.Read(ref _connectedSessionCount);
    

    public void Initialize()
    {
        _receiveEventArgsPool.Init(ServerOpt.MaxConnectionCount, ServerOpt.ReceiveBufferSize);
        _sendEventArgsPool.Init(ServerOpt.MaxConnectionCount, 0); // Send Buffer는 동적으로 할당

        for (int i = 0; i < ServerOpt.MaxConnectionCount; i++)
        {
            var receiveArg = new SocketAsyncEventArgs();
            receiveArg.Completed += OnReceiveCompleted;
            _receiveEventArgsPool.Allocate(receiveArg);

            var sendArg = new SocketAsyncEventArgs();
            sendArg.Completed += OnSendCompleted;
            _sendEventArgsPool.Push(sendArg);
        }
    }

    public void Start(string host, int port, int backlog, bool isNoDelay)
    {
        _clientListener.OnNewClientCallback += OnNewClient;
        _clientListener.Start(host, port, backlog, isNoDelay);
    }

    public void Stop() => _clientListener.Stop();

    private ulong MakeSequenceIdForSession() => ++_sequenceId;

    private void OnNewClient(bool isAccepted, Socket clientSocket)
    {
        ArgumentNullException.ThrowIfNull(clientSocket);

        Interlocked.Increment(ref _connectedSessionCount);

        var uniqueId = MakeSequenceIdForSession();
        var session = new Session(true, uniqueId, PacketDispatcher, ServerOpt);
        session.OnEventSessionClosed += OnSessionClosed;

        var receiveArgs = _receiveEventArgsPool.Pop();
        var sendArgs = _sendEventArgsPool.Pop();

        if (receiveArgs is null || sendArgs is null)
        {
            Console.WriteLine("[ERROR] Failed to allocate SocketAsyncEventArgs.");
            clientSocket.Close();
            return;
        }

        receiveArgs.UserToken = session;
        sendArgs.UserToken = session;

        session.OnConnected();
        BeginReceive(clientSocket, receiveArgs, sendArgs);
    }

    private void BeginReceive(Socket socket, SocketAsyncEventArgs receiveArgs, SocketAsyncEventArgs sendArgs)
    {
        if (receiveArgs.UserToken is not Session session) return;

        session.Sock = socket;
        session.SetEventArgs(receiveArgs, sendArgs);

        try
        {
            if (!socket.ReceiveAsync(receiveArgs))
            {
                OnReceiveCompleted(this, receiveArgs);
            }
        }
        catch (ObjectDisposedException) { /* 소켓이 닫힌 경우 무시 */ }
        catch (Exception ex)
        {
            Console.WriteLine($"BeginReceive error: {ex.Message}");
            session.Close();
        }
    }

    private void OnReceiveCompleted(object? sender, SocketAsyncEventArgs e)
    {
        if (e.LastOperation != SocketAsyncOperation.Receive || e.UserToken is not Session session) return;

        try
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                session.OnReceive(e.Buffer!, e.Offset, e.BytesTransferred);
                if (session.Sock is not null && !session.Sock.ReceiveAsync(e))
                {
                    OnReceiveCompleted(sender, e);
                }
            }
            else
            {
                session.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OnReceiveCompleted error: {ex.Message}");
            session.Close();
        }
    }

    private void OnSendCompleted(object? sender, SocketAsyncEventArgs e)
    {
        if (e.UserToken is Session session && session.IsConnected())
        {
            session.ProcessSend(e);
        }
    }

    private void OnSessionClosed(Session session)
    {
        Interlocked.Decrement(ref _connectedSessionCount);

        if (session.ReceiveEventArgs is not null)
        {
            _receiveEventArgsPool.Push(session.ReceiveEventArgs);
        }

        if (session.SendEventArgs is not null)
        {
            _sendEventArgsPool.Push(session.SendEventArgs);
        }
        
        session.SetEventArgs(null, null);
    }
}