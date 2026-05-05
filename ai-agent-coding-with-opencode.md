# 🚀 OpenCode로 시작하는 AI 코딩 에이전트  

저자: 최흥배, Claude AI   
      
> **대상 도구**: OpenCode (open-code.ai) — 오픈소스 AI 코딩 에이전트 (터미널 기반)
> **실습 언어**: C# (.NET)
> **환경**: Windows 11

---

## **📌 OpenCode란?**
OpenCode는 터미널에서 동작하는 오픈소스 AI 코딩 에이전트입니다. TUI(Terminal User Interface)를 기반으로 Claude, GPT, Gemini 등 75개 이상의 LLM 제공자를 지원하며, 파일 편집, 셸 명령 실행, 코드 탐색, LSP(언어 서버) 통합, MCP 확장까지 다양한 기능을 제공합니다.

> ⚠️ **중요한 참고사항**: 기존의 `opencode-ai/opencode` (GitHub) 저장소는 현재 **Charm 팀의 [Crush](https://github.com/charmbracelet/crush)** 로 이전되었습니다. 현재 활발히 유지·개발되고 있는 최신 OpenCode는 **[open-code.ai](https://open-code.ai)** (`anomalyco/opencode`) 기반입니다. 본 로드맵은 **open-code.ai 기준 최신 버전**을 따릅니다.

---

## **📚 목차 전체 구조**

```
Part 1. 기초 환경 구축
Part 2. OpenCode 핵심 개념 이해
Part 3. C# 프로젝트와 연동
Part 4. 고급 기능 활용
Part 5. 자동화 및 커스터마이징
Part 6. 실전 프로젝트 실습
```

---

## **Part 1. 기초 환경 구축**

### **1-1. 사전 준비 (Prerequisites)**
Windows 11에서 OpenCode를 원활하게 사용하려면 다음 도구들이 필요합니다.

- **모던 터미널 설치**: Windows Terminal(권장), 또는 WezTerm, Alacritty 등 TUI 렌더링이 잘 되는 터미널 에뮬레이터
- **.NET SDK 설치**: C# 개발을 위한 .NET 9 이상 설치 (`dotnet --version`으로 확인)
- **LLM API 키 준비**: Anthropic (Claude), OpenAI, Google Gemini, GitHub Copilot 중 하나 이상
- **Git 설치**: 버전 관리 및 프로젝트 초기화에 필요

**🔨 실습 1-1**: 터미널에서 아래 명령어들을 실행하여 환경을 점검한다.
```powershell
dotnet --version
git --version
node --version   # NPM 설치 방법 사용 시
```

---

### **1-2. OpenCode 설치 (Windows 11)**
Windows 11에서는 여러 방법으로 설치할 수 있습니다.

- **Chocolatey 사용 (권장)**:
```powershell
choco install opencode
```
- **Scoop 사용**:
```powershell
scoop install opencode
```
- **NPM 사용**:
```powershell
npm install -g opencode-ai
```
- **바이너리 직접 다운로드**: [GitHub Releases](https://github.com/anomalyco/opencode/releases) 에서 `opencode-windows-amd64.exe` 다운로드 후 PATH에 등록

**🔨 실습 1-2**: 설치 후 버전 확인 및 첫 실행
```powershell
opencode --version
opencode   # TUI 실행
```

---

### **1-3. LLM 제공자 연결 및 API 키 설정**
OpenCode는 다양한 방식으로 API 키를 설정할 수 있습니다.

**TUI 내 `/connect` 명령어로 설정**하는 방법이 가장 간편합니다. 또는 환경변수로 설정할 수도 있습니다.

```powershell
# PowerShell 환경변수 설정 (영구 등록)
[System.Environment]::SetEnvironmentVariable("ANTHROPIC_API_KEY", "sk-ant-...", "User")
[System.Environment]::SetEnvironmentVariable("OPENAI_API_KEY", "sk-...", "User")
[System.Environment]::SetEnvironmentVariable("GEMINI_API_KEY", "AIza...", "User")
```

**글로벌 설정 파일 위치 (Windows)**:
```
C:\Users\<사용자명>\.config\opencode\opencode.json
```

**🔨 실습 1-3**: `opencode` 실행 후 TUI에서 `/connect` 명령을 입력하여 원하는 프로바이더를 연결하고, `Ctrl+O`로 모델 선택 다이얼로그를 열어 모델을 변경해본다.

---

### **1-4. C# LSP(Language Server) 설정**
OpenCode는 LSP를 통해 코드 인텔리전스(오류 진단 등)를 제공합니다. C# 최신 버전 지원을 위해 **`csharp-ls`** 설치가 권장됩니다.

> **왜 `csharp-ls`인가?** OpenCode에 내장된 C# 언어 서버는 즉시 업데이트되지 않아 C# 14의 `field` 키워드 같은 최신 기능을 인식하지 못할 수 있습니다. `csharp-ls`를 전역 설치하면 OpenCode가 자동으로 이를 감지하여 내장 서버 대신 사용합니다.

```powershell
dotnet tool install --global csharp-ls
```

설치 후 별도 설정 없이 OpenCode가 자동으로 `csharp-ls`를 우선 사용합니다.

**🔨 실습 1-4**: 설치 완료 후 기존 C# 프로젝트 폴더에서 OpenCode를 실행하고 `/init` 명령을 실행하여 LSP가 정상 동작하는지 확인한다.


### roslyn-language-server 사용하기 가 더 좋다
OpenCode GitHub(anomalyco/opencode) Issue [#14462](https://github.com/anomalyco/opencode/issues/14462)와 PR [#14463](https://github.com/anomalyco/opencode/pull/14463)에 따르면, `roslyn-language-server`는 **현재 실험적(Experimental) Opt-in 방식**으로 지원이 추가되었습니다.

즉, **기본값은 여전히 `csharp-ls`이며**, 환경변수 플래그를 설정해야 `roslyn-language-server`로 전환됩니다.

```powershell
# 1. 먼저 roslyn-language-server 전역 설치
dotnet tool install --global roslyn-language-server --prerelease

# 2. OpenCode 실행 시 환경변수로 활성화
$env:OPENCODE_EXPERIMENTAL_LSP_ROSLYN = "1"
opencode
```

또는 PowerShell 프로파일에 영구 등록:
```powershell
# $PROFILE 파일에 추가
[System.Environment]::SetEnvironmentVariable("OPENCODE_EXPERIMENTAL_LSP_ROSLYN", "1", "User")
```

> ⚠️ **주의**: `--prerelease` 플래그가 현재 필요합니다. Microsoft가 "아직 초기 실험 단계"라고 밝혔기 때문에, 안정성 측면에서 간헐적인 문제가 발생할 수 있습니다. 플래그를 설정하면 `csharp-ls`는 자동으로 비활성화됩니다.  

---
  

## **Part 2. OpenCode 핵심 개념 이해**

### **2-1. TUI 인터페이스 및 기본 조작법**
OpenCode의 TUI는 Bubble Tea 프레임워크로 구축된 인터랙티브 터미널 UI입니다. 핵심 단축키를 익히는 것이 생산성의 핵심입니다.

| 단축키 | 기능 |
|---|---|
| `Ctrl+K` | 커맨드 다이얼로그 열기 |
| `Ctrl+O` | 모델 선택 다이얼로그 |
| `Ctrl+N` | 새 세션 생성 |
| `Ctrl+A` | 세션 전환 |
| `Tab` | 모드 전환 (Build ↔ Plan) |
| `Ctrl+S` | 메시지 전송 |
| `Ctrl+E` | 외부 에디터로 메시지 작성 |
| `Ctrl+X` | 현재 작업 취소 |
| `Ctrl+?` | 도움말 토글 |

**🔨 실습 2-1**: TUI를 실행하고 위 단축키들을 모두 직접 눌러보며 각 기능을 확인한다.

---

### **2-2. 모드(Modes) 이해 — Build vs Plan**
OpenCode는 두 가지 내장 모드를 제공하며, 작업 목적에 따라 전환하며 사용하는 것이 핵심 워크플로우입니다.

**Build 모드** (기본값)는 파일 쓰기, 편집, 셸 명령 실행 등 모든 툴이 활성화된 실제 구현 모드입니다.

**Plan 모드**는 파일 편집과 셸 명령 실행이 비활성화되어 코드 분석과 구현 계획만 제안하는 안전한 검토 모드입니다. `.opencode/plans/*.md` 파일에만 계획을 기록할 수 있어, 의도치 않은 코드 변경을 방지합니다.

**🔨 실습 2-2**: C# 콘솔 앱 프로젝트에서 다음 순서로 실습한다.
1. **Plan 모드**(Tab)로 전환 후 "사용자 입력을 받아 계산기 기능을 구현하는 계획을 세워줘"라고 요청
2. 계획을 확인하고 피드백 제공
3. **Build 모드**로 전환 후 "방금 계획대로 구현해줘"라고 요청

---

### **2-3. 세션(Session) 관리**
OpenCode는 SQLite 기반으로 모든 대화를 저장하고 세션 단위로 관리합니다.

- `Ctrl+N`: 새 세션 시작
- `Ctrl+A`: 이전 세션 목록에서 선택하여 대화 재개
- `/undo`: 직전 변경 사항 되돌리기
- `/redo`: 되돌린 변경 사항 다시 적용
- `/share`: 현재 세션을 공개 링크로 공유

**🔨 실습 2-3**: 세션을 여러 개 만들고, `Ctrl+A`로 세션 간 전환을 연습한다. `/undo`와 `/redo`를 사용하여 코드 변경을 되돌리고 다시 적용해본다.

---

### **2-4. 내장 도구(Tools) 이해**
AI가 코드 작업 시 사용하는 도구들을 이해하면 더 정확한 프롬프트를 작성할 수 있습니다.

| 도구 | 설명 |
|---|---|
| `write` | 새 파일 생성 |
| `edit` | 기존 파일 수정 |
| `patch` | diff 패치 적용 |
| `read` | 파일 내용 읽기 |
| `grep` | 파일 내용 검색 |
| `glob` | 패턴으로 파일 찾기 |
| `list` | 디렉토리 목록 |
| `bash` | 셸 명령 실행 |
| `webfetch` | URL에서 데이터 가져오기 |
| `todowrite/todoread` | 작업 목록 관리 |

**🔨 실습 2-4**: "현재 프로젝트의 모든 `.cs` 파일을 찾아서 네임스페이스 목록을 알려줘"라고 요청하여 `glob`과 `grep` 도구가 어떻게 동작하는지 관찰한다.

---

### **2-5. 에이전트(Agents) 이해**
에이전트는 특정 목적에 맞게 설정된 특화 AI 어시스턴트입니다. 모드(Mode)가 UI 전환 방식이라면, 에이전트는 독립적인 전문가 인격체입니다.

**내장 Primary 에이전트:**
- **Build**: 모든 툴 활성화, 기본 개발 에이전트
- **Plan**: 파일 수정 불가, 계획·분석 전용

**내장 Subagent (@ 멘션으로 호출):**
- **@general**: 복잡한 멀티스텝 작업 처리
- **@explore**: 코드베이스 탐색 전용 (읽기 전용)

**🔨 실습 2-5**: 메시지 입력창에 `@explore C# 파일 중 static 메서드가 있는 클래스를 모두 찾아줘`라고 입력하여 subagent 호출을 경험한다.

---

## **Part 3. C# 프로젝트와 연동**

### **3-1. C# 프로젝트 초기화 및 AGENTS.md 생성**
새로운 C# 프로젝트를 시작할 때 OpenCode를 초기화하면 프로젝트 구조와 패턴을 AI가 학습하는 `AGENTS.md` 파일이 생성됩니다.

```powershell
# C# 콘솔 앱 생성
dotnet new console -n MyApp -o MyApp
cd MyApp
git init
git add .
git commit -m "initial commit"

# OpenCode 실행 및 초기화
opencode
# TUI에서: /init 명령 실행
```

**🔨 실습 3-1**: 위 순서로 새 C# 콘솔 앱을 만들고 `/init`을 실행하여 생성된 `AGENTS.md` 파일의 내용을 확인한다.

---

### **3-2. 코드 생성 실습 — C# 클래스 및 메서드 작성**
OpenCode에게 코드를 생성 요청할 때는 구체적이고 상세한 프롬프트가 좋은 결과를 만들어냅니다.

**🔨 실습 3-2**: 다음 프롬프트들을 순서대로 실습한다.

```
1. "제네릭 스택(Stack<T>) 클래스를 C#으로 구현해줘. 
   Push, Pop, Peek, IsEmpty 메서드와 Count 프로퍼티를 포함하고, 
   스레드 안전성을 위해 lock을 사용해줘."

2. "방금 만든 스택 클래스에 대한 xUnit 단위 테스트를 작성해줘. 
   경계 케이스(빈 스택에서 Pop)도 포함해줘."

3. "dotnet test로 테스트를 실행하고 결과를 알려줘."
```

---

### **3-3. 코드 리팩토링 실습**
기존 코드를 개선하는 작업에서 OpenCode가 특히 강력한 능력을 발휘합니다.

**🔨 실습 3-3**: 의도적으로 잘못 작성된 아래 코드를 파일로 저장하고 OpenCode에게 리팩토링을 요청한다.

```csharp
// BadCode.cs - 리팩토링 전
public class calc {
    public int add(int a, int b) { return a + b; }
    public int sub(int a, int b) { return a - b; }
    public double div(int a, int b) { return a / b; }
    public int mul(int a, int b) { return a * b; }
}
```

```
"BadCode.cs를 C# 코딩 컨벤션에 맞게 리팩토링해줘. 
네이밍, 타입 안전성, 예외 처리를 개선하고, 
인터페이스도 분리해줘."
```

---

### **3-4. 버그 탐지 및 수정 실습**
OpenCode의 LSP 통합과 진단 도구를 활용한 버그 수정 워크플로우를 익힙니다.

**🔨 실습 3-4**: 의도적으로 버그가 포함된 C# 코드를 작성하고 OpenCode에게 분석을 요청한다.

```csharp
// BuggyCode.cs
public class DataProcessor {
    private List<int> data = null;
    
    public void AddItem(int item) {
        data.Add(item);  // NullReferenceException!
    }
    
    public double GetAverage() {
        return data.Sum() / data.Count;  // DivideByZeroException!
    }
}
```

```
"BuggyCode.cs의 버그를 모두 찾아서 수정해줘. 
각 버그의 원인과 수정 방법도 설명해줘."
```

---

### **3-5. ASP.NET Core API 프로젝트 실습**
실전에 가까운 웹 API 프로젝트를 OpenCode와 함께 구축해봅니다.

**🔨 실습 3-5**: 다음 순서로 실습한다.

```powershell
dotnet new webapi -n TodoApi -o TodoApi
cd TodoApi
opencode
```

```
TUI에서 순서대로 요청:

1. "/init 으로 프로젝트 초기화"

2. "Todo 아이템을 관리하는 REST API를 만들어줘.
   - Todo 모델: Id(int), Title(string), IsCompleted(bool), CreatedAt(DateTime)
   - CRUD 엔드포인트 전체 구현
   - Entity Framework Core + SQLite 사용
   - 서비스 레이어와 컨트롤러 분리"

3. "방금 만든 API에 대한 통합 테스트를 작성해줘."

4. "dotnet run으로 실행하고 Swagger UI URL을 알려줘."
```

---

## **Part 4. 고급 기능 활용**

### **4-1. 커스텀 에이전트(Custom Agent) 만들기 — C# 전용 리뷰어**
C# 코드 리뷰 전용 에이전트를 직접 만들어봅니다. 에이전트는 Markdown 파일로 정의됩니다.

**🔨 실습 4-1**: 프로젝트 루트에 `.opencode/agents/csharp-reviewer.md` 파일을 생성한다.

```markdown
---
description: Reviews C# code for best practices, performance, and .NET conventions
mode: subagent
model: anthropic/claude-sonnet-4-20250514
temperature: 0.1
tools:
  write: false
  edit: false
  bash: false
---
You are a senior C# developer specializing in .NET best practices.

When reviewing code, focus on:
- C# naming conventions (PascalCase for classes/methods, camelCase for locals)
- SOLID principles adherence
- Async/await patterns and deadlock risks
- IDisposable and resource management
- Null safety and nullable reference types
- LINQ performance considerations
- Exception handling best practices
- Thread safety issues

Always provide specific line references and concrete improvement suggestions.
Do NOT make direct changes — only analyze and report.
```

그 다음 TUI에서 `@csharp-reviewer DataProcessor.cs 코드를 리뷰해줘`와 같이 호출한다.

---

### **4-2. 커스텀 커맨드(Custom Commands) 만들기**
반복적으로 사용하는 프롬프트 패턴을 커스텀 커맨드로 등록합니다.

**🔨 실습 4-2**: `.opencode/commands/` 폴더에 다음 파일들을 생성한다.

**`.opencode/commands/cs-unittest.md`** — 단위 테스트 자동 생성 커맨드
```markdown
$FILE_PATH에 있는 모든 public 메서드에 대한 xUnit 단위 테스트를 작성해줘.
- Happy path, edge case, exception case를 모두 포함해줘.
- FluentAssertions 라이브러리를 사용해줘.
- 테스트 파일은 $FILE_PATH와 같은 구조의 Tests 프로젝트에 생성해줘.
```

**`.opencode/commands/cs-review.md`** — 코드 리뷰 커맨드
```markdown
현재 git staged 파일들에 대해 C# 코드 리뷰를 수행해줘.
다음 기준으로 평가해줘:
1. C# 코딩 컨벤션 준수
2. 잠재적 버그 및 예외 처리
3. 성능 이슈 (LINQ, async/await, 메모리)
4. 보안 취약점
```

`Ctrl+K`로 커맨드 다이얼로그를 열어 등록된 커맨드를 실행한다.

---

### **4-3. MCP(Model Context Protocol) 서버 연동**
MCP를 통해 외부 도구와 서비스를 OpenCode에 연결합니다.

**🔨 실습 4-3**: GitHub MCP 서버를 연동하여 코드 리뷰 워크플로우를 자동화한다.

```json
// opencode.json에 추가
{
  "$schema": "https://opencode.ai/config.json",
  "mcp": {
    "github": {
      "type": "stdio",
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-github"],
      "env": {
        "GITHUB_PERSONAL_ACCESS_TOKEN": "{env:GITHUB_TOKEN}"
      }
    },
    "filesystem": {
      "type": "stdio",
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-filesystem", "C:\\Projects"]
    }
  }
}
```

연동 후 TUI에서 "GitHub에서 최근 PR을 불러와서 C# 코드 변경 사항을 리뷰해줘"라고 요청해본다.

---

### **4-4. Non-Interactive 모드 (자동화 스크립트)**
OpenCode를 CI/CD 파이프라인이나 PowerShell 스크립트에서 자동화하여 사용하는 방법을 익힙니다.

**🔨 실습 4-4**: 다음 PowerShell 스크립트를 작성하고 실행해본다.

```powershell
# auto-review.ps1
# git commit 전 자동 코드 리뷰 스크립트

$changedFiles = git diff --cached --name-only --diff-filter=M | Where-Object { $_ -like "*.cs" }

if ($changedFiles) {
    Write-Host "🔍 C# 파일 변경 감지. OpenCode 코드 리뷰 실행 중..." -ForegroundColor Cyan
    
    $fileList = $changedFiles -join ", "
    $prompt = "다음 파일들의 변경 사항을 C# 코딩 컨벤션 관점에서 빠르게 리뷰해줘: $fileList"
    
    # Non-interactive 모드로 실행 (-q: 스피너 숨김)
    opencode -p $prompt -q
} else {
    Write-Host "✅ 변경된 C# 파일 없음." -ForegroundColor Green
}
```

```powershell
# 단일 프롬프트 실행 예시
opencode -p "Program.cs 파일을 읽고 잠재적 버그를 JSON 형식으로 알려줘" -f json -q
```

---

### **4-5. 규칙(Rules) 및 AGENTS.md 최적화**
프로젝트별 AI 행동 규칙을 정의하여 더 일관된 코드를 생성하도록 유도합니다.

**🔨 실습 4-5**: 프로젝트 루트에 `AGENTS.md` 파일을 직접 커스터마이징한다.

```markdown
# AGENTS.md — C# 프로젝트 AI 행동 규칙

## 프로젝트 개요
- .NET 9 기반 ASP.NET Core Web API
- C# 14 사용 (field 키워드 등 최신 기능 적극 활용)
- 아키텍처: Clean Architecture (Domain / Application / Infrastructure / API)

## 코딩 컨벤션
- 모든 public API에 XML 문서 주석 필수
- nullable reference types 활성화 (#nullable enable)
- 비동기 메서드는 반드시 `Async` 접미사 사용
- LINQ는 가독성 우선, 성능이 중요할 때만 for loop 사용

## 금지 사항
- `var` 대신 명시적 타입 선호 (단, LINQ 결과는 var 허용)
- `Thread.Sleep` 금지 → `Task.Delay` 사용
- `Console.WriteLine` 금지 → `ILogger` 사용

## 테스트 규칙
- 테스트 클래스 이름: `{ClassName}Tests`
- 테스트 메서드 이름: `{Method}_{Scenario}_{ExpectedResult}`
- AAA 패턴 (Arrange / Act / Assert) 주석 필수
```

---

## **Part 5. 자동화 및 커스터마이징**
  
### **5-1. opencode.json 고급 설정**
프로젝트별 최적화된 설정 파일을 작성합니다.

**🔨 실습 5-1**: 프로젝트 루트에 `opencode.json`을 생성한다.

```json
{
  "$schema": "https://opencode.ai/config.json",
  "model": "anthropic/claude-sonnet-4-20250514",
  "small_model": "anthropic/claude-haiku-4-20250514",
  "theme": "opencode",
  "autoupdate": true,
  "compaction": {
    "auto": true,
    "prune": true
  },
  "permission": {
    "bash": "ask",
    "edit": "allow",
    "webfetch": "allow"
  },
  "agent": {
    "build": {
      "model": "anthropic/claude-sonnet-4-20250514",
      "temperature": 0.3
    },
    "plan": {
      "model": "anthropic/claude-haiku-4-20250514",
      "temperature": 0.1
    }
  },
  "formatter": {
    "*.cs": {
      "command": "dotnet",
      "args": ["csharpier", "{file}"]
    }
  },
  "watcher": {
    "ignore": ["bin", "obj", ".git", "*.user", "*.suo"]
  }
}
```

---

### **5-2. 코드 포매터(Formatter) 연동**
CSharpier를 OpenCode와 연동하여 AI가 코드를 작성한 후 자동으로 포매팅되도록 설정합니다.

**🔨 실습 5-2**:
```powershell
# CSharpier 전역 설치
dotnet tool install --global csharpier

# 동작 확인
csharpier --version
```

opencode.json에 포매터 설정 후, OpenCode가 C# 파일을 생성/수정할 때마다 자동으로 CSharpier가 실행되는지 확인한다.

---

### **5-3. 멀티 에이전트 워크플로우 설계**
여러 전문 에이전트가 협력하는 복잡한 워크플로우를 구성합니다.

**🔨 실습 5-3**: 다음 에이전트 파일들을 만들어 멀티 에이전트 시스템을 구성한다.

- `.opencode/agents/architect.md` — 아키텍처 설계 전담
- `.opencode/agents/implementer.md` — 코드 구현 전담
- `.opencode/agents/tester.md` — 테스트 작성 전담
- `.opencode/agents/documenter.md` — 문서화 전담

```
Build 모드에서 요청:
"새로운 결제 모듈을 추가해야 해. 
@architect 먼저 아키텍처를 설계하고,
구현 계획이 확정되면 @implementer가 코드를 작성하고,
@tester가 테스트를 작성해줘."
```

---

## **Part 6. 실전 프로젝트 실습**

### **6-1. 미니 프로젝트: C# CLI 도구 만들기**
OpenCode를 활용하여 처음부터 끝까지 완성된 C# CLI 도구를 만드는 실습입니다.

**목표**: 파일 내용을 분석하여 C# 코드 통계(클래스 수, 메서드 수, 코드 줄 수 등)를 출력하는 CLI 도구

**🔨 실습 순서**:

```
Step 1 (Plan 모드): 
"C# 파일을 분석하는 CLI 도구의 설계 계획을 세워줘.
System.CommandLine 패키지를 사용하고,
--path, --recursive, --output(json/table) 옵션을 지원해야 해."

Step 2 (Build 모드): 
"방금 계획대로 구현해줘."

Step 3:
"단위 테스트를 작성하고 dotnet test로 실행해줘."

Step 4:
"README.md와 사용 예시도 작성해줘."

Step 5 (Non-interactive):
opencode -p "현재 프로젝트를 dotnet publish로 빌드하고 실행 파일을 생성해줘" -q
```

---

### **6-2. 미니 프로젝트: ASP.NET Core + Entity Framework 실전 API**
**목표**: 도서 관리 REST API (CRUD, 검색, 페이징, 인증)

**🔨 실습 순서**:

```
Step 1: dotnet new webapi -n BookstoreApi

Step 2 (Plan 모드):
"도서 관리 API의 전체 아키텍처를 설계해줘.
- Clean Architecture 적용
- JWT 인증 포함
- EF Core + SQLite
- 검색(제목, 저자), 페이징, 정렬 기능
- Swagger 문서화"

Step 3 (Build 모드):
"계획대로 전체 구현을 시작해줘. 
도메인 모델부터 시작해서 인프라, 애플리케이션, API 레이어 순서로."

Step 4:
"@tester 통합 테스트와 단위 테스트를 모두 작성해줘."

Step 5:
"API 문서를 README.md로 작성해줘."
```

---

### **6-3. 레거시 코드 리팩토링 프로젝트**
실제 업무에서 가장 흔한 시나리오인 레거시 코드 현대화 실습입니다.

**🔨 실습 순서**:

```powershell
# 의도적으로 레거시 스타일(.NET Framework 스타일)로 작성된 코드 생성
dotnet new console -n LegacyApp
```

```
Step 1 (@explore 에이전트):
"@explore 현재 프로젝트 코드베이스를 분석하고 
현대화가 필요한 패턴들을 목록으로 알려줘."

Step 2 (Plan 모드):
"분석 결과를 바탕으로 다음 현대화 작업의 우선순위와 계획을 세워줘:
- 동기 코드를 async/await로 전환
- 예외 처리 개선
- Nullable Reference Types 활성화
- 의존성 주입 패턴 적용"

Step 3 (Build 모드):
"계획에 따라 단계별로 리팩토링을 진행해줘."

Step 4:
"리팩토링 전후 변경 사항을 문서화해줘."
```

---

## **🎓 학습 완료 체크리스트**
다음 항목들을 스스로 수행할 수 있으면 OpenCode를 능숙하게 사용하는 것입니다.

- [ ] Windows 11에 OpenCode 설치 및 API 키 연결 완료
- [ ] C# LSP (`csharp-ls`) 설정 완료
- [ ] Build/Plan 모드 전환을 활용한 워크플로우 수행
- [ ] 세션 관리 및 `/undo`, `/redo` 활용
- [ ] `@explore`, `@general` subagent 호출 경험
- [ ] C# 전용 커스텀 에이전트 파일 작성 및 활용
- [ ] 커스텀 커맨드 (`.opencode/commands/`) 등록 및 사용
- [ ] MCP 서버 하나 이상 연동 경험
- [ ] Non-interactive 모드로 PowerShell 스크립트 자동화
- [ ] `opencode.json` 커스터마이징 (포매터, 권한, 에이전트 설정)
- [ ] `AGENTS.md` 작성으로 프로젝트 맥락 제공
- [ ] 처음부터 끝까지 OpenCode와 함께 완성한 C# 프로젝트 1개 이상

---

## **📖 참고 자료**

- **공식 문서**: [open-code.ai/en/docs](https://open-code.ai/en/docs)
- **GitHub**: [github.com/anomalyco/opencode](https://github.com/anomalyco/opencode)
- **C# LSP (csharp-ls)**: `dotnet tool install --global csharp-ls`
- **CSharpier 포매터**: [csharpier.com](https://csharpier.com)
- **MCP 서버 목록**: [modelcontextprotocol.io](https://modelcontextprotocol.io)   
   

  

# OpenCode 활용 완전 가이드 — 코딩부터 문서·에이전트까지

## **🗂️ 전체 목차**

```
Part 1. 준비 — 공통 환경 세팅
Part 2. 프로그래밍 활용
Part 3. 발표 문서 만들기
Part 4. 리서치 & 기술 문서 자동화
Part 5. 에이전트 활용
Part 6. 자동화 스크립트 활용
```

---

## **Part 1. 준비 — 공통 환경 세팅**

### **OpenCode 설치 및 기본 설정**

모든 실습에 앞서 OpenCode와 필수 도구들을 설치합니다. Windows Terminal(또는 PowerShell 7 이상)을 **관리자 권한**으로 열고 다음을 실행합니다.

```powershell
# Scoop으로 OpenCode 설치 (없으면 Scoop 먼저 설치)
scoop install opencode

# 또는 NPM으로 설치
npm install -g opencode-ai

# 설치 확인
opencode --version
```

다음으로 글로벌 설정 파일을 만듭니다. 위치는 `C:\Users\<사용자명>\.config\opencode\opencode.json`입니다.

```json
{
  "$schema": "https://opencode.ai/config.json",
  "theme": "opencode",
  "autoupdate": true,
  "permission": {
    "bash": "ask",
    "edit": "allow",
    "write": "allow",
    "webfetch": "allow",
    "websearch": "allow"
  },
  "compaction": {
    "auto": true,
    "prune": true
  }
}
```

OpenCode를 실행하고 `/connect` 명령으로 LLM 제공자를 연결합니다. 무료로 시작하려면 GitHub Copilot을 연결하거나, Anthropic Claude / Google Gemini API 키를 등록합니다.

```powershell
opencode
# TUI 실행 후: /connect 입력 → 프로바이더 선택 → API 키 입력
```

---

## **Part 2. 프로그래밍 활용**

### **2-1. 첫 번째 실습 — C# 프로젝트 처음부터 함께 만들기**
OpenCode와 함께 처음부터 프로젝트를 만드는 기본 워크플로우를 익힙니다.

```powershell
# 프로젝트 생성
dotnet new console -n MyFirstApp -o MyFirstApp
cd MyFirstApp
git init && git add . && git commit -m "init"

# C# LSP 설치 (최신 Roslyn 기반)
$env:OPENCODE_EXPERIMENTAL_LSP_ROSLYN = "1"
dotnet tool install --global roslyn-language-server --prerelease

# OpenCode 실행
opencode
```

TUI가 열리면 가장 먼저 `/init`을 실행합니다. 이렇게 하면 OpenCode가 프로젝트 구조를 분석하여 `AGENTS.md`를 생성하고, 이후 모든 대화에서 이 맥락을 참조합니다.

이제 **Plan 모드**(Tab 키)로 전환하고 다음과 같이 요청해봅니다.

```
할 일 목록(Todo)을 관리하는 CLI 앱을 만들고 싶어.
- 기능: 추가(add), 목록(list), 완료 처리(done), 삭제(delete)
- 데이터는 JSON 파일로 로컬 저장
- System.CommandLine 패키지 사용
- 구현 계획을 먼저 세워줘.
```

계획을 확인하고 문제가 없으면 **Build 모드**로 전환 후 `"계획대로 구현해줘"`라고 입력합니다. OpenCode가 파일을 생성하고 수정하는 과정을 실시간으로 지켜볼 수 있습니다.

```powershell
# 완료 후 직접 실행해보기
dotnet run -- add "OpenCode 공부하기"
dotnet run -- list
dotnet run -- done 1
```

---

### **2-2. 레거시 코드 리팩토링 실습**
실무에서 가장 자주 마주치는 상황인 "오래된 코드를 현대적으로 바꾸기"를 연습합니다.

먼저 의도적으로 낡은 스타일의 C# 코드를 작성합니다. `LegacyService.cs`라는 이름으로 프로젝트 폴더 안에 저장합니다.

```csharp
// LegacyService.cs — 일부러 나쁘게 쓴 코드
using System;
using System.Collections.Generic;

public class dataservice {
    public static List<string> items = new List<string>();

    public static void addItem(string s) {
        if(s != null) {
            items.Add(s);
            Console.WriteLine("added: " + s);
        }
    }

    public static string getItem(int i) {
        try {
            return items[i];
        } catch(Exception e) {
            Console.WriteLine(e.Message);
            return null;
        }
    }

    public static void deleteAll() {
        items = new List<string>();
    }
}
```

OpenCode TUI에서 다음과 같이 요청합니다.

```
LegacyService.cs 파일을 열어서 분석해줘.
다음 기준으로 리팩토링해줘:
1. C# 네이밍 컨벤션 (PascalCase)
2. nullable 참조 타입 (#nullable enable)
3. 제네릭으로 타입 안전성 향상
4. 예외 처리 개선 (throw 대신 결과 패턴)
5. 인터페이스 분리
리팩토링 전후를 비교해서 무엇이 왜 바뀌었는지 설명해줘.
```

결과를 확인하고 마음에 들지 않는 부분이 있으면 `/undo`로 되돌린 뒤 다시 요청할 수 있습니다.

---

### **2-3. 버그 탐지 + 자동 테스트 생성 실습**

OpenCode의 LSP 진단 기능과 테스트 생성 능력을 함께 활용합니다.

```powershell
# xUnit 테스트 프로젝트 추가
dotnet new xunit -n MyFirstApp.Tests -o MyFirstApp.Tests
dotnet sln add MyFirstApp.Tests/MyFirstApp.Tests.csproj
dotnet add MyFirstApp.Tests/MyFirstApp.Tests.csproj reference MyFirstApp/MyFirstApp.csproj
```

TUI에서 요청합니다.

```
방금 만든 TodoService의 모든 public 메서드에 대한 
xUnit 단위 테스트를 MyFirstApp.Tests 프로젝트에 작성해줘.
- 정상 케이스, 경계 케이스, 예외 케이스 모두 포함
- AAA(Arrange/Act/Assert) 패턴 주석 필수
- 테스트 작성이 끝나면 dotnet test로 실행해서 결과를 알려줘.
```

모든 테스트가 통과하면 다음을 추가로 요청합니다.

```
일부러 버그를 하나 만들어서 테스트가 실패하게 해봐.
그 다음 버그를 찾아서 고치는 과정을 보여줘.
```

이 흐름이 실무에서 가장 많이 쓰이는 **빨간 불 → 고침 → 초록 불** 사이클입니다.

---

### **2-4. ASP.NET Core API 실전 실습**

더 큰 규모의 실전 프로젝트를 OpenCode와 처음부터 끝까지 완성해봅니다.

```powershell
dotnet new webapi -n BookApi -o BookApi
cd BookApi
git init && git add . && git commit -m "init"
opencode
```

TUI에서 Plan 모드로 시작합니다.

```
도서 관리 REST API를 Clean Architecture로 만들고 싶어.

요구사항:
- Book 모델: Id, Title, Author, ISBN, PublishedYear, Price
- CRUD 엔드포인트 (GET/POST/PUT/DELETE)
- EF Core + SQLite 사용
- 제목/저자로 검색, 페이징(page, pageSize) 지원
- FluentValidation으로 입력 검증
- 레이어 구조: Domain / Application / Infrastructure / API

전체 구현 계획을 단계별로 세워줘.
```

계획이 마음에 들면 Build 모드로 전환 후 단계별로 구현합니다.

```
1단계: 폴더 구조와 프로젝트 파일들을 먼저 만들어줘.
```

각 단계가 완료될 때마다 `dotnet build`로 빌드를 확인하고 오류가 생기면 그대로 OpenCode에게 붙여넣어 수정을 요청합니다. 이것이 가장 빠른 디버깅 루프입니다.

---

## **Part 3. 발표 문서 만들기**

OpenCode는 코드만 만드는 도구가 아닙니다. `webfetch`, `websearch`, `write` 도구를 결합하면 발표 자료를 자동으로 리서치하고 완성된 슬라이드 파일까지 만들 수 있습니다. 여기서는 **Marp**(Markdown으로 슬라이드를 만드는 도구)를 함께 사용합니다.

### **3-1. Marp 환경 준비**

```powershell
# Marp CLI 설치
npm install -g @marp-team/marp-cli

# 발표 문서 작업할 폴더 생성
mkdir C:\Presentations\dotnet-ai-talk
cd C:\Presentations\dotnet-ai-talk
opencode
```

---

### **3-2. 리서치 → 슬라이드 자동 생성 실습**

OpenCode의 `websearch`와 `webfetch` 도구를 사용해 최신 정보를 수집하고 슬라이드를 만듭니다.

TUI에서 다음과 같이 요청합니다.

```
".NET과 AI 통합의 최신 트렌드"를 주제로 발표 슬라이드를 만들어줘.

준비 과정:
1. websearch 도구로 2025~2026년 .NET AI 관련 최신 뉴스와 트렌드를 검색해줘.
2. Microsoft의 공식 .NET AI 관련 문서(https://learn.microsoft.com)에서 
   관련 내용을 webfetch로 가져와줘.
3. 수집한 내용을 바탕으로 30분 발표용 슬라이드를 Marp Markdown 형식으로 
   presentation.md 파일에 작성해줘.

슬라이드 구성:
- 표지 (제목, 발표자, 날짜)
- 목차 (5~7개 섹션)
- 각 섹션 2~3장 (핵심 내용 + 코드 예시)
- 마무리 (핵심 요약 + Q&A)
총 20~25장 분량
```

OpenCode가 실시간으로 웹을 검색하고 내용을 수집한 뒤 `presentation.md` 파일을 작성합니다. 완료되면 Marp로 HTML과 PDF를 생성합니다.

```powershell
# HTML 슬라이드 생성 (브라우저에서 발표 가능)
marp presentation.md -o presentation.html

# PDF 생성
marp presentation.md -o presentation.pdf --pdf

# 미리보기 서버 실행 (저장할 때마다 자동 갱신)
marp presentation.md --preview
```

---

### **3-3. 슬라이드 스타일 커스터마이징 실습**

생성된 슬라이드에 디자인을 입힙니다.

```
presentation.md 파일을 열어서 다음을 개선해줘:

1. Marp 커스텀 테마를 추가해줘:
   - 배경색: 진한 네이비 (#0a1628)
   - 강조색: 밝은 청록 (#00d4ff)
   - 폰트: 코드는 JetBrains Mono, 본문은 Segoe UI
   - 각 섹션 헤더 슬라이드는 배경이 다르게

2. 코드 블록이 있는 슬라이드에 C# syntax highlighting이 
   잘 보이도록 조정해줘.

3. 마지막 슬라이드에 발표자 연락처와 GitHub 링크 섹션을 추가해줘.
```

---

### **3-4. 기술 제안서(Word/Markdown) 작성 실습**

발표 슬라이드 외에 기술 제안서 형태의 문서도 만들 수 있습니다.

```powershell
mkdir C:\Docs\proposal
cd C:\Docs\proposal
opencode
```

```
우리 팀에 AI 코딩 도구(OpenCode)를 도입하자는 
기술 제안서를 작성해줘.

문서 구성:
1. 요약 (Executive Summary) - 1페이지
2. 현황 문제 분석 - 개발 생산성 이슈
3. 제안 솔루션 - OpenCode 소개
4. 기대 효과 - 정량적 수치 포함
5. 도입 비용 분석 - ROI 계산
6. 구현 로드맵 - 3개월 계획
7. 리스크 및 대응 방안
8. 결론 및 승인 요청

형식: Markdown (.md)
분량: A4 기준 10~15페이지
대상 독자: 비기술직 경영진
```

문서가 생성되면 Pandoc으로 Word나 PDF로 변환할 수 있습니다.

```powershell
# Pandoc 설치 (없는 경우)
scoop install pandoc

# Word 문서로 변환
pandoc proposal.md -o proposal.docx

# PDF로 변환
pandoc proposal.md -o proposal.pdf
```

---

## **Part 4. 리서치 & 기술 문서 자동화**

### **4-1. 웹 리서치 에이전트 만들기**
OpenCode의 `websearch`와 `webfetch` 도구를 활용하여 특정 주제를 심층 조사하는 흐름을 만들어봅니다.

```powershell
mkdir C:\Research\csharp-performance
cd C:\Research\csharp-performance
opencode
```

```
C# .NET 9의 성능 개선 사항을 심층 조사해줘.

조사 방법:
1. websearch로 ".NET 9 performance improvements benchmarks" 검색
2. 상위 결과 URL들을 webfetch로 상세 내용 수집
3. Microsoft 공식 블로그(https://devblogs.microsoft.com)에서 
   관련 포스트도 수집해줘
4. 수집한 내용을 research-report.md 파일로 정리해줘

보고서 형식:
- 각 개선 사항마다: 설명, 수치, 코드 예시, 출처 URL
- 마지막에 핵심 요약 표 (개선 항목 / 개선 전 / 개선 후 / 비율)
```

---

### **4-2. 코드베이스 자동 문서화 실습**
기존 C# 프로젝트의 문서가 없거나 오래되었을 때 OpenCode로 자동으로 재생성합니다.

앞서 만든 `BookApi` 프로젝트 폴더에서 OpenCode를 실행합니다.

```powershell
cd C:\Projects\BookApi
opencode
```

```
이 프로젝트 전체를 분석해서 개발자용 README.md를 작성해줘.

포함 내용:
1. 프로젝트 개요 및 아키텍처 설명
2. 폴더 구조 트리 (glob 도구로 실제 구조 파악)
3. 설치 및 실행 방법 (단계별 명령어)
4. API 엔드포인트 목록 (메서드, URL, 설명, 요청/응답 예시)
5. 환경변수 설정 방법
6. 테스트 실행 방법
7. 기여 가이드라인

실제 코드를 읽어서 정확한 내용을 작성해줘.
상상으로 쓰지 말고, 모르는 부분은 코드에서 확인해줘.
```

---

### **4-3. 변경 이력(Changelog) 자동 생성 실습**
Git 커밋 이력을 분석해서 사람이 읽을 수 있는 변경 이력을 자동 작성합니다.

```
git log를 실행해서 최근 커밋 이력을 가져와줘.
커밋 메시지들을 분석해서 CHANGELOG.md를 작성해줘.

형식:
- Keep a Changelog 표준 (https://keepachangelog.com) 따르기
- 버전별로 Added / Changed / Fixed / Removed 분류
- 기술적 내용을 사용자가 이해할 수 있는 언어로 변환
```

---

## **Part 5. 에이전트 활용**
이 파트는 OpenCode의 가장 강력한 기능인 **커스텀 에이전트**와 **멀티 에이전트 협업**을 실습합니다.

### **5-1. 커스텀 에이전트 만들기 — 역할 분리의 핵심**

단일 에이전트로 모든 것을 처리하면 품질이 낮아집니다. 역할을 분리한 전문 에이전트를 만들어봅니다.

프로젝트 루트에 `.opencode/agents/` 폴더를 만들고 다음 파일들을 생성합니다.

**`.opencode/agents/architect.md`** — 아키텍처 설계 전문가

```markdown
---
description: 소프트웨어 아키텍처를 설계하고 기술적 의사결정을 도와주는 시니어 아키텍트
mode: subagent
model: anthropic/claude-sonnet-4-20250514
temperature: 0.2
tools:
  write: false
  edit: false
  bash: false
  read: true
  grep: true
  glob: true
  websearch: true
  webfetch: true
---
당신은 15년 경력의 .NET 시니어 아키텍트입니다.

역할:
- 요구사항을 분석하여 최적의 아키텍처를 제안합니다
- Clean Architecture, DDD, CQRS 등 패턴의 적용 여부를 판단합니다
- 기술 스택 선택에 대한 트레이드오프를 명확히 설명합니다
- 구현 계획을 단계별로 문서화합니다

절대로 코드를 직접 작성하거나 파일을 수정하지 마세요.
오직 분석, 제안, 계획 문서 작성만 수행하세요.
```

**`.opencode/agents/implementer.md`** — 코드 구현 전문가

```markdown
---
description: 아키텍처 계획에 따라 실제 C# 코드를 구현하는 시니어 개발자
mode: subagent
model: anthropic/claude-sonnet-4-20250514
temperature: 0.1
tools:
  write: true
  edit: true
  bash: true
  read: true
  grep: true
  glob: true
---
당신은 C# .NET 전문 시니어 개발자입니다.

역할:
- 주어진 설계 문서에 따라 정확하게 코드를 구현합니다
- C# 최신 기능과 .NET 9 API를 적극 활용합니다
- SOLID 원칙을 준수하며 테스트 가능한 코드를 작성합니다
- 구현 후 반드시 dotnet build로 빌드 성공을 확인합니다

코딩 컨벤션:
- nullable reference types 활성화
- async/await 패턴 적용
- XML 주석 필수
- 매직 넘버 금지 (상수 사용)
```

**`.opencode/agents/reviewer.md`** — 코드 리뷰 전문가

```markdown
---
description: C# 코드 품질, 보안, 성능을 검토하는 코드 리뷰어
mode: subagent
model: anthropic/claude-sonnet-4-20250514
temperature: 0.1
tools:
  write: false
  edit: false
  bash: true
  read: true
  grep: true
  glob: true
---
당신은 C# 코드 리뷰 전문가입니다.

리뷰 기준:
1. 보안: SQL 인젝션, 입력 검증 누락, 민감정보 노출
2. 성능: N+1 쿼리, 불필요한 메모리 할당, 비효율적 LINQ
3. 품질: 단일 책임 원칙, 중복 코드, 테스트 가능성
4. C# 관례: 네이밍, null 처리, 예외 처리 패턴

리뷰 결과는 심각도(CRITICAL/WARNING/INFO)와 함께 
구체적인 라인 번호와 개선 방안을 포함해야 합니다.

절대로 코드를 직접 수정하지 마세요.
```

**`.opencode/agents/tester.md`** — 테스트 전문가

```markdown
---
description: xUnit 단위 테스트와 통합 테스트를 작성하는 QA 엔지니어
mode: subagent
model: anthropic/claude-haiku-4-20250514
temperature: 0.1
tools:
  write: true
  edit: true
  bash: true
  read: true
  grep: true
  glob: true
---
당신은 .NET 테스트 전문 QA 엔지니어입니다.

테스트 작성 원칙:
- xUnit + FluentAssertions + NSubstitute 사용
- AAA(Arrange/Act/Assert) 패턴 엄수
- 테스트 이름: {메서드명}_{시나리오}_{기대결과}
- 각 테스트는 하나의 시나리오만 검증
- Happy path, Edge case, Exception case 모두 포함
- 테스트 작성 후 dotnet test로 반드시 통과 확인
```

---

### **5-2. 멀티 에이전트 협업 실습 — 새 기능 추가**
위에서 만든 에이전트들을 실제로 협업시켜 새 기능을 추가해봅니다.

`BookApi` 프로젝트에서 OpenCode를 실행합니다.

```powershell
cd C:\Projects\BookApi
opencode
```

Build 모드(기본)에서 다음과 같이 요청합니다.

```
BookApi에 "도서 대출 시스템"을 추가하고 싶어.

다음 순서로 진행해줘:

1단계 - @architect 에게 요청:
   기존 BookApi 코드베이스를 분석하고,
   도서 대출 시스템(Borrowing System)의 아키텍처를 설계해줘.
   사용자가 책을 빌리고 반납하며 대출 이력을 조회할 수 있어야 해.

2단계 - 설계 결과를 확인하고 내가 승인하면 @implementer 에게 요청:
   @architect의 설계대로 구현해줘.

3단계 - 구현 완료 후 @reviewer 에게 요청:
   방금 구현된 코드 전체를 리뷰해줘.

4단계 - 리뷰 피드백 반영 후 @tester 에게 요청:
   구현된 대출 시스템의 테스트를 작성하고 실행해줘.
```

> 💡 **팁**: 각 단계 사이에 에이전트의 결과물을 직접 읽어보고 피드백을 제공하세요. "이 부분은 다르게 해줘"라고 개입하면 에이전트가 방향을 수정합니다.

---

### **5-3. 리서치 에이전트 만들기 — 코딩 외 작업용**
코딩과 관련 없는 순수 리서치·정보 수집 전용 에이전트를 만들어봅니다. 이 에이전트는 코드 프로젝트가 없는 빈 폴더에서도 유용합니다.

**`~/.config/opencode/agents/researcher.md`** (전역 에이전트로 등록)

```markdown
---
description: 웹에서 정보를 수집하고 체계적인 보고서를 작성하는 리서치 에이전트
mode: subagent
model: anthropic/claude-sonnet-4-20250514
temperature: 0.3
tools:
  write: true
  edit: true
  bash: false
  read: true
  websearch: true
  webfetch: true
---
당신은 전문 리서치 에이전트입니다.

리서치 프로세스:
1. websearch로 주제 관련 최신 정보를 3~5개 키워드로 나눠 검색
2. 신뢰도 높은 소스(공식 문서, 학술 자료, 주요 미디어)를 선별
3. webfetch로 각 소스의 상세 내용 수집
4. 수집 내용을 검증하고 교차 확인
5. 마크다운 보고서로 정리 (출처 URL 반드시 포함)

보고서 형식:
- 요약 (3~5문장)
- 핵심 발견 사항 (섹션별)
- 데이터/통계 (표 형식)
- 출처 목록
```

이 에이전트를 사용하는 방법입니다. 빈 폴더에서도 동작합니다.

```powershell
mkdir C:\Research\topics
cd C:\Research\topics
opencode
```

```
@researcher "2026년 C# vs Python 생산성 비교" 주제를 
심층 조사해서 research-csharp-vs-python.md 파일로 저장해줘.
특히 AI/ML 작업에서의 비교를 중점적으로 다뤄줘.
```

---

### **5-4. 오케스트레이터 에이전트 — 에이전트들을 지휘하는 에이전트**
더 복잡한 작업을 위해 다른 에이전트들을 자동으로 조율하는 마스터 에이전트를 만들어봅니다.

**`.opencode/agents/orchestrator.md`**

```markdown
---
description: 복잡한 개발 태스크를 분석하고 적절한 전문 에이전트들에게 작업을 분배하는 오케스트레이터
mode: primary
model: anthropic/claude-sonnet-4-20250514
temperature: 0.2
tools:
  read: true
  glob: true
  grep: true
  bash: true
  todowrite: true
  todoread: true
permission:
  task:
    "*": "allow"
---
당신은 개발 프로젝트를 총괄하는 오케스트레이터입니다.

역할:
- 사용자의 요청을 분석하여 필요한 작업을 파악합니다
- todowrite로 작업 목록을 먼저 작성합니다
- 각 작업을 적합한 전문 에이전트에게 위임합니다
  * 설계/분석 → @architect
  * 코드 구현 → @implementer  
  * 코드 리뷰 → @reviewer
  * 테스트 작성 → @tester
  * 리서치 → @researcher
- 각 에이전트의 결과를 취합하여 사용자에게 보고합니다

직접 코드를 작성하지 마세요.
항상 전문 에이전트에게 위임하세요.
```

이제 이 오케스트레이터를 Tab 키로 전환하여 사용할 수 있습니다.

```
새로운 "결제 모듈"을 BookApi에 추가해줘.
Stripe API 연동, 결제 이력 저장, 환불 처리가 필요해.
알아서 설계부터 테스트까지 진행해줘.
```

오케스트레이터가 자동으로 할 일 목록을 만들고, 각 전문 에이전트에게 순서대로 작업을 위임하는 과정을 볼 수 있습니다.

---

## **Part 6. 자동화 스크립트 활용**
OpenCode의 Non-interactive 모드(`-p` 플래그)를 사용하면 TUI 없이 스크립트로 자동화할 수 있습니다.

### **6-1. Git Commit 전 자동 코드 리뷰 훅**
코드를 커밋하기 전에 자동으로 OpenCode가 리뷰를 수행하는 Git pre-commit 훅을 만듭니다.

`.git/hooks/pre-commit` 파일을 만들고 PowerShell 스크립트로 작성합니다. 단, Git 훅은 bash 스크립트이므로 PowerShell을 호출하도록 합니다.

```bash
#!/bin/sh
# .git/hooks/pre-commit

powershell.exe -NonInteractive -File ".git/hooks/pre-commit.ps1"
exit $?
```

`.git/hooks/pre-commit.ps1`

```powershell
# .git/hooks/pre-commit.ps1
$changedFiles = git diff --cached --name-only --diff-filter=ACM | 
    Where-Object { $_ -match "\.(cs)$" }

if (-not $changedFiles) {
    Write-Host "✅ C# 파일 변경 없음. 커밋 진행." -ForegroundColor Green
    exit 0
}

Write-Host "🔍 변경된 C# 파일 발견. OpenCode 빠른 리뷰 실행 중..." -ForegroundColor Cyan
Write-Host "파일: $($changedFiles -join ', ')" -ForegroundColor Gray

$fileList = $changedFiles -join ", "
$prompt = @"
다음 파일들의 staged 변경 사항(git diff --cached)을 확인하고
C# 관점에서 빠른 리뷰를 해줘: $fileList

심각한 문제(null 참조 위험, 보안 취약점, 빌드 오류 가능성)가 있으면
[BLOCK] 태그로 표시해줘.
그 외 개선 제안은 [SUGGEST] 태그로 표시해줘.
문제가 없으면 [OK] 라고만 답해줘.
"@

$result = opencode -p $prompt -q 2>&1

Write-Host "`n📋 리뷰 결과:" -ForegroundColor Yellow
Write-Host $result

if ($result -match "\[BLOCK\]") {
    Write-Host "`n❌ 심각한 문제가 발견되었습니다. 커밋이 차단되었습니다." -ForegroundColor Red
    Write-Host "문제를 수정하거나 'git commit --no-verify'로 건너뛸 수 있습니다." -ForegroundColor Gray
    exit 1
}

Write-Host "`n✅ 리뷰 통과. 커밋 진행." -ForegroundColor Green
exit 0
```

---

### **6-2. 매일 아침 프로젝트 상태 보고서 자동 생성**
작업 시작 전 프로젝트 현황을 자동으로 요약해주는 스크립트입니다.

`daily-report.ps1`로 저장합니다.

```powershell
# daily-report.ps1
# 사용법: .\daily-report.ps1 -ProjectPath "C:\Projects\BookApi"

param(
    [string]$ProjectPath = (Get-Location).Path
)

Set-Location $ProjectPath

$today = Get-Date -Format "yyyy-MM-dd"
$reportFile = ".\reports\daily-$today.md"

New-Item -ItemType Directory -Force -Path ".\reports" | Out-Null

Write-Host "📊 프로젝트 일일 보고서 생성 중..." -ForegroundColor Cyan

$prompt = @"
이 프로젝트의 오늘($today) 기준 상태를 분석해서 
일일 보고서를 작성해줘.

분석 항목:
1. git log --oneline -10 으로 최근 커밋 이력
2. git status 로 현재 작업 상태
3. dotnet build 로 빌드 상태 확인
4. dotnet test 로 테스트 통과율 확인
5. TODO/FIXME 주석이 있는 파일들 grep으로 찾기

보고서 형식 (Markdown):
# 일일 보고서 - $today

## 📌 오늘의 상태 요약
## 🔨 최근 작업 내역  
## ✅ 빌드 & 테스트 현황
## ⚠️ 미완료 작업 (TODO/FIXME)
## 🎯 오늘 추천 작업

보고서를 $reportFile 파일로 저장해줘.
"@

opencode -p $prompt -q

if (Test-Path $reportFile) {
    Write-Host "✅ 보고서 생성 완료: $reportFile" -ForegroundColor Green
    # 선택: 메모장으로 바로 열기
    # notepad $reportFile
} else {
    Write-Host "⚠️ 보고서 파일 생성 실패" -ForegroundColor Yellow
}
```

작업 시작 시마다 실행하거나, Windows 작업 스케줄러에 등록하면 매일 아침 자동으로 실행됩니다.

```powershell
.\daily-report.ps1 -ProjectPath "C:\Projects\BookApi"
```

---

### **6-3. Marp 슬라이드 자동 빌드 파이프라인**

발표 자료를 Git에 올릴 때마다 자동으로 HTML, PDF, PPTX를 생성하는 파이프라인입니다.

`build-slides.ps1`

```powershell
# build-slides.ps1
# 모든 .md 발표 파일을 찾아서 다양한 형식으로 자동 변환

param(
    [string]$InputDir = ".\slides",
    [string]$OutputDir = ".\output"
)

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$mdFiles = Get-ChildItem -Path $InputDir -Filter "*.md" -Recurse

if ($mdFiles.Count -eq 0) {
    Write-Host "⚠️ 변환할 Markdown 파일이 없습니다." -ForegroundColor Yellow
    exit 0
}

foreach ($file in $mdFiles) {
    $baseName = $file.BaseName
    Write-Host "🔄 변환 중: $($file.Name)" -ForegroundColor Cyan

    # HTML 생성
    marp $file.FullName -o "$OutputDir\$baseName.html" --html 2>&1 | Out-Null
    
    # PDF 생성
    marp $file.FullName -o "$OutputDir\$baseName.pdf" --pdf 2>&1 | Out-Null

    Write-Host "  ✅ HTML + PDF 생성 완료" -ForegroundColor Green
}

# OpenCode로 슬라이드 품질 체크
Write-Host "`n📋 OpenCode로 슬라이드 내용 최종 검토 중..." -ForegroundColor Cyan

$prompt = @"
$InputDir 폴더의 모든 .md 슬라이드 파일을 읽고
각 파일에 대해 다음을 체크해줘:
1. 오탈자 및 문법 오류
2. 슬라이드 흐름이 자연스러운지
3. 코드 블록 문법이 올바른지
4. 개선 제안 사항

결과를 $OutputDir\quality-report.md 파일로 저장해줘.
"@

opencode -p $prompt -q

Write-Host "`n🎉 모든 슬라이드 변환 완료!" -ForegroundColor Green
Write-Host "출력 폴더: $OutputDir" -ForegroundColor Gray
```

---

### **6-4. 종합 워크플로우 — 처음부터 끝까지 한 번에**

지금까지 배운 모든 것을 결합한 최종 자동화 스크립트입니다. 아이디어 하나를 입력하면 설계, 구현, 테스트, 문서화, 발표 자료까지 자동으로 만들어줍니다.

`full-workflow.ps1`

```powershell
# full-workflow.ps1
# 사용법: .\full-workflow.ps1 -Idea "사용자 인증 모듈"

param(
    [Parameter(Mandatory=$true)]
    [string]$Idea,
    [string]$ProjectPath = (Get-Location).Path
)

Set-Location $ProjectPath
$timestamp = Get-Date -Format "yyyyMMdd-HHmm"
$outputDir = ".\workflow-$timestamp"
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

Write-Host "🚀 전체 개발 워크플로우 시작: '$Idea'" -ForegroundColor Magenta
Write-Host "=" * 60

# Step 1: 아키텍처 설계
Write-Host "`n[1/5] 🏗️ 아키텍처 설계 중..." -ForegroundColor Cyan
opencode -p "@architect '$Idea' 기능의 아키텍처 설계서를 $outputDir\architecture.md 에 작성해줘." -q

# Step 2: 코드 구현
Write-Host "`n[2/5] 💻 코드 구현 중..." -ForegroundColor Cyan
opencode -p "$outputDir\architecture.md 를 읽고 @implementer 설계대로 구현해줘. 빌드 성공까지 확인해줘." -q

# Step 3: 코드 리뷰
Write-Host "`n[3/5] 🔍 코드 리뷰 중..." -ForegroundColor Cyan
opencode -p "@reviewer 방금 구현된 '$Idea' 코드를 전체 리뷰하고 $outputDir\review.md 에 저장해줘." -q

# Step 4: 테스트 작성
Write-Host "`n[4/5] 🧪 테스트 작성 중..." -ForegroundColor Cyan
opencode -p "@tester '$Idea' 구현 코드의 단위 테스트를 작성하고 dotnet test로 통과 확인해줘." -q

# Step 5: 문서 + 발표자료 생성
Write-Host "`n[5/5] 📄 문서 및 발표자료 생성 중..." -ForegroundColor Cyan
$slidePrompt = "'$Idea' 구현 내용을 5분 발표용 Marp 슬라이드로 $outputDir\slides.md 에 작성해줘. 아키텍처, 핵심 코드, 테스트 결과 포함."
opencode -p $slidePrompt -q
marp "$outputDir\slides.md" -o "$outputDir\slides.html" --html 2>&1 | Out-Null

Write-Host "`n✨ 전체 워크플로우 완료!" -ForegroundColor Green
Write-Host "📁 결과물 위치: $outputDir" -ForegroundColor Gray
Write-Host "  - architecture.md : 아키텍처 설계서"
Write-Host "  - review.md       : 코드 리뷰 보고서"
Write-Host "  - slides.html     : 발표 슬라이드"

# 결과 폴더 탐색기로 열기
explorer.exe $outputDir
```

실행 방법입니다.

```powershell
cd C:\Projects\BookApi
.\full-workflow.ps1 -Idea "사용자 인증 및 JWT 토큰 발급 모듈"
```

---

## **🎓 활용 요약 — 한눈에 보기**

| 활용 분야 | 핵심 도구/기능 | 대표 실습 |
|---|---|---|
| **프로그래밍** | Build/Plan 모드, LSP, bash | C# API 처음부터 완성 |
| **발표 문서** | websearch + webfetch + Marp | 리서치 → 슬라이드 자동 생성 |
| **기술 문서** | read + glob + grep + write | README, Changelog 자동화 |
| **전문 에이전트** | Custom agents (md 파일) | 아키텍트/구현/리뷰/테스트 분리 |
| **멀티 에이전트** | @ 멘션 + 오케스트레이터 | 에이전트 협업 파이프라인 |
| **자동화 스크립트** | `-p` 비대화형 모드 | Git 훅, 일일 보고서, 빌드 파이프라인 |

OpenCode는 사용할수록 더 강력해집니다. `AGENTS.md`에 프로젝트 규칙을 쌓아가고, 커스텀 에이전트를 정교하게 다듬을수록 AI가 여러분의 프로젝트를 더 깊이 이해하게 됩니다. 처음에는 단순한 코드 생성 도구로 시작해도 충분합니다. 익숙해질수록 자연스럽게 에이전트 조율과 자동화로 발전하게 됩니다.  