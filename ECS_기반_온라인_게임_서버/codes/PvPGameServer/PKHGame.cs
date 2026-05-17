using MemoryPack;
using System;
using System.Collections.Generic;
using PvPGameServer.Ecs.Components;
using PvPGameServer.Ecs.Entities;
using PvPGameServer.Ecs.Systems;
using PvPGameServer.Game.Omok;

namespace PvPGameServer;

public sealed class PKHGame : PKHandler, IRoomGameCoordinator
{
    const int BoardSize = 15;
    static readonly TimeSpan TurnTimeLimit = TimeSpan.FromSeconds(30);
    static readonly TimeSpan CooldownDuration = TimeSpan.FromSeconds(10);

    readonly Dictionary<int, OmokRoomGame> _roomGames = new();
    readonly object _syncRoot = new();
    readonly Random _random = new();

    public new void Init(PlayerSystem playerSystem, RoomSystem roomSystem)
    {
        base.Init(playerSystem, roomSystem);
    }

    public void RegistPacketHandler(Dictionary<int, Action<MemoryPackBinaryRequestInfo>> packetHandlerDict)
    {
        packetHandlerDict.Add((int)PacketId.ReqRoomGameStart, HandleRequestGameStart);
        packetHandlerDict.Add((int)PacketId.ReqRoomGameStartDecision, HandleRequestGameStartDecision);
        packetHandlerDict.Add((int)PacketId.ReqRoomGamePlaceStone, HandleRequestGamePlaceStone);
    }

    public void OnPlayerEntered(RoomEntity room, RoomOccupant occupant)
    {
        var game = GetOrCreateRoomGame(room);

        lock (game.SyncRoot)
        {
            if (game.Phase == OmokGamePhase.Idle && game.PendingRequest is null && game.Cooldown is null)
            {
                return;
            }

            var packet = BuildStatePacket(game);
            var sendPacket = Serialize(packet, PacketId.NtfRoomGameState);
            NetSendFunc(occupant.SessionId, sendPacket);
        }
    }

    public void OnPlayerLeft(RoomEntity room, RoomOccupant occupant)
    {
        var game = GetOrCreateRoomGame(room);

        lock (game.SyncRoot)
        {
            HandlePendingRequestOnLeave(game, room, occupant);
            HandleActiveGameOnLeave(game, room, occupant);
        }
    }

    public void OnRoomOwnerChanged(RoomEntity room, RoomOccupant? newOwner)
    {
        var packet = new PKTNtfRoomMasterChanged
        {
            UserId = newOwner?.UserId ?? string.Empty
        };

        var sendPacket = Serialize(packet, PacketId.NtfRoomMasterChanged);
        Broadcast(room, sendPacket);
    }

