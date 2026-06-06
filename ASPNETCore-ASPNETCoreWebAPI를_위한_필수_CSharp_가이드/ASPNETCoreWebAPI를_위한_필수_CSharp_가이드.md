# ASP.NET Core Web API를 위한 필수 C# 가이드  

저자: 최흥배, AI-Assisted     
    
권장 개발 환경
- **IDE**: Visual Studio 2022 (Community 이상)
- **닷넷 버전**: .NET 9 이상

-----    
  
# ASP.NET Core Web API를 위한 필수 C# 가이드

## 1. 기본 문법

### 1.1 변수와 데이터 타입
C#은 **정적 타입(statically-typed) 언어**다. 컴파일 시점에 모든 변수의 타입이 결정되며, 이로 인해 런타임 이전에 타입 관련 오류를 잡아낼 수 있다. 변수를 선언할 때는 타입을 명시하는 방법과 `var` 키워드로 컴파일러가 타입을 추론하도록 하는 방법 두 가지가 있다.

주요 기본 타입은 다음과 같다.

- `string`: 문자열을 저장한다. 참조 타입이며 불변(immutable)이다.
- `int`: 32비트 정수 (범위: 약 -21억 ~ +21억)이다.
- `long`: 64비트 정수로, 더 큰 수를 담는다.
- `decimal`: 128비트 고정 소수점 숫자로, 금융 계산처럼 정밀도가 중요한 경우에 사용한다. 리터럴 뒤에 `m` 접미사를 붙여야 한다.
- `double`, `float`: 부동 소수점 숫자이며, 정밀도보다 속도가 중요한 과학/공학 계산에 사용한다.
- `bool`: `true` 또는 `false`만 가질 수 있는 불리언 타입이다.
- `DateTime`, `DateTimeOffset`: 날짜와 시간을 표현한다. Web API에서는 UTC 기반인 `DateTime.UtcNow`를 권장한다.

`var` 키워드는 **변수의 실제 타입을 제거하는 것이 아니라**, 컴파일러에게 우변의 표현식으로부터 타입을 추론하라고 지시하는 것이다. 따라서 `var`를 사용해도 내부적으로는 강력한 정적 타입이 유지된다.

```csharp
// 명시적 타입 선언
string name = "홍길동";
int age = 30;
decimal price = 19.99m;
bool isActive = true;

// 타입 추론
var email = "test@example.com"; // string으로 추론
var count = 10; // int로 추론
```

**실무 팁**: Web API에서 금액 필드는 반드시 `decimal`을 사용하는 것이 원칙이다. `double`은 이진 부동소수점 오차 때문에 `0.1 + 0.2 != 0.3` 같은 문제를 일으킬 수 있다.

### 1.2 Null 처리
C# 8.0부터 도입된 **Nullable Reference Types(NRT, 널 허용 참조 형식)** 은 참조 타입에도 '이 변수가 null이 될 수 있는가'를 컴파일러가 추적하도록 한다. 게임 서버나 Web API처럼 수많은 요청이 오가는 환경에서는 `NullReferenceException` 하나가 전체 요청을 실패시킬 수 있으므로, null 안정성은 매우 중요하다.

- `string`: null을 허용하지 않는다고 선언한다. null 대입 시 컴파일러 경고가 발생한다.
- `string?`: null을 허용한다고 선언한다.
- `??` (null 병합 연산자, null-coalescing operator): 좌측이 null이면 우측 값을 반환한다.
- `??=` (null 병합 대입 연산자): 좌변이 null일 때만 우변 값을 대입한다.
- `?.` (null 조건 연산자, null-conditional operator): 좌변이 null이면 전체 식이 null이 되고, 아니면 멤버 접근을 수행한다. "안전한 내비게이션"이라고도 부른다.

```csharp
// nullable 타입
string? optionalName = null; // null 허용
string requiredName = "필수값"; // null 불가

// null 병합 연산자
string displayName = optionalName ?? "기본이름";

// null 조건 연산자
int? length = optionalName?.Length;
```

위 예제에서 `optionalName?.Length`의 반환 타입이 `int`가 아닌 `int?`라는 점에 주목해야 한다. 체인 중간에 null이 발생하면 결과 자체가 null이 되기 때문이다.

**실무 팁**: 프로젝트 파일(`.csproj`)에 `<Nullable>enable</Nullable>`을 추가하면 프로젝트 전체에 NRT가 적용된다. 팀 프로젝트에서는 반드시 켜두는 것을 권장한다.
  

## 2. 클래스와 객체지향

### 2.1 클래스 정의
클래스는 객체의 **설계도**다. Web API에서는 데이터를 담는 모델(Model), 비즈니스 로직을 수행하는 서비스(Service), HTTP 요청을 처리하는 컨트롤러(Controller)가 모두 클래스로 구현된다. 클래스는 참조 타입이므로 힙(heap)에 할당되고, 변수는 객체의 주소만 담는다.

접근 제한자(access modifier)는 다음과 같다.

- `public`: 어디서든 접근 가능하다.
- `private`: 해당 클래스 내부에서만 접근 가능하다 (기본값).
- `protected`: 해당 클래스와 파생 클래스에서 접근 가능하다.
- `internal`: 동일 어셈블리(프로젝트) 내에서만 접근 가능하다.
- `protected internal`: 동일 어셈블리 또는 파생 클래스에서 접근 가능하다.
- `private protected`: 동일 어셈블리의 파생 클래스에서만 접근 가능하다.

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

`Name` 프로퍼티가 `= string.Empty`로 초기화된 이유는 NRT가 활성화된 상태에서 null 경고를 피하기 위함이다. `string`은 null을 허용하지 않으므로 어떤 초기값이든 반드시 지정해야 한다.

### 2.2 생성자
생성자(constructor)는 객체가 생성될 때 **단 한 번** 호출되어 초기 상태를 구성한다. ASP.NET Core의 의존성 주입(DI) 시스템은 대부분 **생성자 주입(constructor injection)** 방식으로 동작하므로, 서비스 클래스의 생성자에 필요한 의존성을 매개변수로 선언하는 것이 관례다.

```csharp
public class UserService
{
    private readonly ILogger<UserService> _logger;
    
    // 생성자 - 의존성 주입에 필수
    public UserService(ILogger<UserService> logger)
    {
        _logger = logger;
    }
}
```

`readonly` 키워드는 해당 필드가 **생성자에서만 값을 지정할 수 있음**을 의미한다. DI로 주입된 서비스는 인스턴스 생애 동안 바뀌면 안 되므로 `readonly`를 붙이는 것이 표준 패턴이다. 필드명 앞의 언더스코어(`_`)는 Microsoft의 네이밍 규칙에 따른 관례다.

