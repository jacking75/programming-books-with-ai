using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECSNetServer.Network;


// 서버 리스너 인터페이스
public interface INetworkListener
{
    Task StartAsync(int port);
    Task StopAsync();
    event Action<IConnection> OnClientConnected;
    event Action<IConnection, byte[]> OnDataReceived;
    event Action<IConnection> OnClientDisconnected;
}  