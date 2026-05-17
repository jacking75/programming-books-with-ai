using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECSNetServer.Components;

// 플레이어 컴포넌트
public struct PlayerComponent : ECS.IComponent
{
    public string Username;
    public int Score;

    public PlayerComponent(string username)
    {
        Username = username;
        Score = 0;
    }
}