**기본 생성자(primary constructor, C# 12)** 문법도 있는데, 간단한 경우에 코드를 더 짧게 만들어준다.

```csharp
public class UserService(ILogger<UserService> logger)
{
    private readonly ILogger<UserService> _logger = logger;
}
```
  
### 2.3 프로퍼티
프로퍼티(property)는 외부에서 보기에는 필드처럼 보이지만, 내부적으로는 `get`/`set` 메서드로 동작한다. 캡슐화를 유지하면서도 필드처럼 편하게 접근할 수 있게 해주는 C#의 핵심 기능이다.

- **자동 프로퍼티(auto-implemented property)**: `{ get; set; }`처럼 본문 없이 선언하면 컴파일러가 자동으로 백킹 필드(backing field)를 생성한다.
- `init` 접근자(C# 9+): 생성자나 객체 초기화 구문에서만 값을 설정할 수 있고, 이후에는 읽기 전용이 된다. **불변(immutable) 객체**를 만들기에 이상적이다.
- 읽기 전용 자동 프로퍼티 (`{ get; }`): 필드 초기화 또는 생성자에서만 값 설정이 가능하다.

```csharp
public class CreateProductRequest
{
    // 자동 프로퍼티
    public string Name { get; set; } = string.Empty;
    
    // init - 생성 시에만 설정 가능
    public DateTime RequestedAt { get; init; } = DateTime.UtcNow;
    
    // 읽기 전용
    public string Type { get; } = "Product";
}
```

**실무 팁**: DTO(Data Transfer Object)에는 `init`을 사용하여 한 번 생성된 요청 데이터가 실수로 변경되지 않도록 방어하는 것이 안전하다.
  

## 3. 컬렉션

### 3.1 List와 배열
배열(`T[]`)은 **고정 크기** 연속 메모리 공간이고, `List<T>`는 내부적으로 배열을 사용하지만 **동적 크기 조절**이 가능한 컬렉션이다. 원소를 자주 추가/삭제한다면 `List<T>`를, 크기가 고정되고 성능이 극도로 중요하다면 배열을 쓴다.

- `List<T>`: 가장 많이 사용되는 동적 컬렉션이다. `Add`, `Remove`, `Insert` 등의 메서드를 제공한다.
- `T[]`: 크기가 고정된 배열이다. 인덱서 접근이 가장 빠르다.
- `IEnumerable<T>`: 순회(foreach) 가능한 모든 컬렉션의 최상위 인터페이스다. 실제 구현이 아닌 추상화를 매개변수 타입으로 사용하면 유연성이 높아진다.
- `IReadOnlyList<T>`, `IReadOnlyCollection<T>`: 읽기 전용으로 노출하고 싶을 때 사용한다.

```csharp
// List - 동적 크기
List<string> names = new List<string> { "김", "이", "박" };
names.Add("최");

// 배열 - 고정 크기
string[] colors = new[] { "red", "green", "blue" };

// 컬렉션 초기화 (C# 12)
List<int> numbers = [1, 2, 3, 4, 5];
```

C# 12의 **컬렉션 표현식(collection expression)** `[1, 2, 3]`은 대입받는 변수의 타입에 따라 `List<int>`, `int[]`, `IEnumerable<int>` 등으로 자동 변환된다. 문법이 간결해지고 배열과 리스트 간 변환이 매끄러워졌다.

### 3.2 Dictionary

`Dictionary<TKey, TValue>`는 **해시 테이블**을 기반으로 한 키-값 쌍 컬렉션이다. 평균 O(1)로 조회가 가능해 캐시, 룩업 테이블, 설정값 저장에 자주 사용된다. 키로 사용하는 타입은 `GetHashCode()`와 `Equals()`가 올바르게 구현되어 있어야 한다.

- 인덱서 `[key]`는 키가 존재하지 않으면 `KeyNotFoundException`을 던진다.
- `TryGetValue`는 예외 없이 존재 여부와 값을 동시에 반환한다. **예외는 비용이 크므로** 조회 후 분기를 태우는 경우에는 `TryGetValue`가 권장된다.
- `ContainsKey` + 인덱서 조합은 해시 조회를 두 번 하게 되므로 비효율적이다. `TryGetValue`가 한 번의 조회로 끝낸다.

```csharp
Dictionary<string, int> ages = new()
{
    ["홍길동"] = 30,
    ["김철수"] = 25
};

// 값 가져오기
if (ages.TryGetValue("홍길동", out int age))
{
    Console.WriteLine(age);
}
```

**실무 팁**: 멀티스레드 환경에서 동시에 읽고 쓰려면 `Dictionary<T,V>` 대신 `ConcurrentDictionary<T,V>`를 사용해야 한다. 게임 서버의 세션 관리나 Web API의 인메모리 캐시에서 자주 만나는 타입이다.
  

## 4. LINQ
LINQ(Language Integrated Query)는 **컬렉션, 데이터베이스, XML, JSON 등 다양한 데이터 소스를 동일한 문법으로 질의할 수 있게 해주는 기능**이다. SQL처럼 데이터를 필터링하고 변환하는 연산을 코드 수준에서 선언적으로 표현할 수 있다.

주요 연산자는 다음과 같다.

- `Where(조건)`: 조건에 맞는 원소만 남긴다 (SQL의 `WHERE`).
- `Select(변환)`: 각 원소를 다른 형태로 변환한다 (SQL의 `SELECT`).
- `OrderBy` / `OrderByDescending`: 정렬한다. 복수 정렬은 `ThenBy`를 연이어 쓴다.
- `GroupBy`: 키 기준으로 묶는다.
- `FirstOrDefault` / `SingleOrDefault`: 첫 번째 원소 또는 유일한 원소를 가져오되, 없으면 기본값(참조 타입은 null) 반환한다. `First`, `Single`은 없을 때 예외를 던진다.
- `Any` / `All`: 하나라도 있는지 / 모두가 조건을 만족하는지 검사한다.
- `Count` / `Sum` / `Average` / `Min` / `Max`: 집계 함수다.
- `ToList()` / `ToArray()` / `ToDictionary()`: 지연 실행을 종료하고 실제 컬렉션을 만든다.

LINQ의 중요한 특성 중 하나는 **지연 실행(deferred execution)** 이다. `Where`, `Select` 등은 호출되는 순간 실행되지 않고, `ToList()`나 `foreach`로 열거될 때 비로소 실행된다. Entity Framework에서는 이 원리를 이용해 `Where`/`Select`를 SQL로 번역해 DB에서 실행한다.

```csharp
List<Product> products = GetProducts();

// Where - 필터링
var expensive = products.Where(p => p.Price > 100);

// Select - 변환
var names = products.Select(p => p.Name);

// OrderBy - 정렬
var sorted = products.OrderBy(p => p.Price);

// FirstOrDefault - 첫 항목 또는 기본값
var first = products.FirstOrDefault(p => p.Id == 1);

// Any - 존재 여부
bool hasExpensive = products.Any(p => p.Price > 1000);

// 메서드 체이닝
var result = products
    .Where(p => p.Price > 50)
    .OrderBy(p => p.Name)
    .Select(p => new { p.Name, p.Price })
    .ToList();
```

`new { p.Name, p.Price }`는 **익명 형식(anonymous type)** 으로, 임시로 필요한 데이터 구조를 이름 없이 바로 만들어 쓰는 방법이다.

**실무 팁**: EF Core에서는 DB에 쿼리를 보내는 시점(예: `ToListAsync()`)에 어떤 SQL이 생성되는지 반드시 로그로 확인해야 한다. LINQ로는 깔끔해 보여도 내부적으로는 N+1 쿼리나 풀스캔이 발생할 수 있다.
  
  

## 5. 비동기 프로그래밍
Web API는 I/O 바운드 작업(DB 조회, 외부 API 호출, 파일 입출력)이 매우 많다. 이런 작업을 동기적으로 처리하면 스레드가 대기 상태로 묶여 서버의 처리량이 급격히 떨어진다. `async/await`는 **스레드를 점유하지 않고 I/O 완료를 기다리는** 방식으로 이 문제를 해결한다.

### 5.1 async/await

- `async`: 해당 메서드가 비동기 메서드임을 나타내는 키워드다. `async` 키워드만으로는 비동기가 되지 않으며, 내부에 `await`가 있어야 의미가 있다.
- `await`: `Task`가 완료될 때까지 기다리되, **스레드는 반환**한다. 완료되면 이어서 실행된다.
- `Task`: 반환값이 없는 비동기 작업을 나타낸다 (`void`의 비동기 버전이라고 생각하면 된다).
- `Task<T>`: 결과 타입이 `T`인 비동기 작업을 나타낸다.
- `ValueTask<T>`: 자주 동기적으로 완료되는 경우에 힙 할당을 줄이기 위해 사용한다 (성능 최적화용이다).

```csharp
public async Task<List<User>> GetUsersAsync()
{
    // 비동기 데이터베이스 조회
    var users = await _dbContext.Users.ToListAsync();
    return users;
}

public async Task<User?> GetUserByIdAsync(int id)
{
    return await _dbContext.Users
        .FirstOrDefaultAsync(u => u.Id == id);
}
```

메서드 이름에 `Async` 접미사를 붙이는 것은 .NET의 강력한 관례다. 호출자는 이름만 보고 비동기 여부를 알 수 있어야 한다.

### 5.2 Task 반환

```csharp
// 값을 반환하는 비동기 메서드
public async Task<string> FetchDataAsync()
{
    await Task.Delay(1000);
    return "데이터";
}

// 반환값이 없는 비동기 메서드
public async Task SaveDataAsync(string data)
{
    await _repository.SaveAsync(data);
}
```

**반드시 피해야 할 안티패턴**들이다.

- `async void`: 예외를 캐치할 수 없고 완료 추적이 불가능하다. 이벤트 핸들러가 아닌 이상 사용하지 말아야 한다.
- `.Result`, `.Wait()`: 동기로 기다리는 호출로, ASP.NET 환경에서는 **데드락**을 일으킬 수 있다.
- `Task.Run`으로 감싸기: 이미 비동기인 메서드를 `Task.Run`으로 감싸는 것은 스레드풀 스레드 하나를 더 소비할 뿐 이득이 없다.

**실무 팁**: `ConfigureAwait(false)`는 라이브러리 코드에서 유용하지만, ASP.NET Core는 더 이상 동기화 컨텍스트를 사용하지 않으므로 애플리케이션 코드에서는 붙일 필요가 없다.
  

## 6. 인터페이스
인터페이스(interface)는 **"무엇을 할 수 있는가"에 대한 계약**이다. 구현 세부사항은 담지 않고 어떤 메서드와 프로퍼티가 있어야 하는지만 기술한다. 의존성 주입, 단위 테스트의 목(mock) 객체, 다형성을 위한 핵심 도구다.

인터페이스를 사용해야 하는 이유는 다음과 같다.

- **느슨한 결합(loose coupling)**: 컨트롤러가 `ProductService` 구체 클래스 대신 `IProductService` 인터페이스에 의존하면, 구현을 교체해도 컨트롤러 코드는 변경할 필요가 없다.
- **테스트 용이성**: 단위 테스트에서 실제 DB에 접근하는 대신 인터페이스의 가짜 구현을 주입할 수 있다.
- **다중 구현**: 한 인터페이스를 상황에 따라 다른 구현으로 갈아끼울 수 있다 (예: `ICacheService`를 메모리 / Redis 버전으로 교체).

C# 네이밍 관례상 인터페이스는 **대문자 `I`로 시작**한다 (`IProductService`, `IRepository` 등).

```csharp
// 인터페이스 정의
public interface IProductService
{
    Task<List<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
    Task<Product> CreateAsync(Product product);
}

// 구현
public class ProductService : IProductService
{
    private readonly AppDbContext _context;
    
    public ProductService(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<Product>> GetAllAsync()
    {
        return await _context.Products.ToListAsync();
    }
    
    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products.FindAsync(id);
    }
    
    public async Task<Product> CreateAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }
}
```

`Program.cs`에서 DI 컨테이너에 등록하면 실제 연결이 이루어진다.

```csharp
builder.Services.AddScoped<IProductService, ProductService>();
```

등록할 때 수명(lifetime)은 세 가지가 있다.

- `Singleton`: 애플리케이션 전체에서 하나의 인스턴스가 사용된다.
- `Scoped`: HTTP 요청마다 새로 생성된다 (Web API의 기본 선택지다).
- `Transient`: 요청할 때마다 새로 생성된다.
  

## 7. 레코드 (C# 9+)
`record`는 **값 기반 동등성(value equality)** 을 가지는 참조 타입이다. 일반 클래스는 참조 동등성(같은 메모리 주소를 가리켜야 같음)을 가지지만, 레코드는 **모든 프로퍼티 값이 같으면 동등**하다고 판단한다.

레코드의 주요 특징은 다음과 같다.

- **불변성(immutability)**: 기본적으로 `init` 접근자를 사용해 생성 후 변경 불가능하다.
- **값 기반 동등성**: `Equals`, `GetHashCode`, `==`, `!=`가 프로퍼티 값 기준으로 자동 생성된다.
- **`with` 표현식**: 일부 프로퍼티만 변경한 복사본을 쉽게 만들 수 있다.
- **분해(deconstruction)**: 위치 매개변수 레코드는 자동으로 분해 가능하다.
- **예쁜 `ToString`**: 디버깅에 유용한 문자열 표현이 자동 생성된다.

```csharp
// 간결한 레코드 정의
public record ProductDto(int Id, string Name, decimal Price);

// 프로퍼티가 있는 레코드
public record CreateProductRequest
{
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
}

// 사용
var dto = new ProductDto(1, "노트북", 1500000m);
var updated = dto with { Price = 1400000m }; // 복사 후 수정
```

**레코드를 언제 쓸 것인가**:

- DTO (Data Transfer Object): API 요청/응답 모델에 가장 잘 맞는다.
- 값 객체(Value Object): 도메인 주도 설계(DDD)의 값 객체 구현에 적합하다.
- 이벤트 메시지: 메시징 시스템에서 불변 이벤트를 표현할 때 쓴다.

**레코드를 피해야 할 때**:

- EF Core 엔티티: 일반적으로 `class`를 사용하는 것이 권장된다. 레코드의 값 동등성은 엔티티의 ID 기반 동등성과 충돌할 수 있다.
  

## 8. 패턴 매칭
패턴 매칭은 **값의 모양(shape)을 검사하고 분기하는 선언적 문법**이다. C#은 버전이 올라갈수록 패턴 매칭이 강력해졌다.

- **상수 패턴**: `x is 0`처럼 상수와 비교한다.
- **타입 패턴**: `obj is Product product`처럼 타입을 검사하고 변수로 바인딩한다.
- **관계 패턴**: `< 100`, `>= 0`처럼 비교 연산자를 쓴다 (C# 9+).
- **논리 패턴**: `and`, `or`, `not`을 조합한다 (C# 9+).
- **프로퍼티 패턴**: `{ Price: > 1000, Status: "Active" }` 형태로 객체의 프로퍼티를 검사한다.
- **`switch` 표현식**: 문이 아닌 **표현식**으로 값을 반환한다. `_`는 기본 케이스다.

```csharp
// switch 표현식
string GetCategory(decimal price) => price switch
{
    < 10000 => "저가",
    < 100000 => "중가",
    _ => "고가"
};

// is 패턴
if (obj is Product product)
{
    Console.WriteLine(product.Name);
}

// null 체크
if (user is not null)
{
    Console.WriteLine(user.Name);
}
```

`is not null`은 `!= null`과 비슷하지만 **항상 정확한 null 검사**를 보장한다. `==`는 연산자 오버로딩이 가능하지만 `is`는 그렇지 않아, 의도치 않은 동작을 방지할 수 있다.

**실무 팁**: 복잡한 `if-else` 체인은 `switch` 표현식으로 바꾸면 가독성이 크게 향상된다. 게임 서버에서 상태 전이 로직을 표현할 때 특히 효과적이다.
  

## 9. 예외 처리
예외(exception)는 **예상치 못한 오류 상황**을 나타낸다. 정상적인 제어 흐름에는 예외를 사용하지 않는 것이 원칙이다(예외는 비용이 크다).

- `try`: 예외가 발생할 가능성이 있는 블록을 감싼다.
- `catch`: 발생한 예외를 처리한다. 구체적인 예외부터 일반적인 예외 순으로 배치한다.
- `finally`: 예외 발생 여부와 관계없이 항상 실행되는 블록이다. 리소스 정리에 사용하지만, `using`이 있으면 대부분 필요 없다.
- `throw`: 예외를 던진다.
- `throw;` (세미콜론만): 잡은 예외를 원래 스택 트레이스를 보존하며 다시 던진다. `throw ex;`는 스택 정보를 잃기 때문에 피해야 한다.

```csharp
public async Task<Product> GetProductAsync(int id)
{
    try
    {
        var product = await _context.Products.FindAsync(id);
        
        if (product is null)
        {
            throw new NotFoundException($"Product {id} not found");
        }
        
        return product;
    }
    catch (DbUpdateException ex)
    {
        _logger.LogError(ex, "Database error");
        throw;
    }
}
```

**자주 쓰는 예외 타입**들은 다음과 같다.

- `ArgumentNullException`: 매개변수가 null이면 안 될 때 던진다.
- `ArgumentException`, `ArgumentOutOfRangeException`: 매개변수 값이 잘못되었을 때 던진다.
- `InvalidOperationException`: 객체가 해당 연산을 수행할 수 없는 상태일 때 던진다.
- `NotImplementedException`: 아직 구현되지 않은 메서드에 사용한다.

**Web API 모범 사례**: 전역 예외 처리 미들웨어(`UseExceptionHandler` 또는 커스텀 미들웨어)를 사용해 예외를 일관된 JSON 응답으로 변환하고, .NET 8+의 `IExceptionHandler` 인터페이스를 활용하는 것이 권장된다.
  

## 10. 특성(Attributes)
특성(Attribute)은 **메타데이터**다. 코드의 동작을 직접 바꾸는 대신, 컴파일러나 런타임, 프레임워크가 읽어 해석한다. ASP.NET Core는 라우팅, 모델 바인딩, 검증, 직렬화 등 거의 모든 부분에서 특성을 활용한다.

자주 쓰는 Web API 특성들은 다음과 같다.

- `[ApiController]`: 컨트롤러에 API 전용 기능(자동 모델 검증, 문제 세부 사항 응답 등)을 활성화한다.
- `[Route("api/[controller]")]`: 라우팅 템플릿을 지정한다. `[controller]`는 컨트롤러 이름(접미사 `Controller` 제거)으로 치환된다.
- `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`, `[HttpPatch]`: HTTP 메서드를 지정한다.
- `[FromBody]`: 매개변수를 요청 본문(JSON 등)에서 바인딩한다.
- `[FromQuery]`: 쿼리 문자열에서 바인딩한다.
- `[FromRoute]`: URL 라우트에서 바인딩한다.
- `[FromHeader]`: 헤더에서 바인딩한다.

검증(Validation) 특성들은 `System.ComponentModel.DataAnnotations` 네임스페이스에 있다.

- `[Required]`: 필수 값임을 지정한다.
- `[StringLength(max)]`, `[MinLength]`, `[MaxLength]`: 문자열 길이를 제한한다.
- `[Range(min, max)]`: 숫자 범위를 제한한다.
- `[RegularExpression("^...$")]`: 정규식 패턴을 검사한다.
- `[EmailAddress]`, `[Url]`, `[Phone]`: 형식 검증이다.

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<Product>>> GetAll()
    {
        // ...
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetById(int id)
    {
        // ...
    }
    
    [HttpPost]
    public async Task<ActionResult<Product>> Create(
        [FromBody] CreateProductRequest request)
    {
        // ...
    }
}

// 검증 특성
public class CreateProductRequest
{
    [Required(ErrorMessage = "이름은 필수입니다")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Range(0, 1000000)]
    public decimal Price { get; set; }
}
```

`[ApiController]`가 붙은 컨트롤러는 모델 검증이 실패하면 **자동으로 400 Bad Request**를 반환해 준다. 이 덕분에 컨트롤러 메서드 안에서 `if (!ModelState.IsValid)`를 일일이 쓸 필요가 없다.
  

## 11. 확장 메서드
확장 메서드(extension method)는 **기존 타입을 수정하지 않고 새 메서드를 추가**할 수 있는 기능이다. 특히 남이 만든 타입(BCL, 서드파티 라이브러리)에 편의 메서드를 붙일 때 강력하다.

규칙은 다음과 같다.

- 반드시 **정적 클래스(`static class`)** 내부에 정의해야 한다.
- **정적 메서드**여야 한다.
- 첫 매개변수에 `this` 키워드를 붙이면, 그 매개변수의 타입에 대한 확장 메서드가 된다.

LINQ 자체가 확장 메서드로 구현되어 있다. `IEnumerable<T>`에 `Where`, `Select` 같은 메서드가 **원래는 없었지만**, `System.Linq.Enumerable` 정적 클래스의 확장 메서드로 제공된다.

```csharp
public static class StringExtensions
{
    public static bool IsValidEmail(this string email)
    {
        return email.Contains("@") && email.Contains(".");
    }
}

// 사용
string email = "test@example.com";
bool valid = email.IsValidEmail();
```

**주의 사항**:

- 동일한 시그니처의 **인스턴스 메서드가 우선**한다. 확장 메서드는 인스턴스 메서드가 없을 때만 고려된다.
- 네임스페이스를 `using`으로 가져와야 확장 메서드가 보인다.
- 과도한 확장 메서드는 가독성을 해칠 수 있다. "이 타입에 자연스럽게 있었어야 하는가?"를 기준으로 판단한다.

**실무 팁**: IQueryable에 페이징, 정렬 등을 확장 메서드로 제공하면 코드 재사용성이 매우 높아진다 (24.2절의 `Paginate` 예제 참조).
  

## 12. 실전 Web API 컨트롤러 예제
지금까지 배운 내용을 종합한 완전한 컨트롤러다. 의존성 주입, 비동기, LINQ, 특성, 검증, 레코드, 패턴 매칭이 모두 녹아 있다.

컨트롤러의 각 액션이 반환하는 `ActionResult<T>`는 **200/201 같은 성공 응답의 데이터** 또는 **`NotFound()`, `BadRequest()` 같은 상태 코드 전용 결과**를 유연하게 반환할 수 있게 해주는 특별한 타입이다. `ControllerBase`는 `Controller`와 달리 뷰 관련 기능이 없는 API 전용 기반 클래스다.

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;
    
    public ProductsController(
        IProductService productService,
        ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }
    
    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetAll(
        [FromQuery] decimal? minPrice = null)
    {
        try
        {
            var products = await _productService.GetAllAsync();
            
            if (minPrice.HasValue)
            {
                products = products
                    .Where(p => p.Price >= minPrice.Value)
                    .ToList();
            }
            
            var dtos = products
                .Select(p => new ProductDto(p.Id, p.Name, p.Price))
                .ToList();
            
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "제품 목록 조회 실패");
            return StatusCode(500, "서버 오류가 발생했습니다");
        }
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        
        if (product is null)
        {
            return NotFound($"제품 {id}를 찾을 수 없습니다");
        }
        
        return Ok(new ProductDto(product.Id, product.Name, product.Price));
    }
    
    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(
        [FromBody] CreateProductRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var product = new Product
        {
            Name = request.Name,
            Price = request.Price,
            CreatedAt = DateTime.UtcNow
        };
        
        var created = await _productService.CreateAsync(product);
        var dto = new ProductDto(created.Id, created.Name, created.Price);
        
        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            dto);
    }
    
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(
        int id,
        [FromBody] UpdateProductRequest request)
    {
        var existing = await _productService.GetByIdAsync(id);
        
        if (existing is null)
        {
            return NotFound();
        }
        
        existing.Name = request.Name;
        existing.Price = request.Price;
        
        await _productService.UpdateAsync(existing);
        
        return NoContent();
    }
    
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        
        if (product is null)
        {
            return NotFound();
        }
        
        await _productService.DeleteAsync(id);
        
        return NoContent();
    }
}
```

주요 응답 헬퍼 메서드는 다음과 같다.

- `Ok(data)`: 200 OK와 본문을 반환한다.
- `CreatedAtAction(액션명, 라우트값, data)`: 201 Created와 함께 `Location` 헤더를 설정한다. REST의 자원 생성 규약에 맞다.
- `NoContent()`: 204 No Content를 반환한다. 업데이트/삭제 성공 시 흔하게 쓰인다.
- `NotFound()`, `BadRequest()`, `Unauthorized()`, `Forbid()`, `Conflict()`: 각각의 상태 코드 응답이다.

**실무 팁**: `nameof(GetById)`를 사용하면 메서드명을 리팩터링해도 자동으로 반영된다. 하드코딩된 문자열을 피하는 습관이 유지보수를 훨씬 쉽게 만든다.
   

## 13. 제네릭 (Generics)
제네릭은 **타입을 매개변수화**하는 기능이다. `List<T>`의 `T`처럼 실제 타입을 나중에 결정할 수 있게 해 주어, 하나의 코드를 다양한 타입에 재사용할 수 있게 만든다. Repository 패턴, 캐시, 응답 래퍼 등 ASP.NET Core의 거의 모든 재사용 코드는 제네릭을 기반으로 한다.

제네릭의 장점은 다음과 같다.

- **타입 안정성**: 박싱/언박싱 없이 컴파일 시점에 타입 오류를 잡는다.
- **성능**: `object`로 모든 타입을 받는 방식에 비해 빠르다 (값 타입의 박싱 비용이 없다).
- **코드 재사용**: 같은 로직을 타입마다 복제할 필요가 없다.

### 13.1 제네릭 클래스

`where T : class` 같은 **제약 조건(constraint)** 은 제네릭 타입 매개변수에 제한을 둔다.

- `where T : class`: 참조 타입만 허용한다.
- `where T : struct`: 값 타입만 허용한다.
- `where T : new()`: 매개변수 없는 공개 생성자를 가져야 한다.
- `where T : BaseEntity`: 특정 기본 클래스를 상속해야 한다.
- `where T : IEntity`: 특정 인터페이스를 구현해야 한다.
- `where T : notnull`: null이 될 수 없는 타입이어야 한다.

```csharp
// 제네릭 Repository
public interface IRepository<T> where T : class
{
    Task<List<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}

public class Repository<T> : IRepository<T> where T : class
{
    private readonly DbContext _context;
    private readonly DbSet<T> _dbSet;
    
    public Repository(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }
    
    public async Task<List<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }
    
    public async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }
}
```

### 13.2 제네릭 메서드
제네릭은 클래스 전체뿐 아니라 **개별 메서드**에도 적용 가능하다. 정적 팩토리 메서드에서 자주 활용된다.

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    
    public static ApiResponse<T> SuccessResult(T data)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data
        };
    }
    
    public static ApiResponse<T> ErrorResult(string error)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = error
        };
    }
}

