using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleECSGameServer;

// --------------------------------
// 컴포넌트 인터페이스
// --------------------------------
public interface IComponent { }


// --------------------------------
// 기본 컴포넌트 정의
// --------------------------------
public struct PositionComponent : IComponent
{
    public float X;
    public float Y;

    public PositionComponent(float x, float y)
    {
        X = x;
        Y = y;
    }
}

public struct VelocityComponent : IComponent
{
    public float X;
    public float Y;

    public VelocityComponent(float x, float y)
    {
        X = x;
        Y = y;
    }
}

public struct HealthComponent : IComponent
{
    public int Current;
    public int Maximum;

    public HealthComponent(int current, int maximum)
    {
        Current = current;
        Maximum = maximum;
    }
}

public struct PlayerComponent : IComponent
{
    public uint ClientId;
    public string Name;

    public PlayerComponent(uint clientId, string name)
    {
        ClientId = clientId;
        Name = name;
    }
}


// 네트워크 컴포넌트
public struct NetworkComponent : IComponent
{
    public uint ClientId;

    public NetworkComponent(uint clientId)
    {
        ClientId = clientId;
    }
}