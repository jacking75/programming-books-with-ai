# C#과 P2P 통신을 이용한 온라인 게임 만들기

저자: 최흥배, AI-Assisted   
  
------  
  
# 9장: 지연 시간 최적화
온라인 액션 게임에서 지연 시간(latency)은 게임 경험을 좌우하는 핵심 요소입니다. P2P 연결에서 홀펀칭 과정과 데이터 전송 경로 최적화를 통해 지연 시간을 최소화하는 방법을 알아보겠습니다.

## 9.1 홀펀칭 지연 시간 최소화 기법
홀펀칭 과정에서 발생하는 지연 시간을 줄이기 위해서는 병렬 처리, 캐싱, 그리고 최적화된 알고리즘이 필요합니다.

### 9.1.1 병렬 홀펀칭과 타임아웃 최적화
여러 후보 경로를 동시에 시도하여 가장 빠른 연결을 확보하는 방법입니다.

```csharp
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class OptimizedHolePuncher
{
    private readonly ConcurrentDictionary<string, ConnectionCandidate> _candidates;
    private readonly int _baseTimeout = 500; // 기본 타임아웃 (ms)
    private readonly int _maxConcurrentAttempts = 8;
    
    public OptimizedHolePuncher()
    {
        _candidates = new ConcurrentDictionary<string, ConnectionCandidate>();
    }
    
    public async Task<UdpClient> FastHolePunchAsync(List<IPEndPoint> candidateEndpoints, 
        CancellationToken cancellationToken = default)
    {
        var completionSource = new TaskCompletionSource<UdpClient>();
        var semaphore = new SemaphoreSlim(_maxConcurrentAttempts);
        var attempts = new List<Task>();
        
        foreach (var endpoint in candidateEndpoints)
        {
            var attempt = TryConnectWithAdaptiveTimeout(endpoint, semaphore, completionSource);
            attempts.Add(attempt);
        }
        
        // 첫 번째 성공한 연결을 반환
        var firstCompleted = await Task.WhenAny(
            completionSource.Task,
            Task.Delay(TimeSpan.FromSeconds(10), cancellationToken)
        );
        
        if (firstCompleted == completionSource.Task)
        {
            return await completionSource.Task;
        }
        
        throw new TimeoutException("홀펀칭이 타임아웃되었습니다.");
    }
    
    private async Task TryConnectWithAdaptiveTimeout(IPEndPoint endpoint, 
        SemaphoreSlim semaphore, TaskCompletionSource<UdpClient> completionSource)
    {
        await semaphore.WaitAsync();
        
        try
        {
            var candidateKey = endpoint.ToString();
            var candidate = _candidates.GetOrAdd(candidateKey, 
                _ => new ConnectionCandidate(endpoint));
            
            // 이전 연결 시도 기록을 바탕으로 적응적 타임아웃 계산
            var adaptiveTimeout = CalculateAdaptiveTimeout(candidate);
            
            using var cts = new CancellationTokenSource(adaptiveTimeout);
            var udpClient = await AttemptConnection(endpoint, cts.Token);
            
            if (udpClient != null)
            {
                candidate.RecordSuccess();
                completionSource.TrySetResult(udpClient);
            }
        }
        catch (Exception ex)
        {
            var candidateKey = endpoint.ToString();
            if (_candidates.TryGetValue(candidateKey, out var candidate))
            {
                candidate.RecordFailure();
            }
            Console.WriteLine($"연결 시도 실패 {endpoint}: {ex.Message}");
        }
        finally
        {
            semaphore.Release();
        }
    }
    
    private int CalculateAdaptiveTimeout(ConnectionCandidate candidate)
    {
        // 성공률과 평균 응답 시간을 고려한 적응적 타임아웃
        var baseTimeout = _baseTimeout;
        
        if (candidate.AverageResponseTime > 0)
        {
            // 평균 응답 시간의 3배로 설정
            baseTimeout = Math.Max(_baseTimeout, (int)(candidate.AverageResponseTime * 3));
        }
        
        // 실패율이 높으면 타임아웃을 줄여서 빠르게 포기
        if (candidate.FailureRate > 0.7)
        {
            baseTimeout = (int)(baseTimeout * 0.5);
        }
        
        return Math.Min(baseTimeout, 2000); // 최대 2초
    }
    
    private async Task<UdpClient> AttemptConnection(IPEndPoint endpoint, 
        CancellationToken cancellationToken)
    {
        var udpClient = new UdpClient();
        var startTime = DateTime.UtcNow;
        
        try
        {
            // 홀펀칭 패킷 전송
            var punchPacket = System.Text.Encoding.UTF8.GetBytes("PUNCH");
            await udpClient.SendAsync(punchPacket, punchPacket.Length, endpoint);
            
            // 응답 대기 (짧은 타임아웃으로 빠른 실패 처리)
            var response = await udpClient.ReceiveAsync().WaitAsync(cancellationToken);
            
            var responseTime = DateTime.UtcNow - startTime;
            var candidateKey = endpoint.ToString();
            
            if (_candidates.TryGetValue(candidateKey, out var candidate))
            {
                candidate.RecordResponseTime(responseTime.TotalMilliseconds);
            }
            
            return udpClient;
        }
        catch
        {
            udpClient?.Dispose();
            throw;
        }
    }
}

public class ConnectionCandidate
{
    public IPEndPoint EndPoint { get; }
    public int SuccessCount { get; private set; }
    public int FailureCount { get; private set; }
    public double AverageResponseTime { get; private set; }
    public double FailureRate => TotalAttempts > 0 ? (double)FailureCount / TotalAttempts : 0;
    public int TotalAttempts => SuccessCount + FailureCount;
    
    private readonly object _lock = new object();
    private double _totalResponseTime;
    
    public ConnectionCandidate(IPEndPoint endPoint)
    {
        EndPoint = endPoint;
    }
    
    public void RecordSuccess()
    {
        lock (_lock)
        {
            SuccessCount++;
        }
    }
    
    public void RecordFailure()
    {
        lock (_lock)
        {
            FailureCount++;
        }
    }
    
    public void RecordResponseTime(double responseTimeMs)
    {
        lock (_lock)
        {
            _totalResponseTime += responseTimeMs;
            if (SuccessCount > 0)
            {
                AverageResponseTime = _totalResponseTime / SuccessCount;
            }
        }
    }
}
```

### 9.1.2 STUN 바인딩 캐싱
STUN 서버에서 얻은 바인딩 정보를 캐시하여 재사용하는 방법입니다.

```csharp
using System;
using System.Collections.Concurrent;
using System.Net;

public class StunBindingCache
{
    private readonly ConcurrentDictionary<string, CachedBinding> _cache;
    private readonly TimeSpan _defaultTtl = TimeSpan.FromMinutes(5);
    
    public StunBindingCache()
    {
        _cache = new ConcurrentDictionary<string, CachedBinding>();
        
        // 백그라운드에서 만료된 캐시 정리
        _ = Task.Run(CleanupExpiredBindings);
    }
    
    public void CacheBinding(string localInterface, IPEndPoint publicEndpoint, 
        TimeSpan? ttl = null)
    {
        var binding = new CachedBinding
        {
            PublicEndpoint = publicEndpoint,
            LocalInterface = localInterface,
            Timestamp = DateTime.UtcNow,
            TimeToLive = ttl ?? _defaultTtl
        };
        
        _cache.AddOrUpdate(localInterface, binding, (key, old) => binding);
        
        Console.WriteLine($"STUN 바인딩 캐시됨: {localInterface} -> {publicEndpoint}");
    }
    
    public IPEndPoint GetCachedBinding(string localInterface)
    {
        if (_cache.TryGetValue(localInterface, out var binding))
        {
            if (DateTime.UtcNow - binding.Timestamp < binding.TimeToLive)
            {
                Console.WriteLine($"캐시된 바인딩 사용: {localInterface} -> {binding.PublicEndpoint}");
                return binding.PublicEndpoint;
            }
            else
            {
                // 만료된 캐시 제거
                _cache.TryRemove(localInterface, out _);
            }
        }
        
        return null;
    }
    
    public bool InvalidateBinding(string localInterface)
    {
        var removed = _cache.TryRemove(localInterface, out _);
        if (removed)
        {
            Console.WriteLine($"바인딩 캐시 무효화: {localInterface}");
        }
        return removed;
    }
    
    private async Task CleanupExpiredBindings()
    {
        while (true)
        {
            try
            {
                var now = DateTime.UtcNow;
                var expiredKeys = new List<string>();
                
                foreach (var kvp in _cache)
                {
                    if (now - kvp.Value.Timestamp >= kvp.Value.TimeToLive)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }
                
                foreach (var key in expiredKeys)
                {
                    _cache.TryRemove(key, out _);
                }
                
                if (expiredKeys.Count > 0)
                {
                    Console.WriteLine($"만료된 바인딩 {expiredKeys.Count}개 정리됨");
                }
                
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"캐시 정리 중 오류: {ex.Message}");
            }
        }
    }
    
    private class CachedBinding
    {
        public IPEndPoint PublicEndpoint { get; set; }
        public string LocalInterface { get; set; }
        public DateTime Timestamp { get; set; }
        public TimeSpan TimeToLive { get; set; }
    }
}
```
 

## 9.2 다중 경로 탐색과 최적 경로 선택
여러 네트워크 경로를 동시에 탐색하고 가장 효율적인 경로를 선택하는 방법입니다.

### 9.2.1 다중 경로 품질 측정

```csharp
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;

public class PathQualityAnalyzer
{
    private readonly ConcurrentDictionary<string, PathMetrics> _pathMetrics;
    private readonly int _measurementWindow = 10; // 최근 10회 측정 기준
    
    public PathQualityAnalyzer()
    {
        _pathMetrics = new ConcurrentDictionary<string, PathMetrics>();
    }
    
    public async Task<PathQuality> AnalyzePathQuality(IPEndPoint endpoint, 
        int testPackets = 5)
    {
        var pathKey = endpoint.ToString();
        var metrics = _pathMetrics.GetOrAdd(pathKey, _ => new PathMetrics());
        
        var latencies = new List<double>();
        var packetLoss = 0;
        var jitterValues = new List<double>();
        
        for (int i = 0; i < testPackets; i++)
        {
            try
            {
                var latency = await MeasureLatency(endpoint);
                latencies.Add(latency);
                
                if (latencies.Count > 1)
                {
                    var jitter = Math.Abs(latency - latencies[latencies.Count - 2]);
                    jitterValues.Add(jitter);
                }
                
                await Task.Delay(100); // 패킷 간 간격
            }
            catch
            {
                packetLoss++;
            }
        }
        
        var avgLatency = latencies.Count > 0 ? latencies.Average() : double.MaxValue;
        var avgJitter = jitterValues.Count > 0 ? jitterValues.Average() : 0;
        var lossRate = (double)packetLoss / testPackets;
        
        var quality = new PathQuality
        {
            Endpoint = endpoint,
            AverageLatency = avgLatency,
            Jitter = avgJitter,
            PacketLossRate = lossRate,
            QualityScore = CalculateQualityScore(avgLatency, avgJitter, lossRate),
            Timestamp = DateTime.UtcNow
        };
        
        metrics.RecordMeasurement(quality);
        
        return quality;
    }
    
    private async Task<double> MeasureLatency(IPEndPoint endpoint)
    {
        using var udpClient = new UdpClient();
        var stopwatch = Stopwatch.StartNew();
        
        var testPacket = System.Text.Encoding.UTF8.GetBytes("PING");
        await udpClient.SendAsync(testPacket, testPacket.Length, endpoint);
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await udpClient.ReceiveAsync().WaitAsync(cts.Token);
        
        stopwatch.Stop();
        return stopwatch.Elapsed.TotalMilliseconds;
    }
    
    private double CalculateQualityScore(double latency, double jitter, double lossRate)
    {
        // 점수 계산 (0-100, 높을수록 좋음)
        var latencyScore = Math.Max(0, 100 - (latency / 10)); // 1000ms에서 0점
        var jitterScore = Math.Max(0, 100 - (jitter * 2)); // 50ms에서 0점  
        var lossScore = Math.Max(0, 100 - (lossRate * 200)); // 50% 손실에서 0점
        
        // 가중 평균 (지연시간을 가장 중요하게)
        return (latencyScore * 0.5) + (jitterScore * 0.3) + (lossScore * 0.2);
    }
    
    public PathQuality GetBestPath(List<IPEndPoint> candidates)
    {
        PathQuality bestPath = null;
        double bestScore = -1;
        
        foreach (var candidate in candidates)
        {
            var pathKey = candidate.ToString();
            if (_pathMetrics.TryGetValue(pathKey, out var metrics))
            {
                var recentQuality = metrics.GetRecentQuality();
                if (recentQuality != null && recentQuality.QualityScore > bestScore)
                {
                    bestScore = recentQuality.QualityScore;
                    bestPath = recentQuality;
                }
            }
        }
        
        return bestPath;
    }
}

public class PathQuality
{
    public IPEndPoint Endpoint { get; set; }
    public double AverageLatency { get; set; }
    public double Jitter { get; set; }
    public double PacketLossRate { get; set; }
    public double QualityScore { get; set; }
    public DateTime Timestamp { get; set; }
    
    public override string ToString()
    {
        return $"Path to {Endpoint}: Latency={AverageLatency:F1}ms, " +
               $"Jitter={Jitter:F1}ms, Loss={PacketLossRate:P1}, Score={QualityScore:F1}";
    }
}

public class PathMetrics
{
    private readonly Queue<PathQuality> _recentMeasurements = new Queue<PathQuality>();
    private readonly object _lock = new object();
    private readonly int _maxMeasurements = 10;
    
    public void RecordMeasurement(PathQuality quality)
    {
        lock (_lock)
        {
            _recentMeasurements.Enqueue(quality);
            
            while (_recentMeasurements.Count > _maxMeasurements)
            {
                _recentMeasurements.Dequeue();
            }
        }
    }
    
    public PathQuality GetRecentQuality()
    {
        lock (_lock)
        {
            return _recentMeasurements.Count > 0 ? _recentMeasurements.Last() : null;
        }
    }
    
    public PathQuality GetAverageQuality()
    {
        lock (_lock)
        {
            if (_recentMeasurements.Count == 0) return null;
            
            var avgLatency = _recentMeasurements.Average(m => m.AverageLatency);
            var avgJitter = _recentMeasurements.Average(m => m.Jitter);
            var avgLoss = _recentMeasurements.Average(m => m.PacketLossRate);
            var avgScore = _recentMeasurements.Average(m => m.QualityScore);
            
            return new PathQuality
            {
                Endpoint = _recentMeasurements.First().Endpoint,
                AverageLatency = avgLatency,
                Jitter = avgJitter,
                PacketLossRate = avgLoss,
                QualityScore = avgScore,
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
```

### 9.2.2 동적 경로 전환
네트워크 상태 변화에 따라 동적으로 최적 경로를 전환하는 시스템입니다.

```csharp
public class DynamicPathSwitcher
{
    private readonly PathQualityAnalyzer _analyzer;
    private readonly Timer _monitoringTimer;
    private PathQuality _currentPath;
    private readonly List<IPEndPoint> _alternativePaths;
    private readonly object _pathLock = new object();
    
    public event Action<PathQuality, PathQuality> PathSwitched;
    
    public DynamicPathSwitcher(PathQualityAnalyzer analyzer)
    {
        _analyzer = analyzer;
        _alternativePaths = new List<IPEndPoint>();
        
        // 5초마다 경로 품질 모니터링
        _monitoringTimer = new Timer(MonitorPaths, null, 
            TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }
    
    public void SetCurrentPath(PathQuality path)
    {
        lock (_pathLock)
        {
            _currentPath = path;
            Console.WriteLine($"현재 경로 설정: {path}");
        }
    }
    
    public void AddAlternativePath(IPEndPoint endpoint)
    {
        lock (_pathLock)
        {
            if (!_alternativePaths.Contains(endpoint))
            {
                _alternativePaths.Add(endpoint);
                Console.WriteLine($"대안 경로 추가: {endpoint}");
            }
        }
    }
    
    private async void MonitorPaths(object state)
    {
        try
        {
            await MonitorAndSwitchIfNeeded();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"경로 모니터링 중 오류: {ex.Message}");
        }
    }
    
    private async Task MonitorAndSwitchIfNeeded()
    {
        PathQuality currentPath;
        List<IPEndPoint> alternatives;
        
        lock (_pathLock)
        {
            currentPath = _currentPath;
            alternatives = new List<IPEndPoint>(_alternativePaths);
        }
        
        if (currentPath == null) return;
        
        // 현재 경로 품질 재측정
        var currentQuality = await _analyzer.AnalyzePathQuality(currentPath.Endpoint, 3);
        
        // 품질이 임계값 이하로 떨어진 경우 대안 경로 탐색
        if (ShouldSwitchPath(currentQuality))
        {
            var bestAlternative = await FindBestAlternativePath(alternatives);
            
            if (bestAlternative != null && bestAlternative.QualityScore > currentQuality.QualityScore + 10)
            {
                var oldPath = currentPath;
                lock (_pathLock)
                {
                    _currentPath = bestAlternative;
                }
                
                Console.WriteLine($"경로 전환: {oldPath.Endpoint} -> {bestAlternative.Endpoint}");
                PathSwitched?.Invoke(oldPath, bestAlternative);
            }
        }
    }
    
    private bool ShouldSwitchPath(PathQuality currentQuality)
    {
        // 경로 전환 조건
        return currentQuality.AverageLatency > 200 ||  // 200ms 초과
               currentQuality.PacketLossRate > 0.05 ||  // 5% 초과 패킷 손실
               currentQuality.QualityScore < 50;        // 품질 점수 50 미만
    }
    
    private async Task<PathQuality> FindBestAlternativePath(List<IPEndPoint> alternatives)
    {
        var tasks = alternatives.Select(endpoint => 
            _analyzer.AnalyzePathQuality(endpoint, 3)).ToArray();
        
        var qualities = await Task.WhenAll(tasks);
        
        return qualities.Where(q => q.QualityScore > 0)
                       .OrderByDescending(q => q.QualityScore)
                       .FirstOrDefault();
    }
    
    public PathQuality GetCurrentPath()
    {
        lock (_pathLock)
        {
            return _currentPath;
        }
    }
    
    public void Dispose()
    {
        _monitoringTimer?.Dispose();
    }
}
```
  

## 9.3 예측 연결과 사전 홀펀칭
게임 로직을 분석하여 필요한 연결을 미리 예측하고 사전에 홀펀칭을 수행하는 기법입니다.

### 9.3.1 연결 패턴 학습과 예측

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

public class ConnectionPredictor
{
    private readonly Dictionary<string, PlayerConnectionPattern> _patterns;
    private readonly Queue<ConnectionEvent> _recentEvents;
    private readonly int _maxEventHistory = 1000;
    private readonly double _predictionThreshold = 0.7;
    
    public ConnectionPredictor()
    {
        _patterns = new Dictionary<string, PlayerConnectionPattern>();
        _recentEvents = new Queue<ConnectionEvent>();
    }
    
    public void RecordConnectionEvent(string playerId, string targetPlayerId, 
        ConnectionEventType eventType, DateTime timestamp)
    {
        var connectionEvent = new ConnectionEvent
        {
            PlayerId = playerId,
            TargetPlayerId = targetPlayerId,
            EventType = eventType,
            Timestamp = timestamp
        };
        
        _recentEvents.Enqueue(connectionEvent);
        
        while (_recentEvents.Count > _maxEventHistory)
        {
            _recentEvents.Dequeue();
        }
        
        UpdatePlayerPattern(playerId, connectionEvent);
    }
    
    private void UpdatePlayerPattern(string playerId, ConnectionEvent connectionEvent)
    {
        if (!_patterns.TryGetValue(playerId, out var pattern))
        {
            pattern = new PlayerConnectionPattern(playerId);
            _patterns[playerId] = pattern;
        }
        
        pattern.AddEvent(connectionEvent);
    }
    
    public List<ConnectionPrediction> PredictConnections(string playerId, 
        List<string> availablePlayers, int maxPredictions = 5)
    {
        if (!_patterns.TryGetValue(playerId, out var pattern))
        {
            return new List<ConnectionPrediction>();
        }
        
        var predictions = new List<ConnectionPrediction>();
        
        foreach (var targetPlayer in availablePlayers)
        {
            if (targetPlayer == playerId) continue;
            
            var probability = CalculateConnectionProbability(pattern, targetPlayer);
            
            if (probability >= _predictionThreshold)
            {
                predictions.Add(new ConnectionPrediction
                {
                    PlayerId = playerId,
                    TargetPlayerId = targetPlayer,
                    Probability = probability,
                    PredictionTime = DateTime.UtcNow
                });
            }
        }
        
        return predictions.OrderByDescending(p => p.Probability)
                         .Take(maxPredictions)
                         .ToList();
    }
    