    void HandleRequestGameStart(MemoryPackBinaryRequestInfo packetData)
    {
        var sessionId = packetData.SessionID;

        if (_playerSystem.TryGetPlayerBySessionId(sessionId, out var player) == false)
        {
            SendGameStartResponse(sessionId, ErrorCode.RoomEnterInvalidUser);
            return;
        }

        if (player.Membership.IsInRoom == false || player.Membership.RoomEntityId.HasValue == false)
        {
            SendGameStartResponse(sessionId, ErrorCode.GameNotInRoom);
            return;
        }

        if (_roomSystem.TryGetRoomByEntityId(player.Membership.RoomEntityId.Value, out var room) == false)
        {
            SendGameStartResponse(sessionId, ErrorCode.GameNotInRoom);
            return;
        }

        if (room.Occupancy.TryGetBySession(sessionId, out var requester) == false)
        {
            SendGameStartResponse(sessionId, ErrorCode.GameNotInRoom);
            return;
        }

        var game = GetOrCreateRoomGame(room);

        lock (game.SyncRoot)
        {
            if (game.Phase == OmokGamePhase.InProgress)
            {
                SendGameStartResponse(sessionId, ErrorCode.GameAlreadyInProgress);
                return;
            }

            if (game.Phase == OmokGamePhase.Cooldown)
            {
                SendGameStartResponse(sessionId, ErrorCode.GameCooldown);
                return;
            }

            if (game.PendingRequest.HasValue)
            {
                SendGameStartResponse(sessionId, ErrorCode.GamePendingRequestExists);
                return;
            }

            var occupants = _roomSystem.GetOccupants(room);
            if (occupants.Count < 2)
            {
                SendGameStartResponse(sessionId, ErrorCode.GameStartNotEnoughPlayers);
                return;
            }

            if (room.Occupancy.TryGetLeader(out var leader) == false)
            {
                SendGameStartResponse(sessionId, ErrorCode.GameStartNotEnoughPlayers);
                return;
            }

            if (string.Equals(leader.SessionId, requester.SessionId, StringComparison.Ordinal))
            {
                SendGameStartResponse(sessionId, ErrorCode.GameStartOnlyChallenger);
                return;
            }

            var pending = new OmokRoomGame.PendingStartRequest(
                leader.UserId,
                leader.SessionId,
                requester.UserId,
                requester.SessionId,
                DateTime.UtcNow);

            game.SetPendingRequest(pending);

            SendGameStartResponse(sessionId, ErrorCode.None);

            var statePacket = BuildStatePacket(game);
            Broadcast(room, Serialize(statePacket, PacketId.NtfRoomGameState));

            var notify = new PKTNtfRoomGameStartRequested
            {
                RequesterUserId = requester.UserId
            };

            var sendPacket = Serialize(notify, PacketId.NtfRoomGameStartRequested);
            NetSendFunc(leader.SessionId, sendPacket);
        }
    }

