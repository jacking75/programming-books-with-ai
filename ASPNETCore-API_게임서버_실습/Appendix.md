# ASP.NET Web API를 이용한 API 게임 서버 실습  

저자: 최흥배, AI-Assisted   
    
권장 개발 환경
- **IDE**: Visual Studio 2022 (Community 이상)
- **.NET**: .NET 9 이상  
- **OS**: Windows 10 이상
  
-----  
  
# 부록

## A. 수집형 RPG 게임 기획서 샘플

### 게임 개요
본 샘플 기획서는 수집형 RPG 게임 서버 개발 실습을 위한 기본 게임 시스템을 정의한다.

**게임 장르**: 수집형 RPG  
**플랫폼**: 모바일 (Android/iOS)  
**게임 방식**: 비동기 턴제 전투, 캐릭터 수집 및 육성

### 핵심 게임 루프
1. 던전 진행을 통한 보상 획득
2. 캐릭터 및 아이템 수집
3. 캐릭터 육성 및 강화
4. 더 높은 난이도의 던전 도전

### 게임 시스템 구성

#### 1. 계정 시스템
- 계정 생성: 이메일 기반 회원가입
- 로그인: 이메일과 비밀번호 인증
- 닉네임: 중복 불가, 2~12자 한글/영문/숫자
- 레벨: 최대 레벨 100
- 경험치: 던전 클리어 시 획득

#### 2. 캐릭터 시스템
- 등급: Common, Rare, Epic, Legendary
- 레벨: 캐릭터당 최대 레벨 50
- 능력치: HP, 공격력, 방어력, 속도
- 획득 방법: 가챠, 던전 보상, 상점 구매
- 최대 보유: 100개

#### 3. 아이템 시스템
- 아이템 타입: 장비, 소비, 재료, 화폐
- 장비: 무기, 방어구, 악세서리
- 소비: 포션, 경험치 부스터
- 재료: 강화 재료, 진화 재료
- 화폐: 골드, 다이아몬드
- 인벤토리: 기본 100칸, 확장 가능 (최대 500칸)

#### 4. 던전 시스템
- 스테이지 구성: 월드 10개, 월드당 스테이지 10개
- 난이도: Normal, Hard, Hell
- 입장 제한: 스태미너 소모 (스태미너 최대치 100, 5분당 1회복)
- 보상: 골드, 경험치, 아이템, 캐릭터
- 클리어 조건: 모든 적 처치

#### 5. 강화 시스템
- 캐릭터 강화: 골드와 경험치 소모
- 장비 강화: 골드와 강화석 소모
- 강화 레벨: +0 ~ +10
- 강화 성공률: 레벨에 따라 차등 (100% ~ 30%)

#### 6. 출석 시스템
- 연속 출석 보상: 7일 사이클
- 누적 출석 보상: 월간 출석 일수 기준
- 보상: 골드, 다이아몬드, 아이템

#### 7. 미션 시스템
- 일일 미션: 매일 자정 초기화
- 주간 미션: 매주 월요일 초기화
- 업적: 영구 달성 목표
- 미션 타입: 던전 클리어, 강화, 로그인, 아이템 수집

#### 8. 우편함 시스템
- 보관 기간: 7일
- 우편 타입: 시스템, 보상, 친구
- 보상 수령: 아이템, 골드, 다이아몬드
- 읽음/안읽음 표시

#### 9. 상점 시스템
- 골드 상점: 골드로 아이템 구매
- 다이아몬드 상점: 다이아몬드로 프리미엄 아이템 구매
- 갱신 주기: 일일 자정, 주간 월요일
- 구매 제한: 일일/주간 구매 가능 횟수

#### 10. 친구 시스템
- 친구 상한: 최대 50명
- 친구 검색: 닉네임 또는 유저 ID
- 선물 보내기: 하루 1회, 스태미너 또는 골드
- 친구 랭킹: 친구 간 레벨 및 전투력 순위

#### 11. 랭킹 시스템
- 랭킹 타입: 레벨 랭킹, 전투력 랭킩, 던전 클리어 랭킹
- 갱신 주기: 실시간
- 보상: 시즌별 상위 랭커 보상
- 표시 범위: 상위 100위, 내 주변 순위

### 게임 재화 설계

#### 골드
- 획득: 던전 클리어, 미션 보상, 아이템 판매
- 사용: 캐릭터 강화, 장비 강화, 상점 구매

#### 다이아몬드
- 획득: 과금, 이벤트 보상, 출석 보상
- 사용: 가챠, 프리미엄 상점, 스태미너 회복

#### 스태미너
- 최대치: 100
- 회복: 5분당 1
- 사용: 던전 입장 시 소모
- 즉시 회복: 다이아몬드 소모

### 전투 시스템

#### 전투 방식
- 턴제 기반 자동 전투
- 속도 능력치 기준 행동 순서 결정
- 스킬 사용: 자동 발동

#### 전투 결과
- 승리: 보상 획득, 다음 스테이지 진행 가능
- 패배: 보상 없음, 소모한 스태미너는 반환되지 않음

#### 보상 시스템
- 기본 보상: 골드, 경험치
- 추가 보상: 확률 기반 아이템 드롭
- 첫 클리어 보상: 다이아몬드 추가 지급

### 데이터 테이블 참조
본 기획서의 시스템 구현을 위한 데이터베이스 스키마는 부록 C를 참조한다.

### 밸런싱 가이드

#### 경험치 획득량
- Lv1 → Lv2: 100 EXP
- 레벨당 증가율: 이전 레벨 필요 경험치 × 1.2

#### 강화 비용
- 캐릭터 강화: (레벨 × 100) 골드
- 장비 강화: (강화 레벨 × 500) 골드 + 강화석

#### 던전 스태미너 소모
- Normal: 5 스태미너
- Hard: 10 스태미너
- Hell: 15 스태미너

---

## B. API 명세서 작성 가이드 (UserId, Token 포함)

### API 명세서 기본 구조

API 명세서는 클라이언트 개발자와 서버 개발자 간의 약속이다. 명확하고 일관된 형식으로 작성한다.

### 공통 규칙

#### 요청 형식
- 모든 API는 HTTP POST 메서드를 사용한다
- Content-Type은 `application/json`을 사용한다
- 인증이 필요한 API는 Header에 Token을 포함한다

#### 응답 형식
모든 API는 다음 구조를 따른다:

```json
{
  "result": 0,
  "message": "성공",
  "data": {}
}
```

- `result`: 결과 코드 (0: 성공, 1 이상: 오류 코드)
- `message`: 결과 메시지
- `data`: 응답 데이터 (없을 경우 빈 객체)

#### 오류 코드 체계
- 1000번대: 인증 관련 오류
- 2000번대: 데이터베이스 관련 오류
- 3000번대: 비즈니스 로직 오류
- 4000번대: 파라미터 검증 오류
- 5000번대: 서버 내부 오류

### API 명세서 작성 템플릿

#### 1. API 기본 정보

```
# API 명: 로그인
- URL: /api/auth/login
- Method: POST
- 인증 필요: 없음
- 설명: 이메일과 비밀번호로 로그인하여 인증 토큰을 발급받는다
```

#### 2. Request (요청)

```json
{
  "email": "user@example.com",
  "password": "password123"
}
```

**Request Parameters**

| 파라미터 | 타입 | 필수 | 설명 | 제약사항 |
|---------|------|------|------|---------|
| email | string | O | 사용자 이메일 | 이메일 형식, 최대 100자 |
| password | string | O | 비밀번호 | 6~20자 |

#### 3. Response (응답)

**성공 시 (result: 0)**

```json
{
  "result": 0,
  "message": "로그인 성공",
  "data": {
    "userId": 12345,
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "nickname": "플레이어1",
    "level": 10
  }
}
```

**Response Data Fields**

| 필드 | 타입 | 설명 |
|-----|------|------|
| userId | long | 사용자 고유 ID |
| token | string | 인증 토큰 (이후 API 호출 시 사용) |
| nickname | string | 사용자 닉네임 |
| level | int | 사용자 레벨 |

**실패 시 (result: 1 이상)**

```json
{
  "result": 1001,
  "message": "이메일 또는 비밀번호가 일치하지 않습니다",
  "data": {}
}
```

#### 4. Error Codes (오류 코드)

| 코드 | 메시지 | 설명 |
|-----|--------|------|
| 1001 | 이메일 또는 비밀번호가 일치하지 않습니다 | 인증 정보 불일치 |
| 4001 | 이메일 형식이 올바르지 않습니다 | 파라미터 검증 실패 |
| 4002 | 비밀번호는 6~20자여야 합니다 | 파라미터 검증 실패 |
| 5000 | 서버 내부 오류가 발생했습니다 | 예상치 못한 오류 |

### 인증이 필요한 API 작성 예시

#### API 기본 정보

```
# API 명: 던전 입장
- URL: /api/dungeon/enter
- Method: POST
- 인증 필요: 있음
- 설명: 특정 던전 스테이지에 입장한다
```

#### Request Headers

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

**Header Parameters**

| 헤더 | 타입 | 필수 | 설명 |
|-----|------|------|------|
| Authorization | string | O | Bearer 토큰 형식의 인증 토큰 |

#### Request Body

```json
{
  "stageId": 101
}
```

**Request Parameters**

| 파라미터 | 타입 | 필수 | 설명 | 제약사항 |
|---------|------|------|------|---------|
| stageId | int | O | 입장할 스테이지 ID | 1 이상 |

#### Response

**성공 시**

```json
{
  "result": 0,
  "message": "던전 입장 성공",
  "data": {
    "dungeonSessionId": "abc123def456",
    "stageId": 101,
    "staminaUsed": 5,
    "remainingStamina": 45,
    "enemies": [
      {
        "enemyId": 1001,
        "name": "슬라임",
        "hp": 100,
        "attack": 10
      }
    ]
  }
}
```

**Response Data Fields**

| 필드 | 타입 | 설명 |
|-----|------|------|
| dungeonSessionId | string | 던전 세션 ID (클리어 시 사용) |
| stageId | int | 입장한 스테이지 ID |
| staminaUsed | int | 소모된 스태미너 |
| remainingStamina | int | 남은 스태미너 |
| enemies | array | 적 정보 목록 |

#### Error Codes

| 코드 | 메시지 | 설명 |
|-----|--------|------|
| 1002 | 토큰이 유효하지 않습니다 | 토큰 검증 실패 |
| 1003 | 토큰이 만료되었습니다 | 토큰 만료 |
| 3001 | 스태미너가 부족합니다 | 스태미너 부족 |
| 3002 | 존재하지 않는 스테이지입니다 | 잘못된 스테이지 ID |
| 3003 | 이전 스테이지를 먼저 클리어해야 합니다 | 진행 조건 미충족 |

### UserId 활용 방법

인증이 필요한 API에서 UserId는 서버 내부에서 토큰을 통해 자동으로 추출한다.

```csharp
// 미들웨어에서 토큰 검증 후 UserId를 HttpContext에 저장
var userId = long.Parse(httpContext.Items["UserId"].ToString());
```

클라이언트는 UserId를 요청 파라미터로 보내지 않는다. 서버가 토큰에서 추출하여 사용한다.

### Token 활용 방법

#### 토큰 발급
- 로그인 성공 시 서버가 토큰을 생성하여 반환한다
- 토큰에는 UserId, 발급 시간, 만료 시간이 포함된다

#### 토큰 사용
- 클라이언트는 모든 인증이 필요한 API 호출 시 Header에 토큰을 포함한다
- 형식: `Authorization: Bearer {token}`

#### 토큰 만료
- 토큰은 발급 후 24시간 유효하다
- 만료 시 클라이언트는 재로그인을 수행한다

### API 명세서 작성 시 주의사항

1. **일관성 유지**: 모든 API가 동일한 형식을 따르도록 한다
2. **명확한 설명**: 각 파라미터와 필드의 의미를 명확히 기술한다
3. **실제 예시**: 실제 사용 가능한 JSON 예시를 제공한다
4. **오류 케이스 문서화**: 발생 가능한 모든 오류 코드를 명시한다
5. **버전 관리**: API 변경 시 버전 정보를 기록한다

