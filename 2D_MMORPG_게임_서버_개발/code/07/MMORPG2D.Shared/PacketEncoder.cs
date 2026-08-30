using System.Buffers.Binary;
using MemoryPack;

namespace MMORPG2D.Shared;

/// <summary>
/// [Length(2)][PacketId(2)][Body(N)] 형태의 패킷을 만들고/푸는 공용 헬퍼.
/// 송신 측은 Encode, 수신 측은 직접 Length/PacketId를 읽고 본문은 MemoryPack 으로 역직렬화한다.
/// </summary>
public static class PacketEncoder
{
    public const int HeaderSize = 4;       // Length(2) + PacketId(2)
    public const int LengthFieldSize = 2;
    public const int PacketIdFieldSize = 2;
    public const int MaxPacketSize = ushort.MaxValue;

    /// <summary>
    /// MemoryPack 직렬화 + 길이/ID 헤더 부착. 학습용으로 매번 새 byte[] 를 할당한다.
    /// 8주차 최적화에서 ArrayPool 로 옮긴다.
    /// </summary>
    public static byte[] Encode<T>(PacketId id, T body)
    {
        var bodyBytes = MemoryPackSerializer.Serialize(body);
        var total = HeaderSize + bodyBytes.Length;
        if (total > MaxPacketSize)
            throw new InvalidOperationException($"packet too big: {total}");

        var buf = new byte[total];
        BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(0, 2), (ushort)total);
        BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(2, 2), (ushort)id);
        bodyBytes.CopyTo(buf.AsSpan(HeaderSize));
        return buf;
    }

    public static ushort ReadTotalLength(ReadOnlySpan<byte> header)
        => BinaryPrimitives.ReadUInt16LittleEndian(header[..LengthFieldSize]);

    public static PacketId ReadPacketId(ReadOnlySpan<byte> header)
        => (PacketId)BinaryPrimitives.ReadUInt16LittleEndian(
            header.Slice(LengthFieldSize, PacketIdFieldSize));
}
