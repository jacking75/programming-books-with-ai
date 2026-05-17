using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PvPGameServer.Ecs.Components;

/// <summary>
/// Immutable snapshot describing a single player that currently belongs to a room.
/// </summary>
public readonly record struct RoomOccupant(int PlayerEntityId, string UserId, string SessionId);

/// <summary>
/// Tracks the occupants of a room and exposes lookup helpers by multiple keys.
/// The component also preserves the order in which players entered so that a room owner can be determined.
/// </summary>
public sealed class RoomOccupancyComponent : IComponent
{
    readonly Dictionary<int, RoomOccupant> _occupantsByEntityId = new();
    readonly Dictionary<string, int> _entityIdBySessionId = new(StringComparer.Ordinal);
    readonly Dictionary<string, int> _entityIdByUserId = new(StringComparer.Ordinal);
    readonly List<int> _entryOrderByEntityId = new();

#if NET9_0_OR_GREATER
    readonly Lock _lock = new();
    private IDisposable EnterScope() => _lock.EnterScope();
#else
    readonly object _syncRoot = new();
    private IDisposable EnterScope()
    {
        Monitor.Enter(_syncRoot);
        return new MonitorScope(_syncRoot);
    }

    sealed class MonitorScope : IDisposable
    {
        readonly object _target;

        public MonitorScope(object target)
        {
            _target = target;
        }

        public void Dispose()
        {
            Monitor.Exit(_target);
        }
    }
#endif

    public int Count
    {
        get
        {
            using var scope = EnterScope();
            return _occupantsByEntityId.Count;
        }
    }

    public bool IsEmpty => Count == 0;

    public bool ContainsEntity(int playerEntityId)
    {
        using var scope = EnterScope();
        return _occupantsByEntityId.ContainsKey(playerEntityId);
    }

    public bool ContainsSession(string sessionId)
    {
        using var scope = EnterScope();
        return _entityIdBySessionId.ContainsKey(sessionId);
    }

    public bool ContainsUser(string userId)
    {
        using var scope = EnterScope();
        return _entityIdByUserId.ContainsKey(userId);
    }

    public bool TryAdd(RoomOccupant occupant)
    {
        using var scope = EnterScope();

        if (_occupantsByEntityId.ContainsKey(occupant.PlayerEntityId) ||
            _entityIdBySessionId.ContainsKey(occupant.SessionId) ||
            _entityIdByUserId.ContainsKey(occupant.UserId))
        {
            return false;
        }

        _occupantsByEntityId.Add(occupant.PlayerEntityId, occupant);
        _entityIdBySessionId.Add(occupant.SessionId, occupant.PlayerEntityId);
        _entityIdByUserId.Add(occupant.UserId, occupant.PlayerEntityId);
        _entryOrderByEntityId.Add(occupant.PlayerEntityId);
        return true;
    }

    public bool RemoveByEntityId(int playerEntityId, out RoomOccupant removed)
    {
        using var scope = EnterScope();

        if (_occupantsByEntityId.Remove(playerEntityId, out removed) == false)
        {
            removed = default;
            return false;
        }

        _entityIdBySessionId.Remove(removed.SessionId);
        _entityIdByUserId.Remove(removed.UserId);
        _entryOrderByEntityId.Remove(playerEntityId);
        return true;
    }

    public bool TryGetBySession(string sessionId, out RoomOccupant occupant)
    {
        using var scope = EnterScope();

        if (_entityIdBySessionId.TryGetValue(sessionId, out var entityId) &&
            _occupantsByEntityId.TryGetValue(entityId, out occupant))
        {
            return true;
        }

        occupant = default;
        return false;
    }

    public bool TryGetByUserId(string userId, out RoomOccupant occupant)
    {
        using var scope = EnterScope();

        if (_entityIdByUserId.TryGetValue(userId, out var entityId) &&
            _occupantsByEntityId.TryGetValue(entityId, out occupant))
        {
            return true;
        }

        occupant = default;
        return false;
    }

    public IReadOnlyCollection<RoomOccupant> GetSnapshot()
    {
        using var scope = EnterScope();

        if (_entryOrderByEntityId.Count == 0)
        {
            return Array.Empty<RoomOccupant>();
        }

        var snapshot = new List<RoomOccupant>(_entryOrderByEntityId.Count);
        foreach (var entityId in _entryOrderByEntityId)
        {
            if (_occupantsByEntityId.TryGetValue(entityId, out var occupant))
            {
                snapshot.Add(occupant);
            }
        }

        return snapshot;
    }

    public bool TryGetLeader(out RoomOccupant leader)
    {
        using var scope = EnterScope();

        if (_entryOrderByEntityId.Count == 0)
        {
            leader = default;
            return false;
        }

        var leaderEntityId = _entryOrderByEntityId[0];
        if (_occupantsByEntityId.TryGetValue(leaderEntityId, out leader))
        {
            return true;
        }

        leader = default;
        return false;
    }

    public void Clear()
    {
        using var scope = EnterScope();
        _occupantsByEntityId.Clear();
        _entityIdBySessionId.Clear();
        _entityIdByUserId.Clear();
        _entryOrderByEntityId.Clear();
    }
}
