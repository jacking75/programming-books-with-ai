using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECSNetServer.Network;

// 네트워크 연결을 나타내는 인터페이스
public interface IConnection
{
    string ConnectionId { get; }
    bool IsConnected { get; }
    Task SendAsync(byte[] data);
    void Close();
}
