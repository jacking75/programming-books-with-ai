using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetServerLib;


public class PacketProcessor : IPacketProcessor
{
    private readonly ConcurrentDictionary<ushort, Type> _packetTypes = new ConcurrentDictionary<ushort, Type>();
    private readonly ConcurrentDictionary<Type, Action<IPacket, ISession>> _packetHandlers = new ConcurrentDictionary<Type, Action<IPacket, ISession>>();

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

    public void HandlePacket(byte[] data, ISession session)
    {
        using (var ms = new MemoryStream(data))
        using (var reader = new BinaryReader(ms, Encoding.UTF8))
        {
            ushort packetId = reader.ReadUInt16();

            if (!_packetTypes.TryGetValue(packetId, out Type packetType))
            {
                Console.WriteLine($"Unknown packet ID: {packetId}");
                return;
            }

            var packet = (IPacket)Activator.CreateInstance(packetType);
            packet.Deserialize(reader);

            if (_packetHandlers.TryGetValue(packetType, out var handler))
            {
                handler(packet, session);
            }
        }
    }

    public byte[] SerializePacket(IPacket packet)
    {
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms, Encoding.UTF8))
        {
            writer.Write(packet.Id);
            packet.Serialize(writer);
            return ms.ToArray();
        }
    }
}
