using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ECSNetServer.Network;


// 패킷 헤더 구조체
public readonly struct PacketHeader
{
    public const int HeaderSize = 16;

    public readonly ushort PacketId;
    public readonly ushort PacketLength;
    public readonly ushort SequenceNumber;
    public readonly ushort Reserved;
    public readonly long Timestamp;

    public PacketHeader(ushort packetId, ushort packetLength, ushort sequenceNumber, long timestamp)
    {
        PacketId = packetId;
        PacketLength = packetLength;
        SequenceNumber = sequenceNumber;
        Reserved = 0;
        Timestamp = timestamp;
    }

    // 헤더를 바이트 배열로 직렬화
    public byte[] Serialize()
    {
        var headerBuffer = new byte[HeaderSize];

        BinaryPrimitives.WriteUInt16LittleEndian(headerBuffer.AsSpan(0, 2), PacketId);
        BinaryPrimitives.WriteUInt16LittleEndian(headerBuffer.AsSpan(2, 2), PacketLength);
        BinaryPrimitives.WriteUInt16LittleEndian(headerBuffer.AsSpan(4, 2), SequenceNumber);
        BinaryPrimitives.WriteUInt16LittleEndian(headerBuffer.AsSpan(6, 2), Reserved);
        BinaryPrimitives.WriteInt64LittleEndian(headerBuffer.AsSpan(8, 8), Timestamp);

        return headerBuffer;
    }

    // 바이트 배열에서 헤더 역직렬화
    public static PacketHeader Deserialize(ReadOnlySpan<byte> buffer)
    {
        var packetId = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(0, 2));
        var packetLength = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(2, 2));
        var sequenceNumber = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(4, 2));
        var timestamp = BinaryPrimitives.ReadInt64LittleEndian(buffer.Slice(8, 8));

        return new PacketHeader(packetId, packetLength, sequenceNumber, timestamp);
    }
}

// 기본 패킷 클래스
public abstract class Packet
{
    private static ushort _nextSequence = 0;

    public abstract ushort PacketId { get; }

    // 패킷을 바이트 배열로 직렬화
    public byte[] Serialize()
    {
        var payload = SerializePayload();
        var packetLength = (ushort)(PacketHeader.HeaderSize + payload.Length);
        var sequenceNumber = GetNextSequence();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var header = new PacketHeader(PacketId, packetLength, sequenceNumber, timestamp);
        var headerBytes = header.Serialize();

        var packet = new byte[packetLength];
        Array.Copy(headerBytes, 0, packet, 0, headerBytes.Length);
        Array.Copy(payload, 0, packet, headerBytes.Length, payload.Length);

        return packet;
    }

    // 패킷 페이로드 직렬화 (자식 클래스에서 구현)
    protected abstract byte[] SerializePayload();

    // 다음 시퀀스 번호 가져오기
    private static ushort GetNextSequence()
    {
        return _nextSequence++;
    }
}

// JSON 기반 패킷 베이스 클래스
public abstract class JsonPacket : Packet
{
    // JSON으로 페이로드 직렬화
    protected override byte[] SerializePayload()
    {
        return JsonSerializer.SerializeToUtf8Bytes(this);
    }

    // JSON에서 패킷 역직렬화
    public static T Deserialize<T>(ReadOnlySpan<byte> data) where T : JsonPacket
    {
        return JsonSerializer.Deserialize<T>(data.Slice(PacketHeader.HeaderSize));
    }
}

// 예시: 로그인 요청 패킷
public class LoginRequestPacket : JsonPacket
{
    public override ushort PacketId => 1001;
    public string Username { get; set; }
    public string Password { get; set; }
}

// 예시: 로그인 응답 패킷
public class LoginResponsePacket : JsonPacket
{
    public override ushort PacketId => 1002;
    public bool Success { get; set; }
    public string Message { get; set; }
    public int PlayerId { get; set; }
}

// 패킷 처리기 팩토리
public class PacketHandlerFactory
{
    private readonly Dictionary<ushort, Func<byte[], IPacketHandler>> _handlerFactories = new();

    public void RegisterHandler<T>(Func<T, IPacketHandler> factory) where T : JsonPacket
    {
        var packetInstance = Activator.CreateInstance<T>();
        _handlerFactories[packetInstance.PacketId] = (data) =>
        {
            var packet = JsonPacket.Deserialize<T>(data);
            return factory(packet);
        };
    }

    public IPacketHandler CreateHandler(byte[] packetData)
    {
        var header = PacketHeader.Deserialize(packetData);

        if (_handlerFactories.TryGetValue(header.PacketId, out var factory))
        {
            return factory(packetData);
        }

        return null;
    }
}
