using GameLogAnalyzer.Models;

namespace GameLogAnalyzer.Simulator;

/// <summary>
/// 가상 플레이어 100명을 현실적인 분포로 생성한다.
/// - 무과금 60%, 소과금 25%, 중과금 12%, 고래(고과금) 3%
/// - 플레이 패턴은 저녁 집중형이 많고 나머지는 분산
/// </summary>
public static class PlayerFactory
{
    private static readonly Random _rng = new Random(42); // 재현 가능한 시드

    private static readonly string[] _nicknames =
    [
        "DragonSlayer", "NightHunter", "IronFist", "ShadowBlade", "StarLord",
        "CrimsonWolf", "FrostArrow", "ThunderKing", "DarkMage", "HolyPaladin",
        "SilverWing", "BloodRayne", "StormBreaker", "VoidWalker", "FireDancer",
        "IceMaster", "LightBringer", "DoomBringer", "SoulReaper", "GhostRider",
        "TigerClaw", "EagleEye", "BearFist", "SnakeFang", "WolfHowl",
        "MoonStar", "SunBurst", "DeepSea", "HighSky", "RedRock",
        "BlueFlame", "GreenLeaf", "PurpleRain", "WhiteSnow", "BlackHole",
        "GoldRush", "SilverMoon", "BronzeAge", "CopperKey", "IronGate",
        "AncientOne", "NewHope", "LastStand", "FirstBlood", "FinalBoss",
        "QuickDraw", "SlowBurn", "HardCore", "SoftTouch", "ColdBlood",
        "HotHead", "WarmHeart", "BraveHeart", "LionHeart", "DarkHeart",
        "LightStep", "HeavyStep", "QuickStep", "SlowStep", "SideStep",
        "UpperCut", "LowerBlow", "LeftHook", "RightCross", "BodyShot",
        "HeadShot", "BackStab", "FrontLine", "SideArm", "CrossBow",
        "LongSword", "ShortSpear", "BigShield", "SmallDagger", "TwinBlades",
        "FireBolt", "IceLance", "WindArrow", "EarthShatter", "LightBeam",
        "DarkVoid", "PureLight", "ChaosStrike", "OrderShield", "BalanceSeeker",
        "LoneWolf", "PackLeader", "HiddenAce", "WildCard", "JokerFace",
        "KingPin", "QueenBee", "RookMove", "BishopCross", "KnightRide",
        "PawnPush", "CheckMate", "Stalemate", "Endgame", "NewGame"
    ];

    private static readonly string[] _regions = ["KR", "KR", "KR", "KR", "US", "JP", "EU"];
    private static readonly string[] _platforms = ["PC", "PC", "PC", "Mobile", "Mobile"];

    public static List<Player> CreatePlayers(int count = 100)
    {
        var players = new List<Player>(count);

        for (int i = 0; i < count; i++)
        {
            var type = PickPlayerType(i, count);
            var pattern = PickPlayPattern();

            players.Add(new Player
            {
                PlayerId = $"P{i + 1:D4}",
                Nickname = _nicknames[i % _nicknames.Length],
                Level = GenerateStartingLevel(type),
                Type = type,
                Pattern = pattern,
                ActivityRate = GenerateActivityRate(type),
                BattleSkill = GenerateBattleSkill(),
                AvgSessionMinutes = GenerateAvgSessionMinutes(type, pattern)
            });
        }

        return players;
    }

    /// <summary>
    /// 과금 유형 분포: 무과금 60%, 소과금 25%, 중과금 12%, 고래 3%
    /// </summary>
    private static PlayerType PickPlayerType(int index, int total)
    {
        // 결정론적으로 분포를 맞춘다
        double ratio = (double)index / total;
        return ratio switch
        {
            < 0.60 => PlayerType.Free,
            < 0.85 => PlayerType.Light,
            < 0.97 => PlayerType.Heavy,
            _ => PlayerType.Whale
        };
    }

    private static PlayPattern PickPlayPattern()
    {
        // 저녁 45%, 캐주얼 25%, 심야 15%, 점심 10%, 아침 5%
        double r = _rng.NextDouble();
        return r switch
        {
            < 0.05 => PlayPattern.Morning,
            < 0.15 => PlayPattern.Lunch,
            < 0.60 => PlayPattern.Evening,
            < 0.75 => PlayPattern.Night,
            _ => PlayPattern.Casual
        };
    }

    private static int GenerateStartingLevel(PlayerType type)
    {
        // 과금 유형이 높을수록 레벨이 높은 경향
        return type switch
        {
            PlayerType.Whale => _rng.Next(60, 100),
            PlayerType.Heavy => _rng.Next(40, 80),
            PlayerType.Light => _rng.Next(20, 60),
            _ => _rng.Next(1, 40)
        };
    }

    private static double GenerateActivityRate(PlayerType type)
    {
        // 과금 유저일수록 더 자주 접속
        return type switch
        {
            PlayerType.Whale => 0.7 + _rng.NextDouble() * 0.3,   // 0.7~1.0
            PlayerType.Heavy => 0.5 + _rng.NextDouble() * 0.4,   // 0.5~0.9
            PlayerType.Light => 0.3 + _rng.NextDouble() * 0.4,   // 0.3~0.7
            _ => 0.1 + _rng.NextDouble() * 0.4                    // 0.1~0.5
        };
    }

    private static double GenerateBattleSkill()
    {
        // 전투 실력은 정규분포에 가깝게 (평균 0.5, 표준편차 0.2, 0.0~1.0 클리핑)
        double u1 = 1.0 - _rng.NextDouble();
        double u2 = 1.0 - _rng.NextDouble();
        double normal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        double skill = 0.5 + normal * 0.2;
        return Math.Clamp(skill, 0.05, 0.95);
    }

    private static int GenerateAvgSessionMinutes(PlayerType type, PlayPattern pattern)
    {
        int baseMinutes = type switch
        {
            PlayerType.Whale => 120,
            PlayerType.Heavy => 90,
            PlayerType.Light => 60,
            _ => 30
        };

        // 심야 패턴은 세션이 길다
        if (pattern == PlayPattern.Night) baseMinutes = (int)(baseMinutes * 1.5);

        return baseMinutes + _rng.Next(-15, 30);
    }
}
