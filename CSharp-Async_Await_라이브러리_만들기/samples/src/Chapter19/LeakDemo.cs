// 파일: samples/src/Chapter19/LeakDemo.cs
namespace AsyncAwaitLab.Chapter19;

public sealed class LeakDemo
{
    private readonly List<CancellationTokenRegistration> _kept = new();
    private readonly byte[] _hugeBuffer = new byte[1024 * 1024]; // 1MB
    private readonly CancellationTokenSource _global;

    public LeakDemo(CancellationTokenSource global) { _global = global; }

    public void Leak()
    {
        var ctr = _global.Token.Register(() => _hugeBuffer.AsSpan().Clear());
        _kept.Add(ctr);
    }

    public int RegistrationCount => _kept.Count;
}
