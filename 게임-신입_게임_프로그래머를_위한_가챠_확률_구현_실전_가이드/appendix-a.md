# 부록 A. C# 소스 코드 모음

각 챕터에서 다룬 핵심 코드를 한곳에 모았다. 그대로 복붙해서 게임 서버에 붙일 수 있다. 의존성은 모두 .NET 10 표준이며, MySqlConnector / SqlKata.Execution / CloudStructures / MathNet.Numerics만 NuGet에서 추가하면 된다.

```xml
<!-- Game.Server.csproj -->
<ItemGroup>
  <PackageReference Include="MySqlConnector" Version="2.4.0" />
  <PackageReference Include="SqlKata" Version="3.0.0" />
  <PackageReference Include="SqlKata.Execution" Version="3.0.0" />
  <PackageReference Include="CloudStructures" Version="3.0.0" />
  <PackageReference Include="StackExchange.Redis" Version="2.8.0" />
  <PackageReference Include="MathNet.Numerics" Version="5.0.0" />
  <PackageReference Include="Dapper" Version="2.1.35" />
</ItemGroup>
```

---

## A-1. 스레드 안전 난수 유틸리티

```csharp
public interface IRandomProvider
{
    double NextDouble();
    int NextInt(int minInclusive, int maxExclusive);
    int NextInt(int maxExclusive);
    T Choose<T>(IReadOnlyList<T> items);
}

public sealed class ThreadSafeRandomProvider : IRandomProvider
{
    public double NextDouble() => Random.Shared.NextDouble();
    public int NextInt(int min, int max) => Random.Shared.Next(min, max);
    public int NextInt(int max) => Random.Shared.Next(max);
    public T Choose<T>(IReadOnlyList<T> items) => items[Random.Shared.Next(items.Count)];
}

public sealed class SeededRandomProvider : IRandomProvider
{
    private readonly Random _rng;
    private readonly object _lock = new();
    public SeededRandomProvider(int seed) => _rng = new Random(seed);
    public double NextDouble() { lock(_lock) return _rng.NextDouble(); }
    public int NextInt(int min, int max) { lock(_lock) return _rng.Next(min, max); }
    public int NextInt(int max) { lock(_lock) return _rng.Next(max); }
    public T Choose<T>(IReadOnlyList<T> items) { lock(_lock) return items[_rng.Next(items.Count)]; }
}

public sealed class QueuedRandomProvider : IRandomProvider
{
    private readonly Queue<double> _doubles;
    private readonly Queue<int> _ints;
    public QueuedRandomProvider(IEnumerable<double>? doubles = null, IEnumerable<int>? ints = null)
    {
        _doubles = new(doubles ?? Array.Empty<double>());
        _ints = new(ints ?? Array.Empty<int>());
    }
    public double NextDouble() => _doubles.Dequeue();
    public int NextInt(int min, int max) => _ints.Dequeue();
    public int NextInt(int max) => NextInt(0, max);
    public T Choose<T>(IReadOnlyList<T> items) => items[NextInt(items.Count)];
}
```

---

## A-2. 룰렛 휠 선택기

```csharp
public sealed class RouletteWheelPicker<T> : IWeightedPicker<T>
{
    private readonly List<(T Item, double Weight)> _items;
    private readonly double _total;
    private readonly IRandomProvider _rng;

    public RouletteWheelPicker(IEnumerable<(T, double)> items, IRandomProvider rng)
    {
        _items = items.ToList();
        if (_items.Count == 0) throw new ArgumentException("Empty pool");
        if (_items.Any(x => x.Weight <= 0)) throw new ArgumentException("Weights must be > 0");
        _total = _items.Sum(x => x.Weight);
        _rng = rng;
    }

    public T Pick()
    {
        var roll = _rng.NextDouble() * _total;
        double cum = 0;
        foreach (var (item, w) in _items) { cum += w; if (roll < cum) return item; }
        return _items[^1].Item;
    }
}

public interface IWeightedPicker<T> { T Pick(); }
```

---

## A-3. 누적 확률 + 이진 탐색 선택기