    void HandleRequestGameStartDecision(MemoryPackBinaryRequestInfo packetData)
    {
        var sessionId = packetData.SessionID;

        if (_playerSystem.TryGetPlayerBySessionId(sessionId, out var player) == false)
        {
            SendGameStartDecisionResponse(sessionId, ErrorCode.RoomEnterInvalidUser, false);
            return;
        }

        if (player.Membership.IsInRoom == false || player.Membership.RoomEntityId.HasValue == false)
        {
            SendGameStartDecisionResponse(sessionId, ErrorCode.GameNotInRoom, false);
            return;
        }

        if (_roomSystem.TryGetRoomByEntityId(player.Membership.RoomEntityId.Value, out var room) == false)
        {
            SendGameStartDecisionResponse(sessionId, ErrorCode.GameNotInRoom, false);
            return;
        }

        var request = MemoryPackSerializer.Deserialize<PKTReqRoomGameStartDecision>(packetData.Data);
        var game = GetOrCreateRoomGame(room);

        lock (game.SyncRoot)
        {
            if (game.PendingRequest.HasValue == false)
            {
                SendGameStartDecisionResponse(sessionId, ErrorCode.GameStartNoPendingRequest, request.Approved);
                return;
            }

            var pending = game.PendingRequest.Value;
            if (string.Equals(pending.LeaderSessionId, sessionId, StringComparison.Ordinal) == false)
            {
                SendGameStartDecisionResponse(sessionId, ErrorCode.GameStartOnlyLeaderCanApprove, request.Approved);
                return;
            }

            if (string.Equals(pending.RequesterUserId, request.RequesterUserId, StringComparison.Ordinal) == false)
            {
                SendGameStartDecisionResponse(sessionId, ErrorCode.GameStartUnknownRequester, request.Approved);
                return;
            }

            if (request.Approved == false)
            {
                game.ClearPendingRequest();
                SendGameStartDecisionResponse(sessionId, ErrorCode.None, false);

                var reject = new PKTNtfRoomGameStartRejected
                {
                    RequesterUserId = pending.RequesterUserId,
                    Reason = (short)ErrorCode.None
                };

                var sendReject = Serialize(reject, PacketId.NtfRoomGameStartRejected);
                NetSendFunc(pending.RequesterSessionId, sendReject);

                var state = BuildStatePacket(game);
                Broadcast(room, Serialize(state, PacketId.NtfRoomGameState));
                return;
            }

            if (room.Occupancy.TryGetBySession(pending.RequesterSessionId, out var requester) == false)
            {
                game.ClearPendingRequest();
                SendGameStartDecisionResponse(sessionId, ErrorCode.GameStartUnknownRequester, true);
                var stateAfterFailure = BuildStatePacket(game);
                Broadcast(room, Serialize(stateAfterFailure, PacketId.NtfRoomGameState));
                return;
            }

            if (room.Occupancy.TryGetBySession(pending.LeaderSessionId, out var leader) == false)
            {
                game.ClearPendingRequest();
                SendGameStartDecisionResponse(sessionId, ErrorCode.GameStartUnknownRequester, true);
                var stateAfterFailure = BuildStatePacket(game);
                Broadcast(room, Serialize(stateAfterFailure, PacketId.NtfRoomGameState));
                return;
            }

            var assigned = AssignStonesRandomly(leader, requester);
            game.StartGame(assigned.Black, assigned.White, () => HandleTurnTimeout(room.Id));
            SendGameStartDecisionResponse(sessionId, ErrorCode.None, true);

            var startNotify = new PKTNtfRoomGameStarted
            {
                BlackUserId = assigned.Black.UserId,
                WhiteUserId = assigned.White.UserId,
                CurrentTurnUserId = assigned.Black.UserId,
                BoardSize = BoardSize,
                TurnTimeSeconds = (int)TurnTimeLimit.TotalSeconds,
            };

            var startPacket = Serialize(startNotify, PacketId.NtfRoomGameStarted);
            Broadcast(room, startPacket);

            var turnNotify = new PKTNtfRoomGameTurnChanged
            {
                UserId = assigned.Black.UserId,
                RemainingMilliseconds = (int)TurnTimeLimit.TotalMilliseconds,
            };

            var turnPacket = Serialize(turnNotify, PacketId.NtfRoomGameTurnChanged);
            Broadcast(room, turnPacket);

            var stateSnapshot = BuildStatePacket(game);
            Broadcast(room, Serialize(stateSnapshot, PacketId.NtfRoomGameState));
        }
    }