    private double CalculateConnectionProbability(PlayerConnectionPattern pattern, 
        string targetPlayerId)
    {
        var recentConnections = pattern.GetRecentConnections(TimeSpan.FromMinutes(30));
        var targetConnections = recentConnections.Where(e => e.TargetPlayerId == targetPlayerId).ToList();
        
        if (targetConnections.Count == 0) return 0;
        
        // 최근 연결 빈도
        var frequency = (double)targetConnections.Count / recentConnections.Count;
        
        // 시간 패턴 (같은 시간대에 연결되는 경향)
        var currentTime = DateTime.UtcNow.TimeOfDay;
        var timeSimilarity = CalculateTimeSimilarity(targetConnections, currentTime);
        
        // 연결 지속성 (한 번 연결되면 계속 연결되는 경향)
        var persistence = CalculateConnectionPersistence(targetConnections);
        
        // 종합 점수 계산
        return (frequency * 0.4) + (timeSimilarity * 0.3) + (persistence * 0.3);
    }
    
    private double CalculateTimeSimilarity(List<ConnectionEvent> targetConnections, 
        TimeSpan currentTime)
    {
        if (targetConnections.Count == 0) return 0;
        
        var similarities = targetConnections.Select(e => 
        {
            var eventTime = e.Timestamp.TimeOfDay;
            var timeDiff = Math.Abs((currentTime - eventTime).TotalHours);
            return Math.Max(0, 1 - (timeDiff / 12)); // 12시간 기준으로 유사도 계산
        });
        
        return similarities.Average();
    }
    
    private double CalculateConnectionPersistence(List<ConnectionEvent> targetConnections)
    {
        if (targetConnections.Count < 2) return 0;
        
        var connections = targetConnections.Where(e => e.EventType == ConnectionEventType.Connected).ToList();
        var disconnections = targetConnections.Where(e => e.EventType == ConnectionEventType.Disconnected).ToList();
        
        if (connections.Count == 0) return 0;
        
        var totalConnectionTime = TimeSpan.Zero;
        var connectionCount = 0;
        
        foreach (var connection in connections)
        {
            var disconnection = disconnections.FirstOrDefault(d => d.Timestamp > connection.Timestamp);
            if (disconnection != null)
            {
                totalConnectionTime += disconnection.Timestamp - connection.Timestamp;
                connectionCount++;
            }
        }
        
        if (connectionCount == 0) return 0.5; // 연결만 있고 종료가 없으면 중간 점수
        
        var avgConnectionTime = totalConnectionTime.TotalMinutes / connectionCount;
        return Math.Min(1.0, avgConnectionTime / 60); // 1시간 기준으로 정규화
    }
}

public class PlayerConnectionPattern
{
    public string PlayerId { get; }
    private readonly Queue<ConnectionEvent> _events;
    private readonly int _maxEvents = 100;
    
    public PlayerConnectionPattern(string playerId)
    {
        PlayerId = playerId;
        _events = new Queue<ConnectionEvent>();
    }
    
    public void AddEvent(ConnectionEvent connectionEvent)
    {
        _events.Enqueue(connectionEvent);
        
        while (_events.Count > _maxEvents)
        {
            _events.Dequeue();
        }
    }
    
    public List<ConnectionEvent> GetRecentConnections(TimeSpan timeSpan)
    {
        var cutoffTime = DateTime.UtcNow - timeSpan;
        return _events.Where(e => e.Timestamp >= cutoffTime).ToList();
    }
}

public class ConnectionEvent
{
    public string PlayerId { get; set; }
    public string TargetPlayerId { get; set; }
    public ConnectionEventType EventType { get; set; }
    public DateTime Timestamp { get; set; }
}

public enum ConnectionEventType
{
    Connected,
    Disconnected,
    Failed
}

public class ConnectionPrediction
{
    public string PlayerId { get; set; }
    public string TargetPlayerId { get; set; }
    public double Probability { get; set; }
    public DateTime PredictionTime { get; set; }
    
    public override string ToString()
    {
        return $"{PlayerId} -> {TargetPlayerId}: {Probability:P1}";
    }
}
```

### 9.3.2 사전 홀펀칭 매니저
예측된 연결에 대해 미리 홀펀칭을 수행하는 시스템입니다.

```csharp
public class ProactiveHolePunchManager
{
    private readonly ConnectionPredictor _predictor;
    private readonly OptimizedHolePuncher _holePuncher;
    private readonly ConcurrentDictionary<string, PreestablishedConnection> _preConnections;
    private readonly Timer _predictionTimer;
    private readonly SemaphoreSlim _holePunchSemaphore;
    
