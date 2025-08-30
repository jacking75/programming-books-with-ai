using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetServerLib;


public class PacketProcessor : IPacketProcessor
{
    private readonly ConcurrentDictionary<ushort, Type> _packetTypes = new();
    private readonly ConcurrentDictionary<Type, Action<IPacket, ISession>> _packetHandlers = new();

    public void RegisterPacket<T>() where T : IPacket, new()
    {
        var instance = new T();
        if (!_packetTypes.TryAdd(instance.Id, typeof(T)))
        {
            Console.WriteLine($"Warning: Packet with ID {instance.Id} is already registered.");
        }
    }

    public void RegisterPacketHandler<T>(Action<T, ISession> handler) where T : IPacket
    {
        Type packetType = typeof(T);
        _packetHandlers[packetType] = (packet, session) => handler((T)packet, session);
    }

    public void UnregisterPacketHandler<T>() where T : IPacket
    {
        _packetHandlers.TryRemove(typeof(T), out _);
    }

    /// <summary>
    /// async-await를 사용하지 않는 동기 버전의 패킷 처리 메서드입니다.
    /// </summary>
    public IPacket ProcessPacket(byte[] data, ISession session)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms, Encoding.UTF8);

        ushort packetId = reader.ReadUInt16();

        if (!_packetTypes.TryGetValue(packetId, out Type packetType))
        {
            throw new InvalidDataException($"Unknown packet ID: {packetId}");
        }

        var packet = (IPacket)Activator.CreateInstance(packetType);
        packet.Deserialize(reader);

        if (_packetHandlers.TryGetValue(packetType, out var handler))
        {
            // 등록된 핸들러를 동기적으로 호출합니다.
            handler(packet, session);
        }

        return packet; // Task 대신 IPacket 객체를 직접 반환합니다.
    }

    public byte[] SerializePacket(IPacket packet)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8);

        writer.Write(packet.Id);
        packet.Serialize(writer);

        return ms.ToArray();
    }
}
