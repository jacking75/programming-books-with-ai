# 1주일만에 배우는 Redis 프로그래밍  

저자: 최흥배, Claude AI   
    
권장 개발 환경
- **IDE**: Visual Studio 2022 (Community 이상)
- **.NET**: 버전 9 이상
- **Redis**: 버전 6 이상

-----  
  
## Day 1: Redis 시작하기
### Chapter 1. Redis 소개와 설치
- 1.1 Redis란 무엇인가?
  - In-Memory 데이터 저장소의 특징
  - Redis의 장점과 활용 사례
  - 온라인 게임에서 Redis가 필요한 이유
- 1.2 Redis 설치 및 환경 설정
  - Windows/Linux에 Redis 6.0+ 설치
  - Redis CLI 기본 사용법
  - Redis 서버 설정 파일 이해하기
- 1.3 C# 프로젝트 설정
  - CloudStructures 라이브러리 소개
  - NuGet을 통한 CloudStructures 설치
  - Redis 연결 설정 및 테스트

## Day 2: Redis 핵심 아키텍처
### Chapter 2. Redis 내부 구조와 데이터 타입
- 2.1 Redis 아키텍처 개요
  - Single-Threaded 모델과 성능
  - 메모리 관리 방식
  - Persistence (RDB, AOF)
- 2.2 Redis 핵심 데이터 타입
  - String: 기본 키-값 저장
  - Hash: 객체 데이터 저장
  - List: 순서가 있는 데이터
  - Set: 중복 없는 집합
  - Sorted Set: 점수 기반 정렬 집합
- 2.3 CloudStructures로 데이터 타입 다루기
  - RedisString, RedisHash 사용법
  - RedisList, RedisSet, RedisSortedSet 활용

## Day 3: 게임 로그인과 세션 관리
### Chapter 3. 플레이어 세션과 인증
- 3.1 로그인 시스템 구현
  - 세션 토큰 생성 및 저장
  - String과 Hash를 활용한 세션 관리
  - 세션 만료 처리 (TTL)
- 3.2 중복 로그인 방지
  - Set을 활용한 동시 접속 관리
  - 디바이스별 로그인 제한
- 3.3 온라인 유저 목록 관리
  - Sorted Set으로 최근 접속 시간 추적
  - 접속자 수 실시간 집계

## Day 4: 게임 데이터 캐싱
### Chapter 4. 플레이어 데이터와 인벤토리
- 4.1 플레이어 프로필 캐싱
  - DB와 Redis 연동 전략
  - Cache-Aside 패턴 구현
  - 데이터 일관성 유지하기
- 4.2 인벤토리 시스템
  - Hash를 활용한 아이템 저장
  - 아이템 개수 증감 (HINCRBY)
  - 인벤토리 전체 조회 최적화
- 4.3 게임 설정과 메타데이터
  - 게임 서버 설정값 캐싱
  - 공지사항 및 이벤트 정보 관리

## Day 5: 실시간 랭킹과 리더보드
### Chapter 5. 경쟁 시스템 구축
- 5.1 실시간 랭킹 시스템
  - Sorted Set을 활용한 리더보드
  - 점수 업데이트 및 순위 조회
  - 상위/하위 N명 조회
- 5.2 시즌별 랭킹 관리
  - 키 네이밍 전략 (시즌, 구간별)
  - 랭킹 초기화 및 보상 처리
  - 내 순위 주변 유저 조회
- 5.3 길드/클랜 랭킹
  - 그룹별 점수 집계
  - 멤버 기여도 추적

## Day 6: 메시징과 알림
### Chapter 6. 실시간 통신 시스템
- 6.1 Pub/Sub 패턴
  - Redis Pub/Sub 개념
  - 채팅 시스템 구현
  - 서버 간 이벤트 브로드캐스팅
- 6.2 메일함 시스템
  - List를 활용한 메시지 큐
  - 읽지 않은 메시지 카운트
  - 메시지 만료 처리
- 6.3 친구 시스템
  - Set을 활용한 친구 목록
  - 친구 요청/수락 관리
  - 온라인 친구 표시

## Day 7: 게임 이벤트와 성능 최적화
### Chapter 7. 이벤트 시스템과 고급 활용
- 7.1 출석 체크 시스템
  - Bitmap을 활용한 출석 기록
  - 월간/주간 출석 현황 조회
  - 연속 출석 보상 처리