    void HandleRequestGamePlaceStone(MemoryPackBinaryRequestInfo packetData)
    {
        var sessionId = packetData.SessionID;

        if (_playerSystem.TryGetPlayerBySessionId(sessionId, out var player) == false)
        {
            SendPlaceStoneResponse(sessionId, ErrorCode.RoomEnterInvalidUser);
            return;
        }

        if (player.Membership.IsInRoom == false || player.Membership.RoomEntityId.HasValue == false)
        {
            SendPlaceStoneResponse(sessionId, ErrorCode.GameNotInRoom);
            return;
        }

        if (_roomSystem.TryGetRoomByEntityId(player.Membership.RoomEntityId.Value, out var room) == false)
        {
            SendPlaceStoneResponse(sessionId, ErrorCode.GameNotInRoom);
            return;
        }

        var request = MemoryPackSerializer.Deserialize<PKTReqRoomGamePlaceStone>(packetData.Data);
        var game = GetOrCreateRoomGame(room);

        lock (game.SyncRoot)
        {
            if (game.Phase != OmokGamePhase.InProgress || game.ActiveGame is null)
            {
                SendPlaceStoneResponse(sessionId, ErrorCode.GameNotInProgress);
                return;
            }

            var active = game.ActiveGame;
            var currentPlayer = active.CurrentTurn == OmokStone.Black ? active.Black : active.White;
            var waitingPlayer = active.CurrentTurn == OmokStone.Black ? active.White : active.Black;

            if (string.Equals(active.Black.SessionId, sessionId, StringComparison.Ordinal) == false &&
                string.Equals(active.White.SessionId, sessionId, StringComparison.Ordinal) == false)
            {
                SendPlaceStoneResponse(sessionId, ErrorCode.GameMoveNotParticipant);
                return;
            }

            if (string.Equals(currentPlayer.SessionId, sessionId, StringComparison.Ordinal) == false)
            {
                SendPlaceStoneResponse(sessionId, ErrorCode.GameMoveNotYourTurn);
                return;
            }

            var boardResult = game.Board.TryPlaceStone(request.X, request.Y, active.CurrentTurn);
            if (boardResult.IsSuccess == false)
            {
                SendPlaceStoneResponse(sessionId, MapMoveError(boardResult.Error));
                return;
            }

            SendPlaceStoneResponse(sessionId, ErrorCode.None);

            var stonePacket = new PKTNtfRoomGameStonePlaced
            {
                UserId = currentPlayer.UserId,
                Stone = active.CurrentTurn,
                X = request.X,
                Y = request.Y
            };

            Broadcast(room, Serialize(stonePacket, PacketId.NtfRoomGameStonePlaced));

            if (boardResult.IsWin)
            {
                var result = OmokGameResult.Victory(currentPlayer.UserId, active.CurrentTurn, OmokGameEndReason.FiveInRow);
                FinishGame(game, room, result, active.Black, active.White);
                return;
            }

            if (boardResult.IsDraw)
            {
                FinishGame(game, room, OmokGameResult.Draw(), active.Black, active.White);
                return;
            }

            game.AdvanceTurn(() => HandleTurnTimeout(room.Id));
            var turnPacket = new PKTNtfRoomGameTurnChanged
            {
                UserId = waitingPlayer.UserId,
                RemainingMilliseconds = (int)TurnTimeLimit.TotalMilliseconds,
            };

            Broadcast(room, Serialize(turnPacket, PacketId.NtfRoomGameTurnChanged));

            var stateAfterMove = BuildStatePacket(game);
            Broadcast(room, Serialize(stateAfterMove, PacketId.NtfRoomGameState));
        }
    }

    void HandlePendingRequestOnLeave(OmokRoomGame game, RoomEntity room, RoomOccupant occupant)
    {
        if (game.PendingRequest.HasValue == false)
        {
            return;
        }

        var pending = game.PendingRequest.Value;
        var isLeader = string.Equals(pending.LeaderSessionId, occupant.SessionId, StringComparison.Ordinal);
        var isRequester = string.Equals(pending.RequesterSessionId, occupant.SessionId, StringComparison.Ordinal);

        if (isLeader == false && isRequester == false)
        {
            return;
        }

        game.ClearPendingRequest();

        var targetSessionId = isLeader ? pending.RequesterSessionId : pending.LeaderSessionId;
        var notify = new PKTNtfRoomGameStartRejected
        {
            RequesterUserId = pending.RequesterUserId,
            Reason = (short)ErrorCode.GameStartNoPendingRequest
        };

        NetSendFunc(targetSessionId, Serialize(notify, PacketId.NtfRoomGameStartRejected));

        var state = BuildStatePacket(game);
        Broadcast(room, Serialize(state, PacketId.NtfRoomGameState));
    }

    void HandleActiveGameOnLeave(OmokRoomGame game, RoomEntity room, RoomOccupant occupant)
    {
        if (game.Phase != OmokGamePhase.InProgress || game.ActiveGame is null)
        {
            return;
        }

        var active = game.ActiveGame;
        OmokGamePlayer? quittingPlayer = null;
        OmokGamePlayer? remainingPlayer = null;

        if (string.Equals(active.Black.SessionId, occupant.SessionId, StringComparison.Ordinal))
        {
            quittingPlayer = active.Black;
            remainingPlayer = active.White;
        }
        else if (string.Equals(active.White.SessionId, occupant.SessionId, StringComparison.Ordinal))
        {
            quittingPlayer = active.White;
            remainingPlayer = active.Black;
        }

        if (quittingPlayer is null || remainingPlayer is null)
        {
            return;
        }

        var result = OmokGameResult.Victory(remainingPlayer.UserId, remainingPlayer.Stone, OmokGameEndReason.PlayerLeft);
        FinishGame(game, room, result, active.Black, active.White);
    }

