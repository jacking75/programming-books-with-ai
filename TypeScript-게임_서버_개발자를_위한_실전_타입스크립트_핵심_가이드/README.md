# 게임 서버 개발자를 위한 실전 타입스크립트 핵심 가이드  

저자: 최흥배, AI-Assisted   
    
권장 개발 환경
- **IDE**: Visual Studio Code
- **컴파일러**: TypeScript 5.9 이상
- **OS**: Windows 10 이상

---  

## 📚 TypeScript 게임 서버 개발자를 위한 핵심 학습 목차

### Chapter 0: 시작하기 전에 
- 0.1 왜 게임 서버에 TypeScript인가?
  - JavaScript의 문제점: 런타임 에러의 악몽
  - TypeScript가 주는 3가지 핵심 가치
  - 실제 게임 회사들의 TypeScript 도입 사례
- 0.2 학습 로드맵 & 예상 소요 시간

---

### Chapter 1: 개발 환경 구축 
- **1.1** Windows + VSCode 환경 설정
  - Node.js 설치 및 버전 관리
  - TypeScript 5.9 설치 (`npm install -g typescript`)
  - VSCode 필수 익스텐션 3가지
- **1.2** 첫 TypeScript 프로젝트 생성
  - `tsconfig.json` 핵심 옵션 5가지만 알기
  - 게임 서버용 권장 설정
- **1.3** 디버깅 환경 설정
  - VSCode에서 TypeScript 디버깅하기

---

### Chapter 2: 타입 시스템 핵심 
- **2.1** 기본 타입 - 알아야 할 것만
  - `string`, `number`, `boolean`
  - `null` vs `undefined` (게임 서버에서 자주 만나는 버그)
  - `any` vs `unknown` - 언제 써야 하나?
- **2.2** 객체와 인터페이스
  - 게임 엔티티 모델링 예제 (Player, Monster, Item)
  - `interface` vs `type` - 실전 선택 기준
  - Optional Properties (`?`)와 Readonly
- **2.3** 배열과 튜플
  - 인벤토리 시스템 구현 예제
  - `Array<T>` vs `T[]`
- **2.4** 유니온과 리터럴 타입
  - 게임 상태 관리 (`'idle' | 'running' | 'paused'`)
  - 타입 좁히기 (Type Narrowing)

---

### Chapter 3: 함수와 제네릭 
- **3.1** 함수 타입 정의
  - 매개변수와 반환값 타입
  - 옵셔널 파라미터 vs 기본값
  - 콜백 함수 타입 정의
- **3.2** 제네릭의 힘
  - 왜 제네릭이 필요한가? (실전 문제 상황)
  - 패킷 핸들러 제네릭 구현
  - Constraint (`extends`) 활용
- **3.3** 비동기 처리
  - `Promise<T>` 타입
  - `async/await` 패턴
  - 게임 서버 DB 쿼리 예제

---

### Chapter 4: 클래스와 OOP 
- **4.1** 클래스 기본
  - 접근 제어자 (`public`, `private`, `protected`)
  - 생성자와 프로퍼티 초기화
- **4.2** 상속과 다형성
  - 게임 엔티티 계층 구조 설계
  - Abstract 클래스 활용
- **4.3** 인터페이스 구현
  - 컴포넌트 패턴 구현 예제

---

### Chapter 5: 고급 타입 패턴
- **5.1** 유틸리티 타입 TOP 5
  - `Partial<T>` - 부분 업데이트
  - `Required<T>` - 필수 필드 강제
  - `Pick<T, K>` - 필요한 속성만
  - `Omit<T, K>` - 제외하고 선택
  - `Record<K, T>` - 딕셔너리 타입
- **5.2** 타입 가드
  - 사용자 정의 타입 가드
  - `instanceof`와 `typeof`
- **5.3** 맵드 타입과 조건부 타입 (간단히)
  - 실전 예제로만 이해하기

---

### Chapter 6: 모듈과 네임스페이스 
- **6.1** ES6 모듈 시스템
  - `import`/`export` 패턴
  - Default vs Named Export
- **6.2** 프로젝트 구조화
  - 게임 서버 폴더 구조 예제
  - Barrel Export 패턴
- **6.3** 외부 라이브러리 사용
  - `@types` 패키지 이해
  - 타입 정의 파일 (`.d.ts`)

---

### Chapter 7: 실전 게임 서버 패턴 
- **7.1** WebSocket 서버 구현
  - Socket.io + TypeScript 설정
  - 타입 안전한 이벤트 핸들링
- **7.2** 상태 관리 패턴
  - Room 관리 시스템
  - Player Session 관리
- **7.3** 데이터베이스 연동
  - TypeORM 또는 Prisma 소개
  - 타입 안전한 쿼리
- **7.4** 에러 핸들링
  - 커스텀 에러 타입
  - 에러 처리 미들웨어

---

### Chapter 8: 성능과 최적화 
- **8.1** 컴파일 옵션 최적화
  - 빌드 속도 향상 팁
  - 프로덕션 빌드 설정
- **8.2** 타입 추론 활용
  - 불필요한 타입 선언 줄이기
  - `as const` assertion
- **8.3** 흔한 실수와 안티패턴
  - `any` 남용 피하기
  - 순환 의존성 해결

---

### Chapter 9: 테스팅 
- **9.1** Jest + TypeScript 설정
  - 테스트 환경 구성
- **9.2** 유닛 테스트 작성
  - 게임 로직 테스트 예제
  - Mock 객체 활용
- **9.3** 타입 안전한 테스트 작성

---

### Chapter 10: 실전 미니 프로젝트 
- **10.1** 실시간 멀티플레이어 게임 서버 만들기
  - 요구사항 정의
  - 프로젝트 구조 설계
  - 단계별 구현
- **10.2** 완성 코드 리뷰
  - TypeScript를 활용한 코드 품질 향상
  - 리팩토링 포인트

```
┌─────────────────────────────────────────────┐
│  Mini Project: Real-time Battle Server      │
├─────────────────────────────────────────────┤
│  ✓ Player Connection Management             │
│  ✓ Room-based Matchmaking                   │
│  ✓ Turn-based Combat Logic                  │
│  ✓ Real-time State Synchronization          │
│  ✓ Type-safe Packet Communication           │
└─────────────────────────────────────────────┘
```

---

### Appendix: 빠른 참조
- **A.1** 자주 쓰는 타입 치트시트
- **A.2** tsconfig.json 옵션 가이드
- **A.3** VSCode 단축키 & 팁
- **A.4** 추가 학습 리소스
  - 공식 문서
  - 게임 서버 오픈소스 프로젝트
  - TypeScript 커뮤니티

---   
  

# Chapter 0: 시작하기 전에

## 0.1 왜 게임 서버에 TypeScript인가?

### JavaScript의 문제점: 런타임 에러의 악몽
게임 서버 개발자라면 다음과 같은 상황을 경험해보셨을 겁니다. 수백 명의 유저가 접속한 상태에서 갑자기 서버가 크래시됩니다. 로그를 확인해보니 `Cannot read property 'health' of undefined`라는 에러가 발생했다. 누군가 코드를 수정하면서 Player 객체의 속성명을 `hp`에서 `health`로 변경했는데, 한 곳에서 업데이트를 빠뜨린 것이죠. JavaScript는 이런 실수를 실행 전까지는 절대 알려주지 않았다.

```javascript
// JavaScript - 런타임 전까지 에러를 모름
function dealDamage(player, amount) {
  player.helth -= amount;  // 오타! 하지만 실행 전까지 모름
  if (player.helth <= 0) {
    player.status = 'ded';  // 또 오타!
  }
}

// 게임 중에 갑자기...
dealDamage(somePlayer, 50);  // 💥 서버 크래시!
```

게임 서버는 24시간 365일 안정적으로 돌아가야 한다. 수천 명의 유저가 동시에 접속하고, 실시간으로 상호작용한다. 런타임 에러 하나가 전체 서버를 다운시킬 수 있고, 이는 곧 유저 이탈과 매출 손실로 이어진다.

### TypeScript가 주는 3가지 핵심 가치

**1. 컴파일 타임 에러 검출 - 배포 전에 버그를 잡는다**  
TypeScript는 코드를 작성하는 순간 에러를 발견한다. 심지어 VSCode에서는 타이핑하는 동안 빨간 밑줄로 문제를 알려준다.

```typescript
// TypeScript - 코드 작성 중에 바로 에러 발견
interface Player {
  health: number;
  maxHealth: number;
  status: 'alive' | 'dead' | 'respawning';
}

function dealDamage(player: Player, amount: number): void {
  player.helth -= amount;  // ❌ 즉시 에러: Property 'helth' does not exist
  if (player.health <= 0) {
    player.status = 'ded';  // ❌ 에러: Type '"ded"' is not assignable
  }
}
```

VSCode에서 `helth`를 입력하는 순간 빨간 밑줄이 그어지고, 마우스를 올리면 정확히 무엇이 잘못되었는지 알려준다. 배포 전에 모든 오타와 타입 불일치를 잡아낼 수 있다.
  
**2. 강력한 자동완성과 리팩토링 - 개발 속도가 2배 빨라진다**  
게임 서버는 수많은 엔티티와 시스템이 복잡하게 얽혀있다. Player, Monster, Item, Skill, Quest... 각각 수십 개의 속성을 가지고 있죠. TypeScript를 사용하면 객체의 속성을 일일이 기억할 필요가 없다.

```typescript
interface Monster {
  id: string;
  name: string;
  level: number;
  health: number;
  attackPower: number;
  defenseRating: number;
  experienceReward: number;
  dropItems: Item[];
  aiType: 'aggressive' | 'passive' | 'defensive';
}

function processMonster(monster: Monster) {
  monster.  // ← 점을 찍는 순간 모든 속성이 자동완성!
  // IDE가 9개 속성을 모두 보여주고, 각 타입도 알려줌
}
```

속성명을 변경해야 할 때도 마찬가지이다. `attackPower`를 `attack`으로 변경하고 싶다면, VSCode의 "Rename Symbol" 기능(F2)을 사용하면 프로젝트 전체에서 해당 속성을 사용하는 모든 곳이 한 번에 변경된다. 수백 개의 파일에서 수동으로 찾아 바꿀 필요가 없다.
  
**3. 명확한 계약 - 팀 협업이 수월해진다**  
게임 서버는 여러 개발자가 동시에 작업한다. 전투 시스템, 인벤토리 시스템, 퀘스트 시스템... 각 시스템의 인터페이스가 명확하지 않으면 끊임없이 소통하며 확인해야 한다. "이 함수에 뭘 넘겨야 하죠?", "이 객체에 어떤 속성이 있나요?"

```typescript
// 인터페이스가 곧 문서이자 계약
interface CombatResult {
  attackerId: string;
  targetId: string;
  damageDealt: number;
  isCritical: boolean;
  targetDied: boolean;
  experienceGained?: number;  // optional - 타겟이 죽었을 때만
}

// 이 함수의 시그니처만 봐도 모든 것을 알 수 있음
function processCombat(
  attacker: Player, 
  target: Monster
): CombatResult {
  // 구현...
}
```

다른 개발자가 `processCombat` 함수를 사용할 때, 함수 정의를 보러 갈 필요도 없이 타입 정보만으로 모든 것을 파악할 수 있다. 문서가 코드에 내장되어 있고, 항상 최신 상태로 유지된다.
  

### 실제 게임 회사들의 TypeScript 도입 사례

```
┌─────────────────────────────────────────────────────┐
│  Major Game Companies Using TypeScript              │
├─────────────────────────────────────────────────────┤
│                                                      │
│  🎮 Riot Games                                       │
│     - League of Legends 웹 클라이언트               │
│     - 내부 툴 및 서비스                             │
│                                                      │
│  🎮 Unity Technologies                               │
│     - Unity Dashboard & Services                     │
│                                                      │
│  🎮 Roblox                                           │
│     - Luau (TypeScript에서 영감받은 타입 시스템)    │
│                                                      │
│  🎮 Supercell                                        │
│     - 백엔드 서비스 일부                            │
│                                                      │
│  🎮 Epic Games                                       │
│     - Epic Games Store 웹 플랫폼                    │
│                                                      │
└─────────────────────────────────────────────────────┘
```

특히 실시간 멀티플레이어 게임 서버에서 TypeScript는 강력한 장점을 발휘한다. Socket.io나 WebSocket과 결합하면 클라이언트와 서버 간 통신 프로토콜을 타입으로 정의할 수 있어, 패킷 구조 불일치로 인한 버그를 원천 차단할 수 있다.

### 실제 비교: JavaScript vs TypeScript
게임 서버에서 흔히 발생하는 버그 시나리오를 비교해보겠다.

```
┌──────────────────────────────────────────────────────┐
│  Scenario: 플레이어 이동 패킷 처리                   │
└──────────────────────────────────────────────────────┘

JavaScript 버전:
──────────────────────────────────────────────────────
function handleMove(packet) {
  const player = players.get(packet.playerId);
  player.position.x = packet.x;  // 💣 player가 null이면?
  player.position.y = packet.y;  // 💣 packet.x가 문자열이면?
  
  broadcastToRoom(player.roomId, {
    type: 'playerMoved',
    data: {
      playerId: packet.playerId,
      pos: { x: packet.x, y: packet.y }  // 오타: position이 아닌 pos
    }
  });
}

🚨 발생 가능한 문제들:
- player가 존재하지 않을 때 크래시
- packet 데이터 형식이 잘못되었을 때 크래시
- 클라이언트와 속성명 불일치로 버그
- 실행 전까지 문제를 알 수 없음


TypeScript 버전:
──────────────────────────────────────────────────────
interface MovePacket {
  playerId: string;
  x: number;
  y: number;
}

interface Position {
  x: number;
  y: number;
}

function handleMove(packet: MovePacket): void {
  const player = players.get(packet.playerId);
  
  if (!player) {  // ✅ 타입 가드로 안전하게 처리
    console.error('Player not found');
    return;
  }
  
  player.position.x = packet.x;  // ✅ 타입 체크 완료
  player.position.y = packet.y;
  
  broadcastToRoom(player.roomId, {
    type: 'playerMoved',
    data: {
      playerId: packet.playerId,
      pos: { x: packet.x, y: packet.y }  // ❌ 컴파일 에러!
    }                                     // position으로 수정 요구
  });
}

✅ 보장되는 안전성:
- packet 구조가 정확히 정의됨
- null/undefined 체크 강제
- 타입 불일치 즉시 발견
- 리팩토링 시 안전
```

---

## 0.2 학습 로드맵 & 예상 소요 시간
이 가이드는 "완벽한 이해"보다 "빠른 실전 투입"에 초점을 맞췄다. 경력 개발자인 당신은 이미 프로그래밍 개념을 이해하고 있으므로, TypeScript의 핵심만 집중적으로 학습한다.

```
 ╔══════════════════════════════════════════════════════╗
 ║           학습 여정 로드맵                           ║
 ╚══════════════════════════════════════════════════════╝

 ├─ Chapter 0-1: 환경 설정 & 첫 프로젝트       
 ├─ Chapter 2: 타입 시스템 핵심                
 ├─ Chapter 3: 함수와 제네릭                   
 └─ Chapter 4: 클래스와 OOP                    
     └─> 결과: 기본 문법 완전 숙지
 ├─ Chapter 5: 고급 타입 패턴                  
 ├─ Chapter 6: 모듈과 프로젝트 구조            
 └─ Chapter 7: 실전 게임 서버 패턴             
     └─> 결과: 실전 패턴 습득
 ├─ Chapter 8-9: 최적화 & 테스팅               
 └─ Chapter 10: 미니 프로젝트                  
     └─> 결과: 완전한 게임 서버 구현 경험

 ═══════════════════════════════════════════════════════
```

### 학습 전략

**집중 학습 모드**: 각 챕터의 예제 코드를 직접 타이핑하면서 따라하는 것이 중요하다. 복사-붙여넣기가 아닌 직접 타이핑해야 VSCode의 자동완성과 에러 메시지를 경험할 수 있다.

**틈새 학습 모드**: 시간이 부족하다면 챕터별로 나눠서 학습해도 괜찮다. 각 챕터는 독립적으로 구성되어 있어, 필요한 부분부터 먼저 학습할 수 있다. 예를 들어, 당장 제네릭이 필요하다면 Chapter 3만 먼저 봐도 된다.

**실전 적용 우선**: Chapter 7까지만 학습해도 실제 프로젝트에 바로 적용할 수 있다. 나머지는 필요할 때 참고하는 레퍼런스로 활용한다.

이제 본격적으로 시작해보겠다!
  
---
  
# Chapter 1: 개발 환경 구축

## 1.1 Windows + VSCode 환경 설정

### Node.js 설치 및 버전 관리
TypeScript는 Node.js 환경에서 실행되므로, 먼저 Node.js를 설치해야 한다. 게임 서버 개발자라면 이미 Node.js가 설치되어 있을 가능성이 높지만, 최신 LTS 버전을 권장한다.

**설치 단계:**

1. Node.js 공식 사이트 방문: `https://nodejs.org`
2. LTS 버전 다운로드 (2026년 1월 기준 권장: Node.js 20.x 이상)
3. 설치 프로그램 실행 후 기본 옵션으로 진행

**설치 확인:**

윈도우 터미널(또는 PowerShell)을 열고 다음 명령어를 실행한다:

```bash
node --version
# 출력 예: v20.11.0

npm --version
# 출력 예: 10.2.4
```

두 명령어가 모두 버전을 출력하면 정상적으로 설치된 것이다.

**버전 관리 팁 (선택사항):**  
여러 프로젝트를 진행하며 다양한 Node.js 버전이 필요하다면 `nvm-windows`(Node Version Manager for Windows)를 사용하는 것을 추천한다. 하지만 당장은 필수는 아니므로, 나중에 필요할 때 도입해도 된다.

### TypeScript 5.9 설치
Node.js가 준비되었다면 이제 TypeScript를 전역으로 설치한다. 전역 설치는 시스템 어디서든 `tsc` (TypeScript Compiler) 명령어를 사용할 수 있게 해준다.

```bash
npm install -g typescript
```

설치가 완료되면 버전을 확인하자:

```bash
tsc --version
# 출력 예: Version 5.9.2
```

TypeScript 5.9가 설치되었다! 참고로 곧 TypeScript 6.0이 출시될 예정이지만, 5.9는 매우 안정적이며 실무에서 충분히 사용할 수 있다.

#### Windows
PowerShell을 관리자 권한으로 실행한 후 다음 명령어를 입력한다:  
```
Set-ExecutionPolicy RemoteSigned -Scope CurrentUser
```
또는 모든 사용자에게 적용하려면:  
```
CopySet-ExecutionPolicy RemoteSigned -Scope LocalMachine
```
확인 메시지가 나오면 Y를 입력하여 승인한다.
  

### VSCode 필수 익스텐션 3가지
Visual Studio Code는 TypeScript 지원이 내장되어 있어 별도 설정 없이도 기본 기능을 사용할 수 있다. 하지만 다음 익스텐션을 설치하면 개발 경험이 훨씬 향상된다.

**1. ESLint**  
코드 스타일과 잠재적 버그를 실시간으로 검사해줍니다. TypeScript와 완벽하게 통합되어 타입 관련 문제도 추가로 잡아냅니다.

- VSCode에서 `Ctrl + Shift + X`로 익스텐션 패널 열기
- "ESLint" 검색 후 설치 (제작자: Microsoft)

**2. Prettier - Code formatter**  
코드 포매팅을 자동으로 처리해준다. 팀 프로젝트에서 일관된 코드 스타일을 유지하는 데 필수적이다.

- "Prettier" 검색 후 설치 (제작자: Prettier)
- 설정에서 "Format On Save" 활성화 권장
  
**3. Error Lens**    
에러와 경고를 코드 라인 옆에 바로 표시해준다. 기본적으로는 마우스를 올려야 에러 메시지를 볼 수 있지만, 이 익스텐션을 사용하면 즉시 확인할 수 있어 생산성이 크게 향상된다.

- "Error Lens" 검색 후 설치

```
일반 VSCode:
  const player: Player = null;
                         ~~~~ (마우스를 올려야 에러 확인)

Error Lens 사용 시:
  const player: Player = null; ⚠️ Type 'null' is not assignable to type 'Player'
                                  (라인 옆에 바로 표시)
```

**VSCode 설정 최적화 (선택사항):**  
`Ctrl + ,`로 설정을 열고 다음을 검색하여 설정한다:

- "Format On Save": 체크 (저장할 때 자동 포매팅)
- "Auto Save": `afterDelay`로 설정 (자동 저장)
- "Tab Size": 2 또는 4 (프로젝트 컨벤션에 따라)

---

## 1.2 첫 TypeScript 프로젝트 생성
이제 실제로 TypeScript 프로젝트를 만들어보자. 간단한 게임 서버 시뮬레이션 프로젝트를 생성하겠다.

### 프로젝트 폴더 생성 및 초기화
원하는 위치에 프로젝트 폴더를 만들고 초기화한다:  

```bash
# 프로젝트 폴더 생성
mkdir game-server-ts
cd game-server-ts

# npm 프로젝트 초기화
npm init -y
```

`npm init -y` 명령어는 `package.json` 파일을 기본 설정으로 생성한다. 이 파일은 프로젝트의 의존성과 스크립트를 관리한다.

### TypeScript 프로젝트 초기화
이제 TypeScript 설정 파일을 생성한다:  

```bash
tsc --init
```

이 명령어는 `tsconfig.json` 파일을 생성한다. 이 파일에는 수많은 옵션이 주석으로 들어있는데, 처음에는 압도적으로 느껴질 수 있다. 그러나 걱정하지 말아라. 실제로 자주 사용하는 옵션은 몇 가지뿐이다.

### tsconfig.json 핵심 옵션 5가지만 알기
생성된 `tsconfig.json` 파일을 열어보면 수백 줄의 주석이 있다. 이 중 게임 서버 개발에 필수적인 옵션만 추려서 간단하게 재구성하겠다.

기존 내용을 모두 지우고 다음으로 교체하자:

```json
{
  "compilerOptions": {
    /* 핵심 옵션 1: 타겟 JavaScript 버전 */
    "target": "ES2020",
    "module": "commonjs",
    "lib": ["ES2020"],
    
    /* 핵심 옵션 2: 출력 디렉토리 */
    "outDir": "./dist",
    "rootDir": "./src",
    
    /* 핵심 옵션 3: 엄격한 타입 체크 (가장 중요!) */
    "strict": true,
    
    /* 핵심 옵션 4: 모듈 해석 방식 */
    "moduleResolution": "node",
    "esModuleInterop": true,
    
    /* 핵심 옵션 5: 소스맵 생성 (디버깅용) */
    "sourceMap": true,
    
    /* 추가 편의 옵션 */
    "skipLibCheck": true,
    "forceConsistentCasingInFileNames": true
  },
  "include": ["src/**/*"],
  "exclude": ["node_modules", "dist"]
}
```

각 옵션을 자세히 설명하겠다.

**옵션 1: target과 module**

- `"target": "ES2020"`: TypeScript 코드가 어떤 JavaScript 버전으로 컴파일될지 지정한다. ES2020은 현대적인 Node.js에서 안정적으로 동작하며, async/await 같은 최신 기능을 모두 지원한다.
- `"module": "commonjs"`: Node.js는 CommonJS 모듈 시스템을 사용하므로 이 옵션을 선택한다. (`require`와 `module.exports`)

**옵션 2: 출력 디렉토리**

- `"outDir": "./dist"`: 컴파일된 JavaScript 파일이 저장될 폴더이다.
- `"rootDir": "./src"`: TypeScript 소스 코드가 위치할 폴더이다.

이렇게 설정하면 프로젝트 구조가 깔끔하게 분리된다:

```
game-server-ts/
├── src/           ← TypeScript 소스 코드
│   └── index.ts
├── dist/          ← 컴파일된 JavaScript (자동 생성)
│   └── index.js
├── tsconfig.json
└── package.json
```

**옵션 3: strict 모드 (가장 중요!)**

`"strict": true`는 TypeScript의 모든 엄격한 타입 체크를 활성화한다. 이것이 TypeScript를 사용하는 핵심 이유이다! 이 옵션은 다음을 포함한다:

- `strictNullChecks`: `null`과 `undefined`를 명시적으로 처리하도록 강제
- `strictFunctionTypes`: 함수 파라미터 타입을 엄격하게 체크
- `noImplicitAny`: 타입을 추론할 수 없을 때 `any` 사용을 금지

처음에는 에러가 많이 나서 귀찮게 느껴질 수 있지만, 이것이 바로 런타임 버그를 방지하는 핵심이다. 절대 `false`로 바꾸지 말아라!

**옵션 4: 모듈 해석**

- `"moduleResolution": "node"`: Node.js 방식으로 모듈을 찾는다.
- `"esModuleInterop": true"`: CommonJS 모듈을 import할 때 호환성을 향상시킨다.

**옵션 5: 소스맵**

`"sourceMap": true`는 `.js.map` 파일을 생성한다. 이 파일 덕분에 디버깅할 때 컴파일된 JavaScript가 아닌 원본 TypeScript 코드를 볼 수 있다. 개발 중에는 필수이다.

### 게임 서버용 권장 설정
위 기본 설정으로 충분하지만, 게임 서버 특성상 다음을 추가하면 더 좋다:

```json
{
  "compilerOptions": {
    // ... 위의 설정 유지 ...
    
    /* 게임 서버 추가 권장 사항 */
    "resolveJsonModule": true,      // JSON 파일 import 허용
    "removeComments": true,         // 프로덕션 빌드 크기 감소
    "noUnusedLocals": true,         // 사용하지 않는 변수 경고
    "noUnusedParameters": true,     // 사용하지 않는 파라미터 경고
    "noImplicitReturns": true       // 함수가 항상 값을 반환하도록 강제
  }
}
```

이제 설정이 완료되었다!

### 첫 TypeScript 파일 작성

`src` 폴더를 만들고 첫 TypeScript 파일을 작성해보자:

```bash
mkdir src
```

VSCode에서 `src/index.ts` 파일을 생성하고 다음 코드를 입력한다:

```typescript
// src/index.ts

// 플레이어 인터페이스 정의
interface Player {
  id: string;
  name: string;
  health: number;
  maxHealth: number;
  level: number;
}

// 플레이어 생성 함수
function createPlayer(name: string, level: number): Player {
  const maxHealth = 100 + (level - 1) * 10;
  
  return {
    id: `player_${Date.now()}`,
    name: name,
    health: maxHealth,
    maxHealth: maxHealth,
    level: level
  };
}

// 데미지 처리 함수
function takeDamage(player: Player, damage: number): void {
  player.health -= damage;
  
  if (player.health < 0) {
    player.health = 0;
  }
  
  console.log(`${player.name} took ${damage} damage. HP: ${player.health}/${player.maxHealth}`);
  
  if (player.health === 0) {
    console.log(`${player.name} has been defeated!`);
  }
}

// 게임 시뮬레이션
const warrior = createPlayer("Warrior", 5);
console.log(`${warrior.name} joined the game! (Level ${warrior.level})`);

takeDamage(warrior, 30);
takeDamage(warrior, 50);
takeDamage(warrior, 100);
```

코드를 작성하면서 VSCode의 자동완성이 어떻게 동작하는지 주목하자. `player.`를 입력하면 `id`, `name`, `health` 등 모든 속성이 나타난다. 이것이 TypeScript의 힘이다!

### 컴파일 및 실행
TypeScript 코드를 JavaScript로 컴파일한다:

```bash
tsc
```

명령어를 실행하면 `dist` 폴더가 생성되고 그 안에 `index.js` 파일이 만들어진다. 이제 실행해보자:

```bash
node dist/index.js
```

**출력 결과:**

```
Warrior joined the game! (Level 5)
Warrior took 30 damage. HP: 114/140
Warrior took 50 damage. HP: 64/140
Warrior took 100 damage. HP: 0/140
Warrior has been defeated!
```

축하한다! 첫 TypeScript 프로그램을 성공적으로 실행했다!

### 개발 편의성 향상: npm scripts
매번 `tsc`를 입력하고 `node dist/index.js`를 실행하는 것은 번거롭다. `package.json`에 스크립트를 추가하여 편리하게 만들어보자:

`package.json` 파일을 열고 `"scripts"` 섹션을 다음과 같이 수정한다:

```json
{
  "name": "game-server-ts",
  "version": "1.0.0",
  "scripts": {
    "build": "tsc",
    "start": "node dist/index.js",
    "dev": "tsc && node dist/index.js"
  },
  "devDependencies": {
    "typescript": "^5.9.0"
  }
}
```

이제 다음 명령어로 간편하게 실행할 수 있다:

```bash
npm run dev
```

**더 나은 개발 경험: ts-node와 nodemon**

개발 중에는 코드를 수정할 때마다 컴파일하고 실행하는 것이 번거롭다. `ts-node`와 `nodemon`을 사용하면 파일이 변경될 때마다 자동으로 재실행된다.

```bash
npm install --save-dev ts-node nodemon @types/node
```

`nodemon.json` 파일을 프로젝트 루트에 생성하자:

```json
{
  "watch": ["src"],
  "ext": "ts",
  "exec": "ts-node src/index.ts"
}
```

`package.json`의 스크립트를 업데이트한다:

```json
{
  "scripts": {
    "build": "tsc",
    "start": "node dist/index.js",
    "dev": "nodemon"
  }
}
```

이제 `npm run dev`를 실행하면 파일을 저장할 때마다 자동으로 재실행된다! 개발 생산성이 크게 향상되었다.


## 1.3 디버깅 환경 설정

### VSCode에서 TypeScript 디버깅하기
VSCode는 강력한 디버깅 기능을 내장하고 있다. 중단점(breakpoint)을 설정하고 변수 값을 실시간으로 확인할 수 있다.

**디버깅 설정 파일 생성:**

1. VSCode에서 `Ctrl + Shift + D`로 디버그 패널 열기
2. "create a launch.json file" 클릭
3. "Node.js" 선택

`.vscode/launch.json` 파일이 생성된다. 다음과 같이 수정하자:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "type": "node",
      "request": "launch",
      "name": "Debug TypeScript",
      "runtimeArgs": ["-r", "ts-node/register"],
      "args": ["${workspaceFolder}/src/index.ts"],
      "cwd": "${workspaceFolder}",
      "protocol": "inspector",
      "sourceMaps": true,
      "outFiles": ["${workspaceFolder}/dist/**/*.js"]
    }
  ]
}
```

**디버깅 사용 방법:**

1. `src/index.ts` 파일을 열기
2. 줄 번호 왼쪽을 클릭하여 중단점(빨간 점) 설정
3. `F5` 키를 눌러 디버깅 시작
4. 코드가 중단점에서 멈추면 변수에 마우스를 올려 값 확인
5. `F10` (Step Over), `F11` (Step Into), `Shift+F11` (Step Out)으로 코드 탐색

```
┌──────────────────────────────────────────────────┐
│  VSCode 디버깅 단축키                            │
├──────────────────────────────────────────────────┤
│  F5          : 디버깅 시작/계속                  │
│  F9          : 중단점 토글                       │
│  F10         : 다음 줄로 (Step Over)             │
│  F11         : 함수 안으로 (Step Into)           │
│  Shift+F11   : 함수 밖으로 (Step Out)            │
│  Ctrl+Shift+F5 : 재시작                          │
│  Shift+F5    : 중지                              │
└──────────────────────────────────────────────────┘
```

### **실전 디버깅 예제**
`takeDamage` 함수에 버그가 있다고 가정해보자. 중단점을 설정하고 변수 값을 추적해본다:

```typescript
function takeDamage(player: Player, damage: number): void {
  console.log(`Before: ${player.health}`);  // ← 여기에 중단점 설정
  player.health -= damage;
  
  if (player.health < 0) {
    player.health = 0;
  }
  
  console.log(`After: ${player.health}`);
}
```

디버깅을 시작하면 중단점에서 멈추고, 왼쪽 패널에서 `player` 객체의 모든 속성 값을 확인할 수 있다. 이 방식으로 복잡한 게임 로직도 쉽게 디버깅할 수 있다.

---

## 🎉 Chapter 0-1 완료!
축하한다! 이제 TypeScript 개발 환경이 완벽하게 준비되었다.

**지금까지 배운 것:**
- ✅ TypeScript를 사용해야 하는 명확한 이유
- ✅ Windows + VSCode + Node.js 환경 구축
- ✅ TypeScript 프로젝트 생성 및 설정
- ✅ 첫 TypeScript 코드 작성 및 실행
- ✅ 디버깅 환경 설정

**다음 Chapter에서는:**
TypeScript의 핵심인 타입 시스템을 학습한다. 게임 엔티티를 모델링하면서 인터페이스, 유니온 타입, 타입 가드 등을 실전 예제로 익히게 된다.



# Chapter 2: 타입 시스템 핵심

## 2.1 기본 타입 - 알아야 할 것만
TypeScript의 타입 시스템은 JavaScript의 모든 값에 타입을 부여합니다. 게임 서버 개발에 자주 사용하는 핵심 타입부터 시작하겠습니다.

### 원시 타입 (Primitive Types)
게임 서버에서 가장 많이 사용하는 기본 타입들이다.

```typescript
// 문자열 - 플레이어 이름, ID, 메시지 등
let playerName: string = "DragonSlayer";
let sessionId: string = "sess_12345";

// 숫자 - 레벨, HP, 데미지, 좌표 등
let level: number = 42;
let health: number = 100.5;
let posX: number = 128;

// 불리언 - 상태 플래그
let isAlive: boolean = true;
let hasCompletedQuest: boolean = false;

// 배열 표현 방식 두 가지 (동일함)
let inventory: string[] = ["sword", "potion", "shield"];
let scores: Array<number> = [100, 95, 87];
```

**타입 추론 (Type Inference):**  
TypeScript는 똑똑해서 값을 보고 타입을 자동으로 추론합한다. 명시적으로 타입을 작성하지 않아도 된다.

```typescript
// 타입을 명시하지 않아도 TypeScript가 추론함
let playerName = "DragonSlayer";  // string으로 추론
let level = 42;                   // number로 추론
let isAlive = true;               // boolean으로 추론

// 마우스를 올리면 VSCode가 추론된 타입을 보여줌
playerName = 123;  // ❌ 에러! Type 'number' is not assignable to type 'string'
```

일반적으로 값을 초기화할 때는 타입을 생략하고, 함수 파라미터나 반환값에는 명시하는 것이 관례이다.

### null vs undefined - 게임 서버에서 자주 만나는 버그
이 둘은 JavaScript에서 가장 많은 런타임 에러를 유발하는 주범이다. TypeScript는 이를 안전하게 다룰 수 있게 해준다.

```typescript
// undefined: 변수가 선언되었지만 값이 할당되지 않음
let playerData: string | undefined;
console.log(playerData);  // undefined

// null: 명시적으로 "값이 없음"을 나타냄
let currentTarget: Player | null = null;
```

**게임 서버에서 흔한 버그 시나리오:**

```typescript
// 플레이어 목록에서 특정 플레이어 찾기
const players = new Map<string, Player>();

function getPlayer(playerId: string): Player | undefined {
  return players.get(playerId);  // 존재하지 않으면 undefined 반환
}

// ❌ 위험한 코드 - strict 모드에서 에러!
function attackPlayer(targetId: string): void {
  const target = getPlayer(targetId);
  target.health -= 10;  // ❌ Object is possibly 'undefined'
}

// ✅ 안전한 코드 - null 체크
function attackPlayerSafe(targetId: string): void {
  const target = getPlayer(targetId);
  
  if (!target) {
    console.log("Target not found");
    return;
  }
  
  // 여기서는 target이 Player임이 보장됨
  target.health -= 10;  // ✅ OK!
}
```

TypeScript의 `strict` 모드는 이런 실수를 컴파일 타임에 잡아준다. 게임이 실행 중일 때 크래시하는 대신, 코드를 작성하는 순간 에러를 발견한다.

**Optional Chaining과 Nullish Coalescing:**

TypeScript(ES2020)에는 null/undefined를 안전하게 다루는 편리한 문법이 있다.

```typescript
// Optional Chaining (?.)
const playerName = currentPlayer?.name;  // currentPlayer가 null이면 undefined 반환
const guildName = player?.guild?.name;   // 중첩 속성도 안전하게 접근

// Nullish Coalescing (??)
const displayName = player?.name ?? "Guest";  // null/undefined면 기본값 사용
const maxHP = config?.maxHealth ?? 100;

// 실전 예제: 플레이어 길드 정보 가져오기
function getPlayerGuildName(playerId: string): string {
  const player = getPlayer(playerId);
  return player?.guild?.name ?? "No Guild";
}
```

이 문법들은 게임 서버에서 매우 자주 사용된다. 플레이어가 존재하지 않거나, 길드에 소속되지 않았거나, 옵셔널한 설정값이 없을 때 안전하게 처리할 수 있다.

### any vs unknown - 언제 써야 하나?
때로는 타입을 미리 알 수 없는 상황이 있다. 예를 들어, 클라이언트로부터 받은 JSON 패킷을 파싱할 때이다.

**any: 타입 체크 포기 (위험!)**

```typescript
let data: any = JSON.parse(packetString);
data.foobar();  // ✅ 컴파일 OK, 💥 런타임 크래시!
data.nonexistent.property;  // ✅ 컴파일 OK, 💥 런타임 크래시!

// any는 TypeScript의 보호를 완전히 포기하는 것
// 긴급 상황이 아니면 절대 사용하지 마세요!
```

**unknown: 안전한 대안**

```typescript
let data: unknown = JSON.parse(packetString);

// ❌ 바로 사용 불가
data.someMethod();  // ❌ 에러! Object is of type 'unknown'

// ✅ 타입을 확인한 후 사용
if (typeof data === "object" && data !== null) {
  // 타입 가드로 안전하게 처리
  if ("type" in data && "payload" in data) {
    // 이제 사용 가능
  }
}
```

**실전 게임 서버 예제:**

```typescript
// 클라이언트 패킷 처리
interface GamePacket {
  type: string;
  playerId: string;
  data: unknown;  // 패킷 타입에 따라 다른 구조
}

function handlePacket(packet: GamePacket): void {
  switch (packet.type) {
    case "move":
      // 타입 가드로 검증
      if (isMoveData(packet.data)) {
        processMove(packet.playerId, packet.data);
      }
      break;
    
    case "attack":
      if (isAttackData(packet.data)) {
        processAttack(packet.playerId, packet.data);
      }
      break;
  }
}

// 타입 가드 함수
function isMoveData(data: unknown): data is { x: number; y: number } {
  return (
    typeof data === "object" &&
    data !== null &&
    "x" in data &&
    "y" in data &&
    typeof (data as any).x === "number" &&
    typeof (data as any).y === "number"
  );
}
```

**규칙: `any`는 피하고, 정말 필요하면 `unknown`을 사용한다.**


## 2.2 객체와 인터페이스
게임 서버의 핵심은 데이터 모델링이다. Player, Monster, Item, Quest... 이런 복잡한 객체들을 TypeScript로 어떻게 정의할까?

### 게임 엔티티 모델링 예제

**Player 엔티티:**

```typescript
// Player 인터페이스 정의
interface Player {
  id: string;
  name: string;
  level: number;
  experience: number;
  health: number;
  maxHealth: number;
  mana: number;
  maxMana: number;
  position: {
    x: number;
    y: number;
    mapId: string;
  };
  inventory: Item[];
  guild: Guild | null;
  status: PlayerStatus;
}

// 플레이어 상태를 명확하게 정의
type PlayerStatus = "online" | "offline" | "in_combat" | "in_trade";

// 아이템 인터페이스
interface Item {
  id: string;
  name: string;
  type: "weapon" | "armor" | "consumable" | "quest_item";
  quantity: number;
  rarity: "common" | "rare" | "epic" | "legendary";
}

// 길드 인터페이스
interface Guild {
  id: string;
  name: string;
  level: number;
  memberCount: number;
}
```

이렇게 정의하면, VSCode에서 플레이어 객체를 다룰 때 모든 속성이 자동완성된다. 오타도 즉시 발견된다.

```typescript
function displayPlayerInfo(player: Player): void {
  console.log(`Name: ${player.name}`);
  console.log(`Level: ${player.level}`);
  console.log(`HP: ${player.health}/${player.maxHealth}`);
  console.log(`Position: (${player.position.x}, ${player.position.y})`);
  console.log(`Guild: ${player.guild?.name ?? "None"}`);
  
  // 오타 시 즉시 에러
  console.log(player.helth);  // ❌ Property 'helth' does not exist
}
```

**Monster 엔티티:**

```typescript
interface Monster {
  id: string;
  templateId: string;  // 몬스터 종류 식별
  name: string;
  level: number;
  health: number;
  maxHealth: number;
  attackPower: number;
  defense: number;
  position: {
    x: number;
    y: number;
    mapId: string;
  };
  aiType: "aggressive" | "passive" | "defensive";
  dropTable: DropItem[];
  respawnTime: number;  // 초 단위
}

interface DropItem {
  itemId: string;
  dropRate: number;  // 0.0 ~ 1.0
  minQuantity: number;
  maxQuantity: number;
}
```

### interface vs type - 실전 선택 기준
TypeScript에는 타입을 정의하는 두 가지 방법이 있습니다: `interface`와 `type`.

```typescript
// interface 방식
interface Player {
  id: string;
  name: string;
}

// type 방식
type Player = {
  id: string;
  name: string;
};
```

**언제 무엇을 쓸까?**  
대부분의 경우 둘 다 사용 가능하지만, 다음 가이드라인을 따르자:

**interface를 사용하는 경우:**
- 객체의 형태를 정의할 때 (Player, Monster, Item 등)
- 나중에 확장할 가능성이 있을 때
- 클래스와 함께 사용할 때

```typescript
// interface는 확장 가능
interface BaseEntity {
  id: string;
  createdAt: Date;
}

interface Player extends BaseEntity {
  name: string;
  level: number;
}

// 같은 이름으로 선언 병합도 가능 (드물게 사용)
interface Player {
  lastLoginAt: Date;
}
// 자동으로 병합됨
```

**type을 사용하는 경우:**
- 유니온 타입이나 튜플을 정의할 때
- 복잡한 타입 조작이 필요할 때
- 타입 별칭(alias)이 필요할 때

```typescript
// 유니온 타입
type EntityType = "player" | "monster" | "npc";

// 튜플
type Position3D = [number, number, number];

// 함수 타입
type DamageCalculator = (attacker: Player, target: Monster) => number;

// 유틸리티 타입과 조합
type PartialPlayer = Partial<Player>;
```

**게임 서버 실전 권장사항:**

```typescript
// ✅ 게임 엔티티는 interface
interface Player { /* ... */ }
interface Monster { /* ... */ }
interface Item { /* ... */ }

// ✅ 상태, 액션 타입은 type
type GameAction = "move" | "attack" | "use_item" | "trade";
type CombatResult = {
  damage: number;
  isCritical: boolean;
  targetDefeated: boolean;
};

// ✅ 함수 시그니처는 type
type PacketHandler = (playerId: string, data: unknown) => void;
```

### Optional Properties (?) 와 Readonly
모든 속성이 필수는 아니다. 선택적 속성과 불변 속성을 정의하는 방법이다.

```typescript
interface Player {
  // 필수 속성
  id: string;
  name: string;
  level: number;
  
  // 선택적 속성 (있을 수도, 없을 수도)
  guild?: Guild;
  mentor?: Player;
  title?: string;
  
  // 읽기 전용 (생성 후 변경 불가)
  readonly accountId: string;
  readonly createdAt: Date;
}

function createPlayer(name: string, accountId: string): Player {
  return {
    id: `player_${Date.now()}`,
    name: name,
    level: 1,
    accountId: accountId,  // 한 번 설정되면 변경 불가
    createdAt: new Date(),
    // guild는 선택사항이므로 생략 가능
  };
}

const player = createPlayer("Hero", "acc_123");
player.level = 2;  // ✅ OK
player.guild = someGuild;  // ✅ OK

player.accountId = "acc_456";  // ❌ 에러! Cannot assign to 'accountId' because it is a read-only property
```

**실전 활용:**

```typescript
// 게임 설정 객체 - 런타임에 변경되면 안 됨
interface GameConfig {
  readonly maxPlayers: number;
  readonly mapSize: number;
  readonly tickRate: number;
}

const config: GameConfig = {
  maxPlayers: 1000,
  mapSize: 2048,
  tickRate: 60
};

// config.maxPlayers = 2000;  // ❌ 에러!

// 퀘스트 정의 - 완료 여부는 선택적
interface Quest {
  id: string;
  name: string;
  description: string;
  requiredLevel: number;
  rewards: Item[];
  completedAt?: Date;  // 완료 전에는 없음
}
```


## 2.3 배열과 튜플

### 배열 타입
게임 서버에서 배열은 필수이다. 인벤토리, 플레이어 목록, 스킬 리스트 등 모든 곳에 사용된다.

```typescript
// 배열 선언 두 가지 방식
let items: Item[] = [];
let playerIds: Array<string> = [];

// 빈 배열로 초기화
const inventory: Item[] = [];

// 배열 메서드 사용
inventory.push(newItem);
const index = inventory.findIndex(item => item.id === "sword_01");
const weapons = inventory.filter(item => item.type === "weapon");
```

**인벤토리 시스템 구현 예제:**

```typescript
interface InventoryItem {
  item: Item;
  slot: number;
  quantity: number;
}

class Inventory {
  private items: InventoryItem[] = [];
  private readonly maxSlots: number = 30;
  
  addItem(item: Item, quantity: number): boolean {
    // 같은 아이템이 이미 있는지 확인
    const existing = this.items.find(inv => inv.item.id === item.id);
    
    if (existing) {
      existing.quantity += quantity;
      return true;
    }
    
    // 빈 슬롯이 있는지 확인
    if (this.items.length >= this.maxSlots) {
      return false;  // 인벤토리 가득 찼음
    }
    
    // 새 슬롯에 추가
    this.items.push({
      item: item,
      slot: this.items.length,
      quantity: quantity
    });
    
    return true;
  }
  
  removeItem(itemId: string, quantity: number): boolean {
    const index = this.items.findIndex(inv => inv.item.id === itemId);
    
    if (index === -1) {
      return false;  // 아이템 없음
    }
    
    const inventoryItem = this.items[index];
    
    if (inventoryItem.quantity < quantity) {
      return false;  // 수량 부족
    }
    
    inventoryItem.quantity -= quantity;
    
    // 수량이 0이 되면 제거
    if (inventoryItem.quantity === 0) {
      this.items.splice(index, 1);
      this.reindexSlots();
    }
    
    return true;
  }
  
  private reindexSlots(): void {
    this.items.forEach((item, index) => {
      item.slot = index;
    });
  }
  
  getItems(): readonly InventoryItem[] {
    return this.items;  // 읽기 전용으로 반환
  }
}
```

### 튜플 (Tuple)
튜플은 고정된 길이와 타입 순서를 가진 배열이다. 게임 서버에서는 좌표, RGB 색상, 범위 등에 사용된다.

```typescript
// 2D 좌표
type Position2D = [number, number];
const playerPos: Position2D = [128, 256];

// 3D 좌표
type Position3D = [number, number, number];
const monsterPos: Position3D = [128, 256, 10];

// 범위 (최소, 최대)
type Range = [number, number];
const damageRange: Range = [10, 20];
const levelRange: Range = [1, 99];

// RGB 색상
type RGB = [number, number, number];
const red: RGB = [255, 0, 0];

// 튜플 사용 예제
function calculateDistance(pos1: Position2D, pos2: Position2D): number {
  const [x1, y1] = pos1;  // 구조 분해
  const [x2, y2] = pos2;
  
  const dx = x2 - x1;
  const dy = y2 - y1;
  
  return Math.sqrt(dx * dx + dy * dy);
}

const distance = calculateDistance([0, 0], [3, 4]);  // 5
```

**실전 예제: 데미지 계산**

```typescript
// 데미지는 최소~최대 범위
interface Weapon {
  name: string;
  damageRange: [number, number];  // [min, max]
  criticalRate: number;
}

function calculateDamage(weapon: Weapon): number {
  const [minDamage, maxDamage] = weapon.damageRange;
  const baseDamage = Math.random() * (maxDamage - minDamage) + minDamage;
  
  // 크리티컬 체크
  const isCritical = Math.random() < weapon.criticalRate;
  
  return isCritical ? baseDamage * 2 : baseDamage;
}

const sword: Weapon = {
  name: "Excalibur",
  damageRange: [50, 100],
  criticalRate: 0.15
};

const damage = calculateDamage(sword);
```


## 2.4 유니온과 리터럴 타입

### 리터럴 타입: 정확한 값 지정
리터럴 타입은 특정 값만 허용한다. 게임 상태 관리에 매우 유용하다.

```typescript
// 문자열 리터럴 타입
type GameStatus = "lobby" | "playing" | "paused" | "finished";

let status: GameStatus = "lobby";  // ✅ OK
status = "playing";  // ✅ OK
status = "running";  // ❌ 에러! Type '"running"' is not assignable

// 숫자 리터럴 타입
type DiceRoll = 1 | 2 | 3 | 4 | 5 | 6;

function rollDice(): DiceRoll {
  return (Math.floor(Math.random() * 6) + 1) as DiceRoll;
}
```

**게임 상태 관리 실전 예제:**

```typescript
type PlayerState = "idle" | "moving" | "attacking" | "casting" | "dead";
type Direction = "north" | "south" | "east" | "west";

interface Player {
  id: string;
  name: string;
  state: PlayerState;
  direction: Direction;
}

function canMove(player: Player): boolean {
  // 특정 상태에서만 이동 가능
  return player.state === "idle" || player.state === "moving";
}

function changeState(player: Player, newState: PlayerState): void {
  // 상태 전환 로직
  const validTransitions: Record<PlayerState, PlayerState[]> = {
    idle: ["moving", "attacking", "casting"],
    moving: ["idle", "attacking"],
    attacking: ["idle", "dead"],
    casting: ["idle", "dead"],
    dead: ["idle"]  // 부활
  };
  
  if (validTransitions[player.state].includes(newState)) {
    player.state = newState;
    console.log(`${player.name}: ${player.state} → ${newState}`);
  } else {
    console.log(`Invalid state transition: ${player.state} → ${newState}`);
  }
}
```

### 유니온 타입: 여러 타입 중 하나
유니온 타입은 값이 여러 타입 중 하나일 수 있음을 나타낸다.

```typescript
// 숫자 또는 문자열
type ID = string | number;

let playerId: ID = "player_123";  // ✅ OK
playerId = 456;  // ✅ OK
playerId = true;  // ❌ 에러!

// 여러 객체 타입의 유니온
type Entity = Player | Monster | NPC;

function getEntityPosition(entity: Entity): Position2D {
  return [entity.position.x, entity.position.y];
}
```

**실전 예제: 다양한 패킷 타입**

```typescript
interface MovePacket {
  type: "move";
  x: number;
  y: number;
}

interface AttackPacket {
  type: "attack";
  targetId: string;
}

interface ChatPacket {
  type: "chat";
  message: string;
  channel: "global" | "guild" | "whisper";
}

interface ItemUsePacket {
  type: "use_item";
  itemId: string;
  targetId?: string;
}

// 모든 패킷 타입의 유니온
type GamePacket = MovePacket | AttackPacket | ChatPacket | ItemUsePacket;

// 패킷 처리 함수
function handlePacket(playerId: string, packet: GamePacket): void {
  // type 속성으로 구분
  switch (packet.type) {
    case "move":
      console.log(`Player ${playerId} moves to (${packet.x}, ${packet.y})`);
      break;
    
    case "attack":
      console.log(`Player ${playerId} attacks ${packet.targetId}`);
      break;
    
    case "chat":
      console.log(`[${packet.channel}] ${playerId}: ${packet.message}`);
      break;
    
    case "use_item":
      console.log(`Player ${playerId} uses item ${packet.itemId}`);
      break;
  }
}
```

### 타입 좁히기 (Type Narrowing)
유니온 타입을 사용할 때 TypeScript는 자동으로 타입을 좁혀준다.

```typescript
function processValue(value: string | number): string {
  // typeof로 타입 체크
  if (typeof value === "string") {
    // 이 블록 안에서는 value가 string으로 좁혀짐
    return value.toUpperCase();
  } else {
    // 이 블록 안에서는 value가 number로 좁혀짐
    return value.toFixed(2);
  }
}

// 객체 타입 판별
function isPlayer(entity: Entity): entity is Player {
  return "inventory" in entity;
}

function processEntity(entity: Entity): void {
  if (isPlayer(entity)) {
    // 이 안에서는 entity가 Player 타입
    console.log(`Player: ${entity.name}, Inventory: ${entity.inventory.length}`);
  } else {
    console.log(`Not a player`);
  }
}
```

**Discriminated Union (판별 유니온):**  
가장 강력한 패턴 중 하나이다. 공통 속성(보통 `type`)으로 타입을 판별한다.

```typescript
// 모든 타입이 공통 'type' 속성을 가짐
type GameEvent =
  | { type: "player_joined"; playerId: string; name: string }
  | { type: "player_left"; playerId: string }
  | { type: "damage_dealt"; attackerId: string; targetId: string; damage: number }
  | { type: "item_dropped"; itemId: string; x: number; y: number }
  | { type: "level_up"; playerId: string; newLevel: number };

function handleGameEvent(event: GameEvent): void {
  switch (event.type) {
    case "player_joined":
      // TypeScript가 자동으로 타입 추론
      console.log(`${event.name} (${event.playerId}) joined the game`);
      break;
    
    case "player_left":
      console.log(`Player ${event.playerId} left`);
      break;
    
    case "damage_dealt":
      console.log(`${event.attackerId} dealt ${event.damage} damage to ${event.targetId}`);
      break;
    
    case "item_dropped":
      console.log(`Item ${event.itemId} dropped at (${event.x}, ${event.y})`);
      break;
    
    case "level_up":
      console.log(`Player ${event.playerId} reached level ${event.newLevel}!`);
      break;
  }
}
```

이 패턴은 게임 서버의 이벤트 시스템, 패킷 처리, 상태 머신 등에서 매우 자주 사용된다.


## 🎯 Chapter 2 실전 종합 예제
지금까지 배운 내용을 모두 활용한 미니 전투 시스템을 만들어보자.

```typescript
// ========== 타입 정의 ==========

// 엔티티 기본 인터페이스
interface BaseEntity {
  id: string;
  name: string;
  level: number;
  health: number;
  maxHealth: number;
  position: [number, number];  // 튜플
}

// 플레이어
interface Player extends BaseEntity {
  experience: number;
  inventory: Item[];
  status: "alive" | "dead";  // 리터럴 타입
}

// 몬스터
interface Monster extends BaseEntity {
  attackPower: number;
  defense: number;
  experienceReward: number;
  status: "alive" | "dead";
}

// 아이템
interface Item {
  id: string;
  name: string;
  type: "weapon" | "potion" | "armor";
}

// 전투 결과
interface CombatResult {
  attacker: string;
  target: string;
  damage: number;
  targetDefeated: boolean;
  experienceGained?: number;  // 옵셔널
}

// ========== 전투 시스템 ==========

class CombatSystem {
  // 데미지 계산
  calculateDamage(attacker: Player | Monster, target: Player | Monster): number {
    const baseDamage = this.getAttackPower(attacker);
    const defense = this.getDefense(target);
    
    // 방어력 계산 (최소 1 데미지는 보장)
    const damage = Math.max(1, baseDamage - defense);
    
    // 랜덤 요소 추가 (±20%)
    const variance = 0.8 + Math.random() * 0.4;
    
    return Math.floor(damage * variance);
  }
  
  // 공격력 가져오기
  private getAttackPower(entity: Player | Monster): number {
    if (this.isMonster(entity)) {
      return entity.attackPower;
    } else {
      // 플레이어는 레벨 기반 공격력
      return 10 + entity.level * 2;
    }
  }
  
  // 방어력 가져오기
  private getDefense(entity: Player | Monster): number {
    if (this.isMonster(entity)) {
      return entity.defense;
    } else {
      return entity.level;
    }
  }
  
  // 타입 가드: Monster 판별
  private isMonster(entity: Player | Monster): entity is Monster {
    return "attackPower" in entity;
  }
  
  // 전투 실행
  executeCombat(attacker: Player, target: Monster): CombatResult {
    const damage = this.calculateDamage(attacker, target);
    
    // 데미지 적용
    target.health -= damage;
    
    if (target.health <= 0) {
      target.health = 0;
      target.status = "dead";
    }
    
    const result: CombatResult = {
      attacker: attacker.name,
      target: target.name,
      damage: damage,
      targetDefeated: target.status === "dead"
    };
    
    // 몬스터 처치 시 경험치 획득
    if (result.targetDefeated) {
      attacker.experience += target.experienceReward;
      result.experienceGained = target.experienceReward;
      
      this.checkLevelUp(attacker);
    }
    
    return result;
  }
  
  // 레벨업 체크
  private checkLevelUp(player: Player): void {
    const requiredExp = player.level * 100;
    
    if (player.experience >= requiredExp) {
      player.level++;
      player.experience -= requiredExp;
      player.maxHealth += 10;
      player.health = player.maxHealth;
      
      console.log(`🎉 ${player.name} leveled up to ${player.level}!`);
    }
  }
}

// ========== 게임 시뮬레이션 ==========

function runCombatSimulation(): void {
  const player: Player = {
    id: "player_001",
    name: "Hero",
    level: 5,
    health: 100,
    maxHealth: 100,
    experience: 0,
    inventory: [],
    position: [0, 0],
    status: "alive"
  };
  
  const goblin: Monster = {
    id: "monster_001",
    name: "Goblin",
    level: 3,
    health: 50,
    maxHealth: 50,
    attackPower: 15,
    defense: 3,
    experienceReward: 50,
    position: [10, 10],
    status: "alive"
  };
  
  const combat = new CombatSystem();
  
  console.log("⚔️  Combat Start!");
  console.log(`${player.name} (Lv.${player.level}) vs ${goblin.name} (Lv.${goblin.level})`);
  console.log("=".repeat(50));
  
  let turn = 1;
  
  // 전투 루프
  while (player.status === "alive" && goblin.status === "alive") {
    console.log(`\n--- Turn ${turn} ---`);
    
    // 플레이어 공격
    const result = combat.executeCombat(player, goblin);
    console.log(`${result.attacker} attacks ${result.target} for ${result.damage} damage!`);
    console.log(`${goblin.name} HP: ${goblin.health}/${goblin.maxHealth}`);
    
    if (result.targetDefeated) {
      console.log(`\n💀 ${goblin.name} has been defeated!`);
      console.log(`🌟 ${player.name} gained ${result.experienceGained} experience!`);
      break;
    }
    
    turn++;
    
    // 실제 게임에서는 몬스터도 반격하겠지만, 예제는 간단하게
  }
  
  console.log("\n" + "=".repeat(50));
  console.log(`Final Status: ${player.name} - HP: ${player.health}/${player.maxHealth}, Lv: ${player.level}, Exp: ${player.experience}`);
}

// 실행
runCombatSimulation();
```

**실행 결과:**

```
⚔️  Combat Start!
Hero (Lv.5) vs Goblin (Lv.3)
==================================================

--- Turn 1 ---
Hero attacks Goblin for 18 damage!
Goblin HP: 32/50

--- Turn 2 ---
Hero attacks Goblin for 15 damage!
Goblin HP: 17/50

--- Turn 3 ---
Hero attacks Goblin for 19 damage!
Goblin HP: 0/50

💀 Goblin has been defeated!
🌟 Hero gained 50 experience!

==================================================
Final Status: Hero - HP: 100/100, Lv: 5, Exp: 50
```

## 🎉 Chapter 2 완료!
축하한다! TypeScript 타입 시스템의 핵심을 모두 학습했다.

**지금까지 배운 것:**
- ✅ 기본 타입과 타입 추론
- ✅ null/undefined 안전한 처리
- ✅ any vs unknown
- ✅ 인터페이스로 게임 엔티티 모델링
- ✅ interface vs type 선택 기준
- ✅ Optional과 Readonly
- ✅ 배열과 튜플
- ✅ 리터럴 타입과 유니온 타입
- ✅ 타입 좁히기 (Type Narrowing)
- ✅ 실전 전투 시스템 구현


**다음 Chapter 미리보기:**   
Chapter 3에서는 함수 타입과 제네릭을 학습한다. 패킷 핸들러, 데이터베이스 쿼리, 비동기 처리 등 게임 서버의 핵심 기능을 타입 안전하게 구현하는 방법을 배운다. 특히 제네릭은 처음에는 어렵게 느껴질 수 있지만, 한 번 이해하면 코드 재사용성이 엄청나게 향상된다.  
  


# Chapter 3: 함수와 제네릭, 비동기

## 3.1 함수 타입 정의
함수는 게임 서버의 핵심이다. 패킷 처리, 데이터 검증, 로직 실행... 모든 것이 함수로 이루어진다.   TypeScript에서 함수를 타입 안전하게 작성하는 방법을 배워보자.  
  
### 매개변수와 반환값 타입
JavaScript에서는 함수의 매개변수와 반환값이 무엇인지 실행 전까지 알 수 없다. TypeScript는 이를 명확하게 정의한다.

```typescript
// 기본 함수 타입 정의
function calculateDamage(attack: number, defense: number): number {
  return Math.max(0, attack - defense);
}

// 반환값이 없는 함수 (void)
function logMessage(message: string): void {
  console.log(`[LOG] ${message}`);
}

// 화살표 함수도 동일
const addNumbers = (a: number, b: number): number => {
  return a + b;
};

// 간단한 화살표 함수 (타입 추론)
const multiply = (a: number, b: number) => a * b;  // 반환 타입 자동 추론
```

**실전 예제: 플레이어 검증**

```typescript
// 플레이어가 특정 행동을 할 수 있는지 검증
function canPlayerAttack(player: Player): boolean {
  if (player.status !== "alive") {
    return false;
  }
  
  if (player.health <= 0) {
    return false;
  }
  
  // 쿨다운 체크 등 추가 로직...
  return true;
}

// 경험치 계산
function calculateRequiredExp(level: number): number {
  return level * 100 + Math.floor(level * level * 0.5);
}

// 거리 계산
function getDistance(pos1: [number, number], pos2: [number, number]): number {
  const [x1, y1] = pos1;
  const [x2, y2] = pos2;
  const dx = x2 - x1;
  const dy = y2 - y1;
  return Math.sqrt(dx * dx + dy * dy);
}
```

### 옵셔널 파라미터 vs 기본값
모든 매개변수가 필수는 아니다. 선택적 매개변수와 기본값을 설정하는 방법이다.

```typescript
// 옵셔널 파라미터 (?)
function createMonster(
  name: string,
  level: number,
  boss?: boolean  // 선택사항
): Monster {
  return {
    id: `monster_${Date.now()}`,
    templateId: name.toLowerCase(),
    name: name,
    level: level,
    health: boss ? level * 100 : level * 50,  // boss가 undefined일 수 있음
    maxHealth: boss ? level * 100 : level * 50,
    attackPower: level * 5,
    defense: level * 2,
    position: { x: 0, y: 0, mapId: "default" },
    aiType: "aggressive",
    dropTable: [],
    respawnTime: 60
  };
}

// 사용
const goblin = createMonster("Goblin", 5);  // boss 생략 가능
const dragon = createMonster("Dragon", 50, true);  // boss 지정

// 기본값 사용 (더 권장됨)
function createMonsterWithDefaults(
  name: string,
  level: number,
  boss: boolean = false  // 기본값
): Monster {
  return {
    id: `monster_${Date.now()}`,
    templateId: name.toLowerCase(),
    name: name,
    level: level,
    health: boss ? level * 100 : level * 50,
    maxHealth: boss ? level * 100 : level * 50,
    attackPower: level * 5,
    defense: level * 2,
    position: { x: 0, y: 0, mapId: "default" },
    aiType: "aggressive",
    dropTable: [],
    respawnTime: 60
  };
}

// boss가 false로 자동 설정됨
const slime = createMonsterWithDefaults("Slime", 3);
```

**여러 옵셔널 파라미터:**

```typescript
// 아이템 생성 함수
function createItem(
  name: string,
  type: "weapon" | "armor" | "consumable",
  rarity: "common" | "rare" | "epic" | "legendary" = "common",
  quantity: number = 1,
  enchantLevel?: number  // 옵셔널
): Item & { enchantLevel?: number } {
  return {
    id: `item_${Date.now()}`,
    name: name,
    type: type,
    rarity: rarity,
    quantity: quantity,
    enchantLevel: enchantLevel
  };
}

// 다양한 호출 방식
const potion = createItem("Health Potion", "consumable");
const sword = createItem("Iron Sword", "weapon", "common", 1);
const epicSword = createItem("Excalibur", "weapon", "legendary", 1, 10);
```

**옵셔널 vs 기본값 선택 기준:**

```typescript
// ✅ 기본값 사용: 일반적인 경우
function dealDamage(target: Player, amount: number, isCritical: boolean = false): void {
  const finalDamage = isCritical ? amount * 2 : amount;
  target.health -= finalDamage;
}

// ✅ 옵셔널 사용: 값이 없는 것과 있는 것이 의미가 다를 때
function sendMessage(message: string, targetId?: string): void {
  if (targetId) {
    // 특정 플레이어에게만
    console.log(`To ${targetId}: ${message}`);
  } else {
    // 전체 브로드캐스트
    console.log(`[ALL]: ${message}`);
  }
}
```

### 콜백 함수 타입 정의
게임 서버에서 콜백은 매우 자주 사용된다. 비동기 처리, 이벤트 핸들러, 타이머 등...

```typescript
// 콜백 타입 정의
type Callback = () => void;
type ErrorCallback = (error: Error) => void;
type DataCallback<T> = (data: T) => void;

// 간단한 타이머
function executeAfterDelay(callback: Callback, delayMs: number): void {
  setTimeout(callback, delayMs);
}

// 사용
executeAfterDelay(() => {
  console.log("3 seconds passed!");
}, 3000);

// 에러 처리 콜백
function loadPlayerData(
  playerId: string,
  onSuccess: (player: Player) => void,
  onError: (error: Error) => void
): void {
  // 데이터베이스에서 플레이어 로드 시뮬레이션
  setTimeout(() => {
    if (playerId.startsWith("player_")) {
      const player: Player = {
        id: playerId,
        name: "Test Player",
        level: 1,
        experience: 0,
        health: 100,
        maxHealth: 100,
        mana: 50,
        maxMana: 50,
        position: { x: 0, y: 0, mapId: "spawn" },
        inventory: [],
        guild: null,
        status: "online"
      };
      onSuccess(player);
    } else {
      onError(new Error("Invalid player ID"));
    }
  }, 100);
}

// 사용
loadPlayerData(
  "player_123",
  (player) => {
    console.log(`Loaded: ${player.name}`);
  },
  (error) => {
    console.error(`Failed: ${error.message}`);
  }
);
```

**제네릭 콜백 (다음 섹션에서 자세히):**

```typescript
// 다양한 타입의 데이터를 처리하는 콜백
type GenericCallback<T> = (data: T) => void;

function processData<T>(data: T, callback: GenericCallback<T>): void {
  // 처리 로직...
  callback(data);
}

// 타입에 맞춰 자동으로 추론
processData({ x: 10, y: 20 }, (pos) => {
  console.log(pos.x, pos.y);  // pos는 자동으로 { x: number, y: number }
});
```


## 3.2 제네릭의 힘
제네릭(Generics)은 TypeScript의 가장 강력한 기능 중 하나이다. 처음에는 복잡해 보이지만, 이해하면 코드 재사용성이 엄청나게 향상된다.

### 왜 제네릭이 필요한가? - 실전 문제 상황
게임 서버에서 다양한 타입의 패킷을 처리한다고 가정해보자.

```typescript
// ❌ 제네릭 없이 - 각 타입마다 함수를 만들어야 함
interface MovePacket {
  type: "move";
  x: number;
  y: number;
}

interface AttackPacket {
  type: "attack";
  targetId: string;
}

function handleMovePacket(packet: MovePacket): void {
  console.log(`Move: ${packet.x}, ${packet.y}`);
}

function handleAttackPacket(packet: AttackPacket): void {
  console.log(`Attack: ${packet.targetId}`);
}

// 패킷 타입마다 함수를 만들어야 함... 😫
```

이 방식은 유지보수가 어렵다. 새로운 패킷 타입이 추가될 때마다 새 함수를 만들어야 한다.  

```typescript
// ✅ 제네릭 사용 - 하나의 함수로 모든 타입 처리
interface Packet<T> {
  type: string;
  timestamp: number;
  data: T;
}

function handlePacket<T>(packet: Packet<T>): void {
  console.log(`[${packet.type}] at ${packet.timestamp}`);
  console.log("Data:", packet.data);
}

// 사용 - 타입 자동 추론!
const movePacket: Packet<{ x: number; y: number }> = {
  type: "move",
  timestamp: Date.now(),
  data: { x: 100, y: 200 }
};

handlePacket(movePacket);  // data의 타입이 { x: number; y: number }로 추론됨

const attackPacket: Packet<{ targetId: string }> = {
  type: "attack",
  timestamp: Date.now(),
  data: { targetId: "monster_001" }
};

handlePacket(attackPacket);  // data의 타입이 { targetId: string }로 추론됨
```

### 제네릭 기본 문법
제네릭은 `<T>`처럼 꺾쇠괄호 안에 타입 변수를 선언한다. `T`는 관례적으로 사용하는 이름이지만, 의미 있는 이름을 사용해도 된다.

```typescript
// 기본 제네릭 함수
function identity<T>(value: T): T {
  return value;
}

// 사용
const num = identity(42);        // T는 number로 추론
const str = identity("hello");   // T는 string으로 추론
const obj = identity({ x: 10 }); // T는 { x: number }로 추론

// 명시적으로 타입 지정도 가능
const result = identity<number>(42);
```

**배열을 다루는 제네릭 함수:**

```typescript
// 배열의 첫 번째 요소 반환
function getFirstElement<T>(array: T[]): T | undefined {
  return array[0];
}

const numbers = [1, 2, 3, 4, 5];
const first = getFirstElement(numbers);  // number | undefined

const players = [player1, player2, player3];
const firstPlayer = getFirstElement(players);  // Player | undefined

// 배열의 마지막 요소 반환
function getLastElement<T>(array: T[]): T | undefined {
  return array[array.length - 1];
}
```

### 패킷 핸들러 제네릭 구현
실제 게임 서버의 패킷 처리 시스템을 제네릭으로 구현해보자.

```typescript
// 패킷 기본 인터페이스
interface BasePacket {
  type: string;
  playerId: string;
  timestamp: number;
}

// 특정 패킷 타입들
interface MoveData {
  x: number;
  y: number;
  speed: number;
}

interface AttackData {
  targetId: string;
  skillId: string;
}

interface ChatData {
  message: string;
  channel: "global" | "guild" | "party";
}

// 제네릭 패킷 래퍼
interface GamePacket<T> extends BasePacket {
  data: T;
}

// 제네릭 패킷 핸들러
type PacketHandler<T> = (playerId: string, data: T) => void;

// 패킷 핸들러 매니저
class PacketHandlerManager {
  private handlers = new Map<string, PacketHandler<any>>();
  
  // 제네릭 등록 함수
  register<T>(packetType: string, handler: PacketHandler<T>): void {
    this.handlers.set(packetType, handler);
  }
  
  // 제네릭 처리 함수
  handle<T>(packet: GamePacket<T>): void {
    const handler = this.handlers.get(packet.type);
    
    if (handler) {
      handler(packet.playerId, packet.data);
    } else {
      console.warn(`No handler for packet type: ${packet.type}`);
    }
  }
}

// 사용 예제
const packetManager = new PacketHandlerManager();

// 핸들러 등록 - 타입 안전!
packetManager.register<MoveData>("move", (playerId, data) => {
  console.log(`${playerId} moves to (${data.x}, ${data.y}) at speed ${data.speed}`);
  // data는 MoveData 타입으로 추론됨
});

packetManager.register<AttackData>("attack", (playerId, data) => {
  console.log(`${playerId} attacks ${data.targetId} with skill ${data.skillId}`);
  // data는 AttackData 타입으로 추론됨
});

packetManager.register<ChatData>("chat", (playerId, data) => {
  console.log(`[${data.channel}] ${playerId}: ${data.message}`);
  // data는 ChatData 타입으로 추론됨
});

// 패킷 처리
const movePacket: GamePacket<MoveData> = {
  type: "move",
  playerId: "player_001",
  timestamp: Date.now(),
  data: { x: 100, y: 200, speed: 5 }
};

packetManager.handle(movePacket);  // ✅ 타입 안전!
```

### Constraint (extends) 활용
때로는 제네릭 타입에 제약을 걸어야 한다. "어떤 타입이든 되지만, 특정 속성은 반드시 있어야 한다"는 조건이다.

```typescript
// 기본 엔티티 인터페이스
interface Entity {
  id: string;
  position: { x: number; y: number };
}

// Entity를 확장하는 타입만 허용
function getEntityDistance<T extends Entity>(entity1: T, entity2: T): number {
  const dx = entity2.position.x - entity1.position.x;
  const dy = entity2.position.y - entity1.position.y;
  return Math.sqrt(dx * dx + dy * dy);
}

// 사용
interface Player extends Entity {
  name: string;
  level: number;
}

interface Monster extends Entity {
  health: number;
  attackPower: number;
}

const player: Player = {
  id: "p1",
  position: { x: 0, y: 0 },
  name: "Hero",
  level: 5
};

const monster: Monster = {
  id: "m1",
  position: { x: 10, y: 10 },
  health: 100,
  attackPower: 20
};

const distance = getEntityDistance(player, monster);  // ✅ OK

// ❌ Entity를 확장하지 않으면 에러
const item = { name: "Sword" };
// getEntityDistance(item, player);  // ❌ 에러!
```

**실전 예제: 데이터베이스 쿼리**

```typescript
// 모든 DB 모델은 id를 가져야 함
interface DBModel {
  id: string;
  createdAt: Date;
  updatedAt: Date;
}

// 제네릭 Repository 클래스
class Repository<T extends DBModel> {
  private data: Map<string, T> = new Map();
  
  // 저장
  save(item: T): void {
    item.updatedAt = new Date();
    this.data.set(item.id, item);
  }
  
  // ID로 찾기
  findById(id: string): T | undefined {
    return this.data.get(id);
  }
  
  // 모두 찾기
  findAll(): T[] {
    return Array.from(this.data.values());
  }
  
  // 조건으로 찾기
  findBy(predicate: (item: T) => boolean): T[] {
    return this.findAll().filter(predicate);
  }
  
  // 삭제
  delete(id: string): boolean {
    return this.data.delete(id);
  }
}

// 플레이어 모델
interface PlayerModel extends DBModel {
  name: string;
  level: number;
  experience: number;
}

// 길드 모델
interface GuildModel extends DBModel {
  name: string;
  memberCount: number;
  level: number;
}

// 사용
const playerRepo = new Repository<PlayerModel>();
const guildRepo = new Repository<GuildModel>();

// 플레이어 저장
playerRepo.save({
  id: "player_001",
  name: "Hero",
  level: 10,
  experience: 500,
  createdAt: new Date(),
  updatedAt: new Date()
});

// 타입 안전한 검색
const highLevelPlayers = playerRepo.findBy(p => p.level >= 10);
// p는 자동으로 PlayerModel 타입

const player = playerRepo.findById("player_001");
if (player) {
  console.log(player.name);  // ✅ 타입 안전
}
```

**여러 제네릭 타입 변수:**

```typescript
// 키-값 쌍
interface KeyValuePair<K, V> {
  key: K;
  value: V;
}

// 캐시 시스템
class Cache<K, V> {
  private storage = new Map<K, V>();
  
  set(key: K, value: V): void {
    this.storage.set(key, value);
  }
  
  get(key: K): V | undefined {
    return this.storage.get(key);
  }
  
  has(key: K): boolean {
    return this.storage.has(key);
  }
}

// 사용
const playerCache = new Cache<string, Player>();
playerCache.set("player_001", somePlayer);

const itemCache = new Cache<number, Item>();
itemCache.set(1001, someItem);
```


## 3.3 비동기 처리
게임 서버는 비동기 작업의 연속이다. 데이터베이스 쿼리, 외부 API 호출, 파일 읽기... 모두 비동기로 처리된다. TypeScript에서 비동기 코드를 타입 안전하게 작성하는 방법을 배워보자.

### Promise<T> 타입
Promise는 비동기 작업의 결과를 나타낸다. 제네릭 타입 `Promise<T>`로 결과값의 타입을 지정한다.

```typescript
// Promise를 반환하는 함수
function loadPlayerAsync(playerId: string): Promise<Player> {
  return new Promise((resolve, reject) => {
    setTimeout(() => {
      if (playerId.startsWith("player_")) {
        const player: Player = {
          id: playerId,
          name: "Async Player",
          level: 1,
          experience: 0,
          health: 100,
          maxHealth: 100,
          mana: 50,
          maxMana: 50,
          position: { x: 0, y: 0, mapId: "spawn" },
          inventory: [],
          guild: null,
          status: "online"
        };
        resolve(player);  // 성공
      } else {
        reject(new Error("Player not found"));  // 실패
      }
    }, 100);
  });
}

// Promise 사용 (.then/.catch)
loadPlayerAsync("player_123")
  .then((player) => {
    console.log(`Loaded: ${player.name}`);  // player는 Player 타입
  })
  .catch((error) => {
    console.error(`Error: ${error.message}`);
  });
```

### async/await 패턴
`async/await`는 비동기 코드를 동기 코드처럼 작성할 수 있게 해준다. 가독성이 훨씬 좋아진다.

```typescript
// async 함수는 자동으로 Promise를 반환
async function getPlayerLevel(playerId: string): Promise<number> {
  const player = await loadPlayerAsync(playerId);
  return player.level;
}

// async 함수 내에서 await 사용
async function processPlayerLogin(playerId: string): Promise<void> {
  try {
    console.log("Loading player...");
    const player = await loadPlayerAsync(playerId);
    
    console.log(`Welcome back, ${player.name}!`);
    console.log(`Your level: ${player.level}`);
    
    // 여러 비동기 작업을 순차적으로 실행
    await saveLoginTime(player.id);
    await updatePlayerStatus(player.id, "online");
    
    console.log("Login complete!");
  } catch (error) {
    console.error("Login failed:", error);
  }
}

// 보조 함수들
function saveLoginTime(playerId: string): Promise<void> {
  return new Promise((resolve) => {
    setTimeout(() => {
      console.log(`Login time saved for ${playerId}`);
      resolve();
    }, 50);
  });
}

function updatePlayerStatus(
  playerId: string,
  status: PlayerStatus
): Promise<void> {
  return new Promise((resolve) => {
    setTimeout(() => {
      console.log(`Status updated to ${status}`);
      resolve();
    }, 50);
  });
}
```

### 게임 서버 DB 쿼리 예제
실제 게임 서버의 데이터베이스 작업을 시뮬레이션해보자.

```typescript
// 데이터베이스 시뮬레이터
class GameDatabase {
  private players = new Map<string, Player>();
  private items = new Map<string, Item>();
  
  // 플레이어 로드 (비동기)
  async loadPlayer(playerId: string): Promise<Player> {
    // 실제로는 DB 쿼리
    return new Promise((resolve, reject) => {
      setTimeout(() => {
        const player = this.players.get(playerId);
        if (player) {
          resolve(player);
        } else {
          reject(new Error(`Player ${playerId} not found`));
        }
      }, 100);
    });
  }
  
  // 플레이어 저장 (비동기)
  async savePlayer(player: Player): Promise<void> {
    return new Promise((resolve) => {
      setTimeout(() => {
        this.players.set(player.id, player);
        console.log(`Player ${player.id} saved`);
        resolve();
      }, 50);
    });
  }
  
  // 인벤토리 아이템 로드
  async loadInventory(playerId: string): Promise<Item[]> {
    return new Promise((resolve) => {
      setTimeout(() => {
        // 실제로는 복잡한 JOIN 쿼리
        const items = Array.from(this.items.values())
          .filter(item => item.id.startsWith(playerId));
        resolve(items);
      }, 80);
    });
  }
  
  // 여러 플레이어 로드 (병렬)
  async loadMultiplePlayers(playerIds: string[]): Promise<Player[]> {
    // Promise.all로 병렬 실행
    const promises = playerIds.map(id => this.loadPlayer(id));
    return Promise.all(promises);
  }
  
  // 레벨별 플레이어 검색
  async findPlayersByLevel(minLevel: number, maxLevel: number): Promise<Player[]> {
    return new Promise((resolve) => {
      setTimeout(() => {
        const results = Array.from(this.players.values())
          .filter(p => p.level >= minLevel && p.level <= maxLevel);
        resolve(results);
      }, 120);
    });
  }
}

// 사용 예제
const db = new GameDatabase();

async function handlePlayerJoin(playerId: string): Promise<void> {
  try {
    console.log(`[1/3] Loading player data...`);
    const player = await db.loadPlayer(playerId);
    
    console.log(`[2/3] Loading inventory...`);
    const inventory = await db.loadInventory(playerId);
    player.inventory = inventory;
    
    console.log(`[3/3] Updating status...`);
    player.status = "online";
    await db.savePlayer(player);
    
    console.log(`✅ ${player.name} joined the game!`);
    console.log(`   Level: ${player.level}`);
    console.log(`   Inventory: ${inventory.length} items`);
  } catch (error) {
    console.error(`❌ Failed to load player:`, error);
  }
}
```

### 병렬 vs 순차 실행
비동기 작업을 효율적으로 처리하려면 병렬/순차 실행을 적절히 선택해야 한다.

```typescript
// ❌ 비효율: 순차 실행 (총 300ms)
async function loadPartyDataSequential(partyMemberIds: string[]): Promise<Player[]> {
  const players: Player[] = [];
  
  for (const id of partyMemberIds) {
    const player = await db.loadPlayer(id);  // 각각 100ms
    players.push(player);
  }
  
  return players;  // 3명이면 300ms 소요
}

// ✅ 효율적: 병렬 실행 (총 100ms)
async function loadPartyDataParallel(partyMemberIds: string[]): Promise<Player[]> {
  // Promise.all로 동시에 실행
  const promises = partyMemberIds.map(id => db.loadPlayer(id));
  return Promise.all(promises);  // 3명이어도 100ms 소요
}

// 실행 비교
async function comparePerformance(): Promise<void> {
  const partyIds = ["player_001", "player_002", "player_003"];
  
  console.time("Sequential");
  await loadPartyDataSequential(partyIds);
  console.timeEnd("Sequential");  // Sequential: ~300ms
  
  console.time("Parallel");
  await loadPartyDataParallel(partyIds);
  console.timeEnd("Parallel");    // Parallel: ~100ms
}
```

**실전 패턴: 일부만 병렬**

```typescript
// 플레이어 데이터 초기화
async function initializePlayer(playerId: string): Promise<Player> {
  // 1단계: 플레이어 기본 데이터 로드 (순차 필수)
  const player = await db.loadPlayer(playerId);
  
  // 2단계: 나머지 데이터 병렬 로드
  const [inventory, guild, quests] = await Promise.all([
    db.loadInventory(playerId),
    loadPlayerGuild(playerId),
    loadPlayerQuests(playerId)
  ]);
  
  // 3단계: 데이터 조합
  player.inventory = inventory;
  player.guild = guild;
  // player.quests = quests;
  
  return player;
}

async function loadPlayerGuild(playerId: string): Promise<Guild | null> {
  // 구현...
  return null;
}

async function loadPlayerQuests(playerId: string): Promise<any[]> {
  // 구현...
  return [];
}
```

### 에러 처리 패턴
비동기 코드에서 에러 처리는 매우 중요하다.

```typescript
// 타입 안전한 Result 패턴
type Result<T, E = Error> = 
  | { success: true; data: T }
  | { success: false; error: E };

async function loadPlayerSafe(playerId: string): Promise<Result<Player>> {
  try {
    const player = await db.loadPlayer(playerId);
    return { success: true, data: player };
  } catch (error) {
    return { 
      success: false, 
      error: error instanceof Error ? error : new Error(String(error))
    };
  }
}

// 사용
async function handlePlayerLogin2(playerId: string): Promise<void> {
  const result = await loadPlayerSafe(playerId);
  
  if (result.success) {
    console.log(`Loaded: ${result.data.name}`);
  } else {
    console.error(`Error: ${result.error.message}`);
  }
}

// 여러 작업의 Result 처리
async function loadMultiplePlayersSafe(
  playerIds: string[]
): Promise<Result<Player[]>> {
  try {
    const promises = playerIds.map(id => db.loadPlayer(id));
    const players = await Promise.all(promises);
    return { success: true, data: players };
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error : new Error(String(error))
    };
  }
}
```

---

## 🎯 Chapter 3 실전 종합 예제
지금까지 배운 함수, 제네릭, 비동기 처리를 모두 활용한 게임 서버 세션 매니저를 만들어보자.

```typescript
// ========== 타입 정의 ==========

interface Session {
  sessionId: string;
  playerId: string;
  connectedAt: Date;
  lastActivityAt: Date;
  status: "active" | "idle" | "disconnected";
}

interface PlayerData {
  player: Player;
  session: Session;
  inventory: Item[];
}

// 제네릭 이벤트 핸들러
type EventHandler<T> = (data: T) => void | Promise<void>;

// ========== 세션 매니저 ==========

class SessionManager {
  private sessions = new Map<string, Session>();
  private playerData = new Map<string, PlayerData>();
  private eventHandlers = new Map<string, EventHandler<any>[]>();
  
  // 제네릭 이벤트 등록
  on<T>(eventName: string, handler: EventHandler<T>): void {
    if (!this.eventHandlers.has(eventName)) {
      this.eventHandlers.set(eventName, []);
    }
    this.eventHandlers.get(eventName)!.push(handler);
  }
  
  // 제네릭 이벤트 발생
  private async emit<T>(eventName: string, data: T): Promise<void> {
    const handlers = this.eventHandlers.get(eventName) || [];
    
    for (const handler of handlers) {
      await handler(data);
    }
  }
  
  // 플레이어 접속
  async connectPlayer(playerId: string): Promise<Result<Session>> {
    try {
      // 1. 세션 생성
      const session: Session = {
        sessionId: `sess_${Date.now()}_${Math.random()}`,
        playerId: playerId,
        connectedAt: new Date(),
        lastActivityAt: new Date(),
        status: "active"
      };
      
      this.sessions.set(session.sessionId, session);
      
      // 2. 플레이어 데이터 로드 (비동기, 병렬)
      console.log(`Loading data for ${playerId}...`);
      const [player, inventory] = await Promise.all([
        this.loadPlayer(playerId),
        this.loadInventory(playerId)
      ]);
      
      // 3. 데이터 저장
      const playerData: PlayerData = {
        player: player,
        session: session,
        inventory: inventory
      };
      
      this.playerData.set(playerId, playerData);
      
      // 4. 이벤트 발생
      await this.emit("player_connected", {
        playerId: playerId,
        sessionId: session.sessionId,
        playerName: player.name
      });
      
      console.log(`✅ ${player.name} connected (${session.sessionId})`);
      
      return { success: true, data: session };
    } catch (error) {
      console.error(`❌ Failed to connect ${playerId}:`, error);
      return {
        success: false,
        error: error instanceof Error ? error : new Error(String(error))
      };
    }
  }
  
  // 플레이어 연결 해제
  async disconnectPlayer(playerId: string): Promise<void> {
    const playerData = this.playerData.get(playerId);
    
    if (!playerData) {
      console.warn(`Player ${playerId} not found`);
      return;
    }
    
    try {
      // 1. 상태 업데이트
      playerData.session.status = "disconnected";
      
      // 2. 데이터 저장
      await this.savePlayer(playerData.player);
      
      // 3. 정리
      this.sessions.delete(playerData.session.sessionId);
      this.playerData.delete(playerId);
      
      // 4. 이벤트 발생
      await this.emit("player_disconnected", {
        playerId: playerId,
        sessionId: playerData.session.sessionId
      });
      
      console.log(`👋 ${playerData.player.name} disconnected`);
    } catch (error) {
      console.error(`Error disconnecting ${playerId}:`, error);
    }
  }
  
  // 활동 업데이트
  updateActivity(playerId: string): void {
    const playerData = this.playerData.get(playerId);
    
    if (playerData) {
      playerData.session.lastActivityAt = new Date();
      playerData.session.status = "active";
    }
  }
  
  // 유휴 세션 체크
  async checkIdleSessions(idleTimeoutMs: number): Promise<void> {
    const now = Date.now();
    const idlePlayers: string[] = [];
    
    for (const [playerId, data] of this.playerData.entries()) {
      const idleTime = now - data.session.lastActivityAt.getTime();
      
      if (idleTime > idleTimeoutMs) {
        data.session.status = "idle";
        idlePlayers.push(playerId);
      }
    }
    
    if (idlePlayers.length > 0) {
      await this.emit("players_idle", { playerIds: idlePlayers });
      console.log(`⏰ ${idlePlayers.length} players are now idle`);
    }
  }
  
  // 플레이어 데이터 가져오기
  getPlayerData(playerId: string): PlayerData | undefined {
    return this.playerData.get(playerId);
  }
  
  // 접속 중인 플레이어 목록
  getActivePlayers(): Player[] {
    return Array.from(this.playerData.values())
      .filter(data => data.session.status === "active")
      .map(data => data.player);
  }
  
  // ========== Private 헬퍼 메서드 ==========
  
  private async loadPlayer(playerId: string): Promise<Player> {
    // DB 로드 시뮬레이션
    return new Promise((resolve) => {
      setTimeout(() => {
        resolve({
          id: playerId,
          name: `Player_${playerId.substring(7)}`,
          level: Math.floor(Math.random() * 50) + 1,
          experience: 0,
          health: 100,
          maxHealth: 100,
          mana: 50,
          maxMana: 50,
          position: { x: 0, y: 0, mapId: "town" },
          inventory: [],
          guild: null,
          status: "online"
        });
      }, 100);
    });
  }
  
  private async loadInventory(playerId: string): Promise<Item[]> {
    // DB 로드 시뮬레이션
    return new Promise((resolve) => {
      setTimeout(() => {
        resolve([
          { id: "item_1", name: "Sword", type: "weapon", quantity: 1, rarity: "common" },
          { id: "item_2", name: "Potion", type: "consumable", quantity: 5, rarity: "common" }
        ]);
      }, 80);
    });
  }
  
  private async savePlayer(player: Player): Promise<void> {
    // DB 저장 시뮬레이션
    return new Promise((resolve) => {
      setTimeout(() => {
        console.log(`  💾 Saved player data for ${player.name}`);
        resolve();
      }, 50);
    });
  }
}

// ========== 사용 예제 ==========

async function runSessionDemo(): Promise<void> {
  const sessionManager = new SessionManager();
  
  // 이벤트 핸들러 등록
  sessionManager.on<{ playerId: string; playerName: string }>(
    "player_connected",
    async (data) => {
      console.log(`📢 Event: ${data.playerName} has joined the server!`);
    }
  );
  
  sessionManager.on<{ playerId: string }>(
    "player_disconnected",
    async (data) => {
      console.log(`📢 Event: Player ${data.playerId} has left the server`);
    }
  );
  
  sessionManager.on<{ playerIds: string[] }>(
    "players_idle",
    async (data) => {
      console.log(`📢 Event: ${data.playerIds.length} players went idle`);
    }
  );
  
  console.log("=".repeat(60));
  console.log("Game Server Session Manager Demo");
  console.log("=".repeat(60));
  
  // 플레이어 3명 접속
  await sessionManager.connectPlayer("player_001");
  await sessionManager.connectPlayer("player_002");
  await sessionManager.connectPlayer("player_003");
  
  console.log("\n" + "-".repeat(60));
  console.log(`Active Players: ${sessionManager.getActivePlayers().length}`);
  console.log("-".repeat(60));
  
  // 활동 업데이트
  sessionManager.updateActivity("player_001");
  
  // 유휴 체크 (2초 이상 활동 없으면 유휴)
  await new Promise(resolve => setTimeout(resolve, 2100));
  await sessionManager.checkIdleSessions(2000);
  
  console.log("\n" + "-".repeat(60));
  
  // 플레이어 연결 해제
  await sessionManager.disconnectPlayer("player_001");
  
  console.log("\n" + "-".repeat(60));
  console.log(`Active Players: ${sessionManager.getActivePlayers().length}`);
  console.log("-".repeat(60));
}

// 실행
runSessionDemo();
```

**실행 결과:**

```
============================================================
Game Server Session Manager Demo
============================================================
Loading data for player_001...
✅ Player_001 connected (sess_1705123456789_0.123)
📢 Event: Player_001 has joined the server!
Loading data for player_002...
✅ Player_002 connected (sess_1705123456890_0.456)
📢 Event: Player_002 has joined the server!
Loading data for player_003...
✅ Player_003 connected (sess_1705123456991_0.789)
📢 Event: Player_003 has joined the server!

------------------------------------------------------------
Active Players: 3
------------------------------------------------------------
⏰ 2 players are now idle
📢 Event: 2 players went idle

------------------------------------------------------------
  💾 Saved player data for Player_001
👋 Player_001 disconnected
📢 Event: Player player_001 has left the server

------------------------------------------------------------
Active Players: 2
------------------------------------------------------------
```

---

## 🎉 Chapter 3 완료!
축하합니다! TypeScript의 함수와 제네릭을 완벽하게 마스터했다.

**지금까지 배운 것:**
- ✅ 함수 타입 정의 (매개변수, 반환값)
- ✅ 옵셔널 파라미터와 기본값
- ✅ 콜백 함수 타입
- ✅ 제네릭의 필요성과 기본 문법
- ✅ 제네릭 패킷 핸들러 구현
- ✅ Constraint (extends) 활용
- ✅ Promise와 async/await
- ✅ 병렬/순차 비동기 실행
- ✅ 타입 안전한 에러 처리
- ✅ 실전 세션 매니저 구현


**다음 Chapter 미리보기:**  
Chapter 4에서는 객체지향 프로그래밍을 TypeScript로 구현한다. 게임 엔티티 계층 구조, 접근 제어자, 추상 클래스, 인터페이스 구현 등을 배우며 실제 게임 서버의 아키텍처를 설계하는 방법을 익힌다.



# Chapter 4: 클래스와 OOP

## 4.1 클래스 기본
클래스는 객체지향 프로그래밍의 핵심이다. 게임 서버의 엔티티, 시스템, 매니저 등을 클래스로 구조화하면 코드 재사용성과 유지보수성이 크게 향상된다.

### 기본 클래스 문법

```typescript
// 기본 클래스 정의
class Player {
  // 속성 (properties)
  id: string;
  name: string;
  level: number;
  health: number;
  maxHealth: number;
  
  // 생성자 (constructor)
  constructor(id: string, name: string, level: number) {
    this.id = id;
    this.name = name;
    this.level = level;
    this.maxHealth = 100 + (level - 1) * 10;
    this.health = this.maxHealth;
  }
  
  // 메서드 (methods)
  takeDamage(amount: number): void {
    this.health -= amount;
    if (this.health < 0) {
      this.health = 0;
    }
    console.log(`${this.name} took ${amount} damage. HP: ${this.health}/${this.maxHealth}`);
  }
  
  heal(amount: number): void {
    this.health += amount;
    if (this.health > this.maxHealth) {
      this.health = this.maxHealth;
    }
    console.log(`${this.name} healed ${amount} HP. HP: ${this.health}/${this.maxHealth}`);
  }
  
  isAlive(): boolean {
    return this.health > 0;
  }
}

// 사용
const player = new Player("p001", "Hero", 5);
player.takeDamage(30);
player.heal(10);
console.log(`Is alive: ${player.isAlive()}`);
```

### 접근 제어자 (public, private, protected)
TypeScript는 세 가지 접근 제어자를 제공합니다. 이를 통해 클래스 내부 구현을 캡슐화하고 외부에서의 잘못된 접근을 방지할 수 있습니다.

```typescript
class BankAccount {
  // public: 어디서든 접근 가능 (기본값)
  public accountId: string;
  public ownerName: string;
  
  // private: 클래스 내부에서만 접근 가능
  private balance: number;
  private pin: string;
  
  // protected: 클래스 내부와 자식 클래스에서만 접근 가능
  protected transactionHistory: string[] = [];
  
  constructor(accountId: string, ownerName: string, initialBalance: number, pin: string) {
    this.accountId = accountId;
    this.ownerName = ownerName;
    this.balance = initialBalance;
    this.pin = pin;
  }
  
  // public 메서드로 안전하게 잔액 조회
  getBalance(inputPin: string): number | null {
    if (this.verifyPin(inputPin)) {
      return this.balance;
    }
    console.log("Invalid PIN");
    return null;
  }
  
  // private 메서드 - 외부에서 직접 호출 불가
  private verifyPin(inputPin: string): boolean {
    return this.pin === inputPin;
  }
  
  // protected 메서드 - 자식 클래스에서 사용 가능
  protected addTransaction(description: string): void {
    this.transactionHistory.push(`${new Date().toISOString()}: ${description}`);
  }
  
  deposit(amount: number): void {
    this.balance += amount;
    this.addTransaction(`Deposit: ${amount}`);
    console.log(`Deposited ${amount}. New balance: ${this.balance}`);
  }
}

// 사용
const account = new BankAccount("acc001", "John", 1000, "1234");
account.deposit(500);

// ✅ OK - public
console.log(account.ownerName);

// ❌ 에러 - private
// console.log(account.balance);  // Property 'balance' is private

// ❌ 에러 - protected
// console.log(account.transactionHistory);  // Property 'transactionHistory' is protected

// ✅ OK - public 메서드로 접근
const balance = account.getBalance("1234");
console.log(`Balance: ${balance}`);
```

**게임 서버 실전 예제:**

```typescript
class GameEntity {
  // public: 모두가 알아야 하는 정보
  public id: string;
  public name: string;
  public position: { x: number; y: number };
  
  // private: 내부 구현 세부사항
  private lastUpdateTime: number;
  private internalState: string;
  
  // protected: 자식 클래스에서 사용
  protected health: number;
  protected maxHealth: number;
  
  constructor(id: string, name: string) {
    this.id = id;
    this.name = name;
    this.position = { x: 0, y: 0 };
    this.health = 100;
    this.maxHealth = 100;
    this.lastUpdateTime = Date.now();
    this.internalState = "initialized";
  }
  
  // public API
  public getHealth(): number {
    return this.health;
  }
  
  public getHealthPercentage(): number {
    return (this.health / this.maxHealth) * 100;
  }
  
  // private: 내부에서만 사용
  private updateTimestamp(): void {
    this.lastUpdateTime = Date.now();
  }
  
  // protected: 자식 클래스에서 오버라이드 가능
  protected onHealthChanged(): void {
    this.updateTimestamp();
    console.log(`${this.name} health changed: ${this.health}/${this.maxHealth}`);
  }
  
  public takeDamage(amount: number): void {
    this.health -= amount;
    if (this.health < 0) {
      this.health = 0;
    }
    this.onHealthChanged();  // protected 메서드 호출
  }
}
```

### 생성자와 프로퍼티 초기화
TypeScript는 생성자 파라미터에서 바로 프로퍼티를 선언하고 초기화할 수 있는 간편 문법을 제공한다.

```typescript
// ❌ 반복적인 코드
class Player_Old {
  id: string;
  name: string;
  level: number;
  
  constructor(id: string, name: string, level: number) {
    this.id = id;
    this.name = name;
    this.level = level;
  }
}

// ✅ 간편한 문법
class Player_New {
  constructor(
    public id: string,
    public name: string,
    public level: number,
    private internalId: number = Math.random()  // private도 가능
  ) {
    // 자동으로 this.id = id 등이 실행됨
    // 추가 초기화 로직만 작성
  }
}

const player = new Player_New("p001", "Hero", 5);
console.log(player.id);     // ✅ OK
console.log(player.name);   // ✅ OK
// console.log(player.internalId);  // ❌ 에러 - private
```

**readonly 프로퍼티:**  
생성 후 변경되면 안 되는 값은 `readonly`로 지정한다.

```typescript
class GameConfig {
  constructor(
    public readonly serverId: string,
    public readonly maxPlayers: number,
    public readonly tickRate: number,
    public currentPlayers: number = 0  // 변경 가능
  ) {}
  
  addPlayer(): void {
    if (this.currentPlayers < this.maxPlayers) {
      this.currentPlayers++;
    }
  }
}

const config = new GameConfig("server_01", 1000, 60);
config.addPlayer();  // ✅ OK

// config.maxPlayers = 2000;  // ❌ 에러 - readonly
// config.serverId = "server_02";  // ❌ 에러 - readonly
```

**실전 예제: 몬스터 클래스**

```typescript
class Monster {
  private isDead: boolean = false;
  private respawnTimer: NodeJS.Timeout | null = null;
  
  constructor(
    public readonly templateId: string,
    public name: string,
    public level: number,
    private health: number,
    private readonly maxHealth: number,
    public readonly attackPower: number,
    public readonly experienceReward: number,
    private readonly respawnTimeSeconds: number
  ) {}
  
  // Getter로 private 값 읽기
  getHealth(): number {
    return this.health;
  }
  
  isAlive(): boolean {
    return !this.isDead;
  }
  
  takeDamage(amount: number): boolean {
    if (this.isDead) {
      return false;
    }
    
    this.health -= amount;
    console.log(`${this.name} takes ${amount} damage. HP: ${this.health}/${this.maxHealth}`);
    
    if (this.health <= 0) {
      this.die();
      return true;  // 죽음
    }
    
    return false;
  }
  
  private die(): void {
    this.isDead = true;
    this.health = 0;
    console.log(`💀 ${this.name} has been defeated!`);
    this.scheduleRespawn();
  }
  
  private scheduleRespawn(): void {
    console.log(`⏰ ${this.name} will respawn in ${this.respawnTimeSeconds} seconds`);
    
    this.respawnTimer = setTimeout(() => {
      this.respawn();
    }, this.respawnTimeSeconds * 1000);
  }
  
  private respawn(): void {
    this.isDead = false;
    this.health = this.maxHealth;
    console.log(`✨ ${this.name} has respawned!`);
  }
  
  // 정리 메서드
  destroy(): void {
    if (this.respawnTimer) {
      clearTimeout(this.respawnTimer);
    }
  }
}

// 사용
const goblin = new Monster(
  "goblin_warrior",
  "Goblin Warrior",
  5,
  100,  // health
  100,  // maxHealth
  15,   // attackPower
  50,   // experienceReward
  10    // respawnTimeSeconds
);

goblin.takeDamage(30);
goblin.takeDamage(80);  // 죽음, 10초 후 리스폰
```


## 4.2 상속과 다형성
상속은 코드 재사용의 핵심이다. 공통 기능을 부모 클래스에 정의하고, 특화된 기능은 자식 클래스에 구현한다.

### **기본 상속**

```typescript
// 기본 엔티티 클래스
class Entity {
  constructor(
    public id: string,
    public name: string,
    protected health: number,
    protected maxHealth: number,
    public position: { x: number; y: number }
  ) {}
  
  getHealth(): number {
    return this.health;
  }
  
  getMaxHealth(): number {
    return this.maxHealth;
  }
  
  takeDamage(amount: number): void {
    this.health -= amount;
    if (this.health < 0) {
      this.health = 0;
    }
  }
  
  isAlive(): boolean {
    return this.health > 0;
  }
  
  moveTo(x: number, y: number): void {
    this.position.x = x;
    this.position.y = y;
    console.log(`${this.name} moved to (${x}, ${y})`);
  }
}

// Player는 Entity를 상속
class Player extends Entity {
  constructor(
    id: string,
    name: string,
    health: number,
    maxHealth: number,
    public level: number,
    public experience: number,
    private inventory: Item[] = []
  ) {
    // super()로 부모 생성자 호출
    super(id, name, health, maxHealth, { x: 0, y: 0 });
  }
  
  // Player만의 메서드
  gainExperience(amount: number): void {
    this.experience += amount;
    console.log(`${this.name} gained ${amount} exp. Total: ${this.experience}`);
    
    const requiredExp = this.level * 100;
    if (this.experience >= requiredExp) {
      this.levelUp();
    }
  }
  
  private levelUp(): void {
    this.level++;
    this.experience = 0;
    this.maxHealth += 10;
    this.health = this.maxHealth;
    console.log(`🎉 ${this.name} leveled up to ${this.level}!`);
  }
  
  addItem(item: Item): void {
    this.inventory.push(item);
    console.log(`${this.name} obtained ${item.name}`);
  }
  
  getInventory(): readonly Item[] {
    return this.inventory;
  }
}

// Monster도 Entity를 상속
class Monster extends Entity {
  constructor(
    id: string,
    name: string,
    health: number,
    maxHealth: number,
    public level: number,
    private attackPower: number,
    private experienceReward: number
  ) {
    super(id, name, health, maxHealth, { x: 0, y: 0 });
  }
  
  // Monster만의 메서드
  getAttackPower(): number {
    return this.attackPower;
  }
  
  getExperienceReward(): number {
    return this.experienceReward;
  }
  
  // AI 행동
  performAI(): void {
    console.log(`${this.name} is thinking...`);
    // AI 로직...
  }
}

// 사용
const player = new Player("p001", "Hero", 100, 100, 5, 0);
const goblin = new Monster("m001", "Goblin", 50, 50, 3, 15, 50);

// 둘 다 Entity의 메서드 사용 가능
player.moveTo(10, 20);
goblin.moveTo(15, 18);

player.takeDamage(30);
goblin.takeDamage(20);

// 각자의 고유 메서드
player.gainExperience(50);
goblin.performAI();
```

### 메서드 오버라이딩 (Overriding)
자식 클래스에서 부모 클래스의 메서드를 재정의할 수 있다.

```typescript
class Entity {
  constructor(
    public id: string,
    public name: string,
    protected health: number
  ) {}
  
  takeDamage(amount: number): void {
    this.health -= amount;
    console.log(`${this.name} took ${amount} damage`);
  }
}

class Player extends Entity {
  constructor(
    id: string,
    name: string,
    health: number,
    private armor: number
  ) {
    super(id, name, health);
  }
  
  // 메서드 오버라이딩 - 방어력 적용
  takeDamage(amount: number): void {
    const reducedDamage = Math.max(1, amount - this.armor);
    console.log(`[Armor -${this.armor}] ${amount} → ${reducedDamage} damage`);
    
    // super.takeDamage()로 부모 메서드 호출
    super.takeDamage(reducedDamage);
  }
}

class Monster extends Entity {
  constructor(
    id: string,
    name: string,
    health: number,
    private isBoss: boolean
  ) {
    super(id, name, health);
  }
  
  // 메서드 오버라이딩 - 보스는 특수 효과
  takeDamage(amount: number): void {
    if (this.isBoss) {
      console.log(`💢 BOSS RAGE!`);
    }
    super.takeDamage(amount);
  }
}

// 사용
const player = new Player("p001", "Knight", 100, 10);
const boss = new Monster("b001", "Dragon", 1000, true);

player.takeDamage(30);  // [Armor -10] 30 → 20 damage
boss.takeDamage(100);   // 💢 BOSS RAGE!
```

### 게임 엔티티 계층 구조 설계
실제 게임 서버의 엔티티 계층을 설계해보자.

```typescript
// ========== 기본 엔티티 ==========
abstract class BaseEntity {
  constructor(
    public readonly id: string,
    public name: string,
    protected health: number,
    protected maxHealth: number,
    public position: { x: number; y: number; mapId: string }
  ) {}
  
  getHealth(): number {
    return this.health;
  }
  
  getHealthPercentage(): number {
    return (this.health / this.maxHealth) * 100;
  }
  
  takeDamage(amount: number): boolean {
    this.health -= amount;
    if (this.health <= 0) {
      this.health = 0;
      this.onDeath();
      return true;  // 죽음
    }
    return false;
  }
  
  heal(amount: number): void {
    this.health += amount;
    if (this.health > this.maxHealth) {
      this.health = this.maxHealth;
    }
  }
  
  isAlive(): boolean {
    return this.health > 0;
  }
  
  // 추상 메서드 - 자식 클래스에서 반드시 구현
  protected abstract onDeath(): void;
  
  // 가상 메서드 - 오버라이드 가능
  moveTo(x: number, y: number): void {
    this.position.x = x;
    this.position.y = y;
  }
}

// ========== 전투 가능 엔티티 ==========
abstract class CombatEntity extends BaseEntity {
  constructor(
    id: string,
    name: string,
    health: number,
    maxHealth: number,
    position: { x: number; y: number; mapId: string },
    protected attackPower: number,
    protected defense: number
  ) {
    super(id, name, health, maxHealth, position);
  }
  
  getAttackPower(): number {
    return this.attackPower;
  }
  
  getDefense(): number {
    return this.defense;
  }
  
  // 전투 메서드
  attack(target: CombatEntity): void {
    const damage = Math.max(1, this.attackPower - target.defense);
    console.log(`${this.name} attacks ${target.name} for ${damage} damage!`);
    
    const killed = target.takeDamage(damage);
    if (killed) {
      this.onKill(target);
    }
  }
  
  // 적을 처치했을 때 - 오버라이드 가능
  protected onKill(target: CombatEntity): void {
    console.log(`${this.name} killed ${target.name}`);
  }
}

// ========== 플레이어 ==========
class Player extends CombatEntity {
  private experience: number = 0;
  private inventory: Item[] = [];
  
  constructor(
    id: string,
    name: string,
    public level: number
  ) {
    const maxHealth = 100 + (level - 1) * 10;
    const attackPower = 10 + level * 2;
    const defense = 5 + level;
    
    super(
      id,
      name,
      maxHealth,
      maxHealth,
      { x: 0, y: 0, mapId: "town" },
      attackPower,
      defense
    );
  }
  
  protected onDeath(): void {
    console.log(`💀 ${this.name} has died!`);
    // 사망 페널티
    this.experience = Math.floor(this.experience * 0.9);
    // 리스폰 로직...
  }
  
  protected onKill(target: CombatEntity): void {
    super.onKill(target);
    
    // 몬스터라면 경험치 획득
    if (target instanceof Monster) {
      this.gainExperience(target.getExperienceReward());
    }
  }
  
  private gainExperience(amount: number): void {
    this.experience += amount;
    console.log(`  🌟 +${amount} EXP (Total: ${this.experience})`);
    
    const requiredExp = this.level * 100;
    if (this.experience >= requiredExp) {
      this.levelUp();
    }
  }
  
  private levelUp(): void {
    this.level++;
    this.experience -= (this.level - 1) * 100;
    
    // 스탯 증가
    this.maxHealth += 10;
    this.health = this.maxHealth;
    this.attackPower += 2;
    this.defense += 1;
    
    console.log(`  🎉 Level Up! ${this.name} is now level ${this.level}`);
  }
  
  addItem(item: Item): void {
    this.inventory.push(item);
  }
  
  getInventory(): readonly Item[] {
    return this.inventory;
  }
}

// ========== 몬스터 ==========
class Monster extends CombatEntity {
  constructor(
    id: string,
    name: string,
    public level: number,
    private experienceReward: number,
    private aiType: "aggressive" | "passive" | "defensive"
  ) {
    const maxHealth = level * 30;
    const attackPower = level * 3;
    const defense = level * 2;
    
    super(
      id,
      name,
      maxHealth,
      maxHealth,
      { x: 0, y: 0, mapId: "field" },
      attackPower,
      defense
    );
  }
  
  protected onDeath(): void {
    console.log(`💀 ${this.name} has been defeated!`);
    // 아이템 드롭 로직...
  }
  
  getExperienceReward(): number {
    return this.experienceReward;
  }
  
  // AI 행동
  performAI(nearbyEntities: CombatEntity[]): void {
    if (!this.isAlive()) return;
    
    switch (this.aiType) {
      case "aggressive":
        // 가장 가까운 플레이어 공격
        const nearestPlayer = this.findNearestPlayer(nearbyEntities);
        if (nearestPlayer) {
          this.attack(nearestPlayer);
        }
        break;
      
      case "passive":
        // 공격받기 전까지 아무것도 하지 않음
        break;
      
      case "defensive":
        // 체력이 낮으면 도망
        if (this.getHealthPercentage() < 30) {
          console.log(`${this.name} is fleeing!`);
        }
        break;
    }
  }
  
  private findNearestPlayer(entities: CombatEntity[]): Player | null {
    const players = entities.filter(e => e instanceof Player) as Player[];
    // 간단한 구현 - 첫 번째 플레이어 반환
    return players[0] || null;
  }
}

// ========== NPC (전투 불가) ==========
class NPC extends BaseEntity {
  constructor(
    id: string,
    name: string,
    private dialogue: string[]
  ) {
    super(id, name, 100, 100, { x: 0, y: 0, mapId: "town" });
  }
  
  protected onDeath(): void {
    // NPC는 죽지 않음 (무적)
  }
  
  talk(): string {
    const randomDialogue = this.dialogue[Math.floor(Math.random() * this.dialogue.length)];
    console.log(`${this.name}: "${randomDialogue}"`);
    return randomDialogue;
  }
  
  // NPC는 이동하지 않음
  moveTo(x: number, y: number): void {
    console.log(`${this.name}: "I don't move!"`);
  }
}
```

**계층 구조 다이어그램:**

```
                    ┌──────────────┐
                    │  BaseEntity  │ (abstract)
                    └──────────────┘
                           △
                           │
            ┌──────────────┼──────────────┐
            │                             │
     ┌──────────────┐               ┌─────────┐
     │CombatEntity  │ (abstract)    │   NPC   │
     └──────────────┘               └─────────┘
            △
            │
     ┌──────┴───────┐
     │              │
┌─────────┐   ┌──────────┐
│ Player  │   │ Monster  │
└─────────┘   └──────────┘
```

**사용 예제:**

```typescript
function runCombatDemo(): void {
  console.log("=".repeat(60));
  console.log("Game Combat System Demo");
  console.log("=".repeat(60));
  
  // 엔티티 생성
  const hero = new Player("p001", "Hero", 5);
  const goblin = new Monster("m001", "Goblin", 3, 50, "aggressive");
  const shopkeeper = new NPC("npc001", "Merchant", [
    "Welcome to my shop!",
    "Looking for weapons?",
    "Come back anytime!"
  ]);
  
  console.log("\n--- Initial Status ---");
  console.log(`${hero.name}: Lv.${hero.level}, HP: ${hero.getHealth()}`);
  console.log(`${goblin.name}: Lv.${goblin.level}, HP: ${goblin.getHealth()}`);
  
  console.log("\n--- Combat Start ---");
  
  // 전투 시뮬레이션
  let turn = 1;
  while (hero.isAlive() && goblin.isAlive()) {
    console.log(`\nTurn ${turn}:`);
    
    // 플레이어 공격
    hero.attack(goblin);
    
    // 고블린 생존 시 반격
    if (goblin.isAlive()) {
      goblin.attack(hero);
    }
    
    turn++;
    
    if (turn > 10) break;  // 무한 루프 방지
  }
  
  console.log("\n--- Combat End ---");
  console.log(`${hero.name}: HP: ${hero.getHealth()}`);
  
  console.log("\n--- NPC Interaction ---");
  shopkeeper.talk();
  shopkeeper.moveTo(10, 10);  // NPC는 이동하지 않음
  
  console.log("\n" + "=".repeat(60));
}

runCombatDemo();
```

**실행 결과:**

```
============================================================
Game Combat System Demo
============================================================

--- Initial Status ---
Hero: Lv.5, HP: 140
Goblin: Lv.3, HP: 90

--- Combat Start ---

Turn 1:
Hero attacks Goblin for 11 damage!
Goblin attacks Hero for 4 damage!

Turn 2:
Hero attacks Goblin for 11 damage!
Goblin attacks Hero for 4 damage!

Turn 3:
Hero attacks Goblin for 11 damage!
Goblin attacks Hero for 4 damage!

Turn 4:
Hero attacks Goblin for 11 damage!
Goblin attacks Hero for 4 damage!

Turn 5:
Hero attacks Goblin for 11 damage!
Goblin attacks Hero for 4 damage!

Turn 6:
Hero attacks Goblin for 11 damage!
Goblin attacks Hero for 4 damage!

Turn 7:
Hero attacks Goblin for 11 damage!
Goblin attacks Hero for 4 damage!

Turn 8:
Hero attacks Goblin for 11 damage!
💀 Goblin has been defeated!
Hero killed Goblin
  🌟 +50 EXP (Total: 50)

--- Combat End ---
Hero: HP: 112

--- NPC Interaction ---
Merchant: "Welcome to my shop!"
Merchant: "I don't move!"

============================================================
```


## 4.3 인터페이스 구현
클래스는 인터페이스를 구현(implement)할 수 있다. 이를 통해 특정 계약을 강제하고, 다양한 클래스가 동일한 인터페이스를 공유할 수 있다.

### 기본 인터페이스 구현

```typescript
// 인터페이스 정의
interface Movable {
  position: { x: number; y: number };
  speed: number;
  moveTo(x: number, y: number): void;
  getSpeed(): number;
}

interface Attackable {
  attack(target: any): void;
  getAttackPower(): number;
}

// 인터페이스 구현
class Player implements Movable, Attackable {
  position: { x: number; y: number } = { x: 0, y: 0 };
  speed: number = 5;
  private attackPower: number = 10;
  
  constructor(public name: string) {}
  
  // Movable 구현
  moveTo(x: number, y: number): void {
    this.position.x = x;
    this.position.y = y;
    console.log(`${this.name} moved to (${x}, ${y})`);
  }
  
  getSpeed(): number {
    return this.speed;
  }
  
  // Attackable 구현
  attack(target: any): void {
    console.log(`${this.name} attacks!`);
  }
  
  getAttackPower(): number {
    return this.attackPower;
  }
}

// 다른 클래스도 같은 인터페이스 구현
class Monster implements Movable, Attackable {
  position: { x: number; y: number } = { x: 0, y: 0 };
  speed: number = 3;
  private attackPower: number = 15;
  
  constructor(public name: string) {}
  
  moveTo(x: number, y: number): void {
    this.position.x = x;
    this.position.y = y;
    console.log(`${this.name} moved to (${x}, ${y})`);
  }
  
  getSpeed(): number {
    return this.speed;
  }
  
  attack(target: any): void {
    console.log(`${this.name} attacks ferociously!`);
  }
  
  getAttackPower(): number {
    return this.attackPower;
  }
}

// 인터페이스 타입으로 다형성 활용
function moveEntity(entity: Movable, x: number, y: number): void {
  entity.moveTo(x, y);
  console.log(`  Speed: ${entity.getSpeed()}`);
}

const player = new Player("Hero");
const monster = new Monster("Goblin");

moveEntity(player, 10, 20);
moveEntity(monster, 15, 18);
```

### 컴포넌트 패턴 구현 예제
게임 엔진에서 자주 사용하는 컴포넌트 패턴을 TypeScript로 구현해보자.

```typescript
// ========== 컴포넌트 인터페이스 ==========
interface Component {
  readonly type: string;
  update(deltaTime: number): void;
  destroy(): void;
}

// ========== 엔티티 클래스 ==========
class GameObject {
  private components: Map<string, Component> = new Map();
  
  constructor(
    public id: string,
    public name: string
  ) {}
  
  addComponent(component: Component): void {
    this.components.set(component.type, component);
    console.log(`Added ${component.type} to ${this.name}`);
  }
  
  getComponent<T extends Component>(type: string): T | undefined {
    return this.components.get(type) as T | undefined;
  }
  
  hasComponent(type: string): boolean {
    return this.components.has(type);
  }
  
  removeComponent(type: string): void {
    const component = this.components.get(type);
    if (component) {
      component.destroy();
      this.components.delete(type);
    }
  }
  
  update(deltaTime: number): void {
    for (const component of this.components.values()) {
      component.update(deltaTime);
    }
  }
  
  destroy(): void {
    for (const component of this.components.values()) {
      component.destroy();
    }
    this.components.clear();
  }
}

// ========== 구체적인 컴포넌트들 ==========

// 체력 컴포넌트
class HealthComponent implements Component {
  readonly type = "health";
  
  constructor(
    private health: number,
    private maxHealth: number
  ) {}
  
  getHealth(): number {
    return this.health;
  }
  
  takeDamage(amount: number): boolean {
    this.health -= amount;
    if (this.health <= 0) {
      this.health = 0;
      return true;  // 죽음
    }
    return false;
  }
  
  heal(amount: number): void {
    this.health += amount;
    if (this.health > this.maxHealth) {
      this.health = this.maxHealth;
    }
  }
  
  update(deltaTime: number): void {
    // 자동 회복 등의 로직
  }
  
  destroy(): void {
    // 정리
  }
}

// 이동 컴포넌트
class MovementComponent implements Component {
  readonly type = "movement";
  
  constructor(
    private position: { x: number; y: number },
    private speed: number
  ) {}
  
  moveTo(x: number, y: number): void {
    this.position.x = x;
    this.position.y = y;
  }
  
  moveBy(dx: number, dy: number): void {
    this.position.x += dx;
    this.position.y += dy;
  }
  
  getPosition(): { x: number; y: number } {
    return { ...this.position };
  }
  
  getSpeed(): number {
    return this.speed;
  }
  
  update(deltaTime: number): void {
    // 이동 업데이트 로직
  }
  
  destroy(): void {
    // 정리
  }
}

// 공격 컴포넌트
class AttackComponent implements Component {
  readonly type = "attack";
  
  private lastAttackTime: number = 0;
  
  constructor(
    private attackPower: number,
    private attackCooldown: number  // 밀리초
  ) {}
  
  canAttack(): boolean {
    const now = Date.now();
    return (now - this.lastAttackTime) >= this.attackCooldown;
  }
  
  attack(target: GameObject): void {
    if (!this.canAttack()) {
      console.log("Attack on cooldown!");
      return;
    }
    
    const targetHealth = target.getComponent<HealthComponent>("health");
    if (targetHealth) {
      const died = targetHealth.takeDamage(this.attackPower);
      console.log(`Attacked for ${this.attackPower} damage!`);
      
      if (died) {
        console.log("Target eliminated!");
      }
    }
    
    this.lastAttackTime = Date.now();
  }
  
  update(deltaTime: number): void {
    // 쿨다운 업데이트
  }
  
  destroy(): void {
    // 정리
  }
}

// AI 컴포넌트
class AIComponent implements Component {
  readonly type = "ai";
  
  constructor(
    private behavior: "passive" | "aggressive" | "patrol"
  ) {}
  
  update(deltaTime: number): void {
    switch (this.behavior) {
      case "aggressive":
        // 플레이어 추적 로직
        break;
      case "patrol":
        // 정찰 로직
        break;
      case "passive":
        // 가만히 있기
        break;
    }
  }
  
  destroy(): void {
    // 정리
  }
}
```

**컴포넌트 패턴 사용 예제:**

```typescript
function runComponentDemo(): void {
  console.log("=".repeat(60));
  console.log("Component Pattern Demo");
  console.log("=".repeat(60));
  
  // 플레이어 생성
  const player = new GameObject("player_001", "Hero");
  player.addComponent(new HealthComponent(100, 100));
  player.addComponent(new MovementComponent({ x: 0, y: 0 }, 5));
  player.addComponent(new AttackComponent(20, 1000));  // 1초 쿨다운
  
  // 몬스터 생성
  const monster = new GameObject("monster_001", "Goblin");
  monster.addComponent(new HealthComponent(50, 50));
  monster.addComponent(new MovementComponent({ x: 10, y: 10 }, 3));
  monster.addComponent(new AttackComponent(10, 1500));
  monster.addComponent(new AIComponent("aggressive"));
  
  console.log("\n--- Entity Setup Complete ---");
  
  // 플레이어 이동
  const playerMovement = player.getComponent<MovementComponent>("movement");
  if (playerMovement) {
    playerMovement.moveTo(5, 5);
    console.log(`Player position: (${playerMovement.getPosition().x}, ${playerMovement.getPosition().y})`);
  }
  
  // 전투
  console.log("\n--- Combat ---");
  const playerAttack = player.getComponent<AttackComponent>("attack");
  const monsterHealth = monster.getComponent<HealthComponent>("health");
  
  if (playerAttack && monsterHealth) {
    console.log(`Monster HP: ${monsterHealth.getHealth()}`);
    
    playerAttack.attack(monster);
    console.log(`Monster HP: ${monsterHealth.getHealth()}`);
    
    playerAttack.attack(monster);  // 쿨다운!
    
    // 1초 대기 후 다시 공격
    setTimeout(() => {
      playerAttack.attack(monster);
      console.log(`Monster HP: ${monsterHealth.getHealth()}`);
    }, 1100);
  }
  
  console.log("\n" + "=".repeat(60));
}

runComponentDemo();
```

**컴포넌트 패턴의 장점:**

```
✅ 유연성: 엔티티에 동적으로 기능 추가/제거
✅ 재사용성: 컴포넌트를 여러 엔티티에 재사용
✅ 조합: 다양한 컴포넌트 조합으로 다양한 엔티티 생성
✅ 유지보수: 각 컴포넌트가 독립적으로 관리됨

예시:
┌──────────────────────────────────────────┐
│ Player GameObject                        │
├──────────────────────────────────────────┤
│ • HealthComponent                        │
│ • MovementComponent                      │
│ • AttackComponent                        │
│ • InventoryComponent                     │
│ • (원하는 만큼 추가 가능)                   │
└──────────────────────────────────────────┘

┌──────────────────────────────────────────┐
│ NPC GameObject                           │
├──────────────────────────────────────────┤
│ • HealthComponent                        │
│ • MovementComponent                      │
│ • DialogueComponent                      │
│ • (공격 컴포넌트 없음)                      │
└──────────────────────────────────────────┘
```


## 🎉 Chapter 4 완료!
축하합니다! TypeScript 클래스와 OOP를 완벽하게 마스터했다.

**지금까지 배운 것:**
- ✅ 클래스 기본 문법과 생성자
- ✅ 접근 제어자 (public, private, protected)
- ✅ readonly와 프로퍼티 초기화
- ✅ 상속 (extends)과 super
- ✅ 메서드 오버라이딩
- ✅ 추상 클래스 (abstract)
- ✅ 게임 엔티티 계층 구조 설계
- ✅ 인터페이스 구현 (implements)
- ✅ 컴포넌트 패턴 구현
  
  
**다음 Chapter 미리보기:**  
Chapter 5에서는 TypeScript의 고급 타입 기능을 학습한다. 유틸리티 타입(`Partial`, `Pick`, `Omit` 등), 타입 가드, 맵드 타입, 조건부 타입 등 실전에서 코드 재사용성과 타입 안전성을 극대화하는 강력한 패턴들을 배운다.

  

# Chapter 5: 고급 타입 패턴

## 5.1 유틸리티 타입 TOP 5
TypeScript는 자주 사용하는 타입 변환 패턴을 유틸리티 타입으로 제공한다. 게임 서버 개발에서 가장 유용한 5가지를 집중적으로 학습한다.

### 1. Partial<T> - 부분 업데이트
모든 속성을 선택적(optional)으로 만든다. 게임 서버에서 엔티티의 일부 속성만 업데이트할 때 매우 유용하다.

```typescript
interface Player {
  id: string;
  name: string;
  level: number;
  health: number;
  maxHealth: number;
  position: { x: number; y: number };
}

// ❌ 전체 객체를 요구하면 불편함
function updatePlayerOld(playerId: string, updates: Player): void {
  // level만 바꾸고 싶어도 모든 필드를 전달해야 함
}

// ✅ Partial로 일부만 업데이트
function updatePlayer(playerId: string, updates: Partial<Player>): void {
  // updates의 모든 속성이 optional
  console.log(`Updating player ${playerId}:`, updates);
  
  // 실제로는 데이터베이스 업데이트
  if (updates.level !== undefined) {
    console.log(`  Level: ${updates.level}`);
  }
  if (updates.health !== undefined) {
    console.log(`  Health: ${updates.health}`);
  }
}

// 사용 - 원하는 필드만 전달
updatePlayer("player_001", { level: 10 });
updatePlayer("player_002", { health: 50, maxHealth: 100 });
updatePlayer("player_003", { position: { x: 100, y: 200 } });
```

**실전 예제: 설정 업데이트**

```typescript
interface GameConfig {
  maxPlayers: number;
  tickRate: number;
  mapSize: number;
  difficulty: "easy" | "normal" | "hard";
  enablePvP: boolean;
  chatFilter: boolean;
}

class GameServer {
  private config: GameConfig = {
    maxPlayers: 1000,
    tickRate: 60,
    mapSize: 2048,
    difficulty: "normal",
    enablePvP: true,
    chatFilter: true
  };
  
  // 일부 설정만 변경 가능
  updateConfig(updates: Partial<GameConfig>): void {
    this.config = { ...this.config, ...updates };
    console.log("Config updated:", updates);
  }
  
  getConfig(): Readonly<GameConfig> {
    return this.config;
  }
}

const server = new GameServer();

// 일부만 업데이트
server.updateConfig({ maxPlayers: 2000 });
server.updateConfig({ difficulty: "hard", enablePvP: false });
```

### 2. Required<T> - 필수 필드 강제
`Partial`의 반대로, 모든 속성을 필수로 만듭니다.

```typescript
interface PlayerOptions {
  name?: string;
  level?: number;
  class?: "warrior" | "mage" | "archer";
}

// 생성 시에는 옵셔널
function createPlayer(options: PlayerOptions): Player {
  // 기본값 설정
  const defaults = {
    name: "NewPlayer",
    level: 1,
    class: "warrior" as const
  };
  
  const finalOptions = { ...defaults, ...options };
  
  // 이제 모든 필드가 확정됨
  return {
    id: `player_${Date.now()}`,
    name: finalOptions.name,
    level: finalOptions.level,
    health: 100,
    maxHealth: 100,
    position: { x: 0, y: 0 }
  };
}

// Required로 검증 함수
function validatePlayerData(data: Required<PlayerOptions>): boolean {
  // data의 모든 필드가 필수
  // name, level, class가 반드시 있어야 함
  return data.name.length > 0 && data.level > 0;
}

// ✅ OK
validatePlayerData({ name: "Hero", level: 5, class: "warrior" });

// ❌ 에러
// validatePlayerData({ name: "Hero" });  // level, class 누락
```

**실전 예제: 데이터 완전성 검사**

```typescript
interface QuestData {
  id?: string;
  title?: string;
  description?: string;
  requiredLevel?: number;
  rewards?: {
    gold?: number;
    experience?: number;
    items?: string[];
  };
}

class QuestValidator {
  // 퀘스트가 완성되었는지 검사
  isComplete(quest: QuestData): quest is Required<QuestData> {
    return (
      quest.id !== undefined &&
      quest.title !== undefined &&
      quest.description !== undefined &&
      quest.requiredLevel !== undefined &&
      quest.rewards !== undefined
    );
  }
  
  // 완성된 퀘스트만 저장
  saveQuest(quest: Required<QuestData>): void {
    console.log(`Saving quest: ${quest.title}`);
    // 모든 필드가 보장됨
  }
}

const validator = new QuestValidator();

const draftQuest: QuestData = {
  title: "Kill 10 Goblins"
};

if (validator.isComplete(draftQuest)) {
  // 타입 가드 통과 시 Required<QuestData>로 좁혀짐
  validator.saveQuest(draftQuest);
} else {
  console.log("Quest is incomplete!");
}
```

### 3. Pick<T, K> - 필요한 속성만
객체 타입에서 특정 속성만 선택한다.

```typescript
interface Player {
  id: string;
  name: string;
  level: number;
  health: number;
  maxHealth: number;
  mana: number;
  maxMana: number;
  experience: number;
  inventory: Item[];
  guild: Guild | null;
}

// 플레이어 목록에서는 기본 정보만 필요
type PlayerListItem = Pick<Player, "id" | "name" | "level">;

function getPlayerList(): PlayerListItem[] {
  return [
    { id: "p001", name: "Hero", level: 10 },
    { id: "p002", name: "Mage", level: 8 },
    { id: "p003", name: "Tank", level: 12 }
  ];
}

// 전투 정보만 필요
type CombatInfo = Pick<Player, "id" | "name" | "health" | "maxHealth">;

function displayCombatStatus(player: CombatInfo): void {
  const hpPercent = (player.health / player.maxHealth * 100).toFixed(0);
  console.log(`${player.name}: ${player.health}/${player.maxHealth} (${hpPercent}%)`);
}

// 위치 정보만 필요
type Position = Pick<Player, "id" | "name"> & {
  position: { x: number; y: number };
};
```

**실전 예제: API 응답 타입**

```typescript
interface FullPlayerData {
  id: string;
  accountId: string;
  name: string;
  email: string;
  passwordHash: string;
  level: number;
  experience: number;
  health: number;
  maxHealth: number;
  createdAt: Date;
  lastLoginAt: Date;
  inventory: Item[];
  privateNotes: string;
}

// 클라이언트에는 민감한 정보 제외
type PublicPlayerData = Pick<
  FullPlayerData,
  "id" | "name" | "level" | "health" | "maxHealth"
>;

// 관리자에게는 더 많은 정보
type AdminPlayerData = Pick<
  FullPlayerData,
  "id" | "accountId" | "name" | "email" | "level" | "createdAt" | "lastLoginAt"
>;

class PlayerAPI {
  // 공개 API
  getPublicProfile(playerId: string): PublicPlayerData {
    return {
      id: playerId,
      name: "Hero",
      level: 10,
      health: 100,
      maxHealth: 100
    };
  }
  
  // 관리자 API
  getAdminProfile(playerId: string): AdminPlayerData {
    return {
      id: playerId,
      accountId: "acc_001",
      name: "Hero",
      email: "hero@game.com",
      level: 10,
      createdAt: new Date(),
      lastLoginAt: new Date()
    };
    // passwordHash, privateNotes는 노출되지 않음
  }
}
```

### 4. Omit<T, K> - 제외하고 선택
`Pick`의 반대로, 특정 속성을 제외한다.

```typescript
interface Player {
  id: string;
  name: string;
  level: number;
  health: number;
  passwordHash: string;
  privateData: any;
}

// passwordHash와 privateData 제외
type SafePlayer = Omit<Player, "passwordHash" | "privateData">;

function sendToClient(player: SafePlayer): void {
  // player에는 passwordHash와 privateData가 없음
  console.log(player);
}

// 생성 시에는 ID 없이
type PlayerCreationData = Omit<Player, "id" | "passwordHash" | "privateData">;

function createNewPlayer(data: PlayerCreationData): Player {
  return {
    ...data,
    id: `player_${Date.now()}`,
    passwordHash: "hashed",
    privateData: {}
  };
}

const newPlayer = createNewPlayer({
  name: "NewHero",
  level: 1,
  health: 100
});
```

**실전 예제: 업데이트 DTO (Data Transfer Object)**

```typescript
interface Monster {
  readonly id: string;
  readonly templateId: string;
  name: string;
  level: number;
  health: number;
  maxHealth: number;
  position: { x: number; y: number };
  createdAt: Date;
}

// 업데이트 시 readonly와 createdAt 제외
type MonsterUpdate = Omit<Monster, "id" | "templateId" | "createdAt">;

class MonsterManager {
  private monsters = new Map<string, Monster>();
  
  updateMonster(id: string, updates: Partial<MonsterUpdate>): boolean {
    const monster = this.monsters.get(id);
    if (!monster) {
      return false;
    }
    
    // id, templateId, createdAt는 변경 불가능
    Object.assign(monster, updates);
    return true;
  }
}

const manager = new MonsterManager();

// ✅ OK - 허용된 필드만
manager.updateMonster("m001", { health: 50 });
manager.updateMonster("m001", { position: { x: 10, y: 20 } });

// ❌ 에러 - readonly 필드 수정 불가
// manager.updateMonster("m001", { id: "m002" });
// manager.updateMonster("m001", { templateId: "goblin" });
```

### 5. Record<K, T> - 딕셔너리 타입
키-값 쌍의 객체 타입을 정의한다. 게임 서버에서 맵, 캐시, 설정 등에 자주 사용된다.   

```typescript
// 플레이어 ID를 키로, 플레이어 객체를 값으로
type PlayerMap = Record<string, Player>;

const players: PlayerMap = {
  "player_001": { id: "player_001", name: "Hero", level: 10, /* ... */ },
  "player_002": { id: "player_002", name: "Mage", level: 8, /* ... */ }
};

// 맵 ID를 키로, 맵 설정을 값으로
type MapId = "town" | "forest" | "dungeon" | "boss_room";

interface MapConfig {
  name: string;
  maxPlayers: number;
  spawnPoint: { x: number; y: number };
}

const mapConfigs: Record<MapId, MapConfig> = {
  town: {
    name: "Peaceful Town",
    maxPlayers: 100,
    spawnPoint: { x: 0, y: 0 }
  },
  forest: {
    name: "Dark Forest",
    maxPlayers: 50,
    spawnPoint: { x: 100, y: 100 }
  },
  dungeon: {
    name: "Ancient Dungeon",
    maxPlayers: 10,
    spawnPoint: { x: 0, y: 0 }
  },
  boss_room: {
    name: "Boss Chamber",
    maxPlayers: 5,
    spawnPoint: { x: 256, y: 256 }
  }
};

// 타입 안전한 접근
function getMapConfig(mapId: MapId): MapConfig {
  return mapConfigs[mapId];  // ✅ 항상 존재함이 보장됨
}
```

**실전 예제: 스킬 데이터베이스**

```typescript
type SkillId = string;

interface Skill {
  id: SkillId;
  name: string;
  description: string;
  cooldown: number;  // 초
  manaCost: number;
  damage: number;
  range: number;
}

class SkillDatabase {
  private skills: Record<SkillId, Skill> = {
    "fireball": {
      id: "fireball",
      name: "Fireball",
      description: "Launches a ball of fire",
      cooldown: 5,
      manaCost: 30,
      damage: 50,
      range: 10
    },
    "heal": {
      id: "heal",
      name: "Heal",
      description: "Restores health",
      cooldown: 10,
      manaCost: 50,
      damage: -30,  // 음수는 회복
      range: 5
    },
    "meteor": {
      id: "meteor",
      name: "Meteor",
      description: "Calls down a meteor",
      cooldown: 60,
      manaCost: 100,
      damage: 200,
      range: 15
    }
  };
  
  getSkill(skillId: SkillId): Skill | undefined {
    return this.skills[skillId];
  }
  
  getAllSkills(): Skill[] {
    return Object.values(this.skills);
  }
  
  addSkill(skill: Skill): void {
    this.skills[skill.id] = skill;
  }
}

// 스킬 쿨다운 관리
type CooldownMap = Record<SkillId, number>;  // 스킬 ID -> 마지막 사용 시각

class PlayerSkillManager {
  private cooldowns: CooldownMap = {};
  
  canUseSkill(skillId: SkillId, skill: Skill): boolean {
    const lastUsed = this.cooldowns[skillId] || 0;
    const now = Date.now();
    const cooldownMs = skill.cooldown * 1000;
    
    return (now - lastUsed) >= cooldownMs;
  }
  
  useSkill(skillId: SkillId): void {
    this.cooldowns[skillId] = Date.now();
  }
  
  getRemainingCooldown(skillId: SkillId, skill: Skill): number {
    const lastUsed = this.cooldowns[skillId] || 0;
    const now = Date.now();
    const cooldownMs = skill.cooldown * 1000;
    const elapsed = now - lastUsed;
    
    return Math.max(0, cooldownMs - elapsed) / 1000;
  }
}
```

**유틸리티 타입 조합:**

```typescript
// 여러 유틸리티 타입을 조합하여 복잡한 타입 생성
interface FullEntity {
  id: string;
  name: string;
  health: number;
  maxHealth: number;
  position: { x: number; y: number };
  createdAt: Date;
  internalState: any;
}

// 1. createdAt와 internalState 제외
type EntityWithoutMeta = Omit<FullEntity, "createdAt" | "internalState">;

// 2. 그 중에서 id, name, position만 선택
type EntityBasicInfo = Pick<EntityWithoutMeta, "id" | "name" | "position">;

// 3. 모든 필드를 선택적으로
type PartialEntity = Partial<FullEntity>;

// 4. 특정 필드는 필수, 나머지는 선택적
type EntityUpdate = Partial<Omit<FullEntity, "id">> & Pick<FullEntity, "id">;

// 사용
function updateEntity(update: EntityUpdate): void {
  // id는 필수, 나머지는 선택적
  console.log(`Updating entity ${update.id}`);
  if (update.health !== undefined) {
    console.log(`  Health: ${update.health}`);
  }
}

updateEntity({ id: "e001" });  // ✅ OK
updateEntity({ id: "e001", health: 50, position: { x: 10, y: 20 } });  // ✅ OK
// updateEntity({ health: 50 });  // ❌ 에러 - id 필수
```


## 5.2 타입 가드
타입 가드는 런타임에 값의 타입을 확인하여 TypeScript가 타입을 좁힐 수 있게 해준다.

### typeof 타입 가드
원시 타입을 확인할 때 사용한다.

```typescript
function processValue(value: string | number): string {
  if (typeof value === "string") {
    // 이 블록 안에서 value는 string
    return value.toUpperCase();
  } else {
    // 이 블록 안에서 value는 number
    return value.toFixed(2);
  }
}

// 실전 예제: 패킷 데이터 검증
function validatePacketData(data: unknown): boolean {
  if (typeof data !== "object" || data === null) {
    return false;
  }
  
  const packet = data as any;
  
  if (typeof packet.type !== "string") {
    return false;
  }
  
  if (typeof packet.timestamp !== "number") {
    return false;
  }
  
  return true;
}
```

### instanceof 타입 가드
클래스 인스턴스를 확인할 때 사용한다.

```typescript
class Player {
  constructor(public name: string, public level: number) {}
  
  attack(): void {
    console.log(`${this.name} attacks!`);
  }
}

class Monster {
  constructor(public name: string, public health: number) {}
  
  roar(): void {
    console.log(`${this.name} roars!`);
  }
}

function handleEntity(entity: Player | Monster): void {
  if (entity instanceof Player) {
    // entity는 Player 타입
    console.log(`Player: ${entity.name}, Level: ${entity.level}`);
    entity.attack();
  } else {
    // entity는 Monster 타입
    console.log(`Monster: ${entity.name}, HP: ${entity.health}`);
    entity.roar();
  }
}

const player = new Player("Hero", 10);
const monster = new Monster("Goblin", 50);

handleEntity(player);
handleEntity(monster);
```

### 사용자 정의 타입 가드
복잡한 타입 검사를 위한 커스텀 타입 가드 함수를 만들 수 있다.

```typescript
// 타입 가드 함수: 반환 타입이 'value is Type' 형태
function isPlayer(entity: any): entity is Player {
  return (
    entity &&
    typeof entity.name === "string" &&
    typeof entity.level === "number" &&
    typeof entity.attack === "function"
  );
}

function isMonster(entity: any): entity is Monster {
  return (
    entity &&
    typeof entity.name === "string" &&
    typeof entity.health === "number" &&
    typeof entity.roar === "function"
  );
}

// 사용
function processEntity(entity: unknown): void {
  if (isPlayer(entity)) {
    // entity는 Player 타입으로 좁혀짐
    console.log(`Player level: ${entity.level}`);
    entity.attack();
  } else if (isMonster(entity)) {
    // entity는 Monster 타입으로 좁혀짐
    console.log(`Monster HP: ${entity.health}`);
    entity.roar();
  } else {
    console.log("Unknown entity type");
  }
}
```

**실전 예제: 패킷 타입 가드**

```typescript
interface BasePacket {
  type: string;
  timestamp: number;
}

interface MovePacket extends BasePacket {
  type: "move";
  x: number;
  y: number;
}

interface AttackPacket extends BasePacket {
  type: "attack";
  targetId: string;
  skillId: string;
}

interface ChatPacket extends BasePacket {
  type: "chat";
  message: string;
  channel: string;
}

// 타입 가드 함수들
function isMovePacket(packet: BasePacket): packet is MovePacket {
  return (
    packet.type === "move" &&
    "x" in packet &&
    "y" in packet &&
    typeof (packet as any).x === "number" &&
    typeof (packet as any).y === "number"
  );
}

function isAttackPacket(packet: BasePacket): packet is AttackPacket {
  return (
    packet.type === "attack" &&
    "targetId" in packet &&
    "skillId" in packet
  );
}

function isChatPacket(packet: BasePacket): packet is ChatPacket {
  return (
    packet.type === "chat" &&
    "message" in packet &&
    "channel" in packet
  );
}

// 패킷 핸들러
function handlePacket(packet: BasePacket): void {
  if (isMovePacket(packet)) {
    console.log(`Move to (${packet.x}, ${packet.y})`);
  } else if (isAttackPacket(packet)) {
    console.log(`Attack ${packet.targetId} with ${packet.skillId}`);
  } else if (isChatPacket(packet)) {
    console.log(`[${packet.channel}] ${packet.message}`);
  } else {
    console.log(`Unknown packet type: ${packet.type}`);
  }
}
```

### in 연산자를 사용한 타입 가드
객체에 특정 속성이 있는지 확인한다.

```typescript
interface Bird {
  fly(): void;
  layEggs(): void;
}

interface Fish {
  swim(): void;
  layEggs(): void;
}

function move(animal: Bird | Fish): void {
  if ("fly" in animal) {
    // animal은 Bird
    animal.fly();
  } else {
    // animal은 Fish
    animal.swim();
  }
  
  // 공통 메서드는 항상 사용 가능
  animal.layEggs();
}
```

**실전 예제: 아이템 타입 구분**

```typescript
interface Weapon {
  type: "weapon";
  name: string;
  damage: number;
  durability: number;
}

interface Armor {
  type: "armor";
  name: string;
  defense: number;
  durability: number;
}

interface Consumable {
  type: "consumable";
  name: string;
  effect: string;
  quantity: number;
}

type Item = Weapon | Armor | Consumable;

// 타입 가드
function isWeapon(item: Item): item is Weapon {
  return item.type === "weapon";
}

function isArmor(item: Item): item is Armor {
  return item.type === "armor";
}

function isConsumable(item: Item): item is Consumable {
  return item.type === "consumable";
}

// 아이템 사용
function useItem(item: Item): void {
  if (isWeapon(item)) {
    console.log(`Equipped ${item.name} (Damage: ${item.damage})`);
  } else if (isArmor(item)) {
    console.log(`Equipped ${item.name} (Defense: ${item.defense})`);
  } else if (isConsumable(item)) {
    console.log(`Used ${item.name} (Effect: ${item.effect})`);
    item.quantity--;
  }
}

// 아이템 정보 표시
function displayItem(item: Item): void {
  console.log(`${item.name} (${item.type})`);
  
  // 공통 속성
  console.log(`  Type: ${item.type}`);
  
  // 타입별 속성
  if ("damage" in item) {
    console.log(`  Damage: ${item.damage}`);
  }
  if ("defense" in item) {
    console.log(`  Defense: ${item.defense}`);
  }
  if ("durability" in item) {
    console.log(`  Durability: ${item.durability}`);
  }
  if ("quantity" in item) {
    console.log(`  Quantity: ${item.quantity}`);
  }
}
```


## 5.3 맵드 타입과 조건부 타입 (간단히)
맵드 타입과 조건부 타입은 TypeScript의 가장 고급 기능이다. 게임 서버에서 자주 사용하는 패턴만 간단히 소개한다.

### 맵드 타입 - 타입 변환  
기존 타입을 기반으로 새로운 타입을 생성한다.

```typescript
// 기본 타입
interface Player {
  id: string;
  name: string;
  level: number;
  health: number;
}

// 모든 속성을 readonly로
type ReadonlyPlayer = {
  readonly [K in keyof Player]: Player[K];
};

// 실제로는 Readonly<T> 유틸리티 타입이 이렇게 구현됨
type MyReadonly<T> = {
  readonly [K in keyof T]: T[K];
};

// 모든 속성을 선택적으로
type MyPartial<T> = {
  [K in keyof T]?: T[K];
};

// 모든 속성을 필수로
type MyRequired<T> = {
  [K in keyof T]-?: T[K];  // -?는 optional 제거
};
```

**실전 예제: 변경 이벤트 타입**

```typescript
interface GameState {
  playerCount: number;
  currentMap: string;
  isRunning: boolean;
  serverLoad: number;
}

// 각 속성의 변경 이벤트 타입 생성
type StateChangeEvents = {
  [K in keyof GameState as `${K}Changed`]: (newValue: GameState[K]) => void;
};

// 결과:
// {
//   playerCountChanged: (newValue: number) => void;
//   currentMapChanged: (newValue: string) => void;
//   isRunningChanged: (newValue: boolean) => void;
//   serverLoadChanged: (newValue: number) => void;
// }

class GameStateManager {
  private state: GameState = {
    playerCount: 0,
    currentMap: "town",
    isRunning: false,
    serverLoad: 0
  };
  
  private listeners: Partial<StateChangeEvents> = {};
  
  // 이벤트 리스너 등록
  on<K extends keyof StateChangeEvents>(
    event: K,
    listener: StateChangeEvents[K]
  ): void {
    this.listeners[event] = listener;
  }
  
  // 상태 업데이트
  setState<K extends keyof GameState>(key: K, value: GameState[K]): void {
    this.state[key] = value;
    
    // 이벤트 발생
    const eventName = `${key}Changed` as keyof StateChangeEvents;
    const listener = this.listeners[eventName];
    if (listener) {
      (listener as any)(value);
    }
  }
}

// 사용
const stateManager = new GameStateManager();

stateManager.on("playerCountChanged", (count) => {
  console.log(`Player count changed: ${count}`);
});

stateManager.on("currentMapChanged", (map) => {
  console.log(`Map changed: ${map}`);
});

stateManager.setState("playerCount", 10);  // Player count changed: 10
stateManager.setState("currentMap", "dungeon");  // Map changed: dungeon
```

### 조건부 타입 - 타입 분기
조건에 따라 다른 타입을 반환한다.

```typescript
// 기본 문법: T extends U ? X : Y

// 배열이면 요소 타입 추출, 아니면 원본 타입
type Unwrap<T> = T extends Array<infer U> ? U : T;

type Test1 = Unwrap<string[]>;  // string
type Test2 = Unwrap<number>;    // number

// 함수 반환 타입 추출
type ReturnType<T> = T extends (...args: any[]) => infer R ? R : never;

type Func = () => Player;
type FuncReturn = ReturnType<Func>;  // Player
```

**실전 예제: 스마트 타입 선택**

```typescript
// 응답 타입이 성공/실패에 따라 다름
type Response<T, IsSuccess extends boolean> = IsSuccess extends true
  ? { success: true; data: T }
  : { success: false; error: string };

// 사용
type SuccessResponse = Response<Player, true>;
// { success: true; data: Player }

type ErrorResponse = Response<Player, false>;
// { success: false; error: string }

// 실전 API 함수
async function fetchPlayer(playerId: string): Promise<Response<Player, boolean>> {
  try {
    // 데이터 로드
    const player = await loadPlayerFromDB(playerId);
    return { success: true, data: player };
  } catch (error) {
    return { 
      success: false, 
      error: error instanceof Error ? error.message : "Unknown error"
    };
  }
}

// 타입 가드와 함께 사용
async function handlePlayerFetch(playerId: string): Promise<void> {
  const response = await fetchPlayer(playerId);
  
  if (response.success) {
    // response.data는 Player 타입
    console.log(`Loaded: ${response.data.name}`);
  } else {
    // response.error는 string 타입
    console.error(`Error: ${response.error}`);
  }
}

async function loadPlayerFromDB(playerId: string): Promise<Player> {
  // DB 로드 시뮬레이션
  return {
    id: playerId,
    name: "Hero",
    level: 10,
    health: 100,
    maxHealth: 100,
    mana: 50,
    maxMana: 50,
    position: { x: 0, y: 0, mapId: "town" },
    inventory: [],
    guild: null,
    status: "online",
    experience: 0
  };
}
```

**Exclude와 Extract:**

```typescript
// Exclude: 특정 타입 제거
type AllEvents = "move" | "attack" | "chat" | "trade" | "logout";
type CombatEvents = "attack";
type NonCombatEvents = Exclude<AllEvents, CombatEvents>;
// "move" | "chat" | "trade" | "logout"

// Extract: 특정 타입만 추출
type NetworkEvents = "move" | "attack" | "chat";
type CombatOnlyEvents = Extract<NetworkEvents, "attack">;
// "attack"

// 실전 예제
type PacketType = "move" | "attack" | "chat" | "trade" | "logout";
type ServerOnlyPackets = "logout";
type ClientPacketTypes = Exclude<PacketType, ServerOnlyPackets>;
// "move" | "attack" | "chat" | "trade"

function handleClientPacket(type: ClientPacketTypes): void {
  // logout은 허용되지 않음
  console.log(`Handling: ${type}`);
}

handleClientPacket("move");    // ✅ OK
handleClientPacket("attack");  // ✅ OK
// handleClientPacket("logout");  // ❌ 에러
```


## 🎯 Chapter 5 실전 종합 예제
지금까지 배운 고급 타입 패턴을 모두 활용한 게임 서버 데이터 관리 시스템을 만들어보자.

```typescript
// ========== 기본 타입 정의 ==========

interface BaseEntity {
  id: string;
  createdAt: Date;
  updatedAt: Date;
}

interface Player extends BaseEntity {
  name: string;
  level: number;
  experience: number;
  health: number;
  maxHealth: number;
  position: { x: number; y: number; mapId: string };
  inventory: Item[];
  guild: Guild | null;
}

interface Guild extends BaseEntity {
  name: string;
  level: number;
  memberCount: number;
  maxMembers: number;
}

interface Item extends BaseEntity {
  name: string;
  type: "weapon" | "armor" | "consumable";
  rarity: "common" | "rare" | "epic" | "legendary";
}

// ========== 유틸리티 타입 활용 ==========

// 1. 생성 데이터 (ID, createdAt, updatedAt 제외)
type CreateData<T extends BaseEntity> = Omit<T, keyof BaseEntity>;

// 2. 업데이트 데이터 (ID, 타임스탬프 제외, 나머지는 선택적)
type UpdateData<T extends BaseEntity> = Partial<
  Omit<T, "id" | "createdAt" | "updatedAt">
>;

// 3. 공개 데이터 (민감한 정보 제외)
type PublicData<T> = Omit<T, "createdAt" | "updatedAt">;

// 4. 요약 데이터 (필수 정보만)
type PlayerSummary = Pick<Player, "id" | "name" | "level">;
type GuildSummary = Pick<Guild, "id" | "name" | "level" | "memberCount">;

// ========== 타입 가드 ==========

function isPlayer(entity: unknown): entity is Player {
  return (
    typeof entity === "object" &&
    entity !== null &&
    "name" in entity &&
    "level" in entity &&
    "experience" in entity &&
    "inventory" in entity
  );
}

function isGuild(entity: unknown): entity is Guild {
  return (
    typeof entity === "object" &&
    entity !== null &&
    "name" in entity &&
    "memberCount" in entity &&
    "maxMembers" in entity
  );
}

// ========== 제네릭 Repository ==========

class Repository<T extends BaseEntity> {
  private data = new Map<string, T>();
  private idCounter = 1;
  
  // 생성
  create(createData: CreateData<T>): T {
    const id = `${this.idCounter++}`;
    const now = new Date();
    
    const entity: T = {
      ...createData,
      id,
      createdAt: now,
      updatedAt: now
    } as T;
    
    this.data.set(id, entity);
    console.log(`✅ Created entity: ${id}`);
    
    return entity;
  }
  
  // 조회
  findById(id: string): T | undefined {
    return this.data.get(id);
  }
  
  // 전체 조회
  findAll(): T[] {
    return Array.from(this.data.values());
  }
  
  // 조건 조회
  findWhere(predicate: (entity: T) => boolean): T[] {
    return this.findAll().filter(predicate);
  }
  
  // 업데이트
  update(id: string, updates: UpdateData<T>): T | undefined {
    const entity = this.data.get(id);
    
    if (!entity) {
      console.error(`❌ Entity not found: ${id}`);
      return undefined;
    }
    
    // 업데이트 적용
    Object.assign(entity, updates, { updatedAt: new Date() });
    console.log(`✅ Updated entity: ${id}`);
    
    return entity;
  }
  
  // 삭제
  delete(id: string): boolean {
    const deleted = this.data.delete(id);
    if (deleted) {
      console.log(`✅ Deleted entity: ${id}`);
    } else {
      console.error(`❌ Entity not found: ${id}`);
    }
    return deleted;
  }
  
  // 개수
  count(): number {
    return this.data.size;
  }
}

// ========== 맵드 타입: 캐시 통계 ==========

type CacheStats<T extends BaseEntity> = {
  [K in keyof T as `${string & K}CacheHits`]?: number;
} & {
  totalEntities: number;
  lastUpdated: Date;
};

// ========== 조건부 타입: 응답 타입 ==========

type OperationResult<T, Success extends boolean = boolean> = Success extends true
  ? { success: true; data: T }
  : { success: false; error: string; code: number };

// ========== 게임 데이터 매니저 ==========

class GameDataManager {
  private playerRepo = new Repository<Player>();
  private guildRepo = new Repository<Guild>();
  private itemRepo = new Repository<Item>();
  
  // ===== 플레이어 관리 =====
  
  createPlayer(data: CreateData<Player>): OperationResult<Player, true> {
    try {
      const player = this.playerRepo.create(data);
      return { success: true, data: player };
    } catch (error) {
      // TypeScript는 이 분기가 절대 실행되지 않음을 알지만, 예제를 위해 유지
      return { success: false, error: "Failed to create player", code: 500 } as any;
    }
  }
  
  getPlayer(id: string): OperationResult<Player, boolean> {
    const player = this.playerRepo.findById(id);
    
    if (player) {
      return { success: true, data: player };
    } else {
      return { success: false, error: "Player not found", code: 404 };
    }
  }
  
  updatePlayer(
    id: string,
    updates: UpdateData<Player>
  ): OperationResult<Player, boolean> {
    const player = this.playerRepo.update(id, updates);
    
    if (player) {
      return { success: true, data: player };
    } else {
      return { success: false, error: "Player not found", code: 404 };
    }
  }
  
  getPlayerSummaries(): PlayerSummary[] {
    return this.playerRepo.findAll().map(p => ({
      id: p.id,
      name: p.name,
      level: p.level
    }));
  }
  
  // 레벨별 검색
  findPlayersByLevel(minLevel: number, maxLevel: number): Player[] {
    return this.playerRepo.findWhere(
      p => p.level >= minLevel && p.level <= maxLevel
    );
  }
  
  // ===== 길드 관리 =====
  
  createGuild(data: CreateData<Guild>): OperationResult<Guild, true> {
    const guild = this.guildRepo.create(data);
    return { success: true, data: guild };
  }
  
  getGuild(id: string): OperationResult<Guild, boolean> {
    const guild = this.guildRepo.findById(id);
    
    if (guild) {
      return { success: true, data: guild };
    } else {
      return { success: false, error: "Guild not found", code: 404 };
    }
  }
  
  // ===== 아이템 관리 =====
  
  createItem(data: CreateData<Item>): Item {
    return this.itemRepo.create(data);
  }
  
  getItemsByRarity(rarity: Item["rarity"]): Item[] {
    return this.itemRepo.findWhere(item => item.rarity === rarity);
  }
  
  // ===== 통계 =====
  
  getStatistics(): {
    players: number;
    guilds: number;
    items: number;
    averageLevel: number;
  } {
    const players = this.playerRepo.findAll();
    const totalLevel = players.reduce((sum, p) => sum + p.level, 0);
    
    return {
      players: this.playerRepo.count(),
      guilds: this.guildRepo.count(),
      items: this.itemRepo.count(),
      averageLevel: players.length > 0 ? totalLevel / players.length : 0
    };
  }
}

// ========== 사용 예제 ==========

function runAdvancedTypeDemo(): void {
  console.log("=".repeat(60));
  console.log("Advanced Type Patterns Demo");
  console.log("=".repeat(60));
  
  const dataManager = new GameDataManager();
  
  // 플레이어 생성
  console.log("\n--- Creating Players ---");
  
  const player1Result = dataManager.createPlayer({
    name: "Hero",
    level: 10,
    experience: 500,
    health: 100,
    maxHealth: 100,
    position: { x: 0, y: 0, mapId: "town" },
    inventory: [],
    guild: null
  });
  
  const player2Result = dataManager.createPlayer({
    name: "Mage",
    level: 15,
    experience: 1200,
    health: 80,
    maxHealth: 80,
    position: { x: 10, y: 10, mapId: "forest" },
    inventory: [],
    guild: null
  });
  
  const player3Result = dataManager.createPlayer({
    name: "Tank",
    level: 8,
    experience: 300,
    health: 150,
    maxHealth: 150,
    position: { x: 5, y: 5, mapId: "town" },
    inventory: [],
    guild: null
  });
  
  // 길드 생성
  console.log("\n--- Creating Guild ---");
  const guildResult = dataManager.createGuild({
    name: "Dragon Slayers",
    level: 5,
    memberCount: 0,
    maxMembers: 50
  });
  
  // 플레이어 업데이트
  console.log("\n--- Updating Player ---");
  dataManager.updatePlayer("1", {
    level: 11,
    experience: 600,
    guild: guildResult.data
  });
  
  // 조회
  console.log("\n--- Fetching Player ---");
  const fetchResult = dataManager.getPlayer("1");
  
  if (fetchResult.success) {
    console.log(`Found: ${fetchResult.data.name} (Lv.${fetchResult.data.level})`);
  } else {
    console.error(`Error: ${fetchResult.error}`);
  }
  
  // 존재하지 않는 플레이어
  console.log("\n--- Fetching Non-existent Player ---");
  const notFoundResult = dataManager.getPlayer("999");
  
  if (!notFoundResult.success) {
    console.error(`Error ${notFoundResult.code}: ${notFoundResult.error}`);
  }
  
  // 플레이어 요약
  console.log("\n--- Player Summaries ---");
  const summaries = dataManager.getPlayerSummaries();
  summaries.forEach(s => {
    console.log(`  ${s.name} (Lv.${s.level})`);
  });
  
  // 레벨 검색
  console.log("\n--- Players Level 10-15 ---");
  const midLevelPlayers = dataManager.findPlayersByLevel(10, 15);
  midLevelPlayers.forEach(p => {
    console.log(`  ${p.name}: Level ${p.level}`);
  });
  
  // 통계
  console.log("\n--- Statistics ---");
  const stats = dataManager.getStatistics();
  console.log(`  Total Players: ${stats.players}`);
  console.log(`  Total Guilds: ${stats.guilds}`);
  console.log(`  Average Level: ${stats.averageLevel.toFixed(2)}`);
  
  console.log("\n" + "=".repeat(60));
}

runAdvancedTypeDemo();
```

**실행 결과:**

```
============================================================
Advanced Type Patterns Demo
============================================================

--- Creating Players ---
✅ Created entity: 1
✅ Created entity: 2
✅ Created entity: 3

--- Creating Guild ---
✅ Created entity: 1

--- Updating Player ---
✅ Updated entity: 1

--- Fetching Player ---
Found: Hero (Lv.11)

--- Fetching Non-existent Player ---
❌ Entity not found: 999
Error 404: Player not found

--- Player Summaries ---
  Hero (Lv.11)
  Mage (Lv.15)
  Tank (Lv.8)

--- Players Level 10-15 ---
  Hero: Level 11
  Mage: Level 15

--- Statistics ---
  Total Players: 3
  Total Guilds: 1
  Average Level: 11.33

============================================================
```


## 🎉 Chapter 5 완료!
축하한다! TypeScript의 고급 타입 패턴을 모두 마스터했다.

**지금까지 배운 것:**
- ✅ Partial<T> - 부분 업데이트
- ✅ Required<T> - 필수 필드 강제
- ✅ Pick<T, K> - 필요한 속성만
- ✅ Omit<T, K> - 제외하고 선택
- ✅ Record<K, T> - 딕셔너리 타입
- ✅ typeof, instanceof 타입 가드
- ✅ 사용자 정의 타입 가드
- ✅ in 연산자 타입 가드
- ✅ 맵드 타입 기초
- ✅ 조건부 타입 기초
- ✅ 실전 데이터 관리 시스템


**다음 Chapter 미리보기:**   
Chapter 6에서는 프로젝트 구조화를 배운다. ES6 모듈 시스템, import/export 패턴, 게임 서버의 폴더 구조 설계, 외부 라이브러리 사용 방법 등 실제 프로덕션 환경에서 필요한 모듈 관리 기법을 학습한다.

    

# Chapter 6: 모듈과 네임스페이스

## 6.1 ES6 모듈 시스템
현대적인 TypeScript 프로젝트는 ES6 모듈 시스템을 사용한다. 코드를 여러 파일로 분리하고, 필요한 부분만 가져와 사용하는 방법을 배워본다.

### import/export 패턴

**기본 Export와 Import:**

```typescript
// ========== player.ts ==========
export interface Player {
  id: string;
  name: string;
  level: number;
  health: number;
}

export class PlayerManager {
  private players = new Map<string, Player>();
  
  addPlayer(player: Player): void {
    this.players.set(player.id, player);
  }
  
  getPlayer(id: string): Player | undefined {
    return this.players.get(id);
  }
}

export function createPlayer(name: string, level: number): Player {
  return {
    id: `player_${Date.now()}`,
    name: name,
    level: level,
    health: 100
  };
}

export const MAX_LEVEL = 100;
export const DEFAULT_HEALTH = 100;

// ========== main.ts ==========
import { Player, PlayerManager, createPlayer, MAX_LEVEL } from "./player";

const manager = new PlayerManager();
const player = createPlayer("Hero", 1);

manager.addPlayer(player);
console.log(`Max level: ${MAX_LEVEL}`);
```

**Named Export (권장):**  
여러 항목을 개별적으로 export 한다. 가장 명확하고 tree-shaking에 유리하다.

```typescript
// ========== combat.ts ==========
export interface CombatResult {
  damage: number;
  isCritical: boolean;
  targetDefeated: boolean;
}

export function calculateDamage(attack: number, defense: number): number {
  return Math.max(1, attack - defense);
}

export function isCriticalHit(critRate: number): boolean {
  return Math.random() < critRate;
}

export const CRITICAL_MULTIPLIER = 2.0;
export const BASE_CRIT_RATE = 0.05;

// ========== game.ts ==========
// 1. 개별 import
import { calculateDamage, isCriticalHit, CRITICAL_MULTIPLIER } from "./combat";

// 2. 전체 import (네임스페이스처럼 사용)
import * as Combat from "./combat";

// 사용
const damage = calculateDamage(50, 10);
const crit = Combat.isCriticalHit(0.15);
```

**Default Export:**  
하나의 주요 항목을 export할 때 사용한다. 클래스나 함수 하나가 파일의 전부일 때 적합하다.

```typescript
// ========== Logger.ts ==========
export default class Logger {
  private prefix: string;
  
  constructor(prefix: string) {
    this.prefix = prefix;
  }
  
  log(message: string): void {
    console.log(`[${this.prefix}] ${message}`);
  }
  
  error(message: string): void {
    console.error(`[${this.prefix}] ERROR: ${message}`);
  }
}

// ========== main.ts ==========
// default export는 이름을 자유롭게 지정 가능
import Logger from "./Logger";
import GameLogger from "./Logger";  // 다른 이름으로도 가능

const logger = new Logger("GameServer");
logger.log("Server started");
```

**Named + Default Export 혼합:**

```typescript
// ========== database.ts ==========
export interface DBConfig {
  host: string;
  port: number;
  database: string;
}

// 기본 export
export default class Database {
  constructor(private config: DBConfig) {}
  
  connect(): void {
    console.log(`Connecting to ${this.config.host}:${this.config.port}`);
  }
}

// 추가 named exports
export const DEFAULT_CONFIG: DBConfig = {
  host: "localhost",
  port: 5432,
  database: "gamedb"
};

// ========== main.ts ==========
// default와 named를 함께 import
import Database, { DBConfig, DEFAULT_CONFIG } from "./database";

const db = new Database(DEFAULT_CONFIG);
db.connect();
```

**Re-export (재출력):**  
여러 모듈을 하나로 모아서 export할 수 있다.

```typescript
// ========== entities/Player.ts ==========
export class Player {
  constructor(public name: string) {}
}

// ========== entities/Monster.ts ==========
export class Monster {
  constructor(public name: string) {}
}

// ========== entities/NPC.ts ==========
export class NPC {
  constructor(public name: string) {}
}

// ========== entities/index.ts (Barrel Export) ==========
export { Player } from "./Player";
export { Monster } from "./Monster";
export { NPC } from "./NPC";

// 또는 한 줄로
export * from "./Player";
export * from "./Monster";
export * from "./NPC";

// ========== main.ts ==========
// entities 폴더에서 모든 엔티티를 한 번에 import
import { Player, Monster, NPC } from "./entities";

const player = new Player("Hero");
const monster = new Monster("Goblin");
const npc = new NPC("Merchant");
```

### **실전 예제: 게임 서버 모듈 구조**

```typescript
// ========== types/entities.ts ==========
export interface BaseEntity {
  id: string;
  name: string;
  position: { x: number; y: number };
}

export interface Player extends BaseEntity {
  level: number;
  health: number;
  inventory: string[];
}

export interface Monster extends BaseEntity {
  health: number;
  attackPower: number;
}

// ========== types/packets.ts ==========
export interface BasePacket {
  type: string;
  timestamp: number;
}

export interface MovePacket extends BasePacket {
  type: "move";
  x: number;
  y: number;
}

export interface AttackPacket extends BasePacket {
  type: "attack";
  targetId: string;
}

export type GamePacket = MovePacket | AttackPacket;

// ========== types/index.ts (Barrel) ==========
export * from "./entities";
export * from "./packets";

// ========== managers/PlayerManager.ts ==========
import { Player } from "../types";

export class PlayerManager {
  private players = new Map<string, Player>();
  
  addPlayer(player: Player): void {
    this.players.set(player.id, player);
    console.log(`Player added: ${player.name}`);
  }
  
  getPlayer(id: string): Player | undefined {
    return this.players.get(id);
  }
  
  getAllPlayers(): Player[] {
    return Array.from(this.players.values());
  }
}

// ========== managers/MonsterManager.ts ==========
import { Monster } from "../types";

export class MonsterManager {
  private monsters = new Map<string, Monster>();
  
  spawnMonster(monster: Monster): void {
    this.monsters.set(monster.id, monster);
    console.log(`Monster spawned: ${monster.name}`);
  }
  
  getMonster(id: string): Monster | undefined {
    return this.monsters.get(id);
  }
}

// ========== managers/index.ts ==========
export { PlayerManager } from "./PlayerManager";
export { MonsterManager } from "./MonsterManager";

// ========== handlers/PacketHandler.ts ==========
import { GamePacket, MovePacket, AttackPacket } from "../types";
import { PlayerManager } from "../managers";

export class PacketHandler {
  constructor(private playerManager: PlayerManager) {}
  
  handlePacket(playerId: string, packet: GamePacket): void {
    switch (packet.type) {
      case "move":
        this.handleMove(playerId, packet);
        break;
      case "attack":
        this.handleAttack(playerId, packet);
        break;
    }
  }
  
  private handleMove(playerId: string, packet: MovePacket): void {
    const player = this.playerManager.getPlayer(playerId);
    if (player) {
      player.position.x = packet.x;
      player.position.y = packet.y;
      console.log(`${player.name} moved to (${packet.x}, ${packet.y})`);
    }
  }
  
  private handleAttack(playerId: string, packet: AttackPacket): void {
    console.log(`${playerId} attacks ${packet.targetId}`);
  }
}

// ========== main.ts ==========
import { Player } from "./types";
import { PlayerManager, MonsterManager } from "./managers";
import { PacketHandler } from "./handlers/PacketHandler";

// 게임 서버 초기화
const playerManager = new PlayerManager();
const monsterManager = new MonsterManager();
const packetHandler = new PacketHandler(playerManager);

// 플레이어 생성
const player: Player = {
  id: "p001",
  name: "Hero",
  level: 10,
  health: 100,
  position: { x: 0, y: 0 },
  inventory: []
};

playerManager.addPlayer(player);

// 패킷 처리
packetHandler.handlePacket("p001", {
  type: "move",
  timestamp: Date.now(),
  x: 10,
  y: 20
});
```


## 6.2 프로젝트 구조화

### 게임 서버 폴더 구조 예제
실제 게임 서버 프로젝트의 권장 폴더 구조이다.

```
game-server/
├── src/
│   ├── index.ts                    # 진입점
│   │
│   ├── types/                      # 타입 정의
│   │   ├── index.ts
│   │   ├── entities.ts
│   │   ├── packets.ts
│   │   └── configs.ts
│   │
│   ├── entities/                   # 게임 엔티티
│   │   ├── index.ts
│   │   ├── BaseEntity.ts
│   │   ├── Player.ts
│   │   ├── Monster.ts
│   │   └── NPC.ts
│   │
│   ├── managers/                   # 매니저 클래스
│   │   ├── index.ts
│   │   ├── PlayerManager.ts
│   │   ├── MonsterManager.ts
│   │   ├── RoomManager.ts
│   │   └── InventoryManager.ts
│   │
│   ├── systems/                    # 게임 시스템
│   │   ├── index.ts
│   │   ├── CombatSystem.ts
│   │   ├── QuestSystem.ts
│   │   └── TradeSystem.ts
│   │
│   ├── network/                    # 네트워크 관련
│   │   ├── index.ts
│   │   ├── Server.ts
│   │   ├── PacketHandler.ts
│   │   └── SessionManager.ts
│   │
│   ├── database/                   # 데이터베이스
│   │   ├── index.ts
│   │   ├── Database.ts
│   │   ├── repositories/
│   │   │   ├── PlayerRepository.ts
│   │   │   └── GuildRepository.ts
│   │   └── models/
│   │       ├── PlayerModel.ts
│   │       └── GuildModel.ts
│   │
│   ├── utils/                      # 유틸리티
│   │   ├── index.ts
│   │   ├── Logger.ts
│   │   ├── Validator.ts
│   │   └── Timer.ts
│   │
│   └── config/                     # 설정 파일
│       ├── index.ts
│       ├── database.config.ts
│       └── server.config.ts
│
├── dist/                           # 컴파일된 JavaScript
├── tests/                          # 테스트 파일
│   ├── entities/
│   ├── systems/
│   └── utils/
│
├── tsconfig.json
├── package.json
└── README.md
```

### 실제 구현 예제
각 폴더의 실제 코드 예제이다.

```typescript
// ========== src/types/entities.ts ==========
export interface BaseEntity {
  id: string;
  name: string;
  createdAt: Date;
}

export interface Player extends BaseEntity {
  level: number;
  experience: number;
  health: number;
  maxHealth: number;
}

export interface Monster extends BaseEntity {
  level: number;
  health: number;
  attackPower: number;
}

// ========== src/types/packets.ts ==========
export interface BasePacket {
  type: string;
  playerId: string;
  timestamp: number;
}

export interface MovePacket extends BasePacket {
  type: "move";
  x: number;
  y: number;
}

export interface AttackPacket extends BasePacket {
  type: "attack";
  targetId: string;
}

export type GamePacket = MovePacket | AttackPacket;

// ========== src/types/index.ts (Barrel) ==========
export * from "./entities";
export * from "./packets";

// ========== src/utils/Logger.ts ==========
export enum LogLevel {
  DEBUG,
  INFO,
  WARN,
  ERROR
}

export class Logger {
  private static instance: Logger;
  private minLevel: LogLevel = LogLevel.INFO;
  
  private constructor() {}
  
  static getInstance(): Logger {
    if (!Logger.instance) {
      Logger.instance = new Logger();
    }
    return Logger.instance;
  }
  
  setLevel(level: LogLevel): void {
    this.minLevel = level;
  }
  
  debug(message: string): void {
    if (this.minLevel <= LogLevel.DEBUG) {
      console.log(`[DEBUG] ${message}`);
    }
  }
  
  info(message: string): void {
    if (this.minLevel <= LogLevel.INFO) {
      console.log(`[INFO] ${message}`);
    }
  }
  
  warn(message: string): void {
    if (this.minLevel <= LogLevel.WARN) {
      console.warn(`[WARN] ${message}`);
    }
  }
  
  error(message: string, error?: Error): void {
    if (this.minLevel <= LogLevel.ERROR) {
      console.error(`[ERROR] ${message}`);
      if (error) {
        console.error(error.stack);
      }
    }
  }
}

// 싱글톤 export
export const logger = Logger.getInstance();

// ========== src/utils/index.ts ==========
export { Logger, LogLevel, logger } from "./Logger";

// ========== src/config/server.config.ts ==========
export interface ServerConfig {
  port: number;
  host: string;
  maxConnections: number;
  tickRate: number;
}

export const serverConfig: ServerConfig = {
  port: 8080,
  host: "0.0.0.0",
  maxConnections: 1000,
  tickRate: 60
};

// ========== src/config/database.config.ts ==========
export interface DatabaseConfig {
  host: string;
  port: number;
  database: string;
  username: string;
  password: string;
}

export const databaseConfig: DatabaseConfig = {
  host: process.env.DB_HOST || "localhost",
  port: parseInt(process.env.DB_PORT || "5432"),
  database: process.env.DB_NAME || "gamedb",
  username: process.env.DB_USER || "admin",
  password: process.env.DB_PASS || "password"
};

// ========== src/config/index.ts ==========
export * from "./server.config";
export * from "./database.config";

// ========== src/managers/PlayerManager.ts ==========
import { Player } from "../types";
import { logger } from "../utils";

export class PlayerManager {
  private players = new Map<string, Player>();
  
  addPlayer(player: Player): void {
    this.players.set(player.id, player);
    logger.info(`Player added: ${player.name} (${player.id})`);
  }
  
  removePlayer(id: string): boolean {
    const player = this.players.get(id);
    if (player) {
      this.players.delete(id);
      logger.info(`Player removed: ${player.name} (${id})`);
      return true;
    }
    logger.warn(`Attempted to remove non-existent player: ${id}`);
    return false;
  }
  
  getPlayer(id: string): Player | undefined {
    return this.players.get(id);
  }
  
  getAllPlayers(): Player[] {
    return Array.from(this.players.values());
  }
  
  getPlayerCount(): number {
    return this.players.size;
  }
}

// ========== src/managers/index.ts ==========
export { PlayerManager } from "./PlayerManager";

// ========== src/systems/CombatSystem.ts ==========
import { Player, Monster } from "../types";
import { logger } from "../utils";

export interface CombatResult {
  damage: number;
  isCritical: boolean;
  targetDefeated: boolean;
}

export class CombatSystem {
  private readonly CRITICAL_RATE = 0.1;
  private readonly CRITICAL_MULTIPLIER = 2.0;
  
  calculateDamage(attacker: Player, target: Monster): number {
    const baseDamage = 10 + attacker.level * 2;
    const defense = target.level;
    return Math.max(1, baseDamage - defense);
  }
  
  executeCombat(attacker: Player, target: Monster): CombatResult {
    const baseDamage = this.calculateDamage(attacker, target);
    const isCritical = Math.random() < this.CRITICAL_RATE;
    const finalDamage = isCritical ? baseDamage * this.CRITICAL_MULTIPLIER : baseDamage;
    
    target.health -= finalDamage;
    
    logger.info(
      `${attacker.name} attacks ${target.name} for ${finalDamage} damage${
        isCritical ? " (CRITICAL!)" : ""
      }`
    );
    
    const targetDefeated = target.health <= 0;
    if (targetDefeated) {
      target.health = 0;
      logger.info(`${target.name} has been defeated!`);
    }
    
    return {
      damage: finalDamage,
      isCritical,
      targetDefeated
    };
  }
}

// ========== src/systems/index.ts ==========
export { CombatSystem, CombatResult } from "./CombatSystem";

// ========== src/index.ts (진입점) ==========
import { serverConfig } from "./config";
import { PlayerManager } from "./managers";
import { CombatSystem } from "./systems";
import { logger, LogLevel } from "./utils";
import { Player, Monster } from "./types";

class GameServer {
  private playerManager: PlayerManager;
  private combatSystem: CombatSystem;
  
  constructor() {
    logger.setLevel(LogLevel.DEBUG);
    logger.info("Initializing Game Server...");
    
    this.playerManager = new PlayerManager();
    this.combatSystem = new CombatSystem();
    
    logger.info(`Server configured: Port ${serverConfig.port}`);
  }
  
  start(): void {
    logger.info("Game Server started!");
    logger.info(`Max connections: ${serverConfig.maxConnections}`);
    logger.info(`Tick rate: ${serverConfig.tickRate} Hz`);
    
    // 테스트 시뮬레이션
    this.runSimulation();
  }
  
  private runSimulation(): void {
    logger.info("\n=== Running Test Simulation ===\n");
    
    // 플레이어 생성
    const player: Player = {
      id: "player_001",
      name: "Hero",
      level: 10,
      experience: 500,
      health: 100,
      maxHealth: 100,
      createdAt: new Date()
    };
    
    this.playerManager.addPlayer(player);
    
    // 몬스터 생성
    const monster: Monster = {
      id: "monster_001",
      name: "Goblin",
      level: 5,
      health: 50,
      attackPower: 15,
      createdAt: new Date()
    };
    
    logger.info(`Monster spawned: ${monster.name} (HP: ${monster.health})`);
    
    // 전투 시뮬레이션
    logger.info("\n--- Combat Start ---\n");
    
    let turn = 1;
    while (monster.health > 0 && turn <= 5) {
      logger.info(`Turn ${turn}:`);
      const result = this.combatSystem.executeCombat(player, monster);
      logger.info(`Monster HP: ${monster.health}`);
      
      if (result.targetDefeated) {
        break;
      }
      
      turn++;
    }
    
    logger.info("\n--- Combat End ---");
    logger.info(`Total players: ${this.playerManager.getPlayerCount()}`);
  }
}

// 서버 시작
const server = new GameServer();
server.start();
```

### Barrel Export 패턴
Barrel Export는 여러 모듈을 하나의 진입점(index.ts)으로 모아서 export하는 패턴이다.

**장점:**
- ✅ Import 경로가 간결해짐
- ✅ 내부 구조 변경 시 외부 코드 수정 불필요
- ✅ 공개 API를 명확하게 정의

```typescript
// ❌ Barrel 없이 (복잡한 import)
import { Player } from "./entities/Player";
import { Monster } from "./entities/Monster";
import { NPC } from "./entities/NPC";
import { PlayerManager } from "./managers/PlayerManager";
import { MonsterManager } from "./managers/MonsterManager";

// ✅ Barrel 사용 (간결한 import)
import { Player, Monster, NPC } from "./entities";
import { PlayerManager, MonsterManager } from "./managers";
```

**주의사항:**

```typescript
// ❌ 순환 참조 주의!
// Player.ts에서 Monster를 import하고
// Monster.ts에서 Player를 import하면
// index.ts에서 둘 다 export할 때 문제 발생

// ✅ 해결: 타입만 필요하면 type-only import 사용
import type { Monster } from "./Monster";
```


## 6.3 외부 라이브러리 사용

### @types 패키지 이해
TypeScript는 JavaScript 라이브러리를 사용할 때 타입 정의가 필요하다. `@types` 패키지가 이를 제공한다.  

```bash
# Node.js 내장 모듈 타입
npm install --save-dev @types/node

# Express 라이브러리 타입
npm install express
npm install --save-dev @types/express

# Socket.io 타입
npm install socket.io
npm install --save-dev @types/socket.io
```

**타입 정의 파일 확인:**

```typescript
// node_modules/@types/node/index.d.ts에 정의됨
import * as fs from "fs";
import * as path from "path";

// VSCode가 자동으로 타입을 인식
fs.readFileSync("./data.txt", "utf-8");  // 타입 자동완성 ✅
path.join(__dirname, "config.json");      // 타입 체크 ✅
```

### 실전 예제: Socket.io 게임 서버

```bash
# 패키지 설치
npm install express socket.io
npm install --save-dev @types/express @types/node
```

```typescript
// ========== src/network/Server.ts ==========
import express from "express";
import { createServer } from "http";
import { Server as SocketServer } from "socket.io";
import { logger } from "../utils";
import { GamePacket } from "../types";

export class GameNetworkServer {
  private app = express();
  private httpServer = createServer(this.app);
  private io = new SocketServer(this.httpServer, {
    cors: {
      origin: "*",
      methods: ["GET", "POST"]
    }
  });
  
  constructor(private port: number) {
    this.setupRoutes();
    this.setupSocketHandlers();
  }
  
  private setupRoutes(): void {
    this.app.get("/", (req, res) => {
      res.send("Game Server is running!");
    });
    
    this.app.get("/status", (req, res) => {
      res.json({
        status: "online",
        players: this.io.sockets.sockets.size,
        uptime: process.uptime()
      });
    });
  }
  
  private setupSocketHandlers(): void {
    this.io.on("connection", (socket) => {
      const playerId = socket.id;
      logger.info(`Player connected: ${playerId}`);
      
      // 연결 환영 메시지
      socket.emit("welcome", {
        message: "Welcome to the game!",
        playerId: playerId
      });
      
      // 패킷 수신
      socket.on("packet", (packet: GamePacket) => {
        logger.debug(`Received packet from ${playerId}: ${packet.type}`);
        this.handlePacket(playerId, packet);
      });
      
      // 채팅 메시지
      socket.on("chat", (message: string) => {
        logger.info(`[Chat] ${playerId}: ${message}`);
        
        // 모든 클라이언트에 브로드캐스트
        this.io.emit("chat", {
          playerId: playerId,
          message: message,
          timestamp: Date.now()
        });
      });
      
      // 연결 해제
      socket.on("disconnect", () => {
        logger.info(`Player disconnected: ${playerId}`);
      });
    });
  }
  
  private handlePacket(playerId: string, packet: GamePacket): void {
    // 패킷 처리 로직
    switch (packet.type) {
      case "move":
        // 이동 처리
        this.io.emit("playerMoved", {
          playerId: playerId,
          x: packet.x,
          y: packet.y
        });
        break;
      
      case "attack":
        // 공격 처리
        this.io.emit("playerAttacked", {
          playerId: playerId,
          targetId: packet.targetId
        });
        break;
    }
  }
  
  start(): void {
    this.httpServer.listen(this.port, () => {
      logger.info(`Game server listening on port ${this.port}`);
      logger.info(`WebSocket server ready`);
    });
  }
  
  broadcast(event: string, data: any): void {
    this.io.emit(event, data);
  }
  
  sendTo(playerId: string, event: string, data: any): void {
    this.io.to(playerId).emit(event, data);
  }
}

// ========== src/index.ts ==========
import { GameNetworkServer } from "./network/Server";
import { serverConfig } from "./config";

const server = new GameNetworkServer(serverConfig.port);
server.start();
```

### 타입 정의 파일 (.d.ts)
외부 JavaScript 라이브러리에 타입 정의가 없다면, 직접 만들 수 있다.

```typescript
// ========== src/types/custom-lib.d.ts ==========
// 타입 정의가 없는 'some-game-library' 라이브러리를 위한 선언

declare module "some-game-library" {
  export interface GameConfig {
    width: number;
    height: number;
    fps: number;
  }
  
  export class GameEngine {
    constructor(config: GameConfig);
    start(): void;
    stop(): void;
    update(deltaTime: number): void;
  }
  
  export function createGame(config: GameConfig): GameEngine;
}

// ========== 사용 ==========
import { GameEngine, createGame } from "some-game-library";

const engine = createGame({
  width: 800,
  height: 600,
  fps: 60
});

engine.start();
```

**전역 타입 확장:**

```typescript
// ========== src/types/global.d.ts ==========
// Node.js의 전역 객체 확장
declare global {
  namespace NodeJS {
    interface ProcessEnv {
      NODE_ENV: "development" | "production" | "test";
      PORT: string;
      DB_HOST: string;
      DB_PORT: string;
      DB_NAME: string;
      DB_USER: string;
      DB_PASS: string;
    }
  }
}

export {};  // 모듈로 만들기 위해 필요

// ========== 사용 ==========
// 이제 process.env가 타입 안전
const port = parseInt(process.env.PORT);  // ✅ 자동완성
const dbHost = process.env.DB_HOST;       // ✅ 타입 체크
```

### tsconfig.json의 타입 관련 설정  

```json
{
  "compilerOptions": {
    "types": [
      "node",
      "express",
      "socket.io"
    ],
    "typeRoots": [
      "./node_modules/@types",
      "./src/types"
    ]
  }
}
```


## 🎯 Chapter 6 실전 종합 예제
지금까지 배운 모듈 시스템을 활용한 완전한 게임 서버 프로젝트이다.

```typescript
// ========== src/types/index.ts ==========
export interface Player {
  id: string;
  name: string;
  level: number;
  position: { x: number; y: number };
}

export interface Room {
  id: string;
  name: string;
  players: Set<string>;
  maxPlayers: number;
}

export interface ChatMessage {
  playerId: string;
  playerName: string;
  message: string;
  timestamp: number;
}

// ========== src/utils/IdGenerator.ts ==========
export class IdGenerator {
  private counters = new Map<string, number>();
  
  generate(prefix: string): string {
    const count = this.counters.get(prefix) || 0;
    this.counters.set(prefix, count + 1);
    return `${prefix}_${count + 1}`;
  }
}

export const idGenerator = new IdGenerator();

// ========== src/utils/index.ts ==========
export { IdGenerator, idGenerator } from "./IdGenerator";
export { logger, LogLevel } from "./Logger";

// ========== src/managers/RoomManager.ts ==========
import { Room } from "../types";
import { logger } from "../utils";

export class RoomManager {
  private rooms = new Map<string, Room>();
  
  createRoom(id: string, name: string, maxPlayers: number): Room {
    const room: Room = {
      id,
      name,
      players: new Set(),
      maxPlayers
    };
    
    this.rooms.set(id, room);
    logger.info(`Room created: ${name} (${id})`);
    
    return room;
  }
  
  getRoom(id: string): Room | undefined {
    return this.rooms.get(id);
  }
  
  joinRoom(roomId: string, playerId: string): boolean {
    const room = this.rooms.get(roomId);
    
    if (!room) {
      logger.warn(`Room not found: ${roomId}`);
      return false;
    }
    
    if (room.players.size >= room.maxPlayers) {
      logger.warn(`Room full: ${roomId}`);
      return false;
    }
    
    room.players.add(playerId);
    logger.info(`Player ${playerId} joined room ${roomId}`);
    
    return true;
  }
  
  leaveRoom(roomId: string, playerId: string): boolean {
    const room = this.rooms.get(roomId);
    
    if (!room) {
      return false;
    }
    
    const removed = room.players.delete(playerId);
    if (removed) {
      logger.info(`Player ${playerId} left room ${roomId}`);
    }
    
    return removed;
  }
  
  getRoomPlayers(roomId: string): string[] {
    const room = this.rooms.get(roomId);
    return room ? Array.from(room.players) : [];
  }
  
  getAllRooms(): Room[] {
    return Array.from(this.rooms.values());
  }
}

// ========== src/managers/PlayerManager.ts ==========
import { Player } from "../types";
import { logger } from "../utils";

export class PlayerManager {
  private players = new Map<string, Player>();
  private playerRooms = new Map<string, string>();  // playerId -> roomId
  
  addPlayer(player: Player): void {
    this.players.set(player.id, player);
    logger.info(`Player registered: ${player.name} (${player.id})`);
  }
  
  removePlayer(id: string): void {
    const player = this.players.get(id);
    if (player) {
      this.players.delete(id);
      this.playerRooms.delete(id);
      logger.info(`Player unregistered: ${player.name} (${id})`);
    }
  }
  
  getPlayer(id: string): Player | undefined {
    return this.players.get(id);
  }
  
  setPlayerRoom(playerId: string, roomId: string): void {
    this.playerRooms.set(playerId, roomId);
  }
  
  getPlayerRoom(playerId: string): string | undefined {
    return this.playerRooms.get(playerId);
  }
  
  getAllPlayers(): Player[] {
    return Array.from(this.players.values());
  }
}

// ========== src/managers/index.ts ==========
export { PlayerManager } from "./PlayerManager";
export { RoomManager } from "./RoomManager";

// ========== src/services/ChatService.ts ==========
import { ChatMessage } from "../types";
import { PlayerManager } from "../managers";
import { logger } from "../utils";

export class ChatService {
  private chatHistory: ChatMessage[] = [];
  private readonly MAX_HISTORY = 100;
  
  constructor(private playerManager: PlayerManager) {}
  
  sendMessage(playerId: string, message: string): ChatMessage | null {
    const player = this.playerManager.getPlayer(playerId);
    
    if (!player) {
      logger.warn(`Cannot send message: Player ${playerId} not found`);
      return null;
    }
    
    const chatMessage: ChatMessage = {
      playerId: player.id,
      playerName: player.name,
      message: message,
      timestamp: Date.now()
    };
    
    this.chatHistory.push(chatMessage);
    
    // 히스토리 크기 제한
    if (this.chatHistory.length > this.MAX_HISTORY) {
      this.chatHistory.shift();
    }
    
    logger.info(`[Chat] ${player.name}: ${message}`);
    
    return chatMessage;
  }
  
  getChatHistory(count: number = 50): ChatMessage[] {
    return this.chatHistory.slice(-count);
  }
}

// ========== src/services/index.ts ==========
export { ChatService } from "./ChatService";

// ========== src/GameServer.ts ==========
import { Player } from "./types";
import { PlayerManager, RoomManager } from "./managers";
import { ChatService } from "./services";
import { logger, idGenerator } from "./utils";

export class GameServer {
  private playerManager: PlayerManager;
  private roomManager: RoomManager;
  private chatService: ChatService;
  
  constructor() {
    logger.info("=".repeat(60));
    logger.info("Initializing Game Server");
    logger.info("=".repeat(60));
    
    this.playerManager = new PlayerManager();
    this.roomManager = new RoomManager();
    this.chatService = new ChatService(this.playerManager);
    
    this.setupDefaultRooms();
  }
  
  private setupDefaultRooms(): void {
    this.roomManager.createRoom("lobby", "Lobby", 100);
    this.roomManager.createRoom("room1", "Battle Arena 1", 10);
    this.roomManager.createRoom("room2", "Battle Arena 2", 10);
  }
  
  start(): void {
    logger.info("\nGame Server Started!");
    logger.info("-".repeat(60));
    
    // 시뮬레이션 실행
    this.runSimulation();
  }
  
  private runSimulation(): void {
    logger.info("\n=== Running Simulation ===\n");
    
    // 플레이어 생성
    const player1: Player = {
      id: idGenerator.generate("player"),
      name: "Hero",
      level: 10,
      position: { x: 0, y: 0 }
    };
    
    const player2: Player = {
      id: idGenerator.generate("player"),
      name: "Warrior",
      level: 8,
      position: { x: 10, y: 10 }
    };
    
    const player3: Player = {
      id: idGenerator.generate("player"),
      name: "Mage",
      level: 12,
      position: { x: 5, y: 5 }
    };
    
    // 플레이어 등록
    this.playerManager.addPlayer(player1);
    this.playerManager.addPlayer(player2);
    this.playerManager.addPlayer(player3);
    
    logger.info("\n--- Players Joining Rooms ---\n");
    
    // 로비 입장
    this.roomManager.joinRoom("lobby", player1.id);
    this.roomManager.joinRoom("lobby", player2.id);
    this.roomManager.joinRoom("lobby", player3.id);
    
    logger.info("\n--- Chat Messages ---\n");
    
    // 채팅
    this.chatService.sendMessage(player1.id, "Hello everyone!");
    this.chatService.sendMessage(player2.id, "Hi! Ready for battle?");
    this.chatService.sendMessage(player3.id, "Let's go to Arena 1!");
    
    logger.info("\n--- Moving to Battle Arena ---\n");
    
    // 배틀 아레나로 이동
    this.roomManager.leaveRoom("lobby", player1.id);
    this.roomManager.leaveRoom("lobby", player2.id);
    
    this.roomManager.joinRoom("room1", player1.id);
    this.roomManager.joinRoom("room1", player2.id);
    
    logger.info("\n--- Room Status ---\n");
    
    // 방 상태 출력
    const rooms = this.roomManager.getAllRooms();
    rooms.forEach(room => {
      const playerCount = room.players.size;
      logger.info(
        `${room.name}: ${playerCount}/${room.maxPlayers} players`
      );
    });
    
    logger.info("\n--- Chat History ---\n");
    
    // 채팅 히스토리
    const history = this.chatService.getChatHistory(10);
    history.forEach(msg => {
      const time = new Date(msg.timestamp).toLocaleTimeString();
      logger.info(`[${time}] ${msg.playerName}: ${msg.message}`);
    });
    
    logger.info("\n=== Simulation Complete ===");
    logger.info("=".repeat(60));
  }
}

// ========== src/index.ts ==========
import { GameServer } from "./GameServer";

const server = new GameServer();
server.start();
```

**package.json 설정:**

```json
{
  "name": "game-server",
  "version": "1.0.0",
  "main": "dist/index.js",
  "scripts": {
    "build": "tsc",
    "start": "node dist/index.js",
    "dev": "ts-node src/index.ts",
    "watch": "nodemon"
  },
  "dependencies": {
    "express": "^4.18.2",
    "socket.io": "^4.6.0"
  },
  "devDependencies": {
    "@types/express": "^4.17.17",
    "@types/node": "^20.0.0",
    "nodemon": "^3.0.1",
    "ts-node": "^10.9.1",
    "typescript": "^5.9.0"
  }
}
```

**실행:**

```bash
npm run dev
```

**실행 결과:**

```
============================================================
Initializing Game Server
============================================================
[INFO] Room created: Lobby (lobby)
[INFO] Room created: Battle Arena 1 (room1)
[INFO] Room created: Battle Arena 2 (room2)

Game Server Started!
------------------------------------------------------------

=== Running Simulation ===

[INFO] Player registered: Hero (player_1)
[INFO] Player registered: Warrior (player_2)
[INFO] Player registered: Mage (player_3)

--- Players Joining Rooms ---

[INFO] Player player_1 joined room lobby
[INFO] Player player_2 joined room lobby
[INFO] Player player_3 joined room lobby

--- Chat Messages ---

[INFO] [Chat] Hero: Hello everyone!
[INFO] [Chat] Warrior: Hi! Ready for battle?
[INFO] [Chat] Mage: Let's go to Arena 1!

--- Moving to Battle Arena ---

[INFO] Player player_1 left room lobby
[INFO] Player player_2 left room lobby
[INFO] Player player_1 joined room room1
[INFO] Player player_2 joined room room1

--- Room Status ---

[INFO] Lobby: 1/100 players
[INFO] Battle Arena 1: 2/10 players
[INFO] Battle Arena 2: 0/10 players

--- Chat History ---

[INFO] [오후 3:45:21] Hero: Hello everyone!
[INFO] [오후 3:45:21] Warrior: Hi! Ready for battle?
[INFO] [오후 3:45:21] Mage: Let's go to Arena 1!

=== Simulation Complete ===
============================================================
```


## 🎉 Chapter 6 완료!   
축하한다! TypeScript 모듈 시스템과 프로젝트 구조화를 완벽하게 마스터했다.

**지금까지 배운 것:**
- ✅ ES6 모듈 시스템 (import/export)
- ✅ Named Export vs Default Export
- ✅ Re-export (Barrel Pattern)
- ✅ 게임 서버 폴더 구조 설계
- ✅ 모듈 간 의존성 관리
- ✅ @types 패키지 사용법
- ✅ 타입 정의 파일 (.d.ts)
- ✅ 외부 라이브러리 통합
- ✅ 완전한 게임 서버 프로젝트 구조
  
  
**다음 단계:**  
이제 TypeScript의 핵심을 모두 학습했다! 남은 Chapter 7-10은 실전 응용에 초점을 맞춘다:
  

  
# Chapter 7: 실전 게임 서버 패턴
게임 서버 개발자라면 TypeScript의 진가는 실제 프로덕션 환경에서 드러난다. 타입 안전성은 런타임 에러를 컴파일 타임에 잡아내고, 팀원들과의 협업에서 명확한 계약(Contract)을 제공한다. 이 챕터에서는 실제 게임 서버에서 자주 마주치는 패턴들을 TypeScript로 어떻게 안전하게 구현하는지 살펴본다.


## 7.1 WebSocket 서버 구현
게임 서버의 핵심은 실시간 양방향 통신이다. HTTP 폴링이나 롱폴링 대신 WebSocket을 사용하면 낮은 지연시간과 효율적인 리소스 사용이 가능하다. 여기서는 Socket.io와 TypeScript를 결합해 타입 안전한 실시간 통신 시스템을 구축한다.

### Socket.io + TypeScript 설정
먼저 필요한 패키지를 설치한다:

```bash
npm install socket.io
npm install -D @types/node
```

기본적인 Socket.io 서버는 다음과 같이 구성된다:

```typescript
import { Server } from 'socket.io';
import { createServer } from 'http';

const httpServer = createServer();
const io = new Server(httpServer, {
  cors: {
    origin: "*",
    methods: ["GET", "POST"]
  }
});

const PORT = 3000;

io.on('connection', (socket) => {
  console.log(`클라이언트 연결: ${socket.id}`);
  
  socket.on('disconnect', () => {
    console.log(`클라이언트 연결 해제: ${socket.id}`);
  });
});

httpServer.listen(PORT, () => {
  console.log(`게임 서버 시작: 포트 ${PORT}`);
});
```

하지만 이 코드는 타입 안전성이 없다. 이벤트 이름을 오타내도, 잘못된 데이터를 전송해도 컴파일러가 잡아내지 못한다. 실제 프로덕션에서는 치명적이다.

### 타입 안전한 이벤트 핸들링
Socket.io v4부터는 제네릭을 통해 이벤트 타입을 정의할 수 있다. 먼저 클라이언트와 서버 간 주고받을 이벤트를 인터페이스로 정의한다:

```typescript
// types/socket.ts
export interface ServerToClientEvents {
  playerJoined: (data: { playerId: string; nickname: string }) => void;
  playerLeft: (data: { playerId: string }) => void;
  gameStateUpdate: (state: GameState) => void;
  chatMessage: (message: ChatMessage) => void;
  error: (error: ErrorResponse) => void;
}

export interface ClientToServerEvents {
  joinRoom: (data: { roomId: string; nickname: string }) => void;
  leaveRoom: () => void;
  sendChatMessage: (message: string) => void;
  playerAction: (action: PlayerAction) => void;
}

export interface InterServerEvents {
  ping: () => void;
}

export interface SocketData {
  playerId: string;
  roomId?: string;
  nickname: string;
}
```

게임에 필요한 데이터 타입들도 정의한다:

```typescript
// types/game.ts
export interface GameState {
  roomId: string;
  players: Player[];
  status: 'waiting' | 'playing' | 'finished';
  currentTurn?: string;
  timer: number;
}

export interface Player {
  id: string;
  nickname: string;
  position: { x: number; y: number };
  hp: number;
  maxHp: number;
  level: number;
}

export interface PlayerAction {
  type: 'move' | 'attack' | 'useSkill';
  targetPosition?: { x: number; y: number };
  targetPlayerId?: string;
  skillId?: number;
}

export interface ChatMessage {
  senderId: string;
  senderNickname: string;
  message: string;
  timestamp: number;
}

export interface ErrorResponse {
  code: string;
  message: string;
}
```

이제 타입이 적용된 서버를 구현한다:

```typescript
// server.ts
import { Server, Socket } from 'socket.io';
import { createServer } from 'http';
import {
  ServerToClientEvents,
  ClientToServerEvents,
  InterServerEvents,
  SocketData
} from './types/socket';

const httpServer = createServer();
const io = new Server<
  ClientToServerEvents,
  ServerToClientEvents,
  InterServerEvents,
  SocketData
>(httpServer, {
  cors: { origin: "*", methods: ["GET", "POST"] }
});

type TypedSocket = Socket<
  ClientToServerEvents,
  ServerToClientEvents,
  InterServerEvents,
  SocketData
>;

io.on('connection', (socket: TypedSocket) => {
  console.log(`클라이언트 연결: ${socket.id}`);
  
  // 타입 안전한 이벤트 핸들러
  socket.on('joinRoom', (data) => {
    // data는 자동으로 { roomId: string; nickname: string } 타입
    const { roomId, nickname } = data;
    
    // 소켓 데이터 저장
    socket.data.playerId = socket.id;
    socket.data.roomId = roomId;
    socket.data.nickname = nickname;
    
    socket.join(roomId);
    
    // 타입 안전한 이벤트 발행
    io.to(roomId).emit('playerJoined', {
      playerId: socket.id,
      nickname: nickname
    });
    
    console.log(`${nickname}님이 방 ${roomId}에 입장`);
  });
  
  socket.on('sendChatMessage', (message) => {
    // message는 자동으로 string 타입
    const roomId = socket.data.roomId;
    
    if (!roomId) {
      socket.emit('error', {
        code: 'NOT_IN_ROOM',
        message: '방에 입장하지 않았다'
      });
      return;
    }
    
    const chatMessage: ChatMessage = {
      senderId: socket.data.playerId,
      senderNickname: socket.data.nickname,
      message: message,
      timestamp: Date.now()
    };
    
    io.to(roomId).emit('chatMessage', chatMessage);
  });
  
  socket.on('playerAction', (action) => {
    // action은 자동으로 PlayerAction 타입
    handlePlayerAction(socket, action);
  });
  
  socket.on('disconnect', () => {
    const roomId = socket.data.roomId;
    if (roomId) {
      io.to(roomId).emit('playerLeft', {
        playerId: socket.id
      });
    }
    console.log(`클라이언트 연결 해제: ${socket.id}`);
  });
});

function handlePlayerAction(socket: TypedSocket, action: PlayerAction) {
  const roomId = socket.data.roomId;
  if (!roomId) return;
  
  // 액션 타입에 따른 분기 처리
  switch (action.type) {
    case 'move':
      if (!action.targetPosition) {
        socket.emit('error', {
          code: 'INVALID_ACTION',
          message: '이동 위치가 지정되지 않았다'
        });
        return;
      }
      // 이동 로직 처리
      console.log(`${socket.data.nickname} 이동: (${action.targetPosition.x}, ${action.targetPosition.y})`);
      break;
      
    case 'attack':
      if (!action.targetPlayerId) {
        socket.emit('error', {
          code: 'INVALID_ACTION',
          message: '공격 대상이 지정되지 않았다'
        });
        return;
      }
      // 공격 로직 처리
      console.log(`${socket.data.nickname}이(가) ${action.targetPlayerId} 공격`);
      break;
      
    case 'useSkill':
      if (!action.skillId) {
        socket.emit('error', {
          code: 'INVALID_ACTION',
          message: '스킬이 지정되지 않았다'
        });
        return;
      }
      // 스킬 사용 로직 처리
      console.log(`${socket.data.nickname}이(가) 스킬 ${action.skillId} 사용`);
      break;
  }
}

httpServer.listen(3000, () => {
  console.log('게임 서버 시작: 포트 3000');
});
```

이제 VSCode에서 이벤트 이름을 입력할 때 자동완성이 제공되고, 잘못된 데이터 타입을 전달하면 컴파일 에러가 발생한다. 런타임 에러를 사전에 방지할 수 있다.

```
┌──────────────────────────────────────────────────┐
│         타입 안전한 Socket 통신 흐름                │
├──────────────────────────────────────────────────┤
│                                                  │
│  Client                    Server                │
│    │                         │                   │
│    │──joinRoom──────────────>│                   │
│    │  {roomId, nickname}     │                   │
│    │                         │                   │
│    │<─playerJoined───────────│                   │
│    │  {playerId, nickname}   │                   │
│    │                         │                   │
│    │──playerAction──────────>│                   │
│    │  {type, target...}      │                   │
│    │                         │                   │
│    │<─gameStateUpdate────────│                   │
│    │  {players, status...}   │                   │
│    │                         │                   │
│         ✓ 모든 데이터 타입 검증됨                   │
└──────────────────────────────────────────────────┘
```


## 7.2 상태 관리 패턴
게임 서버에서 상태 관리는 핵심이다. 여러 플레이어가 동시에 접속하고, 각자 다른 방(Room)에서 게임을 진행한다. TypeScript를 활용하면 복잡한 상태를 안전하게 관리할 수 있다.

### Room 관리 시스템
게임 방은 플레이어들이 모여 게임을 진행하는 독립적인 공간이다. 각 방은 고유한 상태를 가지며, 플레이어 입장/퇴장, 게임 시작/종료 등의 생명주기를 관리한다.

```typescript
// managers/RoomManager.ts
import { GameState, Player } from '../types/game';

export class Room {
  private readonly id: string;
  private readonly maxPlayers: number;
  private players: Map<string, Player>;
  private gameState: GameState;
  private gameLoopInterval?: NodeJS.Timeout;

  constructor(roomId: string, maxPlayers: number = 4) {
    this.id = roomId;
    this.maxPlayers = maxPlayers;
    this.players = new Map();
    this.gameState = {
      roomId: roomId,
      players: [],
      status: 'waiting',
      timer: 0
    };
  }

  // 플레이어 추가
  addPlayer(playerId: string, nickname: string): boolean {
    if (this.players.size >= this.maxPlayers) {
      return false;
    }

    if (this.players.has(playerId)) {
      return false;
    }

    const player: Player = {
      id: playerId,
      nickname: nickname,
      position: { x: 0, y: 0 },
      hp: 100,
      maxHp: 100,
      level: 1
    };

    this.players.set(playerId, player);
    this.updateGameState();
    return true;
  }

  // 플레이어 제거
  removePlayer(playerId: string): boolean {
    const removed = this.players.delete(playerId);
    
    if (removed) {
      this.updateGameState();
      
      // 방이 비었으면 게임 중지
      if (this.players.size === 0) {
        this.stopGame();
      }
    }
    
    return removed;
  }

  // 플레이어 정보 업데이트
  updatePlayer(playerId: string, updates: Partial<Player>): boolean {
    const player = this.players.get(playerId);
    
    if (!player) {
      return false;
    }

    // 타입 안전하게 업데이트
    Object.assign(player, updates);
    this.players.set(playerId, player);
    this.updateGameState();
    return true;
  }

  // 게임 시작
  startGame(): boolean {
    if (this.players.size < 2) {
      return false;
    }

    if (this.gameState.status === 'playing') {
      return false;
    }

    this.gameState.status = 'playing';
    this.gameState.currentTurn = Array.from(this.players.keys())[0];
    this.gameState.timer = 30;

    // 게임 루프 시작 (1초마다 실행)
    this.gameLoopInterval = setInterval(() => {
      this.gameLoop();
    }, 1000);

    return true;
  }

  // 게임 중지
  stopGame(): void {
    if (this.gameLoopInterval) {
      clearInterval(this.gameLoopInterval);
      this.gameLoopInterval = undefined;
    }

    this.gameState.status = 'finished';
    this.updateGameState();
  }

  // 게임 루프
  private gameLoop(): void {
    if (this.gameState.status !== 'playing') {
      return;
    }

    this.gameState.timer--;

    // 턴 타이머가 끝나면 다음 플레이어로 턴 넘김
    if (this.gameState.timer <= 0) {
      this.nextTurn();
    }

    this.updateGameState();
  }

  // 다음 턴으로 전환
  private nextTurn(): void {
    const playerIds = Array.from(this.players.keys());
    const currentIndex = playerIds.indexOf(this.gameState.currentTurn || '');
    const nextIndex = (currentIndex + 1) % playerIds.length;
    
    this.gameState.currentTurn = playerIds[nextIndex];
    this.gameState.timer = 30;
  }

  // 게임 상태 업데이트
  private updateGameState(): void {
    this.gameState.players = Array.from(this.players.values());
  }

  // Getter 메서드들
  getId(): string {
    return this.id;
  }

  getPlayers(): Player[] {
    return Array.from(this.players.values());
  }

  getGameState(): GameState {
    return { ...this.gameState };
  }

  getPlayerCount(): number {
    return this.players.size;
  }

  isFull(): boolean {
    return this.players.size >= this.maxPlayers;
  }

  isEmpty(): boolean {
    return this.players.size === 0;
  }

  hasPlayer(playerId: string): boolean {
    return this.players.has(playerId);
  }
}
```

이제 전체 방들을 관리하는 매니저 클래스를 구현한다:

```typescript
// managers/RoomManager.ts (계속)
export class RoomManager {
  private static instance: RoomManager;
  private rooms: Map<string, Room>;

  private constructor() {
    this.rooms = new Map();
  }

  // 싱글톤 패턴
  static getInstance(): RoomManager {
    if (!RoomManager.instance) {
      RoomManager.instance = new RoomManager();
    }
    return RoomManager.instance;
  }

  // 방 생성
  createRoom(roomId: string, maxPlayers: number = 4): Room | null {
    if (this.rooms.has(roomId)) {
      return null;
    }

    const room = new Room(roomId, maxPlayers);
    this.rooms.set(roomId, room);
    return room;
  }

  // 방 가져오기
  getRoom(roomId: string): Room | undefined {
    return this.rooms.get(roomId);
  }

  // 방 삭제
  deleteRoom(roomId: string): boolean {
    const room = this.rooms.get(roomId);
    
    if (room) {
      room.stopGame();
      return this.rooms.delete(roomId);
    }
    
    return false;
  }

  // 자동으로 빈 방 삭제
  cleanupEmptyRooms(): void {
    for (const [roomId, room] of this.rooms.entries()) {
      if (room.isEmpty()) {
        this.deleteRoom(roomId);
        console.log(`빈 방 제거: ${roomId}`);
      }
    }
  }

  // 입장 가능한 방 찾기
  findAvailableRoom(): Room | null {
    for (const room of this.rooms.values()) {
      if (!room.isFull() && room.getGameState().status === 'waiting') {
        return room;
      }
    }
    return null;
  }

  // 모든 방 목록
  getAllRooms(): Room[] {
    return Array.from(this.rooms.values());
  }

  // 방 개수
  getRoomCount(): number {
    return this.rooms.size;
  }
}
```

서버에 통합한다:

```typescript
// server.ts에 추가
import { RoomManager } from './managers/RoomManager';

const roomManager = RoomManager.getInstance();

// 10분마다 빈 방 정리
setInterval(() => {
  roomManager.cleanupEmptyRooms();
}, 10 * 60 * 1000);

// Socket 이벤트 핸들러에 추가
socket.on('joinRoom', (data) => {
  const { roomId, nickname } = data;
  
  let room = roomManager.getRoom(roomId);
  
  // 방이 없으면 생성
  if (!room) {
    room = roomManager.createRoom(roomId);
    if (!room) {
      socket.emit('error', {
        code: 'ROOM_CREATE_FAILED',
        message: '방 생성에 실패했다'
      });
      return;
    }
  }
  
  // 방이 가득 찼는지 확인
  if (room.isFull()) {
    socket.emit('error', {
      code: 'ROOM_FULL',
      message: '방이 가득 찼다'
    });
    return;
  }
  
  // 플레이어 추가
  const added = room.addPlayer(socket.id, nickname);
  
  if (!added) {
    socket.emit('error', {
      code: 'JOIN_FAILED',
      message: '방 입장에 실패했다'
    });
    return;
  }
  
  socket.data.playerId = socket.id;
  socket.data.roomId = roomId;
  socket.data.nickname = nickname;
  socket.join(roomId);
  
  // 방의 모든 플레이어에게 알림
  io.to(roomId).emit('playerJoined', {
    playerId: socket.id,
    nickname: nickname
  });
  
  // 게임 상태 전송
  io.to(roomId).emit('gameStateUpdate', room.getGameState());
  
  console.log(`${nickname}님이 방 ${roomId}에 입장 (${room.getPlayerCount()}/${4}명)`);
});
```

### Player Session 관리
플레이어 세션은 연결 상태, 인증 정보, 임시 데이터를 관리한다. 네트워크 끊김이나 재접속을 처리할 때 필수적이다.

```typescript
// managers/SessionManager.ts
export interface PlayerSession {
  playerId: string;
  socketId: string;
  nickname: string;
  roomId?: string;
  connectedAt: number;
  lastActivityAt: number;
  isAuthenticated: boolean;
  userData?: {
    level: number;
    exp: number;
    gold: number;
  };
}

export class SessionManager {
  private static instance: SessionManager;
  private sessions: Map<string, PlayerSession>;
  private socketToPlayer: Map<string, string>; // socketId -> playerId 매핑

  private constructor() {
    this.sessions = new Map();
    this.socketToPlayer = new Map();
  }

  static getInstance(): SessionManager {
    if (!SessionManager.instance) {
      SessionManager.instance = new SessionManager();
    }
    return SessionManager.instance;
  }

  // 세션 생성
  createSession(playerId: string, socketId: string, nickname: string): PlayerSession {
    const session: PlayerSession = {
      playerId: playerId,
      socketId: socketId,
      nickname: nickname,
      connectedAt: Date.now(),
      lastActivityAt: Date.now(),
      isAuthenticated: false
    };

    this.sessions.set(playerId, session);
    this.socketToPlayer.set(socketId, playerId);
    
    return session;
  }

  // 세션 가져오기
  getSession(playerId: string): PlayerSession | undefined {
    return this.sessions.get(playerId);
  }

  // Socket ID로 세션 가져오기
  getSessionBySocketId(socketId: string): PlayerSession | undefined {
    const playerId = this.socketToPlayer.get(socketId);
    return playerId ? this.sessions.get(playerId) : undefined;
  }

  // 세션 업데이트
  updateSession(playerId: string, updates: Partial<PlayerSession>): boolean {
    const session = this.sessions.get(playerId);
    
    if (!session) {
      return false;
    }

    Object.assign(session, updates);
    session.lastActivityAt = Date.now();
    this.sessions.set(playerId, session);
    
    return true;
  }

  // 활동 시간 갱신
  updateActivity(playerId: string): void {
    const session = this.sessions.get(playerId);
    if (session) {
      session.lastActivityAt = Date.now();
    }
  }

  // 세션 삭제
  removeSession(playerId: string): boolean {
    const session = this.sessions.get(playerId);
    
    if (session) {
      this.socketToPlayer.delete(session.socketId);
      return this.sessions.delete(playerId);
    }
    
    return false;
  }

  // 오래된 세션 정리 (30분 이상 비활성)
  cleanupInactiveSessions(inactiveTimeout: number = 30 * 60 * 1000): number {
    const now = Date.now();
    let cleanedCount = 0;

    for (const [playerId, session] of this.sessions.entries()) {
      if (now - session.lastActivityAt > inactiveTimeout) {
        this.removeSession(playerId);
        cleanedCount++;
        console.log(`비활성 세션 제거: ${session.nickname} (${playerId})`);
      }
    }

    return cleanedCount;
  }

  // 모든 세션 수
  getSessionCount(): number {
    return this.sessions.size;
  }

  // 인증된 세션 수
  getAuthenticatedSessionCount(): number {
    let count = 0;
    for (const session of this.sessions.values()) {
      if (session.isAuthenticated) {
        count++;
      }
    }
    return count;
  }
}
```

이제 서버에 세션 관리를 통합한다:

```typescript
// server.ts에 추가
import { SessionManager } from './managers/SessionManager';

const sessionManager = SessionManager.getInstance();

io.on('connection', (socket: TypedSocket) => {
  // 세션 생성
  const session = sessionManager.createSession(
    socket.id,
    socket.id,
    'Guest_' + socket.id.substring(0, 6)
  );

  console.log(`새 세션 생성: ${session.playerId}`);

  // 모든 이벤트에서 활동 시간 갱신
  socket.use((_, next) => {
    sessionManager.updateActivity(socket.id);
    next();
  });

  socket.on('disconnect', () => {
    const session = sessionManager.getSessionBySocketId(socket.id);
    
    if (session && session.roomId) {
      const room = roomManager.getRoom(session.roomId);
      if (room) {
        room.removePlayer(session.playerId);
        io.to(session.roomId).emit('playerLeft', {
          playerId: session.playerId
        });
      }
    }

    sessionManager.removeSession(socket.id);
    console.log(`세션 종료: ${socket.id}`);
  });
});

// 5분마다 비활성 세션 정리
setInterval(() => {
  const cleaned = sessionManager.cleanupInactiveSessions();
  if (cleaned > 0) {
    console.log(`${cleaned}개의 비활성 세션 정리됨`);
  }
}, 5 * 60 * 1000);
```

```
┌──────────────────────────────────────────────────┐
│          Room & Session 관리 구조                 │
├──────────────────────────────────────────────────┤
│                                                   │
│  RoomManager (Singleton)                          │
│  ┌─────────────────────────────────────────┐    │
│  │  rooms: Map<roomId, Room>               │    │
│  │                                          │    │
│  │  Room #1                 Room #2         │    │
│  │  ├─ players: Map         ├─ players     │    │
│  │  ├─ gameState            ├─ gameState   │    │
│  │  └─ gameLoop             └─ gameLoop    │    │
│  └─────────────────────────────────────────┘    │
│                                                   │
│  SessionManager (Singleton)                       │
│  ┌─────────────────────────────────────────┐    │
│  │  sessions: Map<playerId, Session>       │    │
│  │  socketToPlayer: Map<socketId, playerId>│    │
│  │                                          │    │
│  │  Session #1              Session #2      │    │
│  │  ├─ playerId            ├─ playerId     │    │
│  │  ├─ socketId            ├─ socketId     │    │
│  │  ├─ nickname            ├─ nickname     │    │
│  │  └─ roomId              └─ roomId       │    │
│  └─────────────────────────────────────────┘    │
│                                                   │
│  ✓ 타입 안전한 상태 관리                          │
│  ✓ 자동 정리 메커니즘                             │
│  ✓ 싱글톤 패턴으로 전역 접근                      │
└──────────────────────────────────────────────────┘
```


## 7.3 데이터베이스 연동
게임 서버에서 플레이어 데이터, 아이템, 랭킹 등은 영구 저장이 필요하다. TypeScript와 ORM을 결합하면 타입 안전한 데이터베이스 작업이 가능하다. 여기서는 Prisma를 사용한다. Prisma는 스키마 기반으로 TypeScript 타입을 자동 생성하며, 직관적인 쿼리 API를 제공한다.

### Prisma 설정
먼저 Prisma를 설치한다:

```bash
npm install prisma @prisma/client
npx prisma init
```

데이터베이스 스키마를 정의한다:

```prisma
// prisma/schema.prisma
generator client {
  provider = "prisma-client-js"
}

datasource db {
  provider = "postgresql"  // 또는 mysql, sqlite 등
  url      = env("DATABASE_URL")
}

model User {
  id        String   @id @default(uuid())
  username  String   @unique
  nickname  String
  password  String
  level     Int      @default(1)
  exp       Int      @default(0)
  gold      Int      @default(1000)
  createdAt DateTime @default(now())
  updatedAt DateTime @updatedAt
  
  characters Character[]
  items      UserItem[]
  
  @@map("users")
}

model Character {
  id        String   @id @default(uuid())
  userId    String
  name      String
  class     String   // warrior, mage, archer 등
  level     Int      @default(1)
  hp        Int      @default(100)
  maxHp     Int      @default(100)
  attack    Int      @default(10)
  defense   Int      @default(5)
  createdAt DateTime @default(now())
  
  user      User     @relation(fields: [userId], references: [id], onDelete: Cascade)
  
  @@map("characters")
}

model Item {
  id          String   @id @default(uuid())
  name        String
  description String
  type        String   // weapon, armor, consumable 등
  rarity      String   // common, rare, epic, legendary
  stats       Json     // { attack: 10, defense: 5 }
  price       Int
  
  userItems   UserItem[]
  
  @@map("items")
}

model UserItem {
  id        String   @id @default(uuid())
  userId    String
  itemId    String
  quantity  Int      @default(1)
  equipped  Boolean  @default(false)
  acquiredAt DateTime @default(now())
  
  user      User     @relation(fields: [userId], references: [id], onDelete: Cascade)
  item      Item     @relation(fields: [itemId], references: [id])
  
  @@unique([userId, itemId])
  @@map("user_items")
}

model GameRecord {
  id          String   @id @default(uuid())
  userId      String
  gameType    String   // pvp, pve, raid 등
  result      String   // win, lose, draw
  score       Int
  duration    Int      // 초 단위
  rewards     Json     // { exp: 100, gold: 500 }
  playedAt    DateTime @default(now())
  
  @@map("game_records")
  @@index([userId, playedAt])
}
```

스키마를 기반으로 클라이언트 코드를 생성한다:

```bash
npx prisma generate
npx prisma migrate dev --name init
```

### 타입 안전한 쿼리
이제 Prisma Client를 사용해 타입 안전한 데이터베이스 작업을 수행한다:

```typescript
// db/prisma.ts
import { PrismaClient } from '@prisma/client';

const prisma = new PrismaClient({
  log: ['query', 'error', 'warn'],
});

export default prisma;
```

사용자 관련 데이터베이스 로직을 캡슐화한다:

```typescript
// services/UserService.ts
import prisma from '../db/prisma';
import { User, Character } from '@prisma/client';
import bcrypt from 'bcrypt';

export interface CreateUserDto {
  username: string;
  nickname: string;
  password: string;
}

export interface UserWithCharacters extends User {
  characters: Character[];
}

export class UserService {
  // 사용자 생성
  async createUser(data: CreateUserDto): Promise<User> {
    const hashedPassword = await bcrypt.hash(data.password, 10);
    
    const user = await prisma.user.create({
      data: {
        username: data.username,
        nickname: data.nickname,
        password: hashedPassword,
      }
    });
    
    return user;
  }

  // 사용자 찾기 (ID)
  async getUserById(userId: string): Promise<UserWithCharacters | null> {
    const user = await prisma.user.findUnique({
      where: { id: userId },
      include: {
        characters: true,
      }
    });
    
    return user;
  }

  // 사용자 찾기 (사용자명)
  async getUserByUsername(username: string): Promise<User | null> {
    const user = await prisma.user.findUnique({
      where: { username: username }
    });
    
    return user;
  }

  // 로그인 검증
  async validateUser(username: string, password: string): Promise<User | null> {
    const user = await this.getUserByUsername(username);
    
    if (!user) {
      return null;
    }
    
    const isValid = await bcrypt.compare(password, user.password);
    
    return isValid ? user : null;
  }

  // 경험치 추가
  async addExp(userId: string, expAmount: number): Promise<User> {
    const user = await prisma.user.findUnique({
      where: { id: userId }
    });
    
    if (!user) {
      throw new Error('사용자를 찾을 수 없다');
    }
    
    let newExp = user.exp + expAmount;
    let newLevel = user.level;
    
    // 레벨업 로직 (100 경험치당 1레벨)
    while (newExp >= 100 * newLevel) {
      newExp -= 100 * newLevel;
      newLevel++;
    }
    
    const updatedUser = await prisma.user.update({
      where: { id: userId },
      data: {
        exp: newExp,
        level: newLevel,
      }
    });
    
    return updatedUser;
  }

  // 골드 추가/차감
  async updateGold(userId: string, goldAmount: number): Promise<User> {
    const updatedUser = await prisma.user.update({
      where: { id: userId },
      data: {
        gold: {
          increment: goldAmount
        }
      }
    });
    
    if (updatedUser.gold < 0) {
      throw new Error('골드가 부족하다');
    }
    
    return updatedUser;
  }

  // 사용자 삭제
  async deleteUser(userId: string): Promise<void> {
    await prisma.user.delete({
      where: { id: userId }
    });
  }
}
```

아이템 관련 서비스도 구현한다:

```typescript
// services/ItemService.ts
import prisma from '../db/prisma';
import { Item, UserItem } from '@prisma/client';

export interface ItemStats {
  attack?: number;
  defense?: number;
  hp?: number;
  speed?: number;
}

export class ItemService {
  // 아이템 생성
  async createItem(
    name: string,
    description: string,
    type: string,
    rarity: string,
    stats: ItemStats,
    price: number
  ): Promise<Item> {
    const item = await prisma.item.create({
      data: {
        name,
        description,
        type,
        rarity,
        stats: stats as any, // Json 타입
        price,
      }
    });
    
    return item;
  }

  // 아이템 획득
  async acquireItem(userId: string, itemId: string, quantity: number = 1): Promise<UserItem> {
    // 이미 보유한 아이템인지 확인
    const existingItem = await prisma.userItem.findUnique({
      where: {
        userId_itemId: {
          userId: userId,
          itemId: itemId,
        }
      }
    });
    
    if (existingItem) {
      // 수량 증가
      const updated = await prisma.userItem.update({
        where: { id: existingItem.id },
        data: {
          quantity: {
            increment: quantity
          }
        }
      });
      return updated;
    } else {
      // 새로 획득
      const userItem = await prisma.userItem.create({
        data: {
          userId: userId,
          itemId: itemId,
          quantity: quantity,
        }
      });
      return userItem;
    }
  }

  // 아이템 장착
  async equipItem(userId: string, itemId: string): Promise<UserItem> {
    const userItem = await prisma.userItem.findUnique({
      where: {
        userId_itemId: {
          userId: userId,
          itemId: itemId,
        }
      },
      include: {
        item: true,
      }
    });
    
    if (!userItem) {
      throw new Error('보유하지 않은 아이템이다');
    }
    
    // 같은 타입의 다른 아이템 장착 해제
    await prisma.userItem.updateMany({
      where: {
        userId: userId,
        equipped: true,
        item: {
          type: userItem.item.type,
        }
      },
      data: {
        equipped: false,
      }
    });
    
    // 아이템 장착
    const equipped = await prisma.userItem.update({
      where: { id: userItem.id },
      data: { equipped: true }
    });
    
    return equipped;
  }

  // 사용자의 인벤토리 조회
  async getUserInventory(userId: string): Promise<(UserItem & { item: Item })[]> {
    const inventory = await prisma.userItem.findMany({
      where: { userId: userId },
      include: { item: true },
      orderBy: { acquiredAt: 'desc' }
    });
    
    return inventory;
  }

  // 아이템 판매
  async sellItem(userId: string, itemId: string, quantity: number = 1): Promise<number> {
    const userItem = await prisma.userItem.findUnique({
      where: {
        userId_itemId: {
          userId: userId,
          itemId: itemId,
        }
      },
      include: { item: true }
    });
    
    if (!userItem || userItem.quantity < quantity) {
      throw new Error('판매할 아이템이 부족하다');
    }
    
    const sellPrice = Math.floor(userItem.item.price * 0.5 * quantity);
    
    // 트랜잭션으로 아이템 감소 + 골드 증가
    await prisma.$transaction(async (tx) => {
      // 아이템 수량 감소
      if (userItem.quantity === quantity) {
        await tx.userItem.delete({
          where: { id: userItem.id }
        });
      } else {
        await tx.userItem.update({
          where: { id: userItem.id },
          data: {
            quantity: {
              decrement: quantity
            }
          }
        });
      }
      
      // 골드 증가
      await tx.user.update({
        where: { id: userId },
        data: {
          gold: {
            increment: sellPrice
          }
        }
      });
    });
    
    return sellPrice;
  }
}
```

게임 기록 서비스:

```typescript
// services/GameRecordService.ts
import prisma from '../db/prisma';
import { GameRecord } from '@prisma/client';

export interface GameRewards {
  exp: number;
  gold: number;
  items?: { itemId: string; quantity: number }[];
}

export class GameRecordService {
  // 게임 기록 저장
  async saveGameRecord(
    userId: string,
    gameType: string,
    result: string,
    score: number,
    duration: number,
    rewards: GameRewards
  ): Promise<GameRecord> {
    const record = await prisma.gameRecord.create({
      data: {
        userId,
        gameType,
        result,
        score,
        duration,
        rewards: rewards as any, // Json 타입
      }
    });
    
    return record;
  }

  // 사용자 게임 기록 조회
  async getUserRecords(
    userId: string,
    limit: number = 10
  ): Promise<GameRecord[]> {
    const records = await prisma.gameRecord.findMany({
      where: { userId },
      orderBy: { playedAt: 'desc' },
      take: limit,
    });
    
    return records;
  }

  // 사용자 통계
  async getUserStats(userId: string): Promise<{
    totalGames: number;
    wins: number;
    losses: number;
    winRate: number;
    totalScore: number;
    averageScore: number;
  }> {
    const records = await prisma.gameRecord.findMany({
      where: { userId }
    });
    
    const totalGames = records.length;
    const wins = records.filter(r => r.result === 'win').length;
    const losses = records.filter(r => r.result === 'lose').length;
    const winRate = totalGames > 0 ? (wins / totalGames) * 100 : 0;
    const totalScore = records.reduce((sum, r) => sum + r.score, 0);
    const averageScore = totalGames > 0 ? totalScore / totalGames : 0;
    
    return {
      totalGames,
      wins,
      losses,
      winRate,
      totalScore,
      averageScore,
    };
  }

  // 랭킹 조회
  async getLeaderboard(
    gameType: string,
    limit: number = 10
  ): Promise<{ userId: string; totalScore: number; gamesPlayed: number }[]> {
    const leaderboard = await prisma.gameRecord.groupBy({
      by: ['userId'],
      where: { gameType },
      _sum: { score: true },
      _count: { id: true },
      orderBy: {
        _sum: {
          score: 'desc'
        }
      },
      take: limit,
    });
    
    return leaderboard.map(entry => ({
      userId: entry.userId,
      totalScore: entry._sum.score || 0,
      gamesPlayed: entry._count.id,
    }));
  }
}
```

서버에 통합한다:

```typescript
// server.ts에 추가
import { UserService } from './services/UserService';
import { ItemService } from './services/ItemService';
import { GameRecordService } from './services/GameRecordService';

const userService = new UserService();
const itemService = new ItemService();
const gameRecordService = new GameRecordService();

// 게임 종료 시 기록 저장 예제
socket.on('gameFinished', async (data: {
  result: 'win' | 'lose';
  score: number;
  duration: number;
}) => {
  try {
    const session = sessionManager.getSessionBySocketId(socket.id);
    if (!session || !session.isAuthenticated) {
      return;
    }
    
    const rewards: GameRewards = {
      exp: data.result === 'win' ? 100 : 50,
      gold: data.score * 10,
    };
    
    // 게임 기록 저장
    await gameRecordService.saveGameRecord(
      session.playerId,
      'pvp',
      data.result,
      data.score,
      data.duration,
      rewards
    );
    
    // 보상 지급
    await userService.addExp(session.playerId, rewards.exp);
    await userService.updateGold(session.playerId, rewards.gold);
    
    // 보상 정보 전송
    socket.emit('gameRewards', rewards);
    
  } catch (error) {
    console.error('게임 기록 저장 실패:', error);
    socket.emit('error', {
      code: 'SAVE_FAILED',
      message: '게임 기록 저장에 실패했다'
    });
  }
});
```


## 7.4 에러 핸들링
게임 서버에서 에러는 예상 가능한 것(유효성 검증 실패)부터 예상 불가능한 것(DB 연결 끊김)까지 다양하다. TypeScript의 타입 시스템을 활용하면 에러도 체계적으로 관리할 수 있다.

### 커스텀 에러 타입
다양한 에러 상황을 표현하는 커스텀 에러 클래스를 정의한다:

```typescript
// errors/GameErrors.ts
export abstract class GameError extends Error {
  abstract readonly code: string;
  abstract readonly statusCode: number;
  
  constructor(message: string) {
    super(message);
    this.name = this.constructor.name;
    Error.captureStackTrace(this, this.constructor);
  }
  
  toJSON() {
    return {
      code: this.code,
      message: this.message,
      name: this.name,
    };
  }
}

// 인증 에러
export class AuthenticationError extends GameError {
  readonly code = 'AUTHENTICATION_ERROR';
  readonly statusCode = 401;
  
  constructor(message: string = '인증에 실패했다') {
    super(message);
  }
}

// 권한 에러
export class AuthorizationError extends GameError {
  readonly code = 'AUTHORIZATION_ERROR';
  readonly statusCode = 403;
  
  constructor(message: string = '권한이 없다') {
    super(message);
  }
}

// 유효성 검증 에러
export class ValidationError extends GameError {
  readonly code = 'VALIDATION_ERROR';
  readonly statusCode = 400;
  readonly field?: string;
  
  constructor(message: string, field?: string) {
    super(message);
    this.field = field;
  }
}

// 리소스 없음 에러
export class NotFoundError extends GameError {
  readonly code = 'NOT_FOUND';
  readonly statusCode = 404;
  readonly resource: string;
  
  constructor(resource: string, message?: string) {
    super(message || `${resource}을(를) 찾을 수 없다`);
    this.resource = resource;
  }
}

// 비즈니스 로직 에러
export class BusinessLogicError extends GameError {
  readonly code = 'BUSINESS_LOGIC_ERROR';
  readonly statusCode = 400;
  
  constructor(message: string) {
    super(message);
  }
}

// 서버 내부 에러
export class InternalServerError extends GameError {
  readonly code = 'INTERNAL_SERVER_ERROR';
  readonly statusCode = 500;
  
  constructor(message: string = '서버 내부 오류가 발생했다') {
    super(message);
  }
}

// 데이터베이스 에러
export class DatabaseError extends GameError {
  readonly code = 'DATABASE_ERROR';
  readonly statusCode = 500;
  
  constructor(message: string = '데이터베이스 오류가 발생했다') {
    super(message);
  }
}

// 네트워크 에러
export class NetworkError extends GameError {
  readonly code = 'NETWORK_ERROR';
  readonly statusCode = 503;
  
  constructor(message: string = '네트워크 오류가 발생했다') {
    super(message);
  }
}
```

특정 게임 로직 에러도 정의한다:

```typescript
// errors/GameErrors.ts (계속)

// 방 관련 에러
export class RoomFullError extends BusinessLogicError {
  constructor() {
    super('방이 가득 찼다');
  }
}

export class RoomNotFoundError extends NotFoundError {
  constructor(roomId: string) {
    super('Room', `방 ${roomId}을(를) 찾을 수 없다`);
  }
}

export class AlreadyInRoomError extends BusinessLogicError {
  constructor() {
    super('이미 방에 입장해 있다');
  }
}

// 게임 플레이 에러
export class NotYourTurnError extends BusinessLogicError {
  constructor() {
    super('당신의 턴이 아니다');
  }
}

export class InvalidActionError extends BusinessLogicError {
  constructor(action: string) {
    super(`유효하지 않은 행동이다: ${action}`);
  }
}

export class InsufficientResourceError extends BusinessLogicError {
  constructor(resource: string) {
    super(`${resource}이(가) 부족하다`);
  }
}

// 아이템 에러
export class ItemNotOwnedError extends BusinessLogicError {
  constructor(itemName: string) {
    super(`${itemName}을(를) 보유하고 있지 않다`);
  }
}

export class ItemNotEquippableError extends BusinessLogicError {
  constructor(itemName: string) {
    super(`${itemName}은(는) 장착할 수 없다`);
  }
}
```

### 에러 처리 미들웨어
Socket.io에서는 미들웨어를 통해 모든 이벤트의 에러를 중앙에서 처리할 수 있다:

```typescript
// middleware/errorHandler.ts
import { Socket } from 'socket.io';
import { GameError } from '../errors/GameErrors';

export function errorHandlerMiddleware(socket: Socket, next: (err?: Error) => void) {
  // 모든 이벤트 핸들러를 래핑
  const originalOn = socket.on.bind(socket);
  
  socket.on = function(event: string, handler: (...args: any[]) => void) {
    const wrappedHandler = async (...args: any[]) => {
      try {
        await handler(...args);
      } catch (error) {
        handleError(socket, error);
      }
    };
    
    return originalOn(event, wrappedHandler);
  };
  
  next();
}

function handleError(socket: Socket, error: unknown): void {
  // GameError 타입 체크
  if (error instanceof GameError) {
    console.error(`[${error.code}] ${error.message}`, {
      socketId: socket.id,
      stack: error.stack,
    });
    
    socket.emit('error', {
      code: error.code,
      message: error.message,
    });
    
    return;
  }
  
  // 일반 Error
  if (error instanceof Error) {
    console.error(`[UNKNOWN_ERROR] ${error.message}`, {
      socketId: socket.id,
      stack: error.stack,
    });
    
    socket.emit('error', {
      code: 'UNKNOWN_ERROR',
      message: '알 수 없는 오류가 발생했다',
    });
    
    return;
  }
  
  // 알 수 없는 타입
  console.error('[UNKNOWN_ERROR] Unknown error type', {
    socketId: socket.id,
    error: error,
  });
  
  socket.emit('error', {
    code: 'UNKNOWN_ERROR',
    message: '알 수 없는 오류가 발생했다',
  });
}
```

서버에 적용한다:

```typescript
// server.ts에 추가
import { errorHandlerMiddleware } from './middleware/errorHandler';

io.use(errorHandlerMiddleware);
```

이제 모든 이벤트 핸들러에서 에러를 throw하면 자동으로 처리된다:

```typescript
// server.ts의 이벤트 핸들러 예제
socket.on('joinRoom', (data) => {
  const { roomId, nickname } = data;
  
  // 유효성 검증
  if (!roomId || roomId.trim() === '') {
    throw new ValidationError('방 ID가 비어있다', 'roomId');
  }
  
  if (!nickname || nickname.trim() === '') {
    throw new ValidationError('닉네임이 비어있다', 'nickname');
  }
  
  let room = roomManager.getRoom(roomId);
  
  if (!room) {
    room = roomManager.createRoom(roomId);
    if (!room) {
      throw new InternalServerError('방 생성에 실패했다');
    }
  }
  
  if (room.isFull()) {
    throw new RoomFullError();
  }
  
  if (socket.data.roomId) {
    throw new AlreadyInRoomError();
  }
  
  const added = room.addPlayer(socket.id, nickname);
  
  if (!added) {
    throw new BusinessLogicError('방 입장에 실패했다');
  }
  
  // 정상 처리
  socket.data.playerId = socket.id;
  socket.data.roomId = roomId;
  socket.data.nickname = nickname;
  socket.join(roomId);
  
  io.to(roomId).emit('playerJoined', {
    playerId: socket.id,
    nickname: nickname
  });
  
  io.to(roomId).emit('gameStateUpdate', room.getGameState());
});

socket.on('playerAction', (action) => {
  const session = sessionManager.getSessionBySocketId(socket.id);
  
  if (!session) {
    throw new AuthenticationError('세션을 찾을 수 없다');
  }
  
  if (!session.roomId) {
    throw new BusinessLogicError('방에 입장하지 않았다');
  }
  
  const room = roomManager.getRoom(session.roomId);
  
  if (!room) {
    throw new RoomNotFoundError(session.roomId);
  }
  
  const gameState = room.getGameState();
  
  if (gameState.status !== 'playing') {
    throw new BusinessLogicError('게임이 진행 중이 아니다');
  }
  
  if (gameState.currentTurn !== socket.id) {
    throw new NotYourTurnError();
  }
  
  // 액션 처리 로직
  handlePlayerAction(socket, action);
});
```

데이터베이스 에러도 처리한다:

```typescript
// services/UserService.ts에 추가
import { DatabaseError, NotFoundError, ValidationError } from '../errors/GameErrors';

export class UserService {
  async createUser(data: CreateUserDto): Promise<User> {
    try {
      // 중복 검사
      const existing = await this.getUserByUsername(data.username);
      if (existing) {
        throw new ValidationError('이미 존재하는 사용자명이다', 'username');
      }
      
      const hashedPassword = await bcrypt.hash(data.password, 10);
      
      const user = await prisma.user.create({
        data: {
          username: data.username,
          nickname: data.nickname,
          password: hashedPassword,
        }
      });
      
      return user;
      
    } catch (error) {
      if (error instanceof GameError) {
        throw error;
      }
      
      console.error('사용자 생성 실패:', error);
      throw new DatabaseError('사용자 생성 중 오류가 발생했다');
    }
  }

  async getUserById(userId: string): Promise<UserWithCharacters> {
    try {
      const user = await prisma.user.findUnique({
        where: { id: userId },
        include: { characters: true }
      });
      
      if (!user) {
        throw new NotFoundError('User', `사용자 ${userId}을(를) 찾을 수 없다`);
      }
      
      return user;
      
    } catch (error) {
      if (error instanceof GameError) {
        throw error;
      }
      
      console.error('사용자 조회 실패:', error);
      throw new DatabaseError('사용자 조회 중 오류가 발생했다');
    }
  }
}
```

```
┌──────────────────────────────────────────────────┐
│            에러 처리 흐름도                       │
├──────────────────────────────────────────────────┤
│                                                   │
│  Event Handler                                    │
│       │                                           │
│       ├─ Validation Error?                       │
│       │       └──> throw ValidationError          │
│       │                                           │
│       ├─ Business Logic Error?                   │
│       │       └──> throw BusinessLogicError       │
│       │                                           │
│       ├─ Database Error?                         │
│       │       └──> throw DatabaseError            │
│       │                                           │
│       ↓                                           │
│  Error Handler Middleware                         │
│       │                                           │
│       ├─ GameError instance?                     │
│       │   ├─ Log error                           │
│       │   └─ Emit typed error to client          │
│       │                                           │
│       ├─ Error instance?                         │
│       │   ├─ Log error                           │
│       │   └─ Emit generic error                  │
│       │                                           │
│       └─ Unknown type?                           │
│           ├─ Log error                           │
│           └─ Emit unknown error                  │
│                                                   │
│  Client receives:                                 │
│  { code: "ERROR_CODE", message: "..." }          │
│                                                   │
│  ✓ 타입 안전한 에러 처리                          │
│  ✓ 일관된 에러 응답 형식                          │
│  ✓ 중앙 집중식 에러 로깅                          │
└──────────────────────────────────────────────────┘
```


## 마무리
이 챕터에서는 실전 게임 서버 개발에 필요한 핵심 패턴들을 살펴보았다. TypeScript의 타입 시스템을 활용하면 다음과 같은 이점을 얻는다:

**타입 안전한 실시간 통신**: Socket.io의 이벤트와 데이터가 모두 타입으로 정의되어 오타나 잘못된 데이터 전송을 컴파일 시점에 잡아낸다. 클라이언트와 서버 간의 계약이 명확해진다.

**체계적인 상태 관리**: Room과 Session 같은 복잡한 상태도 클래스와 인터페이스로 구조화하면 관리가 쉬워진다. 싱글톤 패턴으로 전역 접근도 가능하다.

**안전한 데이터베이스 연동**: Prisma는 스키마에서 TypeScript 타입을 자동 생성하므로 쿼리 작성 시 자동완성과 타입 검증을 받을 수 있다. 실수로 잘못된 필드를 조회하거나 업데이트할 일이 없다.

**명확한 에러 핸들링**: 커스텀 에러 클래스로 다양한 에러 상황을 표현하고, 중앙 집중식 미들웨어로 일관되게 처리한다. 클라이언트는 구조화된 에러 정보를 받아 적절히 대응할 수 있다.

이제 실제 프로젝트에 이 패턴들을 적용해볼 차례다. 다음 챕터에서는 성능 최적화와 컴파일 옵션 튜닝을 다룬다.

   

# Chapter 8: 성능과 최적화
TypeScript는 개발 생산성을 높여주지만, 잘못 설정하면 빌드 시간이 느려지고 불필요한 타입 연산으로 개발 경험이 나빠질 수 있다. 특히 게임 서버처럼 빠른 반복 개발이 중요한 환경에서는 컴파일 속도가 생산성에 직결된다. 이 챕터에서는 TypeScript 프로젝트의 성능을 최적화하고, 타입 시스템을 효율적으로 활용하는 방법을 다룬다.


## 8.1 컴파일 옵션 최적화
`tsconfig.json`은 TypeScript 컴파일러의 동작을 제어하는 핵심 설정 파일이다. 옵션을 제대로 이해하고 조정하면 빌드 속도를 크게 개선할 수 있다.

### 빌드 속도 향상 팁
게임 서버 개발 중에는 코드를 수정할 때마다 빠르게 재컴파일되어야 한다. 다음 옵션들이 빌드 속도에 영향을 준다:

```json
// tsconfig.json
{
  "compilerOptions": {
    // 증분 컴파일 활성화 - 변경된 파일만 재컴파일
    "incremental": true,
    "tsBuildInfoFile": "./.tsbuildinfo",
    
    // 타입 체크 건너뛰기 (빌드만 할 때)
    "skipLibCheck": true,
    
    // 선언 파일 생성 비활성화 (개발 시)
    "declaration": false,
    "declarationMap": false,
    
    // 소스맵 최적화
    "sourceMap": true,
    "inlineSourceMap": false,
    
    // 엄격한 검사 (필요에 따라 조정)
    "strict": true,
    "noUnusedLocals": false,     // 개발 중에는 끄기
    "noUnusedParameters": false,  // 개발 중에는 끄기
    
    // 모듈 해석 최적화
    "moduleResolution": "node",
    "resolveJsonModule": true,
    "esModuleInterop": true,
    
    // 출력 설정
    "outDir": "./dist",
    "rootDir": "./src",
    
    // 타겟 설정
    "target": "ES2020",
    "module": "commonjs",
    "lib": ["ES2020"]
  },
  
  // 컴파일 대상 제한
  "include": [
    "src/**/*"
  ],
  
  // 불필요한 파일 제외
  "exclude": [
    "node_modules",
    "dist",
    "**/*.spec.ts",
    "**/*.test.ts"
  ]
}
```

**증분 컴파일(Incremental Compilation)**은 가장 큰 성능 향상을 가져온다. TypeScript는 `.tsbuildinfo` 파일에 이전 컴파일 정보를 저장하고, 다음 컴파일 시 변경된 파일만 처리한다.

```typescript
// 코드 변경 전
export class Player {
  constructor(public id: string, public name: string) {}
}

// 코드 변경 후
export class Player {
  constructor(
    public id: string, 
    public name: string,
    public level: number = 1  // 필드 추가
  ) {}
}

// 증분 컴파일: Player.ts와 이를 import하는 파일만 재컴파일
// 전체 프로젝트를 재컴파일하지 않음!
```

**skipLibCheck**는 `node_modules`의 타입 정의 파일(`.d.ts`) 검사를 건너뛴다. 외부 라이브러리의 타입 에러는 우리가 수정할 수 없으므로 검사하지 않아 빠르다.

```json
{
  "compilerOptions": {
    "skipLibCheck": true  // socket.io, express 등 외부 라이브러리 타입 검사 생략
  }
}
```

**프로젝트 레퍼런스(Project References)**를 활용하면 대규모 프로젝트를 여러 하위 프로젝트로 나누어 병렬 빌드할 수 있다:

```
game-server/
├── packages/
│   ├── core/              # 핵심 로직
│   │   ├── src/
│   │   └── tsconfig.json
│   ├── network/           # 네트워크 레이어
│   │   ├── src/
│   │   └── tsconfig.json
│   └── database/          # 데이터베이스 레이어
│       ├── src/
│       └── tsconfig.json
└── tsconfig.json          # 루트 설정
```

```json
// packages/network/tsconfig.json
{
  "compilerOptions": {
    "composite": true,
    "outDir": "./dist",
    "rootDir": "./src"
  },
  "references": [
    { "path": "../core" }  // core 패키지 의존
  ]
}

// 루트 tsconfig.json
{
  "files": [],
  "references": [
    { "path": "./packages/core" },
    { "path": "./packages/network" },
    { "path": "./packages/database" }
  ]
}
```

빌드 시 `tsc --build` 명령을 사용하면 의존성 순서에 따라 자동으로 빌드된다:

```bash
tsc --build --verbose
# [core] 빌드 중...
# [network] 빌드 중... (core 의존)
# [database] 빌드 중... (core 의존)
```

### 프로덕션 빌드 설정
개발 환경과 프로덕션 환경은 다른 최적화가 필요하다. 개발 시에는 빠른 피드백이 중요하고, 프로덕션에서는 안정성과 디버깅 정보가 중요하다.

```json
// tsconfig.dev.json - 개발 환경
{
  "extends": "./tsconfig.json",
  "compilerOptions": {
    "incremental": true,
    "sourceMap": true,
    "declaration": false,
    "removeComments": false,
    
    // 빠른 피드백을 위해 일부 검사 완화
    "noUnusedLocals": false,
    "noUnusedParameters": false,
    
    // watch 모드 최적화
    "assumeChangesOnlyAffectDirectDependencies": true
  }
}

// tsconfig.prod.json - 프로덕션 환경
{
  "extends": "./tsconfig.json",
  "compilerOptions": {
    "incremental": false,
    "sourceMap": true,
    "declaration": true,
    "declarationMap": true,
    "removeComments": true,
    
    // 엄격한 검사 활성화
    "strict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noImplicitReturns": true,
    "noFallthroughCasesInSwitch": true,
    
    // 최적화
    "importHelpers": true,  // tslib 사용으로 번들 크기 감소
    "downlevelIteration": false
  }
}
```

`package.json`에 스크립트를 추가한다:

```json
{
  "scripts": {
    "dev": "tsc --project tsconfig.dev.json --watch",
    "build": "tsc --project tsconfig.prod.json",
    "build:fast": "tsc --project tsconfig.dev.json",
    "clean": "rm -rf dist .tsbuildinfo"
  }
}
```

**watch 모드 최적화**는 개발 중 파일 변경을 감지하고 자동으로 재컴파일한다:

```json
{
  "compilerOptions": {
    // watch 모드 설정
  },
  "watchOptions": {
    // 변경 감지 전략
    "watchFile": "useFsEvents",  // 파일 시스템 이벤트 사용
    "watchDirectory": "useFsEvents",
    
    // 폴링 방식 설정 (필요시)
    "fallbackPolling": "dynamicPriority",
    
    // 제외할 디렉토리
    "excludeDirectories": [
      "**/node_modules",
      "**/dist",
      "**/.git"
    ],
    
    // 제외할 파일 패턴
    "excludeFiles": [
      "**/*.spec.ts",
      "**/*.test.ts"
    ]
  }
}
```

개발 서버에서 nodemon과 함께 사용한다:

```json
// nodemon.json
{
  "watch": ["src"],
  "ext": "ts",
  "ignore": ["src/**/*.spec.ts", "src/**/*.test.ts"],
  "exec": "ts-node --project tsconfig.dev.json src/server.ts",
  "env": {
    "NODE_ENV": "development"
  }
}
```

```json
// package.json
{
  "scripts": {
    "dev": "nodemon",
    "dev:debug": "nodemon --inspect"
  }
}
```

**빌드 성능 측정**도 중요하다. TypeScript 컴파일러에 내장된 프로파일링 기능을 사용한다:

```bash
# 상세한 빌드 정보 출력
tsc --extendedDiagnostics

# 출력 예시:
# Files:            150
# Lines:            12000
# Nodes:            50000
# Identifiers:      15000
# Symbols:          8000
# Types:            3000
# I/O Read time:    0.15s
# Parse time:       0.30s
# Bind time:        0.20s
# Check time:       1.50s
# Emit time:        0.25s
# Total time:       2.40s
```

성능 병목을 찾아내면 개선할 수 있다:

```typescript
// 안티패턴: 복잡한 타입 연산
type DeepPartial<T> = {
  [P in keyof T]?: T[P] extends object
    ? DeepPartial<T[P]>
    : T[P];
};

type DeepReadonly<T> = {
  readonly [P in keyof T]: T[P] extends object
    ? DeepReadonly<T[P]>
    : T[P];
};

// 수천 줄의 타입에 적용하면 컴파일이 느려진다
type HugeConfig = DeepPartial<DeepReadonly<VeryLargeType>>;

// 개선: 필요한 부분만 타입 적용
type Config = Partial<Pick<VeryLargeType, 'network' | 'database'>>;
```

```
┌────────────────────────────────────────────────┐
│       TypeScript 빌드 파이프라인                 │
├────────────────────────────────────────────────┤
│                                                │
│  소스 파일 (.ts)                               │
│       ↓                                        │
│  ┌─────────────────────────────────┐          │
│  │  1. 파일 읽기 (I/O Read)        │          │
│  │     - 변경된 파일만 (증분 컴파일) │          │
│  └─────────────────────────────────┘          │
│       ↓                                        │
│  ┌─────────────────────────────────┐          │
│  │  2. 파싱 (Parse)                 │          │
│  │     - AST 생성                   │          │
│  │     - 구문 분석                  │          │
│  └─────────────────────────────────┘          │
│       ↓                                        │
│  ┌─────────────────────────────────┐          │
│  │  3. 바인딩 (Bind)                │          │
│  │     - 심볼 테이블 생성            │          │
│  │     - 스코프 분석                │          │
│  └─────────────────────────────────┘          │
│       ↓                                        │
│  ┌─────────────────────────────────┐          │
│  │  4. 타입 체크 (Check) ⏱️        │          │
│  │     - 타입 추론                  │          │
│  │     - 타입 검증 (가장 느림)      │          │
│  │     - skipLibCheck로 최적화      │          │
│  └─────────────────────────────────┘          │
│       ↓                                        │
│  ┌─────────────────────────────────┐          │
│  │  5. 출력 (Emit)                  │          │
│  │     - JavaScript 생성            │          │
│  │     - 소스맵 생성                │          │
│  │     - 선언 파일 생성             │          │
│  └─────────────────────────────────┘          │
│       ↓                                        │
│  JavaScript 파일 (.js)                         │
│                                                 │
│  ✓ incremental: 1~4 단계 캐싱                 │
│  ✓ skipLibCheck: 외부 라이브러리 4단계 생략   │
│  ✓ declaration: false로 5단계 일부 생략       │
└────────────────────────────────────────────────┘
```


## 8.2 타입 추론 활용
TypeScript의 타입 추론(Type Inference)은 강력하다. 명시적으로 타입을 선언하지 않아도 컴파일러가 자동으로 타입을 파악한다. 타입 추론을 잘 활용하면 코드가 간결해지고, 유지보수도 쉬워진다.

### 불필요한 타입 선언 줄이기
많은 개발자가 습관적으로 모든 변수에 타입을 명시한다. 하지만 TypeScript는 이미 충분히 똑똑하다:

```typescript
// ❌ 불필요한 타입 선언
const playerName: string = "Hero";
const playerLevel: number = 10;
const isOnline: boolean = true;
const items: string[] = ["sword", "shield", "potion"];

// ✅ 타입 추론 활용
const playerName = "Hero";           // string으로 추론
const playerLevel = 10;              // number로 추론
const isOnline = true;               // boolean으로 추론
const items = ["sword", "shield"];   // string[]으로 추론
```

함수의 반환 타입도 대부분 추론 가능하다:

```typescript
// ❌ 불필요한 반환 타입 선언
function calculateDamage(attack: number, defense: number): number {
  return Math.max(0, attack - defense);
}

// ✅ 반환 타입 추론
function calculateDamage(attack: number, defense: number) {
  return Math.max(0, attack - defense);  // number로 추론됨
}
```

하지만 **공개 API나 복잡한 로직**에서는 명시적 타입 선언이 좋다:

```typescript
// ✅ 공개 API는 명시적 타입 선언
export function processGameTick(deltaTime: number): GameState {
  // 복잡한 로직...
  return newState;
}

// ✅ 복잡한 타입은 명시
function transformPacket(data: unknown): ParsedPacket {
  // 타입 가드와 변환 로직
  return parsed;
}
```

객체 리터럴의 타입도 추론된다:

```typescript
// ✅ 타입 추론 활용
const player = {
  id: "player-001",
  nickname: "Hero",
  level: 10,
  position: { x: 100, y: 200 }
};

// player의 타입이 자동으로 추론됨:
// {
//   id: string;
//   nickname: string;
//   level: number;
//   position: { x: number; y: number };
// }

// ❌ 불필요한 타입 선언
const player: {
  id: string;
  nickname: string;
  level: number;
  position: { x: number; y: number };
} = {
  id: "player-001",
  nickname: "Hero",
  level: 10,
  position: { x: 100, y: 200 }
};
```

제네릭 함수의 타입 인자도 추론된다:

```typescript
function createPacket<T>(type: string, data: T) {
  return { type, data, timestamp: Date.now() };
}

// ✅ 타입 인자 생략 가능
const packet = createPacket("MOVE", { x: 10, y: 20 });
// packet: { type: string; data: { x: number; y: number }; timestamp: number }

// ❌ 불필요한 타입 인자 명시
const packet2 = createPacket<{ x: number; y: number }>("MOVE", { x: 10, y: 20 });
```

배열 메서드의 타입도 자동 추론된다:

```typescript
const players = [
  { id: "1", level: 10 },
  { id: "2", level: 15 },
  { id: "3", level: 12 }
];

// ✅ map, filter 등의 반환 타입 자동 추론
const levels = players.map(p => p.level);  // number[]
const highLevel = players.filter(p => p.level > 12);  // { id: string; level: number }[]
const ids = players.map(p => p.id);  // string[]
```

**컨텍스트 타입(Contextual Typing)**도 활용한다. TypeScript는 값이 사용되는 위치에서 타입을 추론한다:

```typescript
interface EventHandler {
  (event: { type: string; data: any }): void;
}

// ✅ 매개변수 타입이 자동 추론됨
const handler: EventHandler = (event) => {
  console.log(event.type);  // event는 { type: string; data: any }로 추론
};

// 배열의 콜백도 마찬가지
const numbers = [1, 2, 3, 4, 5];
numbers.forEach((num) => {  // num은 number로 추론
  console.log(num * 2);
});
```

### as const assertion
`as const`는 리터럴 값을 더 좁은 타입으로 만든다. 불변 데이터를 다룰 때 유용하다:

```typescript
// 기본 타입 추론
const direction = "up";  // string 타입

// as const를 사용한 리터럴 타입
const direction2 = "up" as const;  // "up" 타입 (리터럴)

// 객체에 적용
const config = {
  host: "localhost",
  port: 3000,
  timeout: 5000
} as const;

// config의 타입:
// {
//   readonly host: "localhost";
//   readonly port: 3000;
//   readonly timeout: 5000;
// }

// ❌ 수정 불가능
config.port = 8080;  // 에러: readonly 속성
```

게임 서버에서 상수 정의 시 유용하다:

```typescript
// ✅ 게임 상수 정의
export const GAME_CONSTANTS = {
  MAX_PLAYERS: 4,
  TURN_DURATION: 30,
  MAP_SIZE: { width: 1000, height: 1000 },
  ITEM_TYPES: ["weapon", "armor", "consumable"],
  RARITIES: ["common", "rare", "epic", "legendary"]
} as const;

// 타입으로 사용 가능
type ItemType = typeof GAME_CONSTANTS.ITEM_TYPES[number];
// "weapon" | "armor" | "consumable"

type Rarity = typeof GAME_CONSTANTS.RARITIES[number];
// "common" | "rare" | "epic" | "legendary"
```

패킷 타입 정의에도 활용한다:

```typescript
// ✅ 패킷 타입 맵
const PACKET_TYPES = {
  PLAYER_MOVE: "PLAYER_MOVE",
  PLAYER_ATTACK: "PLAYER_ATTACK",
  PLAYER_CHAT: "PLAYER_CHAT",
  GAME_START: "GAME_START",
  GAME_END: "GAME_END"
} as const;

// 자동으로 유니온 타입 생성
type PacketType = typeof PACKET_TYPES[keyof typeof PACKET_TYPES];
// "PLAYER_MOVE" | "PLAYER_ATTACK" | "PLAYER_CHAT" | "GAME_START" | "GAME_END"

// 패킷 인터페이스
interface Packet<T extends PacketType, D = any> {
  type: T;
  data: D;
  timestamp: number;
}

// 타입 안전한 패킷 생성
function createPacket<T extends PacketType, D>(
  type: T, 
  data: D
): Packet<T, D> {
  return { type, data, timestamp: Date.now() };
}

// ✅ 타입 체크됨
const movePacket = createPacket(PACKET_TYPES.PLAYER_MOVE, { x: 10, y: 20 });

// ❌ 컴파일 에러
const invalidPacket = createPacket("INVALID_TYPE", {});  // 에러!
```

튜플에도 사용한다:

```typescript
// 기본 배열 추론
const position = [100, 200];  // number[] 타입

// as const로 튜플 타입
const position2 = [100, 200] as const;  // readonly [100, 200] 타입

// 함수 반환값으로 사용
function getPlayerPosition(playerId: string) {
  // ... 로직
  return [x, y] as const;  // readonly [number, number] 반환
}

const [x, y] = getPlayerPosition("player-001");  // 구조 분해 가능
```

Enum 대신 사용할 수도 있다:

```typescript
// ❌ Enum 사용
enum Direction {
  Up = "UP",
  Down = "DOWN",
  Left = "LEFT",
  Right = "RIGHT"
}

// ✅ as const 객체 사용 (더 가볍고 유연함)
const Direction = {
  Up: "UP",
  Down: "DOWN",
  Left: "LEFT",
  Right: "RIGHT"
} as const;

type Direction = typeof Direction[keyof typeof Direction];
// "UP" | "DOWN" | "LEFT" | "RIGHT"

// 사용법은 동일
function move(direction: Direction) {
  // ...
}

move(Direction.Up);  // ✅
```

상태 머신에도 유용하다:

```typescript
// ✅ 게임 상태 정의
const GameState = {
  WAITING: "WAITING",
  PLAYING: "PLAYING",
  PAUSED: "PAUSED",
  FINISHED: "FINISHED"
} as const;

type GameState = typeof GameState[keyof typeof GameState];

// 상태 전환 맵
const STATE_TRANSITIONS = {
  [GameState.WAITING]: [GameState.PLAYING],
  [GameState.PLAYING]: [GameState.PAUSED, GameState.FINISHED],
  [GameState.PAUSED]: [GameState.PLAYING, GameState.FINISHED],
  [GameState.FINISHED]: [GameState.WAITING]
} as const;

// 타입 안전한 상태 전환 검증
function canTransition(from: GameState, to: GameState): boolean {
  const allowed = STATE_TRANSITIONS[from];
  return allowed.includes(to as any);
}
```

```
┌──────────────────────────────────────────────────┐
│          타입 추론 vs 명시적 선언                 │
├──────────────────────────────────────────────────┤
│                                                   │
│  타입 추론을 사용하는 경우:                       │
│  ━━━━━━━━━━━━━━━━━━━━━━━━                        │
│  ✓ 지역 변수                                      │
│  ✓ 간단한 함수의 반환값                          │
│  ✓ 배열 메서드의 콜백                            │
│  ✓ 제네릭 함수의 타입 인자                       │
│  ✓ 객체 리터럴                                   │
│                                                   │
│  명시적 선언을 하는 경우:                         │
│  ━━━━━━━━━━━━━━━━━━━━━━━━                        │
│  ✓ 공개 API 함수의 매개변수/반환값              │
│  ✓ 클래스의 프로퍼티                             │
│  ✓ 인터페이스 구현                               │
│  ✓ 복잡한 타입 변환                              │
│  ✓ 문서화가 필요한 경우                          │
│                                                   │
│  as const를 사용하는 경우:                       │
│  ━━━━━━━━━━━━━━━━━━━━━━━━                        │
│  ✓ 상수 정의 (enum 대체)                         │
│  ✓ 설정 객체                                     │
│  ✓ 불변 데이터                                   │
│  ✓ 리터럴 유니온 타입 생성                       │
│  ✓ 튜플 타입                                     │
│                                                   │
└──────────────────────────────────────────────────┘
```


## 8.3 흔한 실수와 안티패턴
TypeScript를 사용하면서 자주 저지르는 실수들이 있다. 이런 패턴들은 타입 안전성을 해치고, 유지보수를 어렵게 만든다.

### any 남용 피하기
`any`는 TypeScript의 타입 체크를 완전히 무력화한다. 편하지만 위험하다:

```typescript
// ❌ any 남용
function processPacket(data: any) {
  console.log(data.type);
  console.log(data.paylaod);  // 오타! 하지만 컴파일 에러 없음
  return data.value + 10;     // data.value가 문자열이면 런타임 에러
}

// ✅ 적절한 타입 정의
interface Packet {
  type: string;
  payload: unknown;
}

function processPacket(data: Packet) {
  console.log(data.type);
  console.log(data.payload);  // 오타 시 컴파일 에러
  
  // payload를 사용하려면 타입 체크 필요
  if (typeof data.payload === 'object' && data.payload !== null) {
    // 안전하게 사용
  }
}
```

`unknown`을 사용하면 타입 안전성을 유지하면서 유연성도 확보할 수 있다:

```typescript
// ✅ unknown 사용
function parseJSON(jsonString: string): unknown {
  return JSON.parse(jsonString);
}

const result = parseJSON('{"name": "Hero", "level": 10}');

// ❌ 바로 사용 불가
console.log(result.name);  // 에러: unknown 타입

// ✅ 타입 가드로 검증 후 사용
if (typeof result === 'object' && result !== null && 'name' in result) {
  console.log(result.name);  // 안전
}

// ✅ 타입 단언 (확신이 있을 때)
interface Player {
  name: string;
  level: number;
}

const player = result as Player;
console.log(player.name);
```

외부 라이브러리를 래핑할 때도 `any` 대신 제네릭을 사용한다:

```typescript
// ❌ any 반환
function cacheGet(key: string): any {
  return cache.get(key);
}

const player = cacheGet("player-001");
player.nonExistentMethod();  // 런타임 에러!

// ✅ 제네릭 사용
function cacheGet<T>(key: string): T | undefined {
  return cache.get(key) as T | undefined;
}

interface Player {
  id: string;
  name: string;
  level: number;
}

const player = cacheGet<Player>("player-001");
if (player) {
  console.log(player.level);  // 타입 체크됨
  // player.nonExistentMethod();  // 컴파일 에러!
}
```

데이터베이스 쿼리 결과도 마찬가지다:  

```typescript
// ❌ any 반환
async function queryUser(userId: string): Promise<any> {
  const result = await db.query("SELECT * FROM users WHERE id = ?", [userId]);
  return result[0];
}

// ✅ 명확한 타입 반환
interface User {
  id: string;
  username: string;
  level: number;
  createdAt: Date;
}

async function queryUser(userId: string): Promise<User | null> {
  const result = await db.query<User>(
    "SELECT * FROM users WHERE id = ?", 
    [userId]
  );
  return result[0] || null;
}
```

**점진적 마이그레이션**을 위해서는 `any` 대신 `// @ts-expect-error` 주석을 사용한다:

```typescript
// ❌ any로 회피
const legacyLib: any = require('legacy-library');
legacyLib.doSomething();

// ✅ 명시적으로 표시
// @ts-expect-error - legacy-library는 타입 정의 없음, 마이그레이션 예정
const legacyLib = require('legacy-library');
legacyLib.doSomething();
```

### 순환 의존성 해결
순환 의존성(Circular Dependency)은 두 모듈이 서로를 import할 때 발생한다. 런타임 에러나 undefined 참조를 일으킬 수 있다:

```typescript
// ❌ 순환 의존성 문제

// Player.ts
import { Room } from './Room';

export class Player {
  constructor(public id: string, public room?: Room) {}
  
  joinRoom(room: Room) {
    this.room = room;
    room.addPlayer(this);
  }
}

// Room.ts
import { Player } from './Player';

export class Room {
  private players: Player[] = [];
  
  addPlayer(player: Player) {
    this.players.push(player);
    player.room = this;  // 순환 참조!
  }
}

// main.ts
import { Player } from './Player';
import { Room } from './Room';

// 모듈 로딩 순서에 따라 undefined 발생 가능
```

**해결 방법 1: 인터페이스 분리**

```typescript
// ✅ 인터페이스로 의존성 끊기

// types.ts
export interface IPlayer {
  id: string;
  joinRoom(room: IRoom): void;
}

export interface IRoom {
  id: string;
  addPlayer(player: IPlayer): void;
}

// Player.ts
import { IPlayer, IRoom } from './types';

export class Player implements IPlayer {
  constructor(public id: string, public room?: IRoom) {}
  
  joinRoom(room: IRoom) {
    this.room = room;
    room.addPlayer(this);
  }
}

// Room.ts
import { IRoom, IPlayer } from './types';

export class Room implements IRoom {
  constructor(public id: string) {}
  private players: IPlayer[] = [];
  
  addPlayer(player: IPlayer) {
    this.players.push(player);
  }
}
```

**해결 방법 2: 의존성 역전 (Dependency Inversion)**

```typescript
// ✅ 중재자 패턴 사용

// GameManager.ts
import { Player } from './Player';
import { Room } from './Room';

export class GameManager {
  joinPlayerToRoom(player: Player, room: Room) {
    room.addPlayer(player);
    player.setRoomId(room.id);
  }
  
  removePlayerFromRoom(player: Player, room: Room) {
    room.removePlayer(player.id);
    player.setRoomId(undefined);
  }
}

// Player.ts
export class Player {
  private roomId?: string;
  
  constructor(public id: string) {}
  
  setRoomId(roomId: string | undefined) {
    this.roomId = roomId;
  }
  
  getRoomId(): string | undefined {
    return this.roomId;
  }
}

// Room.ts
export class Room {
  private playerIds: Set<string> = new Set();
  
  constructor(public id: string) {}
  
  addPlayer(player: { id: string }) {
    this.playerIds.add(player.id);
  }
  
  removePlayer(playerId: string) {
    this.playerIds.delete(playerId);
  }
}
```

**해결 방법 3: 지연 import (동적 import)**

```typescript
// ✅ 필요한 시점에 import

// Player.ts
export class Player {
  constructor(public id: string) {}
  
  async joinRoom(roomId: string) {
    // 지연 로딩
    const { RoomManager } = await import('./RoomManager');
    const room = RoomManager.getInstance().getRoom(roomId);
    if (room) {
      room.addPlayer(this);
    }
  }
}
```

**모듈 구조 개선**도 중요하다:

```
// ❌ 나쁜 구조
src/
├── Player.ts        (Room import)
├── Room.ts          (Player import)
└── Game.ts          (Player, Room import)

// ✅ 개선된 구조
src/
├── types/
│   └── index.ts     (공통 인터페이스)
├── entities/
│   ├── Player.ts    (types만 import)
│   └── Room.ts      (types만 import)
└── managers/
    └── GameManager.ts  (entities import)
```

실제 예제:  

```typescript
// types/index.ts
export interface Entity {
  id: string;
  type: string;
}

export interface IPlayer extends Entity {
  type: 'player';
  nickname: string;
  level: number;
}

export interface IRoom extends Entity {
  type: 'room';
  maxPlayers: number;
  playerIds: string[];
}

// entities/Player.ts
import { IPlayer } from '../types';

export class Player implements IPlayer {
  readonly type = 'player' as const;
  
  constructor(
    public id: string,
    public nickname: string,
    public level: number = 1
  ) {}
}

// entities/Room.ts
import { IRoom } from '../types';

export class Room implements IRoom {
  readonly type = 'room' as const;
  playerIds: string[] = [];
  
  constructor(
    public id: string,
    public maxPlayers: number = 4
  ) {}
  
  addPlayerId(playerId: string): boolean {
    if (this.playerIds.length >= this.maxPlayers) {
      return false;
    }
    this.playerIds.push(playerId);
    return true;
  }
}

// managers/GameManager.ts
import { Player } from '../entities/Player';
import { Room } from '../entities/Room';
import { IPlayer, IRoom } from '../types';

export class GameManager {
  private players = new Map<string, Player>();
  private rooms = new Map<string, Room>();
  
  createPlayer(id: string, nickname: string): IPlayer {
    const player = new Player(id, nickname);
    this.players.set(id, player);
    return player;
  }
  
  createRoom(id: string, maxPlayers: number): IRoom {
    const room = new Room(id, maxPlayers);
    this.rooms.set(id, room);
    return room;
  }
  
  joinPlayerToRoom(playerId: string, roomId: string): boolean {
    const player = this.players.get(playerId);
    const room = this.rooms.get(roomId);
    
    if (!player || !room) {
      return false;
    }
    
    return room.addPlayerId(playerId);
  }
}
```

**순환 의존성 탐지**도 가능하다:

```bash
# madge 패키지로 순환 의존성 탐지
npm install -D madge

# 순환 의존성 확인
npx madge --circular --extensions ts src/

# 의존성 그래프 생성
npx madge --image graph.png src/
```

```
┌──────────────────────────────────────────────────┐
│        안티패턴과 개선 방법 요약                  │
├──────────────────────────────────────────────────┤
│                                                   │
│  1. any 남용                                      │
│     ❌ function process(data: any) { ... }       │
│     ✅ function process<T>(data: T) { ... }      │
│     ✅ function process(data: unknown) { ... }   │
│                                                   │
│  2. 타입 단언 남용                                │
│     ❌ const user = data as User;  // 검증 없이  │
│     ✅ if (isUser(data)) { ... }   // 타입 가드  │
│                                                   │
│  3. 순환 의존성                                   │
│     ❌ A imports B, B imports A                  │
│     ✅ 인터페이스 분리                            │
│     ✅ 중재자 패턴                                │
│     ✅ 동적 import                                │
│                                                   │
│  4. 타입 정의 중복                                │
│     ❌ 각 파일에서 동일 타입 재정의              │
│     ✅ 공통 types/ 폴더로 중앙화                 │
│                                                   │
│  5. 과도한 타입 복잡도                            │
│     ❌ 10단계 중첩 제네릭                        │
│     ✅ 단순한 타입으로 분할                       │
│                                                   │
│  6. strictNullChecks 무시                        │
│     ❌ const user = users.find(...); user.name   │
│     ✅ const user = users.find(...);             │
│        if (user) { user.name }                   │
│                                                   │
└──────────────────────────────────────────────────┘
```

**추가 안티패턴들:**

```typescript
// ❌ 타입 단언 남용
const element = document.getElementById("root") as HTMLDivElement;
element.innerHTML = "...";  // root가 없으면 런타임 에러!

// ✅ 타입 가드 사용
const element = document.getElementById("root");
if (element instanceof HTMLDivElement) {
  element.innerHTML = "...";
}

// ❌ Non-null assertion 남용
function getPlayer(id: string) {
  return players.find(p => p.id === id)!;  // ! 연산자
}

const player = getPlayer("unknown");
player.attack();  // 런타임 에러!

// ✅ 명시적 체크
function getPlayer(id: string): Player | undefined {
  return players.find(p => p.id === id);
}

const player = getPlayer("unknown");
if (player) {
  player.attack();
}

// ❌ 타입 정의 중복
// player.ts
interface Player { id: string; name: string; }

// room.ts
interface Player { id: string; name: string; }  // 중복!

// ✅ 공통 타입 정의
// types/entities.ts
export interface Player { id: string; name: string; }

// player.ts
import { Player } from './types/entities';

// room.ts
import { Player } from './types/entities';
```

**Strict 모드 옵션 활용:**

```json
// tsconfig.json
{
  "compilerOptions": {
    "strict": true,  // 모든 strict 옵션 활성화
    
    // 또는 개별 설정
    "strictNullChecks": true,        // null/undefined 엄격 체크
    "strictFunctionTypes": true,     // 함수 타입 엄격 체크
    "strictBindCallApply": true,     // bind/call/apply 타입 체크
    "strictPropertyInitialization": true,  // 클래스 속성 초기화 체크
    "noImplicitThis": true,          // this 타입 명시 강제
    "noImplicitAny": true,           // 암시적 any 금지
    "alwaysStrict": true,            // "use strict" 자동 삽입
    
    // 추가 안전장치
    "noUncheckedIndexedAccess": true,  // 인덱스 접근 시 undefined 체크
    "noImplicitReturns": true,         // 모든 경로에서 반환 강제
    "noFallthroughCasesInSwitch": true // switch 문 fallthrough 방지
  }
}
```


## 마무리
성능과 최적화는 단순히 빠른 컴파일만을 의미하지 않는다. 타입 시스템을 효율적으로 활용하고, 안티패턴을 피하며, 유지보수 가능한 코드를 작성하는 것이 진정한 최적화다.

**컴파일 옵션 최적화**로 개발 중 빠른 피드백을 받고, **타입 추론**을 활용해 간결한 코드를 작성하며, **흔한 실수**를 피해 안정적인 시스템을 구축할 수 있다.

특히 게임 서버처럼 복잡한 시스템에서는 이런 최적화가 생산성에 직결된다. 빌드가 10초 빨라지면 하루에 수십 번 컴파일할 때 상당한 시간을 절약한다. 타입 안전성을 유지하면 런타임 버그가 줄어 디버깅 시간도 단축된다.

다음 챕터에서는 Jest를 활용한 테스팅을 다룬다. 타입 안전한 테스트 코드를 작성해 코드 품질을 더욱 높일 수 있다.

  

# Chapter 9: 테스팅
게임 서버에서 버그는 곧 플레이어 경험 악화로 이어진다. 전투 계산이 잘못되거나, 아이템이 사라지거나, 방 입장이 안 되는 상황은 치명적이다. 테스트 코드는 이런 문제를 사전에 잡아내는 안전망이다. TypeScript와 Jest를 결합하면 타입 안전성과 함께 견고한 테스트를 작성할 수 있다.

  
## 9.1 Jest + TypeScript 설정
Jest는 JavaScript 생태계에서 가장 인기 있는 테스트 프레임워크다. TypeScript와 함께 사용하려면 약간의 설정이 필요하다.

### 테스트 환경 구성
먼저 필요한 패키지를 설치한다:

```bash
npm install -D jest @types/jest ts-jest
```

각 패키지의 역할은 다음과 같다:

- **jest**: 테스트 프레임워크 본체
- **@types/jest**: Jest API의 TypeScript 타입 정의
- **ts-jest**: TypeScript 파일을 Jest에서 실행할 수 있게 변환하는 프리셋

Jest 설정 파일을 생성한다:

```bash
npx ts-jest config:init
```

생성된 `jest.config.js`를 게임 서버에 맞게 수정한다:

```javascript
// jest.config.js
module.exports = {
  // TypeScript 파일 변환
  preset: 'ts-jest',
  
  // 테스트 실행 환경
  testEnvironment: 'node',
  
  // 테스트 파일 패턴
  testMatch: [
    '**/__tests__/**/*.ts',
    '**/?(*.)+(spec|test).ts'
  ],
  
  // 커버리지 수집 대상
  collectCoverageFrom: [
    'src/**/*.ts',
    '!src/**/*.d.ts',
    '!src/**/*.spec.ts',
    '!src/**/*.test.ts',
    '!src/server.ts'
  ],
  
  // 모듈 경로 매핑 (tsconfig.json의 paths와 일치)
  moduleNameMapper: {
    '^@/(.*)$': '<rootDir>/src/$1',
    '^@types/(.*)$': '<rootDir>/src/types/$1',
    '^@services/(.*)$': '<rootDir>/src/services/$1'
  },
  
  // 테스트 타임아웃 (게임 로직은 복잡할 수 있음)
  testTimeout: 10000,
  
  // 각 테스트 전 실행
  setupFilesAfterEnv: ['<rootDir>/jest.setup.ts'],
  
  // 커버리지 리포트 형식
  coverageReporters: ['text', 'lcov', 'html'],
  
  // 최소 커버리지 설정
  coverageThreshold: {
    global: {
      branches: 70,
      functions: 70,
      lines: 70,
      statements: 70
    }
  }
};
```

Jest 초기화 파일을 생성한다:

```typescript
// jest.setup.ts
// 전역 설정, 목 객체, 테스트 유틸리티 등
beforeAll(() => {
  console.log('🧪 테스트 시작...');
});

afterAll(() => {
  console.log('✅ 테스트 완료');
});

// 전역 타임아웃 설정
jest.setTimeout(10000);

// 날짜 목업 (테스트 재현성을 위해)
const MOCK_DATE = new Date('2024-01-01T00:00:00.000Z');
global.Date.now = jest.fn(() => MOCK_DATE.getTime());
```

`tsconfig.json`에 테스트 설정을 추가한다:

```json
// tsconfig.json
{
  "compilerOptions": {
    // ... 기존 설정
    "types": ["jest", "node"]
  },
  "exclude": [
    "node_modules",
    "dist",
    "**/*.spec.ts",
    "**/*.test.ts"
  ]
}
```

`package.json`에 테스트 스크립트를 추가한다:

```json
{
  "scripts": {
    "test": "jest",
    "test:watch": "jest --watch",
    "test:coverage": "jest --coverage",
    "test:verbose": "jest --verbose",
    "test:debug": "node --inspect-brk node_modules/.bin/jest --runInBand"
  }
}
```

프로젝트 구조는 다음과 같이 구성한다:

```
game-server/
├── src/
│   ├── services/
│   │   ├── UserService.ts
│   │   └── UserService.spec.ts       # 서비스 테스트
│   ├── utils/
│   │   ├── combat.ts
│   │   └── combat.spec.ts            # 유틸리티 테스트
│   └── managers/
│       ├── RoomManager.ts
│       └── RoomManager.spec.ts       # 매니저 테스트
├── __tests__/
│   ├── integration/                   # 통합 테스트
│   │   └── game-flow.test.ts
│   └── e2e/                          # E2E 테스트
│       └── multiplayer.test.ts
├── jest.config.js
├── jest.setup.ts
└── package.json
```

테스트가 정상 작동하는지 확인한다:

```typescript
// src/utils/math.ts
export function add(a: number, b: number): number {
  return a + b;
}

// src/utils/math.spec.ts
import { add } from './math';

describe('Math Utils', () => {
  it('두 숫자를 더한다', () => {
    expect(add(2, 3)).toBe(5);
  });
  
  it('음수도 처리한다', () => {
    expect(add(-1, 1)).toBe(0);
  });
});
```

테스트를 실행한다:

```bash
npm test

# 출력:
# PASS  src/utils/math.spec.ts
#   Math Utils
#     ✓ 두 숫자를 더한다 (2 ms)
#     ✓ 음수도 처리한다 (1 ms)
# 
# Test Suites: 1 passed, 1 total
# Tests:       2 passed, 2 total
```

```
┌────────────────────────────────────────────┐
│         Jest + TypeScript 구조              │
├────────────────────────────────────────────┤
│                                            │
│  TypeScript 파일                            │
│       ↓                                    │
│  ts-jest 변환                               │
│       ↓                                    │
│  Jest 실행                                  │
│       ↓                                    │
│  ┌────────────────────────────────┐        │
│  │  describe('테스트 스위트', ...)  │        │
│  │    ├─ it('테스트 케이스 1')      │        │
│  │    ├─ it('테스트 케이스 2')      │        │
│  │    └─ it('테스트 케이스 3')      │        │
│  └────────────────────────────────┘        │
│       ↓                                     │
│  결과 리포트                                 │
│  (통과/실패/커버리지)                         │
│                                            │
└────────────────────────────────────────────┘
```
  
  
## 9.2 유닛 테스트 작성
유닛 테스트는 개별 함수나 클래스를 독립적으로 테스트한다. 게임 로직의 정확성을 보장하는 핵심이다.

### 게임 로직 테스트 예제
전투 시스템을 테스트해보자. 먼저 전투 계산 로직을 구현한다:

```typescript
// src/utils/combat.ts
export interface CombatStats {
  attack: number;
  defense: number;
  hp: number;
  critRate: number;  // 0.0 ~ 1.0
  critMultiplier: number;
}

export interface CombatResult {
  damage: number;
  isCritical: boolean;
  remainingHp: number;
  isDead: boolean;
}

export function calculateDamage(
  attacker: CombatStats,
  defender: CombatStats,
  randomValue: number = Math.random()
): CombatResult {
  // 기본 데미지 계산
  let baseDamage = Math.max(0, attacker.attack - defender.defense);
  
  // 크리티컬 판정
  const isCritical = randomValue < attacker.critRate;
  const damage = isCritical 
    ? Math.floor(baseDamage * attacker.critMultiplier)
    : baseDamage;
  
  // 남은 체력 계산
  const remainingHp = Math.max(0, defender.hp - damage);
  const isDead = remainingHp === 0;
  
  return {
    damage,
    isCritical,
    remainingHp,
    isDead
  };
}

export function applyDamage(
  stats: CombatStats,
  damage: number
): CombatStats {
  return {
    ...stats,
    hp: Math.max(0, stats.hp - damage)
  };
}

export function isAlive(stats: CombatStats): boolean {
  return stats.hp > 0;
}
```

이제 테스트를 작성한다:

```typescript
// src/utils/combat.spec.ts
import { calculateDamage, applyDamage, isAlive, CombatStats } from './combat';

describe('Combat System', () => {
  // 테스트용 기본 스탯
  const defaultAttacker: CombatStats = {
    attack: 50,
    defense: 10,
    hp: 100,
    critRate: 0.2,
    critMultiplier: 2.0
  };
  
  const defaultDefender: CombatStats = {
    attack: 30,
    defense: 20,
    hp: 100,
    critRate: 0.1,
    critMultiplier: 1.5
  };
  
  describe('calculateDamage', () => {
    it('기본 데미지를 정확히 계산한다', () => {
      // 크리티컬 없이 (randomValue = 1.0)
      const result = calculateDamage(defaultAttacker, defaultDefender, 1.0);
      
      // attack(50) - defense(20) = 30
      expect(result.damage).toBe(30);
      expect(result.isCritical).toBe(false);
      expect(result.remainingHp).toBe(70);
      expect(result.isDead).toBe(false);
    });
    
    it('크리티컬 데미지를 정확히 계산한다', () => {
      // 크리티컬 발동 (randomValue = 0.0)
      const result = calculateDamage(defaultAttacker, defaultDefender, 0.0);
      
      // (attack - defense) * critMultiplier = 30 * 2.0 = 60
      expect(result.damage).toBe(60);
      expect(result.isCritical).toBe(true);
      expect(result.remainingHp).toBe(40);
      expect(result.isDead).toBe(false);
    });
    
    it('방어력이 공격력보다 높으면 데미지가 0이다', () => {
      const weakAttacker: CombatStats = {
        ...defaultAttacker,
        attack: 10
      };
      
      const result = calculateDamage(weakAttacker, defaultDefender, 1.0);
      
      expect(result.damage).toBe(0);
      expect(result.remainingHp).toBe(100);
    });
    
    it('치명타로 적을 처치하면 isDead가 true다', () => {
      const strongAttacker: CombatStats = {
        ...defaultAttacker,
        attack: 100,
        critMultiplier: 3.0
      };
      
      const result = calculateDamage(strongAttacker, defaultDefender, 0.0);
      
      // (100 - 20) * 3.0 = 240 > 100 hp
      expect(result.isDead).toBe(true);
      expect(result.remainingHp).toBe(0);
    });
    
    it('체력은 음수가 되지 않는다', () => {
      const weakDefender: CombatStats = {
        ...defaultDefender,
        hp: 10
      };
      
      const result = calculateDamage(defaultAttacker, weakDefender, 1.0);
      
      expect(result.remainingHp).toBeGreaterThanOrEqual(0);
    });
  });
  
  describe('applyDamage', () => {
    it('데미지만큼 HP가 감소한다', () => {
      const damaged = applyDamage(defaultDefender, 30);
      
      expect(damaged.hp).toBe(70);
      expect(damaged.attack).toBe(defaultDefender.attack);
      expect(damaged.defense).toBe(defaultDefender.defense);
    });
    
    it('데미지가 HP보다 크면 0이 된다', () => {
      const damaged = applyDamage(defaultDefender, 200);
      
      expect(damaged.hp).toBe(0);
    });
    
    it('원본 객체를 변경하지 않는다 (불변성)', () => {
      const original = { ...defaultDefender };
      const damaged = applyDamage(defaultDefender, 50);
      
      expect(defaultDefender.hp).toBe(original.hp);
      expect(damaged.hp).toBe(50);
    });
  });
  
  describe('isAlive', () => {
    it('HP가 0보다 크면 생존 상태다', () => {
      expect(isAlive(defaultDefender)).toBe(true);
      expect(isAlive({ ...defaultDefender, hp: 1 })).toBe(true);
    });
    
    it('HP가 0이면 사망 상태다', () => {
      expect(isAlive({ ...defaultDefender, hp: 0 })).toBe(false);
    });
  });
});
```

테스트를 실행한다:

```bash
npm test combat.spec.ts

# 출력:
# PASS  src/utils/combat.spec.ts
#   Combat System
#     calculateDamage
#       ✓ 기본 데미지를 정확히 계산한다 (3 ms)
#       ✓ 크리티컬 데미지를 정확히 계산한다(2 ms)
#       ✓ 방어력이 공격력보다 높으면 데미지가 0이다
#       ✓ 치명타로 적을 처치하면 isDead가 true다
#       ✓ 체력은 음수가 되지 않는다
#     applyDamage
#       ✓ 데미지만큼 HP가 감소한다
#       ✓ 데미지가 HP보다 크면 0이 된다
#       ✓ 원본 객체를 변경하지 않는다 (불변성)
#     isAlive
#       ✓ HP가 0보다 크면 생존 상태다
#       ✓ HP가 0이면 사망 상태다
```

**경계값 테스트(Boundary Testing)**도 중요하다:

```typescript
describe('Combat System - Edge Cases', () => {
  it('모든 스탯이 0인 경우', () => {
    const zeroStats: CombatStats = {
      attack: 0,
      defense: 0,
      hp: 0,
      critRate: 0,
      critMultiplier: 0
    };
    
    const result = calculateDamage(zeroStats, zeroStats, 0.5);
    
    expect(result.damage).toBe(0);
    expect(result.isDead).toBe(true);
  });
  
  it('크리티컬 확률이 100%인 경우', () => {
    const alwaysCrit: CombatStats = {
      ...defaultAttacker,
      critRate: 1.0
    };
    
    const result = calculateDamage(alwaysCrit, defaultDefender, 0.5);
    
    expect(result.isCritical).toBe(true);
  });
  
  it('극단적으로 높은 수치', () => {
    const maxStats: CombatStats = {
      attack: Number.MAX_SAFE_INTEGER,
      defense: 0,
      hp: 100,
      critRate: 0,
      critMultiplier: 1
    };
    
    const result = calculateDamage(maxStats, defaultDefender, 1.0);
    
    expect(result.damage).toBeGreaterThan(0);
    expect(result.isDead).toBe(true);
  });
});
```

### Mock 객체 활용
외부 의존성(데이터베이스, API, 파일 시스템 등)을 테스트할 때는 Mock 객체를 사용한다. 실제 리소스 없이도 테스트가 가능하다.

```typescript
// src/services/UserService.ts
import prisma from '../db/prisma';
import { User } from '@prisma/client';

export class UserService {
  async getUser(userId: string): Promise<User | null> {
    const user = await prisma.user.findUnique({
      where: { id: userId }
    });
    return user;
  }
  
  async updateUserLevel(userId: string, level: number): Promise<User> {
    const user = await prisma.user.update({
      where: { id: userId },
      data: { level }
    });
    return user;
  }
  
  async addUserExp(userId: string, expAmount: number): Promise<User> {
    const user = await this.getUser(userId);
    
    if (!user) {
      throw new Error('사용자를 찾을 수 없다');
    }
    
    let newExp = user.exp + expAmount;
    let newLevel = user.level;
    
    // 레벨업 로직
    while (newExp >= 100 * newLevel) {
      newExp -= 100 * newLevel;
      newLevel++;
    }
    
    return this.updateUserLevel(userId, newLevel);
  }
}
```

Mock을 사용한 테스트:

```typescript
// src/services/UserService.spec.ts
import { UserService } from './UserService';
import prisma from '../db/prisma';
import { User } from '@prisma/client';

// Prisma 클라이언트 Mock
jest.mock('../db/prisma', () => ({
  user: {
    findUnique: jest.fn(),
    update: jest.fn()
  }
}));

describe('UserService', () => {
  let userService: UserService;
  
  // 테스트용 사용자 데이터
  const mockUser: User = {
    id: 'user-001',
    username: 'testuser',
    nickname: 'TestHero',
    password: 'hashed_password',
    level: 5,
    exp: 50,
    gold: 1000,
    createdAt: new Date(),
    updatedAt: new Date()
  };
  
  beforeEach(() => {
    userService = new UserService();
    jest.clearAllMocks();
  });
  
  describe('getUser', () => {
    it('사용자를 찾으면 반환한다', async () => {
      // Mock 설정
      (prisma.user.findUnique as jest.Mock).mockResolvedValue(mockUser);
      
      const result = await userService.getUser('user-001');
      
      expect(result).toEqual(mockUser);
      expect(prisma.user.findUnique).toHaveBeenCalledWith({
        where: { id: 'user-001' }
      });
      expect(prisma.user.findUnique).toHaveBeenCalledTimes(1);
    });
    
    it('사용자가 없으면 null을 반환한다', async () => {
      (prisma.user.findUnique as jest.Mock).mockResolvedValue(null);
      
      const result = await userService.getUser('nonexistent');
      
      expect(result).toBeNull();
    });
  });
  
  describe('updateUserLevel', () => {
    it('사용자 레벨을 업데이트한다', async () => {
      const updatedUser = { ...mockUser, level: 10 };
      (prisma.user.update as jest.Mock).mockResolvedValue(updatedUser);
      
      const result = await userService.updateUserLevel('user-001', 10);
      
      expect(result.level).toBe(10);
      expect(prisma.user.update).toHaveBeenCalledWith({
        where: { id: 'user-001' },
        data: { level: 10 }
      });
    });
  });
  
  describe('addUserExp', () => {
    it('경험치를 추가하고 레벨업하지 않는다', async () => {
      (prisma.user.findUnique as jest.Mock).mockResolvedValue(mockUser);
      (prisma.user.update as jest.Mock).mockResolvedValue({
        ...mockUser,
        exp: 80
      });
      
      const result = await userService.addUserExp('user-001', 30);
      
      // level 5, exp 50 + 30 = 80 (레벨업 필요 exp = 500)
      expect(result.level).toBe(5);
    });
    
    it('경험치가 충분하면 레벨업한다', async () => {
      (prisma.user.findUnique as jest.Mock).mockResolvedValue(mockUser);
      (prisma.user.update as jest.Mock).mockResolvedValue({
        ...mockUser,
        level: 6,
        exp: 0
      });
      
      // level 5, exp 50 + 450 = 500 (레벨업!)
      const result = await userService.addUserExp('user-001', 450);
      
      expect(result.level).toBe(6);
    });
    
    it('사용자가 없으면 에러를 던진다', async () => {
      (prisma.user.findUnique as jest.Mock).mockResolvedValue(null);
      
      await expect(
        userService.addUserExp('nonexistent', 100)
      ).rejects.toThrow('사용자를 찾을 수 없다');
    });
  });
});
```

**Spy 함수**로 호출 여부를 확인할 수도 있다:

```typescript
describe('UserService - Spy 예제', () => {
  it('getUser가 내부적으로 호출된다', async () => {
    const getUserSpy = jest.spyOn(userService, 'getUser');
    
    (prisma.user.findUnique as jest.Mock).mockResolvedValue(mockUser);
    (prisma.user.update as jest.Mock).mockResolvedValue(mockUser);
    
    await userService.addUserExp('user-001', 50);
    
    expect(getUserSpy).toHaveBeenCalledWith('user-001');
    expect(getUserSpy).toHaveBeenCalledTimes(1);
    
    getUserSpy.mockRestore();
  });
});
```

**Timer Mock**도 유용하다:

```typescript
// src/managers/GameTimer.ts
export class GameTimer {
  private timerId?: NodeJS.Timeout;
  
  start(callback: () => void, interval: number): void {
    this.timerId = setInterval(callback, interval);
  }
  
  stop(): void {
    if (this.timerId) {
      clearInterval(this.timerId);
      this.timerId = undefined;
    }
  }
}

// src/managers/GameTimer.spec.ts
describe('GameTimer', () => {
  let timer: GameTimer;
  
  beforeEach(() => {
    timer = new GameTimer();
    jest.useFakeTimers();
  });
  
  afterEach(() => {
    timer.stop();
    jest.useRealTimers();
  });
  
  it('지정된 간격으로 콜백을 호출한다', () => {
    const callback = jest.fn();
    
    timer.start(callback, 1000);
    
    expect(callback).not.toHaveBeenCalled();
    
    jest.advanceTimersByTime(1000);
    expect(callback).toHaveBeenCalledTimes(1);
    
    jest.advanceTimersByTime(2000);
    expect(callback).toHaveBeenCalledTimes(3);
  });
  
  it('stop을 호출하면 타이머가 멈춘다', () => {
    const callback = jest.fn();
    
    timer.start(callback, 1000);
    jest.advanceTimersByTime(1000);
    
    timer.stop();
    jest.advanceTimersByTime(5000);
    
    expect(callback).toHaveBeenCalledTimes(1);
  });
});
```

```
┌──────────────────────────────────────────────────┐
│            Mock 객체 활용 패턴                     │
├──────────────────────────────────────────────────┤
│                                                  │
│  실제 코드                                        │
│  ┌────────────────────────────────┐             │
│  │  UserService                   │             │
│  │    └─ prisma.user.findUnique() │             │
│  └────────────────────────────────┘             │
│         ↓ (실제 DB 호출)                         │
│  ┌────────────────────────────────┐             │
│  │  PostgreSQL Database           │             │
│  └────────────────────────────────┘             │
│                                                  │
│  테스트 코드                                      │
│  ┌────────────────────────────────┐             │
│  │  UserService                   │             │
│  │    └─ prisma.user.findUnique() │             │
│  └────────────────────────────────┘             │
│         ↓ (Mock 반환)                            │
│  ┌────────────────────────────────┐             │
│  │  jest.mock() - Mock Data       │             │
│  │  { id: '...', name: '...' }    │             │
│  └────────────────────────────────┘             │
│                                                 │
│  ✓ DB 없이도 테스트 가능                          │
│  ✓ 빠른 실행 속도                                 │
│  ✓ 격리된 테스트 환경                             │
│  ✓ 예측 가능한 결과                               │
│                                                 │
└─────────────────────────────────────────────────┘
```


## 9.3 타입 안전한 테스트 작성
TypeScript의 타입 시스템을 테스트에도 활용하면 더욱 견고한 코드를 작성할 수 있다.

**타입 체크를 활용한 테스트:**

```typescript
// src/types/packet.ts
export type PacketType = 
  | 'PLAYER_MOVE'
  | 'PLAYER_ATTACK'
  | 'PLAYER_CHAT'
  | 'GAME_START';

export interface BasePacket {
  type: PacketType;
  timestamp: number;
}

export interface PlayerMovePacket extends BasePacket {
  type: 'PLAYER_MOVE';
  data: {
    x: number;
    y: number;
  };
}

export interface PlayerAttackPacket extends BasePacket {
  type: 'PLAYER_ATTACK';
  data: {
    targetId: string;
    skillId: number;
  };
}

export type Packet = PlayerMovePacket | PlayerAttackPacket;

// src/utils/packetHandler.ts
export function handlePacket(packet: Packet): string {
  switch (packet.type) {
    case 'PLAYER_MOVE':
      return `이동: (${packet.data.x}, ${packet.data.y})`;
    case 'PLAYER_ATTACK':
      return `공격: ${packet.data.targetId}`;
    default:
      // TypeScript가 모든 케이스를 처리했는지 확인
      const exhaustiveCheck: never = packet;
      throw new Error(`처리되지 않은 패킷: ${exhaustiveCheck}`);
  }
}
```

테스트 작성:

```typescript
// src/utils/packetHandler.spec.ts
import { handlePacket, Packet } from './packetHandler';

describe('PacketHandler', () => {
  it('PLAYER_MOVE 패킷을 처리한다', () => {
    const packet: Packet = {
      type: 'PLAYER_MOVE',
      timestamp: Date.now(),
      data: { x: 10, y: 20 }
    };
    
    const result = handlePacket(packet);
    
    expect(result).toBe('이동: (10, 20)');
  });
  
  it('PLAYER_ATTACK 패킷을 처리한다', () => {
    const packet: Packet = {
      type: 'PLAYER_ATTACK',
      timestamp: Date.now(),
      data: { targetId: 'enemy-001', skillId: 5 }
    };
    
    const result = handlePacket(packet);
    
    expect(result).toBe('공격: enemy-001');
  });
  
  // ❌ 컴파일 에러: 잘못된 타입
  // it('잘못된 패킷 타입', () => {
  //   const packet: Packet = {
  //     type: 'INVALID_TYPE',  // 컴파일 에러!
  //     timestamp: Date.now(),
  //     data: {}
  //   };
  //   handlePacket(packet);
  // });
});
```

**제네릭 테스트 헬퍼:**

```typescript
// __tests__/helpers/testUtils.ts
export function createMockUser<T extends Partial<User>>(
  overrides?: T
): User & T {
  const defaults: User = {
    id: 'test-user-id',
    username: 'testuser',
    nickname: 'TestHero',
    password: 'hashed_password',
    level: 1,
    exp: 0,
    gold: 1000,
    createdAt: new Date(),
    updatedAt: new Date()
  };
  
  return { ...defaults, ...overrides };
}

export function createMockPlayer<T extends Partial<Player>>(
  overrides?: T
): Player & T {
  const defaults: Player = {
    id: 'test-player-id',
    nickname: 'TestPlayer',
    position: { x: 0, y: 0 },
    hp: 100,
    maxHp: 100,
    level: 1
  };
  
  return { ...defaults, ...overrides };
}

// 사용 예제
describe('User Tests', () => {
  it('레벨 10 사용자 생성', () => {
    const user = createMockUser({ level: 10, exp: 500 });
    
    expect(user.level).toBe(10);
    expect(user.exp).toBe(500);
    expect(user.username).toBe('testuser');  // 기본값 유지
  });
});
```

**커스텀 Matcher:**

```typescript
// __tests__/helpers/customMatchers.ts
export const customMatchers = {
  toBeValidPlayer(received: any) {
    const pass = 
      typeof received.id === 'string' &&
      typeof received.nickname === 'string' &&
      typeof received.hp === 'number' &&
      received.hp >= 0 &&
      received.hp <= received.maxHp;
    
    return {
      pass,
      message: () => 
        pass
          ? `예상: ${received}가 유효한 플레이어가 아니어야 한다`
          : `예상: ${received}가 유효한 플레이어여야 한다`
    };
  },
  
  toBeInRange(received: number, min: number, max: number) {
    const pass = received >= min && received <= max;
    
    return {
      pass,
      message: () =>
        pass
          ? `예상: ${received}가 ${min}~${max} 범위 밖이어야 한다`
          : `예상: ${received}가 ${min}~${max} 범위 안이어야 한다`
    };
  }
};

declare global {
  namespace jest {
    interface Matchers<R> {
      toBeValidPlayer(): R;
      toBeInRange(min: number, max: number): R;
    }
  }
}

// jest.setup.ts에 추가
import { customMatchers } from './__tests__/helpers/customMatchers';

expect.extend(customMatchers);

// 사용 예제
describe('Custom Matchers', () => {
  it('유효한 플레이어 검증', () => {
    const player = createMockPlayer();
    expect(player).toBeValidPlayer();
  });
  
  it('데미지 범위 검증', () => {
    const damage = calculateDamage(attacker, defender);
    expect(damage).toBeInRange(0, 100);
  });
});
```

**비동기 테스트:**

```typescript
// src/services/GameService.ts
export class GameService {
  async startGame(roomId: string): Promise<void> {
    await this.validateRoom(roomId);
    await this.initializeGame(roomId);
    await this.notifyPlayers(roomId);
  }
  
  private async validateRoom(roomId: string): Promise<void> {
    // 비동기 검증 로직
    await new Promise(resolve => setTimeout(resolve, 100));
  }
  
  private async initializeGame(roomId: string): Promise<void> {
    // 게임 초기화
    await new Promise(resolve => setTimeout(resolve, 100));
  }
  
  private async notifyPlayers(roomId: string): Promise<void> {
    // 플레이어 알림
    await new Promise(resolve => setTimeout(resolve, 100));
  }
}

// src/services/GameService.spec.ts
describe('GameService', () => {
  let gameService: GameService;
  
  beforeEach(() => {
    gameService = new GameService();
  });
  
  it('게임을 시작한다 (async/await)', async () => {
    await expect(gameService.startGame('room-001')).resolves.toBeUndefined();
  });
  
  it('게임을 시작한다 (done 콜백)', (done) => {
    gameService.startGame('room-001')
      .then(() => {
        expect(true).toBe(true);
        done();
      })
      .catch(done);
  });
  
  it('에러를 올바르게 처리한다', async () => {
    jest.spyOn(gameService as any, 'validateRoom')
      .mockRejectedValue(new Error('방이 유효하지 않다'));
    
    await expect(gameService.startGame('invalid'))
      .rejects
      .toThrow('방이 유효하지 않다');
  });
});
```

**통합 테스트 예제:**

```typescript
// __tests__/integration/game-flow.test.ts
import { RoomManager } from '../../src/managers/RoomManager';
import { Player } from '../../src/entities/Player';

describe('게임 플로우 통합 테스트', () => {
  let roomManager: RoomManager;
  
  beforeEach(() => {
    roomManager = RoomManager.getInstance();
  });
  
  afterEach(() => {
    // 테스트 후 정리
    roomManager.getAllRooms().forEach(room => {
      roomManager.deleteRoom(room.getId());
    });
  });
  
  it('전체 게임 플로우가 정상 작동한다', () => {
    // 1. 방 생성
    const room = roomManager.createRoom('test-room', 2);
    expect(room).toBeTruthy();
    
    // 2. 플레이어 입장
    const player1Added = room!.addPlayer('player-1', 'Hero');
    const player2Added = room!.addPlayer('player-2', 'Warrior');
    expect(player1Added).toBe(true);
    expect(player2Added).toBe(true);
    
    // 3. 게임 시작
    const started = room!.startGame();
    expect(started).toBe(true);
    
    // 4. 게임 상태 확인
    const gameState = room!.getGameState();
    expect(gameState.status).toBe('playing');
    expect(gameState.players).toHaveLength(2);
    expect(gameState.currentTurn).toBeTruthy();
    
    // 5. 플레이어 퇴장
    room!.removePlayer('player-1');
    expect(room!.getPlayerCount()).toBe(1);
    
    // 6. 게임 종료
    room!.stopGame();
    expect(room!.getGameState().status).toBe('finished');
  });
  
  it('방이 가득 차면 추가 입장이 불가능하다', () => {
    const room = roomManager.createRoom('full-room', 2);
    
    room!.addPlayer('player-1', 'Hero1');
    room!.addPlayer('player-2', 'Hero2');
    
    const player3Added = room!.addPlayer('player-3', 'Hero3');
    expect(player3Added).toBe(false);
    expect(room!.getPlayerCount()).toBe(2);
  });
});
```

**테스트 커버리지 확인:**

```bash
npm run test:coverage

# 출력:
# ------------------|---------|----------|---------|---------|
# File              | % Stmts | % Branch | % Funcs | % Lines |
# ------------------|---------|----------|---------|---------|
# All files         |   85.5  |   78.2   |   90.1  |   86.3  |
#  combat.ts        |   100   |   100    |   100   |   100   |
#  UserService.ts   |   82.5  |   75.0   |   88.9  |   83.2  |
#  RoomManager.ts   |   78.3  |   70.5   |   85.7  |   79.1  |
# ------------------|---------|----------|---------|---------|
```

커버리지가 낮은 부분을 확인하고 테스트를 추가한다:

```bash
# HTML 리포트 생성
npm run test:coverage

# dist/coverage/lcov-report/index.html 열기
# 커버되지 않은 라인이 빨간색으로 표시됨
```

```
┌──────────────────────────────────────────────────┐
│          테스트 피라미드                          │
├──────────────────────────────────────────────────┤
│                                                   │
│                   E2E Tests                       │
│                   ▲                              │
│                  ╱ ╲                             │
│                 ╱   ╲                            │
│                ╱     ╲                           │
│            Integration Tests                      │
│              ▲                                   │
│             ╱ ╲                                  │
│            ╱   ╲                                 │
│           ╱     ╲                                │
│          ╱       ╲                               │
│      Unit Tests                                   │
│      ▲                                           │
│     ╱ ╲                                          │
│    ╱   ╲                                         │
│   ╱     ╲                                        │
│  ╱       ╲                                       │
│ ╱_________╲                                      │
│                                                   │
│  Unit Tests (많음, 빠름)                         │
│  - 개별 함수/클래스 테스트                        │
│  - Mock 객체 활용                                │
│  - 빠른 피드백                                   │
│                                                   │
│  Integration Tests (중간)                        │
│  - 여러 모듈 통합 테스트                         │
│  - 실제 의존성 일부 사용                         │
│  - 시스템 동작 검증                              │
│                                                   │
│  E2E Tests (적음, 느림)                          │
│  - 전체 시스템 테스트                            │
│  - 실제 환경과 유사                              │
│  - 사용자 시나리오 검증                          │
│                                                   │
└──────────────────────────────────────────────────┘
```


## 마무리
테스트는 선택이 아닌 필수다. 게임 서버처럼 많은 플레이어가 실시간으로 상호작용하는 시스템에서 버그는 치명적이다. TypeScript와 Jest를 결합하면 타입 안전성과 함께 견고한 테스트 환경을 구축할 수 있다.

**Jest + TypeScript 설정**으로 테스트 인프라를 구축하고, **유닛 테스트**로 개별 로직의 정확성을 보장하며, **Mock 객체**로 외부 의존성을 격리한다. **타입 안전한 테스트**를 작성하면 컴파일 시점에 더 많은 에러를 잡아낼 수 있다.

테스트 커버리지 목표는 프로젝트마다 다르지만, 핵심 비즈니스 로직(전투 시스템, 아이템 관리, 방 매칭 등)은 최소 80% 이상을 유지하는 것이 좋다. 테스트 작성이 처음엔 번거롭지만, 장기적으로는 개발 속도를 높이고 버그를 줄여준다.

다음 챕터에서는 지금까지 배운 모든 내용을 종합해 실전 미니 프로젝트를 구현한다. 실시간 멀티플레이어 게임 서버를 처음부터 끝까지 만들어본다.   

   

# Chapter 10: 실전 미니 프로젝트
지금까지 배운 TypeScript의 모든 개념을 종합하여 실제로 동작하는 실시간 멀티플레이어 게임 서버를 구축한다. 이 장에서는 턴제 배틀 시스템을 갖춘 간단한 게임 서버를 처음부터 끝까지 구현하면서, TypeScript가 어떻게 실전 프로젝트에서 코드 품질과 생산성을 향상시키는지 직접 경험한다.
  
## 10.1 실시간 멀티플레이어 게임 서버 만들기

### 프로젝트 개요: "타입배틀" 서버
우리가 만들 게임은 2명의 플레이어가 실시간으로 대결하는 턴제 배틀 게임이다. 플레이어는 공격, 방어, 스킬 사용 등의 행동을 선택하고, 서버는 턴을 진행하며 게임 상태를 동기화한다. 이 프로젝트는 실제 게임 서버에서 마주치는 핵심 문제들을 다룬다.

```
┌──────────────────────────────────────────────────────────┐
│              TypeBattle Game Server Architecture          │
├──────────────────────────────────────────────────────────┤
│                                                            │
│   ┌─────────┐         ┌──────────────┐                   │
│   │ Client  │◄───────►│   Socket.io  │                   │
│   │ Player1 │  Events  │   Gateway    │                   │
│   └─────────┘         └──────┬───────┘                   │
│                              │                            │
│   ┌─────────┐         ┌──────▼───────┐                   │
│   │ Client  │◄───────►│    Room      │                   │
│   │ Player2 │  Sync    │   Manager    │                   │
│   └─────────┘         └──────┬───────┘                   │
│                              │                            │
│                       ┌──────▼───────┐                   │
│                       │  Battle      │                   │
│                       │  Engine      │                   │
│                       └──────────────┘                   │
│                                                            │
└──────────────────────────────────────────────────────────┘
```

### 요구사항 정의
프로젝트를 시작하기 전에 명확한 요구사항을 정의한다. 경험 많은 서버 개발자라면 이 단계가 얼마나 중요한지 잘 알 것이다.

**기능 요구사항**

- 플레이어는 서버에 접속하여 닉네임을 등록한다
- 2명의 플레이어가 매칭되면 자동으로 방이 생성된다
- 각 플레이어는 HP 100, MP 50으로 시작한다
- 턴마다 플레이어는 공격(Attack), 방어(Defend), 스킬(Skill) 중 하나를 선택한다
- 양쪽 플레이어가 행동을 선택하면 턴이 진행된다
- 한 플레이어의 HP가 0 이하가 되면 게임이 종료된다
- 모든 게임 상태 변화는 실시간으로 클라이언트에 전달된다

**기술 요구사항**

- 타입 안전한 패킷 통신 시스템
- 명확한 에러 핸들링
- 확장 가능한 아키텍처
- 테스트 가능한 구조
  

### 프로젝트 구조 설계
실제 게임 서버의 폴더 구조를 설계한다. TypeScript의 모듈 시스템을 활용하여 관심사를 명확히 분리한다.

```
typebattle-server/
├── src/
│   ├── index.ts                 # 서버 엔트리 포인트
│   ├── types/                   # 타입 정의
│   │   ├── index.ts
│   │   ├── packet.types.ts      # 패킷 타입
│   │   ├── player.types.ts      # 플레이어 타입
│   │   └── game.types.ts        # 게임 로직 타입
│   ├── core/                    # 핵심 게임 로직
│   │   ├── Player.ts
│   │   ├── BattleEngine.ts
│   │   └── Room.ts
│   ├── managers/                # 관리 시스템
│   │   ├── RoomManager.ts
│   │   └── PlayerManager.ts
│   ├── handlers/                # 이벤트 핸들러
│   │   ├── ConnectionHandler.ts
│   │   └── GameHandler.ts
│   └── utils/                   # 유틸리티
│       └── validators.ts
├── tests/
│   └── core/
│       └── BattleEngine.test.ts
├── tsconfig.json
└── package.json
```

### 1단계: 타입 정의 - 프로젝트의 뼈대
모든 것은 타입 정의에서 시작된다. 경력 개발자로서 당신은 명확한 인터페이스가 얼마나 중요한지 알 것이다. TypeScript는 이를 강제한다.

**src/types/game.types.ts**

```typescript
// 게임 상태를 표현하는 열거형
export enum GameState {
  WAITING = 'WAITING',
  IN_PROGRESS = 'IN_PROGRESS',
  FINISHED = 'FINISHED'
}

// 플레이어 행동 타입
export enum ActionType {
  ATTACK = 'ATTACK',
  DEFEND = 'DEFEND',
  SKILL = 'SKILL'
}

// 플레이어 스탯 인터페이스
export interface PlayerStats {
  hp: number;
  maxHp: number;
  mp: number;
  maxMp: number;
  attack: number;
  defense: number;
}

// 플레이어 행동 인터페이스
export interface PlayerAction {
  playerId: string;
  type: ActionType;
  timestamp: number;
}

// 턴 결과 인터페이스
export interface TurnResult {
  turnNumber: number;
  actions: {
    player1: PlayerAction;
    player2: PlayerAction;
  };
  damages: {
    player1Damage: number;
    player2Damage: number;
  };
  newStats: {
    player1: PlayerStats;
    player2: PlayerStats;
  };
  logs: string[];
}
```

이 타입들은 프로젝트 전체에서 사용되며, 컴파일 타임에 많은 버그를 잡아낸다. 예를 들어 `ActionType`을 잘못 입력하면 즉시 에러가 발생한다.

**src/types/packet.types.ts**

```typescript
import { PlayerStats, ActionType, TurnResult, GameState } from './game.types';

// 클라이언트 → 서버 패킷
export interface C2S_Packets {
  'player:join': {
    nickname: string;
  };
  'game:action': {
    action: ActionType;
  };
  'player:leave': {};
}

// 서버 → 클라이언트 패킷
export interface S2C_Packets {
  'player:joined': {
    playerId: string;
    nickname: string;
  };
  'game:matched': {
    roomId: string;
    opponent: {
      id: string;
      nickname: string;
    };
    yourStats: PlayerStats;
    opponentStats: PlayerStats;
  };
  'game:state': {
    state: GameState;
    turnNumber: number;
    yourStats: PlayerStats;
    opponentStats: PlayerStats;
    isYourTurn: boolean;
  };
  'game:turn-result': TurnResult;
  'game:finished': {
    winnerId: string;
    winnerNickname: string;
    reason: string;
  };
  'error': {
    code: string;
    message: string;
  };
}

// 타입 안전한 이벤트 이름 추출
export type C2S_EventNames = keyof C2S_Packets;
export type S2C_EventNames = keyof S2C_Packets;
```

이 패킷 타입 정의는 매우 강력하다. Socket.io 이벤트를 타입 안전하게 만들어주며, 잘못된 이벤트 이름이나 데이터 구조를 사용하는 것을 방지한다. 이는 런타임 에러를 대폭 줄여준다.

### 2단계: 핵심 게임 로직 구현
이제 타입이 정의되었으니, 실제 게임 로직을 구현한다. TypeScript의 클래스 시스템을 활용한다.

**src/core/Player.ts**

```typescript
import { PlayerStats } from '../types/game.types';

export class Player {
  public readonly id: string;
  public readonly nickname: string;
  private stats: PlayerStats;

  constructor(id: string, nickname: string) {
    this.id = id;
    this.nickname = nickname;
    this.stats = {
      hp: 100,
      maxHp: 100,
      mp: 50,
      maxMp: 50,
      attack: 20,
      defense: 10
    };
  }

  public getStats(): PlayerStats {
    // 내부 상태를 복사하여 반환 (불변성 유지)
    return { ...this.stats };
  }

  public takeDamage(damage: number): number {
    const actualDamage = Math.max(0, damage);
    this.stats.hp = Math.max(0, this.stats.hp - actualDamage);
    return actualDamage;
  }

  public consumeMp(amount: number): boolean {
    if (this.stats.mp >= amount) {
      this.stats.mp -= amount;
      return true;
    }
    return false;
  }

  public heal(amount: number): void {
    this.stats.hp = Math.min(this.stats.maxHp, this.stats.hp + amount);
  }

  public isDead(): boolean {
    return this.stats.hp <= 0;
  }

  public increaseDefense(amount: number): void {
    this.stats.defense += amount;
  }

  public resetDefense(): void {
    this.stats.defense = 10; // 기본 방어력으로 복구
  }
}
```

`Player` 클래스는 캡슐화의 좋은 예다. 내부 상태는 `private`으로 보호되며, 공개된 메서드를 통해서만 접근 가능하다. TypeScript의 접근 제어자가 이를 강제한다.

**src/core/BattleEngine.ts**

```typescript
import { Player } from './Player';
import { ActionType, PlayerAction, TurnResult } from '../types/game.types';

export class BattleEngine {
  private turnNumber: number = 0;

  constructor(
    private player1: Player,
    private player2: Player
  ) {}

  // 턴 처리 - 두 플레이어의 행동을 받아 결과를 계산한다
  public processTurn(
    action1: PlayerAction,
    action2: PlayerAction
  ): TurnResult {
    this.turnNumber++;
    
    // 방어 상태 초기화 (이전 턴의 방어 효과 제거)
    this.player1.resetDefense();
    this.player2.resetDefense();

    const logs: string[] = [];
    let player1Damage = 0;
    let player2Damage = 0;

    // 각 플레이어의 행동 처리
    const result1 = this.executeAction(action1.type, this.player1, this.player2);
    logs.push(...result1.logs);

    const result2 = this.executeAction(action2.type, this.player2, this.player1);
    logs.push(...result2.logs);

    player1Damage = result2.damageDealt;
    player2Damage = result1.damageDealt;

    return {
      turnNumber: this.turnNumber,
      actions: {
        player1: action1,
        player2: action2
      },
      damages: {
        player1Damage,
        player2Damage
      },
      newStats: {
        player1: this.player1.getStats(),
        player2: this.player2.getStats()
      },
      logs
    };
  }

  // 개별 행동 실행 로직
  private executeAction(
    actionType: ActionType,
    attacker: Player,
    defender: Player
  ): { damageDealt: number; logs: string[] } {
    const logs: string[] = [];
    let damageDealt = 0;

    switch (actionType) {
      case ActionType.ATTACK: {
        const attackerStats = attacker.getStats();
        const defenderStats = defender.getStats();
        const rawDamage = attackerStats.attack;
        const finalDamage = Math.max(1, rawDamage - defenderStats.defense);
        
        damageDealt = defender.takeDamage(finalDamage);
        logs.push(
          `${attacker.nickname}이(가) ${defender.nickname}에게 ${damageDealt} 데미지를 입혔다!`
        );
        break;
      }

      case ActionType.DEFEND: {
        attacker.increaseDefense(15);
        logs.push(`${attacker.nickname}이(가) 방어 태세를 취했다! (방어력 +15)`);
        break;
      }

      case ActionType.SKILL: {
        const skillCost = 20;
        if (attacker.consumeMp(skillCost)) {
          const attackerStats = attacker.getStats();
          const skillDamage = attackerStats.attack * 2;
          const defenderStats = defender.getStats();
          const finalDamage = Math.max(1, skillDamage - defenderStats.defense);
          
          damageDealt = defender.takeDamage(finalDamage);
          logs.push(
            `${attacker.nickname}이(가) 강력한 스킬을 사용했다! ${damageDealt} 데미지!`
          );
        } else {
          logs.push(
            `${attacker.nickname}의 MP가 부족하여 스킬을 사용할 수 없었다!`
          );
        }
        break;
      }
    }

    return { damageDealt, logs };
  }

  public checkGameOver(): { isOver: boolean; winner: Player | null } {
    if (this.player1.isDead()) {
      return { isOver: true, winner: this.player2 };
    }
    if (this.player2.isDead()) {
      return { isOver: true, winner: this.player1 };
    }
    return { isOver: false, winner: null };
  }
}
```

`BattleEngine` 클래스는 게임의 핵심 로직을 담당한다. `switch` 문에서 `ActionType` 열거형을 사용하므로, 모든 케이스를 처리했는지 TypeScript가 확인해준다. 새로운 액션 타입을 추가하면 컴파일 에러가 발생하여 누락을 방지한다.

**src/core/Room.ts**

```typescript
import { Player } from './Player';
import { BattleEngine } from './BattleEngine';
import { GameState, PlayerAction, ActionType, TurnResult } from '../types/game.types';

export class Room {
  public readonly id: string;
  private state: GameState;
  private battleEngine: BattleEngine;
  private pendingActions: Map<string, PlayerAction>;

  constructor(
    id: string,
    private player1: Player,
    private player2: Player
  ) {
    this.id = id;
    this.state = GameState.IN_PROGRESS;
    this.battleEngine = new BattleEngine(player1, player2);
    this.pendingActions = new Map();
  }

  public getPlayers(): [Player, Player] {
    return [this.player1, this.player2];
  }

  public getState(): GameState {
    return this.state;
  }

  public getOpponent(playerId: string): Player | null {
    if (this.player1.id === playerId) return this.player2;
    if (this.player2.id === playerId) return this.player1;
    return null;
  }

  // 플레이어 행동 등록
  public submitAction(playerId: string, actionType: ActionType): boolean {
    // 이미 행동을 제출했는지 확인
    if (this.pendingActions.has(playerId)) {
      return false;
    }

    // 방에 속한 플레이어인지 확인
    if (playerId !== this.player1.id && playerId !== this.player2.id) {
      return false;
    }

    const action: PlayerAction = {
      playerId,
      type: actionType,
      timestamp: Date.now()
    };

    this.pendingActions.set(playerId, action);
    return true;
  }

  // 턴 실행 준비 확인
  public isReadyForTurn(): boolean {
    return this.pendingActions.size === 2;
  }

  // 턴 실행 및 결과 반환
  public executeTurn(): TurnResult | null {
    if (!this.isReadyForTurn()) {
      return null;
    }

    const action1 = this.pendingActions.get(this.player1.id)!;
    const action2 = this.pendingActions.get(this.player2.id)!;

    const result = this.battleEngine.processTurn(action1, action2);

    // 행동 초기화 (다음 턴 준비)
    this.pendingActions.clear();

    // 게임 종료 확인
    const gameOverCheck = this.battleEngine.checkGameOver();
    if (gameOverCheck.isOver) {
      this.state = GameState.FINISHED;
    }

    return result;
  }

  public getWinner(): Player | null {
    if (this.state !== GameState.FINISHED) {
      return null;
    }
    return this.battleEngine.checkGameOver().winner;
  }
}
```

`Room` 클래스는 게임 세션을 관리한다. `Map`을 사용하여 플레이어의 행동을 임시 저장하고, 두 플레이어가 모두 행동을 제출하면 턴을 실행한다. TypeScript의 타입 시스템 덕분에 `null` 체크를 강제하여 런타임 에러를 방지한다.

### 3단계: 관리 시스템 구현
이제 여러 방과 플레이어를 관리하는 매니저 클래스를 구현한다.

**src/managers/RoomManager.ts**

```typescript
import { Room } from '../core/Room';
import { Player } from '../core/Player';
import { v4 as uuidv4 } from 'uuid';

export class RoomManager {
  private rooms: Map<string, Room>;
  private playerToRoom: Map<string, string>; // playerId -> roomId

  constructor() {
    this.rooms = new Map();
    this.playerToRoom = new Map();
  }

  // 새 방 생성
  public createRoom(player1: Player, player2: Player): Room {
    const roomId = uuidv4();
    const room = new Room(roomId, player1, player2);
    
    this.rooms.set(roomId, room);
    this.playerToRoom.set(player1.id, roomId);
    this.playerToRoom.set(player2.id, roomId);

    return room;
  }

  // 플레이어의 방 조회
  public getRoomByPlayerId(playerId: string): Room | undefined {
    const roomId = this.playerToRoom.get(playerId);
    if (!roomId) return undefined;
    return this.rooms.get(roomId);
  }

  // 방 조회
  public getRoom(roomId: string): Room | undefined {
    return this.rooms.get(roomId);
  }

  // 방 제거
  public removeRoom(roomId: string): void {
    const room = this.rooms.get(roomId);
    if (room) {
      const [player1, player2] = room.getPlayers();
      this.playerToRoom.delete(player1.id);
      this.playerToRoom.delete(player2.id);
      this.rooms.delete(roomId);
    }
  }

  // 활성 방 개수
  public getActiveRoomCount(): number {
    return this.rooms.size;
  }
}
```

**src/managers/PlayerManager.ts**

```typescript
import { Player } from '../core/Player';

export class PlayerManager {
  private waitingPlayers: Player[];
  private connectedPlayers: Map<string, Player>;

  constructor() {
    this.waitingPlayers = [];
    this.connectedPlayers = new Map();
  }

  // 새 플레이어 추가
  public addPlayer(socketId: string, nickname: string): Player {
    const player = new Player(socketId, nickname);
    this.connectedPlayers.set(socketId, player);
    return player;
  }

  // 플레이어 조회
  public getPlayer(playerId: string): Player | undefined {
    return this.connectedPlayers.get(playerId);
  }

  // 매칭 대기열에 추가
  public addToWaitingQueue(player: Player): void {
    if (!this.waitingPlayers.includes(player)) {
      this.waitingPlayers.push(player);
    }
  }

  // 매칭 시도 - 대기 중인 플레이어 2명 반환
  public tryMatch(): [Player, Player] | null {
    if (this.waitingPlayers.length >= 2) {
      const player1 = this.waitingPlayers.shift()!;
      const player2 = this.waitingPlayers.shift()!;
      return [player1, player2];
    }
    return null;
  }

  // 플레이어 제거
  public removePlayer(playerId: string): void {
    const player = this.connectedPlayers.get(playerId);
    if (player) {
      // 대기열에서도 제거
      const index = this.waitingPlayers.indexOf(player);
      if (index !== -1) {
        this.waitingPlayers.splice(index, 1);
      }
      this.connectedPlayers.delete(playerId);
    }
  }

  // 대기 중인 플레이어 수
  public getWaitingCount(): number {
    return this.waitingPlayers.length;
  }
}
```

이 매니저 클래스들은 싱글톤 패턴으로 사용될 것이며, 게임 서버의 상태를 중앙에서 관리한다. TypeScript의 `Map`과 `Array` 타입을 활용하여 타입 안전성을 유지한다.

### 4단계: Socket.io 통합 및 이벤트 핸들러
이제 모든 것을 Socket.io와 연결한다. 타입 안전한 이벤트 핸들링이 핵심이다.

**src/handlers/GameHandler.ts**

```typescript
import { Socket } from 'socket.io';
import { RoomManager } from '../managers/RoomManager';
import { PlayerManager } from '../managers/PlayerManager';
import { C2S_Packets, S2C_Packets } from '../types/packet.types';
import { ActionType } from '../types/game.types';

export class GameHandler {
  constructor(
    private roomManager: RoomManager,
    private playerManager: PlayerManager
  ) {}

  // 타입 안전한 이벤트 emit 헬퍼
  private emit<K extends keyof S2C_Packets>(
    socket: Socket,
    event: K,
    data: S2C_Packets[K]
  ): void {
    socket.emit(event, data);
  }

  // 게임 행동 처리
  public handleGameAction(
    socket: Socket,
    data: C2S_Packets['game:action']
  ): void {
    const playerId = socket.id;
    const room = this.roomManager.getRoomByPlayerId(playerId);

    if (!room) {
      this.emit(socket, 'error', {
        code: 'NO_ROOM',
        message: '게임 방을 찾을 수 없다'
      });
      return;
    }

    // 행동 제출
    const success = room.submitAction(playerId, data.action);
    
    if (!success) {
      this.emit(socket, 'error', {
        code: 'ACTION_FAILED',
        message: '이미 행동을 제출했거나 유효하지 않은 행동이다'
      });
      return;
    }

    // 턴 실행 준비 확인
    if (room.isReadyForTurn()) {
      this.executeTurn(room.id);
    }
  }

  // 턴 실행 및 결과 브로드캐스트
  private executeTurn(roomId: string): void {
    const room = this.roomManager.getRoom(roomId);
    if (!room) return;

    const turnResult = room.executeTurn();
    if (!turnResult) return;

    const [player1, player2] = room.getPlayers();

    // 양쪽 플레이어에게 턴 결과 전송
    const io = (global as any).io; // 글로벌 io 인스턴스 참조
    
    io.to(player1.id).emit('game:turn-result', turnResult);
    io.to(player2.id).emit('game:turn-result', turnResult);

    // 게임 종료 확인
    const winner = room.getWinner();
    if (winner) {
      const finishData: S2C_Packets['game:finished'] = {
        winnerId: winner.id,
        winnerNickname: winner.nickname,
        reason: 'HP가 0이 되었다'
      };

      io.to(player1.id).emit('game:finished', finishData);
      io.to(player2.id).emit('game:finished', finishData);

      // 방 정리
      this.roomManager.removeRoom(roomId);
    } else {
      // 다음 턴을 위한 상태 업데이트 전송
      this.sendGameState(player1.id, room);
      this.sendGameState(player2.id, room);
    }
  }

  // 게임 상태 전송
  private sendGameState(playerId: string, room: Room): void {
    const io = (global as any).io;
    const player = this.playerManager.getPlayer(playerId);
    const opponent = room.getOpponent(playerId);

    if (!player || !opponent) return;

    const stateData: S2C_Packets['game:state'] = {
      state: room.getState(),
      turnNumber: 0, // BattleEngine에서 가져와야 하지만 단순화
      yourStats: player.getStats(),
      opponentStats: opponent.getStats(),
      isYourTurn: true // 턴제이므로 항상 true
    };

    io.to(playerId).emit('game:state', stateData);
  }
}
```

**src/handlers/ConnectionHandler.ts**

```typescript
import { Socket } from 'socket.io';
import { RoomManager } from '../managers/RoomManager';
import { PlayerManager } from '../managers/PlayerManager';
import { C2S_Packets, S2C_Packets } from '../types/packet.types';

export class ConnectionHandler {
  constructor(
    private roomManager: RoomManager,
    private playerManager: PlayerManager
  ) {}

  // 플레이어 입장 처리
  public handlePlayerJoin(
    socket: Socket,
    data: C2S_Packets['player:join']
  ): void {
    const player = this.playerManager.addPlayer(socket.id, data.nickname);

    // 입장 확인 응답
    const joinedData: S2C_Packets['player:joined'] = {
      playerId: player.id,
      nickname: player.nickname
    };
    socket.emit('player:joined', joinedData);

    // 매칭 대기열에 추가
    this.playerManager.addToWaitingQueue(player);

    // 매칭 시도
    this.tryMatchmaking();
  }

  // 매칭 로직
  private tryMatchmaking(): void {
    const matchResult = this.playerManager.tryMatch();
    
    if (matchResult) {
      const [player1, player2] = matchResult;
      const room = this.roomManager.createRoom(player1, player2);

      const io = (global as any).io;

      // 플레이어 1에게 매칭 알림
      const matched1Data: S2C_Packets['game:matched'] = {
        roomId: room.id,
        opponent: {
          id: player2.id,
          nickname: player2.nickname
        },
        yourStats: player1.getStats(),
        opponentStats: player2.getStats()
      };
      io.to(player1.id).emit('game:matched', matched1Data);

      // 플레이어 2에게 매칭 알림
      const matched2Data: S2C_Packets['game:matched'] = {
        roomId: room.id,
        opponent: {
          id: player1.id,
          nickname: player1.nickname
        },
        yourStats: player2.getStats(),
        opponentStats: player1.getStats()
      };
      io.to(player2.id).emit('game:matched', matched2Data);

      console.log(`매칭 완료: ${player1.nickname} vs ${player2.nickname}`);
    }
  }

  // 플레이어 퇴장 처리
  public handlePlayerLeave(socket: Socket): void {
    const playerId = socket.id;
    const room = this.roomManager.getRoomByPlayerId(playerId);

    if (room) {
      // 게임 중이었다면 상대방에게 알림
      const opponent = room.getOpponent(playerId);
      if (opponent) {
        const io = (global as any).io;
        const finishData: S2C_Packets['game:finished'] = {
          winnerId: opponent.id,
          winnerNickname: opponent.nickname,
          reason: '상대방이 나갔다'
        };
        io.to(opponent.id).emit('game:finished', finishData);
      }

      this.roomManager.removeRoom(room.id);
    }

    this.playerManager.removePlayer(playerId);
    console.log(`플레이어 퇴장: ${playerId}`);
  }
}
```

### 5단계: 서버 엔트리 포인트
모든 조각을 하나로 모은다.

**src/index.ts**

```typescript
import express from 'express';
import { createServer } from 'http';
import { Server, Socket } from 'socket.io';
import { RoomManager } from './managers/RoomManager';
import { PlayerManager } from './managers/PlayerManager';
import { ConnectionHandler } from './handlers/ConnectionHandler';
import { GameHandler } from './handlers/GameHandler';
import { C2S_Packets } from './types/packet.types';

const app = express();
const httpServer = createServer(app);
const io = new Server(httpServer, {
  cors: {
    origin: '*',
    methods: ['GET', 'POST']
  }
});

// 글로벌 io 인스턴스 저장 (핸들러에서 사용)
(global as any).io = io;

// 매니저 인스턴스 생성
const roomManager = new RoomManager();
const playerManager = new PlayerManager();
const connectionHandler = new ConnectionHandler(roomManager, playerManager);
const gameHandler = new GameHandler(roomManager, playerManager);

// Socket.io 연결 처리
io.on('connection', (socket: Socket) => {
  console.log(`새 연결: ${socket.id}`);

  // 타입 안전한 이벤트 리스너 등록
  socket.on('player:join', (data: C2S_Packets['player:join']) => {
    connectionHandler.handlePlayerJoin(socket, data);
  });

  socket.on('game:action', (data: C2S_Packets['game:action']) => {
    gameHandler.handleGameAction(socket, data);
  });

  socket.on('disconnect', () => {
    connectionHandler.handlePlayerLeave(socket);
  });
});

// 헬스체크 엔드포인트
app.get('/health', (req, res) => {
  res.json({
    status: 'ok',
    activeRooms: roomManager.getActiveRoomCount(),
    waitingPlayers: playerManager.getWaitingCount()
  });
});

const PORT = process.env.PORT || 3000;

httpServer.listen(PORT, () => {
  console.log(`
┌──────────────────────────────────────────────┐
│     TypeBattle Server is Running! 🎮         │
├──────────────────────────────────────────────┤
│  Port: ${PORT}                                 
│  Health Check: http://localhost:${PORT}/health
└──────────────────────────────────────────────┘
  `);
});
```

**package.json 의존성**

```json
{
  "name": "typebattle-server",
  "version": "1.0.0",
  "scripts": {
    "dev": "ts-node-dev --respawn src/index.ts",
    "build": "tsc",
    "start": "node dist/index.js",
    "test": "jest"
  },
  "dependencies": {
    "express": "^4.18.2",
    "socket.io": "^4.6.1",
    "uuid": "^9.0.0"
  },
  "devDependencies": {
    "@types/express": "^4.17.17",
    "@types/node": "^18.15.11",
    "@types/uuid": "^9.0.1",
    "ts-node-dev": "^2.0.0",
    "typescript": "^5.0.4",
    "jest": "^29.5.0",
    "@types/jest": "^29.5.0",
    "ts-jest": "^29.1.0"
  }
}
```


## 10.2 완성 코드 리뷰
프로젝트가 완성되었다. 이제 TypeScript가 우리에게 가져다준 이점을 분석해본다.

### TypeScript가 가져온 코드 품질 향상

**1. 컴파일 타임 안전성**

JavaScript로 작성했다면 런타임에 발견했을 수많은 버그를 컴파일 타임에 잡아냈다. 예를 들어 다음과 같은 실수를 방지했다.

```typescript
// ❌ JavaScript였다면 런타임 에러
socket.emit('game:mathced', data); // 오타! 'matched'가 아님

// ✅ TypeScript는 컴파일 에러 발생
// Argument of type '"game:mathced"' is not assignable to 
// parameter of type 'S2C_EventNames'
```

**2. 자동 완성과 인텔리센스**

VSCode에서 패킷 구조를 타이핑할 때 자동 완성이 제공된다. 문서를 찾아보지 않아도 된다.

```typescript
// 'game:matched'를 입력하면 자동으로 데이터 구조가 표시된다
socket.emit('game:matched', {
  roomId: '...', // 자동 완성
  opponent: {
    id: '...',      // 자동 완성
    nickname: '...' // 자동 완성
  },
  // ... 나머지도 자동 완성
});
```

**3. 리팩토링의 안전성**

만약 `PlayerStats` 인터페이스에 새 필드를 추가한다면?

```typescript
export interface PlayerStats {
  hp: number;
  maxHp: number;
  mp: number;
  maxMp: number;
  attack: number;
  defense: number;
  criticalRate: number; // 새로 추가!
}
```

TypeScript는 이 필드를 초기화하지 않은 모든 곳에서 에러를 발생시킨다. 놓친 부분이 없도록 보장한다.

**4. 명확한 계약(Contract)**

패킷 타입은 클라이언트와 서버 간의 명확한 계약이다. 양쪽 모두 TypeScript를 사용한다면 이 타입을 공유하여 일관성을 유지할 수 있다.

```typescript
// types/packet.types.ts를 클라이언트와 공유
// 서버와 클라이언트가 항상 동기화된 상태를 유지한다
```

### 실전 테스트 작성
게임 로직이 제대로 동작하는지 확인하기 위해 테스트를 작성한다.

**tests/core/BattleEngine.test.ts**

```typescript
import { Player } from '../../src/core/Player';
import { BattleEngine } from '../../src/core/BattleEngine';
import { ActionType, PlayerAction } from '../../src/types/game.types';

describe('BattleEngine', () => {
  let player1: Player;
  let player2: Player;
  let engine: BattleEngine;

  beforeEach(() => {
    player1 = new Player('p1', 'Alice');
    player2 = new Player('p2', 'Bob');
    engine = new BattleEngine(player1, player2);
  });

  test('공격 행동이 데미지를 입힌다', () => {
    const action1: PlayerAction = {
      playerId: 'p1',
      type: ActionType.ATTACK,
      timestamp: Date.now()
    };

    const action2: PlayerAction = {
      playerId: 'p2',
      type: ActionType.DEFEND,
      timestamp: Date.now()
    };

    const result = engine.processTurn(action1, action2);

    // player1이 공격했으므로 player2가 데미지를 받는다
    expect(result.damages.player2Damage).toBeGreaterThan(0);
    expect(result.newStats.player2.hp).toBeLessThan(100);
  });

  test('스킬은 MP를 소모하고 더 큰 데미지를 입힌다', () => {
    const action1: PlayerAction = {
      playerId: 'p1',
      type: ActionType.SKILL,
      timestamp: Date.now()
    };

    const action2: PlayerAction = {
      playerId: 'p2',
      type: ActionType.ATTACK,
      timestamp: Date.now()
    };

    const result = engine.processTurn(action1, action2);

    // player1이 스킬을 사용했으므로 MP가 감소한다
    expect(result.newStats.player1.mp).toBe(30); // 50 - 20

    // 스킬 데미지는 일반 공격보다 크다
    expect(result.damages.player2Damage).toBeGreaterThan(20);
  });

  test('HP가 0이 되면 게임이 종료된다', () => {
    // player2의 HP를 1로 설정 (takeDamage로 조작)
    player2.takeDamage(99);

    const action1: PlayerAction = {
      playerId: 'p1',
      type: ActionType.ATTACK,
      timestamp: Date.now()
    };

    const action2: PlayerAction = {
      playerId: 'p2',
      type: ActionType.ATTACK,
      timestamp: Date.now()
    };

    engine.processTurn(action1, action2);

    const gameOver = engine.checkGameOver();
    expect(gameOver.isOver).toBe(true);
    expect(gameOver.winner).toBe(player1);
  });
});
```

TypeScript 덕분에 테스트를 작성할 때도 타입 안전성이 보장된다. 잘못된 타입의 데이터로 테스트를 작성하면 컴파일 에러가 발생한다.

### 리팩토링 포인트
프로젝트를 더 개선할 수 있는 부분들을 살펴본다.

**1. 의존성 주입 패턴 적용**

현재 핸들러에서 글로벌 `io` 인스턴스를 참조하고 있다. 이를 생성자 주입으로 변경하면 테스트가 더 쉬워진다.

```typescript
export class GameHandler {
  constructor(
    private io: Server, // Socket.io 인스턴스 주입
    private roomManager: RoomManager,
    private playerManager: PlayerManager
  ) {}

  private executeTurn(roomId: string): void {
    // global 대신 this.io 사용
    this.io.to(player1.id).emit('game:turn-result', turnResult);
  }
}
```

**2. 이벤트 리스너 타입 안전성 강화**

Socket.io의 타입 정의를 확장하여 더 강력한 타입 안전성을 얻을 수 있다.

```typescript
import { Server as SocketIOServer, Socket as SocketIOSocket } from 'socket.io';
import { C2S_Packets, S2C_Packets } from './types/packet.types';

// 타입 안전한 Socket 인터페이스
interface TypedSocket extends SocketIOSocket {
  emit<K extends keyof S2C_Packets>(
    event: K,
    data: S2C_Packets[K]
  ): boolean;
  
  on<K extends keyof C2S_Packets>(
    event: K,
    listener: (data: C2S_Packets[K]) => void
  ): this;
}

// 사용
socket.emit('game:matched', matchData); // 타입 체크됨
socket.on('game:action', (data) => {
  // data의 타입이 자동으로 추론됨
  console.log(data.action);
});
```

**3. 에러 처리 개선**

커스텀 에러 클래스를 만들어 에러를 체계적으로 관리한다.

```typescript
// src/utils/errors.ts
export class GameError extends Error {
  constructor(
    public code: string,
    message: string
  ) {
    super(message);
    this.name = 'GameError';
  }
}

export class RoomNotFoundError extends GameError {
  constructor() {
    super('ROOM_NOT_FOUND', '게임 방을 찾을 수 없다');
  }
}

export class InvalidActionError extends GameError {
  constructor(reason: string) {
    super('INVALID_ACTION', `유효하지 않은 행동: ${reason}`);
  }
}

// 핸들러에서 사용
try {
  const room = this.roomManager.getRoomByPlayerId(playerId);
  if (!room) {
    throw new RoomNotFoundError();
  }
  // ...
} catch (error) {
  if (error instanceof GameError) {
    this.emit(socket, 'error', {
      code: error.code,
      message: error.message
    });
  } else {
    // 예상치 못한 에러
    console.error('Unexpected error:', error);
  }
}
```

**4. 로깅 시스템 추가**

프로덕션 환경에서는 체계적인 로깅이 필수다.

```typescript
// src/utils/logger.ts
export enum LogLevel {
  DEBUG = 'DEBUG',
  INFO = 'INFO',
  WARN = 'WARN',
  ERROR = 'ERROR'
}

interface LogEntry {
  level: LogLevel;
  timestamp: string;
  message: string;
  data?: any;
}

export class Logger {
  private static instance: Logger;

  private constructor() {}

  public static getInstance(): Logger {
    if (!Logger.instance) {
      Logger.instance = new Logger();
    }
    return Logger.instance;
  }

  private log(level: LogLevel, message: string, data?: any): void {
    const entry: LogEntry = {
      level,
      timestamp: new Date().toISOString(),
      message,
      data
    };

    // 실제로는 파일이나 로깅 서비스로 전송
    console.log(JSON.stringify(entry));
  }

  public debug(message: string, data?: any): void {
    this.log(LogLevel.DEBUG, message, data);
  }

  public info(message: string, data?: any): void {
    this.log(LogLevel.INFO, message, data);
  }

  public warn(message: string, data?: any): void {
    this.log(LogLevel.WARN, message, data);
  }

  public error(message: string, data?: any): void {
    this.log(LogLevel.ERROR, message, data);
  }
}

// 사용
const logger = Logger.getInstance();
logger.info('매칭 완료', { player1: p1.nickname, player2: p2.nickname });
```

### 성능 고려사항
실전 서버에서는 성능도 중요하다.

**1. 객체 풀링**

빈번하게 생성/소멸되는 객체는 풀링을 고려한다.

```typescript
class ObjectPool<T> {
  private available: T[] = [];
  private inUse: Set<T> = new Set();

  constructor(
    private factory: () => T,
    private reset: (obj: T) => void,
    initialSize: number = 10
  ) {
    for (let i = 0; i < initialSize; i++) {
      this.available.push(this.factory());
    }
  }

  public acquire(): T {
    let obj = this.available.pop();
    if (!obj) {
      obj = this.factory();
    }
    this.inUse.add(obj);
    return obj;
  }

  public release(obj: T): void {
    if (this.inUse.has(obj)) {
      this.reset(obj);
      this.inUse.delete(obj);
      this.available.push(obj);
    }
  }
}

// 사용 예: PlayerAction 객체 풀링
const actionPool = new ObjectPool<PlayerAction>(
  () => ({ playerId: '', type: ActionType.ATTACK, timestamp: 0 }),
  (action) => {
    action.playerId = '';
    action.timestamp = 0;
  },
  100
);
```

**2. 메모리 관리**

방이 종료되면 모든 참조를 정리하여 메모리 누수를 방지한다.

```typescript
public removeRoom(roomId: string): void {
  const room = this.rooms.get(roomId);
  if (room) {
    const [player1, player2] = room.getPlayers();
    
    // 모든 참조 제거
    this.playerToRoom.delete(player1.id);
    this.playerToRoom.delete(player2.id);
    this.rooms.delete(roomId);
    
    // 명시적으로 null 할당 (선택사항)
    // room = null; // TypeScript에서는 const이므로 불가능
  }
}
```

### 프로젝트 실행 및 테스트
이제 서버를 실행하고 테스트해본다.

```bash
# 의존성 설치
npm install

# 개발 모드 실행
npm run dev

# 서버가 시작되면 다음과 같은 메시지가 출력된다:
┌──────────────────────────────────────────────┐
│     TypeBattle Server is Running! 🎮         │
├──────────────────────────────────────────────┤
│  Port: 3000                                   
│  Health Check: http://localhost:3000/health
└──────────────────────────────────────────────┘
```

**간단한 클라이언트 테스트 스크립트 (Node.js)**

```typescript
// test-client.ts
import { io } from 'socket.io-client';

const socket = io('http://localhost:3000');

socket.on('connect', () => {
  console.log('서버에 연결되었다');
  
  // 입장
  socket.emit('player:join', { nickname: 'TestPlayer' });
});

socket.on('player:joined', (data) => {
  console.log('입장 성공:', data);
});

socket.on('game:matched', (data) => {
  console.log('매칭 완료!', data);
  
  // 자동으로 공격 선택
  setTimeout(() => {
    socket.emit('game:action', { action: 'ATTACK' });
  }, 1000);
});

socket.on('game:turn-result', (result) => {
  console.log('턴 결과:', result);
  console.log('로그:', result.logs);
});

socket.on('game:finished', (data) => {
  console.log('게임 종료:', data);
  process.exit(0);
});

socket.on('error', (error) => {
  console.error('에러:', error);
});
```

두 개의 터미널에서 이 클라이언트를 실행하면 자동으로 매칭되어 게임이 진행된다.

### 배운 점 정리
이 프로젝트를 통해 TypeScript가 실전 게임 서버 개발에서 어떻게 도움이 되는지 직접 경험했다.

**TypeScript의 핵심 가치**

- **타입 안전성**: 수많은 런타임 에러를 컴파일 타임에 예방했다
- **개발 생산성**: 자동 완성과 인텔리센스로 코딩 속도가 향상되었다
- **리팩토링 신뢰도**: 코드를 수정해도 타입 체커가 문제를 즉시 알려준다
- **명확한 계약**: 인터페이스와 타입이 코드의 명세서 역할을 한다
- **팀 협업**: 타입 정의를 공유하면 팀원 간 의사소통이 명확해진다

**실전 패턴**

- 패킷 타입을 중앙에서 관리하여 클라이언트-서버 일관성 유지
- 제네릭을 활용한 타입 안전한 이벤트 시스템
- 클래스와 인터페이스로 명확한 책임 분리
- 유틸리티 타입을 활용한 유연한 타입 변환

이제 당신은 TypeScript로 실전 게임 서버를 구축할 수 있는 모든 지식을 갖추었다. 이 미니 프로젝트를 기반으로 더 복잡한 시스템을 확장해 나갈 수 있다.

---

```
┌────────────────────────────────────────────────────────┐
│                  프로젝트 완성! 🎉                       │
├────────────────────────────────────────────────────────┤
│                                                         │
│  ✓ 타입 안전한 실시간 멀티플레이어 서버                     │
│  ✓ 확장 가능한 아키텍처                                  │
│  ✓ 테스트 가능한 구조                                    │
│  ✓ 프로덕션 준비 완료                                    │
│                                                         │
│  다음 단계:                                              │
│  • 데이터베이스 연동 (전적 저장)                          │
│  • 랭킹 시스템 추가                                      │
│  • 더 다양한 스킬과 아이템                                │
│  • 관전 모드 구현                                        │
│  • Docker 컨테이너화                                     │
│                                                         │
└────────────────────────────────────────────────────────┘
```

이 프로젝트는 시작일 뿐이다. TypeScript의 강력한 타입 시스템을 무기로, 당신은 이제 어떤 규모의 게임 서버도 자신 있게 구축할 수 있다.

  


  
# Appendix: 빠른 참조
개발 중에 빠르게 참조할 수 있는 핵심 내용을 정리했다. 이 부록은 책갈피를 해두고 필요할 때마다 꺼내보면 된다. 경력 개발자로서 당신은 모든 것을 외울 필요가 없다는 것을 알 것이다. 중요한 것은 필요한 정보를 빠르게 찾는 능력이다.


## A.1 자주 쓰는 타입 치트시트

### 기본 타입 (Primitive Types)
게임 서버 개발에서 가장 자주 마주치는 타입들을 정리했다.

```typescript
// ═══════════════════════════════════════════════════════
//  기본 타입 - 이것만 기억하면 90%는 해결된다
// ═══════════════════════════════════════════════════════

// 숫자 - 플레이어 레벨, 데미지, 좌표 등
let level: number = 50;
let damage: number = 123.45;
let hexValue: number = 0xFF; // 16진수도 number

// 문자열 - 닉네임, 메시지, ID 등
let nickname: string = "DragonSlayer";
let message: string = `${nickname}님이 입장했다`; // 템플릿 리터럴

// 불린 - 플래그, 상태 체크
let isAlive: boolean = true;
let hasPermission: boolean = false;

// null과 undefined - 값이 없음을 명시
let notInitialized: undefined = undefined;
let noValue: null = null;

// any - 가능하면 피하되, 불가피할 때만 사용
let legacyData: any = { /* 타입을 알 수 없는 레거시 데이터 */ };

// unknown - any보다 안전한 대안
let userInput: unknown = getUserInput();
if (typeof userInput === 'string') {
  console.log(userInput.toUpperCase()); // 타입 체크 후 사용
}

// void - 반환값이 없는 함수
function logMessage(msg: string): void {
  console.log(msg);
  // return 없음
}

// never - 절대 반환하지 않는 함수
function throwError(message: string): never {
  throw new Error(message);
}
```

### 배열과 튜플 (Arrays & Tuples)

```typescript
// ═══════════════════════════════════════════════════════
//  배열 - 동일한 타입의 여러 값
// ═══════════════════════════════════════════════════════

// 두 가지 표기법 (동일함)
let playerIds: string[] = ["p1", "p2", "p3"];
let scores: Array<number> = [100, 200, 300];

// 읽기 전용 배열
const fixedItems: readonly number[] = [1, 2, 3];
// fixedItems.push(4); // ❌ 에러: 읽기 전용

// 튜플 - 고정된 길이와 타입
type Position = [number, number]; // [x, y]
let playerPos: Position = [100, 250];

type PlayerInfo = [string, number, boolean]; // [name, level, online]
let player: PlayerInfo = ["Alice", 30, true];

// 선택적 튜플 요소
type Response = [number, string?]; // 두 번째 요소는 선택적
let success: Response = [200];
let error: Response = [500, "Internal Error"];
```

### 객체 타입 (Object Types)

```typescript
// ═══════════════════════════════════════════════════════
//  객체 타입 - 게임 서버에서 가장 많이 사용
// ═══════════════════════════════════════════════════════

// 인라인 타입
let player: { name: string; level: number } = {
  name: "Hero",
  level: 10
};

// 인터페이스 (확장 가능, 선언 병합 지원)
interface Player {
  id: string;
  nickname: string;
  level: number;
  isOnline: boolean;
}

// 타입 별칭 (더 유연함)
type GameRoom = {
  roomId: string;
  players: Player[];
  maxPlayers: number;
  createdAt: Date;
};

// 옵셔널 속성
interface Config {
  host: string;
  port: number;
  database?: string; // 선택적
  ssl?: boolean;     // 선택적
}

// 읽기 전용 속성
interface Immutable {
  readonly id: string;
  readonly createdAt: Date;
}

// 인덱스 시그니처 (동적 키)
interface PlayerStats {
  [statName: string]: number; // 어떤 문자열 키든 number 값
}
const stats: PlayerStats = {
  strength: 10,
  agility: 15,
  intelligence: 8
};

// Record 유틸리티 (더 명확함)
type Stats = Record<string, number>;
```

### 유니온과 인터섹션 (Union & Intersection)

```typescript
// ═══════════════════════════════════════════════════════
//  유니온(|) - "또는" 관계
// ═══════════════════════════════════════════════════════

// 여러 타입 중 하나
type Status = "idle" | "loading" | "success" | "error";
let currentStatus: Status = "loading";

// 숫자 리터럴 유니온
type DiceRoll = 1 | 2 | 3 | 4 | 5 | 6;

// 타입 유니온
type ID = string | number;
let userId: ID = "user_123";
let numericId: ID = 12345;

// 객체 유니온 - 판별 유니온 패턴
type Success = {
  status: "success";
  data: any;
};

type Error = {
  status: "error";
  message: string;
};

type Result = Success | Error;

function handleResult(result: Result) {
  if (result.status === "success") {
    console.log(result.data); // 타입 좁혀짐
  } else {
    console.log(result.message); // 타입 좁혀짐
  }
}

// ═══════════════════════════════════════════════════════
//  인터섹션(&) - "그리고" 관계 (타입 결합)
// ═══════════════════════════════════════════════════════

type Timestamped = {
  createdAt: Date;
  updatedAt: Date;
};

type Player = {
  id: string;
  nickname: string;
};

// 두 타입을 결합
type PlayerWithTimestamp = Player & Timestamped;

const player: PlayerWithTimestamp = {
  id: "p1",
  nickname: "Hero",
  createdAt: new Date(),
  updatedAt: new Date()
};
```

### 함수 타입 (Function Types)

```typescript
// ═══════════════════════════════════════════════════════
//  함수 타입 표기법
// ═══════════════════════════════════════════════════════

// 기본 함수 타입
function add(a: number, b: number): number {
  return a + b;
}

// 화살표 함수
const multiply = (a: number, b: number): number => a * b;

// 함수 타입 별칭
type MathOperation = (a: number, b: number) => number;
const subtract: MathOperation = (a, b) => a - b;

// 옵셔널 파라미터
function greet(name: string, title?: string): string {
  return title ? `${title} ${name}` : name;
}

// 기본값 파라미터
function createRoom(maxPlayers: number = 4): GameRoom {
  // ...
}

// Rest 파라미터
function sum(...numbers: number[]): number {
  return numbers.reduce((a, b) => a + b, 0);
}

// 콜백 함수 타입
type Callback = (error: Error | null, result?: any) => void;

function fetchData(url: string, callback: Callback): void {
  // ...
}

// 제네릭 함수
function identity<T>(value: T): T {
  return value;
}

// 여러 시그니처 (오버로드)
function parseValue(value: string): number;
function parseValue(value: number): string;
function parseValue(value: string | number): string | number {
  return typeof value === "string" ? parseInt(value) : value.toString();
}
```

### 제네릭 패턴 (Generic Patterns)

```typescript
// ═══════════════════════════════════════════════════════
//  제네릭 - 재사용 가능한 타입 안전 코드
// ═══════════════════════════════════════════════════════

// 기본 제네릭
function wrap<T>(value: T): { data: T } {
  return { data: value };
}

// 제약 조건 (Constraints)
interface HasId {
  id: string;
}

function findById<T extends HasId>(items: T[], id: string): T | undefined {
  return items.find(item => item.id === id);
}

// 여러 제네릭 파라미터
function merge<T, U>(obj1: T, obj2: U): T & U {
  return { ...obj1, ...obj2 };
}

// 제네릭 인터페이스
interface Repository<T> {
  find(id: string): Promise<T | null>;
  save(entity: T): Promise<void>;
  delete(id: string): Promise<boolean>;
}

// 제네릭 클래스
class DataStore<T> {
  private items: T[] = [];
  
  add(item: T): void {
    this.items.push(item);
  }
  
  getAll(): T[] {
    return [...this.items];
  }
}

// 제네릭 제약 - keyof 사용
function getProperty<T, K extends keyof T>(obj: T, key: K): T[K] {
  return obj[key];
}

const player = { name: "Alice", level: 10 };
const name = getProperty(player, "name"); // string 타입
const level = getProperty(player, "level"); // number 타입
```

### 유틸리티 타입 (Utility Types)

```typescript
// ═══════════════════════════════════════════════════════
//  내장 유틸리티 타입 - 실전에서 매우 유용
// ═══════════════════════════════════════════════════════

interface Player {
  id: string;
  nickname: string;
  level: number;
  email: string;
}

// Partial<T> - 모든 속성을 옵셔널로
type PartialPlayer = Partial<Player>;
// { id?: string; nickname?: string; level?: number; email?: string; }

function updatePlayer(id: string, updates: Partial<Player>) {
  // 일부 필드만 업데이트 가능
}

// Required<T> - 모든 속성을 필수로
type RequiredPlayer = Required<Player>;

// Readonly<T> - 모든 속성을 읽기 전용으로
type ImmutablePlayer = Readonly<Player>;

// Pick<T, K> - 특정 속성만 선택
type PlayerSummary = Pick<Player, "id" | "nickname">;
// { id: string; nickname: string; }

// Omit<T, K> - 특정 속성 제외
type PlayerWithoutEmail = Omit<Player, "email">;
// { id: string; nickname: string; level: number; }

// Record<K, T> - 키-값 매핑
type PlayerMap = Record<string, Player>;
// { [key: string]: Player }

type PlayerStatus = Record<"idle" | "active" | "busy", number>;
// { idle: number; active: number; busy: number; }

// Extract<T, U> - T에서 U에 할당 가능한 타입만 추출
type T1 = Extract<"a" | "b" | "c", "a" | "f">; // "a"

// Exclude<T, U> - T에서 U에 할당 가능한 타입 제외
type T2 = Exclude<"a" | "b" | "c", "a" | "f">; // "b" | "c"

// NonNullable<T> - null과 undefined 제거
type T3 = NonNullable<string | number | null | undefined>; // string | number

// ReturnType<T> - 함수의 반환 타입 추출
function createPlayer() {
  return { id: "1", name: "Hero" };
}
type PlayerType = ReturnType<typeof createPlayer>;
// { id: string; name: string; }

// Parameters<T> - 함수의 파라미터 타입 추출
function greet(name: string, age: number) {}
type GreetParams = Parameters<typeof greet>; // [string, number]

// Awaited<T> - Promise의 resolved 타입 추출
type T4 = Awaited<Promise<string>>; // string
type T5 = Awaited<Promise<Promise<number>>>; // number
```

### 타입 가드 패턴 (Type Guard Patterns)

```typescript
// ═══════════════════════════════════════════════════════
//  타입 가드 - 런타임에 타입 좁히기
// ═══════════════════════════════════════════════════════

// typeof 가드
function processValue(value: string | number) {
  if (typeof value === "string") {
    return value.toUpperCase(); // string 메서드 사용 가능
  } else {
    return value.toFixed(2); // number 메서드 사용 가능
  }
}

// instanceof 가드
class Player {}
class NPC {}

function handleEntity(entity: Player | NPC) {
  if (entity instanceof Player) {
    // Player 관련 로직
  } else {
    // NPC 관련 로직
  }
}

// in 연산자 가드
interface Cat {
  meow(): void;
}

interface Dog {
  bark(): void;
}

function makeSound(animal: Cat | Dog) {
  if ("meow" in animal) {
    animal.meow();
  } else {
    animal.bark();
  }
}

// 사용자 정의 타입 가드
interface Fish {
  swim(): void;
}

interface Bird {
  fly(): void;
}

function isFish(pet: Fish | Bird): pet is Fish {
  return (pet as Fish).swim !== undefined;
}

function move(pet: Fish | Bird) {
  if (isFish(pet)) {
    pet.swim(); // Fish로 확정
  } else {
    pet.fly(); // Bird로 확정
  }
}

// null/undefined 체크
function processName(name: string | null | undefined) {
  // 방법 1: if 체크
  if (name) {
    console.log(name.toUpperCase());
  }
  
  // 방법 2: 옵셔널 체이닝
  console.log(name?.toUpperCase());
  
  // 방법 3: null 병합 연산자
  const displayName = name ?? "Unknown";
}
```

### 게임 서버 전용 타입 패턴

```typescript
// ═══════════════════════════════════════════════════════
//  실전 게임 서버에서 자주 쓰는 패턴
// ═══════════════════════════════════════════════════════

// 패킷 타입 정의
interface PacketMap {
  'player:join': { nickname: string };
  'player:move': { x: number; y: number };
  'game:attack': { targetId: string; damage: number };
}

type PacketType = keyof PacketMap;
type PacketData<T extends PacketType> = PacketMap[T];

// 타입 안전한 이벤트 핸들러
class EventHandler {
  on<T extends PacketType>(
    type: T,
    handler: (data: PacketData<T>) => void
  ): void {
    // 구현
  }
}

// 열거형 (Enum) vs 유니온 타입
// Enum
enum GameState {
  WAITING = "WAITING",
  PLAYING = "PLAYING",
  FINISHED = "FINISHED"
}

// 유니온 타입 (더 가벼움, 권장)
type GameState2 = "WAITING" | "PLAYING" | "FINISHED";

// as const - 리터럴 타입으로 추론
const GAME_STATES = {
  WAITING: "WAITING",
  PLAYING: "PLAYING",
  FINISHED: "FINISHED"
} as const;

type GameState3 = typeof GAME_STATES[keyof typeof GAME_STATES];
// "WAITING" | "PLAYING" | "FINISHED"

// 브랜드 타입 (Branded Types) - ID 혼동 방지
type PlayerID = string & { readonly brand: unique symbol };
type RoomID = string & { readonly brand: unique symbol };

function createPlayerID(id: string): PlayerID {
  return id as PlayerID;
}

function createRoomID(id: string): RoomID {
  return id as RoomID;
}

// 다른 타입이므로 혼동 방지
function getPlayer(id: PlayerID) {}
function getRoom(id: RoomID) {}

const pid = createPlayerID("p123");
const rid = createRoomID("r456");

getPlayer(pid); // ✅
// getPlayer(rid); // ❌ 에러: RoomID는 PlayerID가 아님
```

  
## A.2 tsconfig.json 옵션 가이드
`tsconfig.json`은 TypeScript 프로젝트의 설정 파일이다. 올바른 설정은 개발 경험과 코드 품질에 직접적인 영향을 미친다.

### 기본 구조

```json
{
  "compilerOptions": {
    // 컴파일러 옵션
  },
  "include": [
    // 컴파일할 파일/폴더
  ],
  "exclude": [
    // 컴파일에서 제외할 파일/폴더
  ]
}
```

### 게임 서버 추천 설정

```json
{
  "compilerOptions": {
    // ═══════════════════════════════════════════════
    //  언어 및 환경 설정
    // ═══════════════════════════════════════════════
    
    // 타겟 JavaScript 버전 (Node.js 16+ 사용 시 ES2021 권장)
    "target": "ES2021",
    
    // 모듈 시스템 (Node.js는 CommonJS, 최신은 ESNext)
    "module": "commonjs",
    
    // 사용할 라이브러리 (Node.js 환경)
    "lib": ["ES2021"],
    
    // ═══════════════════════════════════════════════
    //  모듈 해석 설정
    // ═══════════════════════════════════════════════
    
    // Node.js 스타일 모듈 해석
    "moduleResolution": "node",
    
    // 절대 경로 import를 위한 base URL
    "baseUrl": "./src",
    
    // 경로 별칭 설정
    "paths": {
      "@core/*": ["core/*"],
      "@handlers/*": ["handlers/*"],
      "@types/*": ["types/*"],
      "@utils/*": ["utils/*"]
    },
    
    // JSON 파일 import 허용
    "resolveJsonModule": true,
    
    // ═══════════════════════════════════════════════
    //  출력 설정
    // ═══════════════════════════════════════════════
    
    // 컴파일된 파일 출력 디렉토리
    "outDir": "./dist",
    
    // 소스 디렉토리
    "rootDir": "./src",
    
    // 선언 파일(.d.ts) 생성
    "declaration": true,
    
    // 선언 파일 출력 위치
    "declarationDir": "./dist/types",
    
    // 소스맵 생성 (디버깅용)
    "sourceMap": true,
    
    // 주석 제거 (프로덕션 빌드 크기 감소)
    "removeComments": true,
    
    // ═══════════════════════════════════════════════
    //  타입 체크 엄격도 (모두 활성화 권장!)
    // ═══════════════════════════════════════════════
    
    // 모든 엄격 옵션 활성화
    "strict": true,
    
    // 아래는 strict: true에 포함되지만 명시적으로 표시
    "noImplicitAny": true,              // any 타입 암시적 사용 금지
    "strictNullChecks": true,           // null/undefined 엄격 체크
    "strictFunctionTypes": true,        // 함수 타입 엄격 체크
    "strictBindCallApply": true,        // bind/call/apply 엄격 체크
    "strictPropertyInitialization": true, // 클래스 속성 초기화 체크
    "noImplicitThis": true,             // this 타입 암시적 사용 금지
    "alwaysStrict": true,               // 'use strict' 자동 추가
    
    // ═══════════════════════════════════════════════
    //  추가 체크 옵션 (버그 예방)
    // ═══════════════════════════════════════════════
    
    // 사용하지 않는 로컬 변수 에러
    "noUnusedLocals": true,
    
    // 사용하지 않는 파라미터 에러
    "noUnusedParameters": true,
    
    // switch의 fallthrough 체크
    "noFallthroughCasesInSwitch": true,
    
    // 함수의 모든 경로에서 값 반환 체크
    "noImplicitReturns": true,
    
    // 인덱스 접근 시 undefined 체크
    "noUncheckedIndexedAccess": true,
    
    // 재정의된 함수가 override 키워드 사용하도록 강제
    "noImplicitOverride": true,
    
    // ═══════════════════════════════════════════════
    //  상호 운용성 (Interop)
    // ═══════════════════════════════════════════════
    
    // CommonJS와 ES6 모듈 간 상호 운용
    "esModuleInterop": true,
    
    // 모듈을 네임스페이스 객체처럼 import 허용
    "allowSyntheticDefaultImports": true,
    
    // ═══════════════════════════════════════════════
    //  실험적 기능
    // ═══════════════════════════════════════════════
    
    // 데코레이터 지원 (NestJS 등에서 필요)
    "experimentalDecorators": true,
    
    // 데코레이터 메타데이터 내보내기
    "emitDecoratorMetadata": true,
    
    // ═══════════════════════════════════════════════
    //  기타 설정
    // ═══════════════════════════════════════════════
    
    // 대소문자 구분 강제 (크로스 플랫폼 호환성)
    "forceConsistentCasingInFileNames": true,
    
    // import 생략 시 확장자 처리
    "skipLibCheck": true,
    
    // 증분 컴파일 (빌드 속도 향상)
    "incremental": true
  },
  
  // 컴파일 대상 파일
  "include": [
    "src/**/*"
  ],
  
  // 컴파일 제외 파일
  "exclude": [
    "node_modules",
    "dist",
    "**/*.spec.ts",
    "**/*.test.ts"
  ],
  
  // 타입 체크만 할 파일 (컴파일 안 함)
  "files": [
    "./src/types/global.d.ts"
  ]
}
```

### 주요 옵션 상세 설명

```typescript
// ═══════════════════════════════════════════════════════
//  자주 헷갈리는 옵션 설명
// ═══════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────
// strict 옵션군
// ─────────────────────────────────────────────────────

// noImplicitAny: true
// ❌ 에러 발생
function add(a, b) { // a와 b의 타입이 any로 추론됨
  return a + b;
}

// ✅ 해결
function add(a: number, b: number): number {
  return a + b;
}

// ─────────────────────────────────────────────────────
// strictNullChecks: true
// ─────────────────────────────────────────────────────

// ❌ 에러 발생
let player: Player | null = getPlayer();
player.nickname; // player가 null일 수 있음

// ✅ 해결
if (player !== null) {
  console.log(player.nickname);
}
// 또는
console.log(player?.nickname);

// ─────────────────────────────────────────────────────
// noUnusedLocals / noUnusedParameters
// ─────────────────────────────────────────────────────

// ❌ 에러 발생
function process(data: string) {
  const temp = "unused"; // 사용되지 않는 변수
  return data.toUpperCase();
}

// ✅ 해결 1: 사용하기
function process(data: string) {
  const temp = "processed";
  return temp + data.toUpperCase();
}

// ✅ 해결 2: 언더스코어 접두사 (의도적으로 사용 안 함)
function process(_data: string) {
  return "default";
}

// ─────────────────────────────────────────────────────
// noImplicitReturns
// ─────────────────────────────────────────────────────

// ❌ 에러 발생
function getValue(flag: boolean): string {
  if (flag) {
    return "yes";
  }
  // 모든 경로에서 값을 반환하지 않음
}

// ✅ 해결
function getValue(flag: boolean): string {
  if (flag) {
    return "yes";
  }
  return "no"; // else 경로 추가
}
```

### 환경별 설정 예제

```json
// ═══════════════════════════════════════════════════════
//  개발 환경 (tsconfig.dev.json)
// ═══════════════════════════════════════════════════════
{
  "extends": "./tsconfig.json",
  "compilerOptions": {
    "sourceMap": true,        // 디버깅용 소스맵
    "removeComments": false,  // 주석 유지
    "incremental": true       // 빠른 재컴파일
  }
}

// ═══════════════════════════════════════════════════════
//  프로덕션 환경 (tsconfig.prod.json)
// ═══════════════════════════════════════════════════════
{
  "extends": "./tsconfig.json",
  "compilerOptions": {
    "sourceMap": false,       // 소스맵 제거 (용량 감소)
    "removeComments": true,   // 주석 제거
    "declaration": false      // 선언 파일 불필요
  }
}

// ═══════════════════════════════════════════════════════
//  테스트 환경 (tsconfig.test.json)
// ═══════════════════════════════════════════════════════
{
  "extends": "./tsconfig.json",
  "compilerOptions": {
    "types": ["jest", "node"]  // Jest 타입 포함
  },
  "include": [
    "src/**/*",
    "tests/**/*"
  ]
}
```

  
## A.3 VSCode 단축키 & 팁
VSCode는 TypeScript 개발에 최적화된 에디터다. 이 단축키들을 익히면 생산성이 비약적으로 향상된다.

### 필수 단축키 (Windows 기준)

```
┌──────────────────────────────────────────────────────────┐
│           TypeScript 개발 핵심 단축키                      │
├──────────────────────────────────────────────────────────┤
│                                                            │
│  코드 탐색                                                 │
│  ─────────────────────────────────────────────────────   │
│  Ctrl + P              파일 빠른 열기                      │
│  Ctrl + Shift + O      현재 파일의 심볼(함수, 클래스) 찾기  │
│  Ctrl + T              워크스페이스 전체 심볼 찾기          │
│  F12                   정의로 이동                         │
│  Ctrl + Click          정의로 이동 (마우스)                │
│  Alt + F12             정의 미리보기 (Peek)                │
│  Shift + F12           모든 참조 찾기                      │
│  Ctrl + -              이전 위치로 돌아가기                │
│  Ctrl + Shift + -      다음 위치로 이동                    │
│                                                            │
│  편집                                                      │
│  ─────────────────────────────────────────────────────   │
│  F2                    심볼 이름 변경 (리팩토링)            │
│  Ctrl + .              Quick Fix / 코드 액션 표시          │
│  Ctrl + Space          IntelliSense 수동 트리거           │
│  Ctrl + Shift + Space  파라미터 힌트 표시                  │
│  Alt + ↑/↓             줄 이동                            │
│  Ctrl + D              다음 동일 단어 선택 (멀티 커서)      │
│  Ctrl + Shift + L      모든 동일 단어 선택                 │
│  Ctrl + /              줄 주석 토글                        │
│  Shift + Alt + A       블록 주석 토글                      │
│                                                            │
│  타입 정보                                                 │
│  ─────────────────────────────────────────────────────   │
│  Ctrl + K, Ctrl + I    타입 정보 호버 표시                 │
│  F8                    다음 에러/경고로 이동               │
│  Shift + F8            이전 에러/경고로 이동               │
│  Ctrl + Shift + M      문제 패널 토글                      │
│                                                            │
│  디버깅                                                    │
│  ─────────────────────────────────────────────────────   │
│  F5                    디버깅 시작                         │
│  F9                    브레이크포인트 토글                 │
│  F10                   Step Over                          │
│  F11                   Step Into                          │
│  Shift + F11           Step Out                           │
│                                                            │
│  터미널                                                    │
│  ─────────────────────────────────────────────────────   │
│  Ctrl + `              통합 터미널 토글                    │
│  Ctrl + Shift + `      새 터미널 생성                      │
│                                                            │
└──────────────────────────────────────────────────────────┘
```

### 고급 활용 팁

**1. 코드 스니펫 활용**

VSCode에 내장된 스니펫을 활용하면 빠르게 코드를 작성할 수 있다.

```typescript
// "interface" 입력 후 Tab
interface  {
  
}

// "class" 입력 후 Tab
class  {
  constructor() {
    
  }
}

// "for" 입력 후 Tab
for (let index = 0; index < array.length; index++) {
  const element = array[index];
  
}
```

**커스텀 스니펫 만들기**

파일 > 기본 설정 > 사용자 코드 조각 > typescript.json

```json
{
  "Game Handler Function": {
    "prefix": "ghandler",
    "body": [
      "public handle${1:Event}(socket: Socket, data: C2S_Packets['${2:event}']): void {",
      "  const playerId = socket.id;",
      "  $0",
      "}"
    ],
    "description": "게임 이벤트 핸들러 템플릿"
  },
  
  "Interface with Timestamp": {
    "prefix": "itime",
    "body": [
      "interface ${1:Name} {",
      "  id: string;",
      "  createdAt: Date;",
      "  updatedAt: Date;",
      "  $0",
      "}"
    ],
    "description": "타임스탬프가 있는 인터페이스"
  }
}
```

**2. 멀티 커서 활용**

```typescript
// 예제: 여러 변수를 동시에 수정
let player1 = getPlayer();
let player2 = getPlayer();
let player3 = getPlayer();

// Ctrl + D로 "player" 단어를 차례대로 선택하고
// 수정하면 모든 인스턴스가 동시에 변경됨
```

**3. 코드 액션 (Quick Fix)**

```typescript
// 타입 에러가 있는 곳에서 Ctrl + . 누르면 자동 수정 제안

// 예: 없는 import 자동 추가
const player = new Player(); // ❌ Player를 찾을 수 없음
// Ctrl + . → "Import Player from './Player'" 선택

// 예: 함수 자동 생성
handleGameStart(); // ❌ 함수가 정의되지 않음
// Ctrl + . → "Add missing function declaration" 선택
```

**4. 타입 정보 활용**

```typescript
// Ctrl + K, Ctrl + I 로 상세한 타입 정보 확인
const result = complexFunction();
// 호버하면 result의 정확한 타입이 표시됨

// 제네릭 함수에서 특히 유용
const wrapped = wrap({ name: "Alice", level: 10 });
// wrapped의 타입: { data: { name: string; level: number; } }
```

### 필수 확장 프로그램

```
┌──────────────────────────────────────────────────────────┐
│         TypeScript 개발 필수 확장 프로그램                 │
├──────────────────────────────────────────────────────────┤
│                                                            │
│  1. ESLint                                                 │
│     코드 품질 검사 및 자동 수정                            │
│     설치: ext install dbaeumer.vscode-eslint              │
│                                                            │
│  2. Prettier - Code formatter                              │
│     코드 포맷팅 자동화                                     │
│     설치: ext install esbenp.prettier-vscode              │
│                                                            │
│  3. Error Lens                                             │
│     에러를 줄 끝에 인라인으로 표시 (매우 편리!)            │
│     설치: ext install usernamehw.errorlens                │
│                                                            │
│  4. Path Intellisense                                      │
│     파일 경로 자동 완성                                    │
│     설치: ext install christian-kohler.path-intellisense  │
│                                                            │
│  5. GitLens                                                │
│     Git 히스토리 및 blame 정보                            │
│     설치: ext install eamodio.gitlens                     │
│                                                            │
│  6. Better Comments                                        │
│     주석을 색상으로 구분 (TODO, FIXME 등)                 │
│     설치: ext install aaron-bond.better-comments          │
│                                                            │
│  7. Thunder Client                                         │
│     API 테스트 (Postman 대체, VSCode 내장)                │
│     설치: ext install rangav.vscode-thunder-client        │
│                                                            │
│  8. REST Client                                            │
│     .http 파일로 API 테스트                               │
│     설치: ext install humao.rest-client                   │
│                                                            │
└──────────────────────────────────────────────────────────┘
```

### 추천 설정 (settings.json)

```json
{
  // ═══════════════════════════════════════════════
  //  TypeScript 설정
  // ═══════════════════════════════════════════════
  
  // 파일 저장 시 자동 포맷
  "editor.formatOnSave": true,
  
  // 기본 포맷터를 Prettier로 설정
  "editor.defaultFormatter": "esbenp.prettier-vscode",
  
  // TypeScript 파일에서 자동 import 정리
  "editor.codeActionsOnSave": {
    "source.organizeImports": true,
    "source.fixAll.eslint": true
  },
  
  // IntelliSense 트리거 설정
  "editor.quickSuggestions": {
    "other": true,
    "comments": false,
    "strings": true
  },
  
  // 파라미터 힌트 항상 표시
  "editor.parameterHints.enabled": true,
  
  // 타입 정보 표시 지연 시간 (ms)
  "editor.hover.delay": 300,
  
  // 미니맵 표시
  "editor.minimap.enabled": true,
  
  // 브래킷 색상 구분
  "editor.bracketPairColorization.enabled": true,
  
  // 인덴트 가이드
  "editor.guides.bracketPairs": true,
  
  // ═══════════════════════════════════════════════
  //  파일 설정
  // ═══════════════════════════════════════════════
  
  // 파일 자동 저장
  "files.autoSave": "afterDelay",
  "files.autoSaveDelay": 1000,
  
  // 파일 제외 (성능 향상)
  "files.exclude": {
    "**/.git": true,
    "**/.svn": true,
    "**/.hg": true,
    "**/CVS": true,
    "**/.DS_Store": true,
    "**/node_modules": true,
    "**/dist": true
  },
  
  // 검색에서 제외
  "search.exclude": {
    "**/node_modules": true,
    "**/dist": true,
    "**/*.log": true
  },
  
  // ═══════════════════════════════════════════════
  //  터미널 설정
  // ═══════════════════════════════════════════════
  
  // 통합 터미널 기본 쉘 (Windows)
  "terminal.integrated.defaultProfile.windows": "PowerShell",
  
  // 터미널 폰트 크기
  "terminal.integrated.fontSize": 14,
  
  // ═══════════════════════════════════════════════
  //  확장 프로그램 설정
  // ═══════════════════════════════════════════════
  
  // Error Lens
  "errorLens.enabledDiagnosticLevels": [
    "error",
    "warning"
  ],
  
  // Prettier
  "prettier.singleQuote": true,
  "prettier.semi": true,
  "prettier.tabWidth": 2,
  "prettier.trailingComma": "es5",
  
  // ESLint
  "eslint.validate": [
    "javascript",
    "typescript"
  ],
  
  // ═══════════════════════════════════════════════
  //  TypeScript 컴파일러 설정
  // ═══════════════════════════════════════════════
  
  // TypeScript 버전 명시 (프로젝트 로컬 버전 사용)
  "typescript.tsdk": "node_modules/typescript/lib",
  
  // 사용하지 않는 변수 회색으로 표시
  "typescript.suggest.autoImports": true,
  
  // import 경로 자동 업데이트
  "typescript.updateImportsOnFileMove.enabled": "always"
}
```

  
## A.4 추가 학습 리소스
TypeScript 학습은 이 책이 끝이 아니다. 지속적으로 업데이트되는 자료와 커뮤니티를 통해 최신 정보를 얻어야 한다.

### 공식 문서

```
┌──────────────────────────────────────────────────────────┐
│              TypeScript 공식 리소스                        │
├──────────────────────────────────────────────────────────┤
│                                                            │
│  📚 TypeScript 공식 문서                                   │
│     https://www.typescriptlang.org/docs/                  │
│     → 가장 정확하고 최신의 정보                            │
│     → Handbook, Reference, Tutorials 섹션 추천            │
│                                                            │
│  🎮 TypeScript Playground                                  │
│     https://www.typescriptlang.org/play                   │
│     → 브라우저에서 즉시 테스트 가능                        │
│     → 컴파일 결과 확인 및 공유 기능                        │
│                                                            │
│  📖 Release Notes                                          │
│     https://www.typescriptlang.org/docs/handbook/release-notes/overview.html
│     → 버전별 새로운 기능 확인                              │
│     → 마이그레이션 가이드 제공                             │
│                                                            │
│  ❓ TypeScript GitHub Issues                               │
│     https://github.com/microsoft/TypeScript/issues        │
│     → 버그 리포트 및 기능 요청                             │
│     → 많은 실전 문제 해결 사례                             │
│                                                            │
└──────────────────────────────────────────────────────────┘
```

### 게임 서버 오픈소스 프로젝트
실전 코드를 읽는 것이 가장 빠른 학습 방법이다.

**추천 프로젝트**

```typescript
// ═══════════════════════════════════════════════════════
//  1. Colyseus - 멀티플레이어 게임 서버 프레임워크
// ═══════════════════════════════════════════════════════
// URL: https://github.com/colyseus/colyseus
// 특징:
// - TypeScript로 작성된 실시간 멀티플레이어 서버
// - 상태 동기화 시스템
// - Room 기반 아키텍처
// 배울 점:
// - 제네릭을 활용한 타입 안전한 상태 관리
// - 데코레이터 패턴 활용
// - WebSocket 통신 구현

// ═══════════════════════════════════════════════════════
//  2. NestJS - Node.js 백엔드 프레임워크
// ═══════════════════════════════════════════════════════
// URL: https://github.com/nestjs/nest
// 특징:
// - 엔터프라이즈급 아키텍처
// - 의존성 주입 패턴
// - 모듈 시스템
// 배울 점:
// - 데코레이터 기반 아키텍처
// - 타입 안전한 의존성 주입
// - 미들웨어 패턴

// ═══════════════════════════════════════════════════════
//  3. Socket.IO - 실시간 통신 라이브러리
// ═══════════════════════════════════════════════════════
// URL: https://github.com/socketio/socket.io
// 특징:
// - WebSocket 기반 실시간 통신
// - 룸/네임스페이스 개념
// - 자동 재연결
// 배울 점:
// - 이벤트 시스템 타이핑
// - 연결 관리 패턴
// - 에러 핸들링

// ═══════════════════════════════════════════════════════
//  4. Phaser - HTML5 게임 엔진 (클라이언트)
// ═══════════════════════════════════════════════════════
// URL: https://github.com/photonstorm/phaser
// 특징:
// - TypeScript 지원
// - 게임 루프 및 씬 관리
// 배울 점:
// - 클래스 상속 구조
// - 이벤트 시스템
// - 게임 로직 구조화

// ═══════════════════════════════════════════════════════
//  5. TypeORM - ORM 라이브러리
// ═══════════════════════════════════════════════════════
// URL: https://github.com/typeorm/typeorm
// 특징:
// - 데코레이터 기반 엔티티 정의
// - 타입 안전한 쿼리 빌더
// 배울 점:
// - 데코레이터 활용
// - 제네릭 리포지토리 패턴
// - 마이그레이션 관리
```

### 온라인 학습 플랫폼

```
┌──────────────────────────────────────────────────────────┐
│            추천 학습 플랫폼 및 코스                         │
├──────────────────────────────────────────────────────────┤
│                                                            │
│  🎓 Execute Program - TypeScript                           │
│     https://www.executeprogram.com/courses/typescript     │
│     → 인터랙티브 학습                                      │
│     → 단계적 난이도 증가                                   │
│                                                            │
│  📺 YouTube - Matt Pocock (TypeScript Tips)                │
│     https://www.youtube.com/@mattpocockuk                 │
│     → 고급 TypeScript 팁                                   │
│     → 실전 문제 해결                                       │
│                                                            │
│  📚 TypeScript Deep Dive (무료 전자책)                     │
│     https://basarat.gitbook.io/typescript/                │
│     → 심화 개념 설명                                       │
│     → 베스트 프랙티스                                      │
│                                                            │
│  🏋️ Type Challenges                                        │
│     https://github.com/type-challenges/type-challenges    │
│     → 타입 시스템 연습 문제                                │
│     → 난이도별 분류                                        │
│                                                            │
└──────────────────────────────────────────────────────────┘
```

### TypeScript 커뮤니티

```
┌──────────────────────────────────────────────────────────┐
│              커뮤니티 및 Q&A 사이트                        │
├──────────────────────────────────────────────────────────┤
│                                                            │
│  💬 TypeScript Discord                                     │
│     https://discord.gg/typescript                         │
│     → 실시간 질문/답변                                     │
│     → 공식 팀 멤버 참여                                    │
│                                                            │
│  📝 Stack Overflow - TypeScript 태그                       │
│     https://stackoverflow.com/questions/tagged/typescript │
│     → 방대한 Q&A 아카이브                                  │
│     → 검증된 답변                                          │
│                                                            │
│  💻 Reddit - r/typescript                                  │
│     https://www.reddit.com/r/typescript/                  │
│     → 최신 뉴스 및 토론                                    │
│     → 프로젝트 공유                                        │
│                                                            │
│  🐦 Twitter - #TypeScript 해시태그                         │
│     → 최신 트렌드 확인                                     │
│     → 영향력 있는 개발자 팔로우                            │
│     추천: @typescript, @mattpocockuk, @basarat            │
│                                                            │
│  🇰🇷 한국 커뮤니티                                          │
│     - 생활코딩 (https://opentutorials.org)               │
│     - 인프런 (https://www.inflearn.com)                  │
│     - 벨로그 TypeScript 태그                              │
│       (https://velog.io/tags/typescript)                 │
│                                                            │
└──────────────────────────────────────────────────────────┘
```

### 실전 연습 아이디어
책을 다 읽었다면 이제 직접 만들어보자.

```typescript
// ═══════════════════════════════════════════════════════
//  초급 프로젝트 아이디어
// ═══════════════════════════════════════════════════════

// 1. 채팅 서버
// - Socket.io로 실시간 채팅
// - 방 생성/입장 기능
// - 타입 안전한 메시지 타입 정의
// 배울 것: 이벤트 시스템, 상태 관리

// 2. RESTful API 서버
// - Express + TypeScript
// - CRUD 엔드포인트
// - 데이터 검증
// 배울 것: 라우팅, 미들웨어, 에러 핸들링

// 3. 간단한 게임 로직 라이브러리
// - 턴제 전투 시스템
// - 인벤토리 시스템
// - 스킬/버프 시스템
// 배울 것: 클래스 설계, 상속, 제네릭

// ═══════════════════════════════════════════════════════
//  중급 프로젝트 아이디어
// ═══════════════════════════════════════════════════════

// 1. 실시간 멀티플레이어 게임
// - 플레이어 동기화
// - 충돌 감지
// - 레이턴시 보상
// 배울 것: 상태 동기화, 성능 최적화

// 2. 게임 서버 프레임워크
// - Room 관리 시스템
// - 플러그인 아키텍처
// - 로깅/모니터링
// 배울 것: 아키텍처 설계, 확장성

// 3. 매치메이킹 시스템
// - MMR 기반 매칭
// - 대기열 관리
// - 타임아웃 처리
// 배울 것: 알고리즘, 비동기 처리

// ═══════════════════════════════════════════════════════
//  고급 프로젝트 아이디어
// ═══════════════════════════════════════════════════════

// 1. 분산 게임 서버
// - 마이크로서비스 아키텍처
// - Redis 기반 상태 공유
// - 로드 밸런싱
// 배울 것: 분산 시스템, 메시지 큐

// 2. 게임 분석 시스템
// - 플레이어 행동 추적
// - 실시간 통계
// - 대시보드
// 배울 것: 데이터 처리, 시각화

// 3. 커스텀 게임 엔진
// - ECS(Entity Component System) 구현
// - 물리 엔진 통합
// - 렌더링 추상화
// 배울 것: 고급 디자인 패턴, 최적화
```

### 지속적인 학습 전략

```
┌──────────────────────────────────────────────────────────┐
│          효과적인 TypeScript 학습 루틴                     │
├──────────────────────────────────────────────────────────┤
│                                                            │
│  주간 학습 계획 (권장)                                     │
│  ───────────────────────────────────────────────────────│
│                                                            │
│  월요일: 새로운 개념 학습 (공식 문서, 블로그)              │
│           → 30분 독서 + 메모                              │
│                                                            │
│  화~목: 실전 코딩                                          │
│           → 작은 프로젝트 또는 기존 코드 개선              │
│           → 하루 1시간                                     │
│                                                            │
│  금요일: 코드 리뷰 & 리팩토링                              │
│           → 이번 주 작성한 코드 검토                       │
│           → 타입을 더 정확하게 개선                        │
│                                                            │
│  주말:   오픈소스 코드 읽기                                │
│           → GitHub 프로젝트 탐색                          │
│           → 흥미로운 패턴 분석 및 적용                     │
│                                                            │
│  학습 팁                                                   │
│  ───────────────────────────────────────────────────────│
│                                                            │
│  ✓ 작은 것부터 시작 - 완벽보다 진행이 중요하다            │
│  ✓ 에러를 두려워하지 말 것 - 에러에서 배운다              │
│  ✓ 타인의 코드 읽기 - 다양한 패턴을 접한다                │
│  ✓ 문서화 습관 - 배운 것을 정리하면 더 오래 기억한다      │
│  ✓ 커뮤니티 참여 - 질문하고 답변하며 성장한다             │
│                                                            │
└──────────────────────────────────────────────────────────┘
```

### 버전 업데이트 추적
TypeScript는 3개월마다 메이저 버전이 릴리스된다. 최신 기능을 놓치지 않으려면 정기적으로 확인해야 한다.

```typescript
// ═══════════════════════════════════════════════════════
//  최신 버전 확인 방법
// ═══════════════════════════════════════════════════════

// 1. 현재 설치된 버전 확인
// 터미널에서:
// tsc --version

// 2. 최신 버전으로 업데이트
// npm update typescript

// 3. 특정 버전 설치
// npm install typescript@5.3.0 --save-dev

// 4. Release Notes 구독
// - TypeScript 블로그: https://devblogs.microsoft.com/typescript/
// - RSS 피드로 구독하거나
// - Twitter에서 @typescript 팔로우

// ═══════════════════════════════════════════════════════
//  주요 버전별 핵심 기능 (참고)
// ═══════════════════════════════════════════════════════

// TypeScript 4.7 (2022)
// - ES 모듈 지원 개선
// - typeof를 통한 타입 추론 강화

// TypeScript 4.8 (2022)
// - infer 타입 개선
// - 템플릿 문자열 타입 향상

// TypeScript 4.9 (2022)
// - satisfies 연산자 추가
// - 자동 접근자 (accessor 키워드)

// TypeScript 5.0 (2023)
// - 데코레이터 표준화
// - const 타입 파라미터
// - 모든 enum은 union enum

// TypeScript 5.1 (2023)
// - undefined를 반환하는 함수 개선
// - JSX 개선

// TypeScript 5.2 (2023)
// - using 키워드 (명시적 리소스 관리)
// - 데코레이터 메타데이터

// TypeScript 5.3 (2024)
// - Import 속성
// - Switch(true) narrowing 개선
```

---

## **마무리**

```
┌────────────────────────────────────────────────────────┐
│                                                         │
│            TypeScript 학습 여정의 시작                   │
│                                                         │
├────────────────────────────────────────────────────────┤
│                                                         │
│  이 부록이 당신의 TypeScript 개발에서                    │
│  빠른 참조 자료가 되기를 바란다.                         │
│                                                         │
│  기억하라:                                               │
│                                                         │
│  "완벽한 코드는 없다.                                    │
│   더 나은 코드를 위해 끊임없이 개선할 뿐이다."           │
│                                                         │
│  TypeScript는 도구일 뿐이다.                             │
│  중요한 것은 문제를 해결하는 당신의 사고력이다.          │
│                                                         │
│  Happy Coding! 🚀                                       │
│                                                         │
└────────────────────────────────────────────────────────┘
```

이 빠른 참조 가이드를 언제든 다시 찾아보고, 필요한 정보를 빠르게 활용하라. TypeScript와 함께하는 게임 서버 개발이 더욱 생산적이고 즐거워지기를 바란다.  