- 7.2 쿠폰과 선착순 이벤트
  - String을 활용한 쿠폰 코드 관리
  - INCR/DECR로 한정 수량 처리
  - 중복 사용 방지
- 7.3 Pipeline과 성능 최적화
  - 다중 명령어 일괄 처리
  - CloudStructures에서 Pipeline 사용
  - 대량 데이터 처리 최적화
- 7.4 모니터링과 운영 팁
  - 주요 명령어 (INFO, MONITOR, SLOWLOG)
  - 메모리 사용량 관리
  - 키 설계 모범 사례
  - 게임 서비스 운영 시 주의사항

---

## 부록
- A. CloudStructures 주요 API 레퍼런스
- B. Redis 명령어 치트시트

---



# 부록

## A. CloudStructures 주요 API 레퍼런스
CloudStructures는 StackExchange.Redis를 기반으로 한 C# Redis 클라이언트 라이브러리다. 타입 안전성과 간결한 API를 제공하여 Redis를 더 쉽게 사용할 수 있다.

### A.1 기본 연결 설정

```csharp
// RedisConnection 생성
var config = new RedisConfig("redis-server", "localhost:6379");
var connection = new RedisConnection(config);

// 여러 서버 연결 (Sentinel, Cluster)
var config = new RedisConfig("redis-cluster", 
    "server1:6379,server2:6379,server3:6379");
```

**주요 설정 옵션:**
- `connectTimeout`: 연결 타임아웃 (기본값: 5000ms)
- `syncTimeout`: 동기 작업 타임아웃 (기본값: 1000ms)
- `allowAdmin`: 관리자 명령어 허용 여부
- `password`: Redis 서버 비밀번호
- `defaultDatabase`: 기본 DB 번호 (0-15)

### A.2 RedisString

문자열 및 숫자 값을 저장하고 조회한다.

```csharp
var redis = connection.GetRedisString<string>("mykey");

// 저장 및 조회
await redis.Set("Hello Redis");
var value = await redis.Get();  // "Hello Redis"

// TTL 설정
await redis.Set("Session", TimeSpan.FromMinutes(30));

// 존재하지 않을 때만 저장
bool success = await redis.SetIfNotExists("key", "value");

// 숫자 증감
var counter = connection.GetRedisString<long>("counter");
await counter.Increment(1);      // +1
await counter.Increment(10);     // +10
await counter.Decrement(5);      // -5
```

**주요 메서드:**
- `Set(value, expiry)`: 값 저장
- `Get()`: 값 조회
- `SetIfNotExists(value, expiry)`: 키가 없을 때만 저장
- `Increment(value)`: 숫자 증가
- `Decrement(value)`: 숫자 감소
- `GetWithExpiry()`: 값과 만료 시간 함께 조회

### A.3 RedisHash
해시맵 구조로 여러 필드-값 쌍을 저장한다.

```csharp
var redis = connection.GetRedisHash<Player>("player:1001");

// 단일 필드 저장/조회
await redis.Set("name", "Alice");
await redis.Set("level", 10);
string name = await redis.Get("name");

// 여러 필드 한 번에 저장
var player = new Dictionary<string, int>
{
    { "level", 10 },
    { "exp", 1500 },
    { "gold", 50000 }
};
await redis.SetFields(player);

// 전체 필드 조회
var allFields = await redis.GetAll();

// 숫자 필드 증감
await redis.Increment("gold", 1000);

// 필드 삭제
await redis.Delete("temp_field");

// 필드 존재 여부 확인
bool exists = await redis.Exists("level");
```

**주요 메서드:**
- `Set(field, value)`: 필드 값 설정
- `Get(field)`: 필드 값 조회
- `GetAll()`: 모든 필드-값 조회
- `SetFields(dictionary)`: 여러 필드 한 번에 설정
- `Increment(field, value)`: 숫자 필드 증가
- `Delete(field)`: 필드 삭제
- `Length()`: 필드 개수 조회

### A.4 RedisList

순서가 있는 리스트를 관리한다.