```csharp
public sealed class CumulativeBinarySearchPicker<T> : IWeightedPicker<T>
{
    private readonly T[] _items;
    private readonly double[] _cumulative;
    private readonly double _total;
    private readonly IRandomProvider _rng;

    public CumulativeBinarySearchPicker(IEnumerable<(T, double)> items, IRandomProvider rng)
    {
        var arr = items.ToArray();
        _items = new T[arr.Length];
        _cumulative = new double[arr.Length];
        double sum = 0;
        for (int i = 0; i < arr.Length; i++)
        {
            _items[i] = arr[i].Item1;
            sum += arr[i].Item2;
            _cumulative[i] = sum;
        }
        _total = sum;
        _rng = rng;
    }

    public T Pick()
    {
        var roll = _rng.NextDouble() * _total;
        int idx = Array.BinarySearch(_cumulative, roll);
        if (idx < 0) idx = ~idx;
        if (idx >= _items.Length) idx = _items.Length - 1;
        return _items[idx];
    }
}
```

---

## A-4. Alias Method 선택기

```csharp
public sealed class AliasMethodPicker<T> : IWeightedPicker<T>
{
    private readonly T[] _items;
    private readonly double[] _prob;
    private readonly int[] _alias;
    private readonly IRandomProvider _rng;

    public AliasMethodPicker(IEnumerable<(T, double)> items, IRandomProvider rng)
    {
        var arr = items.ToArray();
        int n = arr.Length;
        _items = arr.Select(x => x.Item1).ToArray();
        _prob = new double[n];
        _alias = new int[n];
        _rng = rng;

        double total = arr.Sum(x => x.Item2);
        var scaled = arr.Select(x => x.Item2 * n / total).ToArray();
        var small = new Stack<int>();
        var large = new Stack<int>();
        for (int i = 0; i < n; i++)
            (scaled[i] < 1.0 ? small : large).Push(i);

        while (small.Count > 0 && large.Count > 0)
        {
            int s = small.Pop(), l = large.Pop();
            _prob[s] = scaled[s];
            _alias[s] = l;
            scaled[l] = scaled[l] + scaled[s] - 1.0;
            (scaled[l] < 1.0 ? small : large).Push(l);
        }
        while (large.Count > 0) _prob[large.Pop()] = 1.0;
        while (small.Count > 0) _prob[small.Pop()] = 1.0;
    }

    public T Pick()
    {
        int idx = _rng.NextInt(_items.Length);
        return _rng.NextDouble() < _prob[idx] ? _items[idx] : _items[_alias[idx]];
    }
}
```

---

## A-5. 등급제 2단계 가챠

```csharp
public enum Tier { SSR = 1, SR = 2, R = 3 }

public sealed record TierProbability(Tier Tier, double Probability);
public sealed record GachaItem(long ItemId, string Code, Tier Tier, double WeightInTier, bool IsPickup);

public sealed class TieredGachaService
{
    private readonly IWeightedPicker<Tier> _tierPicker;
    private readonly Dictionary<Tier, IWeightedPicker<GachaItem>> _itemPickers;

    public TieredGachaService(
        IEnumerable<TierProbability> tiers,
        IEnumerable<GachaItem> items,
        IRandomProvider rng)
    {
        var tierArr = tiers.ToArray();
        var sum = tierArr.Sum(t => t.Probability);
        if (Math.Abs(sum - 1.0) > 1e-6)
            throw new InvalidOperationException($"Tier prob sum != 1.0 ({sum})");

        _tierPicker = new RouletteWheelPicker<Tier>(
            tierArr.Select(t => (t.Tier, t.Probability)), rng);

        _itemPickers = items
            .GroupBy(i => i.Tier)
            .ToDictionary(
                g => g.Key,
                g => (IWeightedPicker<GachaItem>)new CumulativeBinarySearchPicker<GachaItem>(
                    g.Select(i => (i, i.WeightInTier)), rng));
    }

    public GachaItem Draw()
    {
        var tier = _tierPicker.Pick();
        return _itemPickers[tier].Pick();
    }

    public GachaItem DrawSsrOnly() => _itemPickers[Tier.SSR].Pick();
    public GachaItem DrawSrOrR()
    {
        var tier = _tierPicker.Pick();
        if (tier == Tier.SSR) tier = Tier.SR;  // 단순 fallback
        return _itemPickers[tier].Pick();
    }
}
```

---

## A-6. 하드/소프트 천장 + 50/50 시스템

