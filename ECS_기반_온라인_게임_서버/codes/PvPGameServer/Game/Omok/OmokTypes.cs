namespace PvPGameServer.Game.Omok;

public enum OmokStone : byte
{
    None = 0,
    Black = 1,
    White = 2,
}

public enum OmokGameEndReason : byte
{
    None = 0,
    FiveInRow = 1,
    PlayerTimeout = 2,
    PlayerLeft = 3,
    Draw = 4,
    ForbiddenMove = 5,
}

public enum OmokForbiddenType : byte
{
    None = 0,
    DoubleThree = 1,
    DoubleFour = 2,
    Overline = 3,
}

public enum OmokMoveError : byte
{
    None = 0,
    OutOfBoard = 1,
    CellOccupied = 2,
    ForbiddenDoubleThree = 3,
    ForbiddenDoubleFour = 4,
    ForbiddenOverline = 5,
}

public enum OmokGamePhase : byte
{
    Idle = 0,
    PendingApproval = 1,
    InProgress = 2,
    Cooldown = 3,
}
