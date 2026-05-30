using GameLogAnalyzer.Models;

namespace GameLogAnalyzer.Simulator;

/// <summary>
/// 플레이어 프로파일을 기반으로 현실적인 게임 이벤트를 생성한다.
/// 포아송 분포(이벤트 발생 횟수), 정규분포(수치 분포)를 활용한다.
/// </summary>
public class EventGenerator
{
    private readonly Random _rng;

    // 던전 목록 (ID, 이름, 권장 레벨)
    private static readonly (string Id, string Name, int RecommendedLevel)[] _dungeons =
    [
        ("D001", "고블린 동굴",     5),
        ("D002", "언데드 묘지",    15),
        ("D003", "불의 신전",      25),
        ("D004", "얼음 미궁",      35),
        ("D005", "폭풍의 탑",      45),
        ("D006", "심연의 성채",    55),
        ("D007", "천공의 요새",    70),
        ("D008", "혼돈의 차원",    85),
        ("D009", "신들의 황혼",    95),
        ("D010", "태초의 공허",   100)
    ];

    // 아이템 목록 (ID, 이름, 카테고리, 기본 골드)
    private static readonly (string Id, string Name, string Category, int BaseGold)[] _items =
    [
        ("I001", "낡은 단검",       "weapon",   100),
        ("I002", "철제 검",         "weapon",   500),
        ("I003", "강철 대검",       "weapon",  2000),
        ("I004", "전설의 성검",     "weapon", 50000),
        ("I005", "가죽 갑옷",       "armor",    200),
        ("I006", "사슬 갑옷",       "armor",   1000),
        ("I007", "판금 갑옷",       "armor",   5000),
        ("I008", "용의 비늘 갑옷",  "armor",  80000),
        ("I009", "HP 포션(소)",     "potion",    50),
        ("I010", "HP 포션(대)",     "potion",   200),
        ("I011", "MP 포션",         "potion",   150),
        ("I012", "경험치 물약",     "potion",  1000),
        ("I013", "광석",            "material",  30),
        ("I014", "마법석",          "material", 500),
        ("I015", "신성한 정수",     "material",5000)
    ];

    // 결제 상품 목록
    private static readonly (string Id, string Name, int Krw)[] _products =
    [
        ("G001", "크리스탈 60개",      1100),
        ("G002", "크리스탈 330개",     5500),
        ("G003", "크리스탈 680개",    11000),
        ("G004", "크리스탈 1400개",   22000),
        ("G005", "크리스탈 3800개",   55000),
        ("G006", "크리스탈 8200개",  110000),
        ("G007", "월정액 패스",        9900),
        ("G008", "성장 지원 패키지",  33000),
        ("G009", "프리미엄 패키지",   99000),
        ("G010", "한정 전설 패키지", 198000)
    ];

    private static readonly string[] _regions = ["KR", "KR", "KR", "US", "JP", "EU"];
    private static readonly string[] _platforms = ["PC", "PC", "Mobile"];
    private static readonly string[] _payMethods = ["card", "card", "mobile", "gift"];
    private static readonly string[] _stores = ["Steam", "AppStore", "GooglePlay", "PC"];

    public EventGenerator(int seed = 12345)
    {
        _rng = new Random(seed);
    }

