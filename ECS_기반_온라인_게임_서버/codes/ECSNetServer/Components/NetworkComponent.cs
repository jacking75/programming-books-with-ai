using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECSNetServer.Components;

// 네트워크 컴포넌트
public struct NetworkComponent : ECS.IComponent
{
    public string ConnectionId;

    public NetworkComponent(string connectionId)
    {
        ConnectionId = connectionId;
    }
}