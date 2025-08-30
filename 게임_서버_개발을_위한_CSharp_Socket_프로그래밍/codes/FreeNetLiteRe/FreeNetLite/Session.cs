#nullable enable
#pragma warning disable CS9113

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace FreeNetLite;

public class Session(
    bool isClient,
    ulong uniqueId,
    IPacketDispatcher dispatcher,
    ServerOption serverOption)
{
    private enum State { Idle, Connected, Closed }

    public ulong UniqueId { get; } = uniqueId;
    public ServerOption ServerOpt { get; } = serverOption;

    public Socket? Sock { get; set; }
    public SocketAsyncEventArgs? ReceiveEventArgs { get; private set; }
    public SocketAsyncEventArgs? SendEventArgs { get; private set; }

    private readonly IPacketDispatcher _dispatcher = dispatcher;
    private readonly ConcurrentQueue<ArraySegment<byte>> _sendingQueue = new();
    private int _currentState = (int)State.Idle;
    private int _sending; // 0 for false, 1 for true

    public event Action<Session>? OnEventSessionClosed;


    public void OnConnected()
    {
        if (Interlocked.CompareExchange(ref _currentState, (int)State.Connected, (int)State.Idle) != (int)State.Idle)
            return;

        var connectedPacket = new Packet(this, NetworkDefine.SysNtfConnected, null);
        _dispatcher.IncomingPacket(true, this, connectedPacket);
    }

    public void SetEventArgs(SocketAsyncEventArgs? receiveEventArgs, SocketAsyncEventArgs? sendEventArgs)
    {
        ReceiveEventArgs = receiveEventArgs;
        SendEventArgs = sendEventArgs;
    }

    public void OnReceive(byte[] buffer, int offset, int transferred)
    {
        _dispatcher.DispatchPacket(this, new ReadOnlyMemory<byte>(buffer, offset, transferred));
    }

    public void Close()
    {
        if (Interlocked.Exchange(ref _currentState, (int)State.Closed) == (int)State.Closed)
        {
            return;
        }

        try
        {
            Sock?.Shutdown(SocketShutdown.Both);
        }
        catch { /* 무시 */ }
        finally
        {
            Sock?.Close();
            Sock = null;

            if (SendEventArgs is not null) SendEventArgs.UserToken = null;
            if (ReceiveEventArgs is not null) ReceiveEventArgs.UserToken = null;

            _sendingQueue.Clear();
            OnEventSessionClosed?.Invoke(this);

            var closedPacket = new Packet(this, NetworkDefine.SysNtfClosed, null);
            _dispatcher.IncomingPacket(true, this, closedPacket);
        }
    }

    public void Send(ArraySegment<byte> packetData)
    {
        if (!IsConnected()) return;

        _sendingQueue.Enqueue(packetData);

        if (Interlocked.CompareExchange(ref _sending, 1, 0) == 0)
        {
            StartSend();
        }
    }

    private void StartSend()
    {
        if (!IsConnected() || Sock is null || SendEventArgs is null)
        {
            Interlocked.Exchange(ref _sending, 0);
            return;
        }

        try
        {
            var sendingList = new List<ArraySegment<byte>>();
            while (_sendingQueue.TryDequeue(out var data))
            {
                sendingList.Add(data);
                if (sendingList.Sum(s => s.Count) >= ServerOpt.MaxPacketSize)
                {
                    break;
                }
            }

            if (sendingList.Count == 0)
            {
                Interlocked.Exchange(ref _sending, 0);
                return;
            }

            SendEventArgs.BufferList = sendingList;
            if (!Sock.SendAsync(SendEventArgs))
            {
                ProcessSend(SendEventArgs);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"StartSend error: {ex.Message}");
            Close();
        }
    }

    public void ProcessSend(SocketAsyncEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (e.BytesTransferred <= 0 || e.SocketError != SocketError.Success)
        {
            Close();
            return;
        }

        if (!_sendingQueue.IsEmpty)
        {
            StartSend();
        }
        else
        {
            Interlocked.Exchange(ref _sending, 0);
        }
    }

    public bool IsConnected() => Volatile.Read(ref _currentState) == (int)State.Connected;
}