// 사용
var response = ApiResponse<Product>.SuccessResult(product);
```

`ApiResponse<T>` 같은 래퍼는 Web API의 **일관된 응답 구조**를 제공한다. 클라이언트는 성공/실패 플래그와 데이터를 공통 구조로 받기 때문에 파싱이 편해진다.
 


## 14. 람다 식과 델리게이트
델리게이트(delegate)는 **메서드에 대한 참조를 담는 타입**이다. C++의 함수 포인터나 Go의 함수 타입과 개념이 비슷하지만, 타입 안전성과 인스턴스 메서드 지원이 있다.

**람다 식(lambda expression)** 은 이름 없이 즉석에서 정의하는 작은 함수다. LINQ의 `Where(p => p.Price > 100)`에서 `p => p.Price > 100`이 람다다. `=>`는 "~로 간다"(goes to)로 읽는다.

### 14.1 Func와 Action
`Func`와 `Action`은 **내장된 제네릭 델리게이트 타입**이다. 커스텀 델리게이트 타입을 직접 정의하는 일은 거의 없다.

- `Func<T1, ..., TResult>`: **값을 반환**하는 메서드다. 마지막 타입 매개변수가 반환 타입이다.
- `Action<T1, ...>`: **반환값이 없는**(void) 메서드다.
- `Predicate<T>`: `Func<T, bool>`과 같다. 주로 필터링 조건에 사용된다.

```csharp
// Func<입력, 반환> - 값을 반환
Func<int, int, int> add = (a, b) => a + b;
int result = add(5, 3); // 8

