using System;

namespace PvPGameServer.Game.Omok;

public sealed record OmokGamePlayer(string UserId, string SessionId, OmokStone Stone);

public sealed record OmokGameResult(OmokGameEndReason Reason, string WinnerUserId, OmokStone WinnerStone, DateTime CompletedAt)
{
    public static OmokGameResult Victory(string winnerUserId, OmokStone stone, OmokGameEndReason reason)
        => new(reason, winnerUserId, stone, DateTime.UtcNow);

    public static OmokGameResult Draw()
        => new(OmokGameEndReason.Draw, string.Empty, OmokStone.None, DateTime.UtcNow);
}