    /// <summary>
    /// 특정 날짜에 한 플레이어가 생성하는 모든 이벤트를 반환한다.
    /// </summary>
    public List<object> GenerateEventsForDay(Player player, DateTime date)
    {
        var events = new List<object>();

        // 오늘 접속할지 결정 (플레이어 활동률 기반)
        if (_rng.NextDouble() > player.ActivityRate)
            return events; // 오늘은 접속 안 함

        // 접속 이벤트 생성 (1~3회 세션)
        int sessionCount = SamplePoisson(1.5); // 평균 1.5회
        sessionCount = Math.Clamp(sessionCount, 1, 3);

        for (int s = 0; s < sessionCount; s++)
        {
            var loginTime = GetLoginTime(player.Pattern, date, s);
            var sessionSeconds = (player.AvgSessionMinutes + _rng.Next(-10, 30)) * 60;
            sessionSeconds = Math.Max(sessionSeconds, 300);

            var loginEvent = new LoginEvent
            {
                PlayerId = player.PlayerId,
                Nickname = player.Nickname,
                Timestamp = loginTime,
                Date = date.ToString("yyyy-MM-dd"),
                Level = player.Level,
                Region = _regions[_rng.Next(_regions.Length)],
                Platform = _platforms[_rng.Next(_platforms.Length)],
                SessionDurationSeconds = sessionSeconds,
                IsFirstLogin = (s == 0 && date == new DateTime(2025, 1, 1))
            };
            events.Add(loginEvent);

            // 세션 내 전투 이벤트 (포아송 분포, 평균 3회)
            int battleCount = SamplePoisson(3.0);
            var currentTime = loginTime.AddSeconds(60);

            for (int b = 0; b < battleCount; b++)
            {
                if (currentTime > loginTime.AddSeconds(sessionSeconds)) break;

                var battleEvent = GenerateBattleEvent(player, date, currentTime);
                events.Add(battleEvent);

                // 전투 후 아이템 획득
                if (_rng.NextDouble() < 0.7)
                {
                    var itemEvents = GenerateItemEvents(player, date, currentTime.AddSeconds(30), "dungeon");
                    events.AddRange(itemEvents);
                }

                currentTime = currentTime.AddSeconds(battleEvent.DurationSeconds + _rng.Next(60, 300));

                // 레벨업 체크 (간단한 시뮬레이션)
                if (_rng.NextDouble() < 0.02 && player.Level < 100)
                    player.Level++;
            }

            // 상점 아이템 구매/소비
            if (_rng.NextDouble() < 0.3)
            {
                var shopItems = GenerateItemEvents(player, date, loginTime.AddSeconds(sessionSeconds / 2), "shop");
                events.AddRange(shopItems);
            }
        }

        // 결제 이벤트 (과금 유형별 확률)
        var paymentProb = player.Type switch
        {
            PlayerType.Whale => 0.4,
            PlayerType.Heavy => 0.15,
            PlayerType.Light => 0.05,
            _ => 0.005
        };

        if (_rng.NextDouble() < paymentProb)
        {
            var payEvent = GeneratePaymentEvent(player, date);
            events.Add(payEvent);
        }

        return events;
    }

    private DateTime GetLoginTime(PlayPattern pattern, DateTime date, int sessionIndex)
    {
        // 패턴별 기본 접속 시간대
        int baseHour = pattern switch
        {
            PlayPattern.Morning => 7 + _rng.Next(0, 2),
            PlayPattern.Lunch => 12,
            PlayPattern.Evening => 18 + _rng.Next(0, 4),
            PlayPattern.Night => 22 + _rng.Next(0, 3),
            _ => _rng.Next(8, 23)
        };

        // 세션이 여러 개면 시간 간격을 둔다
        if (sessionIndex > 0) baseHour += sessionIndex * 4;
        baseHour = baseHour % 24;

        int minute = _rng.Next(0, 60);
        int second = _rng.Next(0, 60);

        return new DateTime(date.Year, date.Month, date.Day, baseHour, minute, second);
    }

