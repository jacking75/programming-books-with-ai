using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECSNetServer.Components;


// 속도 컴포넌트
public struct VelocityComponent : ECS.IComponent
{
    public float VX;
    public float VY;

    public VelocityComponent(float vx, float vy)
    {
        VX = vx;
        VY = vy;
    }

    public override string ToString() => $"Vel({VX}, {VY})";
}