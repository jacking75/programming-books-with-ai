using System.Collections.Concurrent;
using System.Timers;


#pragma warning disable CS8622

namespace OmokServer;

public class HeartbeatManager
{
    private int interval;
    private int partitions;
    private System.Timers.Timer timer;
    private ConcurrentDictionary<string, DateTime> lastHeartbeatTimes = new ConcurrentDictionary<string, DateTime>();
    private int currentPartition = 0;

    public HeartbeatManager(int intervalMilliseconds, int checkPartitions)
    {
        interval = intervalMilliseconds / checkPartitions; // 총 인터벌을 파티션 수로 나눈다.
        partitions = checkPartitions;
        timer = new System.Timers.Timer(interval);
        timer.Elapsed += CheckHeartbeat;
        timer.AutoReset = true;
        timer.Enabled = true;
    }

    public void RegisterUser(string sessionId)
    {
        lastHeartbeatTimes.TryAdd(sessionId, DateTime.UtcNow);
    }

    public void UnregisterUser(string sessionId)
    {
        lastHeartbeatTimes.TryRemove(sessionId, out var _);
    }

    public void UpdateHeartbeat(string sessionId)
    {
        lastHeartbeatTimes[sessionId] = DateTime.UtcNow;
    }

    private void CheckHeartbeat(object sender, ElapsedEventArgs e)
    {
        var currentTime = DateTime.UtcNow;
        foreach (var kvp in lastHeartbeatTimes)
        {
            if ((kvp.Key.GetHashCode() % partitions) == currentPartition) // 현재 파티션에 해당하는 유저만 검사
            {
                var lastTime = kvp.Value;
                if ((currentTime - lastTime).TotalMilliseconds > interval * partitions)
                {
                    Console.WriteLine($"User {kvp.Key} has timed out.");
                    // 유저 타임아웃 로직 처리
                }
            }
        }

        // 다음 파티션으로 이동
        currentPartition = (currentPartition + 1) % partitions;
    }
}