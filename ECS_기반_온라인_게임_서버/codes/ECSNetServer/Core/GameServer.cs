using ECSNetServer.ECS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECSNetServer.Core;


public class GameServer
{
    private readonly GameWorld _world;
    private readonly SystemManager _systemManager;
    private readonly Network.NetworkManager _networkManager;
    private readonly ILogger _logger;

    private bool _isRunning;
    private int _targetTickRate;
    private TimeSpan _targetTickTime;
    private long _currentTick;

    public GameServer(
        GameWorld world,
        SystemManager systemManager,
        Network.NetworkManager networkManager,
        ILogger logger)
    {
        _world = world;
        _systemManager = systemManager;
        _networkManager = networkManager;
        _logger = logger;
        _targetTickRate = 20; // 기본 20 TPS (틱/초)
        _targetTickTime = TimeSpan.FromMilliseconds(1000.0 / _targetTickRate);
        _currentTick = 0;
    }

    public void SetTickRate(int ticksPerSecond)
    {
        _targetTickRate = ticksPerSecond;
        _targetTickTime = TimeSpan.FromMilliseconds(1000.0 / _targetTickRate);
        _logger.LogInfo($"틱 레이트 설정: {_targetTickRate} TPS ({_targetTickTime.TotalMilliseconds}ms/틱)");
    }

    public async Task StartAsync(int port)
    {
        if (_isRunning)
        {
            _logger.LogWarning("서버가 이미 실행 중입니다.");
            return;
        }

        try
        {
            // 네트워크 리스너 시작
            await _networkManager.StartAsync(port);

            // 게임 월드 초기화
            _world.Initialize();

            // 시스템 초기화
            _systemManager.Initialize();

            _isRunning = true;
            _logger.LogInfo($"게임 서버 시작됨 (포트: {port}, 틱 레이트: {_targetTickRate})");

            // 게임 루프 시작
            _ = GameLoopAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"서버 시작 오류: {ex.Message}");
            throw;
        }
    }

    public async Task StopAsync()
    {
        if (!_isRunning) return;

        _isRunning = false;
        // 게임 루프가 완전히 종료될 때까지 잠시 대기
        await Task.Delay(100);

        _logger.LogInfo("서버 종료 중...");
    }

    private async Task GameLoopAsync()
    {
        var stopwatch = new Stopwatch();
        var lastTime = DateTime.UtcNow;

        while (_isRunning)
        {
            stopwatch.Restart();

            var currentTime = DateTime.UtcNow;
            var deltaTime = (float)(currentTime - lastTime).TotalSeconds;
            lastTime = currentTime;

            // 틱 컨텍스트 생성
            var tickContext = new TickContext
            {
                DeltaTime = deltaTime,
                CurrentTick = _currentTick,
                CurrentTime = currentTime
            };

            try
            {
                // 1. 시스템 업데이트
                _systemManager.Update(tickContext);

                // 2. 틱 카운터 증가
                _currentTick++;

                // 3. 월드 상태 정리 (필요한 경우)
                _world.CleanUp();
            }
            catch (Exception ex)
            {
                _logger.LogError($"게임 루프 오류: {ex.Message}");
            }

            // 프레임 시간 측정 및 대기
            stopwatch.Stop();
            var elapsedTime = stopwatch.Elapsed;
            var remainingTime = _targetTickTime - elapsedTime;

            if (remainingTime > TimeSpan.Zero)
            {
                await Task.Delay(remainingTime);
            }
            else
            {
                _logger.LogWarning($"틱 지연 발생: {Math.Abs(remainingTime.TotalMilliseconds):F2}ms");
            }
        }

        _logger.LogInfo("게임 루프 종료됨");
    }
}