using System;
using System.Collections.Generic;
using System.Text;

namespace PvPGameServer.Game.Omok;

public readonly record struct PlacedStone(int X, int Y, OmokStone Stone);

public sealed class OmokBoard
{
    const int PatternWindowRadius = 5;

    static readonly string[] OpenThreePatterns =
    {
        ".xx.x.",
        ".x.xx.",
        "..xxx.",
        ".xxx..",
    };

    static readonly string[] OpenFourPatterns =
    {
        ".xxxx.",
        "xxxx.",
        ".xxxx",
        "xxx.x",
        "x.xxx",
        "xx.xx",
    };

    readonly OmokStone[,] _cells;
    readonly List<PlacedStone> _history;

    public OmokBoard(int size)
    {
        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        Size = size;
        _cells = new OmokStone[size, size];
        _history = new List<PlacedStone>(size * size);
    }

    public int Size { get; }

    public IReadOnlyList<PlacedStone> History => _history;

    public OmokStone GetCell(int x, int y) => _cells[x, y];

    public bool IsInside(int x, int y)
    {
        return x >= 0 && y >= 0 && x < Size && y < Size;
    }

    public OmokMoveResult TryPlaceStone(int x, int y, OmokStone stone)
    {
        if (IsInside(x, y) == false)
        {
            return OmokMoveResult.Failed(OmokMoveError.OutOfBoard);
        }

        if (_cells[x, y] != OmokStone.None)
        {
            return OmokMoveResult.Failed(OmokMoveError.CellOccupied);
        }

        _cells[x, y] = stone;
        try
        {
            if (stone == OmokStone.Black)
            {
                var forbidden = EvaluateForbiddenPatterns(x, y);
                if (forbidden != OmokForbiddenType.None)
                {
                    return OmokMoveResult.Forbidden(forbidden);
                }
            }

            var maxLineLength = GetMaxLineLength(x, y, stone);
            if (stone == OmokStone.Black)
            {
                if (maxLineLength > 5)
                {
                    return OmokMoveResult.Forbidden(OmokForbiddenType.Overline);
                }

                if (maxLineLength == 5)
                {
                    CommitStone(x, y, stone);
                    return OmokMoveResult.Winner(stone);
                }
            }
            else
            {
                if (maxLineLength >= 5)
                {
                    CommitStone(x, y, stone);
                    return OmokMoveResult.Winner(stone);
                }
            }

            CommitStone(x, y, stone);

            if (_history.Count == Size * Size)
            {
                return OmokMoveResult.Draw();
            }

            return OmokMoveResult.Successful();
        }
        finally
        {
            if (_history.Count == 0 || _history[^1] != new PlacedStone(x, y, stone))
            {
                // The move was not committed; revert the cell.
                if (_cells[x, y] == stone)
                {
                    _cells[x, y] = OmokStone.None;
                }
            }
        }
    }

    public void Reset()
    {
        Array.Clear(_cells, 0, _cells.Length);
        _history.Clear();
    }

    int GetMaxLineLength(int x, int y, OmokStone stone)
    {
        var horizontal = 1 + CountDirection(x, y, -1, 0, stone) + CountDirection(x, y, 1, 0, stone);
        var vertical = 1 + CountDirection(x, y, 0, -1, stone) + CountDirection(x, y, 0, 1, stone);
        var diagonal = 1 + CountDirection(x, y, -1, -1, stone) + CountDirection(x, y, 1, 1, stone);
        var antiDiagonal = 1 + CountDirection(x, y, -1, 1, stone) + CountDirection(x, y, 1, -1, stone);
        return Math.Max(Math.Max(horizontal, vertical), Math.Max(diagonal, antiDiagonal));
    }

    int CountDirection(int x, int y, int dx, int dy, OmokStone stone)
    {
        var count = 0;
        var cx = x + dx;
        var cy = y + dy;

        while (IsInside(cx, cy) && _cells[cx, cy] == stone)
        {
            count++;
            cx += dx;
            cy += dy;
        }

        return count;
    }

