namespace MMORPG2D.GameServer.Game;

public static class Damage
{
    [ThreadStatic] private static Random? _rng;
    private static Random Rng => _rng ??= new Random();

    public static int Calc(PlayerCharacter atk, PlayerCharacter def)
    {
        var raw = atk.Attack - def.Defense;
        if (raw < 1) raw = 1;
        var spread = Math.Max(1, raw / 5); // ~20%
        var dmg = raw + Rng.Next(-spread, spread + 1);
        return Math.Max(1, dmg);
    }
}

public static class Level
{
    public static int RequiredExp(int currentLevel)
        => 100 * currentLevel * currentLevel;

    public static bool MaybeLevelUp(PlayerCharacter p)
    {
        bool up = false;
        while (p.Exp >= RequiredExp(p.Level))
        {
            p.Exp -= RequiredExp(p.Level);
            p.Level++;
            p.MaxHp += 10;
            p.Hp = p.MaxHp;
            p.Attack += 2;
            p.Defense += 1;
            up = true;
        }
        return up;
    }
}

public static class Reward
{
    public static long ExpFor(PlayerCharacter killer, PlayerCharacter victim)
    {
        var diff = victim.Level - killer.Level;
        var bonus = diff switch
        {
            >= 3 => 2.0,
            <= -3 => 0.3,
            _ => 1.0 + diff * 0.1,
        };
        return (long)(50 * victim.Level * bonus);
    }
}
