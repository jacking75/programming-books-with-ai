namespace Ch08_AsyncLocal;

public sealed record RequestInfo(string RequestId, string UserId);

public static class RequestContext
{
    private static readonly AsyncLocal<RequestInfo?> _current = new();

    public static RequestInfo? Current => _current.Value;

    public static IDisposable Begin(string requestId, string userId)
    {
        var previous = _current.Value;
        _current.Value = new RequestInfo(requestId, userId);
        return new Scope(previous);
    }

    private sealed class Scope : IDisposable
    {
        private readonly RequestInfo? _previous;
        public Scope(RequestInfo? previous) => _previous = previous;
        public void Dispose() => _current.Value = _previous;
    }
}