```csharp
var redis = connection.GetRedisList<string>("queue");

// 왼쪽/오른쪽에 추가
await redis.LeftPush("item1");
await redis.RightPush("item2");

// 왼쪽/오른쪽에서 제거
string left = await redis.LeftPop();
string right = await redis.RightPop();

// 범위 조회 (0부터 시작)
var items = await redis.Range(0, 9);  // 처음 10개

// 길이 조회
long length = await redis.Length();

// 특정 인덱스 값 조회
string item = await redis.GetByIndex(0);

// 리스트 자르기 (범위 외 삭제)
await redis.Trim(0, 99);  // 처음 100개만 유지
```

**주요 메서드:**
- `LeftPush(value)`: 왼쪽에 추가
- `RightPush(value)`: 오른쪽에 추가
- `LeftPop()`: 왼쪽에서 제거 및 반환
- `RightPop()`: 오른쪽에서 제거 및 반환
- `Range(start, stop)`: 범위 조회
- `Length()`: 길이 조회
- `Trim(start, stop)`: 범위 외 삭제

### A.5 RedisSet
중복 없는 집합을 관리한다.

```csharp
var redis = connection.GetRedisSet<string>("online_users");

// 추가
await redis.Add("user1");
await redis.Add(new[] { "user2", "user3", "user4" });

// 제거
await redis.Remove("user1");

// 멤버 존재 여부
bool exists = await redis.Contains("user2");

// 모든 멤버 조회
var members = await redis.Members();

// 개수 조회
long count = await redis.Length();

// 집합 연산
var set1 = connection.GetRedisSet<string>("set1");
var set2 = connection.GetRedisSet<string>("set2");

// 합집합
var union = await set1.Union(set2.Key);

// 교집합
var intersect = await set1.Intersect(set2.Key);

// 차집합
var diff = await set1.Difference(set2.Key);
```

**주요 메서드:**
- `Add(value)`: 멤버 추가
- `Remove(value)`: 멤버 제거
- `Contains(value)`: 멤버 존재 확인
- `Members()`: 모든 멤버 조회
- `Length()`: 멤버 개수
- `Union(otherKeys)`: 합집합
- `Intersect(otherKeys)`: 교집합
- `Difference(otherKeys)`: 차집합

### A.6 RedisSortedSet
점수 기반으로 정렬된 집합을 관리한다.

```csharp
var redis = connection.GetRedisSortedSet<string>("ranking");

// 추가 (멤버, 점수)
await redis.Add("player1", 1000);
await redis.Add("player2", 1500);

// 점수 증가
await redis.Increment("player1", 100);  // 1000 -> 1100

// 점수로 범위 조회 (오름차순)
var top10 = await redis.RangeByRank(0, 9);

// 점수로 범위 조회 (내림차순)
var top10Desc = await redis.RangeByRankDescending(0, 9);

// 점수 범위로 조회
var range = await redis.RangeByScore(1000, 2000);

// 순위 조회 (0부터 시작)
long? rank = await redis.Rank("player1");  // 오름차순 순위
long? rankDesc = await redis.RankDescending("player1");  // 내림차순 순위

// 점수 조회
double? score = await redis.Score("player1");

// 제거
await redis.Remove("player1");

// 점수 범위로 제거
await redis.RemoveRangeByScore(0, 100);

// 개수 조회
long count = await redis.Length();
```

**주요 메서드:**
- `Add(member, score)`: 멤버 추가 (점수 포함)
- `Increment(member, value)`: 점수 증가
- `RangeByRank(start, stop)`: 순위 기준 범위 조회 (오름차순)
- `RangeByRankDescending(start, stop)`: 순위 기준 범위 조회 (내림차순)
- `RangeByScore(min, max)`: 점수 기준 범위 조회
- `Rank(member)`: 순위 조회 (오름차순)
- `Score(member)`: 점수 조회
- `Remove(member)`: 멤버 제거

### A.7 RedisBit (Bitmap)
비트 단위로 데이터를 저장하고 조작한다.

```csharp
var redis = connection.GetRedisBit("attendance:2025-12");

// 비트 설정
await redis.SetBit(0, true);   // 1일 출석
await redis.SetBit(1, true);   // 2일 출석

// 비트 조회
bool attended = await redis.GetBit(0);

// 비트 카운트 (true인 비트 개수)
long count = await redis.BitCount();
long rangeCount = await redis.BitCount(0, 6);  // 0~6 범위

// 비트 연산
var bit1 = connection.GetRedisBit("bitmap1");
var bit2 = connection.GetRedisBit("bitmap2");
await bit1.BitwiseAnd("result", bit2.Key);  // AND 연산
await bit1.BitwiseOr("result", bit2.Key);   // OR 연산
await bit1.BitwiseXor("result", bit2.Key);  // XOR 연산
```

