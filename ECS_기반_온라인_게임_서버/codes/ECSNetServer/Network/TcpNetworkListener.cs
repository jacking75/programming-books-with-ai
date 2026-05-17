using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ECSNetServer.Network;


// TCP 기반 네트워크 리스너 구현
public class TcpNetworkListener : INetworkListener
{
    private TcpListener _tcpListener;
    private bool _isRunning;
    private readonly List<TcpConnection> _activeConnections = new();

    public event Action<IConnection> OnClientConnected;
    public event Action<IConnection, byte[]> OnDataReceived;
    public event Action<IConnection> OnClientDisconnected;

    public async Task StartAsync(int port)
    {
        _tcpListener = new TcpListener(IPAddress.Any, port);
        _tcpListener.Start();
        _isRunning = true;

        while (_isRunning)
        {
            try
            {
                var client = await _tcpListener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client);
            }
            catch (Exception ex) when (_isRunning)
            {
                Console.WriteLine($"리스너 오류: {ex.Message}");
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        var connection = new TcpConnection(client);
        _activeConnections.Add(connection);

        OnClientConnected?.Invoke(connection);

        var buffer = new byte[4096];

        try
        {
            var stream = client.GetStream();

            while (client.Connected)
            {
                var bytesRead = await stream.ReadAsync(buffer);
                if (bytesRead == 0) break; // 연결 종료됨

                var data = new byte[bytesRead];
                Array.Copy(buffer, data, bytesRead);

                OnDataReceived?.Invoke(connection, data);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"클라이언트 처리 오류: {ex.Message}");
        }
        finally
        {
            _activeConnections.Remove(connection);
            OnClientDisconnected?.Invoke(connection);
            client.Close();
        }
    }

    public Task StopAsync()
    {
        _isRunning = false;
        _tcpListener?.Stop();

        foreach (var connection in _activeConnections.ToList())
        {
            connection.Close();
        }

        _activeConnections.Clear();
        return Task.CompletedTask;
    }

    private class TcpConnection : IConnection
    {
        private readonly TcpClient _client;

        public TcpConnection(TcpClient client)
        {
            _client = client;
            ConnectionId = Guid.NewGuid().ToString();
        }

        public string ConnectionId { get; }

        public bool IsConnected => _client.Connected;

        public async Task SendAsync(byte[] data)
        {
            if (!IsConnected) return;

            try
            {
                var stream = _client.GetStream();
                await stream.WriteAsync(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"데이터 전송 오류: {ex.Message}");
                Close();
            }
        }

        public void Close()
        {
            _client.Close();
        }
    }
}