using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECSNetServer.ECS;


// 엔티티 ID 타입
public readonly struct EntityId : IEquatable<EntityId>
{
    public readonly int Id;

    public EntityId(int id)
    {
        Id = id;
    }

    public bool Equals(EntityId other) => Id == other.Id;
    public override bool Equals(object obj) => obj is EntityId other && Equals(other);
    public override int GetHashCode() => Id;
    public static bool operator ==(EntityId left, EntityId right) => left.Equals(right);
    public static bool operator !=(EntityId left, EntityId right) => !left.Equals(right);

    public override string ToString() => $"Entity_{Id}";
}