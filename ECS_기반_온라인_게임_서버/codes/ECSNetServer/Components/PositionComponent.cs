using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECSNetServer.Components;


// 위치 컴포넌트
public struct PositionComponent : ECS.IComponent
{
    public float X;
    public float Y;

    public PositionComponent(float x, float y)
    {
        X = x;
        Y = y;
    }

    public override string ToString() => $"Pos({X}, {Y})";
}