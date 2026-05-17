using System;
using System.Threading;

namespace PvPGameServer.Game.Omok;

public sealed class OmokRoomGame : IDisposable
{
    readonly TimeSpan _turnTimeLimit;
    readonly TimeSpan _cooldown;
    readonly int _roomEntityId;

    Timer? _turnTimer;
    Timer? _cooldownTimer;

    public OmokRoomGame(int roomEntityId, int boardSize, TimeSpan turnTimeLimit, TimeSpan cooldown)
    {
        _roomEntityId = roomEntityId;
        Board = new OmokBoard(boardSize);
        _turnTimeLimit = turnTimeLimit;
        _cooldown = cooldown;
        Phase = OmokGamePhase.Idle;
    }

    public object SyncRoot { get; } = new();

    public OmokBoard Board { get; }

    public OmokGamePhase Phase { get; private set; }

    public PendingStartRequest? PendingRequest { get; private set; }

    public ActiveGameState? ActiveGame { get; private set; }

    public CooldownState? Cooldown { get; private set; }

    public void SetPendingRequest(PendingStartRequest request)
    {
        PendingRequest = request;
        Phase = OmokGamePhase.PendingApproval;
    }

    public void ClearPendingRequest()
    {
        PendingRequest = null;
        if (Phase == OmokGamePhase.PendingApproval)
        {
            Phase = OmokGamePhase.Idle;
        }
    }

    public void StartGame(OmokGamePlayer black, OmokGamePlayer white, Action timeoutCallback)
    {
        Board.Reset();
        ActiveGame = new ActiveGameState(black, white, OmokStone.Black, DateTime.UtcNow + _turnTimeLimit);
        Phase = OmokGamePhase.InProgress;
        PendingRequest = null;
        Cooldown = null;

        EnsureTurnTimer(timeoutCallback);
    }

    public void StartCooldown(OmokGameResult result, OmokGamePlayer black, OmokGamePlayer white, Action cooldownFinishedCallback)
    {
        Phase = OmokGamePhase.Cooldown;
        Cooldown = new CooldownState(result, DateTime.UtcNow + _cooldown, black, white);
        ActiveGame = null;
        DisposeTurnTimer();
        EnsureCooldownTimer(cooldownFinishedCallback);
    }

    public void ResetToIdle()
    {
        DisposeTurnTimer();
        DisposeCooldownTimer();
        Board.Reset();
        ActiveGame = null;
        PendingRequest = null;
        Cooldown = null;
        Phase = OmokGamePhase.Idle;
    }

    public void AdvanceTurn(Action timeoutCallback)
    {
        var active = ActiveGame;
        if (active is null)
        {
            return;
        }

        ActiveGame = active with
        {
            CurrentTurn = active.CurrentTurn == OmokStone.Black ? OmokStone.White : OmokStone.Black,
            TurnDeadline = DateTime.UtcNow + _turnTimeLimit,
        };

        EnsureTurnTimer(timeoutCallback);
    }

    void EnsureTurnTimer(Action timeoutCallback)
    {
        var dueTime = (int)_turnTimeLimit.TotalMilliseconds;
        if (_turnTimer == null)
        {
            _turnTimer = new Timer(_ => timeoutCallback(), null, dueTime, Timeout.Infinite);
        }
        else
        {
            _turnTimer.Change(dueTime, Timeout.Infinite);
        }
    }

    void EnsureCooldownTimer(Action cooldownFinishedCallback)
    {
        var dueTime = (int)_cooldown.TotalMilliseconds;
        if (_cooldownTimer == null)
        {
            _cooldownTimer = new Timer(_ => cooldownFinishedCallback(), null, dueTime, Timeout.Infinite);
        }
        else
        {
            _cooldownTimer.Change(dueTime, Timeout.Infinite);
        }
    }

    void DisposeTurnTimer()
    {
        _turnTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        _turnTimer?.Dispose();
        _turnTimer = null;
    }

    void DisposeCooldownTimer()
    {
        _cooldownTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        _cooldownTimer?.Dispose();
        _cooldownTimer = null;
    }

    public void Dispose()
    {
        DisposeTurnTimer();
        DisposeCooldownTimer();
    }

    public readonly record struct PendingStartRequest(string LeaderUserId, string LeaderSessionId, string RequesterUserId, string RequesterSessionId, DateTime RequestedAt);

    public sealed record ActiveGameState(OmokGamePlayer Black, OmokGamePlayer White, OmokStone CurrentTurn, DateTime TurnDeadline);

    public sealed record CooldownState(OmokGameResult Result, DateTime EndsAt, OmokGamePlayer Black, OmokGamePlayer White);
}
