using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleECSGameServer;


// 확장 메서드
public static class ECSWorldExtensions
{
    public static T GetSystem<T>(this ECSWorld world) where T : class, ISystem
    {
        // 실제 구현에서는 시스템 저장소에서 검색하도록 구현 필요
        return null;
    }
}