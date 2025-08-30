using System;
using System.Collections.Generic;
using System.Text;

namespace FreeNetLite;

/// <summary>
/// 서버의 주요 설정을 나타냅니다.
/// </summary>
public record ServerOption
{
    public int Port { get; init; } = 32451;
    public int MaxConnectionCount { get; init; } = 1000;
    public int ReceiveBufferSize { get; init; } = 4096;
    public int MaxPacketSize { get; init; } = 1024;

    public override string ToString()
    {
        return $"""
            [ServerOption]
            Port: {Port}
            MaxConnectionCount: {MaxConnectionCount}
            ReceiveBufferSize: {ReceiveBufferSize}
            MaxPacketSize: {MaxPacketSize}
            """;
    }
}