**주요 메서드:**
- `SetBit(offset, value)`: 비트 설정
- `GetBit(offset)`: 비트 조회
- `BitCount(start, end)`: 1인 비트 개수
- `BitwiseAnd/Or/Xor(destination, keys)`: 비트 연산

### A.8 공통 기능
모든 데이터 타입에서 사용할 수 있는 공통 메서드다.

```csharp
var redis = connection.GetRedisString<string>("mykey");

// 키 삭제
await redis.Delete();

// 키 존재 여부
bool exists = await redis.KeyExists();

// TTL 설정
await redis.Expire(TimeSpan.FromMinutes(10));

// TTL 조회
TimeSpan? ttl = await redis.TimeToLive();

// TTL 제거 (영구 보관)
await redis.Persist();

// 키 이름 변경
await redis.Rename("newkey");
```

### A.9 트랜잭션과 파이프라인
여러 명령을 원자적으로 또는 일괄 처리한다.

```csharp
// 트랜잭션
var tran = connection.CreateTransaction();
var key1 = tran.GetRedisString<int>("counter1");
var key2 = tran.GetRedisString<int>("counter2");

key1.Increment(1);
key2.Increment(1);

bool success = await tran.Execute();

// 파이프라인
var tasks = new List<Task>();
for (int i = 0; i < 100; i++)
{
    var redis = connection.GetRedisString<string>($"key:{i}");
    tasks.Add(redis.Set($"value{i}"));
}
await Task.WhenAll(tasks);
```

### A.10 Pub/Sub
메시지 발행과 구독을 처리한다.

```csharp
// 구독
var subscriber = connection.Connection.GetSubscriber();
await subscriber.SubscribeAsync("chat:room1", (channel, message) =>
{
    Console.WriteLine($"Received: {message}");
});

// 발행
await subscriber.PublishAsync("chat:room1", "Hello!");

// 구독 취소
await subscriber.UnsubscribeAsync("chat:room1");
```

---

## B. Redis 명령어 치트시트
게임 개발에서 자주 사용하는 Redis 명령어를 데이터 타입별로 정리했다.

### B.1 키 관리

```
DEL key [key ...]              # 키 삭제
EXISTS key [key ...]           # 키 존재 확인
EXPIRE key seconds             # 만료 시간 설정 (초)
EXPIREAT key timestamp         # 특정 시각에 만료
TTL key                        # 남은 만료 시간 (초)
PERSIST key                    # 만료 시간 제거
RENAME key newkey              # 키 이름 변경
TYPE key                       # 키의 데이터 타입 조회
KEYS pattern                   # 패턴에 맞는 키 검색 (운영 환경 주의!)
SCAN cursor [MATCH pattern]    # 키 순회 (안전한 방식)
```

**게임 활용 예시:**
```
# 세션 만료 설정
SET session:abc123 "user_data"
EXPIRE session:abc123 1800

# 임시 데이터 삭제
DEL temp:battle:12345

# 키 패턴 검색 (개발 환경)
KEYS player:*
KEYS ranking:season:2025:*
```

### B.2 String 명령어

```
SET key value [EX seconds]     # 값 저장 및 만료 설정
GET key                        # 값 조회
SETNX key value                # 키가 없을 때만 저장
SETEX key seconds value        # 만료 시간과 함께 저장
MSET key value [key value ...] # 여러 키 한 번에 저장
MGET key [key ...]             # 여러 키 한 번에 조회
INCR key                       # 1 증가
INCRBY key increment           # 지정한 값만큼 증가
DECR key                       # 1 감소
DECRBY key decrement           # 지정한 값만큼 감소
APPEND key value               # 문자열 뒤에 추가
GETRANGE key start end         # 부분 문자열 조회
```

**게임 활용 예시:**
```
# 세션 토큰 저장 (30분 만료)
SETEX session:token123 1800 "user:1001"

# 동시 접속자 카운트
INCR online:count

# 골드 차감
DECRBY player:1001:gold 1000

# 쿠폰 사용 (SETNX로 중복 방지)
SETNX coupon:SUMMER2025:user1001 1
```

