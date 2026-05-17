using System;
using System.Collections.Generic;
using PvPGameServer.Ecs.Components;
using PvPGameServer.Ecs.Entities;

namespace PvPGameServer.Ecs.Systems;

public sealed class PlayerSystem
{
    readonly EntityWorld _world;
    readonly object _syncRoot = new();
    readonly Dictionary<string, PlayerIndex> _indexBySessionId = new(StringComparer.Ordinal);
    ulong _nextSequenceNumber;
    int _maxPlayerCount;

    public PlayerSystem(EntityWorld world)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
    }

    public void Initialize(int maxPlayerCount)
    {
        if (maxPlayerCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPlayerCount), maxPlayerCount, "Player count must be positive.");
        }

        lock (_syncRoot)
        {
            _maxPlayerCount = maxPlayerCount;
            _nextSequenceNumber = 0;
            _indexBySessionId.Clear();
        }
    }

    public int Count
    {
        get
        {
            lock (_syncRoot)
            {
                return _indexBySessionId.Count;
            }
        }
    }

    public ErrorCode TryAddPlayer(string sessionId, string userId, out PlayerEntity player)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        lock (_syncRoot)
        {
            if (_indexBySessionId.Count >= _maxPlayerCount)
            {
                player = null;
                return ErrorCode.LoginFullUserCount;
            }

            if (_indexBySessionId.ContainsKey(sessionId))
            {
                player = null;
                return ErrorCode.AddUserDuplication;
            }

            var playerEntity = PlayerEntity.Create(_world, ++_nextSequenceNumber, userId, sessionId);
            playerEntity.Lifecycle.SetState(PlayerLifecycleState.LoggedIn);

            _indexBySessionId[sessionId] = new PlayerIndex(playerEntity.Id, playerEntity.Identity.UserId);
            player = playerEntity;
            return ErrorCode.None;
        }
    }

    public bool TryGetPlayerBySessionId(string sessionId, out PlayerEntity player)
    {
        player = null;
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return false;
        }

        if (TryGetIndex(sessionId, out var index) == false)
        {
            return false;
        }

        if (_world.TryGetEntity(index.EntityId, out var entity) == false)
        {
            RemoveIndex(sessionId);
            return false;
        }

        player = new PlayerEntity(entity);
        return true;
    }

    public bool TryGetUserIdBySessionId(string sessionId, out string userId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            userId = string.Empty;
            return false;
        }

        if (TryGetIndex(sessionId, out var index))
        {
            userId = index.UserId;
            return true;
        }

        userId = string.Empty;
        return false;
    }

    public ErrorCode RemovePlayerBySessionId(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        if (TryGetIndex(sessionId, out var index) == false)
        {
            return ErrorCode.RemoveUserSearchFailureUserId;
        }

        RemoveIndex(sessionId);

        if (_world.TryGetEntity(index.EntityId, out var entity))
        {
            var player = new PlayerEntity(entity);
            if (player.Membership.IsInRoom)
            {
                player.Membership.LeaveRoom();
            }

            player.Lifecycle.SetState(PlayerLifecycleState.Disconnected);
        }

        _world.DestroyEntity(index.EntityId);
        return ErrorCode.None;
    }

    public bool ContainsSession(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        lock (_syncRoot)
        {
            return _indexBySessionId.ContainsKey(sessionId);
        }
    }

    bool TryGetIndex(string sessionId, out PlayerIndex index)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        lock (_syncRoot)
        {
            return _indexBySessionId.TryGetValue(sessionId, out index);
        }
    }

    void RemoveIndex(string sessionId)
    {
        lock (_syncRoot)
        {
            _indexBySessionId.Remove(sessionId);
        }
    }

    readonly record struct PlayerIndex(int EntityId, string UserId);
}