// Action<입력> - 반환값 없음
Action<string> log = message => Console.WriteLine(message);
log("로그 메시지");

// 서비스에서 사용
public class CacheService
{
    public async Task<T> GetOrAddAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan expiration)
    {
        if (_cache.TryGetValue(key, out T cached))
        {
            return cached;
        }
        
        var value = await factory();
        _cache.Set(key, value, expiration);
        return value;
    }
}

// 사용
var product = await _cacheService.GetOrAddAsync(
    "product:1",
    async () => await _productService.GetByIdAsync(1),
    TimeSpan.FromMinutes(5)
);
```

위 캐시 예제는 매우 실전적인 패턴이다. **"캐시에 있으면 돌려주고, 없으면 생성해서 저장한다"** 는 로직을 일반화하기 위해 값 생성 방법(`factory`)을 델리게이트로 받는다. 호출자가 어떻게 값을 만들지 외부에서 주입하므로 유연하다.

### 14.2 표현식 본문 멤버
**표현식 본문 멤버(expression-bodied member)** 는 한 줄짜리 메서드나 프로퍼티를 `=>` 문법으로 간결하게 표현한다. 중괄호와 `return`을 생략할 수 있다.

```csharp
public class Product
{
    public decimal Price { get; set; }
    public decimal Tax { get; set; }
    