    public ProactiveHolePunchManager(ConnectionPredictor predictor, 
        OptimizedHolePuncher holePuncher)
    {
        _predictor = predictor;
        _holePuncher = holePuncher;
        _preConnections = new ConcurrentDictionary<string, PreestablishedConnection>();
        _holePunchSemaphore = new SemaphoreSlim(3); // 동시 홀펀칭 제한
        
        // 10초마다 예측 기반 사전 홀펀칭 수행
        _predictionTimer = new Timer(PerformProactiveHolePunching, null,
            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
    }
    
    private async void PerformProactiveHolePunching(object state)
    {
        try
        {
            var currentPlayers = GetCurrentPlayers(); // 게임에서 현재 플레이어 목록 가져오기
            
            foreach (var player in currentPlayers)
            {
                var predictions = _predictor.PredictConnections(player.Id, 
                    currentPlayers.Select(p => p.Id).ToList(), 3);
                
                foreach (var prediction in predictions)
                {
                    await TryProactiveHolePunch(player, prediction);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"사전 홀펀칭 중 오류: {ex.Message}");
        }
    }
    
    private async Task TryProactiveHolePunch(GamePlayer player, ConnectionPrediction prediction)
    {
        var connectionKey = GetConnectionKey(player.Id, prediction.TargetPlayerId);
        
        // 이미 사전 연결이 있는지 확인
        if (_preConnections.ContainsKey(connectionKey))
        {
            return;
        }
        
        // 현재 활성 연결이 있는지 확인
        if (HasActiveConnection(player.Id, prediction.TargetPlayerId))
        {
            return;
        }
        
        await _holePunchSemaphore.WaitAsync();
        
        try
        {
            Console.WriteLine($"사전 홀펀칭 시도: {prediction}");
            
            var targetPlayer = GetPlayerInfo(prediction.TargetPlayerId);
            if (targetPlayer == null) return;
            
            var endpoints = GetPlayerEndpoints(targetPlayer);
            var connection = await _holePuncher.FastHolePunchAsync(endpoints);
            
            if (connection != null)
            {
                var preConnection = new PreestablishedConnection
                {
                    Connection = connection,
                    PlayerId = player.Id,
                    TargetPlayerId = prediction.TargetPlayerId,
                    EstablishedTime = DateTime.UtcNow,
                    PredictionProbability = prediction.Probability
                };
                
                _preConnections.TryAdd(connectionKey, preConnection);
                
                Console.WriteLine($"사전 홀펀칭 성공: {connectionKey}");
                
                // 일정 시간 후 사용되지 않으면 정리
                _ = Task.Delay(TimeSpan.FromMinutes(5))
                       .ContinueWith(_ => CleanupUnusedConnection(connectionKey));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"사전 홀펀칭 실패 {connectionKey}: {ex.Message}");
        }
        finally
        {
            _holePunchSemaphore.Release();
        }
    }
    
    public UdpClient GetPreestablishedConnection(string playerId, string targetPlayerId)
    {
        var connectionKey = GetConnectionKey(playerId, targetPlayerId);
        
        if (_preConnections.TryRemove(connectionKey, out var preConnection))
        {
            Console.WriteLine($"사전 연결 사용: {connectionKey}, " +
                            $"확률: {preConnection.PredictionProbability:P1}");
            
            return preConnection.Connection;
        }
        
        return null;
    }
    
    private void CleanupUnusedConnection(string connectionKey)
    {
        if (_preConnections.TryRemove(connectionKey, out var preConnection))
        {
            preConnection.Connection?.Dispose();
            Console.WriteLine($"사용되지 않은 사전 연결 정리: {connectionKey}");
        }
    }
    
    private string GetConnectionKey(string playerId, string targetPlayerId)
    {
        var players = new[] { playerId, targetPlayerId }.OrderBy(p => p).ToArray();
        return $"{players[0]}-{players[1]}";
    }
    
    private List<GamePlayer> GetCurrentPlayers()
    {
        // 실제 구현에서는 게임 세션에서 현재 플레이어 목록을 가져옴
        return new List<GamePlayer>();
    }
    
    private GamePlayer GetPlayerInfo(string playerId)
    {
        // 실제 구현에서는 플레이어 정보를 가져옴
        return null;
    }
    
    private List<IPEndPoint> GetPlayerEndpoints(GamePlayer player)
    {
        // 실제 구현에서는 플레이어의 네트워크 엔드포인트를 가져옴
        return new List<IPEndPoint>();
    }
    
    private bool HasActiveConnection(string playerId, string targetPlayerId)
    {
        // 실제 구현에서는 현재 활성 연결이 있는지 확인
        return false;
    }
    
    public void Dispose()
    {
        _predictionTimer?.Dispose();
        
        foreach (var connection in _preConnections.Values)
        {
            connection.Connection?.Dispose();
        }
        
        _preConnections.Clear();
        _holePunchSemaphore?.Dispose();
    }
}

public class PreestablishedConnection
{
    public UdpClient Connection { get; set; }
    public string PlayerId { get; set; }
    public string TargetPlayerId { get; set; }
    public DateTime EstablishedTime { get; set; }
    public double PredictionProbability { get; set; }
}

public class GamePlayer
{
    public string Id { get; set; }
    public IPEndPoint PublicEndpoint { get; set; }
    public IPEndPoint PrivateEndpoint { get; set; }
    public DateTime LastActivity { get; set; }
}
```
  

## 9.4 지역별 STUN/TURN 서버 분산
지리적으로 분산된 STUN/TURN 서버를 효율적으로 활용하여 지연 시간을 최소화하는 방법입니다.

### 9.4.1 지역별 서버 선택 알고리즘

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

public class GeographicServerSelector
{
    private readonly List<StunTurnServer> _servers;
    private readonly Dictionary<string, ServerPerformanceMetrics> _performanceCache;
    private readonly Timer _performanceUpdateTimer;
    
    public GeographicServerSelector()
    {
        _servers = InitializeServers();
        _performanceCache = new Dictionary<string, ServerPerformanceMetrics>();
        
        // 5분마다 서버 성능 측정 업데이트
        _performanceUpdateTimer = new Timer(UpdateServerPerformance, null,
            TimeSpan.Zero, TimeSpan.FromMinutes(5));
    }
    
    private List<StunTurnServer> InitializeServers()
    {
        return new List<StunTurnServer>
        {
            // 아시아 태평양
            new StunTurnServer
            {
                Id = "ap-seoul-1",
                StunEndpoint = new IPEndPoint(IPAddress.Parse("203.0.113.10"), 3478),
                TurnEndpoint = new IPEndPoint(IPAddress.Parse("203.0.113.10"), 3479),
                Region = "ap-northeast-2",
                Location = new GeoLocation { Latitude = 37.5665, Longitude = 126.9780 }, // 서울
                MaxConcurrentConnections = 1000
            },
            new StunTurnServer
            {
                Id = "ap-tokyo-1",
                StunEndpoint = new IPEndPoint(IPAddress.Parse("203.0.113.20"), 3478),
                TurnEndpoint = new IPEndPoint(IPAddress.Parse("203.0.113.20"), 3479),
                Region = "ap-northeast-1",
                Location = new GeoLocation { Latitude = 35.6762, Longitude = 139.6503 }, // 도쿄
                MaxConcurrentConnections = 1500
            },
            
            // 북미
            new StunTurnServer
            {
                Id = "us-west-1",
                StunEndpoint = new IPEndPoint(IPAddress.Parse("198.51.100.10"), 3478),
                TurnEndpoint = new IPEndPoint(IPAddress.Parse("198.51.100.10"), 3479),
                Region = "us-west-1",
                Location = new GeoLocation { Latitude = 37.7749, Longitude = -122.4194 }, // 샌프란시스코
                MaxConcurrentConnections = 2000
            },
            new StunTurnServer
            {
                Id = "us-east-1",
                StunEndpoint = new IPEndPoint(IPAddress.Parse("198.51.100.20"), 3478),
                TurnEndpoint = new IPEndPoint(IPAddress.Parse("198.51.100.20"), 3479),
                Region = "us-east-1",
                Location = new GeoLocation { Latitude = 40.7128, Longitude = -74.0060 }, // 뉴욕
                MaxConcurrentConnections = 2500
            },
            
            // 유럽
            new StunTurnServer
            {
                Id = "eu-west-1",
                StunEndpoint = new IPEndPoint(IPAddress.Parse("192.0.2.10"), 3478),
                TurnEndpoint = new IPEndPoint(IPAddress.Parse("192.0.2.10"), 3479),
                Region = "eu-west-1",
                Location = new GeoLocation { Latitude = 51.5074, Longitude = -0.1278 }, // 런던
                MaxConcurrentConnections = 1800
            }
        };
    }
    
    public async Task<List<StunTurnServer>> SelectOptimalServers(GeoLocation clientLocation, 
        int maxServers = 3)
    {
        var candidates = new List<ServerCandidate>();
        
        foreach (var server in _servers)
        {
            if (!server.IsAvailable) continue;
            
            var distance = CalculateDistance(clientLocation, server.Location);
            var performance = GetServerPerformance(server.Id);
            var load = GetServerLoad(server.Id);
            
            var score = CalculateServerScore(distance, performance, load);
            
            candidates.Add(new ServerCandidate
            {
                Server = server,
                Distance = distance,
                Performance = performance,
                Load = load,
                Score = score
            });
        }
        
        return candidates.OrderByDescending(c => c.Score)
                        .Take(maxServers)
                        .Select(c => c.Server)
                        .ToList();
    }
    
    private double CalculateDistance(GeoLocation from, GeoLocation to)
    {
        // Haversine 공식으로 두 지점 간 거리 계산 (km)
        const double R = 6371; // 지구 반지름 (km)
        
        var lat1Rad = ToRadians(from.Latitude);
        var lat2Rad = ToRadians(to.Latitude);
        var deltaLatRad = ToRadians(to.Latitude - from.Latitude);
        var deltaLonRad = ToRadians(to.Longitude - from.Longitude);
        
        var a = Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLonRad / 2) * Math.Sin(deltaLonRad / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        
        return R * c;
    }
    
    private double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
    
    private ServerPerformanceMetrics GetServerPerformance(string serverId)
    {
        return _performanceCache.TryGetValue(serverId, out var metrics) 
            ? metrics 
            : new ServerPerformanceMetrics();
    }
    
    private double GetServerLoad(string serverId)
    {
        // 실제 구현에서는 서버 모니터링 시스템에서 로드 정보를 가져옴
        return 0.5; // 임시값
    }
    
    private double CalculateServerScore(double distance, ServerPerformanceMetrics performance, 
        double load)
    {
        // 거리 점수 (가까울수록 높음, 최대 20000km 기준)
        var distanceScore = Math.Max(0, 100 - (distance / 200));
        
        // 성능 점수
        var performanceScore = performance.AverageResponseTime > 0 
            ? Math.Max(0, 100 - performance.AverageResponseTime) 
            : 50;
        
        // 로드 점수 (낮을수록 높음)
        var loadScore = Math.Max(0, 100 - (load * 100));
        
        // 가용성 점수
        var availabilityScore = performance.AvailabilityRate * 100;
        
        // 가중 평균
        return (distanceScore * 0.3) + (performanceScore * 0.25) + 
               (loadScore * 0.25) + (availabilityScore * 0.2);
    }
    
    private async void UpdateServerPerformance(object state)
    {
        var tasks = _servers.Select(UpdateSingleServerPerformance).ToArray();
        await Task.WhenAll(tasks);
    }
    
    private async Task UpdateSingleServerPerformance(StunTurnServer server)
    {
        try
        {
            var metrics = await MeasureServerPerformance(server);
            _performanceCache[server.Id] = metrics;
            
            Console.WriteLine($"서버 {server.Id} 성능 업데이트: " +
                            $"응답시간={metrics.AverageResponseTime:F1}ms, " +
                            $"가용성={metrics.AvailabilityRate:P1}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"서버 {server.Id} 성능 측정 실패: {ex.Message}");
            
            // 실패한 경우 가용성을 낮춤
            if (_performanceCache.TryGetValue(server.Id, out var existingMetrics))
            {
                existingMetrics.FailureCount++;
                existingMetrics.RecalculateAvailability();
            }
        }
    }
    
    private async Task<ServerPerformanceMetrics> MeasureServerPerformance(StunTurnServer server)
    {
        const int testCount = 5;
        var responseTimes = new List<double>();
        var successCount = 0;
        
        for (int i = 0; i < testCount; i++)
        {
            try
            {
                var responseTime = await MeasureStunResponseTime(server.StunEndpoint);
                responseTimes.Add(responseTime);
                successCount++;
            }
            catch
            {
                // 실패한 경우 무시
            }
            
            if (i < testCount - 1)
            {
                await Task.Delay(200); // 요청 간 간격
            }
        }
        
        var avgResponseTime = responseTimes.Count > 0 ? responseTimes.Average() : double.MaxValue;
        var availabilityRate = (double)successCount / testCount;
        
        return new ServerPerformanceMetrics
        {
            AverageResponseTime = avgResponseTime,
            AvailabilityRate = availabilityRate,
            LastUpdated = DateTime.UtcNow,
            MeasurementCount = testCount,
            SuccessCount = successCount
        };
    }
    
    private async Task<double> MeasureStunResponseTime(IPEndPoint stunEndpoint)
    {
        using var udpClient = new UdpClient();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // STUN 바인딩 요청 패킷 생성 (간단한 구현)
        var stunRequest = CreateStunBindingRequest();
        
        await udpClient.SendAsync(stunRequest, stunRequest.Length, stunEndpoint);
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await udpClient.ReceiveAsync().WaitAsync(cts.Token);
        
        stopwatch.Stop();
        return stopwatch.Elapsed.TotalMilliseconds;
    }
    
    private byte[] CreateStunBindingRequest()
    {
        // 간단한 STUN 바인딩 요청 패킷
        var packet = new byte[20]; // STUN 헤더 크기
        
        // Message Type: Binding Request (0x0001)
        packet[0] = 0x00;
        packet[1] = 0x01;
        
        // Message Length: 0 (헤더만)
        packet[2] = 0x00;
        packet[3] = 0x00;
        
        // Magic Cookie
        packet[4] = 0x21;
        packet[5] = 0x12;
        packet[6] = 0xA4;
        packet[7] = 0x42;
        
        // Transaction ID (12 bytes) - 랜덤 생성
        var random = new Random();
        for (int i = 8; i < 20; i++)
        {
            packet[i] = (byte)random.Next(256);
        }
        
        return packet;
    }
    
    public void Dispose()
    {
        _performanceUpdateTimer?.Dispose();
    }
}

public class StunTurnServer
{
    public string Id { get; set; }
    public IPEndPoint StunEndpoint { get; set; }
    public IPEndPoint TurnEndpoint { get; set; }
    public string Region { get; set; }
    public GeoLocation Location { get; set; }
    public int MaxConcurrentConnections { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string Username { get; set; }
    public string Password { get; set; }
}

public class GeoLocation
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    
    public override string ToString()
    {
        return $"({Latitude:F4}, {Longitude:F4})";
    }
}

public class ServerPerformanceMetrics
{
    public double AverageResponseTime { get; set; }
    public double AvailabilityRate { get; set; }
    public DateTime LastUpdated { get; set; }
    public int MeasurementCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    
    public void RecalculateAvailability()
    {
        var totalAttempts = SuccessCount + FailureCount;
        AvailabilityRate = totalAttempts > 0 ? (double)SuccessCount / totalAttempts : 0;
    }
}

public class ServerCandidate
{
    public StunTurnServer Server { get; set; }
    public double Distance { get; set; }
    public ServerPerformanceMetrics Performance { get; set; }
    public double Load { get; set; }
    public double Score { get; set; }
    
    public override string ToString()
    {
        return $"{Server.Id}: Score={Score:F1}, Distance={Distance:F1}km, " +
               $"RT={Performance.AverageResponseTime:F1}ms, Load={Load:P1}";
    }
}
```

### 9.4.2 서버 로드 밸런싱과 장애 조치

```csharp
public class ServerLoadBalancer
{
    private readonly GeographicServerSelector _serverSelector;
    private readonly ConcurrentDictionary<string, ServerConnectionPool> _connectionPools;
    private readonly Timer _healthCheckTimer;
    private readonly object _failoverLock = new object();
    
    public event Action<StunTurnServer, string> ServerFailure;
    public event Action<StunTurnServer> ServerRecovered;
    
    public ServerLoadBalancer(GeographicServerSelector serverSelector)
    {
        _serverSelector = serverSelector;
        _connectionPools = new ConcurrentDictionary<string, ServerConnectionPool>();
        
        // 30초마다 헬스 체크
        _healthCheckTimer = new Timer(PerformHealthCheck, null,
            TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }
    
    public async Task<StunTurnServer> GetOptimalServer(GeoLocation clientLocation,
        ServerType serverType = ServerType.Stun)
    {
        var servers = await _serverSelector.SelectOptimalServers(clientLocation, 5);
        
        foreach (var server in servers)
        {
            if (!server.IsAvailable) continue;
            
            var pool = GetOrCreateConnectionPool(server.Id);
            
            if (pool.CanAcceptConnection())
            {
                pool.IncrementActiveConnections();
                return server;
            }
        }
        
        throw new InvalidOperationException("사용 가능한 서버가 없습니다.");
    }
    
    private ServerConnectionPool GetOrCreateConnectionPool(string serverId)
    {
        return _connectionPools.GetOrAdd(serverId, _ => new ServerConnectionPool(serverId));
    }
    
    public void ReportConnectionClosed(string serverId)
    {
        if (_connectionPools.TryGetValue(serverId, out var pool))
        {
            pool.DecrementActiveConnections();
        }
    }
    
    public async Task HandleServerFailure(StunTurnServer failedServer, string reason)
    {
        lock (_failoverLock)
        {
            failedServer.IsAvailable = false;
            Console.WriteLine($"서버 {failedServer.Id} 장애 발생: {reason}");
            
            ServerFailure?.Invoke(failedServer, reason);
        }
        
        // 활성 연결들을 다른 서버로 마이그레이션
        await MigrateActiveConnections(failedServer);
        
        // 복구 시도 스케줄링
        _ = Task.Delay(TimeSpan.FromMinutes(5))
               .ContinueWith(_ => AttemptServerRecovery(failedServer));
    }
    
    private async Task MigrateActiveConnections(StunTurnServer failedServer)
    {
        if (!_connectionPools.TryGetValue(failedServer.Id, out var pool))
            return;
        
        var activeConnections = pool.GetActiveConnectionCount();
        if (activeConnections == 0) return;
        
        Console.WriteLine($"서버 {failedServer.Id}의 활성 연결 {activeConnections}개 마이그레이션 시작");
        
        // 실제 구현에서는 각 활성 연결에 대해 새 서버로 재연결 시도
        // 여기서는 시뮬레이션
        for (int i = 0; i < activeConnections; i++)
        {
            try
            {
                // 클라이언트 위치 정보가 있다면 최적 서버 선택
                // 없다면 기본 위치 사용
                var fallbackLocation = new GeoLocation { Latitude = 37.5665, Longitude = 126.9780 };
                var alternativeServer = await GetOptimalServer(fallbackLocation);
                
                Console.WriteLine($"연결을 {failedServer.Id}에서 {alternativeServer.Id}로 마이그레이션");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"연결 마이그레이션 실패: {ex.Message}");
            }
        }
        
        pool.ClearActiveConnections();
    }
    
    private async Task AttemptServerRecovery(StunTurnServer server)
    {
        try
        {
            Console.WriteLine($"서버 {server.Id} 복구 시도 중...");
            
            // 서버 상태 확인
            var isHealthy = await CheckServerHealth(server);
            
            if (isHealthy)
            {
                lock (_failoverLock)
                {
                    server.IsAvailable = true;
                    Console.WriteLine($"서버 {server.Id} 복구됨");
                    
                    ServerRecovered?.Invoke(server);
                }
            }
            else
            {
                // 5분 후 다시 시도
                _ = Task.Delay(TimeSpan.FromMinutes(5))
                       .ContinueWith(_ => AttemptServerRecovery(server));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"서버 {server.Id} 복구 시도 실패: {ex.Message}");
            
            // 실패 시 다시 스케줄링
            _ = Task.Delay(TimeSpan.FromMinutes(5))
                   .ContinueWith(_ => AttemptServerRecovery(server));
        }
    }
    
    private async void PerformHealthCheck(object state)
    {
        var healthCheckTasks = _connectionPools.Keys.Select(serverId =>
            CheckServerHealthById(serverId)).ToArray();
        
        await Task.WhenAll(healthCheckTasks);
    }
    
    private async Task CheckServerHealthById(string serverId)
    {
        try
        {
            // 실제 구현에서는 서버 정보를 조회
            // 여기서는 간단히 시뮬레이션
            var server = GetServerById(serverId);
            if (server == null) return;
            
            var isHealthy = await CheckServerHealth(server);
            
            if (!isHealthy && server.IsAvailable)
            {
                await HandleServerFailure(server, "헬스 체크 실패");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"서버 {serverId} 헬스 체크 오류: {ex.Message}");
        }
    }
    
    private async Task<bool> CheckServerHealth(StunTurnServer server)
    {
        try
        {
            using var udpClient = new UdpClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            
            var testPacket = System.Text.Encoding.UTF8.GetBytes("HEALTH_CHECK");
            await udpClient.SendAsync(testPacket, testPacket.Length, server.StunEndpoint);
            
            await udpClient.ReceiveAsync().WaitAsync(cts.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private StunTurnServer GetServerById(string serverId)
    {
        // 실제 구현에서는 서버 레지스트리에서 조회
        return null;
    }
    
    public void Dispose()
    {
        _healthCheckTimer?.Dispose();
        
        foreach (var pool in _connectionPools.Values)
        {
            pool.Dispose();
        }
        
        _connectionPools.Clear();
    }
}

public class ServerConnectionPool : IDisposable
{
    public string ServerId { get; }
    private int _activeConnections;
    private readonly int _maxConnections;
    private readonly object _lock = new object();
    
    public ServerConnectionPool(string serverId, int maxConnections = 1000)
    {
        ServerId = serverId;
        _maxConnections = maxConnections;
    }
    
    public bool CanAcceptConnection()
    {
        lock (_lock)
        {
            return _activeConnections < _maxConnections;
        }
    }
    
    public void IncrementActiveConnections()
    {
        lock (_lock)
        {
            _activeConnections++;
        }
    }
    
    public void DecrementActiveConnections()
    {
        lock (_lock)
        {
            if (_activeConnections > 0)
            {
                _activeConnections--;
            }
        }
    }
    
    public int GetActiveConnectionCount()
    {
        lock (_lock)
        {
            return _activeConnections;
        }
    }
    
    public void ClearActiveConnections()
    {
        lock (_lock)
        {
            _activeConnections = 0;
        }
    }
    
    public double GetLoadPercentage()
    {
        lock (_lock)
        {
            return (double)_activeConnections / _maxConnections;
        }
    }
    
    public void Dispose()
    {
        // 정리 작업 수행
    }
}

public enum ServerType
{
    Stun,
    Turn
}
```

이제 9장의 모든 내용을 다뤘습니다. 홀펀칭 지연 시간 최적화를 위한 핵심 기법들을 살펴보았는데, 병렬 홀펀칭과 적응적 타임아웃을 통한 연결 시간 단축, 다중 경로 품질 분석과 동적 전환을 통한 최적 경로 선택, 연결 패턴 학습을 통한 예측적 홀펀칭, 그리고 지리적으로 분산된 서버 인프라를 효율적으로 활용하는 방법들을 다뤘습니다.

다음 장에서는 어떤 주제를 다루고 싶으신가요? 보안 강화, 확장성 개선, 모니터링과 디버깅, 또는 다른 주제가 있으시면 알려주세요.

  
# 10장: 대역폭과 패킷 최적화
온라인 액션 게임에서 대역폭 효율성은 게임 성능과 직결됩니다. P2P 환경에서는 각 클라이언트가 여러 피어와 동시에 통신하므로 대역폭 사용량이 급격히 증가할 수 있습니다. 이 장에서는 트래픽 패턴 분석부터 압축, 흐름 제어까지 포괄적인 최적화 기법을 다룹니다.

## 10.1 P2P 트래픽 패턴 분석
게임 데이터의 특성과 전송 패턴을 분석하여 최적화 전략을 수립하는 것이 첫 번째 단계입니다.

### 10.1.1 게임 데이터 분류와 우선순위

```csharp
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

public enum GameDataType
{
    PlayerMovement,     // 플레이어 위치/이동
    PlayerAction,       // 공격, 스킬 사용 등
    GameState,          // 게임 상태 정보
    Chat,              // 채팅 메시지
    SystemEvent,       // 시스템 이벤트
    AudioData,         // 음성 채팅
    Heartbeat          // 연결 유지
}

public enum DataPriority
{
    Critical = 0,      // 즉시 전송 필요
    High = 1,          // 높은 우선순위
    Normal = 2,        // 일반 우선순위
    Low = 3            // 낮은 우선순위
}

public class GamePacket
{
    public GameDataType DataType { get; set; }
    public DataPriority Priority { get; set; }
    public byte[] Data { get; set; }
    public DateTime Timestamp { get; set; }
    public string PlayerId { get; set; }
    public int SequenceNumber { get; set; }
    public bool RequiresAck { get; set; }
    
    public int GetEstimatedSize()
    {
        return Data?.Length ?? 0 + 32; // 헤더 크기 추가
    }
}

public class TrafficAnalyzer
{
    private readonly ConcurrentDictionary<GameDataType, DataTypeMetrics> _metrics;
    private readonly ConcurrentQueue<PacketSample> _recentPackets;
    private readonly Timer _analysisTimer;
    private readonly int _maxSamples = 10000;
    
    public TrafficAnalyzer()
    {
        _metrics = new ConcurrentDictionary<GameDataType, DataTypeMetrics>();
        _recentPackets = new ConcurrentQueue<PacketSample>();
        
        // 10초마다 트래픽 분석 수행
        _analysisTimer = new Timer(PerformAnalysis, null,
            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
    }
    
    public void RecordPacket(GamePacket packet)
    {
        var sample = new PacketSample
        {
            DataType = packet.DataType,
            Size = packet.GetEstimatedSize(),
            Timestamp = DateTime.UtcNow,
            Priority = packet.Priority
        };
        
        _recentPackets.Enqueue(sample);
        
        // 오래된 샘플 제거
        while (_recentPackets.Count > _maxSamples)
        {
            _recentPackets.TryDequeue(out _);
        }
        
        // 메트릭 업데이트
        var metrics = _metrics.GetOrAdd(packet.DataType, _ => new DataTypeMetrics());
        metrics.RecordPacket(sample);
    }
    
    private void PerformAnalysis(object state)
    {
        try
        {
            var samples = _recentPackets.ToArray();
            if (samples.Length == 0) return;
            
            var analysis = AnalyzeTrafficPatterns(samples);
            LogAnalysisResults(analysis);
            
            // 최적화 권장사항 생성
            var recommendations = GenerateOptimizationRecommendations(analysis);
            foreach (var recommendation in recommendations)
            {
                Console.WriteLine($"최적화 권장: {recommendation}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"트래픽 분석 중 오류: {ex.Message}");
        }
    }
    
    private TrafficAnalysisResult AnalyzeTrafficPatterns(PacketSample[] samples)
    {
        var now = DateTime.UtcNow;
        var recentSamples = samples.Where(s => now - s.Timestamp < TimeSpan.FromMinutes(5)).ToArray();
        
        var result = new TrafficAnalysisResult
        {
            TotalPackets = recentSamples.Length,
            TotalBytes = recentSamples.Sum(s => s.Size),
            AnalysisPeriod = TimeSpan.FromMinutes(5),
            Timestamp = now
        };
        
        // 데이터 타입별 통계
        var byType = recentSamples.GroupBy(s => s.DataType).ToDictionary(
            g => g.Key,
            g => new DataTypeStats
            {
                Count = g.Count(),
                TotalBytes = g.Sum(s => s.Size),
                AverageSize = g.Average(s => s.Size),
                Frequency = g.Count() / result.AnalysisPeriod.TotalSeconds
            });
        
        result.DataTypeStats = byType;
        
        // 우선순위별 통계
        var byPriority = recentSamples.GroupBy(s => s.Priority).ToDictionary(
            g => g.Key,
            g => new PriorityStats
            {
                Count = g.Count(),
                TotalBytes = g.Sum(s => s.Size),
                Percentage = (double)g.Count() / recentSamples.Length
            });
        
        result.PriorityStats = byPriority;
        
        // 시간대별 패턴 분석
        result.HourlyPattern = AnalyzeHourlyPattern(recentSamples);
        
        return result;
    }
    
    private Dictionary<int, double> AnalyzeHourlyPattern(PacketSample[] samples)
    {
        var hourlyTraffic = new Dictionary<int, double>();
        
        var groupedByHour = samples.GroupBy(s => s.Timestamp.Hour);
        
        foreach (var hourGroup in groupedByHour)
        {
            var bytesPerSecond = hourGroup.Sum(s => s.Size) / 3600.0; // 시간당 평균
            hourlyTraffic[hourGroup.Key] = bytesPerSecond;
        }
        
        return hourlyTraffic;
    }
    
    private List<string> GenerateOptimizationRecommendations(TrafficAnalysisResult analysis)
    {
        var recommendations = new List<string>();
        
        // 고용량 데이터 타입 식별
        var highVolumeTypes = analysis.DataTypeStats
            .Where(kvp => kvp.Value.TotalBytes > analysis.TotalBytes * 0.2)
            .OrderByDescending(kvp => kvp.Value.TotalBytes);
        
        foreach (var type in highVolumeTypes)
        {
            if (type.Key == GameDataType.PlayerMovement && type.Value.AverageSize > 100)
            {
                recommendations.Add($"플레이어 이동 데이터 압축 고려 (현재 평균: {type.Value.AverageSize:F1} bytes)");
            }
            
            if (type.Key == GameDataType.AudioData && type.Value.Frequency > 50)
            {
                recommendations.Add($"음성 데이터 비트레이트 조정 고려 (현재 빈도: {type.Value.Frequency:F1}/초)");
            }
        }
        
        // 우선순위 분포 분석
        if (analysis.PriorityStats.TryGetValue(DataPriority.Critical, out var criticalStats))
        {
            if (criticalStats.Percentage > 0.5)
            {
                recommendations.Add("Critical 우선순위 패킷 비율이 높음 - 우선순위 재검토 필요");
            }
        }
        
        // 대역폭 사용량 분석
        var bitsPerSecond = (analysis.TotalBytes * 8) / analysis.AnalysisPeriod.TotalSeconds;
        if (bitsPerSecond > 1000000) // 1Mbps 초과
        {
            recommendations.Add($"높은 대역폭 사용량 감지 ({bitsPerSecond / 1000000:F1} Mbps) - 압축 또는 샘플링 레이트 조정 고려");
        }
        
        return recommendations;
    }
    
    private void LogAnalysisResults(TrafficAnalysisResult analysis)
    {
        Console.WriteLine($"\n=== 트래픽 분석 결과 ({analysis.Timestamp:yyyy-MM-dd HH:mm:ss}) ===");
        Console.WriteLine($"총 패킷: {analysis.TotalPackets:N0}");
        Console.WriteLine($"총 데이터: {analysis.TotalBytes / 1024.0:F1} KB");
        Console.WriteLine($"평균 처리량: {(analysis.TotalBytes * 8) / analysis.AnalysisPeriod.TotalSeconds / 1000:F1} Kbps");
        
        Console.WriteLine("\n데이터 타입별 통계:");
        foreach (var stat in analysis.DataTypeStats.OrderByDescending(kvp => kvp.Value.TotalBytes))
        {
            var percentage = (double)stat.Value.TotalBytes / analysis.TotalBytes * 100;
            Console.WriteLine($"  {stat.Key}: {stat.Value.Count:N0}개, " +
                            $"{stat.Value.TotalBytes / 1024.0:F1} KB ({percentage:F1}%), " +
                            $"평균 크기: {stat.Value.AverageSize:F1} bytes");
        }
    }
    
    public DataTypeMetrics GetMetrics(GameDataType dataType)
    {
        return _metrics.TryGetValue(dataType, out var metrics) ? metrics : new DataTypeMetrics();
    }
    
    public void Dispose()
    {
        _analysisTimer?.Dispose();
    }
}

public class PacketSample
{
    public GameDataType DataType { get; set; }
    public int Size { get; set; }
    public DateTime Timestamp { get; set; }
    public DataPriority Priority { get; set; }
}

public class DataTypeMetrics
{
    private readonly Queue<PacketSample> _recentSamples = new Queue<PacketSample>();
    private readonly object _lock = new object();
    private readonly int _maxSamples = 1000;
    
    public void RecordPacket(PacketSample sample)
    {
        lock (_lock)
        {
            _recentSamples.Enqueue(sample);
            
            while (_recentSamples.Count > _maxSamples)
            {
                _recentSamples.Dequeue();
            }
        }
    }
    
    public double GetAverageSize()
    {
        lock (_lock)
        {
            return _recentSamples.Count > 0 ? _recentSamples.Average(s => s.Size) : 0;
        }
    }
    
    public double GetFrequency(TimeSpan period)
    {
        lock (_lock)
        {
            var cutoff = DateTime.UtcNow - period;
            var recentCount = _recentSamples.Count(s => s.Timestamp >= cutoff);
            return recentCount / period.TotalSeconds;
        }
    }
}

public class TrafficAnalysisResult
{
    public int TotalPackets { get; set; }
    public long TotalBytes { get; set; }
    public TimeSpan AnalysisPeriod { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<GameDataType, DataTypeStats> DataTypeStats { get; set; }
    public Dictionary<DataPriority, PriorityStats> PriorityStats { get; set; }
    public Dictionary<int, double> HourlyPattern { get; set; }
}

public class DataTypeStats
{
    public int Count { get; set; }
    public long TotalBytes { get; set; }
    public double AverageSize { get; set; }
    public double Frequency { get; set; }
}

public class PriorityStats
{
    public int Count { get; set; }
    public long TotalBytes { get; set; }
    public double Percentage { get; set; }
}
```

### 10.1.2 실시간 대역폭 모니터링

```csharp
public class BandwidthMonitor
{
    private readonly ConcurrentDictionary<string, PeerBandwidthStats> _peerStats;
    private readonly Timer _monitoringTimer;
    private long _totalBytesReceived;
    private long _totalBytesSent;
    private DateTime _lastResetTime;
    
    public event Action<BandwidthAlert> BandwidthAlertRaised;
    
    public BandwidthMonitor()
    {
        _peerStats = new ConcurrentDictionary<string, PeerBandwidthStats>();
        _lastResetTime = DateTime.UtcNow;
        
        // 1초마다 대역폭 계산
        _monitoringTimer = new Timer(CalculateBandwidth, null,
            TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }
    
    public void RecordSentData(string peerId, int bytes)
    {
        Interlocked.Add(ref _totalBytesSent, bytes);
        
        var stats = _peerStats.GetOrAdd(peerId, _ => new PeerBandwidthStats(peerId));
        stats.RecordSentData(bytes);
    }
    
    public void RecordReceivedData(string peerId, int bytes)
    {
        Interlocked.Add(ref _totalBytesReceived, bytes);
        
        var stats = _peerStats.GetOrAdd(peerId, _ => new PeerBandwidthStats(peerId));
        stats.RecordReceivedData(bytes);
    }
    
    private void CalculateBandwidth(object state)
    {
        var now = DateTime.UtcNow;
        var elapsed = now - _lastResetTime;
        
        if (elapsed.TotalSeconds < 1) return;
        
        var totalSent = Interlocked.Exchange(ref _totalBytesSent, 0);
        var totalReceived = Interlocked.Exchange(ref _totalBytesReceived, 0);
        
        var sendBandwidth = (totalSent * 8) / elapsed.TotalSeconds; // bps
        var receiveBandwidth = (totalReceived * 8) / elapsed.TotalSeconds; // bps
        
        // 피어별 대역폭 계산
        foreach (var peerStats in _peerStats.Values)
        {
            peerStats.CalculateBandwidth(elapsed);
        }
        
        // 전체 대역폭 로깅
        LogBandwidthStats(sendBandwidth, receiveBandwidth);
        
        // 대역폭 경고 체크
        CheckBandwidthAlerts(sendBandwidth, receiveBandwidth);
        
        _lastResetTime = now;
    }
    
    private void LogBandwidthStats(double sendBps, double receiveBps)
    {
        var sendKbps = sendBps / 1000;
        var receiveKbps = receiveBps / 1000;
        
        Console.WriteLine($"대역폭 - 업로드: {sendKbps:F1} Kbps, 다운로드: {receiveKbps:F1} Kbps");
        
        // 높은 사용량의 피어 표시
        var highUsagePeers = _peerStats.Values
            .Where(p => p.GetTotalBandwidth() > 100000) // 100 Kbps 초과
            .OrderByDescending(p => p.GetTotalBandwidth())
            .Take(5);
        
        foreach (var peer in highUsagePeers)
        {
            Console.WriteLine($"  피어 {peer.PeerId}: {peer.GetTotalBandwidth() / 1000:F1} Kbps " +
                            $"(업로드: {peer.SendBandwidth / 1000:F1}, 다운로드: {peer.ReceiveBandwidth / 1000:F1})");
        }
    }
    
    private void CheckBandwidthAlerts(double sendBps, double receiveBps)
    {
        const double criticalThreshold = 5000000; // 5 Mbps
        const double warningThreshold = 3000000;  // 3 Mbps
        
        if (sendBps > criticalThreshold)
        {
            RaiseAlert(BandwidthAlertType.UploadCritical, sendBps, criticalThreshold);
        }
        else if (sendBps > warningThreshold)
        {
            RaiseAlert(BandwidthAlertType.UploadWarning, sendBps, warningThreshold);
        }
        
        if (receiveBps > criticalThreshold)
        {
            RaiseAlert(BandwidthAlertType.DownloadCritical, receiveBps, criticalThreshold);
        }
        else if (receiveBps > warningThreshold)
        {
            RaiseAlert(BandwidthAlertType.DownloadWarning, receiveBps, warningThreshold);
        }
    }
    
    private void RaiseAlert(BandwidthAlertType alertType, double currentBps, double threshold)
    {
        var alert = new BandwidthAlert
        {
            AlertType = alertType,
            CurrentBandwidth = currentBps,
            Threshold = threshold,
            Timestamp = DateTime.UtcNow,
            Message = GenerateAlertMessage(alertType, currentBps, threshold)
        };
        
        BandwidthAlertRaised?.Invoke(alert);
    }
    
    private string GenerateAlertMessage(BandwidthAlertType alertType, double currentBps, double threshold)
    {
        var current = currentBps / 1000000; // Mbps
        var limit = threshold / 1000000;    // Mbps
        
        return alertType switch
        {
            BandwidthAlertType.UploadCritical => $"업로드 대역폭 위험 수준: {current:F1} Mbps (한계: {limit:F1} Mbps)",
            BandwidthAlertType.UploadWarning => $"업로드 대역폭 경고: {current:F1} Mbps (경고: {limit:F1} Mbps)",
            BandwidthAlertType.DownloadCritical => $"다운로드 대역폭 위험 수준: {current:F1} Mbps (한계: {limit:F1} Mbps)",
            BandwidthAlertType.DownloadWarning => $"다운로드 대역폭 경고: {current:F1} Mbps (경고: {limit:F1} Mbps)",
            _ => $"대역폭 경고: {current:F1} Mbps"
        };
    }
    
    public BandwidthSummary GetCurrentSummary()
    {
        var totalSendBandwidth = _peerStats.Values.Sum(p => p.SendBandwidth);
        var totalReceiveBandwidth = _peerStats.Values.Sum(p => p.ReceiveBandwidth);
        
        return new BandwidthSummary
        {
            TotalSendBandwidth = totalSendBandwidth,
            TotalReceiveBandwidth = totalReceiveBandwidth,
            ActivePeerCount = _peerStats.Count,
            TopPeers = _peerStats.Values
                .OrderByDescending(p => p.GetTotalBandwidth())
                .Take(10)
                .ToList(),
            Timestamp = DateTime.UtcNow
        };
    }
    
    public void Dispose()
    {
        _monitoringTimer?.Dispose();
    }
}

public class PeerBandwidthStats
{
    public string PeerId { get; }
    public double SendBandwidth { get; private set; }
    public double ReceiveBandwidth { get; private set; }
    
    private long _bytesSent;
    private long _bytesReceived;
    private readonly object _lock = new object();
    
    public PeerBandwidthStats(string peerId)
    {
        PeerId = peerId;
    }
    
    public void RecordSentData(int bytes)
    {
        Interlocked.Add(ref _bytesSent, bytes);
    }
    
    public void RecordReceivedData(int bytes)
    {
        Interlocked.Add(ref _bytesReceived, bytes);
    }
    
    public void CalculateBandwidth(TimeSpan elapsed)
    {
        lock (_lock)
        {
            var sent = Interlocked.Exchange(ref _bytesSent, 0);
            var received = Interlocked.Exchange(ref _bytesReceived, 0);
            
            SendBandwidth = (sent * 8) / elapsed.TotalSeconds;
            ReceiveBandwidth = (received * 8) / elapsed.TotalSeconds;
        }
    }
    
    public double GetTotalBandwidth()
    {
        return SendBandwidth + ReceiveBandwidth;
    }
}

public enum BandwidthAlertType
{
    UploadWarning,
    UploadCritical,
    DownloadWarning,
    DownloadCritical
}

public class BandwidthAlert
{
    public BandwidthAlertType AlertType { get; set; }
    public double CurrentBandwidth { get; set; }
    public double Threshold { get; set; }
    public DateTime Timestamp { get; set; }
    public string Message { get; set; }
}

public class BandwidthSummary
{
    public double TotalSendBandwidth { get; set; }
    public double TotalReceiveBandwidth { get; set; }
    public int ActivePeerCount { get; set; }
    public List<PeerBandwidthStats> TopPeers { get; set; }
    public DateTime Timestamp { get; set; }
    
    public override string ToString()
    {
        return $"대역폭 요약: 업로드 {TotalSendBandwidth / 1000:F1} Kbps, " +
               $"다운로드 {TotalReceiveBandwidth / 1000:F1} Kbps, 활성 피어: {ActivePeerCount}";
    }
}
```

## 10.2 적응형 비트레이트와 QoS

네트워크 상태에 따라 동적으로 데이터 전송량과 품질을 조정하는 시스템입니다.

### 10.2.1 적응형 비트레이트 제어

```csharp
public class AdaptiveBitrateController
{
    private readonly BandwidthMonitor _bandwidthMonitor;
    private readonly Dictionary<string, PeerQualitySettings> _peerSettings;
    private readonly Timer _adjustmentTimer;
    private readonly object _settingsLock = new object();
    
    public event Action<string, QualityLevel, QualityLevel> QualityChanged;
    
    public AdaptiveBitrateController(BandwidthMonitor bandwidthMonitor)
    {
        _bandwidthMonitor = bandwidthMonitor;
        _peerSettings = new Dictionary<string, PeerQualitySettings>();
        
        // 5초마다 품질 조정
        _adjustmentTimer = new Timer(AdjustQuality, null,
            TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }
    
    public void RegisterPeer(string peerId, QualityLevel initialQuality = QualityLevel.Medium)
    {
        lock (_settingsLock)
        {
            if (!_peerSettings.ContainsKey(peerId))
            {
                _peerSettings[peerId] = new PeerQualitySettings
                {
                    PeerId = peerId,
                    CurrentQuality = initialQuality,
                    TargetBitrate = GetBitrateForQuality(initialQuality),
                    LastAdjustment = DateTime.UtcNow
                };
                
                Console.WriteLine($"피어 {peerId} 등록됨 - 초기 품질: {initialQuality}");
            }
        }
    }
    
    public void UnregisterPeer(string peerId)
    {
        lock (_settingsLock)
        {
            if (_peerSettings.Remove(peerId))
            {
                Console.WriteLine($"피어 {peerId} 등록 해제됨");
            }
        }
    }
    
    private void AdjustQuality(object state)
    {
        try
        {
            var summary = _bandwidthMonitor.GetCurrentSummary();
            
            lock (_settingsLock)
            {
                foreach (var settings in _peerSettings.Values.ToList())
                {
                    var newQuality = DetermineOptimalQuality(settings, summary);
                    
                    if (newQuality != settings.CurrentQuality)
                    {
                        var oldQuality = settings.CurrentQuality;
                        settings.CurrentQuality = newQuality;
                        settings.TargetBitrate = GetBitrateForQuality(newQuality);
                        settings.LastAdjustment = DateTime.UtcNow;
                        
                        Console.WriteLine($"피어 {settings.PeerId} 품질 조정: {oldQuality} -> {newQuality}");
                        QualityChanged?.Invoke(settings.PeerId, oldQuality, newQuality);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"품질 조정 중 오류: {ex.Message}");
        }
    }
    
    private QualityLevel DetermineOptimalQuality(PeerQualitySettings settings, BandwidthSummary summary)
    {
        // 현재 피어의 대역폭 사용량 확인
        var peerStats = summary.TopPeers.FirstOrDefault(p => p.PeerId == settings.PeerId);
        if (peerStats == null) return settings.CurrentQuality;
        
        var currentUsage = peerStats.GetTotalBandwidth();
        var availableBandwidth = EstimateAvailableBandwidth(summary);
        
        // 네트워크 품질 메트릭
        var networkQuality = CalculateNetworkQuality(currentUsage, availableBandwidth);
        
        // 조정 빈도 제한 (최소 10초 간격)
        var timeSinceLastAdjustment = DateTime.UtcNow - settings.LastAdjustment;
        if (timeSinceLastAdjustment < TimeSpan.FromSeconds(10))
        {
            return settings.CurrentQuality;
        }
        
        // 품질 레벨 결정
        if (networkQuality > 0.8)
        {
            return QualityLevel.High;
        }
        else if (networkQuality > 0.5)
        {
            return QualityLevel.Medium;
        }
        else if (networkQuality > 0.2)
        {
            return QualityLevel.Low;
        }
        else
        {
            return QualityLevel.Minimal;
        }
    }
    
    private double EstimateAvailableBandwidth(BandwidthSummary summary)
    {
        // 총 대역폭에서 현재 사용량을 빼서 사용 가능한 대역폭 추정
        const double maxBandwidth = 10000000; // 10 Mbps 가정
        var currentUsage = summary.TotalSendBandwidth + summary.TotalReceiveBandwidth;
        
        return Math.Max(0, maxBandwidth - currentUsage);
    }
    
    private double CalculateNetworkQuality(double currentUsage, double availableBandwidth)
    {
        if (availableBandwidth <= 0) return 0;
        
        var utilizationRatio = currentUsage / (currentUsage + availableBandwidth);
        return 1.0 - utilizationRatio;
    }
    
    private int GetBitrateForQuality(QualityLevel quality)
    {
        return quality switch
        {
            QualityLevel.Minimal => 32000,   // 32 Kbps
            QualityLevel.Low => 128000,      // 128 Kbps
            QualityLevel.Medium => 512000,   // 512 Kbps
            QualityLevel.High => 1024000,    // 1 Mbps
            QualityLevel.Ultra => 2048000,   // 2 Mbps
            _ => 512000
        };
    }
    
    public GamePacket ProcessOutgoingPacket(string peerId, GamePacket packet)
    {
        lock (_settingsLock)
        {
            if (!_peerSettings.TryGetValue(peerId, out var settings))
            {
                return packet; // 설정이 없으면 원본 반환
            }
            
            return ApplyQualitySettings(packet, settings);
        }
    }
    
    private GamePacket ApplyQualitySettings(GamePacket packet, PeerQualitySettings settings)
    {
        switch (packet.DataType)
        {
            case GameDataType.PlayerMovement:
                return ApplyMovementQuality(packet, settings);
                
            case GameDataType.AudioData:
                return ApplyAudioQuality(packet, settings);
                
            case GameDataType.PlayerAction:
                // 액션 데이터는 품질을 낮추지 않음 (게임플레이 중요)
                return packet;
                
            default:
                return packet;
        }
    }
    
    private GamePacket ApplyMovementQuality(GamePacket packet, PeerQualitySettings settings)
    {
        // 품질에 따라 위치 정보 정밀도 조정
        var precision = settings.CurrentQuality switch
        {
            QualityLevel.Minimal => 0.5f,   // 0.5m 정밀도
            QualityLevel.Low => 0.1f,       // 0.1m 정밀도
            QualityLevel.Medium => 0.05f,   // 0.05m 정밀도
            QualityLevel.High => 0.01f,     // 0.01m 정밀도
            QualityLevel.Ultra => 0.001f,   // 0.001m 정밀도
            _ => 0.1f
        };
        
        // 실제 구현에서는 패킷 데이터를 파싱하고 정밀도를 적용
        // 여기서는 시뮬레이션
        return packet;
    }
    
    private GamePacket ApplyAudioQuality(GamePacket packet, PeerQualitySettings settings)
    {
        // 품질에 따라 오디오 비트레이트 조정
        var targetBitrate = settings.CurrentQuality switch
        {
            QualityLevel.Minimal => 16000,   // 16 Kbps
            QualityLevel.Low => 32000,       // 32 Kbps
            QualityLevel.Medium => 64000,    // 64 Kbps
            QualityLevel.High => 128000,     // 128 Kbps
            QualityLevel.Ultra => 256000,    // 256 Kbps
            _ => 64000
        };
        
        // 실제 구현에서는 오디오 데이터를 리샘플링하거나 압축률 조정
        return packet;
    }
    
    public QualityLevel GetCurrentQuality(string peerId)
    {
        lock (_settingsLock)
        {
            return _peerSettings.TryGetValue(peerId, out var settings) 
                ? settings.CurrentQuality 
                : QualityLevel.Medium;
        }
    }
    
    public void Dispose()
    {
        _adjustmentTimer?.Dispose();
    }
}

public enum QualityLevel
{
    Minimal = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Ultra = 4
}

public class PeerQualitySettings
{
    public string PeerId { get; set; }
    public QualityLevel CurrentQuality { get; set; }
    public int TargetBitrate { get; set; }
    public DateTime LastAdjustment { get; set; }
    public double PacketLossRate { get; set; }
    public double AverageLatency { get; set; }
}
```

### 10.2.2 QoS 기반 패킷 스케줄링

```csharp
public class QoSPacketScheduler
{
    private readonly PriorityQueue<ScheduledPacket, PacketPriority> _packetQueue;
    private readonly Timer _transmissionTimer;
    private readonly SemaphoreSlim _transmissionSemaphore;
    private readonly AdaptiveBitrateController _bitrateController;
    private readonly object _queueLock = new object();
    
    private int _maxPacketsPerInterval = 100;
    private TimeSpan _transmissionInterval = TimeSpan.FromMilliseconds(10);
    
    public QoSPacketScheduler(AdaptiveBitrateController bitrateController)
    {
        _packetQueue = new PriorityQueue<ScheduledPacket, PacketPriority>();
        _bitrateController = bitrateController;
        _transmissionSemaphore = new SemaphoreSlim(1, 1);
        
        // 고정 간격으로 패킷 전송
        _transmissionTimer = new Timer(ProcessTransmissionQueue, null,
            _transmissionInterval, _transmissionInterval);
    }
    
    public async Task SchedulePacket(string peerId, GamePacket packet, 
        Func<GamePacket, Task> transmissionCallback)
    {
        var priority = CalculatePacketPriority(packet);
        var deadline = CalculateDeadline(packet);
        
        var scheduledPacket = new ScheduledPacket
        {
            Packet = packet,
            PeerId = peerId,
            Priority = priority,
            Deadline = deadline,
            ScheduledTime = DateTime.UtcNow,
            TransmissionCallback = transmissionCallback
        };
        
        lock (_queueLock)
        {
            _packetQueue.Enqueue(scheduledPacket, priority);
        }
        
        // 큐 크기 모니터링
        if (_packetQueue.Count > 1000)
        {
            Console.WriteLine($"경고: 패킷 큐 크기가 큼 ({_packetQueue.Count})");
            await DropLowPriorityPackets();
        }
    }
    
    private PacketPriority CalculatePacketPriority(GamePacket packet)
    {
        var basePriority = GetBasePriority(packet.DataType, packet.Priority);
        
        // 시간 민감도 고려
        var urgencyMultiplier = CalculateUrgencyMultiplier(packet);
        
        // 최종 우선순위 계산
        var finalPriority = (int)basePriority + urgencyMultiplier;
        
        return (PacketPriority)Math.Min((int)PacketPriority.Critical, 
                                       Math.Max((int)PacketPriority.Bulk, finalPriority));
    }
    
    private int GetBasePriority(GameDataType dataType, DataPriority priority)
    {
        var typeScore = dataType switch
        {
            GameDataType.PlayerAction => 100,      // 가장 높은 우선순위
            GameDataType.PlayerMovement => 80,
            GameDataType.GameState => 60,
            GameDataType.SystemEvent => 40,
            GameDataType.Chat => 20,
            GameDataType.Heartbeat => 10,
            GameDataType.AudioData => 30,
            _ => 50
        };
        
        var priorityScore = priority switch
        {
            DataPriority.Critical => 50,
            DataPriority.High => 30,
            DataPriority.Normal => 0,
            DataPriority.Low => -20,
            _ => 0
        };
        
        return typeScore + priorityScore;
    }
    
    private int CalculateUrgencyMultiplier(GamePacket packet)
    {
        var age = DateTime.UtcNow - packet.Timestamp;
        
        // 패킷이 오래될수록 우선순위 감소
        if (age > TimeSpan.FromSeconds(1))
        {
            return -20; // 매우 오래된 패킷
        }
        else if (age > TimeSpan.FromMilliseconds(500))
        {
            return -10; // 오래된 패킷
        }
        else if (age < TimeSpan.FromMilliseconds(50))
        {
            return 10; // 신선한 패킷
        }
        
        return 0; // 일반적인 패킷
    }
    
    private DateTime CalculateDeadline(GamePacket packet)
    {
        var deadlineMs = packet.DataType switch
        {
            GameDataType.PlayerAction => 100,      // 100ms 내 전송 필요
            GameDataType.PlayerMovement => 200,    // 200ms 내 전송 필요
            GameDataType.GameState => 500,         // 500ms 내 전송 필요
            GameDataType.Chat => 2000,             // 2초 내 전송 필요
            GameDataType.AudioData => 150,         // 150ms 내 전송 필요
            GameDataType.Heartbeat => 5000,        // 5초 내 전송 필요
            _ => 1000
        };
        
        return packet.Timestamp.AddMilliseconds(deadlineMs);
    }
    
    private async void ProcessTransmissionQueue(object state)
    {
        if (!await _transmissionSemaphore.WaitAsync(10))
        {
            return; // 이전 처리가 아직 진행 중
        }
        
        try
        {
            var packetsToSend = new List<ScheduledPacket>();
            var now = DateTime.UtcNow;
            
            lock (_queueLock)
            {
                // 만료된 패킷 제거
                var expiredPackets = new List<ScheduledPacket>();
                var tempQueue = new List<ScheduledPacket>();
                
                while (_packetQueue.Count > 0)
                {
                    if (_packetQueue.TryDequeue(out var packet, out var priority))
                    {
                        if (now > packet.Deadline)
                        {
                            expiredPackets.Add(packet);
                        }
                        else
                        {
                            tempQueue.Add(packet);
                        }
                    }
                }
                
                // 만료되지 않은 패킷들을 다시 큐에 넣기
                foreach (var packet in tempQueue)
                {
                    _packetQueue.Enqueue(packet, packet.Priority);
                }
                
                // 전송할 패킷 선별
                var transmitted = 0;
                while (_packetQueue.Count > 0 && transmitted < _maxPacketsPerInterval)
                {
                    if (_packetQueue.TryDequeue(out var packet, out var priority))
                    {
                        // 비트레이트 제한 확인
                        if (CanTransmitPacket(packet))
                        {
                            packetsToSend.Add(packet);
                            transmitted++;
                        }
                        else
                        {
                            // 대역폭 부족으로 다시 큐에 넣기
                            _packetQueue.Enqueue(packet, priority);
                            break;
                        }
                    }
                }
                
                if (expiredPackets.Count > 0)
                {
                    Console.WriteLine($"만료된 패킷 {expiredPackets.Count}개 드롭됨");
                }
            }
            
            // 실제 전송 (큐 락 외부에서)
            var transmissionTasks = packetsToSend.Select(TransmitPacket);
            await Task.WhenAll(transmissionTasks);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"패킷 전송 처리 중 오류: {ex.Message}");
        }
        finally
        {
            _transmissionSemaphore.Release();
        }
    }
    
    private bool CanTransmitPacket(ScheduledPacket packet)
    {
        // 피어별 품질 설정에 따른 전송 가능 여부 확인
        var currentQuality = _bitrateController.GetCurrentQuality(packet.PeerId);
        var packetSize = packet.Packet.GetEstimatedSize();
        
        // 간단한 대역폭 체크 (실제로는 더 정교한 로직 필요)
        var allowedSize = currentQuality switch
        {
            QualityLevel.Minimal => 1000,   // 1KB
            QualityLevel.Low => 2000,       // 2KB
            QualityLevel.Medium => 4000,    // 4KB
            QualityLevel.High => 8000,      // 8KB
            QualityLevel.Ultra => 16000,    // 16KB
            _ => 4000
        };
        
        return packetSize <= allowedSize;
    }
    
    private async Task TransmitPacket(ScheduledPacket scheduledPacket)
    {
        try
        {
            // 품질 설정 적용
            var processedPacket = _bitrateController.ProcessOutgoingPacket(
                scheduledPacket.PeerId, scheduledPacket.Packet);
            
            // 실제 전송
            await scheduledPacket.TransmissionCallback(processedPacket);
            
            // 통계 업데이트
            var transmissionDelay = DateTime.UtcNow - scheduledPacket.ScheduledTime;
            if (transmissionDelay > TimeSpan.FromMilliseconds(100))
            {
                Console.WriteLine($"패킷 전송 지연: {transmissionDelay.TotalMilliseconds:F1}ms");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"패킷 전송 실패: {ex.Message}");
        }
    }
    
    private async Task DropLowPriorityPackets()
    {
        lock (_queueLock)
        {
            var packetsToKeep = new List<(ScheduledPacket packet, PacketPriority priority)>();
            var droppedCount = 0;
            
            while (_packetQueue.Count > 0)
            {
                if (_packetQueue.TryDequeue(out var packet, out var priority))
                {
                    if (priority <= PacketPriority.Low && droppedCount < 100)
                    {
                        droppedCount++;
                        continue; // 드롭
                    }
                    
                    packetsToKeep.Add((packet, priority));
                }
            }
            
            // 유지할 패킷들을 다시 큐에 넣기
            foreach (var (packet, priority) in packetsToKeep)
            {
                _packetQueue.Enqueue(packet, priority);
            }
            
            Console.WriteLine($"큐 정리: {droppedCount}개 패킷 드롭됨");
        }
    }
    
    public void AdjustTransmissionRate(int packetsPerSecond)
    {
        _maxPacketsPerInterval = Math.Max(1, packetsPerSecond / 100); // 10ms 간격 기준
        Console.WriteLine($"전송률 조정: {packetsPerSecond} 패킷/초");
    }
    
    public int GetQueueSize()
    {
        lock (_queueLock)
        {
            return _packetQueue.Count;
        }
    }
    
    public void Dispose()
    {
        _transmissionTimer?.Dispose();
        _transmissionSemaphore?.Dispose();
    }
}

public enum PacketPriority
{
    Critical = 200,
    High = 150,
    Normal = 100,
    Low = 50,
    Bulk = 0
}

public class ScheduledPacket
{
    public GamePacket Packet { get; set; }
    public string PeerId { get; set; }
    public PacketPriority Priority { get; set; }
    public DateTime Deadline { get; set; }
    public DateTime ScheduledTime { get; set; }
    public Func<GamePacket, Task> TransmissionCallback { get; set; }
}
```

## 10.3 패킷 압축과 델타 압축
데이터 크기를 최소화하여 대역폭 사용량을 줄이는 압축 기법들입니다.

### 10.3.1 범용 패킷 압축

```csharp
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

public class PacketCompressor
{
    private readonly Dictionary<GameDataType, ICompressionStrategy> _strategies;
    private readonly CompressionStats _stats;
    
    public PacketCompressor()
    {
        _strategies = new Dictionary<GameDataType, ICompressionStrategy>
        {
            { GameDataType.PlayerMovement, new MovementCompressionStrategy() },
            { GameDataType.GameState, new StateCompressionStrategy() },
            { GameDataType.Chat, new TextCompressionStrategy() },
            { GameDataType.AudioData, new AudioCompressionStrategy() },
            { GameDataType.SystemEvent, new GenericCompressionStrategy() },
            { GameDataType.Heartbeat, new NoCompressionStrategy() }
        };
        
        _stats = new CompressionStats();
    }
    
    public CompressedPacket Compress(GamePacket packet)
    {
        var originalSize = packet.Data?.Length ?? 0;
        if (originalSize == 0)
        {
            return new CompressedPacket
            {
                OriginalPacket = packet,
                CompressedData = packet.Data,
                CompressionType = CompressionType.None,
                OriginalSize = 0,
                CompressedSize = 0
            };
        }
        
        var strategy = _strategies.TryGetValue(packet.DataType, out var s) 
            ? s 
            : _strategies[GameDataType.SystemEvent];
        
        try
        {
            var compressed = strategy.Compress(packet.Data);
            var compressedSize = compressed?.Length ?? originalSize;
            
            // 압축 효과가 없으면 원본 사용
            if (compressedSize >= originalSize * 0.9) // 10% 이상 압축되지 않으면
            {
                compressed = packet.Data;
                compressedSize = originalSize;
                
                var result = new CompressedPacket
                {
                    OriginalPacket = packet,
                    CompressedData = compressed,
                    CompressionType = CompressionType.None,
                    OriginalSize = originalSize,
                    CompressedSize = compressedSize
                };
                
                _stats.RecordCompression(packet.DataType, originalSize, compressedSize, false);
                return result;
            }
            else
            {
                var result = new CompressedPacket
                {
                    OriginalPacket = packet,
                    CompressedData = compressed,
                    CompressionType = strategy.CompressionType,
                    OriginalSize = originalSize,
                    CompressedSize = compressedSize
                };
                
                _stats.RecordCompression(packet.DataType, originalSize, compressedSize, true);
                return result;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"압축 실패 ({packet.DataType}): {ex.Message}");
            
            return new CompressedPacket
            {
                OriginalPacket = packet,
                CompressedData = packet.Data,
                CompressionType = CompressionType.None,
                OriginalSize = originalSize,
                CompressedSize = originalSize
            };
        }
    }
    
    public GamePacket Decompress(CompressedPacket compressedPacket)
    {
        if (compressedPacket.CompressionType == CompressionType.None)
        {
            return compressedPacket.OriginalPacket;
        }
        
        var strategy = _strategies.Values.FirstOrDefault(s => 
            s.CompressionType == compressedPacket.CompressionType);
        
        if (strategy == null)
        {
            throw new InvalidOperationException($"압축 해제 전략을 찾을 수 없음: {compressedPacket.CompressionType}");
        }
        
        try
        {
            var decompressed = strategy.Decompress(compressedPacket.CompressedData);
            
            var packet = compressedPacket.OriginalPacket;
            packet.Data = decompressed;
            
            return packet;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"압축 해제 실패 ({compressedPacket.CompressionType}): {ex.Message}");
            throw;
        }
    }
    
    public CompressionStats GetStats()
    {
        return _stats.GetSnapshot();
    }
}

public interface ICompressionStrategy
{
    CompressionType CompressionType { get; }
    byte[] Compress(byte[] data);
    byte[] Decompress(byte[] compressedData);
}

public class GenericCompressionStrategy : ICompressionStrategy
{
    public CompressionType CompressionType => CompressionType.Gzip;
    
    public byte[] Compress(byte[] data)
    {
        using var memoryStream = new MemoryStream();
        using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
        {
            gzipStream.Write(data, 0, data.Length);
        }
        return memoryStream.ToArray();
    }
    
    public byte[] Decompress(byte[] compressedData)
    {
        using var compressedStream = new MemoryStream(compressedData);
        using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
        using var resultStream = new MemoryStream();
        
        gzipStream.CopyTo(resultStream);
        return resultStream.ToArray();
    }
}

public class TextCompressionStrategy : ICompressionStrategy
{
    public CompressionType CompressionType => CompressionType.Text;
    
    public byte[] Compress(byte[] data)
    {
        // 텍스트 데이터에 특화된 압축 (사전 기반)
        var text = Encoding.UTF8.GetString(data);
        var compressed = CompressText(text);
        return Encoding.UTF8.GetBytes(compressed);
    }
    
    public byte[] Decompress(byte[] compressedData)
    {
        var compressedText = Encoding.UTF8.GetString(compressedData);
        var decompressed = DecompressText(compressedText);
        return Encoding.UTF8.GetBytes(decompressed);
    }
    
    private string CompressText(string text)
    {
        // 간단한 사전 기반 압축 (실제로는 더 정교한 알고리즘 사용)
        var dictionary = new Dictionary<string, string>
        {
            { "player", "p" },
            { "position", "pos" },
            { "health", "hp" },
            { "damage", "dmg" },
            { "attack", "atk" },
            { "defend", "def" }
        };
        
        foreach (var kvp in dictionary)
        {
            text = text.Replace(kvp.Key, kvp.Value);
        }
        
        return text;
    }
    
    private string DecompressText(string compressedText)
    {
        var dictionary = new Dictionary<string, string>
        {
            { "p", "player" },
            { "pos", "position" },
            { "hp", "health" },
            { "dmg", "damage" },
            { "atk", "attack" },
            { "def", "defend" }
        };
        
        foreach (var kvp in dictionary)
        {
            compressedText = compressedText.Replace(kvp.Key, kvp.Value);
        }
        
        return compressedText;
    }
}

public class NoCompressionStrategy : ICompressionStrategy
{
    public CompressionType CompressionType => CompressionType.None;
    
    public byte[] Compress(byte[] data) => data;
    public byte[] Decompress(byte[] compressedData) => compressedData;
}

public enum CompressionType
{
    None,
    Gzip,
    Text,
    Movement,
    State,
    Audio
}

public class CompressedPacket
{
    public GamePacket OriginalPacket { get; set; }
    public byte[] CompressedData { get; set; }
    public CompressionType CompressionType { get; set; }
    public int OriginalSize { get; set; }
    public int CompressedSize { get; set; }
    
    public double CompressionRatio => OriginalSize > 0 ? (double)CompressedSize / OriginalSize : 1.0;
    public int BytesSaved => OriginalSize - CompressedSize;
}

public class CompressionStats
{
    private readonly Dictionary<GameDataType, DataTypeCompressionStats> _typeStats;
    private readonly object _lock = new object();
    
    public CompressionStats()
    {
        _typeStats = new Dictionary<GameDataType, DataTypeCompressionStats>();
    }
    
    public void RecordCompression(GameDataType dataType, int originalSize, int compressedSize, bool compressed)
    {
        lock (_lock)
        {
            if (!_typeStats.TryGetValue(dataType, out var stats))
            {
                stats = new DataTypeCompressionStats();
                _typeStats[dataType] = stats;
            }
            
            stats.RecordCompression(originalSize, compressedSize, compressed);
        }
    }
    
    public CompressionStats GetSnapshot()
    {
        lock (_lock)
        {
            var snapshot = new CompressionStats();
            
            foreach (var kvp in _typeStats)
            {
                snapshot._typeStats[kvp.Key] = kvp.Value.Clone();
            }
            
            return snapshot;
        }
    }
    
    public void PrintSummary()
    {
        lock (_lock)
        {
            Console.WriteLine("\n=== 압축 통계 ===");
            
            var totalOriginal = 0L;
            var totalCompressed = 0L;
            var totalPackets = 0;
            
            foreach (var kvp in _typeStats)
            {
                var stats = kvp.Value;
                totalOriginal += stats.TotalOriginalBytes;
                totalCompressed += stats.TotalCompressedBytes;
                totalPackets += stats.PacketCount;
                
                var ratio = stats.TotalOriginalBytes > 0 
                    ? (double)stats.TotalCompressedBytes / stats.TotalOriginalBytes 
                    : 1.0;
                
                Console.WriteLine($"{kvp.Key}: {stats.PacketCount}개 패킷, " +
                                $"압축률 {ratio:P1}, " +
                                $"절약 {(stats.TotalOriginalBytes - stats.TotalCompressedBytes) / 1024.0:F1} KB");
            }
            
            var overallRatio = totalOriginal > 0 ? (double)totalCompressed / totalOriginal : 1.0;
            Console.WriteLine($"전체: {totalPackets}개 패킷, " +
                            $"압축률 {overallRatio:P1}, " +
                            $"절약 {(totalOriginal - totalCompressed) / 1024.0:F1} KB");
        }
    }
}

public class DataTypeCompressionStats
{
    public int PacketCount { get; private set; }
    public long TotalOriginalBytes { get; private set; }
    public long TotalCompressedBytes { get; private set; }
    public int CompressedPacketCount { get; private set; }
    
    public void RecordCompression(int originalSize, int compressedSize, bool compressed)
    {
        PacketCount++;
        TotalOriginalBytes += originalSize;
        TotalCompressedBytes += compressedSize;
        
        if (compressed)
        {
            CompressedPacketCount++;
        }
    }
    
    public DataTypeCompressionStats Clone()
    {
        return new DataTypeCompressionStats
        {
            PacketCount = PacketCount,
            TotalOriginalBytes = TotalOriginalBytes,
            TotalCompressedBytes = TotalCompressedBytes,
            CompressedPacketCount = CompressedPacketCount
        };
    }
}
```

### 10.3.2 델타 압축과 상태 동기화

```csharp
public class DeltaCompressor
{
    private readonly Dictionary<string, GameStateSnapshot> _lastSnapshots;
    private readonly Dictionary<string, MovementPredictor> _movementPredictors;
    private readonly object _lock = new object();
    
    public DeltaCompressor()
    {
        _lastSnapshots = new Dictionary<string, GameStateSnapshot>();
        _movementPredictors = new Dictionary<string, MovementPredictor>();
    }
    
    public DeltaPacket CreateDeltaPacket(string peerId, GameStateSnapshot currentState)
    {
        lock (_lock)
        {
            if (!_lastSnapshots.TryGetValue(peerId, out var lastSnapshot))
            {
                // 첫 번째 스냅샷은 전체 상태 전송
                _lastSnapshots[peerId] = currentState.Clone();
                
                return new DeltaPacket
                {
                    PeerId = peerId,
                    SequenceNumber = currentState.SequenceNumber,
                    IsFullSnapshot = true,
                    DeltaData = SerializeFullSnapshot(currentState),
                    Timestamp = DateTime.UtcNow
                };
            }
            
            var delta = CalculateDelta(lastSnapshot, currentState);
            _lastSnapshots[peerId] = currentState.Clone();
            
            return new DeltaPacket
            {
                PeerId = peerId,
                SequenceNumber = currentState.SequenceNumber,
                BaseSequenceNumber = lastSnapshot.SequenceNumber,
                IsFullSnapshot = false,
                DeltaData = SerializeDelta(delta),
                Timestamp = DateTime.UtcNow
            };
        }
    }
    
    public GameStateSnapshot ApplyDelta(string peerId, DeltaPacket deltaPacket)
    {
        lock (_lock)
        {
            if (deltaPacket.IsFullSnapshot)
            {
                var fullSnapshot = DeserializeFullSnapshot(deltaPacket.DeltaData);
                _lastSnapshots[peerId] = fullSnapshot.Clone();
                return fullSnapshot;
            }
            
            if (!_lastSnapshots.TryGetValue(peerId, out var baseSnapshot))
            {
                throw new InvalidOperationException($"기준 스냅샷이 없음: {peerId}");
            }
            
            // 시퀀스 번호 검증
            if (deltaPacket.BaseSequenceNumber != baseSnapshot.SequenceNumber)
            {
                throw new InvalidOperationException(
                    $"시퀀스 불일치: 예상 {baseSnapshot.SequenceNumber}, 실제 {deltaPacket.BaseSequenceNumber}");
            }
            
            var delta = DeserializeDelta(deltaPacket.DeltaData);
            var newSnapshot = ApplyDeltaToSnapshot(baseSnapshot, delta, deltaPacket.SequenceNumber);
            
            _lastSnapshots[peerId] = newSnapshot.Clone();
            return newSnapshot;
        }
    }
    
    private StateDelta CalculateDelta(GameStateSnapshot oldState, GameStateSnapshot newState)
    {
        var delta = new StateDelta
        {
            PlayerUpdates = new Dictionary<string, PlayerStateDelta>(),
            ObjectUpdates = new Dictionary<string, ObjectStateDelta>(),
            RemovedPlayers = new List<string>(),
            RemovedObjects = new List<string>()
        };
        
        // 플레이어 상태 변화 계산
        foreach (var newPlayer in newState.Players)
        {
            if (oldState.Players.TryGetValue(newPlayer.Key, out var oldPlayer))
            {
                var playerDelta = CalculatePlayerDelta(oldPlayer, newPlayer.Value);
                if (playerDelta.HasChanges())
                {
                    delta.PlayerUpdates[newPlayer.Key] = playerDelta;
                }
            }
            else
            {
                // 새 플레이어 추가
                delta.PlayerUpdates[newPlayer.Key] = CreateFullPlayerDelta(newPlayer.Value);
            }
        }
        
        // 제거된 플레이어 찾기
        foreach (var oldPlayer in oldState.Players)
        {
            if (!newState.Players.ContainsKey(oldPlayer.Key))
            {
                delta.RemovedPlayers.Add(oldPlayer.Key);
            }
        }
        
        // 오브젝트 상태 변화 계산 (플레이어와 유사한 로직)
        CalculateObjectDeltas(oldState, newState, delta);
        
        return delta;
    }
    
    private PlayerStateDelta CalculatePlayerDelta(PlayerState oldPlayer, PlayerState newPlayer)
    {
        var delta = new PlayerStateDelta { PlayerId = newPlayer.PlayerId };
        
        // 위치 변화 (정밀도 고려)
        if (Vector3Distance(oldPlayer.Position, newPlayer.Position) > 0.01f)
        {
            delta.Position = newPlayer.Position;
            delta.HasPositionUpdate = true;
        }
        
        // 회전 변화
        if (Vector3Distance(oldPlayer.Rotation, newPlayer.Rotation) > 0.1f)
        {
            delta.Rotation = newPlayer.Rotation;
            delta.HasRotationUpdate = true;
        }
        
        // 속도 변화 (예측을 위해)
        if (Vector3Distance(oldPlayer.Velocity, newPlayer.Velocity) > 0.01f)
        {
            delta.Velocity = newPlayer.Velocity;
            delta.HasVelocityUpdate = true;
        }
        
        // 체력 변화
        if (Math.Abs(oldPlayer.Health - newPlayer.Health) > 0.1f)
        {
            delta.Health = newPlayer.Health;
            delta.HasHealthUpdate = true;
        }
        
        // 애니메이션 상태 변화
        if (oldPlayer.AnimationState != newPlayer.AnimationState)
        {
            delta.AnimationState = newPlayer.AnimationState;
            delta.HasAnimationUpdate = true;
        }
        
        return delta;
    }
    
    private void CalculateObjectDeltas(GameStateSnapshot oldState, GameStateSnapshot newState, StateDelta delta)
    {
        foreach (var newObject in newState.GameObjects)
        {
            if (oldState.GameObjects.TryGetValue(newObject.Key, out var oldObject))
            {
                var objectDelta = CalculateObjectDelta(oldObject, newObject.Value);
                if (objectDelta.HasChanges())
                {
                    delta.ObjectUpdates[newObject.Key] = objectDelta;
                }
            }
            else
            {
                delta.ObjectUpdates[newObject.Key] = CreateFullObjectDelta(newObject.Value);
            }
        }
        
        foreach (var oldObject in oldState.GameObjects)
        {
            if (!newState.GameObjects.ContainsKey(oldObject.Key))
            {
                delta.RemovedObjects.Add(oldObject.Key);
            }
        }
    }
    
    private ObjectStateDelta CalculateObjectDelta(GameObject oldObject, GameObject newObject)
    {
        var delta = new ObjectStateDelta { ObjectId = newObject.ObjectId };
        
        if (Vector3Distance(oldObject.Position, newObject.Position) > 0.01f)
        {
            delta.Position = newObject.Position;
            delta.HasPositionUpdate = true;
        }
        
        if (Vector3Distance(oldObject.Rotation, newObject.Rotation) > 0.1f)
        {
            delta.Rotation = newObject.Rotation;
            delta.HasRotationUpdate = true;
        }
        
        if (oldObject.State != newObject.State)
        {
            delta.State = newObject.State;
            delta.HasStateUpdate = true;
        }
        
        return delta;
    }
    
    private GameStateSnapshot ApplyDeltaToSnapshot(GameStateSnapshot baseSnapshot, StateDelta delta, int newSequenceNumber)
    {
        var newSnapshot = baseSnapshot.Clone();
        newSnapshot.SequenceNumber = newSequenceNumber;
        newSnapshot.Timestamp = DateTime.UtcNow;
        
        // 플레이어 업데이트 적용
        foreach (var playerUpdate in delta.PlayerUpdates)
        {
            if (newSnapshot.Players.TryGetValue(playerUpdate.Key, out var existingPlayer))
            {
                ApplyPlayerDelta(existingPlayer, playerUpdate.Value);
            }
            else
            {
                // 새 플레이어 추가
                newSnapshot.Players[playerUpdate.Key] = CreatePlayerFromDelta(playerUpdate.Value);
            }
        }
        
        // 제거된 플레이어 처리
        foreach (var removedPlayerId in delta.RemovedPlayers)
        {
            newSnapshot.Players.Remove(removedPlayerId);
        }
        
        // 오브젝트 업데이트 적용
        foreach (var objectUpdate in delta.ObjectUpdates)
        {
            if (newSnapshot.GameObjects.TryGetValue(objectUpdate.Key, out var existingObject))
            {
                ApplyObjectDelta(existingObject, objectUpdate.Value);
            }
            else
            {
                newSnapshot.GameObjects[objectUpdate.Key] = CreateObjectFromDelta(objectUpdate.Value);
            }
        }
        
        // 제거된 오브젝트 처리
        foreach (var removedObjectId in delta.RemovedObjects)
        {
            newSnapshot.GameObjects.Remove(removedObjectId);
        }
        
        return newSnapshot;
    }
    
    private void ApplyPlayerDelta(PlayerState player, PlayerStateDelta delta)
    {
        if (delta.HasPositionUpdate) player.Position = delta.Position;
        if (delta.HasRotationUpdate) player.Rotation = delta.Rotation;
        if (delta.HasVelocityUpdate) player.Velocity = delta.Velocity;
        if (delta.HasHealthUpdate) player.Health = delta.Health;
        if (delta.HasAnimationUpdate) player.AnimationState = delta.AnimationState;
    }
    
    private void ApplyObjectDelta(GameObject gameObject, ObjectStateDelta delta)
    {
        if (delta.HasPositionUpdate) gameObject.Position = delta.Position;
        if (delta.HasRotationUpdate) gameObject.Rotation = delta.Rotation;
        if (delta.HasStateUpdate) gameObject.State = delta.State;
    }
    
    private PlayerStateDelta CreateFullPlayerDelta(PlayerState player)
    {
        return new PlayerStateDelta
        {
            PlayerId = player.PlayerId,
            Position = player.Position,
            Rotation = player.Rotation,
            Velocity = player.Velocity,
            Health = player.Health,
            AnimationState = player.AnimationState,
            HasPositionUpdate = true,
            HasRotationUpdate = true,
            HasVelocityUpdate = true,
            HasHealthUpdate = true,
            HasAnimationUpdate = true
        };
    }
    
    private ObjectStateDelta CreateFullObjectDelta(GameObject gameObject)
    {
        return new ObjectStateDelta
        {
            ObjectId = gameObject.ObjectId,
            Position = gameObject.Position,
            Rotation = gameObject.Rotation,
            State = gameObject.State,
            HasPositionUpdate = true,
            HasRotationUpdate = true,
            HasStateUpdate = true
        };
    }
    
    private PlayerState CreatePlayerFromDelta(PlayerStateDelta delta)
    {
        return new PlayerState
        {
            PlayerId = delta.PlayerId,
            Position = delta.Position,
            Rotation = delta.Rotation,
            Velocity = delta.Velocity,
            Health = delta.Health,
            AnimationState = delta.AnimationState
        };
    }
    
    private GameObject CreateObjectFromDelta(ObjectStateDelta delta)
    {
        return new GameObject
        {
            ObjectId = delta.ObjectId,
            Position = delta.Position,
            Rotation = delta.Rotation,
            State = delta.State
        };
    }
    
    private float Vector3Distance(Vector3 a, Vector3 b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        var dz = a.Z - b.Z;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
    
    private byte[] SerializeFullSnapshot(GameStateSnapshot snapshot)
    {
        // 실제 구현에서는 MessagePack, Protocol Buffers 등 사용
        // 여기서는 간단한 바이너리 직렬화 시뮬레이션
        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(snapshot);
    }
    
    private byte[] SerializeDelta(StateDelta delta)
    {
        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(delta);
    }
    
    private GameStateSnapshot DeserializeFullSnapshot(byte[] data)
    {
        return System.Text.Json.JsonSerializer.Deserialize<GameStateSnapshot>(data);
    }
    
    private StateDelta DeserializeDelta(byte[] data)
    {
        return System.Text.Json.JsonSerializer.Deserialize<StateDelta>(data);
    }
}

// 게임 상태 관련 데이터 구조들
public class GameStateSnapshot
{
    public int SequenceNumber { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, PlayerState> Players { get; set; } = new();
    public Dictionary<string, GameObject> GameObjects { get; set; } = new();
    
    public GameStateSnapshot Clone()
    {
        // 깊은 복사 구현
        var json = System.Text.Json.JsonSerializer.Serialize(this);
        return System.Text.Json.JsonSerializer.Deserialize<GameStateSnapshot>(json);
    }
}

public class StateDelta
{
    public Dictionary<string, PlayerStateDelta> PlayerUpdates { get; set; } = new();
    public Dictionary<string, ObjectStateDelta> ObjectUpdates { get; set; } = new();
    public List<string> RemovedPlayers { get; set; } = new();
    public List<string> RemovedObjects { get; set; } = new();
}

public class PlayerState
{
    public string PlayerId { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
    public Vector3 Velocity { get; set; }
    public float Health { get; set; }
    public string AnimationState { get; set; }
}

public class PlayerStateDelta
{
    public string PlayerId { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
    public Vector3 Velocity { get; set; }
    public float Health { get; set; }
    public string AnimationState { get; set; }
    
    public bool HasPositionUpdate { get; set; }
    public bool HasRotationUpdate { get; set; }
    public bool HasVelocityUpdate { get; set; }
    public bool HasHealthUpdate { get; set; }
    public bool HasAnimationUpdate { get; set; }
    
    public bool HasChanges()
    {
        return HasPositionUpdate || HasRotationUpdate || HasVelocityUpdate || 
               HasHealthUpdate || HasAnimationUpdate;
    }
}

public class GameObject
{
    public string ObjectId { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
    public string State { get; set; }
}

public class ObjectStateDelta
{
    public string ObjectId { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
    public string State { get; set; }
    
    public bool HasPositionUpdate { get; set; }
    public bool HasRotationUpdate { get; set; }
    public bool HasStateUpdate { get; set; }
    
    public bool HasChanges()
    {
        return HasPositionUpdate || HasRotationUpdate || HasStateUpdate;
    }
}

public struct Vector3
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    
    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

public class DeltaPacket
{
    public string PeerId { get; set; }
    public int SequenceNumber { get; set; }
    public int BaseSequenceNumber { get; set; }
    public bool IsFullSnapshot { get; set; }
    public byte[] DeltaData { get; set; }
    public DateTime Timestamp { get; set; }
}
```

## 10.4 혼잡 제어와 흐름 제어

네트워크 혼잡을 감지하고 전송 속도를 조절하여 안정적인 통신을 보장하는 메커니즘입니다.

### 10.4.1 적응형 혼잡 제어

```csharp
public class CongestionController
{
    private readonly Dictionary<string, PeerCongestionState> _peerStates;
    private readonly Timer _controlTimer;
    private readonly object _stateLock = new object();
    
    // 혼잡 제어 파라미터
    private readonly TimeSpan _rtoMin = TimeSpan.FromMilliseconds(50);
    private readonly TimeSpan _rtoMax = TimeSpan.FromSeconds(10);
    private readonly double _rtoBeta = 0.25;
    private readonly double _rtoAlpha = 0.125;
    
    public event Action<string, CongestionEvent> CongestionDetected;
    
    public CongestionController()
    {
        _peerStates = new Dictionary<string, PeerCongestionState>();
        
        // 100ms마다 혼잡 제어 로직 실행
        _controlTimer = new Timer(UpdateCongestionControl, null,
            TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));
    }
    
    public void RegisterPeer(string peerId)
    {
        lock (_stateLock)
        {
            if (!_peerStates.ContainsKey(peerId))
            {
                _peerStates[peerId] = new PeerCongestionState(peerId);
                Console.WriteLine($"혼잡 제어 - 피어 등록: {peerId}");
            }
        }
    }
    
    public void UnregisterPeer(string peerId)
    {
        lock (_stateLock)
        {
            if (_peerStates.Remove(peerId))
            {
                Console.WriteLine($"혼잡 제어 - 피어 해제: {peerId}");
            }
        }
    }
    
    public void RecordPacketSent(string peerId, int sequenceNumber, int packetSize)
    {
        lock (_stateLock)
        {
            if (_peerStates.TryGetValue(peerId, out var state))
            {
                state.RecordPacketSent(sequenceNumber, packetSize);
            }
        }
    }
    
    public void RecordPacketAcknowledged(string peerId, int sequenceNumber, TimeSpan rtt)
    {
        lock (_stateLock)
        {
            if (_peerStates.TryGetValue(peerId, out var state))
            {
                state.RecordAcknowledgment(sequenceNumber, rtt);
                UpdateRttEstimates(state, rtt);
            }
        }
    }
    
    public void RecordPacketLoss(string peerId, int sequenceNumber)
    {
        lock (_stateLock)
        {
            if (_peerStates.TryGetValue(peerId, out var state))
            {
                state.RecordPacketLoss(sequenceNumber);
                HandleCongestion(state, CongestionEventType.PacketLoss);
            }
        }
    }
    
    private void UpdateRttEstimates(PeerCongestionState state, TimeSpan sampleRtt)
    {
        if (state.SmoothedRtt == TimeSpan.Zero)
        {
            // 첫 번째 측정
            state.SmoothedRtt = sampleRtt;
            state.RttVariation = sampleRtt / 2;
        }
        else
        {
            // RFC 6298 알고리즘
            var rttDiff = Math.Abs((sampleRtt - state.SmoothedRtt).TotalMilliseconds);
            state.RttVariation = TimeSpan.FromMilliseconds(
                (1 - _rtoBeta) * state.RttVariation.TotalMilliseconds + _rtoBeta * rttDiff);
            
            state.SmoothedRtt = TimeSpan.FromMilliseconds(
                (1 - _rtoAlpha) * state.SmoothedRtt.TotalMilliseconds + _rtoAlpha * sampleRtt.TotalMilliseconds);
        }
        
        // RTO 계산
        var rto = state.SmoothedRtt + TimeSpan.FromMilliseconds(4 * state.RttVariation.TotalMilliseconds);
        state.RetransmissionTimeout = TimeSpan.FromMilliseconds(
            Math.Max(_rtoMin.TotalMilliseconds, Math.Min(_rtoMax.TotalMilliseconds, rto.TotalMilliseconds)));
    }
    
    private void UpdateCongestionControl(object state)
    {
        lock (_stateLock)
        {
            foreach (var peerState in _peerStates.Values)
            {
                UpdatePeerCongestionWindow(peerState);
                CheckForTimeouts(peerState);
            }
        }
    }
    
    private void UpdatePeerCongestionWindow(PeerCongestionState state)
    {
        var now = DateTime.UtcNow;
        var timeSinceLastUpdate = now - state.LastCongestionUpdate;
        
        if (timeSinceLastUpdate < TimeSpan.FromMilliseconds(100))
        {
            return; // 너무 빈번한 업데이트 방지
        }
        
        switch (state.CongestionState)
        {
            case CongestionPhase.SlowStart:
                HandleSlowStart(state);
                break;
                
            case CongestionPhase.CongestionAvoidance:
                HandleCongestionAvoidance(state);
                break;
                
            case CongestionPhase.FastRecovery:
                HandleFastRecovery(state);
                break;
        }
        
        state.LastCongestionUpdate = now;
    }
    
    private void HandleSlowStart(PeerCongestionState state)
    {
        // Slow Start: ACK를 받을 때마다 CWND를 1씩 증가
        var acksReceived = state.GetNewAcknowledgments();
        
        if (acksReceived > 0)
        {
            state.CongestionWindow = Math.Min(
                state.CongestionWindow + acksReceived,
                state.SlowStartThreshold);
            
            // CWND가 SSTHRESH에 도달하면 Congestion Avoidance로 전환
            if (state.CongestionWindow >= state.SlowStartThreshold)
            {
                state.CongestionState = CongestionPhase.CongestionAvoidance;
                Console.WriteLine($"피어 {state.PeerId}: Congestion Avoidance 단계로 전환");
            }
        }
    }
    
    private void HandleCongestionAvoidance(PeerCongestionState state)
    {
        // Congestion Avoidance: RTT마다 CWND를 1씩 증가
        var acksReceived = state.GetNewAcknowledgments();
        
        if (acksReceived > 0 && state.SmoothedRtt > TimeSpan.Zero)
        {
            // Linear increase
            var increment = Math.Max(1.0, (double)acksReceived / state.CongestionWindow);
            state.CongestionWindow += increment;
        }
    }
    
    private void HandleFastRecovery(PeerCongestionState state)
    {
        // Fast Recovery에서는 duplicate ACK마다 CWND를 1씩 증가
        var duplicateAcks = state.GetDuplicateAcknowledgments();
        
        if (duplicateAcks > 0)
        {
            state.CongestionWindow += duplicateAcks;
        }
        
        // 새로운 ACK를 받으면 Congestion Avoidance로 복귀
        var newAcks = state.GetNewAcknowledgments();
        if (newAcks > 0)
        {
            state.CongestionWindow = state.SlowStartThreshold;
            state.CongestionState = CongestionPhase.CongestionAvoidance;
            Console.WriteLine($"피어 {state.PeerId}: Fast Recovery에서 복귀");
        }
    }
    
    private void CheckForTimeouts(PeerCongestionState state)
    {
        var now = DateTime.UtcNow;
        var timedOutPackets = state.GetTimedOutPackets(now);
        
        foreach (var packet in timedOutPackets)
        {
            Console.WriteLine($"피어 {state.PeerId}: 패킷 {packet.SequenceNumber} 타임아웃");
            HandleCongestion(state, CongestionEventType.Timeout);
        }
    }
    
    private void HandleCongestion(PeerCongestionState state, CongestionEventType eventType)
    {
        Console.WriteLine($"피어 {state.PeerId}: 혼잡 감지 - {eventType}");
        
        switch (eventType)
        {
            case CongestionEventType.PacketLoss:
                // Fast Retransmit/Fast Recovery
                state.SlowStartThreshold = Math.Max(state.CongestionWindow / 2, 2);
                state.CongestionWindow = state.SlowStartThreshold + 3; // 3 duplicate ACKs
                state.CongestionState = CongestionPhase.FastRecovery;
                break;
                
            case CongestionEventType.Timeout:
                // Timeout-based congestion control
                state.SlowStartThreshold = Math.Max(state.CongestionWindow / 2, 2);
                state.CongestionWindow = 1;
                state.CongestionState = CongestionPhase.SlowStart;
                
                // RTO 백오프
                state.RetransmissionTimeout = TimeSpan.FromMilliseconds(
                    Math.Min(_rtoMax.TotalMilliseconds, state.RetransmissionTimeout.TotalMilliseconds * 2));
                break;
        }
        
        var congestionEvent = new CongestionEvent
        {
            PeerId = state.PeerId,
            EventType = eventType,
            NewCongestionWindow = state.CongestionWindow,
            NewSlowStartThreshold = state.SlowStartThreshold,
            NewRto = state.RetransmissionTimeout,
            Timestamp = DateTime.UtcNow
        };
        
        CongestionDetected?.Invoke(state.PeerId, congestionEvent);
    }
    
    public bool CanSendPacket(string peerId)
    {
        lock (_stateLock)
        {
            if (!_peerStates.TryGetValue(peerId, out var state))
            {
                return true; // 상태가 없으면 일단 허용
            }
            
            return state.GetOutstandingPackets() < state.CongestionWindow;
        }
    }
    
    public int GetMaxPacketsToSend(string peerId)
    {
        lock (_stateLock)
        {
            if (!_peerStates.TryGetValue(peerId, out var state))
            {
                return 10; // 기본값
            }
            
            var outstanding = state.GetOutstandingPackets();
            return Math.Max(0, (int)(state.CongestionWindow - outstanding));
        }
    }
    
    public CongestionStats GetCongestionStats(string peerId)
    {
        lock (_stateLock)
        {
            if (!_peerStates.TryGetValue(peerId, out var state))
            {
                return null;
            }
            
            return new CongestionStats
            {
                PeerId = peerId,
                CongestionWindow = state.CongestionWindow,
                SlowStartThreshold = state.SlowStartThreshold,
                SmoothedRtt = state.SmoothedRtt,
                RttVariation = state.RttVariation,
                RetransmissionTimeout = state.RetransmissionTimeout,
                CongestionState = state.CongestionState,
                PacketsSent = state.PacketsSent,
                PacketsAcknowledged = state.PacketsAcknowledged,
                PacketsLost = state.PacketsLost,
                OutstandingPackets = state.GetOutstandingPackets()
            };
        }
    }
    
    public void Dispose()
    {
        _controlTimer?.Dispose();
    }
}

public class PeerCongestionState
{
    public string PeerId { get; }
    public double CongestionWindow { get; set; } = 1;
    public double SlowStartThreshold { get; set; } = 65535;
    public CongestionPhase CongestionState { get; set; } = CongestionPhase.SlowStart;
    
    public TimeSpan SmoothedRtt { get; set; }
    public TimeSpan RttVariation { get; set; }
    public TimeSpan RetransmissionTimeout { get; set; } = TimeSpan.FromSeconds(1);
    
    public int PacketsSent { get; private set; }
    public int PacketsAcknowledged { get; private set; }
    public int PacketsLost { get; private set; }
    
    public DateTime LastCongestionUpdate { get; set; } = DateTime.UtcNow;
    
    private readonly Dictionary<int, SentPacketInfo> _sentPackets = new();
    private readonly Queue<int> _recentAcks = new();
    private int _lastAckedSequence = -1;
    private int _duplicateAckCount = 0;
    
    public PeerCongestionState(string peerId)
    {
        PeerId = peerId;
    }
    
    public void RecordPacketSent(int sequenceNumber, int packetSize)
    {
        PacketsSent++;
        _sentPackets[sequenceNumber] = new SentPacketInfo
        {
            SequenceNumber = sequenceNumber,
            Size = packetSize,
            SentTime = DateTime.UtcNow,
            Retransmitted = false
        };
    }
    
    public void RecordAcknowledgment(int sequenceNumber, TimeSpan rtt)
    {
        if (_sentPackets.Remove(sequenceNumber))
        {
            PacketsAcknowledged++;
            _recentAcks.Enqueue(sequenceNumber);
            
            // 최근 ACK 기록 제한
            while (_recentAcks.Count > 100)
            {
                _recentAcks.Dequeue();
            }
            
            // Duplicate ACK 검사
            if (sequenceNumber == _lastAckedSequence)
            {
                _duplicateAckCount++;
            }
            else if (sequenceNumber > _lastAckedSequence)
            {
                _lastAckedSequence = sequenceNumber;
                _duplicateAckCount = 0;
            }
        }
    }
    
    public void RecordPacketLoss(int sequenceNumber)
    {
        if (_sentPackets.Remove(sequenceNumber))
        {
            PacketsLost++;
        }
    }
    
    public int GetOutstandingPackets()
    {
        return _sentPackets.Count;
    }
    
    public int GetNewAcknowledgments()
    {
        var count = _recentAcks.Count;
        _recentAcks.Clear();
        return count;
    }
    
    public int GetDuplicateAcknowledgments()
    {
        var count = _duplicateAckCount;
        _duplicateAckCount = 0;
        return count;
    }
    
    public List<SentPacketInfo> GetTimedOutPackets(DateTime now)
    {
        var timedOut = new List<SentPacketInfo>();
        var packetsToRemove = new List<int>();
        
        foreach (var packet in _sentPackets.Values)
        {
            if (now - packet.SentTime > RetransmissionTimeout)
            {
                timedOut.Add(packet);
                packetsToRemove.Add(packet.SequenceNumber);
            }
        }
        
        foreach (var sequenceNumber in packetsToRemove)
        {
            _sentPackets.Remove(sequenceNumber);
            PacketsLost++;
        }
        
        return timedOut;
    }
}

public class SentPacketInfo
{
    public int SequenceNumber { get; set; }
    public int Size { get; set; }
    public DateTime SentTime { get; set; }
    public bool Retransmitted { get; set; }
}

public enum CongestionPhase
{
    SlowStart,
    CongestionAvoidance,
    FastRecovery
}

public enum CongestionEventType
{
    PacketLoss,
    Timeout,
    Recovery
}

public class CongestionEvent
{
    public string PeerId { get; set; }
    public CongestionEventType EventType { get; set; }
    public double NewCongestionWindow { get; set; }
    public double NewSlowStartThreshold { get; set; }
    public TimeSpan NewRto { get; set; }
    public DateTime Timestamp { get; set; }
}

public class CongestionStats
{
    public string PeerId { get; set; }
    public double CongestionWindow { get; set; }
    public double SlowStartThreshold { get; set; }
    public TimeSpan SmoothedRtt { get; set; }
    public TimeSpan RttVariation { get; set; }
    public TimeSpan RetransmissionTimeout { get; set; }
    public CongestionPhase CongestionState { get; set; }
    public int PacketsSent { get; set; }
    public int PacketsAcknowledged { get; set; }
    public int PacketsLost { get; set; }
    public int OutstandingPackets { get; set; }
    
    public double PacketLossRate => PacketsSent > 0 ? (double)PacketsLost / PacketsSent : 0;
    public double Throughput => PacketsAcknowledged > 0 ? CongestionWindow / SmoothedRtt.TotalSeconds : 0;
    
    public override string ToString()
    {
        return $"피어 {PeerId}: CWND={CongestionWindow:F1}, " +
               $"RTT={SmoothedRtt.TotalMilliseconds:F1}ms, " +
               $"손실률={PacketLossRate:P2}, " +
               $"상태={CongestionState}";
    }
}
```

### 10.4.2 통합 대역폭 관리 시스템

이제 모든 컴포넌트를 통합하는 메인 시스템을 만들어보겠습니다.

```csharp
public class BandwidthOptimizationManager
{
    private readonly TrafficAnalyzer _trafficAnalyzer;
    private readonly BandwidthMonitor _bandwidthMonitor;
    private readonly AdaptiveBitrateController _bitrateController;
    private readonly QoSPacketScheduler _packetScheduler;
    private readonly PacketCompressor _packetCompressor;
    private readonly DeltaCompressor _deltaCompressor;
    private readonly CongestionController _congestionController;
    
    private readonly Timer _optimizationTimer;
    
    public BandwidthOptimizationManager()
    {
        _trafficAnalyzer = new TrafficAnalyzer();
        _bandwidthMonitor = new BandwidthMonitor();
        _bitrateController = new AdaptiveBitrateController(_bandwidthMonitor);
        _packetScheduler = new QoSPacketScheduler(_bitrateController);
        _packetCompressor = new PacketCompressor();
        _deltaCompressor = new DeltaCompressor();
        _congestionController = new CongestionController();
        
        // 이벤트 연결
        _bandwidthMonitor.BandwidthAlertRaised += OnBandwidthAlert;
        _bitrateController.QualityChanged += OnQualityChanged;
        _congestionController.CongestionDetected += OnCongestionDetected;
        
        // 30초마다 전체 최적화 검토
        _optimizationTimer = new Timer(PerformOptimizationReview, null,
            TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        
        Console.WriteLine("대역폭 최적화 관리자 초기화 완료");
    }
    
    public async Task<bool> SendGamePacket(string peerId, GamePacket packet, 
        Func<GamePacket, Task> transmissionCallback)
    {
        try
        {
            // 1. 트래픽 분석을 위한 기록
            _trafficAnalyzer.RecordPacket(packet);
            
            // 2. 혼잡 제어 확인
            if (!_congestionController.CanSendPacket(peerId))
            {
                Console.WriteLine($"혼잡 제어로 인해 패킷 전송 대기: {peerId}");
                return false;
            }
            
            // 3. 패킷 압축
            var compressedPacket = _packetCompressor.Compress(packet);
            var optimizedPacket = new GamePacket
            {
                DataType = packet.DataType,
                Priority = packet.Priority,
                Data = compressedPacket.CompressedData,
                Timestamp = packet.Timestamp,
                PlayerId = packet.PlayerId,
                SequenceNumber = packet.SequenceNumber,
                RequiresAck = packet.RequiresAck
            };
            
            // 4. QoS 스케줄링
            await _packetScheduler.SchedulePacket(peerId, optimizedPacket, async (p) =>
            {
                // 5. 대역폭 모니터링
                var packetSize = p.GetEstimatedSize();
                _bandwidthMonitor.RecordSentData(peerId, packetSize);
                
                // 6. 혼잡 제어 상태 업데이트
                _congestionController.RecordPacketSent(peerId, p.SequenceNumber, packetSize);
                
                // 7. 실제 전송
                await transmissionCallback(p);
            });
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"패킷 전송 실패 {peerId}: {ex.Message}");
            return false;
        }
    }
    
    public async Task<GameStateSnapshot> SendGameState(string peerId, GameStateSnapshot currentState,
        Func<DeltaPacket, Task> transmissionCallback)
    {
        try
        {
            // 델타 압축을 사용한 상태 동기화
            var deltaPacket = _deltaCompressor.CreateDeltaPacket(peerId, currentState);
            
            // 델타 패킷을 일반 게임 패킷으로 변환
            var gamePacket = new GamePacket
            {
                DataType = GameDataType.GameState,
                Priority = DataPriority.High,
                Data = deltaPacket.DeltaData,
                Timestamp = DateTime.UtcNow,
                PlayerId = "SYSTEM",
                SequenceNumber = deltaPacket.SequenceNumber,
                RequiresAck = true
            };
            
            var success = await SendGamePacket(peerId, gamePacket, async (p) =>
            {
                var reconstructedDelta = new DeltaPacket
                {
                    PeerId = deltaPacket.PeerId,
                    SequenceNumber = deltaPacket.SequenceNumber,
                    BaseSequenceNumber = deltaPacket.BaseSequenceNumber,
                    IsFullSnapshot = deltaPacket.IsFullSnapshot,
                    DeltaData = p.Data,
                    Timestamp = deltaPacket.Timestamp
                };
                
                await transmissionCallback(reconstructedDelta);
            });
            
            return success ? currentState : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"게임 상태 전송 실패 {peerId}: {ex.Message}");
            return null;
        }
    }
    
    public void RegisterPeer(string peerId)
    {
        _bitrateController.RegisterPeer(peerId);
        _congestionController.RegisterPeer(peerId);
        
        Console.WriteLine($"피어 등록: {peerId}");
    }
    
    public void UnregisterPeer(string peerId)
    {
        _bitrateController.UnregisterPeer(peerId);
        _congestionController.UnregisterPeer(peerId);
        
        Console.WriteLine($"피어 해제: {peerId}");
    }
    
    public void RecordPacketReceived(string peerId, GamePacket packet)
    {
        var packetSize = packet.GetEstimatedSize();
        _bandwidthMonitor.RecordReceivedData(peerId, packetSize);
        
        if (packet.RequiresAck)
        {
            // RTT 계산 및 ACK 처리
            var rtt = DateTime.UtcNow - packet.Timestamp;
            _congestionController.RecordPacketAcknowledged(peerId, packet.SequenceNumber, rtt);
        }
    }
    
    private void OnBandwidthAlert(BandwidthAlert alert)
    {
        Console.WriteLine($"대역폭 경고: {alert.Message}");
        
        // 경고에 따른 자동 조치
        switch (alert.AlertType)
        {
            case BandwidthAlertType.UploadCritical:
            case BandwidthAlertType.DownloadCritical:
                // 모든 피어의 품질을 강제로 낮춤
                var summary = _bandwidthMonitor.GetCurrentSummary();
                foreach (var peer in summary.TopPeers.Take(5))
                {
                    var currentQuality = _bitrateController.GetCurrentQuality(peer.PeerId);
                    if (currentQuality > QualityLevel.Low)
                    {
                        // 강제 품질 저하는 실제로는 더 신중하게 구현해야 함
                        Console.WriteLine($"긴급 상황으로 인한 피어 {peer.PeerId} 품질 저하 필요");
                    }
                }
                break;
        }
    }
    
    private void OnQualityChanged(string peerId, QualityLevel oldQuality, QualityLevel newQuality)
    {
        Console.WriteLine($"피어 {peerId} 품질 변경: {oldQuality} -> {newQuality}");
        
        // 품질 변화에 따른 패킷 스케줄러 조정
        var newBitrate = GetEstimatedBitrate(newQuality);
        var packetsPerSecond = EstimatePacketsPerSecond(newBitrate);
        
        _packetScheduler.AdjustTransmissionRate(packetsPerSecond);
    }
    
    private void OnCongestionDetected(string peerId, CongestionEvent congestionEvent)
    {
        Console.WriteLine($"혼잡 감지: {peerId} - {congestionEvent.EventType}");
        
        // 혼잡 상황에 따른 추가 조치
        if (congestionEvent.EventType == CongestionEventType.PacketLoss)
        {
            // 패킷 손실 시 품질 일시적 저하
            var currentQuality = _bitrateController.GetCurrentQuality(peerId);
            if (currentQuality > QualityLevel.Low)
            {
                Console.WriteLine($"패킷 손실로 인한 피어 {peerId} 임시 품질 저하 고려");
            }
        }
    }
    
    private async void PerformOptimizationReview(object state)
    {
        try
        {
            Console.WriteLine("\n=== 대역폭 최적화 검토 ===");
            
            // 트래픽 분석 결과 출력
            var trafficMetrics = _trafficAnalyzer.GetMetrics(GameDataType.PlayerMovement);
            Console.WriteLine($"플레이어 이동 데이터 평균 크기: {trafficMetrics.GetAverageSize():F1} bytes");
            
            // 압축 통계 출력
            var compressionStats = _packetCompressor.GetStats();
            compressionStats.PrintSummary();
            
            // 대역폭 요약 출력
            var bandwidthSummary = _bandwidthMonitor.GetCurrentSummary();
            Console.WriteLine(bandwidthSummary.ToString());
            
            // 큐 상태 점검
            var queueSize = _packetScheduler.GetQueueSize();
            if (queueSize > 500)
            {
                Console.WriteLine($"경고: 패킷 큐 크기가 큼 ({queueSize})");
            }
            
            Console.WriteLine("=== 검토 완료 ===\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"최적화 검토 중 오류: {ex.Message}");
        }
    }
    
    private int GetEstimatedBitrate(QualityLevel quality)
    {
        return quality switch
        {
            QualityLevel.Minimal => 32000,
            QualityLevel.Low => 128000,
            QualityLevel.Medium => 512000,
            QualityLevel.High => 1024000,
            QualityLevel.Ultra => 2048000,
            _ => 512000
        };
    }
    
    private int EstimatePacketsPerSecond(int bitrate)
    {
        const int averagePacketSize = 1000; // 1000 bytes
        var bytesPerSecond = bitrate / 8;
        return bytesPerSecond / averagePacketSize;
    }
    
    public OptimizationSummary GetOptimizationSummary()
    {
        var bandwidthSummary = _bandwidthMonitor.GetCurrentSummary();
        var compressionStats = _packetCompressor.GetStats();
        
        return new OptimizationSummary
        {
            TotalBandwidthUsage = bandwidthSummary.TotalSendBandwidth + bandwidthSummary.TotalReceiveBandwidth,
            ActivePeerCount = bandwidthSummary.ActivePeerCount,
            QueueSize = _packetScheduler.GetQueueSize(),
            CompressionRatio = CalculateOverallCompressionRatio(compressionStats),
            Timestamp = DateTime.UtcNow
        };
    }
    
    private double CalculateOverallCompressionRatio(CompressionStats stats)
    {
        // 전체 압축률 계산 로직
        return 0.75; // 임시값
    }
    
    public void Dispose()
    {
        _optimizationTimer?.Dispose();
        _trafficAnalyzer?.Dispose();
        _bandwidthMonitor?.Dispose();
        _bitrateController?.Dispose();
        _packetScheduler?.Dispose();
        _congestionController?.Dispose();
    }
}

public class OptimizationSummary
{
    public double TotalBandwidthUsage { get; set; }
    public int ActivePeerCount { get; set; }
    public int QueueSize { get; set; }
    public double CompressionRatio { get; set; }
    public DateTime Timestamp { get; set; }
    
    public override string ToString()
    {
        return $"최적화 요약: 대역폭 {TotalBandwidthUsage / 1000:F1} Kbps, " +
               $"활성 피어 {ActivePeerCount}개, 큐 크기 {QueueSize}, " +
               $"압축률 {CompressionRatio:P1}";
    }
}

// 사용 예제
public class GameNetworkManager
{
    private readonly BandwidthOptimizationManager _optimizationManager;
    
    public GameNetworkManager()
    {
        _optimizationManager = new BandwidthOptimizationManager();
    }
    
    public async Task SendPlayerMovement(string peerId, Vector3 position, Vector3 rotation)
    {
        var movementData = SerializeMovement(position, rotation);
        
        var packet = new GamePacket
        {
            DataType = GameDataType.PlayerMovement,
            Priority = DataPriority.High,
            Data = movementData,
            Timestamp = DateTime.UtcNow,
            PlayerId = "LocalPlayer",
            SequenceNumber = GetNextSequenceNumber(),
            RequiresAck = false
        };
        
        await _optimizationManager.SendGamePacket(peerId, packet, TransmitPacketToNetwork);
    }
    
    private byte[] SerializeMovement(Vector3 position, Vector3 rotation)
    {
        // 실제 직렬화 로직
        return new byte[24]; // 3 Vector3 * 4 bytes * 2 = 24 bytes
    }
    
    private async Task TransmitPacketToNetwork(GamePacket packet)
    {
        // 실제 네트워크 전송 로직
        await Task.Delay(1); // 네트워크 지연 시뮬레이션
    }
    
    private int GetNextSequenceNumber()
    {
        // 시퀀스 번호 생성 로직
        return Environment.TickCount;
    }
}
```

이제 10장의 모든 내용을 다뤘습니다. P2P 게임에서 대역폭과 패킷 최적화를 위한 포괄적인 시스템을 구현했는데, 트래픽 패턴 분석을 통한 최적화 포인트 발견, 적응형 비트레이트와 QoS를 통한 동적 품질 조정, 다양한 압축 기법을 통한 데이터 크기 최소화, 그리고 혼잡 제어와 흐름 제어를 통한 안정적인 전송을 다뤘습니다.

다음 장에서는 어떤 주제를 다루고 싶으신가요? 보안과 치팅 방지, 실시간 동기화와 예측, 모니터링과 디버깅, 또는 다른 주제가 있으시면 알려주세요.
  

# 11장: 보안과 치팅 방지
P2P 온라인 게임에서 보안은 매우 중요한 요소입니다. 중앙 서버가 모든 것을 제어하는 클라이언트-서버 모델과 달리, P2P 환경에서는 각 클라이언트가 게임 상태의 일부를 담당하므로 보안 위협이 더욱 다양하고 복잡합니다.

## 11.1 P2P 환경에서의 보안 위협

### 주요 보안 위협 유형

P2P 게임 환경에서 발생할 수 있는 주요 보안 위협들을 살펴보겠습니다.

**1. 패킷 조작 및 스니핑**
클라이언트 간 직접 통신에서 악의적인 사용자가 패킷을 가로채거나 조작할 수 있습니다.

**2. 게임 상태 조작**
각 클라이언트가 게임 상태의 일부를 관리하므로, 악의적인 클라이언트가 자신에게 유리하도록 상태를 조작할 수 있습니다.

**3. 가짜 피어 연결**
NAT 홀펀칭 과정에서 가짜 피어가 네트워크에 참여하여 게임을 방해할 수 있습니다.

다음은 기본적인 보안 위협 탐지 시스템 예제입니다:

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Security.Cryptography;
using System.Text;

public class SecurityThreatDetector
{
    private readonly Dictionary<IPEndPoint, PeerSecurityInfo> _peerSecurityData;
    private readonly Timer _monitoringTimer;
    private readonly object _lockObject = new object();
    
    public SecurityThreatDetector()
    {
        _peerSecurityData = new Dictionary<IPEndPoint, PeerSecurityInfo>();
        _monitoringTimer = new Timer(MonitorSecurityThreats, null, 
            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
    }
    
    public void RecordPacketReceived(IPEndPoint sender, GamePacket packet)
    {
        lock (_lockObject)
        {
            if (!_peerSecurityData.ContainsKey(sender))
            {
                _peerSecurityData[sender] = new PeerSecurityInfo();
            }
            
            var securityInfo = _peerSecurityData[sender];
            securityInfo.PacketCount++;
            securityInfo.LastActivity = DateTime.UtcNow;
            
            // 패킷 빈도 검사
            if (IsPacketFloodingDetected(securityInfo))
            {
                OnSecurityThreatDetected?.Invoke(sender, SecurityThreatType.PacketFlooding);
            }
            
            // 패킷 무결성 검사
            if (!VerifyPacketIntegrity(packet))
            {
                securityInfo.IntegrityViolations++;
                OnSecurityThreatDetected?.Invoke(sender, SecurityThreatType.PacketTampering);
            }
        }
    }
    
    private bool IsPacketFloodingDetected(PeerSecurityInfo securityInfo)
    {
        var timeWindow = TimeSpan.FromSeconds(1);
        var maxPacketsPerSecond = 100;
        
        return securityInfo.PacketCount > maxPacketsPerSecond && 
               DateTime.UtcNow - securityInfo.FirstActivity < timeWindow;
    }
    
    private bool VerifyPacketIntegrity(GamePacket packet)
    {
        // 패킷 체크섬 또는 해시 검증
        using (var sha256 = SHA256.Create())
        {
            var dataHash = sha256.ComputeHash(packet.Data);
            return packet.Checksum.SequenceEqual(dataHash);
        }
    }
    
    private void MonitorSecurityThreats(object state)
    {
        lock (_lockObject)
        {
            var now = DateTime.UtcNow;
            var peersToRemove = new List<IPEndPoint>();
            
            foreach (var kvp in _peerSecurityData)
            {
                var peer = kvp.Key;
                var securityInfo = kvp.Value;
                
                // 비활성 피어 제거
                if (now - securityInfo.LastActivity > TimeSpan.FromMinutes(5))
                {
                    peersToRemove.Add(peer);
                }
                
                // 보안 위반 임계값 검사
                if (securityInfo.IntegrityViolations > 10)
                {
                    OnSecurityThreatDetected?.Invoke(peer, SecurityThreatType.RepeatedViolations);
                }
            }
            
            foreach (var peer in peersToRemove)
            {
                _peerSecurityData.Remove(peer);
            }
        }
    }
    
    public event Action<IPEndPoint, SecurityThreatType> OnSecurityThreatDetected;
}

public class PeerSecurityInfo
{
    public int PacketCount { get; set; }
    public DateTime FirstActivity { get; set; } = DateTime.UtcNow;
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    public int IntegrityViolations { get; set; }
}

public enum SecurityThreatType
{
    PacketFlooding,
    PacketTampering,
    RepeatedViolations,
    SuspiciousActivity
}

public class GamePacket
{
    public byte[] Data { get; set; }
    public byte[] Checksum { get; set; }
    public DateTime Timestamp { get; set; }
    public PacketType Type { get; set; }
}

public enum PacketType
{
    PlayerMove,
    GameState,
    Chat,
    Heartbeat
}
```

## 11.2 암호화와 인증 메커니즘

### 대칭키 암호화를 이용한 통신 보안

P2P 게임에서는 성능과 보안의 균형을 맞춰야 합니다. 대칭키 암호화는 상대적으로 빠르면서도 충분한 보안을 제공합니다.

```csharp
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public class GameCryptography
{
    private readonly byte[] _sharedKey;
    private readonly byte[] _iv;
    
    public GameCryptography()
    {
        using (var aes = Aes.Create())
        {
            aes.GenerateKey();
            aes.GenerateIV();
            _sharedKey = aes.Key;
            _iv = aes.IV;
        }
    }
    
    public GameCryptography(byte[] sharedKey, byte[] iv)
    {
        _sharedKey = sharedKey;
        _iv = iv;
    }
    
    public byte[] EncryptPacket(GamePacket packet)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = _sharedKey;
            aes.IV = _iv;
            
            using (var encryptor = aes.CreateEncryptor())
            using (var msEncrypt = new MemoryStream())
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            {
                var packetData = SerializePacket(packet);
                csEncrypt.Write(packetData, 0, packetData.Length);
                csEncrypt.FlushFinalBlock();
                return msEncrypt.ToArray();
            }
        }
    }
    
    public GamePacket DecryptPacket(byte[] encryptedData)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = _sharedKey;
            aes.IV = _iv;
            
            using (var decryptor = aes.CreateDecryptor())
            using (var msDecrypt = new MemoryStream(encryptedData))
            using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            using (var msResult = new MemoryStream())
            {
                csDecrypt.CopyTo(msResult);
                var decryptedData = msResult.ToArray();
                return DeserializePacket(decryptedData);
            }
        }
    }
    
    private byte[] SerializePacket(GamePacket packet)
    {
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            writer.Write((int)packet.Type);
            writer.Write(packet.Timestamp.ToBinary());
            writer.Write(packet.Data.Length);
            writer.Write(packet.Data);
            return ms.ToArray();
        }
    }
    
    private GamePacket DeserializePacket(byte[] data)
    {
        using (var ms = new MemoryStream(data))
        using (var reader = new BinaryReader(ms))
        {
            var type = (PacketType)reader.ReadInt32();
            var timestamp = DateTime.FromBinary(reader.ReadInt64());
            var dataLength = reader.ReadInt32();
            var packetData = reader.ReadBytes(dataLength);
            
            return new GamePacket
            {
                Type = type,
                Timestamp = timestamp,
                Data = packetData
            };
        }
    }
}
```

### 토큰 기반 인증 시스템

P2P 환경에서도 초기 인증은 중앙 서버를 통해 수행하고, 이후 피어 간 통신에서는 토큰을 사용할 수 있습니다.

```csharp
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public class P2PAuthenticationManager
{
    private readonly Dictionary<string, PeerAuthInfo> _authenticatedPeers;
    private readonly RSA _serverPrivateKey;
    private readonly RSA _serverPublicKey;
    
    public P2PAuthenticationManager()
    {
        _authenticatedPeers = new Dictionary<string, PeerAuthInfo>();
        _serverPrivateKey = RSA.Create(2048);
        _serverPublicKey = RSA.Create();
        _serverPublicKey.ImportRSAPublicKey(_serverPrivateKey.ExportRSAPublicKey(), out _);
    }
    
    public AuthenticationToken GenerateToken(string playerId, TimeSpan validity)
    {
        var token = new AuthenticationToken
        {
            PlayerId = playerId,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(validity),
            TokenId = Guid.NewGuid().ToString()
        };
        
        var tokenJson = JsonSerializer.Serialize(token);
        var tokenBytes = Encoding.UTF8.GetBytes(tokenJson);
        var signature = _serverPrivateKey.SignData(tokenBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        
        token.Signature = Convert.ToBase64String(signature);
        
        _authenticatedPeers[playerId] = new PeerAuthInfo
        {
            Token = token,
            LastActivity = DateTime.UtcNow
        };
        
        return token;
    }
    
    public bool ValidateToken(AuthenticationToken token)
    {
        try
        {
            // 토큰 만료 검사
            if (DateTime.UtcNow > token.ExpiresAt)
            {
                return false;
            }
            
            // 서명 검증
            var tokenCopy = new AuthenticationToken
            {
                PlayerId = token.PlayerId,
                IssuedAt = token.IssuedAt,
                ExpiresAt = token.ExpiresAt,
                TokenId = token.TokenId
            };
            
            var tokenJson = JsonSerializer.Serialize(tokenCopy);
            var tokenBytes = Encoding.UTF8.GetBytes(tokenJson);
            var signature = Convert.FromBase64String(token.Signature);
            
            return _serverPublicKey.VerifyData(tokenBytes, signature, 
                HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }
    
    public bool AuthenticatePeerConnection(string fromPlayerId, string toPlayerId, AuthenticationToken token)
    {
        if (!ValidateToken(token) || token.PlayerId != fromPlayerId)
        {
            return false;
        }
        
        if (_authenticatedPeers.ContainsKey(fromPlayerId))
        {
            _authenticatedPeers[fromPlayerId].LastActivity = DateTime.UtcNow;
            return true;
        }
        
        return false;
    }
    
    public byte[] GetServerPublicKey()
    {
        return _serverPublicKey.ExportRSAPublicKey();
    }
}

public class AuthenticationToken
{
    public string PlayerId { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string TokenId { get; set; }
    public string Signature { get; set; }
}

public class PeerAuthInfo
{
    public AuthenticationToken Token { get; set; }
    public DateTime LastActivity { get; set; }
}
```

## 11.3 서버 기반 검증과 P2P 하이브리드

완전한 P2P 방식의 한계를 극복하기 위해 중요한 게임 이벤트는 서버에서 검증하는 하이브리드 방식을 구현할 수 있습니다.

```csharp
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class HybridGameValidator
{
    private readonly HttpClient _httpClient;
    private readonly string _validationServerUrl;
    private readonly Queue<GameAction> _pendingValidations;
    private readonly Dictionary<string, ValidationResult> _validationCache;
    
    public HybridGameValidator(string validationServerUrl)
    {
        _httpClient = new HttpClient();
        _validationServerUrl = validationServerUrl;
        _pendingValidations = new Queue<GameAction>();
        _validationCache = new Dictionary<string, ValidationResult>();
    }
    
    public async Task<bool> ValidateGameAction(GameAction action)
    {
        // 로컬 유효성 검사 먼저 수행
        if (!ValidateLocally(action))
        {
            return false;
        }
        
        // 중요한 액션은 서버 검증 필요
        if (RequiresServerValidation(action))
        {
            return await ValidateOnServer(action);
        }
        
        return true;
    }
    
    private bool ValidateLocally(GameAction action)
    {
        switch (action.Type)
        {
            case GameActionType.PlayerMove:
                return ValidatePlayerMovement(action);
            case GameActionType.Attack:
                return ValidateAttackAction(action);
            case GameActionType.UseItem:
                return ValidateItemUsage(action);
            default:
                return true;
        }
    }
    
    private bool ValidatePlayerMovement(GameAction action)
    {
        var moveData = JsonSerializer.Deserialize<PlayerMoveData>(action.Data);
        
        // 이동 속도 검증
        var maxSpeed = 10.0f; // 초당 최대 이동 거리
        var timeDelta = (DateTime.UtcNow - action.Timestamp).TotalSeconds;
        var distance = CalculateDistance(moveData.FromPosition, moveData.ToPosition);
        
        return distance <= maxSpeed * timeDelta;
    }
    
    private bool ValidateAttackAction(GameAction action)
    {
        var attackData = JsonSerializer.Deserialize<AttackData>(action.Data);
        
        // 공격 범위 검증
        var maxAttackRange = 5.0f;
        var distance = CalculateDistance(attackData.AttackerPosition, attackData.TargetPosition);
        
        return distance <= maxAttackRange;
    }
    
    private bool ValidateItemUsage(GameAction action)
    {
        var itemData = JsonSerializer.Deserialize<ItemUsageData>(action.Data);
        
        // 아이템 쿨다운 검증
        var lastUsage = GetLastItemUsage(action.PlayerId, itemData.ItemId);
        var cooldownTime = GetItemCooldown(itemData.ItemId);
        
        return DateTime.UtcNow - lastUsage >= cooldownTime;
    }
    
    private bool RequiresServerValidation(GameAction action)
    {
        return action.Type == GameActionType.ScoreChange ||
               action.Type == GameActionType.LevelUp ||
               action.Type == GameActionType.ItemDrop;
    }
    
    private async Task<bool> ValidateOnServer(GameAction action)
    {
        try
        {
            var validationRequest = new ValidationRequest
            {
                Action = action,
                Timestamp = DateTime.UtcNow
            };
            
            var json = JsonSerializer.Serialize(validationRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_validationServerUrl}/validate", content);
            
            if (response.IsSuccessStatusCode)
            {
                var resultJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ValidationResult>(resultJson);
                
                // 결과 캐싱
                _validationCache[action.ActionId] = result;
                
                return result.IsValid;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Server validation failed: {ex.Message}");
            return false; // 서버 검증 실패 시 보수적으로 거부
        }
    }
    
    private float CalculateDistance(Vector3 pos1, Vector3 pos2)
    {
        var dx = pos1.X - pos2.X;
        var dy = pos1.Y - pos2.Y;
        var dz = pos1.Z - pos2.Z;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
    
    private DateTime GetLastItemUsage(string playerId, string itemId)
    {
        // 실제 구현에서는 데이터베이스나 캐시에서 조회
        return DateTime.UtcNow.AddMinutes(-5);
    }
    
    private TimeSpan GetItemCooldown(string itemId)
    {
        // 아이템별 쿨다운 시간 반환
        return TimeSpan.FromSeconds(10);
    }
}

public class GameAction
{
    public string ActionId { get; set; } = Guid.NewGuid().ToString();
    public string PlayerId { get; set; }
    public GameActionType Type { get; set; }
    public string Data { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public enum GameActionType
{
    PlayerMove,
    Attack,
    UseItem,
    ScoreChange,
    LevelUp,
    ItemDrop
}

public class PlayerMoveData
{
    public Vector3 FromPosition { get; set; }
    public Vector3 ToPosition { get; set; }
    public float Speed { get; set; }
}

public class AttackData
{
    public Vector3 AttackerPosition { get; set; }
    public Vector3 TargetPosition { get; set; }
    public string WeaponId { get; set; }
}

public class ItemUsageData
{
    public string ItemId { get; set; }
    public Vector3 PlayerPosition { get; set; }
}

public struct Vector3
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}

public class ValidationRequest
{
    public GameAction Action { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string Reason { get; set; }
    public DateTime ValidatedAt { get; set; }
}
```

## 11.4 DDoS 공격 방어 전략

P2P 환경에서는 각 피어가 다른 피어들로부터 직접 트래픽을 받기 때문에 DDoS 공격에 취약할 수 있습니다.

```csharp
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

public class DDoSProtectionManager
{
    private readonly ConcurrentDictionary<IPAddress, ConnectionInfo> _connectionTracker;
    private readonly Timer _cleanupTimer;
    private readonly RateLimiter _rateLimiter;
    
    // 보호 설정
    private readonly int _maxConnectionsPerIP = 5;
    private readonly int _maxPacketsPerSecond = 50;
    private readonly TimeSpan _banDuration = TimeSpan.FromMinutes(30);
    
    public DDoSProtectionManager()
    {
        _connectionTracker = new ConcurrentDictionary<IPAddress, ConnectionInfo>();
        _rateLimiter = new RateLimiter();
        
        // 주기적으로 오래된 연결 정보 정리
        _cleanupTimer = new Timer(CleanupConnections, null, 
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }
    
    public bool IsConnectionAllowed(IPEndPoint remoteEndPoint)
    {
        var ip = remoteEndPoint.Address;
        
        // IP별 연결 수 제한 검사
        var connectionInfo = _connectionTracker.GetOrAdd(ip, _ => new ConnectionInfo());
        
        lock (connectionInfo)
        {
            // 밴된 IP 검사
            if (connectionInfo.IsBanned && DateTime.UtcNow < connectionInfo.BanExpiry)
            {
                return false;
            }
            
            // 밴 만료 시 초기화
            if (connectionInfo.IsBanned && DateTime.UtcNow >= connectionInfo.BanExpiry)
            {
                connectionInfo.Reset();
            }
            
            // 연결 수 제한 검사
            if (connectionInfo.ActiveConnections >= _maxConnectionsPerIP)
            {
                connectionInfo.SuspiciousActivity++;
                CheckForBan(connectionInfo);
                return false;
            }
            
            connectionInfo.ActiveConnections++;
            connectionInfo.LastActivity = DateTime.UtcNow;
            return true;
        }
    }
    
    public bool IsPacketAllowed(IPEndPoint remoteEndPoint, int packetSize)
    {
        var ip = remoteEndPoint.Address;
        
        // Rate limiting 검사
        if (!_rateLimiter.IsAllowed(ip, packetSize))
        {
            var connectionInfo = _connectionTracker.GetOrAdd(ip, _ => new ConnectionInfo());
            lock (connectionInfo)
            {
                connectionInfo.SuspiciousActivity++;
                CheckForBan(connectionInfo);
            }
            return false;
        }
        
        return true;
    }
    
    public void RecordPacketReceived(IPEndPoint remoteEndPoint, int packetSize)
    {
        var ip = remoteEndPoint.Address;
        var connectionInfo = _connectionTracker.GetOrAdd(ip, _ => new ConnectionInfo());
        
        lock (connectionInfo)
        {
            connectionInfo.PacketsReceived++;
            connectionInfo.BytesReceived += packetSize;
            connectionInfo.LastActivity = DateTime.UtcNow;
            
            // 패킷 크기 이상 검사
            if (packetSize > 65536) // 64KB 초과
            {
                connectionInfo.SuspiciousActivity++;
                CheckForBan(connectionInfo);
            }
        }
    }
    
    public void ConnectionClosed(IPEndPoint remoteEndPoint)
    {
        var ip = remoteEndPoint.Address;
        if (_connectionTracker.TryGetValue(ip, out var connectionInfo))
        {
            lock (connectionInfo)
            {
                connectionInfo.ActiveConnections = Math.Max(0, connectionInfo.ActiveConnections - 1);
            }
        }
    }
    
    private void CheckForBan(ConnectionInfo connectionInfo)
    {
        // 의심스러운 활동이 임계값을 초과하면 밴
        if (connectionInfo.SuspiciousActivity > 10)
        {
            connectionInfo.IsBanned = true;
            connectionInfo.BanExpiry = DateTime.UtcNow.Add(_banDuration);
            OnIPBanned?.Invoke(connectionInfo);
        }
    }
    
    private void CleanupConnections(object state)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-1);
        var keysToRemove = new List<IPAddress>();
        
        foreach (var kvp in _connectionTracker)
        {
            var connectionInfo = kvp.Value;
            lock (connectionInfo)
            {
                if (connectionInfo.LastActivity < cutoffTime && 
                    connectionInfo.ActiveConnections == 0 && 
                    !connectionInfo.IsBanned)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
        }
        
        foreach (var key in keysToRemove)
        {
            _connectionTracker.TryRemove(key, out _);
        }
    }
    
    public event Action<ConnectionInfo> OnIPBanned;
}

public class ConnectionInfo
{
    public int ActiveConnections { get; set; }
    public int PacketsReceived { get; set; }
    public long BytesReceived { get; set; }
    public int SuspiciousActivity { get; set; }
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    public bool IsBanned { get; set; }
    public DateTime BanExpiry { get; set; }
    
    public void Reset()
    {
        SuspiciousActivity = 0;
        IsBanned = false;
        BanExpiry = DateTime.MinValue;
    }
}

public class RateLimiter
{
    private readonly ConcurrentDictionary<IPAddress, TokenBucket> _buckets;
    
    public RateLimiter()
    {
        _buckets = new ConcurrentDictionary<IPAddress, TokenBucket>();
    }
    
    public bool IsAllowed(IPAddress ip, int tokens = 1)
    {
        var bucket = _buckets.GetOrAdd(ip, _ => new TokenBucket(50, 50)); // 50 토큰, 초당 50개 충전
        return bucket.TryConsume(tokens);
    }
}

public class TokenBucket
{
    private readonly int _capacity;
    private readonly int _refillRate;
    private int _tokens;
    private DateTime _lastRefill;
    private readonly object _lock = new object();
    
    public TokenBucket(int capacity, int refillRate)
    {
        _capacity = capacity;
        _refillRate = refillRate;
        _tokens = capacity;
        _lastRefill = DateTime.UtcNow;
    }
    
    public bool TryConsume(int tokens)
    {
        lock (_lock)
        {
            Refill();
            
            if (_tokens >= tokens)
            {
                _tokens -= tokens;
                return true;
            }
            
            return false;
        }
    }
    
    private void Refill()
    {
        var now = DateTime.UtcNow;
        var timePassed = now - _lastRefill;
        var tokensToAdd = (int)(timePassed.TotalSeconds * _refillRate);
        
        if (tokensToAdd > 0)
        {
            _tokens = Math.Min(_capacity, _tokens + tokensToAdd);
            _lastRefill = now;
        }
    }
}
```

### 통합 보안 시스템 사용 예제

```csharp
public class SecureP2PGameClient
{
    private readonly SecurityThreatDetector _threatDetector;
    private readonly GameCryptography _cryptography;
    private readonly P2PAuthenticationManager _authManager;
    private readonly HybridGameValidator _gameValidator;
    private readonly DDoSProtectionManager _ddosProtection;
    
    public SecureP2PGameClient()
    {
        _threatDetector = new SecurityThreatDetector();
        _cryptography = new GameCryptography();
        _authManager = new P2PAuthenticationManager();
        _gameValidator = new HybridGameValidator("https://game-validation-server.com");
        _ddosProtection = new DDoSProtectionManager();
        
        // 보안 이벤트 핸들러 등록
        _threatDetector.OnSecurityThreatDetected += HandleSecurityThreat;
        _ddosProtection.OnIPBanned += HandleIPBanned;
    }
    
    public async Task<bool> ProcessIncomingPacket(IPEndPoint sender, byte[] data)
    {
        // DDoS 보호 검사
        if (!_ddosProtection.IsConnectionAllowed(sender) || 
            !_ddosProtection.IsPacketAllowed(sender, data.Length))
        {
            Console.WriteLine($"Packet from {sender} blocked by DDoS protection");
            return false;
        }
        
        try
        {
            // 패킷 복호화
            var packet = _cryptography.DecryptPacket(data);
            
            // 보안 위협 탐지
            _threatDetector.RecordPacketReceived(sender, packet);
            
            // 게임 액션 검증
            if (packet.Type == PacketType.PlayerMove)
            {
                var gameAction = JsonSerializer.Deserialize<GameAction>(
                    Encoding.UTF8.GetString(packet.Data));
                
                if (!await _gameValidator.ValidateGameAction(gameAction))
                {
                    Console.WriteLine($"Invalid game action from {sender}");
                    return false;
                }
            }
            
            // 패킷 수신 기록
            _ddosProtection.RecordPacketReceived(sender, data.Length);
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing packet from {sender}: {ex.Message}");
            return false;
        }
    }
    
    private void HandleSecurityThreat(IPEndPoint peer, SecurityThreatType threatType)
    {
        Console.WriteLine($"Security threat detected from {peer}: {threatType}");
        
        switch (threatType)
        {
            case SecurityThreatType.PacketFlooding:
                // 해당 피어와의 연결 임시 차단
                BlockPeerTemporarily(peer, TimeSpan.FromMinutes(5));
                break;
            case SecurityThreatType.PacketTampering:
                // 해당 피어와의 연결 영구 차단
                BlockPeerPermanently(peer);
                break;
        }
    }
    
    private void HandleIPBanned(ConnectionInfo connectionInfo)
    {
        Console.WriteLine($"IP banned due to suspicious activity. Ban expires at: {connectionInfo.BanExpiry}");
    }
    
    private void BlockPeerTemporarily(IPEndPoint peer, TimeSpan duration)
    {
        // 임시 차단 로직 구현
        Console.WriteLine($"Temporarily blocking {peer} for {duration}");
    }
    
    private void BlockPeerPermanently(IPEndPoint peer)
    {
        // 영구 차단 로직 구현
        Console.WriteLine($"Permanently blocking {peer}");
    }
}
```

이 장에서는 P2P 온라인 게임의 주요 보안 위협과 대응 방법들을 다뤘습니다. 완벽한 보안은 불가능하지만, 다층 보안 접근법을 통해 대부분의 위협을 효과적으로 방어할 수 있습니다. 특히 P2P 환경에서는 성능과 보안의 균형을 잘 맞춰야 하며, 중요한 게임 이벤트에 대해서는 서버 기반 검증을 병행하는 하이브리드 방식이 효과적입니다.  