    (OmokGamePlayer Black, OmokGamePlayer White) AssignStonesRandomly(RoomOccupant leader, RoomOccupant requester)
    {
        var roll = _random.Next(0, 2);
        if (roll == 0)
        {
            return (
                new OmokGamePlayer(leader.UserId, leader.SessionId, OmokStone.Black),
                new OmokGamePlayer(requester.UserId, requester.SessionId, OmokStone.White));
        }

        return (
            new OmokGamePlayer(requester.UserId, requester.SessionId, OmokStone.Black),
            new OmokGamePlayer(leader.UserId, leader.SessionId, OmokStone.White));
    }

    void SendGameStartResponse(string sessionId, ErrorCode error)
    {
        var response = new PKTResRoomGameStart
        {
            Result = (short)error
        };

        NetSendFunc(sessionId, Serialize(response, PacketId.ResRoomGameStart));
    }

    void SendGameStartDecisionResponse(string sessionId, ErrorCode error, bool approved)
    {
        var response = new PKTResRoomGameStartDecision
        {
            Result = (short)error,
            Approved = approved
        };

        NetSendFunc(sessionId, Serialize(response, PacketId.ResRoomGameStartDecision));
    }

    void SendPlaceStoneResponse(string sessionId, ErrorCode error)
    {
        var response = new PKTResRoomGamePlaceStone
        {
            Result = (short)error
        };

        NetSendFunc(sessionId, Serialize(response, PacketId.ResRoomGamePlaceStone));
    }

    OmokRoomGame GetOrCreateRoomGame(RoomEntity room)
    {
        lock (_syncRoot)
        {
            if (_roomGames.TryGetValue(room.Id, out var game))
            {
                return game;
            }

            var created = new OmokRoomGame(room.Id, BoardSize, TurnTimeLimit, CooldownDuration);
            _roomGames.Add(room.Id, created);
            return created;
        }
    }

    byte[] Serialize<T>(T packet, PacketId packetId)
    {
        var data = MemoryPackSerializer.Serialize(packet);
        MemoryPackPacketHeader.Write(data, packetId);
        return data;
    }

    ErrorCode MapMoveError(OmokMoveError error)
    {
        return error switch
        {
            OmokMoveError.OutOfBoard => ErrorCode.GameMoveOutOfBoard,
            OmokMoveError.CellOccupied => ErrorCode.GameMoveCellOccupied,
            OmokMoveError.ForbiddenDoubleThree => ErrorCode.GameMoveForbiddenDoubleThree,
            OmokMoveError.ForbiddenDoubleFour => ErrorCode.GameMoveForbiddenDoubleFour,
            OmokMoveError.ForbiddenOverline => ErrorCode.GameMoveForbiddenOverline,
            _ => ErrorCode.GameMoveNotParticipant,
        };
    }

    void FinishGame(OmokRoomGame game, RoomEntity room, OmokGameResult result, OmokGamePlayer blackPlayer, OmokGamePlayer whitePlayer)
    {
        game.StartCooldown(result, blackPlayer, whitePlayer, () => HandleCooldownFinished(room.Id));

        var notify = new PKTNtfRoomGameEnded
        {
            WinnerUserId = result.WinnerUserId,
            WinnerStone = result.WinnerStone,
            Reason = result.Reason,
        };

        Broadcast(room, Serialize(notify, PacketId.NtfRoomGameEnded));

        var statePacket = BuildStatePacket(game);
        Broadcast(room, Serialize(statePacket, PacketId.NtfRoomGameState));
    }