    private BattleEvent GenerateBattleEvent(Player player, DateTime date, DateTime timestamp)
    {
        // 플레이어 레벨에 맞는 던전 선택
        var suitableDungeons = _dungeons
            .Where(d => d.RecommendedLevel <= player.Level + 10)
            .ToArray();

        if (suitableDungeons.Length == 0)
            suitableDungeons = [_dungeons[0]];

        var dungeon = suitableDungeons[_rng.Next(suitableDungeons.Length)];

        // 난이도는 무작위 (Normal/Hard/Extreme)
        var difficulties = new[] { "Normal", "Normal", "Hard", "Extreme" };
        var difficulty = difficulties[_rng.Next(difficulties.Length)];

        // 성공률 = 플레이어 스킬 * 난이도 보정
        double baseSuccessRate = player.BattleSkill;
        double difficultyMod = difficulty switch
        {
            "Hard" => -0.15,
            "Extreme" => -0.35,
            _ => 0.0
        };
        bool isSuccess = _rng.NextDouble() < (baseSuccessRate + difficultyMod);

        // 전투 시간 (포아송 분포, 평균 5분)
        int durationSeconds = (SamplePoisson(5) + 2) * 60;
        durationSeconds = Math.Clamp(durationSeconds, 120, 1800);

        // 데미지 수치 (정규분포 기반)
        int baseDmg = player.Level * 100 + (int)(player.BattleSkill * 5000);
        int damageDealt = (int)SampleNormal(baseDmg, baseDmg * 0.2);
        int damageTaken = isSuccess
            ? (int)SampleNormal(baseDmg * 0.3, baseDmg * 0.1)
            : (int)SampleNormal(baseDmg * 0.8, baseDmg * 0.2);

        // 보상 (성공 시만 지급)
        int goldEarned = isSuccess ? dungeon.RecommendedLevel * 10 + _rng.Next(0, 500) : 0;
        int expEarned = isSuccess ? dungeon.RecommendedLevel * 5 + _rng.Next(0, 100) : (int)(dungeon.RecommendedLevel * 1.5);

        return new BattleEvent
        {
            PlayerId = player.PlayerId,
            Timestamp = timestamp,
            Date = date.ToString("yyyy-MM-dd"),
            DungeonId = dungeon.Id,
            DungeonName = dungeon.Name,
            Difficulty = difficulty,
            IsSuccess = isSuccess,
            DurationSeconds = durationSeconds,
            DamageDealt = Math.Max(damageDealt, 100),
            DamageTaken = Math.Max(damageTaken, 0),
            GoldEarned = goldEarned,
            ExpEarned = expEarned,
            PlayerLevel = player.Level
        };
    }

    private List<ItemEvent> GenerateItemEvents(Player player, DateTime date, DateTime timestamp, string source)
    {
        var result = new List<ItemEvent>();
        int count = SamplePoisson(2.0); // 평균 2개 아이템
        count = Math.Clamp(count, 1, 5);

        for (int i = 0; i < count; i++)
        {
            var item = _items[_rng.Next(_items.Length)];
            var action = source == "dungeon" ? "acquire" :
                         source == "shop" ? "consume" :
                         _rng.NextDouble() < 0.5 ? "acquire" : "sell";

            int qty = item.Category == "potion" || item.Category == "material"
                ? _rng.Next(1, 10)
                : 1;

            result.Add(new ItemEvent
            {
                PlayerId = player.PlayerId,
                Timestamp = timestamp.AddSeconds(i * 5),
                Date = date.ToString("yyyy-MM-dd"),
                Action = action,
                ItemId = item.Id,
                ItemName = item.Name,
                ItemCategory = item.Category,
                Quantity = qty,
                GoldValue = item.BaseGold * qty + _rng.Next(0, item.BaseGold / 2),
                Source = source
            });
        }

        return result;
    }

    private PaymentEvent GeneratePaymentEvent(Player player, DateTime date)
    {
        // 과금 유형별 상품 선택 — 고래는 고가 상품 선호
        int maxProductIndex = player.Type switch
        {
            PlayerType.Whale => _products.Length - 1,
            PlayerType.Heavy => 7,
            PlayerType.Light => 4,
            _ => 2
        };

        int productIndex = _rng.Next(0, maxProductIndex + 1);
        var product = _products[productIndex];

        var store = _stores[_rng.Next(_stores.Length)];
        var method = _payMethods[_rng.Next(_payMethods.Length)];

        return new PaymentEvent
        {
            PlayerId = player.PlayerId,
            Timestamp = date.AddHours(_rng.Next(8, 22)).AddMinutes(_rng.Next(0, 60)),
            Date = date.ToString("yyyy-MM-dd"),
            ProductId = product.Id,
            ProductName = product.Name,
            AmountKrw = product.Krw,
            Currency = "KRW",
            PaymentMethod = method,
            Store = store
        };
    }

    /// <summary>
    /// 포아송 분포 샘플링 (Knuth 알고리즘)
    /// </summary>
    private int SamplePoisson(double lambda)
    {
        double L = Math.Exp(-lambda);
        double p = 1.0;
        int k = 0;

        do
        {
            k++;
            p *= _rng.NextDouble();
        } while (p > L);

        return k - 1;
    }

    /// <summary>
    /// 정규분포 샘플링 (Box-Muller 변환)
    /// </summary>
    private double SampleNormal(double mean, double stddev)
    {
        double u1 = 1.0 - _rng.NextDouble();
        double u2 = 1.0 - _rng.NextDouble();
        double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + z * stddev;
    }
}
