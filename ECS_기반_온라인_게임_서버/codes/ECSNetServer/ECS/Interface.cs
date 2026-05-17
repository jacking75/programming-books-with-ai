using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECSNetServer.ECS;


// 컴포넌트 인터페이스
public interface IComponent
{
}

// 시스템 인터페이스
public interface ISystem
{
    void Initialize();
    void Update(Core.TickContext context);
}