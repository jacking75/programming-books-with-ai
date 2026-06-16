// 파일: samples/src/Chapter08/ProgressCopy.cs
using System.Buffers;

namespace AsyncAwaitLab.Chapter08;

public static class ProgressCopy
{
    public static async Task CopyWithProgressAsync(
        Stream source, Stream dest, long? totalLength, IProgress<double> progress,
        CancellationToken ct)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            long copied = 0;
            while (true)
            {
                int n = await source.ReadAsync(buffer.AsMemory(), ct).ConfigureAwait(false);
                if (n == 0) return;
                await dest.WriteAsync(buffer.AsMemory(0, n), ct).ConfigureAwait(false);
                copied += n;

                if (totalLength is long total)
                    progress.Report((double)copied / total);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