```csharp
public sealed class GachaPityState
{
    public int Counter { get; set; }
    public bool GuaranteedNextSsrIsPickup { get; set; }
}

public sealed class SoftPityCurve
{
    public double BaseRate { get; }
    public int SoftStart { get; }
    public int HardPity { get; }
    public double StepIncrease { get; }

    public SoftPityCurve(double baseRate, int softStart, int hardPity, double stepIncrease = 0.06)
    {
        BaseRate = baseRate; SoftStart = softStart; HardPity = hardPity; StepIncrease = stepIncrease;
    }

    public double SsrRate(int counter)
    {
        if (counter >= HardPity) return 1.0;
        if (counter < SoftStart) return BaseRate;
        return Math.Min(1.0, BaseRate + (counter - SoftStart + 1) * StepIncrease);
    }
}

public sealed class FullGachaService
{
    private readonly SoftPityCurve _curve;
    private readonly TieredGachaService _base;
    private readonly GachaItem _pickup;
    private readonly IRandomProvider _rng;

    public FullGachaService(SoftPityCurve curve, TieredGachaService baseService, GachaItem pickup, IRandomProvider rng)
    {
        _curve = curve; _base = baseService; _pickup = pickup; _rng = rng;
    }

    public GachaItem Draw(GachaPityState state)
    {
        state.Counter++;
        var ssrRate = _curve.SsrRate(state.Counter);
        if (_rng.NextDouble() < ssrRate)
        {
            state.Counter = 0;
            return PickSsr(state);
        }
        return _base.DrawSrOrR();
    }

    private GachaItem PickSsr(GachaPityState state)
    {
        bool isPickup;
        if (state.GuaranteedNextSsrIsPickup)
        {
            isPickup = true;
            state.GuaranteedNextSsrIsPickup = false;
        }
        else
        {
            isPickup = _rng.NextDouble() < 0.5;
            if (!isPickup) state.GuaranteedNextSsrIsPickup = true;
        }
        return isPickup ? _pickup : _base.DrawSsrOnly();
    }
}
```

---

## A-7. Fisher-Yates 박스 가챠

```csharp
public static class FisherYates
{
    public static void Shuffle<T>(IList<T> list, IRandomProvider rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.NextInt(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

public sealed record BoxItem(long ItemId, string Code, Tier Tier);
public sealed record BoxDefinition(int BoxDefId, string Name, IReadOnlyList<(BoxItem, int)> Contents);

public sealed class BoxState
{
    public int BoxDefId { get; set; }
    public List<BoxItem> Remaining { get; set; } = new();
    public int RefillCount { get; set; }
}

public sealed class BoxGachaService
{
    private readonly IRandomProvider _rng;
    public BoxGachaService(IRandomProvider rng) => _rng = rng;

    public BoxState OpenNewBox(BoxDefinition def, int refillCount = 0)
    {
        var items = new List<BoxItem>();
        foreach (var (it, cnt) in def.Contents)
            for (int i = 0; i < cnt; i++) items.Add(it);
        FisherYates.Shuffle(items, _rng);
        return new BoxState { BoxDefId = def.BoxDefId, Remaining = items, RefillCount = refillCount };
    }

    public BoxItem DrawOne(BoxState state)
    {
        var lastIdx = state.Remaining.Count - 1;
        var item = state.Remaining[lastIdx];
        state.Remaining.RemoveAt(lastIdx);
        return item;
    }
}
```

---

## A-8. 스텝업 / 컬렉션 가챠

```csharp
public sealed record StepConfig(
    int StepNumber,
    int RequiredPulls,
    double SsrRate,
    bool GuaranteedSr,
    bool GuaranteedSsr,
    bool GuaranteedPickup);

public sealed class StepUpProgressState
{
    public int CurrentStep { get; set; } = 1;
    public int PullsInCurrentStep { get; set; }
}

// 컬렉션 가챠
public sealed class CollectionAwarePicker<T> where T : GachaItem
{
    private readonly List<T> _items;
    private readonly IRandomProvider _rng;
    private readonly double _ownedMultiplier;

    public CollectionAwarePicker(IEnumerable<T> items, IRandomProvider rng, double ownedMultiplier = 0.0)
    {
        _items = items.ToList();
        _rng = rng;
        _ownedMultiplier = ownedMultiplier;
    }

    public T Pick(IReadOnlySet<long> ownedIds)
    {
        var weighted = _items
            .Select(i => (i, w: ownedIds.Contains(i.ItemId) ? i.WeightInTier * _ownedMultiplier : i.WeightInTier))
            .Where(x => x.w > 0)
            .ToList();
        return new RouletteWheelPicker<T>(weighted, _rng).Pick();
    }
}
```

---

## A-9. 재화 차감 + 추첨 트랜잭션