    // 표현식 본문 프로퍼티
    public decimal TotalPrice => Price + Tax;
    
    // 표현식 본문 메서드
    public decimal GetDiscount(decimal rate) => Price * rate;
}
```

**주의**: 표현식 본문 프로퍼티(`=>`)는 **매번 계산**되는 계산된 프로퍼티다. `Price`가 변할 때마다 `TotalPrice`도 자동으로 바뀐다. 캐시된 값을 원한다면 필드나 메서드로 분리해야 한다.
  

## 15. 열거형 (Enum)
`enum`은 **이름이 있는 정수 상수의 집합**이다. "주문 상태"처럼 제한된 선택지를 표현할 때 매직 넘버(`0`, `1`, `2`)나 매직 문자열(`"Pending"`, `"Shipped"`)을 쓰는 것보다 훨씬 안전하고 가독성이 좋다.

기본 타입은 `int`이지만 `byte`, `short`, `long` 등으로 명시할 수 있다. 값을 지정하지 않으면 0부터 순차적으로 할당된다.

```csharp
public enum OrderStatus
{
    Pending = 0,
    Processing = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}

public enum UserRole
{
    User,
    Admin,
    SuperAdmin
}

public class Order
{
    public int Id { get; set; }
    public OrderStatus Status { get; set; }
}

// 사용
var order = new Order { Status = OrderStatus.Pending };

if (order.Status == OrderStatus.Pending)
{
    order.Status = OrderStatus.Processing;
}

// Enum을 문자열로
string statusName = order.Status.ToString(); // "Processing"

// 문자열을 Enum으로
if (Enum.TryParse<OrderStatus>("Shipped", out var status))
{
    order.Status = status;
}
```

**플래그 enum**은 비트 마스크를 활용해 여러 값을 조합할 수 있게 한다. `[Flags]` 특성을 붙이고 값을 2의 거듭제곱으로 준다.

```csharp
[Flags]
public enum Permissions
{
    None = 0,
    Read = 1,
    Write = 2,
    Delete = 4,
    All = Read | Write | Delete  // 7
}

var perms = Permissions.Read | Permissions.Write;
bool canWrite = perms.HasFlag(Permissions.Write); // true
```

**실무 팁**: Web API에서 enum을 JSON으로 직렬화할 때 기본은 숫자로 나간다. 가독성을 위해 **문자열로 직렬화**하려면 `JsonStringEnumConverter`를 등록한다.

```csharp
builder.Services.ConfigureHttpJsonOptions(opt =>
{
    opt.SerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter());
});
```
  

## 16. 네임스페이스와 using
네임스페이스는 **타입을 논리적으로 묶어 이름 충돌을 방지**하는 장치다. `MyApi.Models.Product`와 `MyApi.Dtos.Product`는 이름이 같아도 서로 다른 타입이다. 대규모 프로젝트에서 필수적이다.

프로젝트 구조 관례는 보통 다음과 같다.

- `MyApi.Models` 또는 `MyApi.Entities`: 도메인 엔티티
- `MyApi.Dtos`: 요청/응답 DTO
- `MyApi.Services`: 비즈니스 로직
- `MyApi.Controllers`: API 엔드포인트
- `MyApi.Data`: EF Core 컨텍스트와 마이그레이션

### 16.1 네임스페이스 구조

```csharp
namespace MyApi.Models
{
    public class Product { }
}

namespace MyApi.Services
{
    public interface IProductService { }
    public class ProductService : IProductService { }
}

namespace MyApi.Controllers
{
    using MyApi.Models;
    using MyApi.Services;
    
    public class ProductsController { }
}
```

### 16.2 파일 범위 네임스페이스 (C# 10)
C# 10부터는 **파일 전체에 하나의 네임스페이스만 있는 경우** 중괄호를 생략할 수 있다. 들여쓰기가 한 단계 줄어들어 가독성이 좋아진다. 현대의 .NET 프로젝트는 대부분 이 방식을 쓴다.

```csharp
// 전체 파일에 적용
namespace MyApi.Models;

public class Product
{
    public int Id { get; set; }
}

public class Category
{
    public int Id { get; set; }
}
```

### 16.3 Global using (C# 10)
**전역 using(global using)** 은 한 번 선언하면 프로젝트 전체에서 해당 네임스페이스가 자동으로 `using`된 것으로 간주한다. `System`, `System.Linq`처럼 거의 모든 파일에서 사용하는 네임스페이스를 반복 선언하지 않아도 된다.

```csharp
// GlobalUsings.cs 파일
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.EntityFrameworkCore;
```

`.csproj`에 `<ImplicitUsings>enable</ImplicitUsings>`을 켜면 SDK가 프로젝트 유형에 맞는 기본 네임스페이스들을 자동으로 global using해준다. .NET 6 이후 템플릿의 기본 설정이다.
  

## 17. Nullable 참조 형식 심화
1.2절에서 기본을 다뤘으니, 여기서는 조금 더 깊이 들어간다.

### 17.1 Null 허용 컨텍스트
`#nullable enable` 디렉티브는 해당 파일(또는 범위)에서 NRT를 켠다. 프로젝트 수준에서 끄고 특정 파일에서만 키거나, 그 반대 용도로 쓸 수 있다.

NRT가 켜진 상태에서 nullable 경고는 다음 상황에서 발생한다.

- null 가능 참조를 null 불가 변수에 대입할 때
- null 가능 참조의 멤버에 null 체크 없이 접근할 때
- 생성자에서 null 불가 참조 프로퍼티를 초기화하지 않을 때

```csharp
#nullable enable

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } // 경고: null 불가능이지만 초기화 안됨
    public string? Email { get; set; } // null 허용
    
    // 올바른 방법
    public string Username { get; set; } = string.Empty;
}
```

### 17.2 Null 체크 패턴
null 용서 연산자(`!`, null-forgiving operator)는 **컴파일러에게 "나는 이 값이 null이 아님을 확신한다"** 라고 말하는 것이다. 런타임에는 아무런 검사도 하지 않으므로, 잘못 사용하면 여전히 `NullReferenceException`이 난다. 꼭 필요한 경우(예: ORM이나 리플렉션으로 항상 채워지는 필드)에만 제한적으로 사용한다.

```csharp
public class UserService
{
    public string GetDisplayName(User? user)
    {
        // null 병합
        return user?.Name ?? "익명";
    }
    
    public void ProcessUser(User? user)
    {
        // null 체크 후 사용
        if (user is null)
        {
            throw new ArgumentNullException(nameof(user));
        }
        
        Console.WriteLine(user.Name); // 안전
    }
    
    // null 용서 연산자 (확실할 때만)
    public string GetName(User user)
    {
        return user!.Name; // user가 null이 아님을 확신
    }
}
```

.NET 6+에서는 `ArgumentNullException.ThrowIfNull(user)` 한 줄로 깔끔하게 대체할 수 있다. `[NotNull]`, `[MaybeNull]`, `[NotNullWhen]` 같은 nullable 특성들을 사용하면 컴파일러에게 더 정교한 흐름 분석을 지시할 수 있다.
 

## 18. 문자열 처리
문자열은 Web API의 응답 생성, 로그 출력, SQL/JSON 구성 등 어디서나 쓰인다. C#은 문자열 처리 문법을 꾸준히 발전시켜 왔다.

