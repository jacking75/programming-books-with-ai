// 파일: samples/src/Chapter07/PipelineEcho.cs
// 책 §7.4 — Pipelines 기반 echo 루프.

using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;

namespace AsyncAwaitLab.Chapter07;

public static class PipelineEcho
{
    public static async Task EchoWithPipelinesAsync(Socket socket, CancellationToken ct)
    {
        var pipe = new Pipe();
        var fill = FillPipeAsync(socket, pipe.Writer, ct);
        var read = ReadPipeAsync(socket, pipe.Reader, ct);
        await Task.WhenAll(fill, read);
    }

    private static async Task FillPipeAsync(Socket socket, PipeWriter writer, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            Memory<byte> mem = writer.GetMemory(1024);
            int read = await socket.ReceiveAsync(mem, SocketFlags.None, ct);
            if (read == 0) break;
            writer.Advance(read);
            var result = await writer.FlushAsync(ct);
            if (result.IsCompleted) break;
        }
        await writer.CompleteAsync();
    }

    private static async Task ReadPipeAsync(Socket socket, PipeReader reader, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var result = await reader.ReadAsync(ct);
            var buffer = result.Buffer;
            foreach (var segment in buffer)
            {
                await socket.SendAsync(segment, SocketFlags.None, ct);
            }
            reader.AdvanceTo(buffer.End);
            if (result.IsCompleted) break;
        }
        await reader.CompleteAsync();
    }

    public static bool TryReadFrame(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> frame)
    {
        if (buffer.Length < 4)
        {
            frame = default;
            return false;
        }

        Span<byte> header = stackalloc byte[4];
        buffer.Slice(0, 4).CopyTo(header);
        int length = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(header);

        if (buffer.Length < 4 + length)
        {
            frame = default;
            return false;
        }

        frame = buffer.Slice(4, length);
        buffer = buffer.Slice(4 + length);
        return true;
    }
}
