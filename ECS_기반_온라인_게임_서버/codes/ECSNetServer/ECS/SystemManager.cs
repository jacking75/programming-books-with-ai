using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECSNetServer.ECS;


// 시스템 매니저
public class SystemManager
{
    private readonly List<ISystem> _systems = new();
    private readonly Core.ILogger _logger;

    public SystemManager(Core.ILogger logger)
    {
        _logger = logger;
    }

    // 시스템 등록
    public void RegisterSystem(ISystem system)
    {
        _systems.Add(system);
        _logger.LogInfo($"시스템 등록됨: {system.GetType().Name}");
    }

    // 모든 시스템 초기화
    public void Initialize()
    {
        foreach (var system in _systems)
        {
            try
            {
                system.Initialize();
            }
            catch (Exception ex)
            {
                _logger.LogError($"시스템 초기화 오류 ({system.GetType().Name}): {ex.Message}");
            }
        }
    }

    // 모든 시스템 업데이트
    public void Update(Core.TickContext context)
    {
        foreach (var system in _systems)
        {
            try
            {
                system.Update(context);
            }
            catch (Exception ex)
            {
                _logger.LogError($"시스템 업데이트 오류 ({system.GetType().Name}): {ex.Message}");
            }
        }
    }
}