### 18.1 문자열 보간
**문자열 보간(string interpolation)** 은 `$"..."` 구문 내부에 `{표현식}`을 써서 값을 삽입하는 방법이다. 예전 `string.Format("{0} {1}", a, b)` 방식보다 훨씬 읽기 쉽다.

형식 지정자(format specifier)는 `:` 뒤에 쓴다.

- `:C`: 통화(Currency) 형식이다.
- `:P`: 퍼센트 형식이다.
- `:N`: 천 단위 구분자가 있는 숫자 형식이다.
- `:F2`: 소수점 둘째 자리까지 고정 소수점 형식이다.
- `:yyyy-MM-dd`: 날짜 형식이다.
- `:X`: 16진수 형식이다.

```csharp
string name = "홍길동";
int age = 30;

// 문자열 보간
string message = $"이름: {name}, 나이: {age}";

// 다중 라인
string multiLine = $"""
    사용자 정보:
    - 이름: {name}
    - 나이: {age}
    """;

// 표현식
decimal price = 1000;
string priceText = $"가격: {price:C}원"; // 통화 형식
string percent = $"할인율: {0.15:P}"; // 퍼센트
```

### 18.2 원시 문자열 리터럴 (C# 11)
**원시 문자열 리터럴(raw string literal)** 은 큰따옴표 세 개(`"""`) 이상으로 감싸는 문자열이다. 이스케이프(`\"`, `\\`)가 필요 없어 JSON, SQL, XML, 정규식 등을 그대로 적기 편하다.

- 구분자로 쓰이는 큰따옴표 개수는 내부에 포함된 연속 따옴표보다 많기만 하면 된다.
- 여러 줄 원시 문자열은 **닫는 `"""`의 들여쓰기 수준**을 기준으로 각 줄에서 공통 공백을 제거한다. 코드 상의 들여쓰기와 실제 문자열 내용을 분리할 수 있어 깔끔하다.
- `$`를 조합해 `$"""..."""`로 보간도 가능하다. 중괄호를 문자 그대로 쓰고 싶다면 `$$"""...{{...}}..."""`처럼 달러 기호 개수를 늘린다.

```csharp
// JSON 문자열
string json = """
    {
        "name": "홍길동",
        "age": 30
    }
    """;

// SQL 쿼리
string sql = """
    SELECT *
    FROM Users
    WHERE Age > 18
    """;
```
 

## 19. 튜플
튜플(tuple)은 **이름을 가진 여러 값의 묶음**이다. 클래스나 레코드를 새로 정의할 정도는 아니지만 임시로 두 개 이상의 값을 반환하고 싶을 때 유용하다.

`(bool, string)` 같은 **값 튜플(ValueTuple)** 은 구조체이므로 힙 할당이 없고, 필드마다 이름을 붙일 수도 있다. `Tuple<T1, T2>` 클래스도 있지만 구식이라 잘 쓰이지 않는다.

**분해(deconstruction)** 는 튜플의 각 요소를 개별 변수로 꺼내는 문법이다. `var (a, b) = GetTuple();` 형태로 쓴다.

```csharp
// 튜플 반환
public (bool Success, string Message) ValidateProduct(Product product)
{
    if (string.IsNullOrEmpty(product.Name))
    {
        return (false, "이름은 필수입니다");
    }
    
    if (product.Price <= 0)
    {
        return (false, "가격은 0보다 커야 합니다");
    }
    
    return (true, "유효합니다");
}

// 사용
var (success, message) = ValidateProduct(product);
if (!success)
{
    return BadRequest(message);
}

// 명명된 튜플
public (int Count, decimal Total) CalculateOrder(List<OrderItem> items)
{
    return (
        Count: items.Count,
        Total: items.Sum(i => i.Price * i.Quantity)
    );
}

// 사용
var result = CalculateOrder(items);
Console.WriteLine($"개수: {result.Count}, 합계: {result.Total}");
```

**언제 튜플을, 언제 레코드를?**

- **튜플**: 메서드 내부의 짧은 반환, 2~3개의 작은 값 묶음.
- **레코드**: 여러 곳에서 재사용되거나 API 계약에 노출되는 데이터 구조.

튜플을 외부 API에 노출하면 문서화가 어렵고 필드 이름이 소실될 수 있어 좋지 않다.
  

## 20. out 매개변수와 ref
매개변수 전달 방식은 세 가지가 있다.