### B.3 Hash 명령어

```
HSET key field value           # 필드 설정
HGET key field                 # 필드 조회
HMSET key field value [...]    # 여러 필드 설정
HMGET key field [field ...]    # 여러 필드 조회
HGETALL key                    # 모든 필드 조회
HDEL key field [field ...]     # 필드 삭제
HEXISTS key field              # 필드 존재 확인
HINCRBY key field increment    # 숫자 필드 증가
HKEYS key                      # 모든 필드명 조회
HVALS key                      # 모든 값 조회
HLEN key                       # 필드 개수 조회
```

**게임 활용 예시:**
```
# 플레이어 정보 저장
HMSET player:1001 name "Alice" level 10 exp 1500 gold 50000

# 골드 증가
HINCRBY player:1001 gold 1000

# 레벨과 경험치 조회
HMGET player:1001 level exp

# 아이템 개수 증가
HINCRBY inventory:1001 item:sword 5
```

### B.4 List 명령어

```
LPUSH key value [value ...]    # 왼쪽에 추가
RPUSH key value [value ...]    # 오른쪽에 추가
LPOP key                       # 왼쪽에서 제거
RPOP key                       # 오른쪽에서 제거
LRANGE key start stop          # 범위 조회
LLEN key                       # 길이 조회
LINDEX key index               # 인덱스로 조회
LTRIM key start stop           # 범위 외 삭제
LREM key count value           # 특정 값 제거
```

**게임 활용 예시:**
```
# 최근 전투 기록 (최대 10개 유지)
LPUSH battle:log:1001 "win:boss:2025-12-26"
LTRIM battle:log:1001 0 9

# 메일함
RPUSH mail:1001 "welcome_message"
LRANGE mail:1001 0 -1

# 채팅 메시지 (최근 100개)
LPUSH chat:room1 "Alice: Hello!"
LTRIM chat:room1 0 99
```

### B.5 Set 명령어

```
SADD key member [member ...]   # 멤버 추가
SREM key member [member ...]   # 멤버 제거
SISMEMBER key member           # 멤버 존재 확인
SMEMBERS key                   # 모든 멤버 조회
SCARD key                      # 멤버 개수
SUNION key [key ...]           # 합집합
SINTER key [key ...]           # 교집합
SDIFF key [key ...]            # 차집합
SPOP key [count]               # 랜덤 제거 및 반환
SRANDMEMBER key [count]        # 랜덤 조회
```

**게임 활용 예시:**
```
# 온라인 유저 목록
SADD online:users user1001 user1002
SISMEMBER online:users user1001

# 친구 목록
SADD friends:1001 user1002 user1003
SCARD friends:1001

# 공통 친구 찾기
SINTER friends:1001 friends:1002

# 랜덤 보상 뽑기
SPOP gacha:items 1
```

### B.6 Sorted Set 명령어

```
ZADD key score member [...]    # 멤버 추가 (점수 포함)
ZREM key member [member ...]   # 멤버 제거
ZINCRBY key increment member   # 점수 증가
ZSCORE key member              # 점수 조회
ZRANK key member               # 순위 조회 (오름차순, 0부터)
ZREVRANK key member            # 순위 조회 (내림차순, 0부터)
ZRANGE key start stop          # 순위 범위 조회 (오름차순)
ZREVRANGE key start stop       # 순위 범위 조회 (내림차순)
ZRANGEBYSCORE key min max      # 점수 범위 조회
ZCOUNT key min max             # 점수 범위 개수
ZCARD key                      # 멤버 개수
ZREMRANGEBYSCORE key min max   # 점수 범위 삭제
ZREMRANGEBYRANK key start stop # 순위 범위 삭제
```

**게임 활용 예시:**
```
# 랭킹 시스템
ZADD ranking:global 15000 player1001
ZINCRBY ranking:global 100 player1001

# 상위 10명 조회
ZREVRANGE ranking:global 0 9 WITHSCORES

# 내 순위 조회
ZREVRANK ranking:global player1001

# 특정 점수 이하 삭제 (시즌 정리)
ZREMRANGEBYSCORE ranking:season1 -inf 1000
```

### B.7 Bitmap 명령어