### API 명세서 문서 관리

API 명세서는 다음 도구를 활용하여 관리할 수 있다:

- **Markdown 파일**: Git으로 버전 관리
- **Swagger/OpenAPI**: 자동 문서 생성
- **Postman Collection**: 테스트 케이스 포함
- **Notion/Confluence**: 팀 협업용 위키

본 교육에서는 Markdown 파일로 작성하며 Git으로 관리한다.

---

## C. MySQL 데이터베이스 스키마 전체 구조

### 데이터베이스 개요

본 수집형 RPG 게임은 MySQL 데이터베이스를 사용하여 영구 데이터를 저장한다.

**데이터베이스 명**: `game_db`  
**문자셋**: utf8mb4  
**Collation**: utf8mb4_unicode_ci

### 스키마 설계 원칙

1. **정규화**: 데이터 중복을 최소화하고 무결성을 유지한다
2. **인덱스**: 자주 조회되는 컬럼에 인덱스를 설정한다
3. **타임스탬프**: 생성 시간과 수정 시간을 기록한다
4. **소프트 삭제**: 데이터 복구를 위해 삭제 플래그를 사용한다

### 테이블 목록

#### 사용자 관련 테이블
1. `users` - 사용자 계정 정보
2. `user_game_data` - 사용자 게임 진행 데이터

#### 캐릭터 관련 테이블
3. `characters` - 캐릭터 마스터 데이터
4. `user_characters` - 사용자 보유 캐릭터

#### 아이템 관련 테이블
5. `items` - 아이템 마스터 데이터
6. `user_items` - 사용자 보유 아이템

#### 던전 관련 테이블
7. `stages` - 스테이지 마스터 데이터
8. `stage_rewards` - 스테이지 보상 데이터
9. `user_stage_progress` - 사용자 스테이지 진행 상황

#### 출석 및 미션 관련 테이블
10. `attendance_rewards` - 출석 보상 마스터 데이터
11. `user_attendance` - 사용자 출석 기록
12. `missions` - 미션 마스터 데이터
13. `user_missions` - 사용자 미션 진행 상황

#### 우편 및 상점 관련 테이블
14. `user_mails` - 사용자 우편함
15. `shop_items` - 상점 상품 마스터 데이터
16. `user_purchase_history` - 사용자 구매 이력

#### 소셜 관련 테이블
17. `user_friends` - 친구 관계
18. `friend_gifts` - 친구 선물 내역

#### 랭킹 관련 테이블
19. `ranking_snapshots` - 랭킹 스냅샷 (주기적 저장)

### 테이블 상세 정의

#### 1. users (사용자 계정)

```sql
CREATE TABLE `users` (
  `user_id` BIGINT NOT NULL AUTO_INCREMENT COMMENT '사용자 고유 ID',
  `email` VARCHAR(100) NOT NULL COMMENT '이메일 (로그인 ID)',
  `hashed_password` VARCHAR(100) NOT NULL COMMENT '해시된 비밀번호',
  `salt` VARCHAR(100) NOT NULL COMMENT '비밀번호 솔트',
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '계정 생성 시간',
  `last_login_at` DATETIME NULL COMMENT '마지막 로그인 시간',
  `is_deleted` TINYINT NOT NULL DEFAULT 0 COMMENT '삭제 여부 (0: 정상, 1: 삭제)',
  PRIMARY KEY (`user_id`),
  UNIQUE KEY `idx_email` (`email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='사용자 계정 정보';
