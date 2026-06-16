// 파일: samples/src/Chapter08/RetryHandler.cs
using System.Net.Http;

namespace AsyncAwaitLab.Chapter08;

public sealed class RetryHandler : DelegatingHandler
{
    private readonly int _maxRetries;
    public RetryHandler(int maxRetries = 3) => _maxRetries = maxRetries;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                var res = await base.SendAsync(request, ct).ConfigureAwait(false);
                if ((int)res.StatusCode < 500 || attempt >= _maxRetries) return res;
                res.Dispose();
            }
            catch (HttpRequestException) when (attempt < _maxRetries)
            {
                // try again
            }

            var delay = TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt));
            await Task.Delay(delay, ct).ConfigureAwait(false);
        }
    }
}
