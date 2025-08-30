using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace FreeNetLite;

class Listener
{
    bool _isStarted = false;
    Thread _workerThread = null;
    bool _isNoDelay = false;
    Socket _listenSocket;
    public Action<bool, Socket> OnNewClientCallback = null;


    public void Start(string host, int port, int backlog, bool isNoDelay)
    {
        _isNoDelay = isNoDelay;
        _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        IPAddress address;
        if (host == "0.0.0.0" || host.ToLower() == "localhost")
        {
            address = IPAddress.Any;
        }
        else
        {
            address = IPAddress.Parse(host);
        }
        
        var endpoint = new IPEndPoint(address, port);


        try
        {
            _listenSocket.Bind(endpoint);
            _listenSocket.Listen(backlog);

            _isStarted = true;
            _workerThread = new Thread(Run);
            _workerThread.Start();
        }
        catch (Exception e)
        {
            // TODO: 실제 서비스에서는 파일 로그 등으로 대체해야 합니다.
            Console.WriteLine($"[ERROR] Listener.Start failed: {e.Message}");
        }
    }

    public void Stop()
    {
        if (_isStarted == false)
        {
            return;
        }

        _isStarted = false;
        
        try
        {
            _listenSocket.Close();
        }
        catch (Exception) { }
        
        _workerThread.Join();
    }

    void Run()
    {
        Console.WriteLine("Listen Start");
        
        while (_isStarted)
        {
            try
            {
                var newClient = _listenSocket.Accept();
                newClient.NoDelay = _isNoDelay;
                OnNewClientCallback?.Invoke(true, newClient);
            }
            catch (SocketException e)
            {
                // 서버 소켓이 닫혔을 때 Accept에서 예외가 발생할 수 있습니다.
                if (_isStarted)
                {
                    // TODO: 로깅 시스템을 통해 기록해야 합니다.
                    Console.WriteLine($"[ERROR] Accept failed with SocketException: {e.Message}");
                }
                else
                {
                    // 정상적인 종료 과정입니다.
                    break;
                }
            }
            catch (Exception e)
            {
                // TODO: 로깅 시스템을 통해 기록해야 합니다.
                Console.WriteLine($"[ERROR] An unexpected error occurred in Listener.Run: {e.Message}");
                if (_isStarted == false) break;
            }
        }

        Console.WriteLine("Listen End");
    }
}