    void HandleTurnTimeout(int roomEntityId)
    {
        if (_roomSystem.TryGetRoomByEntityId(roomEntityId, out var room) == false)
        {
            return;
        }

        var game = GetOrCreateRoomGame(room);

        lock (game.SyncRoot)
        {
            if (game.Phase != OmokGamePhase.InProgress || game.ActiveGame is null)
            {
                return;
            }

            var active = game.ActiveGame;
            var timedOutPlayer = active.CurrentTurn == OmokStone.Black ? active.Black : active.White;
            var nextPlayer = active.CurrentTurn == OmokStone.Black ? active.White : active.Black;

            var timeoutPacket = new PKTNtfRoomGamePlayerTimedOut
            {
                UserId = timedOutPlayer.UserId
            };

            Broadcast(room, Serialize(timeoutPacket, PacketId.NtfRoomGamePlayerTimedOut));

            game.AdvanceTurn(() => HandleTurnTimeout(roomEntityId));

            var turnPacket = new PKTNtfRoomGameTurnChanged
            {
                UserId = nextPlayer.UserId,
                RemainingMilliseconds = (int)TurnTimeLimit.TotalMilliseconds,
            };

            Broadcast(room, Serialize(turnPacket, PacketId.NtfRoomGameTurnChanged));

            var state = BuildStatePacket(game);
            Broadcast(room, Serialize(state, PacketId.NtfRoomGameState));
        }
    }

    void HandleCooldownFinished(int roomEntityId)
    {
        if (_roomSystem.TryGetRoomByEntityId(roomEntityId, out var room) == false)
        {
            return;
        }

        var game = GetOrCreateRoomGame(room);

        lock (game.SyncRoot)
        {
            if (game.Phase != OmokGamePhase.Cooldown)
            {
                return;
            }

            game.ResetToIdle();
            var statePacket = BuildStatePacket(game);
            Broadcast(room, Serialize(statePacket, PacketId.NtfRoomGameState));
        }
    }

    PKTNtfRoomGameState BuildStatePacket(OmokRoomGame game)
    {
        var packet = new PKTNtfRoomGameState
        {
            IsInProgress = game.Phase == OmokGamePhase.InProgress,
            IsPendingApproval = game.Phase == OmokGamePhase.PendingApproval,
            IsCooldown = game.Phase == OmokGamePhase.Cooldown,
            BoardSize = BoardSize,
        };

        foreach (var stone in game.Board.History)
        {
            packet.Stones.Add(new PKRoomStoneInfo
            {
                X = stone.X,
                Y = stone.Y,
                Stone = stone.Stone
            });
        }

        if (game.ActiveGame is { } active)
        {
            packet.BlackUserId = active.Black.UserId;
            packet.WhiteUserId = active.White.UserId;
            packet.CurrentTurnUserId = active.CurrentTurn == OmokStone.Black ? active.Black.UserId : active.White.UserId;
            packet.RemainingTurnMilliseconds = Math.Max(0, (int)(active.TurnDeadline - DateTime.UtcNow).TotalMilliseconds);
        }

        if (game.PendingRequest.HasValue)
        {
            packet.PendingRequesterUserId = game.PendingRequest.Value.RequesterUserId;
        }

        if (game.Cooldown is { } cooldown)
        {
            packet.BlackUserId = cooldown.Black.UserId;
            packet.WhiteUserId = cooldown.White.UserId;
            packet.CooldownRemainingMilliseconds = Math.Max(0, (int)(cooldown.EndsAt - DateTime.UtcNow).TotalMilliseconds);
            packet.Reason = cooldown.Result.Reason;
            packet.WinnerUserId = cooldown.Result.WinnerUserId;
            packet.WinnerStone = cooldown.Result.WinnerStone;
        }

        packet.BlackUserId ??= string.Empty;
        packet.WhiteUserId ??= string.Empty;
        packet.CurrentTurnUserId ??= string.Empty;
        packet.PendingRequesterUserId ??= string.Empty;
        packet.WinnerUserId ??= string.Empty;

        return packet;
    }
}