- **값 전달(기본)**: 값이 복사된다.
- **`ref`**: 참조로 전달되어 호출자의 변수도 수정 가능하다. 호출 전 반드시 초기화되어 있어야 한다.
- **`out`**: 참조로 전달되지만 **호출된 메서드 내부에서 반드시 값을 지정**해야 한다. 호출 전 초기화되지 않아도 된다.
- **`in`** (C# 7.2+): 참조로 전달하되 수정은 금지한다. 큰 구조체의 읽기 전용 전달에 유용하다.

### 20.1 out 매개변수
`out`은 **"Try 패턴"** 의 핵심이다. "시도해보고 결과가 성공이면 값을, 실패면 false를 반환" 하는 관용구다. 예외 대신 불리언으로 성공 여부를 알리므로 성능이 중요한 경로에 적합하다.

```csharp
public bool TryGetUser(int id, out User? user)
{
    user = _users.FirstOrDefault(u => u.Id == id);
    return user != null;
}

// 사용
if (TryGetUser(1, out var user))
{
    Console.WriteLine(user.Name);
}

// Dictionary 패턴
if (_cache.TryGetValue("key", out var value))
{
    return value;
}
```

C# 7.0부터는 `out var`로 변수를 인라인 선언할 수 있어 코드가 훨씬 깔끔해졌다.

**실무 팁**: 현대 C#에서는 두 값 이상을 "반환"하고 싶을 때 `out`보다 **튜플이나 레코드**가 더 선호된다. `out`은 주로 BCL의 `TryParse`, `TryGetValue` 같은 관용구에서 만난다.
  

## 21. 값 타입 vs 참조 타입
C#의 타입은 크게 두 부류로 나뉜다.

**값 타입(value type)**:

- `struct`, `enum`, 모든 기본 숫자 타입, `bool`, `char`, 튜플, `DateTime`, `Guid` 등이다.
- **스택 또는 인클로징 객체 내부**에 저장된다 (정확히는 선언 위치에 따라 다르다).
- 대입 시 **복사**된다.
- `null`이 될 수 없다 (Nullable `<T>`를 감싸면 가능하다).

**참조 타입(reference type)**:

- `class`, `record`, `interface`, `string`, 배열, 델리게이트 등이다.
- **힙**에 저장된다.
- 대입 시 **참조(주소)** 가 복사된다. 같은 객체를 공유한다.
- `null`이 될 수 있다.

### 21.1 struct (값 타입)
`struct`는 값 타입을 직접 정의하는 방법이다. **작고(보통 16바이트 이하) 불변이며 동등성 비교가 의미 있는** 데이터에 적합하다.

- 게임 서버에서 좌표, 벡터, 색상 같은 수학적 값.
- 성능 크리티컬한 경로에서 힙 할당을 피하고 싶을 때.
- 의미상 한 덩어리의 값이며 참조 공유가 필요 없을 때.

`readonly struct`는 모든 필드가 변경 불가임을 보장한다. **방어적 복사(defensive copy)** 를 컴파일러가 생략할 수 있어 성능이 좋아진다.

```csharp
// 작은 불변 데이터에 적합
public readonly struct Point
{
    public int X { get; init; }
    public int Y { get; init; }
    
    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }
}

// record struct (C# 10)
public readonly record struct Coordinate(double Latitude, double Longitude);

// 사용
var coord = new Coordinate(37.5665, 126.9780);
```

`record struct`는 레코드의 편의성(값 동등성, `with` 표현식)과 구조체의 효율(힙 할당 없음)을 결합한다. **작은 값 객체에는 거의 최적의 선택**이다.

**주의**: 큰 `struct`(필드 많음)는 오히려 성능을 해친다. 복사 비용이 크기 때문이다. 의심스러우면 `class`를 쓰는 편이 안전하다.
  

## 22. 초기화 구문

### 22.1 객체 초기화
**객체 이니셜라이저(object initializer)** 는 `new` 직후에 중괄호로 프로퍼티를 지정하는 문법이다. 생성자를 호출한 뒤 프로퍼티를 대입하는 것과 같다. 생성자에 모든 프로퍼티를 받지 않아도 편하게 초기화할 수 있게 해준다.

- 컬렉션 이니셜라이저는 컬렉션 객체에도 같은 문법을 제공한다.
- **타겟 타입 `new()`** (C# 9+): 좌변 타입이 명확하면 우변에서 타입명을 생략할 수 있다 (`new List<Product> { new() { ... } }`).
- Dictionary 초기화는 `[키] = 값` 문법 또는 `{ 키, 값 }` 문법을 쓸 수 있다.

```csharp
// 객체 초기화
var product = new Product
{
    Name = "노트북",
    Price = 1500000,
    CreatedAt = DateTime.UtcNow
};

// 컬렉션 초기화
var products = new List<Product>
{
    new() { Name = "마우스", Price = 30000 },
    new() { Name = "키보드", Price = 80000 }
};

// Dictionary 초기화
var settings = new Dictionary<string, string>
{
    ["ConnectionString"] = "...",
    ["Timeout"] = "30"
};
```

### 22.2 컬렉션 표현식 (C# 12)
**컬렉션 표현식**은 `[...]` 문법으로 다양한 컬렉션 타입을 동일한 문법으로 초기화할 수 있게 해준다. **스프레드 연산자(`..`)** 는 기존 컬렉션을 풀어서 포함시킨다. JavaScript/TypeScript의 전개 구문과 유사하다.

컴파일러는 좌변 타입을 보고 가장 효율적인 방식으로 컬렉션을 만든다. 배열 대상이라면 배열로, `List<T>` 대상이라면 리스트로 만든다.

```csharp
// 배열
int[] numbers = [1, 2, 3, 4, 5];

// List
List<string> names = ["김철수", "이영희", "박민수"];

// 스프레드 연산자
int[] moreNumbers = [..numbers, 6, 7, 8];
List<string> allNames = [..names, "최수진"];
```
  

## 23. 조건부 컴파일 및 전처리기
전처리기 지시문(preprocessor directive)은 `#`으로 시작하며 **컴파일 전에 처리**된다. 디버그/릴리스 차이나 플랫폼 분기에 사용한다.

주요 지시문은 다음과 같다.

- `#if`, `#elif`, `#else`, `#endif`: 조건부 컴파일 블록이다.
- `#define`, `#undef`: 심볼을 정의하거나 해제한다.
- `#region`, `#endregion`: IDE에서 접을 수 있는 영역을 만든다.
- `#pragma warning disable/restore`: 특정 경고를 일시적으로 끈다.
- `#nullable enable/disable`: NRT 컨텍스트를 전환한다.

기본 심볼은 빌드 구성에 따라 자동으로 정의된다.

- `DEBUG`: Debug 빌드일 때 정의된다.
- `RELEASE`: Release 빌드일 때 정의된다.
- `NET8_0`, `NET9_0`: 대상 프레임워크 버전이다.

```csharp
#if DEBUG
    Console.WriteLine("디버그 모드");
#elif RELEASE
    Console.WriteLine("릴리스 모드");
#endif

public class ApiService
{
    private readonly string _baseUrl;
    
    public ApiService()
    {
#if DEBUG
        _baseUrl = "http://localhost:5000";
#else
        _baseUrl = "https://api.production.com";
#endif
    }
}
```

**실무 팁**: `#if`를 남용하면 가독성과 테스트 커버리지가 나빠진다. 설정 파일(`appsettings.json`, 환경 변수) 기반 분기가 훨씬 유연하고 유지보수하기 쉽다. 조건부 컴파일은 플랫폼별 API처럼 **런타임 전환이 불가능한 경우**에만 쓰는 것이 원칙이다.
  

## 24. 정적 클래스와 확장 메서드

### 24.1 정적 클래스
**정적 클래스(static class)** 는 인스턴스를 만들 수 없고 **정적 멤버만** 담는 클래스다. 전역적인 유틸리티 함수를 모아둘 때 사용한다.

- 모든 멤버가 `static`이어야 한다.
- 생성자를 정의할 수 없다 (정적 생성자는 가능하다).
- 상속이 불가능하다 (`sealed abstract`로 암묵적으로 취급된다).
- 확장 메서드는 **반드시 정적 클래스** 안에 있어야 한다.

```csharp
public static class DateTimeHelper
{
    public static DateTime StartOfDay(DateTime date)
    {
        return date.Date;
    }
    
    public static DateTime EndOfDay(DateTime date)
    {
        return date.Date.AddDays(1).AddTicks(-1);
    }
}

// 사용
var start = DateTimeHelper.StartOfDay(DateTime.Now);
```

### 24.2 확장 메서드 심화
Repository나 Query 빌더에 자주 쓰는 확장 메서드들을 모아두면 표현력이 크게 올라간다. 아래 `Paginate`는 `IQueryable<T>`에 페이징을 추가하는 매우 실용적인 예다.

```csharp
public static class QueryableExtensions
{
    public static IQueryable<T> Paginate<T>(
        this IQueryable<T> query,
        int page,
        int pageSize)
    {
        return query
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
    }
}

// 사용
var products = _context.Products
    .Where(p => p.Price > 100)
    .Paginate(page: 2, pageSize: 10)
    .ToListAsync();
```

`IQueryable`에 확장 메서드를 걸었기 때문에, EF Core는 이를 **SQL의 OFFSET/FETCH로 번역**한다. 즉 DB에서 페이징이 일어나 네트워크 트래픽과 메모리를 절약한다. 만약 `IEnumerable<T>`에 확장했다면 전체를 가져온 뒤 메모리에서 자르게 되어 성능이 나빠진다.

**호출 시 이름 있는 인수(named argument)** `Paginate(page: 2, pageSize: 10)`를 쓰면 의도가 명확해진다. 매개변수 순서를 외울 필요가 없다.
  

## 25. 상속과 다형성
상속은 **코드 재사용과 분류 관계(is-a)를 표현**하는 객체지향의 기본 도구다. 그러나 과도한 상속은 결합을 높이므로 **"composition over inheritance"** 원칙에 따라 필요할 때만 사용한다.

주요 키워드는 다음과 같다.

- `abstract`: 추상 클래스 또는 추상 멤버를 정의한다. 인스턴스를 만들 수 없고 파생 클래스에서 구현해야 한다.
- `virtual`: 파생 클래스가 재정의할 수 있는 메서드다.
- `override`: 기반 클래스의 virtual/abstract 멤버를 재정의한다.
- `sealed`: 더 이상 상속하거나 재정의할 수 없게 봉인한다.
- `base`: 기반 클래스의 멤버에 접근한다.
- `protected`: 파생 클래스에서 접근 가능한 접근 수준이다.

```csharp
// 기본 클래스
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// 상속
public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// 추상 서비스
public abstract class BaseService<T> where T : BaseEntity
{
    protected readonly DbContext _context;
    
    protected BaseService(DbContext context)
    {
        _context = context;
    }
    
    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _context.Set<T>().FindAsync(id);
    }
    
    public abstract Task<bool> ValidateAsync(T entity);
}

// 구체적 구현
public class ProductService : BaseService<Product>
{
    public ProductService(DbContext context) : base(context) { }
    
    public override async Task<bool> ValidateAsync(Product entity)
    {
        return !string.IsNullOrEmpty(entity.Name) && entity.Price > 0;
    }
}
```

**다형성(polymorphism)** 은 기반 타입의 참조로 파생 타입의 메서드를 호출할 때 **실제 객체의 타입**에 맞는 구현이 실행되는 것을 말한다. `BaseService<T>` 변수에 담아도 `ValidateAsync`는 실제 `ProductService`의 구현을 호출한다.

**인터페이스 vs 추상 클래스**:

- 공통 동작 구현을 공유해야 한다 → 추상 클래스.
- 단순히 계약만 필요하고 다중 상속이 필요하다 → 인터페이스.
- 현대 C#(8.0+)에서는 인터페이스에도 기본 구현을 넣을 수 있어 경계가 흐려졌다. 그러나 단위 테스트와 DI 관점에서는 여전히 **인터페이스가 우선**이다.
 

## 26. IDisposable과 리소스 관리
`IDisposable`은 **관리되지 않는 리소스를 명시적으로 해제**하기 위한 인터페이스다. DB 연결, 파일 핸들, 네트워크 소켓, 암호화 서비스 등은 GC가 처리하기 전에 미리 정리되어야 한다.

- `IDisposable.Dispose()`: 동기 해제다.
- `IAsyncDisposable.DisposeAsync()`: 비동기 해제다 (C# 8.0+). `await using`과 함께 쓴다.
- `using` 문: 블록 종료 시 자동으로 `Dispose`를 호출한다.
- **`using` 선언(C# 8)**: `using var x = ...` 한 줄로 변수의 스코프가 끝날 때 자동 해제된다.

**Dispose 패턴**은 파생 클래스에서도 올바르게 해제되도록 하는 관용구다. 직접 구현할 일은 드물고, 대부분 이미 `IDisposable`을 구현한 타입을 `using`으로 쓰기만 하면 된다.

```csharp
public class DatabaseConnection : IDisposable
{
    private SqlConnection? _connection;
    private bool _disposed = false;
    
    public DatabaseConnection(string connectionString)
    {
        _connection = new SqlConnection(connectionString);
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _connection?.Dispose();
            }
            _disposed = true;
        }
    }
}

// using 문
public async Task<List<User>> GetUsersAsync()
{
    using var connection = new DatabaseConnection(_connectionString);
    // connection은 자동으로 Dispose됨
    return await connection.QueryAsync<User>("SELECT * FROM Users");
}

// using 선언 (C# 8)
public async Task ProcessDataAsync()
{
    using var stream = File.OpenRead("data.txt");
    using var reader = new StreamReader(stream);
    // 메서드 끝에서 자동으로 Dispose
    var content = await reader.ReadToEndAsync();
}
```

**실무 팁**: ASP.NET Core DI 컨테이너는 **스코프 종료 시점에 `IDisposable` 객체들을 자동으로 해제**한다. 따라서 DI로 주입받은 서비스(`DbContext` 등)는 일반적으로 직접 `Dispose`하지 않는다. 직접 인스턴스화해 쓰는 경우에만 `using`이 필요하다.

`GC.SuppressFinalize(this)` 호출은 **파이널라이저가 있는 경우** 이미 `Dispose`로 정리했으니 GC가 두 번 호출하지 않도록 요청하는 것이다. 파이널라이저가 없으면 사실상 불필요하지만, 표준 패턴으로 남겨둔다.
  

## 27. 실전 종합 예제
모든 개념을 활용한 완전한 서비스 레이어다. 레코드 DTO, 제네릭 Repository, ApiResponse 래퍼, 필터/페이징, 튜플 검증, 확장 메서드가 모두 등장한다.

이 예제의 구조는 실제 중대형 ASP.NET Core Web API에서 흔히 볼 수 있는 **레이어드 아키텍처**다.

- **Models(Entity)**: DB 테이블에 매핑되는 도메인 객체.
- **DTOs**: 외부와 주고받는 데이터 모양. 엔티티와 분리해 내부 스키마 유출을 방지한다.
- **Repository**: 데이터 접근 계층. DB 기술을 캡슐화한다.
- **Service**: 비즈니스 로직. Repository를 사용하고 DTO를 반환한다.
- **Controller**: HTTP 요청을 받아 Service를 호출하고 응답을 반환한다 (여기서는 생략되었지만 구성 원칙은 12절과 동일하다).

```csharp
// Models
namespace MyApi.Models;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public ProductStatus Status { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
}

public enum ProductStatus
{
    Draft,
    Active,
    Discontinued
}

// DTOs
public record ProductDto(int Id, string Name, decimal Price, string Status);

public record CreateProductRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; init; } = string.Empty;
    
    [Range(0.01, 1000000)]
    public decimal Price { get; init; }
    
    public int CategoryId { get; init; }
}

public record ProductFilterRequest
{
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public ProductStatus? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

// Repository Interface
public interface IRepository<T> where T : BaseEntity
{
    Task<List<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
    IQueryable<T> Query();
}

// Service Interface
public interface IProductService
{
    Task<ApiResponse<List<ProductDto>>> GetAllAsync(ProductFilterRequest filter);
    Task<ApiResponse<ProductDto>> GetByIdAsync(int id);
    Task<ApiResponse<ProductDto>> CreateAsync(CreateProductRequest request);
    Task<ApiResponse<bool>> DeleteAsync(int id);
}

// Service Implementation
public class ProductService : IProductService
{
    private readonly IRepository<Product> _repository;
    private readonly ILogger<ProductService> _logger;
    private readonly IMapper _mapper;
    
    public ProductService(
        IRepository<Product> repository,
        ILogger<ProductService> logger,
        IMapper mapper)
    {
        _repository = repository;
        _logger = logger;
        _mapper = mapper;
    }
    
    public async Task<ApiResponse<List<ProductDto>>> GetAllAsync(
        ProductFilterRequest filter)
    {
        try
        {
            var query = _repository.Query();
            
            // 필터 적용
            if (filter.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= filter.MinPrice.Value);
            }
            
            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= filter.MaxPrice.Value);
            }
            
            if (filter.Status.HasValue)
            {
                query = query.Where(p => p.Status == filter.Status.Value);
            }
            
            // 페이징
            var products = await query
                .OrderBy(p => p.Name)
                .Paginate(filter.Page, filter.PageSize)
                .ToListAsync();
            
            var dtos = products.Select(p => new ProductDto(
                p.Id,
                p.Name,
                p.Price,
                p.Status.ToString()
            )).ToList();
            
            return ApiResponse<List<ProductDto>>.SuccessResult(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "제품 목록 조회 실패");
            return ApiResponse<List<ProductDto>>.ErrorResult(
                "제품 목록을 불러올 수 없습니다");
        }
    }
    
    public async Task<ApiResponse<ProductDto>> GetByIdAsync(int id)
    {
        var product = await _repository.GetByIdAsync(id);
        
        if (product is null)
        {
            return ApiResponse<ProductDto>.ErrorResult(
                $"제품 {id}를 찾을 수 없습니다");
        }
        
        var dto = new ProductDto(
            product.Id,
            product.Name,
            product.Price,
            product.Status.ToString()
        );
        
        return ApiResponse<ProductDto>.SuccessResult(dto);
    }
    
    public async Task<ApiResponse<ProductDto>> CreateAsync(
        CreateProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name,
            Price = request.Price,
            CategoryId = request.CategoryId,
            Status = ProductStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
        
        var (isValid, errorMessage) = await ValidateProductAsync(product);
        if (!isValid)
        {
            return ApiResponse<ProductDto>.ErrorResult(errorMessage);
        }
        
        var created = await _repository.AddAsync(product);
        
        var dto = new ProductDto(
            created.Id,
            created.Name,
            created.Price,
            created.Status.ToString()
        );
        
        return ApiResponse<ProductDto>.SuccessResult(dto);
    }
    
    public async Task<ApiResponse<bool>> DeleteAsync(int id)
    {
        var product = await _repository.GetByIdAsync(id);
        
        if (product is null)
        {
            return ApiResponse<bool>.ErrorResult("제품을 찾을 수 없습니다");
        }
        
        await _repository.DeleteAsync(id);
        
        return ApiResponse<bool>.SuccessResult(true);
    }
    
    private async Task<(bool IsValid, string ErrorMessage)> ValidateProductAsync(
        Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
        {
            return (false, "제품명은 필수입니다");
        }
        
        if (product.Price <= 0)
        {
            return (false, "가격은 0보다 커야 합니다");
        }
        
        // 중복 이름 체크
        var exists = await _repository.Query()
            .AnyAsync(p => p.Name == product.Name && p.Id != product.Id);
        
        if (exists)
        {
            return (false, "이미 존재하는 제품명입니다");
        }
        
        return (true, string.Empty);
    }
}

// Extension Methods
public static class QueryableExtensions
{
    public static IQueryable<T> Paginate<T>(
        this IQueryable<T> query,
        int page,
        int pageSize)
    {
        return query
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
    }
}

// API Response
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    
    public static ApiResponse<T> SuccessResult(T data) => new()
    {
        Success = true,
        Data = data
    };
    
    public static ApiResponse<T> ErrorResult(string error) => new()
    {
        Success = false,
        Error = error
    };
}
```

**이 코드에서 배울 점**:

- `IQueryable` 위에서 조건부로 `Where`를 쌓아 **동적 쿼리**를 만든다. 필요할 때만 조건이 추가되므로 SQL이 최적화된다.
- 검증 로직이 **튜플로 (성공, 메시지)** 를 반환해 호출자가 깔끔하게 분기한다.
- 서비스 메서드가 `ApiResponse<T>`로 통일되어 컨트롤러에서 HTTP 상태 코드 매핑만 담당하면 된다.
- `Paginate` 확장 메서드로 페이징 로직이 한 군데에 집중되어 있어 테스트하기 쉽다.
- DTO와 엔티티를 분리해 **DB 스키마 변경이 API 계약에 영향을 주지 않게** 보호한다.

**더 나아가기**:

- AutoMapper 대신 Mapperly 같은 소스 제너레이터 기반 매퍼를 고려할 만하다. 런타임 리플렉션이 없어 성능이 더 좋다.
- FluentValidation 라이브러리를 도입하면 복잡한 검증을 한 곳에 응집시킬 수 있다.
- 인증/인가는 `[Authorize]` 특성과 JWT Bearer 스키마를 조합하는 것이 표준이다.
- 관측성(observability)은 OpenTelemetry + Serilog + Grafana 조합이 현대적인 선택지다.

이 가이드의 각 절을 숙지하고 이 종합 예제를 여러 번 읽어보면, 실무 수준의 ASP.NET Core Web API를 설계하고 구현하는 데 필요한 C# 기반은 충분히 갖추게 된다.  