    OmokForbiddenType EvaluateForbiddenPatterns(int x, int y)
    {
        var overlineLength = GetMaxLineLength(x, y, OmokStone.Black);
        if (overlineLength > 5)
        {
            return OmokForbiddenType.Overline;
        }

        var openThreeCount = 0;
        var openFourCount = 0;

        foreach (var (dx, dy) in Directions)
        {
            var line = BuildPatternLine(x, y, dx, dy);
            openThreeCount += CountPattern(line, OpenThreePatterns);
            openFourCount += CountPattern(line, OpenFourPatterns);
        }

        if (openFourCount >= 2)
        {
            return OmokForbiddenType.DoubleFour;
        }

        if (openThreeCount >= 2)
        {
            return OmokForbiddenType.DoubleThree;
        }

        return OmokForbiddenType.None;
    }

    string BuildPatternLine(int x, int y, int dx, int dy)
    {
        var buffer = new StringBuilder(PatternWindowRadius * 2 + 1);

        for (var offset = -PatternWindowRadius; offset <= PatternWindowRadius; offset++)
        {
            var cx = x + offset * dx;
            var cy = y + offset * dy;

            char token;
            if (IsInside(cx, cy) == false)
            {
                token = 'o';
            }
            else
            {
                token = _cells[cx, cy] switch
                {
                    OmokStone.None => '.',
                    OmokStone.Black => 'x',
                    OmokStone.White => 'o',
                    _ => '.',
                };
            }

            buffer.Append(token);
        }

        return buffer.ToString();
    }

    int CountPattern(string line, string[] patterns)
    {
        const int centerIndex = PatternWindowRadius;
        var count = 0;

        foreach (var pattern in patterns)
        {
            var length = pattern.Length;
            for (var start = 0; start <= line.Length - length; start++)
            {
                var endExclusive = start + length;
                if (centerIndex < start || centerIndex >= endExclusive)
                {
                    continue;
                }

                var matched = true;
                for (var i = 0; i < length; i++)
                {
                    if (pattern[i] == '*')
                    {
                        continue;
                    }

                    if (pattern[i] != line[start + i])
                    {
                        matched = false;
                        break;
                    }
                }

                if (matched)
                {
                    count++;
                }
            }
        }

        return count;
    }

    void CommitStone(int x, int y, OmokStone stone)
    {
        _history.Add(new PlacedStone(x, y, stone));
    }

    static readonly (int dx, int dy)[] Directions =
    {
        (-1, 0),
        (0, -1),
        (-1, -1),
        (-1, 1),
    };
}

public readonly struct OmokMoveResult
{
    OmokMoveResult(OmokMoveError error, bool isWin, bool isDraw, OmokStone winner, OmokForbiddenType forbidden)
    {
        Error = error;
        IsWin = isWin;
        IsDraw = isDraw;
        Winner = winner;
        ForbiddenType = forbidden;
    }

    public OmokMoveError Error { get; }
    public bool IsSuccess => Error == OmokMoveError.None;
    public bool IsWin { get; }
    public bool IsDraw { get; }
    public OmokStone Winner { get; }
    public OmokForbiddenType ForbiddenType { get; }

    public static OmokMoveResult Successful() => new(OmokMoveError.None, false, false, OmokStone.None, OmokForbiddenType.None);
    public static OmokMoveResult Winner(OmokStone winner) => new(OmokMoveError.None, true, false, winner, OmokForbiddenType.None);
    public static OmokMoveResult Draw() => new(OmokMoveError.None, false, true, OmokStone.None, OmokForbiddenType.None);
    public static OmokMoveResult Failed(OmokMoveError error) => new(error, false, false, OmokStone.None, OmokForbiddenType.None);
    public static OmokMoveResult Forbidden(OmokForbiddenType type) => type switch
    {
        OmokForbiddenType.DoubleThree => new(OmokMoveError.ForbiddenDoubleThree, false, false, OmokStone.None, type),
        OmokForbiddenType.DoubleFour => new(OmokMoveError.ForbiddenDoubleFour, false, false, OmokStone.None, type),
        OmokForbiddenType.Overline => new(OmokMoveError.ForbiddenOverline, false, false, OmokStone.None, type),
        _ => Successful(),
    };
}