```

#### 2. user_game_data (사용자 게임 데이터)

```sql
CREATE TABLE `user_game_data` (
  `user_id` BIGINT NOT NULL COMMENT '사용자 ID',
  `nickname` VARCHAR(20) NOT NULL COMMENT '닉네임',
  `level` INT NOT NULL DEFAULT 1 COMMENT '레벨',
  `exp` BIGINT NOT NULL DEFAULT 0 COMMENT '경험치',
  `gold` BIGINT NOT NULL DEFAULT 10000 COMMENT '골드',
  `diamond` INT NOT NULL DEFAULT 100 COMMENT '다이아몬드',
  `stamina` INT NOT NULL DEFAULT 100 COMMENT '스태미너',
  `max_stamina` INT NOT NULL DEFAULT 100 COMMENT '최대 스태미너',
  `last_stamina_update` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '마지막 스태미너 갱신 시간',
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '생성 시간',
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '수정 시간',
  PRIMARY KEY (`user_id`),
  UNIQUE KEY `idx_nickname` (`nickname`),
  CONSTRAINT `fk_user_game_data_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='사용자 게임 진행 데이터';
```

#### 3. characters (캐릭터 마스터)

```sql
CREATE TABLE `characters` (
  `character_id` INT NOT NULL AUTO_INCREMENT COMMENT '캐릭터 ID',
  `name` VARCHAR(50) NOT NULL COMMENT '캐릭터 이름',
  `grade` ENUM('Common', 'Rare', 'Epic', 'Legendary') NOT NULL COMMENT '등급',
  `base_hp` INT NOT NULL COMMENT '기본 HP',
  `base_attack` INT NOT NULL COMMENT '기본 공격력',
  `base_defense` INT NOT NULL COMMENT '기본 방어력',
  `base_speed` INT NOT NULL COMMENT '기본 속도',
  `description` TEXT COMMENT '캐릭터 설명',
  PRIMARY KEY (`character_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='캐릭터 마스터 데이터';
```

#### 4. user_characters (사용자 보유 캐릭터)

```sql
CREATE TABLE `user_characters` (
  `user_character_id` BIGINT NOT NULL AUTO_INCREMENT COMMENT '사용자 캐릭터 고유 ID',
  `user_id` BIGINT NOT NULL COMMENT '사용자 ID',
  `character_id` INT NOT NULL COMMENT '캐릭터 ID',
  `level` INT NOT NULL DEFAULT 1 COMMENT '레벨',
  `exp` INT NOT NULL DEFAULT 0 COMMENT '경험치',
  `enhancement_level` INT NOT NULL DEFAULT 0 COMMENT '강화 레벨 (0~10)',
  `hp` INT NOT NULL COMMENT '현재 HP',
  `attack` INT NOT NULL COMMENT '현재 공격력',
  `defense` INT NOT NULL COMMENT '현재 방어력',
  `speed` INT NOT NULL COMMENT '현재 속도',
  `acquired_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '획득 시간',
  PRIMARY KEY (`user_character_id`),
  KEY `idx_user_id` (`user_id`),
  KEY `idx_character_id` (`character_id`),
  CONSTRAINT `fk_user_characters_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE,
  CONSTRAINT `fk_user_characters_character` FOREIGN KEY (`character_id`) REFERENCES `characters` (`character_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='사용자 보유 캐릭터';
```

#### 5. items (아이템 마스터)

```sql
CREATE TABLE `items` (
  `item_id` INT NOT NULL AUTO_INCREMENT COMMENT '아이템 ID',
  `name` VARCHAR(50) NOT NULL COMMENT '아이템 이름',
  `type` ENUM('Equipment', 'Consumable', 'Material', 'Currency') NOT NULL COMMENT '아이템 타입',
  `sub_type` VARCHAR(20) COMMENT '세부 타입 (무기, 방어구, 포션 등)',
  `grade` ENUM('Common', 'Rare', 'Epic', 'Legendary') NOT NULL COMMENT '등급',
  `max_stack` INT NOT NULL DEFAULT 1 COMMENT '최대 중첩 개수',
  `sell_price` INT NOT NULL DEFAULT 0 COMMENT '판매 가격',
  `description` TEXT COMMENT '아이템 설명',
  PRIMARY KEY (`item_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='아이템 마스터 데이터';
```

#### 6. user_items (사용자 보유 아이템)

```sql
CREATE TABLE `user_items` (
  `user_item_id` BIGINT NOT NULL AUTO_INCREMENT COMMENT '사용자 아이템 고유 ID',
  `user_id` BIGINT NOT NULL COMMENT '사용자 ID',
  `item_id` INT NOT NULL COMMENT '아이템 ID',
  `quantity` INT NOT NULL DEFAULT 1 COMMENT '보유 개수',
  `enhancement_level` INT NOT NULL DEFAULT 0 COMMENT '강화 레벨 (장비만 해당)',
  `acquired_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '획득 시간',
  PRIMARY KEY (`user_item_id`),
  KEY `idx_user_id` (`user_id`),
  KEY `idx_item_id` (`item_id`),
  UNIQUE KEY `idx_user_item` (`user_id`, `item_id`),
  CONSTRAINT `fk_user_items_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE,
  CONSTRAINT `fk_user_items_item` FOREIGN KEY (`item_id`) REFERENCES `items` (`item_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='사용자 보유 아이템';
```

#### 7. stages (스테이지 마스터)

```sql
CREATE TABLE `stages` (
  `stage_id` INT NOT NULL AUTO_INCREMENT COMMENT '스테이지 ID',
  `world_id` INT NOT NULL COMMENT '월드 ID',
  `stage_number` INT NOT NULL COMMENT '스테이지 번호',
  `difficulty` ENUM('Normal', 'Hard', 'Hell') NOT NULL COMMENT '난이도',
  `stamina_cost` INT NOT NULL COMMENT '소모 스태미너',
  `recommended_power` INT NOT NULL COMMENT '권장 전투력',
  `base_exp_reward` INT NOT NULL COMMENT '기본 경험치 보상',
  `base_gold_reward` INT NOT NULL COMMENT '기본 골드 보상',
  PRIMARY KEY (`stage_id`),
  KEY `idx_world` (`world_id`, `stage_number`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='스테이지 마스터 데이터';
```

#### 8. stage_rewards (스테이지 보상)

```sql
CREATE TABLE `stage_rewards` (
  `reward_id` INT NOT NULL AUTO_INCREMENT COMMENT '보상 ID',
  `stage_id` INT NOT NULL COMMENT '스테이지 ID',
  `item_id` INT NULL COMMENT '아이템 ID (NULL이면 캐릭터)',
  `character_id` INT NULL COMMENT '캐릭터 ID (NULL이면 아이템)',
  `quantity` INT NOT NULL DEFAULT 1 COMMENT '보상 개수',
  `drop_rate` DECIMAL(5,2) NOT NULL COMMENT '드롭 확률 (0.00 ~ 100.00)',
  `is_first_clear_bonus` TINYINT NOT NULL DEFAULT 0 COMMENT '최초 클리어 보너스 여부',
  PRIMARY KEY (`reward_id`),
  KEY `idx_stage_id` (`stage_id`),
  CONSTRAINT `fk_stage_rewards_stage` FOREIGN KEY (`stage_id`) REFERENCES `stages` (`stage_id`),
  CONSTRAINT `fk_stage_rewards_item` FOREIGN KEY (`item_id`) REFERENCES `items` (`item_id`),
  CONSTRAINT `fk_stage_rewards_character` FOREIGN KEY (`character_id`) REFERENCES `characters` (`character_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='스테이지 보상 데이터';
```

#### 9. user_stage_progress (사용자 스테이지 진행)

```sql
CREATE TABLE `user_stage_progress` (
  `user_id` BIGINT NOT NULL COMMENT '사용자 ID',
  `stage_id` INT NOT NULL COMMENT '스테이지 ID',
  `is_cleared` TINYINT NOT NULL DEFAULT 0 COMMENT '클리어 여부',
  `clear_count` INT NOT NULL DEFAULT 0 COMMENT '클리어 횟수',
  `best_clear_time` INT NULL COMMENT '최고 클리어 시간 (초)',
  `first_clear_at` DATETIME NULL COMMENT '최초 클리어 시간',
  `last_clear_at` DATETIME NULL COMMENT '마지막 클리어 시간',
  PRIMARY KEY (`user_id`, `stage_id`),
  KEY `idx_stage_id` (`stage_id`),
  CONSTRAINT `fk_user_stage_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE,
  CONSTRAINT `fk_user_stage_stage` FOREIGN KEY (`stage_id`) REFERENCES `stages` (`stage_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='사용자 스테이지 진행 상황';
```

#### 10. attendance_rewards (출석 보상 마스터)

```sql
CREATE TABLE `attendance_rewards` (
  `day` INT NOT NULL COMMENT '출석 일차 (1~7)',
  `reward_type` ENUM('Gold', 'Diamond', 'Item', 'Character') NOT NULL COMMENT '보상 타입',
  `reward_id` INT NULL COMMENT '보상 ID (아이템 또는 캐릭터 ID)',
  `quantity` INT NOT NULL DEFAULT 1 COMMENT '보상 개수',
  PRIMARY KEY (`day`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='출석 보상 마스터 데이터';
```

#### 11. user_attendance (사용자 출석 기록)

```sql
CREATE TABLE `user_attendance` (
  `user_id` BIGINT NOT NULL COMMENT '사용자 ID',
  `attendance_date` DATE NOT NULL COMMENT '출석 날짜',
  `day_count` INT NOT NULL COMMENT '연속 출석 일차',
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '출석 시간',
  PRIMARY KEY (`user_id`, `attendance_date`),
  KEY `idx_user_date` (`user_id`, `attendance_date`),
  CONSTRAINT `fk_user_attendance_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='사용자 출석 기록';
```

#### 12. missions (미션 마스터)

```sql
CREATE TABLE `missions` (
  `mission_id` INT NOT NULL AUTO_INCREMENT COMMENT '미션 ID',
  `mission_type` ENUM('Daily', 'Weekly', 'Achievement') NOT NULL COMMENT '미션 타입',
  `title` VARCHAR(100) NOT NULL COMMENT '미션 제목',
  `description` TEXT COMMENT '미션 설명',
  `condition_type` VARCHAR(50) NOT NULL COMMENT '조건 타입 (DUNGEON_CLEAR, LOGIN 등)',
  `condition_value` INT NOT NULL COMMENT '조건 값 (횟수, 개수 등)',
  `reward_gold` INT NOT NULL DEFAULT 0 COMMENT '보상 골드',
  `reward_diamond` INT NOT NULL DEFAULT 0 COMMENT '보상 다이아몬드',
  `reward_exp` INT NOT NULL DEFAULT 0 COMMENT '보상 경험치',
  PRIMARY KEY (`mission_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='미션 마스터 데이터';
```

#### 13. user_missions (사용자 미션 진행)

```sql
CREATE TABLE `user_missions` (
  `user_id` BIGINT NOT NULL COMMENT '사용자 ID',
  `mission_id` INT NOT NULL COMMENT '미션 ID',
  `progress` INT NOT NULL DEFAULT 0 COMMENT '현재 진행도',
  `is_completed` TINYINT NOT NULL DEFAULT 0 COMMENT '완료 여부',
  `is_rewarded` TINYINT NOT NULL DEFAULT 0 COMMENT '보상 수령 여부',
  `started_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '시작 시간',
  `completed_at` DATETIME NULL COMMENT '완료 시간',
  `rewarded_at` DATETIME NULL COMMENT '보상 수령 시간',
  PRIMARY KEY (`user_id`, `mission_id`),
  KEY `idx_mission_id` (`mission_id`),
  CONSTRAINT `fk_user_missions_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE,
  CONSTRAINT `fk_user_missions_mission` FOREIGN KEY (`mission_id`) REFERENCES `missions` (`mission_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='사용자 미션 진행 상황';
```

#### 14. user_mails (사용자 우편함)

```sql
CREATE TABLE `user_mails` (
  `mail_id` BIGINT NOT NULL AUTO_INCREMENT COMMENT '우편 ID',
  `user_id` BIGINT NOT NULL COMMENT '사용자 ID',
  `mail_type` ENUM('System', 'Reward', 'Friend') NOT NULL COMMENT '우편 타입',
  `title` VARCHAR(100) NOT NULL COMMENT '제목',
  `content` TEXT COMMENT '내용',
  `has_item` TINYINT NOT NULL DEFAULT 0 COMMENT '아이템 포함 여부',
  `item_data` JSON NULL COMMENT '아이템 데이터 (JSON)',
  `is_read` TINYINT NOT NULL DEFAULT 0 COMMENT '읽음 여부',
  `is_received` TINYINT NOT NULL DEFAULT 0 COMMENT '보상 수령 여부',
  `expire_at` DATETIME NOT NULL COMMENT '만료 시간',
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '생성 시간',
  `received_at` DATETIME NULL COMMENT '수령 시간',
  PRIMARY KEY (`mail_id`),
  KEY `idx_user_id` (`user_id`),
  KEY `idx_expire_at` (`expire_at`),
  CONSTRAINT `fk_user_mails_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='사용자 우편함';
```

#### 15. shop_items (상점 상품 마스터)

```sql
CREATE TABLE `shop_items` (
  `shop_item_id` INT NOT NULL AUTO_INCREMENT COMMENT '상점 상품 ID',
  `shop_type` ENUM('Gold', 'Diamond') NOT NULL COMMENT '상점 타입',
  `item_id` INT NOT NULL COMMENT '아이템 ID',
  `quantity` INT NOT NULL DEFAULT 1 COMMENT '판매 개수',
  `price_gold` INT NOT NULL DEFAULT 0 COMMENT '골드 가격',
  `price_diamond` INT NOT NULL DEFAULT 0 COMMENT '다이아몬드 가격',
  `daily_limit` INT NOT NULL DEFAULT 0 COMMENT '일일 구매 제한 (0이면 무제한)',
  `weekly_limit` INT NOT NULL DEFAULT 0 COMMENT '주간 구매 제한 (0이면 무제한)',
  PRIMARY KEY (`shop_item_id`),
  KEY `idx_shop_type` (`shop_type`),
  CONSTRAINT `fk_shop_items_item` FOREIGN KEY (`item_id`) REFERENCES `items` (`item_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='상점 상품 마스터 데이터';
```

#### 16. user_purchase_history (사용자 구매 이력)

```sql
CREATE TABLE `user_purchase_history` (
  `purchase_id` BIGINT NOT NULL AUTO_INCREMENT COMMENT '구매 이력 ID',
  `user_id` BIGINT NOT NULL COMMENT '사용자 ID',
  `shop_item_id` INT NOT NULL COMMENT '상점 상품 ID',
  `quantity` INT NOT NULL COMMENT '구매 개수',
  `paid_gold` INT NOT NULL DEFAULT 0 COMMENT '지불한 골드',
  `paid_diamond` INT NOT NULL DEFAULT 0 COMMENT '지불한 다이아몬드',
  `purchased_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '구매 시간',
  PRIMARY KEY (`purchase_id`),
  KEY `idx_user_id` (`user_id`),
  KEY `idx_shop_item_id` (`shop_item_id`),
  KEY `idx_purchased_at` (`purchased_at`),
  CONSTRAINT `fk_user_purchase_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE,
  CONSTRAINT `fk_user_purchase_shop_item` FOREIGN KEY (`shop_item_id`) REFERENCES `shop_items` (`shop_item_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='사용자 구매 이력';
```

#### 17. user_friends (친구 관계)

```sql
CREATE TABLE `user_friends` (
  `user_id` BIGINT NOT NULL COMMENT '사용자 ID',
  `friend_user_id` BIGINT NOT NULL COMMENT '친구 사용자 ID',
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '친구 추가 시간',
  PRIMARY KEY (`user_id`, `friend_user_id`),
  KEY `idx_friend_user_id` (`friend_user_id`),
  CONSTRAINT `fk_user_friends_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE,
  CONSTRAINT `fk_user_friends_friend` FOREIGN KEY (`friend_user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='친구 관계';
```

#### 18. friend_gifts (친구 선물 내역)

```sql
CREATE TABLE `friend_gifts` (
  `gift_id` BIGINT NOT NULL AUTO_INCREMENT COMMENT '선물 ID',
  `sender_user_id` BIGINT NOT NULL COMMENT '보낸 사용자 ID',
  `receiver_user_id` BIGINT NOT NULL COMMENT '받은 사용자 ID',
  `gift_type` ENUM('Stamina', 'Gold') NOT NULL COMMENT '선물 타입',
  `quantity` INT NOT NULL COMMENT '선물 수량',
  `is_received` TINYINT NOT NULL DEFAULT 0 COMMENT '수령 여부',
  `sent_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '발송 시간',
  `received_at` DATETIME NULL COMMENT '수령 시간',
  PRIMARY KEY (`gift_id`),
  KEY `idx_receiver` (`receiver_user_id`, `is_received`),
  CONSTRAINT `fk_friend_gifts_sender` FOREIGN KEY (`sender_user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE,
  CONSTRAINT `fk_friend_gifts_receiver` FOREIGN KEY (`receiver_user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='친구 선물 내역';
```

#### 19. ranking_snapshots (랭킹 스냅샷)

```sql
CREATE TABLE `ranking_snapshots` (
  `snapshot_id` BIGINT NOT NULL AUTO_INCREMENT COMMENT '스냅샷 ID',
  `user_id` BIGINT NOT NULL COMMENT '사용자 ID',
  `ranking_type` ENUM('Level', 'Power', 'Stage') NOT NULL COMMENT '랭킹 타입',
  `score` BIGINT NOT NULL COMMENT '점수 (레벨, 전투력, 스테이지)',
  `rank` INT NOT NULL COMMENT '순위',
  `snapshot_date` DATE NOT NULL COMMENT '스냅샷 날짜',
  PRIMARY KEY (`snapshot_id`),
  KEY `idx_ranking` (`ranking_type`, `snapshot_date`, `rank`),
  KEY `idx_user_ranking` (`user_id`, `ranking_type`, `snapshot_date`),
  CONSTRAINT `fk_ranking_snapshots_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='랭킹 스냅샷 (주기적 저장용)';
```

### ER 다이어그램 관계 요약

**Users (1) --- (1) UserGameData**
- 한 사용자는 하나의 게임 데이터를 가진다

**Users (1) --- (N) UserCharacters**
- 한 사용자는 여러 캐릭터를 보유할 수 있다

**Characters (1) --- (N) UserCharacters**
- 한 캐릭터는 여러 사용자에게 보유될 수 있다

**Users (1) --- (N) UserItems**
- 한 사용자는 여러 아이템을 보유할 수 있다

**Items (1) --- (N) UserItems**
- 한 아이템은 여러 사용자에게 보유될 수 있다

**Stages (1) --- (N) StageRewards**
- 한 스테이지는 여러 보상을 가질 수 있다

**Users (1) --- (N) UserStageProgress**
- 한 사용자는 여러 스테이지 진행 기록을 가진다

**Users (1) --- (N) UserFriends (N) --- (1) Users**
- 사용자 간 다대다 친구 관계

### 인덱스 전략

1. **기본 키 인덱스**: 모든 테이블의 PRIMARY KEY는 자동 인덱스
2. **외래 키 인덱스**: 조인 성능 향상을 위해 FK 컬럼에 인덱스
3. **유니크 인덱스**: email, nickname 등 중복 방지 컬럼
4. **복합 인덱스**: (user_id, item_id), (user_id, stage_id) 등 자주 함께 조회되는 컬럼

### 파티셔닝 고려사항

대용량 데이터가 예상되는 테이블은 파티셔닝을 고려한다:

- `user_purchase_history`: 날짜 기반 파티셔닝
- `user_mails`: 날짜 기반 파티셔닝
- `ranking_snapshots`: 날짜 기반 파티셔닝

본 교육 과정에서는 파티셔닝을 구현하지 않지만 실제 운영 시 고려한다.

---

## D. SqlKata 쿼리 패턴 모음

### SqlKata 기본 개념

SqlKata는 유창한 API를 제공하는 SQL 쿼리 빌더다. 문자열 연결 없이 타입 안전한 방식으로 쿼리를 작성할 수 있다.

### 기본 설정

```csharp
using SqlKata;
using SqlKata.Execution;

// QueryFactory 생성
var db = new QueryFactory(connection, new SqlKata.Compilers.MySqlCompiler());
```

### SELECT 쿼리 패턴

#### 1. 전체 조회

```csharp
// SELECT * FROM users
var users = await db.Query("users").GetAsync<User>();

// SELECT user_id, email FROM users
var users = await db.Query("users")
    .Select("user_id", "email")
    .GetAsync<User>();
```

#### 2. WHERE 조건

```csharp
// SELECT * FROM users WHERE user_id = @p0
var user = await db.Query("users")
    .Where("user_id", userId)
    .FirstOrDefaultAsync<User>();

// SELECT * FROM users WHERE email = @p0 AND is_deleted = @p1
var user = await db.Query("users")
    .Where("email", email)
    .Where("is_deleted", 0)
    .FirstOrDefaultAsync<User>();

// SELECT * FROM user_game_data WHERE level >= @p0
var users = await db.Query("user_game_data")
    .Where("level", ">=", 10)
    .GetAsync<UserGameData>();
```

#### 3. OR 조건

```csharp
// SELECT * FROM items WHERE type = @p0 OR type = @p1
var items = await db.Query("items")
    .Where(q => q
        .Where("type", "Equipment")
        .OrWhere("type", "Consumable")
    )
    .GetAsync<Item>();
```

#### 4. IN 조건

```csharp
// SELECT * FROM user_characters WHERE character_id IN (@p0, @p1, @p2)
var characterIds = new[] { 1, 2, 3 };
var characters = await db.Query("user_characters")
    .WhereIn("character_id", characterIds)
    .GetAsync<UserCharacter>();
```

#### 5. LIKE 검색

```csharp
// SELECT * FROM user_game_data WHERE nickname LIKE @p0
var users = await db.Query("user_game_data")
    .WhereLike("nickname", $"%{searchText}%")
    .GetAsync<UserGameData>();
```

#### 6. NULL 체크

```csharp
// SELECT * FROM user_mails WHERE received_at IS NULL
var unreceivedMails = await db.Query("user_mails")
    .WhereNull("received_at")
    .GetAsync<UserMail>();

// SELECT * FROM user_stage_progress WHERE first_clear_at IS NOT NULL
var clearedStages = await db.Query("user_stage_progress")
    .WhereNotNull("first_clear_at")
    .GetAsync<UserStageProgress>();
```

#### 7. ORDER BY

```csharp
// SELECT * FROM user_game_data ORDER BY level DESC, exp DESC
var users = await db.Query("user_game_data")
    .OrderByDesc("level")
    .OrderByDesc("exp")
    .GetAsync<UserGameData>();
```

#### 8. LIMIT과 OFFSET

```csharp
// SELECT * FROM user_game_data ORDER BY level DESC LIMIT 10 OFFSET 0
var topUsers = await db.Query("user_game_data")
    .OrderByDesc("level")
    .Limit(10)
    .Offset(0)
    .GetAsync<UserGameData>();

// 페이징
int page = 1;
int pageSize = 20;
var users = await db.Query("user_game_data")
    .OrderByDesc("level")
    .Limit(pageSize)
    .Offset((page - 1) * pageSize)
    .GetAsync<UserGameData>();
```

#### 9. COUNT

```csharp
// SELECT COUNT(*) FROM user_items WHERE user_id = @p0
var itemCount = await db.Query("user_items")
    .Where("user_id", userId)
    .CountAsync<int>();
```

#### 10. DISTINCT

```csharp
// SELECT DISTINCT character_id FROM user_characters WHERE user_id = @p0
var characterIds = await db.Query("user_characters")
    .Where("user_id", userId)
    .Select("character_id")
    .Distinct()
    .GetAsync<int>();
```

### JOIN 쿼리 패턴

#### 1. INNER JOIN

```csharp
// SELECT uc.*, c.name, c.grade 
// FROM user_characters uc
// INNER JOIN characters c ON uc.character_id = c.character_id
// WHERE uc.user_id = @p0
var characters = await db.Query("user_characters as uc")
    .Join("characters as c", "uc.character_id", "c.character_id")
    .Select("uc.*", "c.name", "c.grade")
    .Where("uc.user_id", userId)
    .GetAsync<UserCharacterWithInfo>();
```

#### 2. LEFT JOIN

```csharp
// SELECT u.user_id, u.email, ugd.nickname
// FROM users u
// LEFT JOIN user_game_data ugd ON u.user_id = ugd.user_id
var users = await db.Query("users as u")
    .LeftJoin("user_game_data as ugd", "u.user_id", "ugd.user_id")
    .Select("u.user_id", "u.email", "ugd.nickname")
    .GetAsync<UserWithGameData>();
```

#### 3. 복잡한 JOIN

```csharp
// SELECT ui.*, i.name, i.type, i.grade
// FROM user_items ui
// INNER JOIN items i ON ui.item_id = i.item_id
// WHERE ui.user_id = @p0 AND i.type = @p1
var equipments = await db.Query("user_items as ui")
    .Join("items as i", "ui.item_id", "i.item_id")
    .Select("ui.*", "i.name", "i.type", "i.grade")
    .Where("ui.user_id", userId)
    .Where("i.type", "Equipment")
    .GetAsync<UserItemWithInfo>();
```

### INSERT 쿼리 패턴

#### 1. 단일 INSERT

```csharp
// INSERT INTO users (email, hashed_password, salt, created_at) 
// VALUES (@p0, @p1, @p2, @p3)
var insertedId = await db.Query("users").InsertGetIdAsync<long>(new
{
    email = email,
    hashed_password = hashedPassword,
    salt = salt,
    created_at = DateTime.Now
});
```

#### 2. 객체를 이용한 INSERT

```csharp
var user = new User
{
    Email = email,
    HashedPassword = hashedPassword,
    Salt = salt,
    CreatedAt = DateTime.Now
};

var insertedId = await db.Query("users").InsertGetIdAsync<long>(user);
```

#### 3. 여러 컬럼 INSERT

```csharp
var insertedId = await db.Query("user_game_data").InsertGetIdAsync<long>(new Dictionary<string, object>
{
    { "user_id", userId },
    { "nickname", nickname },
    { "level", 1 },
    { "exp", 0 },
    { "gold", 10000 },
    { "diamond", 100 },
    { "stamina", 100 },
    { "max_stamina", 100 },
    { "created_at", DateTime.Now }
});
```

### UPDATE 쿼리 패턴

#### 1. 단순 UPDATE

```csharp
// UPDATE user_game_data SET gold = @p0, updated_at = @p1 WHERE user_id = @p2
var affected = await db.Query("user_game_data")
    .Where("user_id", userId)
    .UpdateAsync(new
    {
        gold = newGold,
        updated_at = DateTime.Now
    });
```

#### 2. 증감 UPDATE

```csharp
// UPDATE user_game_data SET gold = gold + @p0 WHERE user_id = @p1
var affected = await db.Query("user_game_data")
    .Where("user_id", userId)
    .IncrementAsync("gold", amount);

// UPDATE user_game_data SET stamina = stamina - @p0 WHERE user_id = @p1
var affected = await db.Query("user_game_data")
    .Where("user_id", userId)
    .DecrementAsync("stamina", cost);
```

#### 3. 여러 컬럼 UPDATE

```csharp
var affected = await db.Query("user_game_data")
    .Where("user_id", userId)
    .UpdateAsync(new Dictionary<string, object>
    {
        { "level", newLevel },
        { "exp", newExp },
        { "gold", newGold },
        { "updated_at", DateTime.Now }
    });
```

#### 4. 조건부 UPDATE

```csharp
// UPDATE user_items SET quantity = quantity + @p0 
// WHERE user_id = @p1 AND item_id = @p2 AND quantity >= @p3
var affected = await db.Query("user_items")
    .Where("user_id", userId)
    .Where("item_id", itemId)
    .Where("quantity", ">=", requiredQuantity)
    .IncrementAsync("quantity", addAmount);
```

### DELETE 쿼리 패턴

#### 1. 단순 DELETE

```csharp
// DELETE FROM user_mails WHERE mail_id = @p0
var affected = await db.Query("user_mails")
    .Where("mail_id", mailId)
    .DeleteAsync();
```

#### 2. 조건부 DELETE

```csharp
// DELETE FROM user_mails WHERE user_id = @p0 AND expire_at < @p1
var affected = await db.Query("user_mails")
    .Where("user_id", userId)
    .Where("expire_at", "<", DateTime.Now)
    .DeleteAsync();
```

#### 3. Soft Delete (논리 삭제)

```csharp
// UPDATE users SET is_deleted = 1 WHERE user_id = @p0
var affected = await db.Query("users")
    .Where("user_id", userId)
    .UpdateAsync(new { is_deleted = 1 });
```

### 트랜잭션 패턴

#### 1. 기본 트랜잭션

```csharp
using (var transaction = connection.BeginTransaction())
{
    try
    {
        // 골드 차감
        await db.Query("user_game_data")
            .Where("user_id", userId)
            .DecrementAsync("gold", price);

        // 아이템 추가
        await db.Query("user_items").InsertAsync(new
        {
            user_id = userId,
            item_id = itemId,
            quantity = 1
        });

        transaction.Commit();
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}
```

#### 2. 복잡한 트랜잭션

```csharp
using (var transaction = connection.BeginTransaction())
{
    try
    {
        var db = new QueryFactory(connection, new MySqlCompiler());
        
        // 1. 스태미너 확인 및 차감
        var stamina = await db.Query("user_game_data")
            .Where("user_id", userId)
            .Select("stamina")
            .FirstOrDefaultAsync<int>();
        
        if (stamina < staminaCost)
        {
            throw new Exception("스태미너 부족");
        }
        
        await db.Query("user_game_data")
            .Where("user_id", userId)
            .DecrementAsync("stamina", staminaCost);
        
        // 2. 스테이지 진행 기록 업데이트
        var progress = await db.Query("user_stage_progress")
            .Where("user_id", userId)
            .Where("stage_id", stageId)
            .FirstOrDefaultAsync<UserStageProgress>();
        
        if (progress == null)
        {
            await db.Query("user_stage_progress").InsertAsync(new
            {
                user_id = userId,
                stage_id = stageId,
                is_cleared = 1,
                clear_count = 1,
                first_clear_at = DateTime.Now
            });
        }
        else
        {
            await db.Query("user_stage_progress")
                .Where("user_id", userId)
                .Where("stage_id", stageId)
                .IncrementAsync("clear_count", 1);
        }
        
        // 3. 보상 지급
        await db.Query("user_game_data")
            .Where("user_id", userId)
            .IncrementAsync("gold", goldReward);
        
        transaction.Commit();
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}
```

### 집계 함수 패턴

#### 1. SUM

```csharp
// SELECT SUM(quantity) FROM user_items WHERE user_id = @p0
var totalItems = await db.Query("user_items")
    .Where("user_id", userId)
    .SumAsync<int>("quantity");
```

#### 2. AVG

```csharp
// SELECT AVG(level) FROM user_characters WHERE user_id = @p0
var avgLevel = await db.Query("user_characters")
    .Where("user_id", userId)
    .AverageAsync<double>("level");
```

#### 3. MAX/MIN

```csharp
// SELECT MAX(level) FROM user_characters WHERE user_id = @p0
var maxLevel = await db.Query("user_characters")
    .Where("user_id", userId)
    .MaxAsync<int>("level");

// SELECT MIN(level) FROM user_characters WHERE user_id = @p0
var minLevel = await db.Query("user_characters")
    .Where("user_id", userId)
    .MinAsync<int>("level");
```

### GROUP BY 패턴

```csharp
// SELECT item_id, SUM(quantity) as total_quantity
// FROM user_items
// WHERE user_id = @p0
// GROUP BY item_id
var itemSummary = await db.Query("user_items")
    .Where("user_id", userId)
    .Select("item_id")
    .SelectRaw("SUM(quantity) as total_quantity")
    .GroupBy("item_id")
    .GetAsync<ItemSummary>();
```

### Raw SQL 패턴

복잡한 쿼리는 Raw SQL을 사용할 수 있다.

```csharp
// SELECT * FROM users WHERE YEAR(created_at) = @p0
var users = await db.Query("users")
    .WhereRaw("YEAR(created_at) = ?", year)
    .GetAsync<User>();

// UPDATE user_game_data SET stamina = LEAST(stamina + ?, max_stamina)
var affected = await db.Query("user_game_data")
    .Where("user_id", userId)
    .UpdateAsync(new Dictionary<string, object>
    {
        { "stamina", new UnsafeLiteral($"LEAST(stamina + {recovery}, max_stamina)") }
    });
```

### UPSERT 패턴 (INSERT ... ON DUPLICATE KEY UPDATE)

```csharp
// MySQL의 INSERT ... ON DUPLICATE KEY UPDATE 구현
var query = db.Query("user_items");

var exists = await query
    .Where("user_id", userId)
    .Where("item_id", itemId)
    .ExistsAsync();

if (exists)
{
    // UPDATE
    await query
        .Where("user_id", userId)
        .Where("item_id", itemId)
        .IncrementAsync("quantity", quantity);
}
else
{
    // INSERT
    await query.InsertAsync(new
    {
        user_id = userId,
        item_id = itemId,
        quantity = quantity,
        acquired_at = DateTime.Now
    });
}
```

### 성능 최적화 팁

1. **불필요한 컬럼 조회 피하기**: `Select()`로 필요한 컬럼만 조회한다
2. **인덱스 활용**: WHERE 절의 컬럼이 인덱스에 포함되도록 한다
3. **배치 처리**: 여러 레코드를 한 번에 처리할 때는 트랜잭션을 사용한다
4. **N+1 문제 방지**: JOIN을 활용하여 한 번에 조회한다
5. **캐싱 활용**: 자주 조회되는 데이터는 Redis에 캐싱한다

---

## E. CloudStructures 활용 예제 모음

### CloudStructures 기본 개념

CloudStructures는 StackExchange.Redis의 래퍼 라이브러리로, Redis의 다양한 데이터 타입을 쉽게 사용할 수 있게 한다.

### 기본 설정

```csharp
using CloudStructures;
using CloudStructures.Structures;

// RedisConnection 생성
var config = new RedisConfig("name", "localhost:6379");
var connection = new RedisConnection(config);
```

### String 타입 활용

#### 1. 기본 저장 및 조회

```csharp
// 문자열 저장
var redis = new RedisString<string>(connection, "user:1000:name", TimeSpan.FromHours(1));
await redis.SetAsync("플레이어1");

// 문자열 조회
var value = await redis.GetAsync();
if (value.HasValue)
{
    var name = value.Value;
}
```

#### 2. 객체 저장 (JSON 직렬화)

```csharp
public class UserInfo
{
    public long UserId { get; set; }
    public string Nickname { get; set; }
    public int Level { get; set; }
}

var userInfo = new UserInfo
{
    UserId = 1000,
    Nickname = "플레이어1",
    Level = 10
};

var redis = new RedisString<UserInfo>(connection, $"user:{userInfo.UserId}:info", TimeSpan.FromMinutes(30));
await redis.SetAsync(userInfo);

// 조회
var cached = await redis.GetAsync();
if (cached.HasValue)
{
    var info = cached.Value;
    Console.WriteLine($"{info.Nickname}, Lv.{info.Level}");
}
```

#### 3. 만료 시간 설정

```csharp
// 10분 후 만료
var redis = new RedisString<string>(connection, "temp:data", TimeSpan.FromMinutes(10));
await redis.SetAsync("임시 데이터");

// 만료 시간 없이 저장 (영구 저장)
await redis.SetAsync("영구 데이터", null);

// 특정 시간에 만료
var expireAt = DateTime.Now.AddHours(2);
await redis.SetAsync("데이터", expireAt - DateTime.Now);
```

#### 4. 존재 여부 확인 및 삭제

```csharp
var redis = new RedisString<string>(connection, "user:1000:token", TimeSpan.FromHours(24));

// 존재 여부 확인
var exists = await connection.GetConnection().KeyExistsAsync("user:1000:token");

// 삭제
await redis.DeleteAsync();
```

### Hash 타입 활용

#### 1. 기본 Hash 저장 및 조회

```csharp
var redis = new RedisHash<string>(connection, "user:1000:profile", null);

// 필드 저장
await redis.SetAsync("nickname", "플레이어1");
await redis.SetAsync("level", "10");
await redis.SetAsync("exp", "5000");

// 필드 조회
var nickname = await redis.GetAsync("nickname");
if (nickname.HasValue)
{
    Console.WriteLine(nickname.Value);
}

// 여러 필드 한번에 조회
var fields = await redis.GetAllAsync();
foreach (var field in fields)
{
    Console.WriteLine($"{field.Key}: {field.Value}");
}
```

#### 2. 게임 데이터 캐싱

```csharp
public async Task CacheUserGameData(long userId, UserGameData data)
{
    var redis = new RedisHash<string>(connection, $"user:{userId}:game", TimeSpan.FromMinutes(30));
    
    var fields = new Dictionary<string, string>
    {
        { "nickname", data.Nickname },
        { "level", data.Level.ToString() },
        { "exp", data.Exp.ToString() },
        { "gold", data.Gold.ToString() },
        { "diamond", data.Diamond.ToString() },
        { "stamina", data.Stamina.ToString() }
    };
    
    await redis.SetAsync(fields);
}

public async Task<UserGameData> GetCachedUserGameData(long userId)
{
    var redis = new RedisHash<string>(connection, $"user:{userId}:game", null);
    var fields = await redis.GetAllAsync();
    
    if (fields.Count == 0)
    {
        return null;
    }
    
    return new UserGameData
    {
        Nickname = fields["nickname"],
        Level = int.Parse(fields["level"]),
        Exp = long.Parse(fields["exp"]),
        Gold = long.Parse(fields["gold"]),
        Diamond = int.Parse(fields["diamond"]),
        Stamina = int.Parse(fields["stamina"])
    };
}
```

#### 3. 필드 증감

```csharp
var redis = new RedisHash<string>(connection, $"user:{userId}:game", null);

// 골드 증가
await redis.IncrementAsync("gold", 1000);

// 스태미너 감소
await redis.IncrementAsync("stamina", -5);
```

### List 타입 활용

#### 1. 최근 로그인 기록

```csharp
public async Task AddLoginHistory(long userId, DateTime loginTime)
{
    var redis = new RedisList<string>(connection, $"user:{userId}:login_history", null);
    
    // 왼쪽에 추가 (최신이 앞에)
    await redis.LeftPushAsync(loginTime.ToString("yyyy-MM-dd HH:mm:ss"));
    
    // 최대 10개만 유지
    await redis.TrimAsync(0, 9);
}

public async Task<List<string>> GetLoginHistory(long userId)
{
    var redis = new RedisList<string>(connection, $"user:{userId}:login_history", null);
    
    // 전체 조회 (0부터 -1까지)
    var history = await redis.RangeAsync(0, -1);
    return history.ToList();
}
```

#### 2. 채팅 메시지 큐

```csharp
public class ChatMessage
{
    public long SenderId { get; set; }
    public string SenderName { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
}

public async Task SendChatMessage(long channelId, ChatMessage message)
{
    var redis = new RedisList<ChatMessage>(connection, $"chat:{channelId}:messages", null);
    
    await redis.RightPushAsync(message);
    
    // 최근 100개만 유지
    await redis.TrimAsync(-100, -1);
}

public async Task<List<ChatMessage>> GetChatMessages(long channelId, int count = 50)
{
    var redis = new RedisList<ChatMessage>(connection, $"chat:{channelId}:messages", null);
    
    // 최근 메시지부터 조회
    var messages = await redis.RangeAsync(-count, -1);
    return messages.Reverse().ToList();
}
```

### Set 타입 활용

#### 1. 친구 목록 관리

```csharp
public async Task AddFriend(long userId, long friendUserId)
{
    var redis = new RedisSet<long>(connection, $"user:{userId}:friends", null);
    await redis.AddAsync(friendUserId);
}

public async Task RemoveFriend(long userId, long friendUserId)
{
    var redis = new RedisSet<long>(connection, $"user:{userId}:friends", null);
    await redis.RemoveAsync(friendUserId);
}

public async Task<bool> IsFriend(long userId, long friendUserId)
{
    var redis = new RedisSet<long>(connection, $"user:{userId}:friends", null);
    return await redis.ContainsAsync(friendUserId);
}

public async Task<List<long>> GetFriendList(long userId)
{
    var redis = new RedisSet<long>(connection, $"user:{userId}:friends", null);
    var friends = await redis.MembersAsync();
    return friends.ToList();
}

public async Task<int> GetFriendCount(long userId)
{
    var redis = new RedisSet<long>(connection, $"user:{userId}:friends", null);
    return (int)await redis.LengthAsync();
}
```

#### 2. 공통 친구 찾기

```csharp
public async Task<List<long>> GetCommonFriends(long userId1, long userId2)
{
    var db = connection.GetConnection().GetDatabase();
    
    var key1 = $"user:{userId1}:friends";
    var key2 = $"user:{userId2}:friends";
    
    // 교집합 구하기
    var commonFriends = await db.SetCombineAsync(
        SetOperation.Intersect,
        new RedisKey[] { key1, key2 }
    );
    
    return commonFriends.Select(x => long.Parse(x)).ToList();
}
```

#### 3. 온라인 사용자 관리

```csharp
public async Task SetUserOnline(long userId)
{
    var redis = new RedisSet<long>(connection, "users:online", null);
    await redis.AddAsync(userId);
}

public async Task SetUserOffline(long userId)
{
    var redis = new RedisSet<long>(connection, "users:online", null);
    await redis.RemoveAsync(userId);
}

public async Task<bool> IsUserOnline(long userId)
{
    var redis = new RedisSet<long>(connection, "users:online", null);
    return await redis.ContainsAsync(userId);
}

public async Task<int> GetOnlineUserCount()
{
    var redis = new RedisSet<long>(connection, "users:online", null);
    return (int)await redis.LengthAsync();
}
```

### Sorted Set 타입 활용

#### 1. 레벨 랭킹

```csharp
public async Task UpdateLevelRanking(long userId, int level)
{
    var redis = new RedisSortedSet<long>(connection, "ranking:level", null);
    await redis.AddAsync(userId, level);
}

public async Task<List<RankEntry>> GetTopLevelRanking(int count = 100)
{
    var redis = new RedisSortedSet<long>(connection, "ranking:level", null);
    
    // 상위 랭킹 조회 (내림차순)
    var ranking = await redis.RangeByRankAsync(0, count - 1, Order.Descending);
    
    var result = new List<RankEntry>();
    int rank = 1;
    foreach (var entry in ranking)
    {
        result.Add(new RankEntry
        {
            Rank = rank++,
            UserId = entry.Value,
            Score = (int)entry.Score
        });
    }
    
    return result;
}

public async Task<RankInfo> GetUserRank(long userId)
{
    var redis = new RedisSortedSet<long>(connection, "ranking:level", null);
    
    // 사용자 순위 조회 (0부터 시작하므로 +1)
    var rank = await redis.RankAsync(userId, Order.Descending);
    var score = await redis.ScoreAsync(userId);
    
    if (!rank.HasValue || !score.HasValue)
    {
        return null;
    }
    
    return new RankInfo
    {
        Rank = (int)rank.Value + 1,
        Score = (int)score.Value
    };
}
```

#### 2. 전투력 랭킹

```csharp
public async Task UpdatePowerRanking(long userId, long power)
{
    var redis = new RedisSortedSet<long>(connection, "ranking:power", null);
    await redis.AddAsync(userId, power);
}

public async Task<List<RankEntry>> GetUserRankRange(long userId, int range = 5)
{
    var redis = new RedisSortedSet<long>(connection, "ranking:power", null);
    
    var rank = await redis.RankAsync(userId, Order.Descending);
    if (!rank.HasValue)
    {
        return new List<RankEntry>();
    }
    
    // 내 순위 기준 위아래 range명 조회
    var start = Math.Max(0, (int)rank.Value - range);
    var end = (int)rank.Value + range;
    
    var ranking = await redis.RangeByRankAsync(start, end, Order.Descending);
    
    var result = new List<RankEntry>();
    int currentRank = start + 1;
    foreach (var entry in ranking)
    {
        result.Add(new RankEntry
        {
            Rank = currentRank++,
            UserId = entry.Value,
            Score = (long)entry.Score
        });
    }
    
    return result;
}
```

#### 3. 시간 기반 이벤트 스케줄링

```csharp
public async Task ScheduleEvent(string eventId, DateTime executeAt)
{
    var redis = new RedisSortedSet<string>(connection, "scheduled:events", null);
    var timestamp = new DateTimeOffset(executeAt).ToUnixTimeSeconds();
    await redis.AddAsync(eventId, timestamp);
}

public async Task<List<string>> GetExpiredEvents()
{
    var redis = new RedisSortedSet<string>(connection, "scheduled:events", null);
    var now = DateTimeOffset.Now.ToUnixTimeSeconds();
    
    // 현재 시간 이전의 이벤트 조회
    var events = await redis.RangeByScoreAsync(double.NegativeInfinity, now);
    
    return events.Select(x => x.Value).ToList();
}

public async Task RemoveEvent(string eventId)
{
    var redis = new RedisSortedSet<string>(connection, "scheduled:events", null);
    await redis.RemoveAsync(eventId);
}
```

### 실전 활용 패턴

#### 1. 토큰 관리

```csharp
public class TokenManager
{
    private readonly RedisConnection _connection;
    
    public TokenManager(RedisConnection connection)
    {
        _connection = connection;
    }
    
    public async Task SaveToken(long userId, string token)
    {
        var redis = new RedisString<string>(_connection, $"token:{token}", TimeSpan.FromHours(24));
        await redis.SetAsync(userId.ToString());
    }
    
    public async Task<long?> GetUserIdByToken(string token)
    {
        var redis = new RedisString<string>(_connection, $"token:{token}", null);
        var result = await redis.GetAsync();
        
        if (result.HasValue && long.TryParse(result.Value, out var userId))
        {
            return userId;
        }
        
        return null;
    }
    
    public async Task DeleteToken(string token)
    {
        var redis = new RedisString<string>(_connection, $"token:{token}", null);
        await redis.DeleteAsync();
    }
}
```

#### 2. 세션 데이터 관리

```csharp
public class SessionData
{
    public long UserId { get; set; }
    public string Nickname { get; set; }
    public DateTime LoginAt { get; set; }
    public string DeviceId { get; set; }
}

public class SessionManager
{
    private readonly RedisConnection _connection;
    
    public SessionManager(RedisConnection connection)
    {
        _connection = connection;
    }
    
    public async Task SaveSession(string sessionId, SessionData data)
    {
        var redis = new RedisString<SessionData>(_connection, $"session:{sessionId}", TimeSpan.FromHours(2));
        await redis.SetAsync(data);
    }
    
    public async Task<SessionData> GetSession(string sessionId)
    {
        var redis = new RedisString<SessionData>(_connection, $"session:{sessionId}", null);
        var result = await redis.GetAsync();
        return result.HasValue ? result.Value : null;
    }
    
    public async Task RefreshSession(string sessionId)
    {
        var redis = new RedisString<SessionData>(_connection, $"session:{sessionId}", null);
        var data = await GetSession(sessionId);
        if (data != null)
        {
            await SaveSession(sessionId, data);
        }
    }
}
```

#### 3. 속도 제한 (Rate Limiting)

```csharp
public class RateLimiter
{
    private readonly RedisConnection _connection;
    
    public RateLimiter(RedisConnection connection)
    {
        _connection = connection;
    }
    
    public async Task<bool> IsAllowed(long userId, string action, int maxAttempts, TimeSpan window)
    {
        var key = $"rate_limit:{userId}:{action}";
        var redis = new RedisString<int>(_connection, key, window);
        
        var current = await redis.GetAsync();
        var count = current.HasValue ? current.Value : 0;
        
        if (count >= maxAttempts)
        {
            return false;
        }
        
        await redis.IncrementAsync(1);
        return true;
    }
}

// 사용 예시
var rateLimiter = new RateLimiter(connection);
var allowed = await rateLimiter.IsAllowed(userId, "login", 5, TimeSpan.FromMinutes(1));
if (!allowed)
{
    // 너무 많은 시도
    return Error("잠시 후 다시 시도해주세요");
}
```

#### 4. 분산 락 (Distributed Lock)

```csharp
public class DistributedLock
{
    private readonly RedisConnection _connection;
    
    public DistributedLock(RedisConnection connection)
    {
        _connection = connection;
    }
    
    public async Task<bool> AcquireLock(string lockKey, string lockValue, TimeSpan expiry)
    {
        var db = _connection.GetConnection().GetDatabase();
        return await db.StringSetAsync(lockKey, lockValue, expiry, When.NotExists);
    }
    
    public async Task ReleaseLock(string lockKey, string lockValue)
    {
        var db = _connection.GetConnection().GetDatabase();
        
        // Lua 스크립트로 원자적 삭제
        var script = @"
            if redis.call('get', KEYS[1]) == ARGV[1] then
                return redis.call('del', KEYS[1])
            else
                return 0
            end
        ";
        
        await db.ScriptEvaluateAsync(script, new RedisKey[] { lockKey }, new RedisValue[] { lockValue });
    }
}

// 사용 예시
var lockKey = $"lock:user:{userId}:inventory";
var lockValue = Guid.NewGuid().ToString();
var distributedLock = new DistributedLock(connection);

if (await distributedLock.AcquireLock(lockKey, lockValue, TimeSpan.FromSeconds(10)))
{
    try
    {
        // 임계 영역 작업
        await ProcessInventory(userId);
    }
    finally
    {
        await distributedLock.ReleaseLock(lockKey, lockValue);
    }
}
else
{
    // 락 획득 실패
    return Error("다른 작업이 진행 중입니다");
}
```

### 성능 최적화 팁

1. **파이프라인 사용**: 여러 명령을 한 번에 전송한다
2. **적절한 만료 시간**: 메모리 낭비를 방지한다
3. **키 네이밍 규칙**: 일관된 형식을 사용한다 (예: `user:{userId}:profile`)
4. **데이터 크기 제한**: 너무 큰 데이터는 Redis에 저장하지 않는다
5. **비동기 처리**: 모든 Redis 작업은 비동기로 처리한다

---

## F. 커스텀 토큰 생성/검증 알고리즘 상세 가이드

### 토큰 기반 인증 개요

본 교육에서는 JWT 대신 커스텀 토큰 방식을 사용한다. 이는 게임 서버의 특성에 맞춘 간단하고 효율적인 인증 방식이다.

### 토큰 구조 설계

#### 토큰 구성 요소
```
{UserId}|{IssuedAt}|{Signature}
```

- **UserId**: 사용자 고유 ID (long 타입)
- **IssuedAt**: 토큰 발급 시간 (Unix Timestamp, long 타입)
- **Signature**: 서명 (HMAC-SHA256 해시)

#### 예시
```
1000|1732867200|a3f5c8d9e2b1f4a6c7d8e9f0a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0
```

### 서명 알고리즘

#### HMAC-SHA256 해시 생성

```csharp
using System.Security.Cryptography;
using System.Text;

public class TokenGenerator
{
    private readonly string _secretKey;
    
    public TokenGenerator(string secretKey)
    {
        _secretKey = secretKey;
    }
    
    private string GenerateSignature(string data)
    {
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey)))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
    
    public string GenerateToken(long userId)
    {
        var issuedAt = DateTimeOffset.Now.ToUnixTimeSeconds();
        var data = $"{userId}|{issuedAt}";
        var signature = GenerateSignature(data);
        
        return $"{data}|{signature}";
    }
}
```

### 토큰 검증 알고리즘

#### 1. 토큰 파싱

```csharp
public class TokenInfo
{
    public long UserId { get; set; }
    public long IssuedAt { get; set; }
    public string Signature { get; set; }
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; }
}

public class TokenValidator
{
    private readonly string _secretKey;
    private readonly int _tokenExpiryHours;
    
    public TokenValidator(string secretKey, int tokenExpiryHours = 24)
    {
        _secretKey = secretKey;
        _tokenExpiryHours = tokenExpiryHours;
    }
    
    public TokenInfo ParseToken(string token)
    {
        var tokenInfo = new TokenInfo { IsValid = false };
        
        if (string.IsNullOrWhiteSpace(token))
        {
            tokenInfo.ErrorMessage = "토큰이 비어있습니다";
            return tokenInfo;
        }
        
        var parts = token.Split('|');
        if (parts.Length != 3)
        {
            tokenInfo.ErrorMessage = "토큰 형식이 올바르지 않습니다";
            return tokenInfo;
        }
        
        if (!long.TryParse(parts[0], out var userId))
        {
            tokenInfo.ErrorMessage = "UserId 파싱 실패";
            return tokenInfo;
        }
        
        if (!long.TryParse(parts[1], out var issuedAt))
        {
            tokenInfo.ErrorMessage = "IssuedAt 파싱 실패";
            return tokenInfo;
        }
        
        tokenInfo.UserId = userId;
        tokenInfo.IssuedAt = issuedAt;
        tokenInfo.Signature = parts[2];
        tokenInfo.IsValid = true;
        
        return tokenInfo;
    }
}
```

#### 2. 서명 검증

```csharp
private string GenerateSignature(string data)
{
    using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey)))
    {
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
}

public bool VerifySignature(TokenInfo tokenInfo)
{
    var data = $"{tokenInfo.UserId}|{tokenInfo.IssuedAt}";
    var expectedSignature = GenerateSignature(data);
    
    return expectedSignature.Equals(tokenInfo.Signature, StringComparison.OrdinalIgnoreCase);
}
```

#### 3. 만료 시간 검증

```csharp
public bool IsTokenExpired(TokenInfo tokenInfo)
{
    var issuedAtDateTime = DateTimeOffset.FromUnixTimeSeconds(tokenInfo.IssuedAt).DateTime;
    var expiryTime = issuedAtDateTime.AddHours(_tokenExpiryHours);
    
    return DateTime.Now > expiryTime;
}
```

#### 4. 전체 검증 프로세스

```csharp
public (bool isValid, long userId, string errorMessage) ValidateToken(string token)
{
    // 1. 토큰 파싱
    var tokenInfo = ParseToken(token);
    if (!tokenInfo.IsValid)
    {
        return (false, 0, tokenInfo.ErrorMessage);
    }
    
    // 2. 서명 검증
    if (!VerifySignature(tokenInfo))
    {
        return (false, 0, "토큰 서명이 유효하지 않습니다");
    }
    
    // 3. 만료 시간 검증
    if (IsTokenExpired(tokenInfo))
    {
        return (false, 0, "토큰이 만료되었습니다");
    }
    
    return (true, tokenInfo.UserId, null);
}
```

### Redis를 활용한 토큰 관리

#### 1. 토큰 저장

```csharp
public class TokenManager
{
    private readonly RedisConnection _redisConnection;
    private readonly TokenGenerator _tokenGenerator;
    private readonly int _tokenExpiryHours;
    
    public TokenManager(RedisConnection redisConnection, string secretKey, int tokenExpiryHours = 24)
    {
        _redisConnection = redisConnection;
        _tokenGenerator = new TokenGenerator(secretKey);
        _tokenExpiryHours = tokenExpiryHours;
    }
    
    public async Task<string> CreateAndSaveToken(long userId)
    {
        var token = _tokenGenerator.GenerateToken(userId);
        
        // Redis에 토큰 저장 (UserId를 값으로)
        var redis = new RedisString<long>(_redisConnection, $"token:{token}", TimeSpan.FromHours(_tokenExpiryHours));
        await redis.SetAsync(userId);
        
        return token;
    }
}
```

#### 2. 토큰 검증 (Redis 조회 포함)

```csharp
public async Task<(bool isValid, long userId, string errorMessage)> ValidateTokenWithRedis(string token)
{
    // Redis에서 토큰 확인
    var redis = new RedisString<long>(_redisConnection, $"token:{token}", null);
    var cachedUserId = await redis.GetAsync();
    
    if (!cachedUserId.HasValue)
    {
        return (false, 0, "토큰이 존재하지 않거나 만료되었습니다");
    }
    
    // 토큰 검증
    var validator = new TokenValidator(_tokenGenerator.SecretKey, _tokenExpiryHours);
    var validationResult = validator.ValidateToken(token);
    
    if (!validationResult.isValid)
    {
        // 검증 실패 시 Redis에서 토큰 삭제
        await redis.DeleteAsync();
        return validationResult;
    }
    
    // Redis의 UserId와 토큰의 UserId 일치 확인
    if (validationResult.userId != cachedUserId.Value)
    {
        await redis.DeleteAsync();
        return (false, 0, "토큰 정보가 일치하지 않습니다");
    }
    
    return validationResult;
}
```

#### 3. 토큰 무효화 (로그아웃)

```csharp
public async Task InvalidateToken(string token)
{
    var redis = new RedisString<long>(_redisConnection, $"token:{token}", null);
    await redis.DeleteAsync();
}

public async Task InvalidateAllUserTokens(long userId)
{
    // 사용자의 모든 토큰을 추적하는 경우
    var db = _redisConnection.GetConnection().GetDatabase();
    var server = _redisConnection.GetConnection().GetServer(_redisConnection.GetConnection().GetEndPoints().First());
    
    var pattern = $"token:*";
    var keys = server.Keys(pattern: pattern);
    
    foreach (var key in keys)
    {
        var cachedUserId = await db.StringGetAsync(key);
        if (cachedUserId.HasValue && long.Parse(cachedUserId) == userId)
        {
            await db.KeyDeleteAsync(key);
        }
    }
}
```

### 보안 강화 방안

#### 1. 사용자별 토큰 개수 제한

```csharp
public async Task<string> CreateAndSaveTokenWithLimit(long userId, int maxTokensPerUser = 5)
{
    var token = _tokenGenerator.GenerateToken(userId);
    
    // 사용자의 토큰 목록 관리
    var userTokensKey = $"user:{userId}:tokens";
    var userTokens = new RedisList<string>(_redisConnection, userTokensKey, null);
    
    // 기존 토큰 개수 확인
    var tokenCount = await userTokens.LengthAsync();
    
    if (tokenCount >= maxTokensPerUser)
    {
        // 가장 오래된 토큰 삭제
        var oldestToken = await userTokens.LeftPopAsync();
        if (oldestToken.HasValue)
        {
            await InvalidateToken(oldestToken.Value);
        }
    }
    
    // 새 토큰 저장
    var redis = new RedisString<long>(_redisConnection, $"token:{token}", TimeSpan.FromHours(_tokenExpiryHours));
    await redis.SetAsync(userId);
    
    // 사용자 토큰 목록에 추가
    await userTokens.RightPushAsync(token);
    
    return token;
}
```

#### 2. 디바이스 정보 포함

```csharp
public class EnhancedTokenInfo
{
    public long UserId { get; set; }
    public long IssuedAt { get; set; }
    public string DeviceId { get; set; }
    public string Signature { get; set; }
}

public string GenerateEnhancedToken(long userId, string deviceId)
{
    var issuedAt = DateTimeOffset.Now.ToUnixTimeSeconds();
    var data = $"{userId}|{issuedAt}|{deviceId}";
    var signature = GenerateSignature(data);
    
    // Base64 인코딩하여 특수문자 방지
    var encodedDeviceId = Convert.ToBase64String(Encoding.UTF8.GetBytes(deviceId));
    
    return $"{userId}|{issuedAt}|{encodedDeviceId}|{signature}";
}
```

#### 3. IP 주소 검증

```csharp
public class TokenMetadata
{
    public long UserId { get; set; }
    public string IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

public async Task SaveTokenWithMetadata(string token, long userId, string ipAddress)
{
    var metadata = new TokenMetadata
    {
        UserId = userId,
        IpAddress = ipAddress,
        CreatedAt = DateTime.Now
    };
    
    var redis = new RedisString<TokenMetadata>(_redisConnection, $"token_meta:{token}", TimeSpan.FromHours(_tokenExpiryHours));
    await redis.SetAsync(metadata);
}

public async Task<bool> ValidateTokenIpAddress(string token, string currentIp)
{
    var redis = new RedisString<TokenMetadata>(_redisConnection, $"token_meta:{token}", null);
    var metadata = await redis.GetAsync();
    
    if (!metadata.HasValue)
    {
        return false;
    }
    
    // IP 주소 일치 여부 확인 (선택적으로 서브넷 범위 확인도 가능)
    return metadata.Value.IpAddress == currentIp;
}
```

### 토큰 갱신 (Refresh Token)

#### 1. 액세스 토큰과 리프레시 토큰 분리

```csharp
public class TokenPair
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}

public async Task<TokenPair> CreateTokenPair(long userId)
{
    var accessToken = _tokenGenerator.GenerateToken(userId);
    var refreshToken = _tokenGenerator.GenerateToken(userId);
    
    // 액세스 토큰: 1시간
    var accessRedis = new RedisString<long>(_redisConnection, $"token:access:{accessToken}", TimeSpan.FromHours(1));
    await accessRedis.SetAsync(userId);
    
    // 리프레시 토큰: 30일
    var refreshRedis = new RedisString<long>(_redisConnection, $"token:refresh:{refreshToken}", TimeSpan.FromDays(30));
    await refreshRedis.SetAsync(userId);
    
    return new TokenPair
    {
        AccessToken = accessToken,
        RefreshToken = refreshToken
    };
}

public async Task<string> RefreshAccessToken(string refreshToken)
{
    var redis = new RedisString<long>(_redisConnection, $"token:refresh:{refreshToken}", null);
    var userId = await redis.GetAsync();
    
    if (!userId.HasValue)
    {
        throw new Exception("리프레시 토큰이 유효하지 않습니다");
    }
    
    // 새 액세스 토큰 발급
    var newAccessToken = _tokenGenerator.GenerateToken(userId.Value);
    var accessRedis = new RedisString<long>(_redisConnection, $"token:access:{newAccessToken}", TimeSpan.FromHours(1));
    await accessRedis.SetAsync(userId.Value);
    
    return newAccessToken;
}
```

### 비밀 키 관리

#### 1. 환경별 비밀 키 설정

```json
// appsettings.Development.json
{
  "TokenSettings": {
    "SecretKey": "development-secret-key-change-in-production-min-32-chars",
    "ExpiryHours": 24
  }
}

// appsettings.Production.json
{
  "TokenSettings": {
    "SecretKey": "production-secret-key-must-be-very-strong-and-random",
    "ExpiryHours": 24
  }
}
```

#### 2. User Secrets 활용 (개발 환경)

```bash
# User Secrets 초기화
dotnet user-secrets init

# 비밀 키 설정
dotnet user-secrets set "TokenSettings:SecretKey" "your-very-secret-key-here"
```

```csharp
// Program.cs
builder.Configuration.AddUserSecrets<Program>();

// TokenSettings 클래스
public class TokenSettings
{
    public string SecretKey { get; set; }
    public int ExpiryHours { get; set; }
}

// 서비스 등록
builder.Services.Configure<TokenSettings>(builder.Configuration.GetSection("TokenSettings"));
```

### 전체 통합 예제

```csharp
public class AuthService
{
    private readonly TokenManager _tokenManager;
    private readonly IDbConnection _dbConnection;
    private readonly IQueryFactory _queryFactory;
    
    public AuthService(TokenManager tokenManager, IDbConnection dbConnection)
    {
        _tokenManager = tokenManager;
        _dbConnection = dbConnection;
        _queryFactory = new QueryFactory(dbConnection, new MySqlCompiler());
    }
    
    public async Task<(bool success, string token, string message)> Login(string email, string password, string ipAddress)
    {
        // 1. 사용자 조회
        var user = await _queryFactory.Query("users")
            .Where("email", email)
            .Where("is_deleted", 0)
            .FirstOrDefaultAsync<User>();
        
        if (user == null)
        {
            return (false, null, "이메일 또는 비밀번호가 일치하지 않습니다");
        }
        
        // 2. 비밀번호 검증
        var hashedPassword = HashPassword(password, user.Salt);
        if (hashedPassword != user.HashedPassword)
        {
            return (false, null, "이메일 또는 비밀번호가 일치하지 않습니다");
        }
        
        // 3. 토큰 생성
        var token = await _tokenManager.CreateAndSaveTokenWithLimit(user.UserId);
        
        // 4. 메타데이터 저장
        await _tokenManager.SaveTokenWithMetadata(token, user.UserId, ipAddress);
        
        // 5. 마지막 로그인 시간 업데이트
        await _queryFactory.Query("users")
            .Where("user_id", user.UserId)
            .UpdateAsync(new { last_login_at = DateTime.Now });
        
        return (true, token, "로그인 성공");
    }
    
    public async Task<bool> Logout(string token)
    {
        await _tokenManager.InvalidateToken(token);
        return true;
    }
    
    private string HashPassword(string password, string salt)
    {
        using (var sha256 = SHA256.Create())
        {
            var combined = password + salt;
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
```

### 성능 및 보안 고려사항

1. **비밀 키 강도**: 최소 32자 이상의 무작위 문자열을 사용한다
2. **토큰 길이**: 너무 긴 토큰은 네트워크 오버헤드를 발생시킨다
3. **Redis 만료 시간**: 토큰 만료 시간과 Redis TTL을 일치시킨다
4. **토큰 재사용 방지**: 로그아웃 시 반드시 토큰을 삭제한다
5. **HTTPS 사용**: 토큰은 반드시 암호화된 연결로 전송한다

---

## G. 보안 체크리스트

### 1. 인증 및 권한 관리

#### 비밀번호 보안
- [ ] 비밀번호를 평문으로 저장하지 않는다
- [ ] SHA-256 이상의 해시 알고리즘을 사용한다
- [ ] 사용자별 고유한 Salt를 사용한다
- [ ] 비밀번호 최소 길이를 6자 이상으로 제한한다
- [ ] 비밀번호 변경 시 이전 비밀번호와 다른지 확인한다

#### 토큰 보안
- [ ] 토큰 비밀 키를 코드에 하드코딩하지 않는다
- [ ] 토큰 비밀 키는 최소 32자 이상의 강력한 문자열을 사용한다
- [ ] 토큰에 적절한 만료 시간을 설정한다 (24시간 권장)
- [ ] 로그아웃 시 토큰을 무효화한다
- [ ] 토큰을 URL 파라미터로 전송하지 않는다 (Header 사용)

#### 세션 관리
- [ ] 사용자당 동시 접속 토큰 개수를 제한한다
- [ ] 비정상적인 토큰 사용 패턴을 감지한다
- [ ] 장기간 미사용 토큰을 자동으로 만료시킨다
- [ ] 디바이스 정보를 토큰과 함께 검증한다

### 2. 입력 데이터 검증

#### 파라미터 검증
- [ ] 모든 API 입력값에 대해 타입 검증을 수행한다
- [ ] 문자열 길이 제한을 설정한다
- [ ] 숫자 범위 검증을 수행한다 (음수, 최대값)
- [ ] 이메일 형식을 정규식으로 검증한다
- [ ] NULL 또는 빈 문자열을 허용하지 않는다

#### SQL Injection 방지
- [ ] SqlKata의 파라미터 바인딩을 사용한다
- [ ] 사용자 입력을 직접 쿼리에 연결하지 않는다
- [ ] WHERE 조건에 항상 파라미터를 사용한다
- [ ] Raw SQL 사용을 최소화한다

```csharp
// 안전하지 않음
var query = $"SELECT * FROM users WHERE email = '{email}'";

// 안전함
var user = await db.Query("users")
    .Where("email", email)
    .FirstOrDefaultAsync<User>();
```

#### XSS (Cross-Site Scripting) 방지
- [ ] 사용자 입력 문자열을 HTML 인코딩한다
- [ ] 닉네임, 메시지 등에 스크립트 태그를 허용하지 않는다
- [ ] 특수문자 필터링을 적용한다

### 3. 데이터베이스 보안

#### 접근 제어
- [ ] 데이터베이스 계정에 최소 권한만 부여한다
- [ ] 애플리케이션 계정과 관리자 계정을 분리한다
- [ ] 데이터베이스 비밀번호를 코드에 하드코딩하지 않는다
- [ ] 연결 문자열을 환경 변수 또는 설정 파일에서 관리한다

#### 데이터 암호화
- [ ] 민감한 개인정보는 암호화하여 저장한다
- [ ] 데이터베이스 백업 파일을 암호화한다
- [ ] 전송 중인 데이터는 SSL/TLS를 사용한다

#### 트랜잭션 관리
- [ ] 중요한 작업은 트랜잭션으로 묶는다
- [ ] 트랜잭션 타임아웃을 설정한다
- [ ] 데드락 발생 시 재시도 로직을 구현한다

### 4. Redis 보안

#### 접근 제어
- [ ] Redis에 비밀번호를 설정한다
- [ ] Redis를 외부에 노출하지 않는다 (localhost만 허용)
- [ ] Redis Sentinel 또는 Cluster를 사용하여 고가용성을 확보한다

#### 데이터 관리
- [ ] 민감한 데이터를 Redis에 저장하지 않는다
- [ ] 모든 캐시 데이터에 만료 시간을 설정한다
- [ ] Redis 메모리 사용량을 모니터링한다
- [ ] 주기적으로 불필요한 키를 정리한다

### 5. API 보안

#### Rate Limiting (속도 제한)
- [ ] 로그인 API에 속도 제한을 적용한다 (분당 5회)
- [ ] 회원가입 API에 속도 제한을 적용한다
- [ ] 비용이 큰 API에 속도 제한을 적용한다
- [ ] IP 기반 차단 기능을 구현한다

```csharp
public async Task<bool> CheckRateLimit(string userId, string apiName, int maxRequests, TimeSpan window)
{
    var key = $"rate_limit:{userId}:{apiName}";
    var redis = new RedisString<int>(connection, key, window);
    
    var current = await redis.GetAsync();
    var count = current.HasValue ? current.Value : 0;
    
    if (count >= maxRequests)
    {
        return false;
    }
    
    await redis.IncrementAsync(1);
    return true;
}
```

#### CORS 설정
- [ ] 허용된 도메인만 접근할 수 있도록 CORS를 설정한다
- [ ] 개발 환경과 운영 환경의 CORS 정책을 분리한다
- [ ] Preflight 요청을 적절히 처리한다

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("GamePolicy", policy =>
    {
        policy.WithOrigins("https://yourgame.com")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

#### HTTPS 강제
- [ ] 운영 환경에서 HTTPS만 허용한다
- [ ] HTTP 요청을 HTTPS로 리다이렉트한다
- [ ] HSTS (HTTP Strict Transport Security) 헤더를 설정한다

### 6. 게임 로직 보안

#### 치팅 방지
- [ ] 클라이언트에서 전송된 데이터를 절대 신뢰하지 않는다
- [ ] 중요한 계산은 서버에서 수행한다
- [ ] 아이템 획득, 재화 증감은 서버에서 검증한다
- [ ] 게임 결과 데이터를 서버에서 재계산한다

```csharp
// 클라이언트가 전송한 결과를 그대로 사용하지 않음
public async Task<bool> CompleteDungeon(long userId, int stageId, DungeonResult clientResult)
{
    // 서버에서 재계산
    var expectedResult = CalculateDungeonResult(userId, stageId);
    
    // 클라이언트 결과와 비교 (허용 오차 범위 내)
    if (Math.Abs(expectedResult.Score - clientResult.Score) > 100)
    {
        // 의심스러운 활동 로깅
        LogSuspiciousActivity(userId, "던전 점수 불일치");
        return false;
    }
    
    // 검증 통과 시 보상 지급
    await GrantRewards(userId, expectedResult.Rewards);
    return true;
}
```

#### 자원 관리
- [ ] 재화 증감 시 오버플로우를 방지한다
- [ ] 음수 값을 허용하지 않는다
- [ ] 최대치를 초과하지 않도록 제한한다

```csharp
public async Task<bool> AddGold(long userId, long amount)
{
    if (amount < 0)
    {
        throw new ArgumentException("음수 금액은 추가할 수 없습니다");
    }
    
    var currentGold = await GetUserGold(userId);
    
    // 오버플로우 방지
    if (currentGold > long.MaxValue - amount)
    {
        throw new InvalidOperationException("골드 최대치를 초과합니다");
    }
    
    await db.Query("user_game_data")
        .Where("user_id", userId)
        .IncrementAsync("gold", amount);
    
    return true;
}
```

#### 동시성 제어
- [ ] 중요한 작업에 분산 락을 적용한다
- [ ] 아이템 사용, 재화 소모 시 동시성을 고려한다
- [ ] 트랜잭션 격리 수준을 적절히 설정한다

### 7. 로깅 및 모니터링

#### 로깅
- [ ] 모든 인증 실패를 로깅한다
- [ ] 의심스러운 활동을 로깅한다
- [ ] 에러 발생 시 상세 정보를 로깅한다
- [ ] 개인정보를 로그에 포함하지 않는다
- [ ] 로그 파일을 정기적으로 백업한다

#### 모니터링
- [ ] 비정상적인 API 호출 패턴을 감지한다
- [ ] 데이터베이스 성능을 모니터링한다
- [ ] Redis 메모리 사용량을 모니터링한다
- [ ] 서버 리소스 사용량을 모니터링한다

### 8. 환경 설정 및 배포

#### 환경 분리
- [ ] 개발, 스테이징, 운영 환경을 분리한다
- [ ] 각 환경별 설정 파일을 분리한다
- [ ] 운영 환경 설정을 코드 저장소에 포함하지 않는다

#### 민감 정보 관리
- [ ] API 키, 비밀번호를 환경 변수로 관리한다
- [ ] User Secrets를 활용한다 (개발 환경)
- [ ] Azure Key Vault 등을 활용한다 (운영 환경)
- [ ] .gitignore에 설정 파일을 추가한다

```
# .gitignore
appsettings.Production.json
appsettings.Staging.json
*.user
```

#### 코드 보안
- [ ] 디버그 모드를 운영 환경에서 비활성화한다
- [ ] 상세 에러 메시지를 클라이언트에 노출하지 않는다
- [ ] 사용하지 않는 API를 비활성화한다
- [ ] 정기적으로 보안 업데이트를 적용한다

### 9. 정기 점검 항목

#### 주간 점검
- [ ] 의심스러운 로그인 시도 확인
- [ ] 비정상적인 API 호출 패턴 확인
- [ ] 데이터베이스 슬로우 쿼리 확인

#### 월간 점검
- [ ] 사용하지 않는 토큰 정리
- [ ] 만료된 우편 데이터 삭제
- [ ] 데이터베이스 백업 확인
- [ ] 보안 패치 적용 여부 확인

#### 분기별 점검
- [ ] 전체 보안 점검 수행
- [ ] 침투 테스트 실시
- [ ] 백업 복구 테스트
- [ ] 재해 복구 계획 검토

### 10. 사고 대응

#### 보안 사고 발생 시
- [ ] 즉시 관련 계정 및 토큰을 무효화한다
- [ ] 영향 범위를 파악한다
- [ ] 데이터베이스를 백업한다
- [ ] 관련 로그를 수집한다
- [ ] 원인을 분석하고 재발 방지 조치를 취한다

#### 비상 연락 체계
- [ ] 보안 사고 대응 팀을 구성한다
- [ ] 비상 연락망을 준비한다
- [ ] 사고 대응 매뉴얼을 작성한다

### 보안 체크리스트 사용 방법

1. **개발 단계**: 각 기능 구현 시 관련 항목을 확인한다
2. **코드 리뷰**: 리뷰 시 체크리스트를 참고한다
3. **배포 전**: 모든 항목이 완료되었는지 최종 확인한다
4. **정기 점검**: 주기적으로 보안 상태를 점검한다

---

## H. 자주 발생하는 에러 해결 방법

### 1. 데이터베이스 연결 오류

#### 에러 메시지
```
MySql.Data.MySqlClient.MySqlException: Unable to connect to any of the specified MySQL hosts.
```

#### 원인
- MySQL 서버가 실행되지 않음
- 연결 문자열이 잘못됨
- 방화벽이 연결을 차단함

#### 해결 방법

1. MySQL 서비스 상태 확인
```bash
# Windows
services.msc에서 MySQL 서비스 확인

# Linux
sudo systemctl status mysql
```

2. 연결 문자열 확인
```json
{
  "ConnectionStrings": {
    "GameDb": "Server=localhost;Database=game_db;Uid=root;Pwd=password;CharSet=utf8mb4;"
  }
}
```

3. 방화벽 설정 확인
```bash
# MySQL 포트 3306이 열려있는지 확인
netstat -an | findstr 3306
```

### 2. Redis 연결 오류

#### 에러 메시지
```
StackExchange.Redis.RedisConnectionException: It was not possible to connect to the redis server(s).
```

#### 원인
- Redis 서버가 실행되지 않음
- Redis 설정이 잘못됨
- 네트워크 연결 문제

#### 해결 방법

1. Redis 서비스 상태 확인
```bash
# Windows
redis-cli ping
# 응답: PONG

# Linux
sudo systemctl status redis
```

2. Redis 연결 설정 확인
```csharp
var config = new RedisConfig("name", "localhost:6379");
var connection = new RedisConnection(config);
```

3. Redis 비밀번호 확인
```
# redis.conf
requirepass your_password
```

```csharp
var config = new RedisConfig("name", "localhost:6379,password=your_password");
```

### 3. SqlKata 쿼리 오류

#### 에러 메시지
```
MySql.Data.MySqlClient.MySqlException: Unknown column 'XXX' in 'field list'
```

#### 원인
- 존재하지 않는 컬럼을 조회함
- 테이블명 또는 컬럼명 오타

#### 해결 방법

1. 컬럼명 확인
```csharp
// 잘못된 컬럼명
var user = await db.Query("users")
    .Select("user_id", "emial")  // emial은 오타
    .FirstOrDefaultAsync<User>();

// 올바른 컬럼명
var user = await db.Query("users")
    .Select("user_id", "email")
    .FirstOrDefaultAsync<User>();
```

2. 테이블 스키마 확인
```sql
DESCRIBE users;
```

### 4. 토큰 검증 실패

#### 에러 메시지
```
토큰이 유효하지 않습니다
```

#### 원인
- 토큰 형식이 잘못됨
- 토큰이 만료됨
- 서명 검증 실패
- Redis에 토큰이 없음

#### 해결 방법

1. 토큰 형식 확인
```csharp
// 토큰 형식: {UserId}|{IssuedAt}|{Signature}
var parts = token.Split('|');
if (parts.Length != 3)
{
    return Error("토큰 형식이 올바르지 않습니다");
}
```

2. 토큰 만료 시간 확인
```csharp
var issuedAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(parts[1])).DateTime;
var expiryTime = issuedAt.AddHours(24);

if (DateTime.Now > expiryTime)
{
    return Error("토큰이 만료되었습니다");
}
```

3. Redis 토큰 확인
```csharp
var redis = new RedisString<long>(connection, $"token:{token}", null);
var userId = await redis.GetAsync();

if (!userId.HasValue)
{
    return Error("토큰이 존재하지 않습니다");
}
```

### 5. JSON 직렬화/역직렬화 오류

#### 에러 메시지
```
System.Text.Json.JsonException: The JSON value could not be converted to XXX
```

#### 원인
- JSON 형식이 잘못됨
- 데이터 타입 불일치
- NULL 값 처리 문제

#### 해결 방법

1. 클래스 속성과 JSON 필드명 일치 확인
```csharp
public class UserInfo
{
    [JsonPropertyName("user_id")]  // snake_case → camelCase 매핑
    public long UserId { get; set; }
    
    [JsonPropertyName("nickname")]
    public string Nickname { get; set; }
}
```

2. NULL 허용 설정
```csharp
public class UserInfo
{
    public long UserId { get; set; }
    public string? Nickname { get; set; }  // NULL 허용
}
```

3. JSON 옵션 설정
```csharp
var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

var user = JsonSerializer.Deserialize<UserInfo>(json, options);
```

### 6. 트랜잭션 오류

#### 에러 메시지
```
System.InvalidOperationException: This SqlConnection is already in use
```

#### 원인
- 트랜잭션이 중첩됨  