```
SETBIT key offset value        # 비트 설정
GETBIT key offset              # 비트 조회
BITCOUNT key [start end]       # 1인 비트 개수
BITOP AND|OR|XOR dest key ...  # 비트 연산
BITPOS key bit [start] [end]   # 특정 비트 위치 찾기
```

**게임 활용 예시:**
```
# 출석 체크 (일별)
SETBIT attendance:1001:2025-12 25 1

# 이번 달 출석 일수
BITCOUNT attendance:1001:2025-12

# 연속 출석 체크
GETBIT attendance:1001:2025-12 24
GETBIT attendance:1001:2025-12 25
GETBIT attendance:1001:2025-12 26
```

### B.8 Pub/Sub 명령어

```
PUBLISH channel message        # 메시지 발행
SUBSCRIBE channel [channel ...] # 채널 구독
UNSUBSCRIBE [channel ...]      # 구독 취소
PSUBSCRIBE pattern [...]       # 패턴 구독
PUNSUBSCRIBE [pattern ...]     # 패턴 구독 취소
PUBSUB CHANNELS [pattern]      # 활성 채널 목록
PUBSUB NUMSUB [channel ...]    # 채널별 구독자 수
```

**게임 활용 예시:**
```
# 전체 공지
PUBLISH broadcast:all "Server maintenance in 10 minutes"

# 채팅방
PUBLISH chat:room:1001 "Alice: Hello everyone!"

# 길드 채널
SUBSCRIBE guild:12345
```

### B.9 서버 관리

```
INFO [section]                 # 서버 정보 조회
DBSIZE                         # 키 개수 조회
FLUSHDB                        # 현재 DB 전체 삭제
FLUSHALL                       # 모든 DB 전체 삭제
SAVE                           # 동기 저장 (블로킹)
BGSAVE                         # 비동기 저장
CONFIG GET parameter           # 설정 조회
CONFIG SET parameter value     # 설정 변경
CLIENT LIST                    # 연결된 클라이언트 목록
SLOWLOG GET [count]            # 느린 쿼리 로그
MONITOR                        # 실시간 명령어 모니터링
```

**게임 운영 예시:**
```
# 메모리 사용량 확인
INFO memory

# 느린 쿼리 확인
SLOWLOG GET 10

# 현재 키 개수
DBSIZE

# 설정 조회
CONFIG GET maxmemory
CONFIG GET maxmemory-policy
```

### B.10 명령어 성능 가이드

```
┌─────────────────┬──────────────┬────────────────────────┐
│ 복잡도          │ 명령어       │ 설명                   │
├─────────────────┼──────────────┼────────────────────────┤
│ O(1)            │ GET, SET     │ 단일 키 조회/설정      │
│                 │ HGET, HSET   │ 해시 단일 필드         │
│                 │ SADD, SREM   │ Set 추가/제거          │
│                 │ ZADD         │ Sorted Set 추가        │
│                 │ LPUSH, RPUSH │ List 추가              │
├─────────────────┼──────────────┼────────────────────────┤
│ O(log N)        │ ZADD, ZREM   │ Sorted Set 정렬 필요   │
│                 │ ZRANK        │ Sorted Set 순위 조회   │
├─────────────────┼──────────────┼────────────────────────┤
│ O(N)            │ KEYS         │ 전체 키 검색 (위험!)   │
│                 │ HGETALL      │ 해시 전체 조회         │
│                 │ SMEMBERS     │ Set 전체 조회          │
│                 │ LRANGE       │ List 범위 조회         │
│                 │ ZRANGE       │ Sorted Set 범위 조회   │
├─────────────────┼──────────────┼────────────────────────┤
│ O(N*M)          │ SUNION       │ 여러 Set 합집합        │
│                 │ SINTER       │ 여러 Set 교집합        │
└─────────────────┴──────────────┴────────────────────────┘
```

**운영 시 주의사항:**
- `KEYS` 명령어는 운영 환경에서 사용하지 않는다 (전체 키 스캔으로 블로킹 발생)
- 대신 `SCAN`을 사용하여 커서 기반으로 순회한다
- `HGETALL`, `SMEMBERS` 등은 데이터 크기가 클 때 성능 저하가 발생한다
- 큰 컬렉션은 `HSCAN`, `SSCAN`, `ZSCAN`을 사용한다  