```csharp
public sealed class SafeGachaTransaction
{
    private readonly QueryFactory _db;
    private readonly IGachaDrawer _drawer;
    private readonly int _costPerDraw;

    public async Task<DrawResponse> ExecuteAsync(long userId, DrawRequest req, CancellationToken ct = default)
    {
        await using var conn = (MySqlConnection)_db.CreateConnection();
        await conn.OpenAsync(ct);

        // 멱등성 체크
        var existing = await conn.QueryFirstOrDefaultAsync<string>(@"
            SELECT response_json FROM gacha_idempotency
             WHERE user_id=@u AND idem_key=@k", new { u = userId, k = req.IdempotencyKey });
        if (existing != null)
            return JsonSerializer.Deserialize<DrawResponse>(existing)!;

        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var totalCost = _costPerDraw * req.Count;
            var currency = await conn.QueryFirstAsync<int>(
                "SELECT currency FROM user_wallet WHERE user_id=@u FOR UPDATE",
                new { u = userId }, tx);
            if (currency < totalCost) throw new GachaException("재화 부족");

            // 천장 로드, 추첨, 차감, 인벤, 천장 갱신, 이력, 멱등 키 저장 ...
            // (11장 코드 참조)

            await tx.CommitAsync(ct);
            return new DrawResponse(/* ... */);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
```

---

## A-10. 카이제곱 검정 유틸리티

```csharp
using MathNet.Numerics.Distributions;

public static class ChiSquaredTest
{
    public sealed record Result(double ChiSquared, int DegreesOfFreedom, double PValue, bool IsRejected);

    public static Result Test(IReadOnlyList<int> observed, IReadOnlyList<double> expectedProbs,
                               int totalTrials, double significance = 0.05)
    {
        if (observed.Count != expectedProbs.Count)
            throw new ArgumentException("Category count mismatch");

        double chi2 = 0;
        for (int i = 0; i < observed.Count; i++)
        {
            var expected = expectedProbs[i] * totalTrials;
            if (expected < 5)
                throw new InvalidOperationException($"Expected count {expected:F1} too low");
            chi2 += Math.Pow(observed[i] - expected, 2) / expected;
        }
        int df = observed.Count - 1;
        double p = 1.0 - ChiSquared.CDF(df, chi2);
        return new Result(chi2, df, p, p < significance);
    }
}
```

---

## A-11. 몬테카를로 시뮬레이터

```csharp
public sealed class MonteCarloSimulator
{
    private readonly IGachaDrawer _drawer;
    public MonteCarloSimulator(IGachaDrawer drawer) => _drawer = drawer;

    public Dictionary<Tier, int> SimulateTiers(int trials)
    {
        var counts = new Dictionary<Tier, int> { [Tier.SSR]=0, [Tier.SR]=0, [Tier.R]=0 };
        for (int i = 0; i < trials; i++)
        {
            var item = _drawer.Draw(0, 0).MainItem;
            counts[item.Tier]++;
        }
        return counts;
    }

    public Dictionary<long, int> SimulateItems(int trials)
    {
        var counts = new Dictionary<long, int>();
        for (int i = 0; i < trials; i++)
        {
            var item = _drawer.Draw(0, 0).MainItem;
            counts.TryGetValue(item.ItemId, out var c);
            counts[item.ItemId] = c + 1;
        }
        return counts;
    }
}
```

---

## A-12. 3시그마 이상 탐지기

```csharp
public sealed class SigmaAnomalyDetector
{
    public sealed record Report(bool IsAnomaly, double Observed, double Expected, double SigmaCount);

    public Report Check(int observed, int total, double expectedRate, int sigmaThreshold = 3)
    {
        if (total < 100) return new Report(false, 0, expectedRate, 0);

        double obsRate = (double)observed / total;
        double sigma = Math.Sqrt(expectedRate * (1 - expectedRate) / total);
        double sigmaCnt = Math.Abs(obsRate - expectedRate) / sigma;
        return new Report(sigmaCnt >= sigmaThreshold, obsRate, expectedRate, sigmaCnt);
    }
}
```

---

## docker-compose.yml (실습 환경)

```yaml
version: "3.9"
services:
  mysql:
    image: mysql:8.0
    environment:
      MYSQL_ROOT_PASSWORD: gameroot
      MYSQL_DATABASE: game
      MYSQL_USER: game
      MYSQL_PASSWORD: gamepw
    ports: ["3306:3306"]
    command: --default-authentication-plugin=mysql_native_password
  redis:
    image: redis:7-alpine
    ports: ["6379:6379"]
```

`docker compose up -d`로 띄운 뒤 위 코드들을 순서대로 적용하면 모든 실습이 동작한다.
