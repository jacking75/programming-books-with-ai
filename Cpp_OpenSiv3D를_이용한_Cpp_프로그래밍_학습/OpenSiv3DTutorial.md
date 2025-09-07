# OpenSiv3D 학습 가이드
이 글은 https://github.com/Siv3D/siv3d.docs/tree/main/en-us/docs  문서를 번역&정리한 것이다.    

[Siv3D-Samples](https://github.com/Siv3D/Siv3D-Samples )   
샘플 프로그램을 참고하면 OpenSiv3D로 어디까지 할 수 있는지, 어떻게 사용하는지 배우는데 도움이 된다.  
  
[Tutorial](https://siv3d.github.io/en-us/tutorial/hello/ )  
   [GUI](https://siv3d.github.io/en-us/tutorial2/gui/)  
   
[Sample 설명](https://siv3d.github.io/en-us/samples/games/ )  
  
[API 리스](https://siv3d.github.io/en-us/api/classes/ )    
  

  
-----    

## 1. Siv3D 소개

### Siv3D란?
- C++로 게임이나 애플리케이션을 쉽고 간단하게 개발할 수 있는 프레임워크
- 2D/3D 그래픽, 오디오, 입력 처리 등을 통합적으로 제공
- 크로스 플랫폼 지원 (Windows, macOS, Linux, Web)

### 주요 특징
- 직관적이고 간결한 API
- 풍부한 내장 기능 (3,700개 이상의 이모지, 물리 엔진, 음성 처리 등)
- 강력한 2D/3D 렌더링 기능
- 실시간 코드 수정 지원 (Hot Reload)

---

## 2. 개발 환경 설정

### 기본 설정
```cpp
# include <Siv3D.hpp>

void Main()
{
    // 여기에 프로그램 코드 작성
}
```

### 헤더 파일
- `<Siv3D.hpp>` 하나만 포함하면 모든 기능 사용 가능
- 표준 라이브러리 헤더들이 자동으로 포함됨

---

## 3. 기본 프로그램 구조

### Main 함수 구조
일반 C++의 `int main()` 대신 `void Main()` 사용:

```cpp
# include <Siv3D.hpp>

void Main()
{
    // 초기화 (메인 루프 이전)
    // - 화면 설정, 텍스처 생성, 폰트 초기화 등
    
    while (System::Update())
    {
        // 메인 루프 (초당 60-120회 실행)
        // - 입력 처리와 그리기
    }
    
    // 정리 작업 (메인 루프 이후)
    // - 게임 데이터 저장 등
}
```

### 프로그램 종료 방법
1. 윈도우 닫기 버튼 클릭
2. ESC 키 누르기
3. 코드에서 `System::Exit()` 호출

### 메인 루프의 중요성
- `System::Update()`는 모니터 주사율에 맞춰 실행됨
- 화면 업데이트, 입력 처리 등을 내부적으로 처리
- 무거운 작업(이미지 로딩 등)은 메인 루프 밖에서 수행

---

## 4. 화면과 좌표 시스템

### 기본 화면 크기
- 기본: 800 × 600 픽셀
- 변경: `Window::Resize(width, height)`

```cpp
// 화면 크기를 1280×720으로 변경
Window::Resize(1280, 720);
```

### 좌표 시스템
- 원점 (0, 0): 화면 왼쪽 상단
- X축: 오른쪽으로 갈수록 증가
- Y축: 아래로 갈수록 증가

### 좌표 클래스
```cpp
// 정수 좌표
Point pos1{ 100, 200 };

// 실수 좌표  
Vec2 pos2{ 100.5, 200.3 };

// 변환
Point intPos = pos2.asPoint(); // Vec2 → Point
Vec2 floatPos = pos1;          // Point → Vec2 (자동 변환)
```

---

## 5. 기본 도형 그리기

### 원 그리기
```cpp
void Main()
{
    while (System::Update())
    {
        // 기본 원 (중심 x, y, 반지름)
        Circle{ 200, 300, 50 }.draw();
        
        // 색상이 있는 원
        Circle{ 400, 300, 50 }.draw(Palette::Red);
        
        // 반투명 원 (r, g, b, a)
        Circle{ 600, 300, 50 }.draw(ColorF{ 1.0, 0.0, 0.0, 0.5 });
        
        // 원의 테두리만
        Circle{ 200, 500, 50 }.drawFrame(5, Palette::Blue);
    }
}
```

### 사각형 그리기
```cpp
void Main()
{
    while (System::Update())
    {
        // 기본 사각형 (x, y, 너비, 높이)
        Rect{ 100, 100, 200, 150 }.draw();
        
        // 중심점으로 생성
        Rect{ Arg::center(400, 300), 200, 150 }.draw(Palette::Orange);
        
        // 정사각형 (x, y, 크기)
        Rect{ 600, 100, 100 }.draw(ColorF{ 0.8, 0.9, 1.0 });
        
        // 그라데이션
        Rect{ 100, 400, 200, 100 }
            .draw(Arg::top = ColorF{ 1.0 }, Arg::bottom = ColorF{ 0.0 });
    }
}
```

### 배경색 변경
```cpp
void Main()
{
    // RGB로 지정
    Scene::SetBackground(ColorF{ 0.6, 0.8, 0.7 });
    
    // HSV로 지정  
    Scene::SetBackground(HSV{ 200.0, 0.5, 1.0 });
    
    // 팔레트 색상
    Scene::SetBackground(Palette::Skyblue);
    
    while (System::Update())
    {
        // 시간에 따라 변화하는 배경
        double hue = Scene::Time() * 60;
        Scene::SetBackground(HSV{ hue });
    }
}
```

---

## 6. 텍스트와 이모지

### 폰트 생성과 텍스트 그리기
```cpp
void Main()
{
    Scene::SetBackground(ColorF{ 0.6, 0.8, 0.7 });
    
    // 폰트 생성 (MSDF 방식, 크기 48, 굵게)
    const Font font{ FontMethod::MSDF, 48, Typeface::Bold };
    
    while (System::Update())
    {
        // 기본 텍스트 (크기, 위치, 색상)
        font(U"Hello, Siv3D!").draw(80, Vec2{ 80, 100 }, ColorF{ 0.2 });
        
        // 중앙 정렬
        font(U"Center").drawAt(60, Vec2{ 400, 300 });
        
        // 우측 정렬
        font(U"Right").draw(40, Arg::rightCenter(780, 200), ColorF{ 0.1 });
        
        // 변수 포함 (포맷 문자열)
        int score = 12345;
        font(U"Score: {}"_fmt(score)).draw(40, Vec2{ 100, 400 });
        
        // 소수점 지정
        double pi = 3.14159;
        font(U"PI: {:.2f}"_fmt(pi)).draw(40, Vec2{ 100, 450 });
    }
}
```  
 
`const Font font{ FontMethod::MSDF, 48, Typeface::Bold };` 이렇게 하면 한글의 경우 글자가 깨져 나온다. 한글이 제대로 나오게 하려면 아래와 같이 한다.  
`const Font font{ FontMethod::SDF, 48, Typeface::CJK_Regular_KR };`  
`Typeface::CJK_Regular_KR` 이 폰트를 사용하게 해야 한글이 출력된다.  

### 이모지 사용
```cpp
void Main()
{
    Scene::SetBackground(ColorF{ 0.6, 0.8, 0.7 });
    
    // 이모지 텍스처 생성 (메인 루프 밖에서!)
    const Texture emoji{ U"🐱"_emoji };
    
    while (System::Update())
    {
        // 이모지 그리기
        emoji.drawAt(400, 300);
        
        // 크기 조절
        emoji.scaled(0.5).drawAt(200, 200);
        
        // 회전
        emoji.rotated(45_deg).drawAt(600, 200);
        
        // 좌우 반전
        emoji.mirrored(true).drawAt(400, 500);
    }
}
```

### 간단한 출력 (디버깅용)
```cpp
void Main()
{
    while (System::Update())
    {
        ClearPrint(); // 이전 출력 지우기
        
        // 간단한 텍스트 출력
        Print << U"Hello";
        Print << U"Score: " << 12345;
        Print << U"Mouse: " << Cursor::Pos();
    }
}
```

---

## 7. 입력 처리

### 키보드 입력
```cpp
void Main()
{
    Vec2 playerPos{ 400, 300 };
    
    while (System::Update())
    {
        double moveSpeed = Scene::DeltaTime() * 200;
        
        // 키 눌림 체크
        if (KeyA.pressed()) playerPos.x -= moveSpeed;
        if (KeyD.pressed()) playerPos.x += moveSpeed;
        if (KeyW.pressed()) playerPos.y -= moveSpeed;
        if (KeyS.pressed()) playerPos.y += moveSpeed;
        
        // 키를 눌렀을 때만 (한 번만)
        if (KeySpace.down())
        {
            Print << U"Space pressed!";
        }
        
        Circle{ playerPos, 30 }.draw();
    }
}
```

### 마우스 입력
```cpp
void Main()
{
    Vec2 circlePos{ 400, 300 };
    
    while (System::Update())
    {
        // 마우스 위치 표시
        Circle{ Cursor::Pos(), 20 }.draw(ColorF{ 1.0, 0.0, 0.0, 0.5 });
        
        // 좌클릭 시 원 이동
        if (MouseL.down())
        {
            circlePos = Cursor::Pos();
        }
        
        // 원과 마우스 충돌 검사
        Circle mainCircle{ circlePos, 50 };
        
        if (mainCircle.mouseOver())
        {
            Cursor::RequestStyle(CursorStyle::Hand);
        }
        
        if (mainCircle.leftClicked())
        {
            Print << U"Circle clicked!";
        }
        
        mainCircle.draw();
    }
}
```

---

## 8. 시간과 애니메이션

### 기본 시간 처리
```cpp
void Main()
{
    double totalTime = 0.0;
    
    while (System::Update())
    {
        // 이전 프레임으로부터 경과 시간 (초)
        double deltaTime = Scene::DeltaTime();
        totalTime += deltaTime;
        
        Print << U"Total: {:.2f}s"_fmt(totalTime);
        Print << U"Delta: {:.4f}s"_fmt(deltaTime);
    }
}
```

### 객체 애니메이션
```cpp
void Main()
{
    Scene::SetBackground(ColorF{ 0.6, 0.8, 0.7 });
    
    const Texture emoji{ U"🐱"_emoji };
    Vec2 pos{ 100, 300 };
    Vec2 velocity{ 100.0, 100.0 }; // 초당 픽셀 이동량
    
    while (System::Update())
    {
        // 위치 업데이트
        pos += velocity * Scene::DeltaTime();
        
        // 경계에서 반사
        if (pos.x < 60 || pos.x > 740) velocity.x *= -1;
        if (pos.y < 60 || pos.y > 540) velocity.y *= -1;
        
        emoji.drawAt(pos);
    }
}
```

### 타이머 구현
```cpp
void Main()
{
    const Font font{ FontMethod::MSDF, 48 };
    double remainingTime = 10.0;
    
    while (System::Update())
    {
        remainingTime -= Scene::DeltaTime();
        
        if (remainingTime > 0)
        {
            font(U"Time: {:.1f}"_fmt(remainingTime))
                .draw(40, Vec2{ 100, 100 });
        }
        else
        {
            font(U"Time's Up!").draw(40, Vec2{ 100, 100 });
            
            if (KeyEnter.down())
            {
                remainingTime = 10.0; // 리셋
            }
        }
    }
}
```

### 랜덤 수 생성
```cpp
void Main()
{
    Scene::SetBackground(ColorF{ 0.6, 0.8, 0.7 });
    
    const Texture emoji{ U"🎯"_emoji };
    Vec2 targetPos{ 400, 300 };
    
    while (System::Update())
    {
        if (MouseL.down())
        {
            // 정수 랜덤 (1~6)
            int dice = Random(1, 6);
            
            // 실수 랜덤 (0.5 이상 2.0 미만)
            double scale = Random(0.5, 2.0);
            
            // 랜덤 위치
            targetPos = Vec2{ Random(100, 700), Random(100, 500) };
            
            Print << U"Dice: {} Scale: {:.2f}"_fmt(dice, scale);
        }
        
        emoji.drawAt(targetPos);
    }
}
```

---

## 9. 샘플 프로젝트

### 간단한 클릭 게임
```cpp
# include <Siv3D.hpp>

Vec2 GetRandomPos()
{
    return Vec2{ Random(60, 740), Random(60, 540) };
}

void Main()
{
    Scene::SetBackground(ColorF{ 0.6, 0.8, 0.7 });
    
    const Font font{ FontMethod::MSDF, 48, Typeface::Bold };
    const Texture targetEmoji{ U"🎯"_emoji };
    
    Circle targetCircle{ 400, 300, 60 };
    int32 score = 0;
    double remainingTime = 15.0;
    
    while (System::Update())
    {
        remainingTime -= Scene::DeltaTime();
        
        if (remainingTime > 0)
        {
            // 마우스 오버 시 손 모양 커서
            if (targetCircle.mouseOver())
            {
                Cursor::RequestStyle(CursorStyle::Hand);
            }
            
            // 클릭 시 점수 획득
            if (targetCircle.leftClicked())
            {
                score += 100;
                targetCircle.center = GetRandomPos();
            }
        }
        else
        {
            // 게임 오버 시 리셋
            if (KeyEnter.down())
            {
                score = 0;
                remainingTime = 15.0;
                targetCircle.center = GetRandomPos();
            }
        }
        
        // 그리기
        targetEmoji.drawAt(targetCircle.center);
        
        font(U"SCORE: {}"_fmt(score))
            .draw(40, Vec2{ 40, 40 }, ColorF{ 0.1 });
            
        if (remainingTime > 0)
        {
            font(U"TIME: {:.1f}"_fmt(remainingTime))
                .draw(40, Arg::topRight(760, 40), ColorF{ 0.1 });
        }
        else
        {
            font(U"TIME'S UP!")
                .drawAt(60, Vec2{ 400, 300 }, ColorF{ 0.1 });
            font(U"Press [Enter] to restart")
                .drawAt(40, Vec2{ 400, 400 }, ColorF{ 0.1 });
        }
    }
}
```

### 패턴 그리기
```cpp
void Main()
{
    Scene::SetBackground(Palette::White);
    
    while (System::Update())
    {
        // 체크무늬 패턴
        for (int32 y = 0; y < 6; ++y)
        {
            for (int32 x = 0; x < 8; ++x)
            {
                if ((x + y) % 2 == 0)
                {
                    Rect{ x * 100, y * 100, 100 }
                        .draw(Palette::Skyblue);
                }
            }
        }
        
        // 동심원 패턴
        for (int32 i = 0; i < 5; ++i)
        {
            Circle{ 400, 300, (i + 1) * 50 }
                .drawFrame(4, HSV{ i * 60, 0.8, 1.0 });
        }
    }
}
```

## 주요 클래스 및 함수 정리

### 기본 타입
- `int32`, `double`, `bool`, `String` (UTF-32)
- `Point` (정수 좌표), `Vec2` (실수 좌표)
- `Array<Type>` (동적 배열)

### 도형 클래스
- `Circle{ x, y, radius }` - 원
- `Rect{ x, y, width, height }` - 사각형
- `Line{ x1, y1, x2, y2 }` - 선

### 색상
- `Palette::색상명` - 기본 팔레트
- `ColorF{ r, g, b, a }` - RGB+알파
- `HSV{ h, s, v, a }` - HSV+알파

### 주요 함수
- `System::Update()` - 메인 루프 업데이트
- `Scene::DeltaTime()` - 프레임 시간
- `Scene::Time()` - 총 경과 시간
- `Cursor::Pos()` - 마우스 위치
- `Random(min, max)` - 랜덤 수 생성

이 가이드를 통해 Siv3D의 기본기를 익히고, 점진적으로 더 복잡한 프로젝트를 만들어 볼 수 있다. 각 섹션의 예제 코드를 직접 실행해보면서 학습하는 것을 권한다. 


---  
  
<br>  
<br>  

---  


## 1. 배열 (Array) 기본 사용법
Siv3D에서 동적 배열은 `Array<Type>` 클래스로 관리됩니다. `std::vector`와 유사하지만 추가 기능들이 제공됩니다.

### 1.1 배열 생성 방법

```cpp
// 빈 배열 생성
Array<int32> emptyArray;

// 초기값으로 배열 생성
Array<int32> numbers = { 10, 20, 30, 40, 50 };

// 크기 × 값으로 배열 생성 (5개 요소를 모두 -5로 초기화)
Array<int32> repeated(5, -5);

// 크기 × 기본값으로 배열 생성 (5개 요소를 모두 0으로 초기화)
Array<int32> defaultValues(5);
```

### 1.2 배열 기본 조작

```cpp
Array<int32> arr = { 1, 2, 3 };

// 크기 확인
size_t size = arr.size();           // 3
bool isEmpty = arr.isEmpty();       // false
if (arr) { /* 배열이 비어있지 않으면 */ }

// 요소 추가 (끝에 추가)
arr << 4;              // { 1, 2, 3, 4 }
arr << 5 << 6;         // { 1, 2, 3, 4, 5, 6 }

// 요소 제거 (끝에서 제거)
arr.pop_back();        // { 1, 2, 3, 4, 5 }

// 모든 요소 제거
arr.clear();           // { }

// 크기 변경
arr.resize(3);         // { 0, 0, 0 }
```

### 1.3 배열 순회

```cpp
Array<int32> numbers = { 10, 20, 30 };

// const 참조로 순회 (읽기 전용)
for (const auto& num : numbers) {
    Print << num;
}

// 참조로 순회 (수정 가능)
for (auto& num : numbers) {
    num *= 2;  // 모든 요소를 2배로
}
```

### 1.4 배열 요소 접근

```cpp
Array<int32> arr = { 10, 20, 30, 40, 50 };

// 인덱스로 접근
int32 first = arr[0];     // 10
int32 last = arr[4];      // 50

// 첫 번째/마지막 요소
int32 front = arr.front();  // 10 (arr[0]과 같음)
int32 back = arr.back();    // 50 (arr[4]와 같음)

// 요소 수정
arr[1] = 200;            // { 10, 200, 30, 40, 50 }
```

### 1.5 유용한 배열 함수들

```cpp
Array<int32> arr = { 3, 1, 4, 1, 5 };

// 정렬
arr.sort();              // { 1, 1, 3, 4, 5 } (오름차순)
arr.rsort();             // { 5, 4, 3, 1, 1 } (내림차순)

// 순서 뒤집기
arr.reverse();           // { 1, 1, 3, 4, 5 }

// 요소 합계
int32 sum = arr.sum();   // 14

// 모든 요소를 같은 값으로 설정
arr.fill(0);             // { 0, 0, 0, 0, 0 }

// 조건에 맞는 요소 제거
arr.remove_if([](int32 x) { return x < 3; });  // 3 미만인 요소 제거
```

---

## 2. 2차원 배열 (Grid) 사용법
`Grid<Type>`은 2차원 배열을 효율적으로 관리하는 클래스입니다. 내부적으로 1차원 배열로 관리되어 성능이 우수합니다.

### 2.1 Grid 생성

```cpp
// 빈 Grid 생성
Grid<int32> emptyGrid;

// 초기값으로 생성
Grid<int32> grid = {
    { 1, 2, 3 },
    { 4, 5, 6 },
    { 7, 8, 9 }
};

// 크기와 초기값으로 생성 (폭 4, 높이 3, 모든 요소를 -1로)
Grid<int32> grid1(Size{4, 3}, -1);
Grid<int32> grid2(4, 3, -1);  // 같은 결과

// 크기만 지정 (기본값 0으로 초기화)
Grid<int32> grid3(4, 3);
```

### 2.2 Grid 크기 확인

```cpp
Grid<int32> grid(4, 3, -1);

size_t width = grid.width();     // 4 (열 수)
size_t height = grid.height();   // 3 (행 수)
Size size = grid.size();         // (4, 3)
bool isEmpty = grid.isEmpty();   // false
```

### 2.3 Grid 요소 접근

```cpp
Grid<int32> grid = {
    { 1, 2, 3 },
    { 4, 5, 6 },
    { 7, 8, 9 }
};

// [행][열] 방식으로 접근
int32 value1 = grid[0][0];        // 1
int32 value2 = grid[2][1];        // 8

// Point로 접근 (주의: [열][행] 순서)
int32 value3 = grid[Point{0, 0}];  // 1
int32 value4 = grid[Point{1, 2}];  // 8

// 요소 수정
grid[1][1] = 50;                   // { {1,2,3}, {4,50,6}, {7,8,9} }
```

### 2.4 Grid 조작

```cpp
Grid<int32> grid(3, 3, 0);

// 행 추가/제거
grid.push_back_row(99);      // 마지막에 행 추가
grid.pop_back_row();         // 마지막 행 제거
grid.insert_row(1, -1);      // 1번 위치에 행 삽입

// 열 추가/제거
grid.push_back_column(88);   // 마지막에 열 추가  
grid.pop_back_column();      // 마지막 열 제거
grid.insert_column(1, -2);   // 1번 위치에 열 삽입

// 크기 변경
grid.resize(5, 5, 1);        // 5×5로 크기 변경, 새 요소는 1로 초기화
```

### 2.5 Grid 시각화 예제

```cpp
void VisualizeGrid(const Grid<int32>& grid, const Font& font) {
    // 배경 그리기 (테두리 역할)
    Rect{grid.size() * 80}.draw(ColorF{0.2});
    
    // 각 셀의 사각형 그리기
    for (int32 y = 0; y < grid.height(); ++y) {
        for (int32 x = 0; x < grid.width(); ++x) {
            const Rect rect{x * 80, y * 80, 80};
            rect.stretched(-1).draw();
        }
    }
    
    // 각 셀의 숫자 그리기
    for (int32 y = 0; y < grid.height(); ++y) {
        for (int32 x = 0; x < grid.width(); ++x) {
            const auto& value = grid[y][x];
            const Rect rect{x * 80, y * 80, 80};
            font(value).drawAt(36, rect.center(), ColorF{0.2});
        }
    }
}
```

---

## 3. 텍스트와 폰트
Siv3D에서 텍스트 출력은 `Font` 클래스를 통해 관리됩니다.

### 3.1 폰트 생성

```cpp
// 기본 폰트 생성 (비트맵 방식, 크기 48)
Font font{48};

// 렌더링 방식 지정
Font fontSDF{FontMethod::SDF, 48};      // SDF 방식
Font fontMSDF{FontMethod::MSDF, 48};    // MSDF 방식 (추천)

// 서체 지정
Font boldFont{FontMethod::MSDF, 48, Typeface::Bold};
Font lightFont{FontMethod::MSDF, 48, Typeface::Light};

// 파일에서 폰트 로드
Font customFont{FontMethod::MSDF, 48, U"fonts/custom.ttf"};
```

### 3.2 텍스트 그리기

```cpp
Font font{FontMethod::MSDF, 48, Typeface::Bold};

// 기본 그리기 (왼쪽 위 좌표 기준)
font(U"Hello, Siv3D!").draw(60, Vec2{40, 40}, ColorF{0.2});

// 중심 좌표 기준으로 그리기
font(U"Center Text").drawAt(60, Vec2{400, 300}, ColorF{0.2});

// 베이스라인 기준으로 그리기 (다른 크기 폰트와 정렬할 때 유용)
font(U"Baseline").drawBase(60, Vec2{40, 400}, ColorF{0.2});

// 사각형 내에 맞춰 그리기
Rect textArea{100, 100, 400, 200};
font(U"Long text...").draw(24, textArea, ColorF{0.2});
```

### 3.3 텍스트 스타일

```cpp
Font font{FontMethod::MSDF, 48, Typeface::Bold};

// 그림자 효과
TextStyle shadowStyle = TextStyle::Shadow(Vec2{2, 2}, ColorF{0.0, 0.5});
font(U"Shadow Text").draw(shadowStyle, 60, Vec2{40, 40}, ColorF{1.0});

// 외곽선 효과
TextStyle outlineStyle = TextStyle::Outline(0.2, ColorF{0.0});
font(U"Outline Text").draw(outlineStyle, 60, Vec2{40, 140}, ColorF{1.0});

// 그림자 + 외곽선
TextStyle combinedStyle = TextStyle::OutlineShadow(
    0.2, ColorF{0.0},      // 외곽선
    Vec2{2, 2}, ColorF{0.0, 0.5}  // 그림자
);
font(U"Combined").draw(combinedStyle, 60, Vec2{40, 240}, ColorF{1.0});
```

### 3.4 문자열 포매팅

```cpp
Font font{FontMethod::MSDF, 48};

// 여러 값 표시
int32 score = 1250;
double time = 45.67;
Point position = {100, 200};

font(U"Score: ", score).draw(40, Vec2{40, 40}, ColorF{0.2});
font(U"Time: {:.2f}s"_fmt(time)).draw(40, Vec2{40, 80}, ColorF{0.2});
font(U"Position: {}"_fmt(position)).draw(40, Vec2{40, 120}, ColorF{0.2});
```

---

## 4. 문자열 포매팅과 파싱

### 4.1 _fmt를 이용한 문자열 포매팅

```cpp
int32 year = 2025, month = 12, day = 31;
double pi = 3.14159265;

// 기본 포매팅
String date = U"{}/{}/{}"_fmt(year, month, day);  // "2025/12/31"

// 인덱스 지정
String dateReverse = U"{2}/{1}/{0}"_fmt(year, month, day);  // "31/12/2025"

// 소수점 자리수 지정
String piStr = U"π ≈ {:.3f}"_fmt(pi);  // "π ≈ 3.142"

// 패딩
String padded = U"{:0>5}"_fmt(42);  // "00042"

// 진법 변환
String hex = U"0x{:08X}"_fmt(255);  // "0x000000FF"
```

### 4.2 문자열 파싱

```cpp
// 기본 파싱
int32 number = Parse<int32>(U"123");
double value = Parse<double>(U"-3.14159");
Point pos = Parse<Point>(U"(10, 20)");

// 안전한 파싱 (실패시 기본값 반환)
int32 safeNum = ParseOr<int32>(U"invalid", -1);  // -1 반환

// Optional 파싱 (실패시 invalid)
Optional<int32> optNum = ParseOpt<int32>(U"123");
if (optNum) {
    Print << U"Parsed: " << *optNum;
}
```

### 4.3 Format 함수

```cpp
// 값을 문자열로 변환
String numStr = Format(12345);        // "12345"
String vecStr = Format(Vec2{11, 22}); // "(11, 22)"

// 천 단위 구분자
String formatted = ThousandsSeparate(1234567);  // "1,234,567"
```

---

## 5. 인터페이스 요소 - 버튼
게임 UI에서 자주 사용되는 버튼을 만들어보겠습니다.

### 5.1 기본 버튼 함수

```cpp
bool Button(const Rect& rect, const Font& font, const String& text, bool enabled = true) {
    // 마우스 오버시 커서 변경
    if (enabled && rect.mouseOver()) {
        Cursor::RequestStyle(CursorStyle::Hand);
    }
    
    const RoundRect roundRect = rect.rounded(6);
    
    // 그림자와 배경 그리기
    roundRect
        .drawShadow(Vec2{2, 2}, 12, 0)
        .draw(ColorF{0.9, 0.8, 0.6});
    
    // 테두리 그리기  
    rect.stretched(-3).rounded(3)
        .drawFrame(2, ColorF{0.4, 0.3, 0.2});
    
    // 텍스트 그리기
    font(text).drawAt(40, rect.center(), ColorF{0.4, 0.3, 0.2});
    
    // 비활성화 상태
    if (!enabled) {
        roundRect.draw(ColorF{0.8, 0.6});
    }
    
    return enabled && rect.leftClicked();
}
```

### 5.2 아이콘이 있는 버튼

```cpp
bool IconButton(const Rect& rect, const Texture& icon, const Font& font, const String& text, bool enabled = true) {
    if (enabled && rect.mouseOver()) {
        Cursor::RequestStyle(CursorStyle::Hand);
    }
    
    const RoundRect roundRect = rect.rounded(6);
    
    // 배경 그리기
    roundRect
        .drawShadow(Vec2{2, 2}, 12, 0)
        .draw(ColorF{0.9, 0.8, 0.6});
    
    rect.stretched(-3).rounded(3)
        .drawFrame(2, ColorF{0.4, 0.3, 0.2});
    
    // 아이콘 그리기
    icon.scaled(0.4).drawAt(rect.x + 60, rect.center().y);
    
    // 텍스트 그리기 (아이콘 옆으로 이동)
    font(text).drawAt(40, rect.center().movedBy(30, 0), ColorF{0.4, 0.3, 0.2});
    
    if (!enabled) {
        roundRect.draw(ColorF{0.8, 0.6});
    }
    
    return enabled && rect.leftClicked();
}
```

### 5.3 버튼 사용 예제

```cpp
void Main() {
    Scene::SetBackground(ColorF{0.6, 0.8, 0.7});
    
    const Font font{FontMethod::MSDF, 48, Typeface::Bold};
    const Texture breadIcon{U"🍞"_emoji};
    const Texture riceIcon{U"🍚"_emoji};
    
    bool gameStarted = false;
    
    while (System::Update()) {
        // 기본 버튼
        if (Button(Rect{100, 200, 200, 60}, font, U"시작", true)) {
            gameStarted = true;
            Print << U"게임 시작!";
        }
        
        // 아이콘 버튼
        if (IconButton(Rect{100, 280, 300, 80}, breadIcon, font, U"빵", gameStarted)) {
            Print << U"빵 선택!";
        }
        
        if (IconButton(Rect{100, 380, 300, 80}, riceIcon, font, U"밥", gameStarted)) {
            Print << U"밥 선택!";
        }
    }
}
```

---

## 6. 게임 프로젝트 예제
실제 게임을 만들어보면서 Siv3D의 기능들을 종합적으로 학습해보겠습니다.

### 6.1 아이템 수집 게임
떨어지는 아이템을 수집하는 간단한 게임을 만들어보겠습니다.

#### 플레이어 클래스

```cpp
struct Player {
    Circle circle{400, 530, 30};
    Texture texture{U"😃"_emoji};
    
    void update(double deltaTime) {
        const double speed = deltaTime * 400.0;
        
        // 키보드 입력 처리
        if (KeyLeft.pressed()) {
            circle.x -= speed;
        }
        if (KeyRight.pressed()) {
            circle.x += speed;
        }
        
        // 화면 밖으로 나가지 않게 제한
        circle.x = Clamp(circle.x, 30.0, 770.0);
    }
    
    void draw() const {
        texture.scaled(0.5).drawAt(circle.center);
    }
};
```

#### 아이템 클래스

```cpp
struct Item {
    Circle circle;
    int32 type;  // 0: 사탕, 1: 케이크
    
    void update(double deltaTime) {
        circle.y += deltaTime * 200.0;  // 아래로 떨어짐
    }
    
    void draw(const Array<Texture>& textures) const {
        // 타입에 따른 회전 효과
        textures[type].scaled(0.5)
            .rotated(circle.y * 0.3_deg)
            .drawAt(circle.center);
    }
};
```

#### 게임 진행 상태

```cpp
struct GameState {
    int32 score = 0;
    double remainingTime = 20.0;
    Array<Item> items;
    
    void addItem() {
        items << Item{
            Circle{Random(30.0, 770.0), -30, 30},
            Random(0, 1)
        };
    }
    
    void updateItems(double deltaTime, const Player& player) {
        // 아이템 업데이트
        for (auto& item : items) {
            item.update(deltaTime);
        }
        
        // 플레이어와 충돌 검사
        for (auto it = items.begin(); it != items.end();) {
            if (player.circle.intersects(it->circle)) {
                score += (it->type == 0) ? 10 : 50;  // 사탕: 10점, 케이크: 50점
                it = items.erase(it);
            } else {
                ++it;
            }
        }
        
        // 바닥에 떨어진 아이템 제거
        items.remove_if([](const Item& item) {
            return item.circle.y > 580;
        });
    }
};
```

#### 메인 게임 루프

```cpp
void Main() {
    Scene::SetBackground(ColorF{0.6, 0.8, 0.7});
    
    const Font font{FontMethod::MSDF, 48, Typeface::Bold};
    const Array<Texture> itemTextures = {
        Texture{U"🍬"_emoji},  // 사탕
        Texture{U"🍰"_emoji}   // 케이크
    };
    
    Player player;
    GameState gameState;
    
    double itemSpawnTimer = 0.0;
    const double itemSpawnInterval = 0.8;
    
    while (System::Update()) {
        const double deltaTime = Scene::DeltaTime();
        
        // 게임이 진행 중일 때만
        if (gameState.remainingTime > 0.0) {
            // 시간 감소
            gameState.remainingTime = Max(gameState.remainingTime - deltaTime, 0.0);
            
            // 아이템 생성
            itemSpawnTimer += deltaTime;
            if (itemSpawnTimer >= itemSpawnInterval) {
                gameState.addItem();
                itemSpawnTimer -= itemSpawnInterval;
            }
            
            // 업데이트
            player.update(deltaTime);
            gameState.updateItems(deltaTime, player);
        } else {
            // 게임 종료 시 모든 아이템 제거
            gameState.items.clear();
        }
        
        // 배경 그리기
        Rect{0, 0, 800, 550}.draw(
            Arg::top(0.3, 0.6, 1.0), 
            Arg::bottom(0.6, 0.9, 1.0)
        );
        Rect{0, 550, 800, 50}.draw(ColorF{0.3, 0.6, 0.3});
        
        // 게임 오브젝트 그리기
        player.draw();
        for (const auto& item : gameState.items) {
            item.draw(itemTextures);
        }
        
        // UI 그리기
        font(U"SCORE: {}"_fmt(gameState.score)).draw(30, Vec2{20, 20});
        font(U"TIME: {:.0f}"_fmt(gameState.remainingTime))
            .draw(30, Arg::topRight(780, 20));
        
        if (gameState.remainingTime <= 0.0) {
            font(U"TIME'S UP!").drawAt(80, Vec2{400, 270}, ColorF{0.3});
        }
    }
}
```

### 6.2 쿠키 클리커 게임
클릭하여 쿠키를 모으고 생산 시설을 구매하는 게임입니다.

#### 게임 정보 클래스

```cpp
struct CookieGame {
    double cookies = 0.0;
    int32 farmCount = 0;
    int32 factoryCount = 0;
    
    // 초당 쿠키 생산량
    int32 getCPS() const {
        return farmCount + factoryCount * 10;
    }
    
    // 농장 가격 (구매할 때마다 증가)
    int32 getFarmCost() const {
        return 10 + farmCount * 10;
    }
    
    // 공장 가격
    int32 getFactoryCost() const {
        return 100 + factoryCount * 100;
    }
    
    void update(double deltaTime) {
        // 0.1초마다 쿠키 생산
        static double accumTime = 0.0;
        accumTime += deltaTime;
        
        if (accumTime >= 0.1) {
            cookies += getCPS() * 0.1;
            accumTime -= 0.1;
        }
    }
};
```

#### 쿠키 클리커 메인

```cpp
void Main() {
    const Font font{FontMethod::MSDF, 48, Typeface::Bold};
    const Texture cookieEmoji{U"🍪"_emoji};
    const Texture farmEmoji{U"🌾"_emoji};
    const Texture factoryEmoji{U"🏭"_emoji};
    
    const Circle cookieCircle{170, 300, 90};
    CookieGame game;
    double cookieScale = 1.5;
    
    while (System::Update()) {
        const double deltaTime = Scene::DeltaTime();
        
        // 게임 업데이트
        game.update(deltaTime);
        
        // 쿠키 클릭 처리
        if (cookieCircle.mouseOver()) {
            Cursor::RequestStyle(CursorStyle::Hand);
        }
        
        if (cookieCircle.leftClicked()) {
            game.cookies += 1;
            cookieScale = 1.3;  // 클릭 효과
        }
        
        // 쿠키 크기 복원
        cookieScale = Min(cookieScale + deltaTime, 1.5);
        
        // 배경
        Rect{800, 600}.draw(
            Arg::top(0.6, 0.5, 0.3), 
            Arg::bottom(0.2, 0.5, 0.3)
        );
        
        // 쿠키 정보 표시
        font(U"{:.0f}"_fmt(game.cookies)).drawAt(60, Vec2{170, 100});
        font(U"{} CPS"_fmt(game.getCPS())).drawAt(24, Vec2{170, 160});
        
        // 쿠키 그리기
        cookieEmoji.scaled(cookieScale).drawAt(cookieCircle.center);
        
        // 농장 구매 버튼
        if (IconButton(
            Rect{340, 40, 420, 100}, 
            farmEmoji, font, 
            U"Cookie Farm\nC{} / 1 CPS"_fmt(game.getFarmCost()),
            game.getFarmCost() <= game.cookies
        )) {
            game.cookies -= game.getFarmCost();
            game.farmCount++;
        }
        
        // 공장 구매 버튼
        if (IconButton(
            Rect{340, 160, 420, 100}, 
            factoryEmoji, font,
            U"Cookie Factory\nC{} / 10 CPS"_fmt(game.getFactoryCost()),
            game.getFactoryCost() <= game.cookies
        )) {
            game.cookies -= game.getFactoryCost();
            game.factoryCount++;
        }
    }
}
```

---

## 마무리

이 가이드를 통해 Siv3D의 기본적인 기능들을 학습했습니다:

1. **배열과 2차원 배열**: 게임 데이터 관리의 기초
2. **텍스트와 폰트**: 게임 UI와 정보 표시
3. **문자열 처리**: 데이터 변환과 표시
4. **인터페이스**: 사용자와의 상호작용
5. **게임 프로젝트**: 실제 게임 개발 경험

서버 개발 경험을 바탕으로 클라이언트 개발에도 도전해보세요. Siv3D는 프로토타이핑부터 완성된 게임까지 다양한 용도로 활용할 수 있는 강력한 프레임워크입니다.

다음 단계로는 물리 시뮬레이션, 네트워크 통신, 오디오 처리 등의 고급 기능들을 학습해보시기 바랍니다.
  
---

## 7. 인터랙티브 프로그램 만들기

### 7.1 클릭으로 원 배치하기
마우스 클릭으로 원을 생성하는 간단한 예제다.

```cpp
Array<Circle> circles;

while (System::Update())
{
    if (MouseL.down())
    {
        // 클릭한 위치에 랜덤 크기의 원 추가
        circles << Circle{ Cursor::Pos(), Random(10.0, 30.0) };
    }
    
    // 모든 원 그리기
    for (const auto& circle : circles)
    {
        circle.draw(HSV{ circle.center.x, 0.8, 0.9 });
    }
}
```

### 7.2 그리드 타일 색칠하기
클릭으로 격자의 색을 바꾸는 예제다.

```cpp
// 8x6 크기의 2D 배열, 모든 요소를 0으로 초기화
Grid<int32> grid(8, 6);

while (System::Update())
{
    if (MouseL.down())
    {
        for (int32 y = 0; y < grid.height(); ++y)
        {
            for (int32 x = 0; x < grid.width(); ++x)
            {
                const RectF rect{ (x * 100), (y * 100), 100 };
                
                if (rect.mouseOver())
                {
                    // 클릭할 때마다 값 변경: 0 → 1 → 2 → 3 → 0...
                    ++grid[y][x] %= 4;
                }
            }
        }
    }
    
    // 격자 그리기
    for (int32 y = 0; y < grid.height(); ++y)
    {
        for (int32 x = 0; x < grid.width(); ++x)
        {
            const RectF rect{ (x * 100), (y * 100), 100 };
            const ColorF color{ (3 - grid[y][x]) / 3.0 };
            rect.stretched(-1).draw(color);
        }
    }
}
```

### 7.3 구조체를 활용한 공 애니메이션
여러 공이 벽에서 튕기는 애니메이션이다.

```cpp
struct Ball
{
    Vec2 pos;      // 위치
    Vec2 velocity; // 속도
};

Array<Ball> balls;
const double ballRadius = 20.0;

// 초기 공 생성
for (int32 i = 0; i < 5; ++i)
{
    balls << Ball{ 
        RandomVec2(Scene::Rect().stretched(-ballRadius)), 
        RandomVec2(200) 
    };
}

while (System::Update())
{
    // 공 위치 업데이트
    for (auto& ball : balls)
    {
        ball.pos += (ball.velocity * Scene::DeltaTime());
        
        // 벽 충돌 처리
        if ((ball.pos.x <= ballRadius) || (800 <= (ball.pos.x + ballRadius)))
        {
            ball.velocity.x *= -1.0;
        }
        
        if ((ball.pos.y <= ballRadius) || (600 <= (ball.pos.y + ballRadius)))
        {
            ball.velocity.y *= -1.0;
        }
    }
    
    // 공 그리기
    for (const auto& ball : balls)
    {
        Circle{ ball.pos, ballRadius }.draw().drawFrame(2, 0, ColorF{ 0.2 });
    }
}
```

### 7.4 부드러운 움직임 구현
관성이 있는 부드러운 움직임을 만들어보자.

```cpp
struct SmoothedVec2
{
    Vec2 current{ 400, 300 };  // 현재 위치
    Vec2 target = current;     // 목표 위치
    Vec2 velocity{ 0, 0 };     // 속도

    void update()
    {
        // 부드럽게 목표 위치로 이동
        current = Math::SmoothDamp(current, target, velocity, 0.3);
    }
};

SmoothedVec2 pos;
const double speed = 300.0;

while (System::Update())
{
    const double deltaTime = Scene::DeltaTime();
    
    // 키보드 입력으로 목표 위치 조정
    if (KeyLeft.pressed())  pos.target.x -= (speed * deltaTime);
    if (KeyRight.pressed()) pos.target.x += (speed * deltaTime);
    if (KeyUp.pressed())    pos.target.y -= (speed * deltaTime);
    if (KeyDown.pressed())  pos.target.y += (speed * deltaTime);
    
    pos.update();
    
    RectF{ Arg::center = pos.current, 120, 80 }.draw(ColorF{ 0.2, 0.6, 0.9 });
}
```

### 7.5 메시지 박스 시스템
게임에서 자주 사용하는 대화 시스템이다.

```cpp
struct DialogBox
{
    Rect rect{ 40, 440, 720, 120 };
    Array<String> messages;
    size_t messageIndex = 0;
    Stopwatch stopwatch;
    
    bool isFinished() const
    {
        const int32 count = Max(((stopwatch.ms() - 200) / 24), 0);
        return (static_cast<int32>(messages[messageIndex].length()) <= count);
    }
    
    void update()
    {
        if (isFinished() && (rect.leftClicked() || KeySpace.down()))
        {
            ++messageIndex %= messages.size();
            stopwatch.restart();
        }
    }
    
    void draw(const Font& font) const
    {
        const int32 count = Max(((stopwatch.ms() - 200) / 24), 0);
        
        rect.rounded(10).drawShadow(Vec2{ 1, 1 }, 8).draw().drawFrame(2, ColorF{ 0.4 });
        
        font(messages[messageIndex].substr(0, count))
            .draw(28, rect.stretched(-36, -20), ColorF{ 0.2 });
        
        if (isFinished())
        {
            Triangle{ rect.br().movedBy(-30, -30), 20, 180_deg }
                .draw(ColorF{ 0.2, Periodic::Sine0_1(2.0s) });
        }
    }
};
```

---

## 8. 실전 활용 팁

### 8.1 색상 다루기
Siv3D는 다양한 색상 표현 방식을 제공한다.

```cpp
// RGB 색상
ColorF redColor{ 1.0, 0.0, 0.0 };

// HSV 색상 (색상, 채도, 명도)
HSV blueHSV{ 240, 1.0, 1.0 };

// 미리 정의된 색상
Palette::Red.draw();
Palette::Skyblue.draw();

// 색상 변환
ColorF fromHSV = HSV{ 120, 0.8, 0.9 }; // HSV를 ColorF로 자동 변환
```

### 8.2 시간 다루기
```cpp
// 델타 타임 (이전 프레임과의 시간 차)
double dt = Scene::DeltaTime();

// 주기적인 값들
double sine = Periodic::Sine0_1(2.0s); // 2초 주기로 0~1 사인파
double triangle = Periodic::Triangle0_1(1.0s); // 1초 주기로 0~1 삼각파

// 스톱워치
Stopwatch stopwatch{ StartImmediately::Yes };
double elapsed = stopwatch.sF(); // 경과 시간(초)
```

### 8.3 충돌 검사 최적화
많은 객체의 충돌을 효율적으로 처리하는 방법이다.

```cpp
Array<Circle> enemies;

// Iterator를 사용한 안전한 제거
for (auto it = enemies.begin(); it != enemies.end();)
{
    if (playerCircle.intersects(*it))
    {
        Print << U"Hit!";
        it = enemies.erase(it); // 제거 후 다음 iterator 반환
    }
    else
    {
        ++it;
    }
}
```

### 8.4 파일 입출력
```cpp
// 텍스트 파일 읽기
TextReader reader{ U"data.txt" };
if (reader)
{
    String line;
    while (reader.readLine(line))
    {
        Print << line;
    }
}

// 텍스트 파일 쓰기
TextWriter writer{ U"output.txt" };
if (writer)
{
    writer.writeln(U"Hello, Siv3D!");
    writer.writeln(U"Score: {}"_fmt(score));
}
```

### 8.5 마우스와 키보드 입력
```cpp
// 마우스
if (MouseL.down())       Print << U"왼쪽 버튼 누름";
if (MouseL.pressed())    Print << U"왼쪽 버튼 누르고 있음";
if (MouseL.up())         Print << U"왼쪽 버튼 뗌";

Point mousePos = Cursor::Pos();
Vec2 mouseDelta = Cursor::DeltaF(); // 이전 프레임과의 마우스 이동량

// 키보드
if (KeySpace.down())     Print << U"스페이스 키 누름";
if (KeyA.pressed())      Print << U"A 키 누르고 있음";
if (KeyEnter.up())       Print << U"엔터 키 뗌";
```

---

## 9. 자주 하는 실수와 해결법

### 9.1 그리기 순서
나중에 그린 것이 위에 표시된다는 점을 기억해야 한다.

```cpp
// 잘못된 예 - 배경이 모든 것 위에 그려짐
circle.draw();
background.draw(); // 이렇게 하면 원이 보이지 않는다

// 올바른 예
background.draw();
circle.draw(); // 원이 배경 위에 그려진다
```

### 9.2 좌표계 이해
Siv3D는 왼쪽 상단이 (0, 0)인 좌표계를 사용한다.

```cpp
// 화면 중앙
Vec2 center = Scene::Center(); // 일반적으로 (400, 300)

// 화면 크기
Size windowSize = Scene::Size(); // 일반적으로 (800, 600)
```

### 9.3 메모리 관리
큰 객체나 복잡한 폴리곤은 반복해서 생성하지 않는 것이 좋다.

```cpp
// 나쁜 예 - 매 프레임마다 복잡한 폴리곤 생성
while (System::Update())
{
    Polygon complexShape = Shape2D::Star(100).asPolygon(); // 비효율적
    complexShape.draw();
}

// 좋은 예 - 한 번만 생성
const Polygon complexShape = Shape2D::Star(100).asPolygon();
while (System::Update())
{
    complexShape.draw();
}
```

### 9.4 문자열 리터럴
Siv3D에서는 유니코드 문자열을 위해 `U` 접두사를 사용한다.

```cpp
String text = U"안녕하세요"; // 유니코드 문자열
font(text).draw(Vec2{ 100, 100 });
```

---

## 10. 추천 학습 순서

1. **기본 도형 그리기** - 원, 사각형, 선부터 시작
2. **마우스/키보드 입력** - 사용자 상호작용 만들기
3. **충돌 검사** - 게임의 기본 로직 구현
4. **GUI 컴포넌트** - 버튼, 슬라이더로 인터페이스 만들기
5. **구조체와 배열** - 복잡한 객체 관리
6. **애니메이션** - 시간에 따른 변화 만들기
7. **고급 기능** - 파일 입출력, 사운드, 이미지 처리

각 단계를 충분히 연습한 후 다음 단계로 넘어가는 것을 추천한다. 작은 프로젝트를 만들어보면서 배운 내용을 응용해보는 것이 가장 효과적인 학습 방법이다.


---  
  
<br>  
<br>  

---  
  

## 1. 유용한 함수들

### 1.1 기본 수학 함수

#### 최솟값과 최댓값 구하기
```cpp
// 두 값 중 더 작은/큰 값 반환
Print << Min(10, 20);    // 10
Print << Max(10, 20);    // 20

// 타입이 다를 때는 명시적 지정
String s = U"Hello";
Print << Min<size_t>(s.size(), 4);  // 4
```

#### 값을 범위 내로 제한하기
```cpp
// Clamp(값, 최솟값, 최댓값)
Print << Clamp(-20, 0, 100);  // 0
Print << Clamp(50, 0, 100);   // 50
Print << Clamp(120, 0, 100);  // 100
```

#### 범위 확인하기
```cpp
// 값이 범위 내에 있는지 확인
Print << InRange(50, 0, 100);   // true
Print << InRange(120, 0, 100);  // false
```

#### 홀수/짝수 확인하기
```cpp
Print << IsOdd(3);   // true (홀수)
Print << IsEven(4);  // true (짝수)
```

### 1.2 반복문 간소화

#### 인덱스와 함께 반복하기
```cpp
const Array<String> animals = { U"cat", U"dog", U"bird" };

// 인덱스와 값을 함께 가져오기
for (auto&& [i, animal] : Indexed(animals))
{
    Print << U"{}: {}"_fmt(i, animal);
    // 출력: 0: cat, 1: dog, 2: bird
}
```

#### 간단한 범위 반복
```cpp
// 0부터 N-1까지 반복
for (auto i : step(3))
{
    Print << i;  // 0, 1, 2
}

// from부터 to까지 반복
for (auto i : Range(5, 10))
{
    Print << i;  // 5, 6, 7, 8, 9, 10
}
```

### 1.3 수학 상수

```cpp
Print << Math::Pi;     // π (파이)
Print << Math::Phi;    // φ (황금비)
Print << Math::E;      // e (자연상수)
Print << Math::Sqrt2;  // √2
```

### 1.4 각도 표현

```cpp
// 도(degree)를 라디안으로
Print << 90_deg;    // 90도를 라디안으로 변환
Print << 180_deg;   // 180도를 라디안으로 변환

// π 단위로 표현
Print << 0.5_pi;    // π/2

// τ(tau) 단위로 표현  
Print << 0.5_tau;   // τ/2 (π와 동일)
```

---

## 2. 시간과 모션 처리

### 2.1 시간 측정 기초

#### 기본 시간 함수
```cpp
// 프로그램 시작부터 경과 시간 (초)
double currentTime = Scene::Time();

// 이전 프레임부터 경과 시간 (초)
double deltaTime = Scene::DeltaTime();
```

#### 시간 기반 애니메이션
```cpp
void Main()
{
    while (System::Update())
    {
        const double t = Scene::Time();
        
        // 시간에 따라 위치가 변하는 사각형
        RectF{ (t * 50), 40, 40, 200 }.draw(ColorF{ 0.2 });
        
        // 시간에 따라 크기가 변하는 원
        Circle{ 200, 400, (t * 20) }.draw(Palette::Seagreen);
    }
}
```

### 2.2 정기적인 이벤트 처리

```cpp
void Main()
{
    const double interval = 0.5;  // 0.5초 간격
    double accumulatedTime = 0.0;
    int32 count = 0;

    while (System::Update())
    {
        accumulatedTime += Scene::DeltaTime();
        
        // 0.5초마다 카운트 증가
        while (interval <= accumulatedTime)
        {
            Print << ++count;
            accumulatedTime -= interval;
        }
    }
}
```

### 2.3 Stopwatch 사용하기

```cpp
void Main()
{
    // 즉시 측정 시작
    Stopwatch stopwatch{ StartImmediately::Yes };
    
    while (System::Update())
    {
        // 좌클릭으로 일시정지/재개
        if (MouseL.down())
        {
            if (stopwatch.isPaused())
                stopwatch.resume();
            else
                stopwatch.pause();
        }
        
        // 우클릭으로 재시작
        if (MouseR.down())
        {
            stopwatch.restart();
        }
        
        // 경과 시간에 따른 진행 바
        RectF{ 0, 200, (stopwatch.sF() * 100), 200 }.draw();
        
        // 시간 표시 (mm:ss.xx 형식)
        font(stopwatch.format(U"mm:ss.xx")).draw(40, Vec2{ 40, 40 });
    }
}
```

### 2.4 Timer 사용하기

```cpp
void Main()
{
    // 10초 타이머 시작
    Timer timer{ 10s, StartImmediately::Yes };
    
    while (System::Update())
    {
        // 남은 시간 비율로 진행 바 그리기
        RectF{ 0, 200, (timer.progress1_0() * 800), 200 }.draw();
        
        if (timer.reachedZero())
        {
            // 시간 종료
            font(U"Time's up!").draw(40, Vec2{ 40, 40 }, Palette::Red);
        }
        else
        {
            // 남은 시간 표시
            font(timer.format(U"mm:ss.xx")).draw(40, Vec2{ 40, 40 });
        }
    }
}
```

### 2.5 주기적 모션 함수

```cpp
void Main()
{
    while (System::Update())
    {
        // 다양한 주기 함수들 (모두 2초 주기)
        const double square = Periodic::Square0_1(2s);    // 사각파 (0↔1)
        const double triangle = Periodic::Triangle0_1(2s); // 삼각파 (0↔1)
        const double sine = Periodic::Sine0_1(2s);        // 사인파 (0↔1)
        const double sawtooth = Periodic::Sawtooth0_1(2s); // 톱니파 (0→1)
        
        // 각각 다른 높이에서 좌우 이동
        Circle{ (100 + square * 600), 100, 20 }.draw();
        Circle{ (100 + triangle * 600), 200, 20 }.draw();
        Circle{ (100 + sine * 600), 300, 20 }.draw();
        Circle{ (100 + sawtooth * 600), 400, 20 }.draw();
    }
}
```

### 2.6 부드러운 전환 (Transition)

```cpp
void Main()
{
    // 1초에 걸쳐 증가, 0.25초에 걸쳐 감소
    Transition transition{ 1.0s, 0.25s };
    
    while (System::Update())
    {
        // 마우스 좌클릭 시 증가, 아닐 때 감소
        transition.update(MouseL.pressed());
        
        // 전환 값에 따른 진행 바
        RectF{ 0, 200, (transition.value() * 800), 200 }.draw();
    }
}
```

### 2.7 선형 보간 (Linear Interpolation)

```cpp
void Main()
{
    const ColorF color0{ 0.1, 0.5, 1.0 };
    const ColorF color1{ 0.1, 1.0, 0.5 };
    
    const Circle circle0{ 100, 200, 20 };
    const Circle circle1{ 700, 200, 40 };
    
    while (System::Update())
    {
        // 0.0~1.0 사이를 3초 주기로 변화
        const double t = Periodic::Triangle0_1(3s);
        
        // 색상 보간
        RectF{ 200, 50, 400, 80 }.draw(color0.lerp(color1, t));
        
        // 원 보간 (위치와 크기 모두)
        circle0.lerp(circle1, t).draw(ColorF{ 0.2 });
    }
}
```

---

## 3. 텍스처 그리기

### 3.1 텍스처 생성 방법

#### 이모지에서 텍스처 생성
```cpp
// 이모지를 텍스처로 변환 (3700개 이상의 이모지 지원)
const Texture emoji{ U"🐱"_emoji };
const Texture gift{ U"🎁"_emoji };
```

#### 아이콘에서 텍스처 생성
```cpp
// 16진수 코드로 아이콘 생성 (7000개 이상의 아이콘 지원)
const Texture icon1{ 0xF0493_icon, 100 };  // 크기 100
const Texture icon2{ 0xF0787_icon, 100 };
```

#### 이미지 파일에서 텍스처 생성
```cpp
// 다양한 형식 지원 (PNG, JPEG, BMP, SVG, GIF 등)
const Texture windmill{ U"example/windmill.png" };
const Texture character{ U"example/siv3d-kun.png" };
```

### 3.2 텍스처 그리기

#### 기본 그리기
```cpp
void Main()
{
    const Texture texture{ U"🐱"_emoji };
    
    while (System::Update())
    {
        // 좌상단 좌표 지정
        texture.draw(100, 100);
        
        // 중심 좌표 지정
        texture.drawAt(400, 300);
        
        // 색상 곱셈으로 색조 변경
        texture.draw(200, 200, ColorF{ 0.5, 0.0, 0.0 });  // 빨간색 톤
        
        // 투명도 조절
        texture.draw(300, 200, ColorF{ 1.0, 0.5 });  // 반투명
    }
}
```

#### 다양한 좌표 기준점
```cpp
// 9가지 기준점으로 그리기 가능
texture.draw(Arg::topLeft = Vec2{ 0, 0 });        // 좌상단 기준
texture.draw(Arg::topCenter = Vec2{ 400, 0 });    // 상단 중앙 기준
texture.draw(Arg::topRight = Vec2{ 800, 0 });     // 우상단 기준
texture.draw(Arg::center = Vec2{ 400, 300 });     // 중앙 기준
texture.draw(Arg::bottomLeft = Vec2{ 0, 600 });   // 좌하단 기준
```

### 3.3 텍스처 변형

#### 크기 조절
```cpp
// 배율 조정
texture.scaled(0.5).draw(100, 100);      // 절반 크기
texture.scaled(2.0).draw(300, 100);      // 두 배 크기
texture.scaled(0.8, 0.5).draw(500, 100); // 가로 0.8배, 세로 0.5배

// 픽셀 단위 크기 조정
texture.resized(64, 64).draw(100, 200);  // 64x64 픽셀
texture.resized(128).draw(200, 200);     // 긴 변을 128픽셀로
```

#### 회전
```cpp
void Main()
{
    const Texture texture{ U"🎁"_emoji };
    
    while (System::Update())
    {
        const double angle = (Scene::Time() * 90_deg);
        
        // 중심 기준 회전
        texture.rotated(angle).drawAt(200, 300);
        
        // 특정 점 기준 회전
        texture.rotatedAt(Vec2{ 58, 13 }, angle).drawAt(600, 300);
    }
}
```

#### 뒤집기
```cpp
// 좌우 뒤집기
texture.mirrored().drawAt(300, 100);

// 상하 뒤집기  
texture.flipped().drawAt(300, 300);

// 조건부 뒤집기
bool shouldFlip = true;
texture.mirrored(shouldFlip).drawAt(500, 100);
```

### 3.4 부분 텍스처 그리기

```cpp
void Main()
{
    const Texture texture{ U"example/windmill.png" };
    
    while (System::Update())
    {
        // 픽셀 좌표로 부분 추출 (x, y, width, height)
        texture(250, 100, 200, 150).draw(40, 40);
        
        // UV 좌표로 부분 추출 (0.0~1.0 범위)
        texture.uv(0.5, 0.0, 0.5, 0.75).drawAt(400, 300);
    }
}
```

### 3.5 고급 그리기

#### 사각형에 맞춰 그리기
```cpp
// 비율 유지하며 사각형에 맞춤
const Rect rect{ 50, 100, 320, 200 };
rect.drawFrame(0, 4, Palette::Green);  // 경계선
texture.fitted(rect.size).drawAt(rect.center());
```

#### 타일 패턴 그리기
```cpp
void Main()
{
    const Texture pattern{ U"🌳"_emoji };
    
    while (System::Update())
    {
        // Repeat 샘플러 상태 설정
        const ScopedRenderStates2D sampler{ SamplerState::RepeatLinear };
        
        // 타일 패턴으로 그리기
        pattern.mapped(300, 400).draw();
        pattern.repeated(2.5, 4).draw(400, 0);
    }
}
```

#### 둥근 모서리
```cpp
// 둥근 모서리로 그리기
texture.rounded(20).drawAt(400, 300);
```

### 3.6 복합 변형

```cpp
void Main()
{
    const Texture texture{ U"example/windmill.png" };
    
    while (System::Update())
    {
        // 여러 변형을 연결해서 적용
        texture
            .uv(0.5, 0.5, 0.5, 0.5)  // 부분 추출
            .scaled(2.0)              // 크기 조절
            .rotated(20_deg)          // 회전
            .draw(20, 20);            // 그리기
    }
}
```

---

## 4. 문자열 처리

### 4.1 String 기본 사용법

#### 문자열 생성
```cpp
// 빈 문자열 생성
String s1;

// 문자열 리터럴에서 생성
String s2 = U"Siv3D";

// 특정 문자 반복
String s3(5, U'A');  // "AAAAA"

// 한글도 UTF-32로 완벽 지원
String s4 = U"안녕하세요, Siv3D!";
```

#### 문자열 길이와 확인
```cpp
String s = U"Hello";
Print << s.size();      // 5 (문자 개수)
Print << s.isEmpty();   // false (비어있지 않음)

// 빈 문자열 확인 방법
if (s) {
    Print << U"문자열이 있습니다";
}
```

### 4.2 문자열 조작

#### 요소 추가/제거
```cpp
String s;
s << U'H' << U'e' << U'l' << U'l' << U'o';  // "Hello"

s.pop_back();     // 마지막 문자 제거 ("Hell")
s.push_back(U'o'); // 마지막에 문자 추가 ("Hello")

s += U" World";   // 문자열 추가 ("Hello World")
s.clear();        // 모든 문자 삭제
```

#### 문자 접근
```cpp
String s = U"Siv3D";

Print << s[0];      // 'S' (첫 번째 문자)
Print << s[4];      // 'D' (다섯 번째 문자)
Print << s.front(); // 'S' (첫 번째 문자)
Print << s.back();  // 'D' (마지막 문자)

s[3] = U'4';        // 문자 변경 ("Siv4D")
```

### 4.3 문자열 검색과 확인

```cpp
String s = U"Hello, Siv3D!";

// 문자/문자열 포함 확인
Print << s.contains(U'S');        // true
Print << s.contains(U"3D");       // true
Print << s.contains(U"Hi");       // false

// 시작/끝 확인
Print << s.starts_with(U"Hello"); // true
Print << s.ends_with(U"3D!");     // true
Print << s.starts_with(U"Hi");    // false
```

### 4.4 문자열 변환

#### 대소문자 변환
```cpp
String s = U"Hello, Siv3D!";

Print << s.lowercased();  // "hello, siv3d!" (소문자)
Print << s.uppercased();  // "HELLO, SIV3D!" (대문자)
```

#### 문자열 뒤집기와 섞기
```cpp
String s = U"Hello";

Print << s.reversed();    // "olleH" (뒤집기)
Print << s.shuffled();    // "leHol" (무작위 섞기, 실행할 때마다 다름)

s.reverse();              // 원본 문자열 뒤집기
s.shuffle();              // 원본 문자열 무작위 섞기
```

### 4.5 문자열 가공

#### 부분 문자열
```cpp
String s = U"Hello, Siv3D!";

Print << s.substr(0, 5);    // "Hello" (0번째부터 5글자)
Print << s.substr(7, 3);    // "Siv" (7번째부터 3글자)
Print << s.substr(7);       // "Siv3D!" (7번째부터 끝까지)
```

#### 문자열 교체
```cpp
String s = U"Hello, Siv3D!";

// 새 문자열 생성
Print << s.replaced(U"Siv3D", U"C++");  // "Hello, C++!"

// 원본 문자열 변경
s.replace(U'!', U'?');      // "Hello, Siv3D?"
```

#### 공백 제거
```cpp
String s = U"  Hello, Siv3D!   ";

s.trim();                   // 앞뒤 공백 제거
Print << s;                 // "Hello, Siv3D!"

// 새 문자열로 공백 제거
String s2 = U"\n\n Siv3D  \n\n\n";
Print << s2.trimmed();      // "Siv3D"
```

### 4.6 문자열 분할

```cpp
String s = U"red,green,blue";

Array<String> colors = s.split(U',');
Print << colors;            // {red, green, blue}

// 숫자 문자열을 정수 배열로 변환
String numbers = U"1, 2, 3, 4, 5";
Array<int32> values = numbers.split(U',').map(Parse<int32>);
Print << values;            // {1, 2, 3, 4, 5}
```

### 4.7 다른 문자열 타입과의 변환

```cpp
String s = U"안녕하세요, Siv3D!";

// 다른 문자열 타입으로 변환
std::string narrow = s.narrow();        // 환경 의존 인코딩
std::string utf8 = s.toUTF8();         // UTF-8
std::wstring wide = s.toWstr();         // wstring
std::u16string utf16 = s.toUTF16();     // UTF-16

// 다른 타입에서 String으로 변환
String from_narrow = Unicode::Widen("Hello");
String from_utf8 = Unicode::FromUTF8("Hello");
String from_wide = Unicode::FromWstring(L"Hello");
```

---

## 5. 비디오 그리기

### 5.1 기본 비디오 재생

```cpp
void Main()
{
    Scene::SetBackground(ColorF{ 0.6, 0.8, 0.7 });
    
    // 비디오 로드 (Loop::Yes = 반복, Loop::No = 한번만)
    const VideoTexture video{ U"example/video/river.mp4", Loop::Yes };
    
    while (System::Update())
    {
        // 재생 위치 / 총 길이 표시
        Print << U"{:.2f}/{:.2f}"_fmt(video.posSec(), video.lengthSec());
        
        // 비디오 시간 진행 (기본값: Scene::DeltaTime() 초)
        video.advance();
        
        // 비디오 그리기 (텍스처처럼 사용 가능)
        video.scaled(0.5).draw();
        
        // 회전하며 마우스 위치에 그리기
        video.scaled(0.5)
             .rotated(Scene::Time() * 30_deg)
             .drawAt(Cursor::Pos());
    }
}
```

### 5.2 오디오가 있는 비디오 재생

```cpp
void Main()
{
    const FilePath path = U"your_video_with_audio.mp4";
    
    // 비디오와 오디오 동시 로드
    const VideoTexture video{ path, Loop::Yes };
    const Audio audio{ Audio::Stream, path, Loop::Yes };
    
    // 오디오 재생 시작
    audio.play();
    
    while (System::Update())
    {
        const double videoTime = video.posSec();
        const double audioTime = audio.posSec();
        
        // 비디오와 오디오 동기화 (차이가 0.1초 이상 날 때)
        if (0.1 < AbsDiff(audioTime, videoTime))
        {
            audio.seekTime(videoTime);
        }
        
        video.advance();
        video.scaled(0.5).draw();
    }
}
```

### 5.3 GIF 애니메이션 그리기

```cpp
void Main()
{
    Scene::SetBackground(ColorF{ 0.6, 0.8, 0.7 });
    
    // GIF 파일 열기
    const AnimatedGIFReader gif{ U"example/gif/test.gif" };
    
    // 각 프레임의 이미지와 지연시간 로드
    Array<Image> images;
    Array<int32> delays;  // 각 프레임의 지연시간 (밀리초)
    gif.read(images, delays);
    
    // 이미지를 텍스처로 변환
    const Array<Texture> textures = images.map([](const Image& image) {
        return Texture{ image };
    });
    
    // 메모리 절약을 위해 이미지 데이터 삭제
    images.clear();
    
    while (System::Update())
    {
        Print << textures.size() << U" frames";
        Print << U"delays: " << delays;
        
        // 현재 시간을 기준으로 어떤 프레임을 그릴지 결정
        const double t = Scene::Time();
        const size_t frameIndex = AnimatedGIFReader::GetFrameIndex(t, delays);
        
        Print << U"current frame: " << frameIndex;
        
        // 해당 프레임 그리기
        textures[frameIndex].draw(200, 200);
    }
}
```

---

## 실제 활용 예제

### 간단한 애니메이션 예제

```cpp
# include <Siv3D.hpp>

void Main()
{
    Scene::SetBackground(ColorF{ 0.1, 0.1, 0.2 });
    
    const Texture emoji{ U"🚀"_emoji };
    const Font font{ FontMethod::MSDF, 48 };
    
    // 부드러운 전환을 위한 객체들
    Transition rocketTransition{ 2.0s, 1.0s };
    Vec2 targetPos{ 400, 300 };
    Vec2 currentPos{ 100, 500 };
    Vec2 velocity{ 0, 0 };
    
    while (System::Update())
    {
        // 마우스 클릭 시 로켓 발사
        if (MouseL.down())
        {
            targetPos = Cursor::Pos();
        }
        
        // 로켓이 목표점에 가까이 있는지 확인
        bool nearTarget = (currentPos.distanceFrom(targetPos) < 50);
        rocketTransition.update(not nearTarget);
        
        // 부드러운 이동
        currentPos = Math::SmoothDamp(currentPos, targetPos, velocity, 0.8);
        
        // 로켓 회전각 계산 (이동 방향을 향하도록)
        const double angle = velocity.angle();
        
        // 배경 효과
        for (int32 i : step(20))
        {
            const double phase = (Scene::Time() + i * 0.1);
            const Vec2 pos{ 
                400 + Math::Sin(phase) * (50 + i * 10),
                300 + Math::Cos(phase * 0.7) * (30 + i * 5)
            };
            Circle{ pos, 2 }.draw(ColorF{ 0.3, 0.6, 1.0, 0.3 });
        }
        
        // 로켓 그리기
        emoji.scaled(1.0 + rocketTransition.value() * 0.5)
             .rotated(angle)
             .drawAt(currentPos);
        
        // 목표점 표시
        targetPos.asCircle(5).drawFrame(2, Palette::Yellow);
        
        // 정보 표시
        font(U"클릭해서 로켓을 날려보세요!").draw(20, Vec2{ 20, 20 }, ColorF{ 0.8 });
        font(U"속도: {:.1f}"_fmt(velocity.length())).draw(20, Vec2{ 20, 80 }, ColorF{ 0.8 });
    }
}
```

이 가이드로 OpenSiv3D의 기본기를 탄탄히 다질 수 있을 것이다. 각 섹션의 예제 코드를 직접 실행해보면서 감을 익혀보길 권한다.


---  
  
<br>  
<br>  

---  


## 1. 에셋 관리 시스템

### 1.1 에셋 관리 개요
OpenSiv3D의 에셋 관리 시스템을 통해 `Texture`, `Font`, `Audio` 등의 에셋 데이터를 이름으로 관리하고 프로그램 어디서든 접근할 수 있습니다.

#### 에셋 관리의 5단계
1. **등록(Registration)** - 에셋을 엔진에 등록
2. **로딩(Loading)** - 실제로 에셋 데이터를 로드 (선택사항)
3. **사용(Usage)** - 등록된 에셋을 사용
4. **해제(Release)** - 메모리에서 에셋 데이터를 해제 (선택사항)
5. **등록 해제(Unregistration)** - 에셋 등록 정보를 삭제 (선택사항)

#### 주요 함수들
```cpp
Register(name, ...)     // 에셋 등록
IsRegistered(name)      // 에셋이 등록되어 있는지 확인
Load(name)              // 에셋 로드
LoadAsync(name)         // 비동기 로드 시작
Wait(name)              // 비동기 로드 완료까지 대기
IsReady(name)           // 로드 완료 여부 확인
Release(name)           // 에셋 해제
Unregister(name)        // 에셋 등록 해제
ReleaseAll()            // 모든 등록된 에셋 해제
UnregisterAll()         // 모든 등록된 에셋 등록 해제
Enumerate()             // 모든 등록된 에셋 정보 나열
```

### 1.2 텍스처 에셋

```cpp
#include <Siv3D.hpp>

void Main()
{
    Scene::SetBackground(ColorF{0.6, 0.8, 0.7});

    // 텍스처 에셋 등록
    TextureAsset::Register(U"Windmill", U"example/windmill.png");
    TextureAsset::Register(U"Siv3D-kun", U"example/siv3d-kun.png", TextureDesc::Mipped);
    TextureAsset::Register(U"Cat", U"🐈"_emoji);

    while (System::Update())
    {
        // 텍스처 에셋 사용
        TextureAsset(U"Windmill").draw(40, 40);
        TextureAsset(U"Siv3D-kun").scaled(0.8).drawAt(300, 300);
        TextureAsset(U"Cat").drawAt(600, 400);
    }
}
```

### 1.3 고급 텍스처 에셋

복잡한 텍스처 생성이 필요할 때는 `TextureAssetData`를 사용합니다.

```cpp
std::unique_ptr<TextureAssetData> MakeTextureAssetData()
{
    auto assetData = std::make_unique<TextureAssetData>();

    // 로딩 작업 설정
    assetData->onLoad = [](TextureAssetData& asset, const String&)
    {
        // 에셋 데이터에 텍스처 할당
        asset.texture = Texture{Image{256, 256, Palette::Seagreen}, TextureDesc::Mipped};
        return static_cast<bool>(asset.texture);
    };

    return assetData;
}
```

### 1.4 폰트 에셋

```cpp
void Main()
{
    Scene::SetBackground(ColorF{0.6, 0.8, 0.7});

    // 폰트 에셋 등록
    FontAsset::Register(U"Title", FontMethod::MSDF, 48, U"example/font/RocknRoll/RocknRollOne-Regular.ttf");
    FontAsset::Register(U"Menu", FontMethod::MSDF, 48, Typeface::Bold);

    while (System::Update())
    {
        // 폰트 에셋 사용
        FontAsset(U"Title")(U"My Game").drawAt(80, Vec2{400, 100}, Palette::Seagreen);
        FontAsset(U"Menu")(U"Play").drawAt(40, Vec2{400, 400}, ColorF{0.1});
        FontAsset(U"Menu")(U"Exit").drawAt(40, Vec2{400, 500}, ColorF{0.1});
    }
}
```

### 1.5 오디오 에셋

```cpp
void Main()
{
    // 오디오 에셋 등록
    AudioAsset::Register(U"BGM", Audio::Stream, U"example/test.mp3");
    AudioAsset::Register(U"SE", U"example/shot.mp3");
    AudioAsset::Register(U"Piano", GMInstrument::Piano1, PianoKey::A4, 0.5s);

    // 배경음악 재생
    AudioAsset(U"BGM").setVolume(0.2);
    AudioAsset(U"BGM").play();

    while (System::Update())
    {
        if (MouseL.down())
        {
            AudioAsset(U"Piano").playOneShot();
        }

        if (MouseR.down())
        {
            AudioAsset(U"SE").playOneShot();
        }
    }
}
```

### 1.6 사전 로딩과 비동기 로딩

#### 사전 로딩
```cpp
// 사전 로드로 프레임 시간 급증 방지
FontAsset::Load(U"MyFont", preloadText);
TextureAsset::Load(U"MyTexture");
AudioAsset::Load(U"MyAudio");

// 로딩 완료 확인
Print << FontAsset::IsReady(U"MyFont");
Print << TextureAsset::IsReady(U"MyTexture");
```

#### 비동기 로딩
```cpp
// 비동기 로딩 시작 (메인 스레드 차단 방지)
FontAsset::LoadAsync(U"MyFont", preloadText);
TextureAsset::LoadAsync(U"MyTexture");

while (System::Update())
{
    // 로딩 완료 상태 확인
    Print << FontAsset::IsReady(U"MyFont");
    Print << TextureAsset::IsReady(U"MyTexture");
}
```

### 1.7 에셋 태그 활용

```cpp
void Main()
{
    // 태그와 함께 에셋 등록
    AudioAsset::Register({U"BGM-0", {U"BGM"}}, Audio::Stream, U"example/test.mp3");
    AudioAsset::Register({U"PianoC", {U"SE", U"Piano"}}, GMInstrument::Piano1, PianoKey::C4, 0.5s);
    AudioAsset::Register({U"TrumpetC", {U"SE", U"Trumpet"}}, GMInstrument::Trumpet, PianoKey::C4, 0.5s);

    // 특정 태그를 가진 에셋들만 로드
    for (auto&& [name, info] : AudioAsset::Enumerate())
    {
        if (info.tags.includes(U"SE"))
        {
            AudioAsset::Load(name);
        }
    }
}
```

---

## 2. 오디오 재생

### 2.1 기본 오디오 재생

#### 오디오 파일에서 로드
```cpp
void Main()
{
    // 오디오 파일 로드하여 Audio 생성
    const Audio audio{U"example/test.mp3"};

    // 오디오 재생
    audio.play();

    while (System::Update())
    {
        // 메인 루프
    }
}
```

#### 지원하는 오디오 포맷
| 포맷 | 확장자 | Windows | macOS | Linux | Web |
|------|--------|:-------:|:-----:|:-----:|:---:|
| WAVE | .wav | ✅ | ✅ | ✅ | ✅ |
| MP3 | .mp3 | ✅ | ✅ | ✅ | ✅* |
| AAC | .m4a | ✅ | ✅ | ✅ | ✅* |
| OggVorbis | .ogg | ✅ | ✅ | ✅ | ✅ |
| MIDI | .mid | ✅ | ✅ | ✅ | ✅ |

### 2.2 스트리밍 재생

대용량 오디오 파일의 경우 스트리밍 재생을 사용하여 메모리 사용량과 로딩 시간을 줄일 수 있습니다.

```cpp
void Main()
{
    // 스트리밍 재생으로 Audio 생성
    const Audio audio{Audio::Stream, U"example/test.mp3"};

    // 스트리밍 재생 활성화 여부 확인
    Print << audio.isStreaming();

    audio.play();

    while (System::Update())
    {
        // 메인 루프
    }
}
```

### 2.3 악기 소리 생성
프로그래밍 방식으로 악기 소리를 생성할 수 있습니다.

```cpp
void Main()
{
    // 피아노 C4(도) 소리
    const Audio piano{GMInstrument::Piano1, PianoKey::C4, 0.5s};

    // 클라리넷 D4(레) 소리  
    const Audio clarinet{GMInstrument::Clarinet, PianoKey::D4, 0.5s};

    // 트럼펫 E4(미) 소리 (노트온 2초, 노트오프 0.5초)
    const Audio trumpet{GMInstrument::Trumpet, PianoKey::E4, 2.0s, 0.5s};

    while (System::Update())
    {
        if (SimpleGUI::Button(U"Piano", Vec2{20, 20}))
        {
            piano.play();
        }

        if (SimpleGUI::Button(U"Clarinet", Vec2{20, 60}))
        {
            clarinet.play();
        }

        if (SimpleGUI::Button(U"Trumpet", Vec2{20, 100}))
        {
            trumpet.play();
        }
    }
}
```

### 2.4 오디오 제어

#### 재생 제어
```cpp
// 기본 재생 제어
audio.play();      // 재생
audio.pause();     // 일시정지
audio.stop();      // 정지 (재생 위치를 처음으로)

// 상태 확인
audio.isPlaying(); // 재생 중인지
audio.isPaused();  // 일시정지 중인지
```

#### 중복 재생
```cpp
// 같은 오디오를 중복 재생
audio.playOneShot();                    // 기본
audio.playOneShot(0.5);                 // 볼륨 0.5
audio.playOneShot(0.5, 0.0, 1.0);      // 볼륨, 팬, 속도 설정
```

#### 볼륨 제어
```cpp
// 볼륨 설정 (0.0 ~ 1.0)
audio.setVolume(0.5);

// 페이드 효과
audio.fadeVolume(1.0, 2s);  // 2초에 걸쳐 볼륨을 1.0으로

// 현재 볼륨 가져오기
double currentVolume = audio.getVolume();
```

#### 재생 속도 제어
```cpp
// 재생 속도 설정 (기본값 1.0)
audio.setSpeed(1.2);        // 1.2배속
audio.fadeSpeed(0.8, 1s);   // 1초에 걸쳐 0.8배속으로

// 현재 속도 가져오기
double currentSpeed = audio.getSpeed();
```

#### 팬(좌우 음향 밸런스) 제어
```cpp
// 팬 설정 (-1.0: 왼쪽, 0.0: 중앙, 1.0: 오른쪽)
audio.setPan(-0.5);         // 왼쪽으로 치우침
audio.fadePan(0.9, 2s);     // 2초에 걸쳐 오른쪽으로

// 현재 팬 값 가져오기
double currentPan = audio.getPan();
```

### 2.5 재생 위치 제어

#### 재생 시간 정보
```cpp
// 전체 재생 시간
double totalSec = audio.lengthSec();
int64 totalSamples = audio.lengthSample();

// 현재 재생 위치
double currentSec = audio.posSec();
int64 currentSamples = audio.posSample();
```

#### 재생 위치 이동
```cpp
// 샘플 단위로 이동
audio.seekSamples(441000);

// 시간 단위로 이동
audio.seekTime(20.0);  // 20초 위치로
```

### 2.6 루프 재생

#### 전체 루프
```cpp
// 전체 오디오를 루프 재생
const Audio audio{Audio::Stream, U"example/test.mp3", Loop::Yes};
audio.play();

// 루프 설정 여부와 횟수 확인
Print << audio.isLoop();
Print << audio.loopCount();
```

#### 구간 루프
```cpp
// 1.5초부터 44.5초까지 구간을 루프
const Audio audio{U"example/test.mp3", 
                  Arg::loopBegin = 1.5s, 
                  Arg::loopEnd = 44.5s};
audio.play();
```

### 2.7 믹싱 버스와 글로벌 오디오

오디오를 그룹별로 분류하여 각각의 볼륨과 필터를 제어할 수 있습니다.

```cpp
void Main()
{
    const Audio pianoC{GMInstrument::Piano1, PianoKey::C4, 0.5s};
    const Audio pianoD{GMInstrument::Piano1, PianoKey::D4, 0.5s};

    double globalVolume = 1.0, mixBus0Volume = 1.0, mixBus1Volume = 1.0;

    while (System::Update())
    {
        // 글로벌 오디오 볼륨 조절
        if (SimpleGUI::Slider(U"Global Vol", globalVolume, Vec2{20, 20}, 120, 200))
        {
            GlobalAudio::SetVolume(globalVolume);
        }

        // MixBus0 볼륨 조절
        if (SimpleGUI::Slider(U"Bus0 Vol", mixBus0Volume, Vec2{20, 60}, 120, 120))
        {
            GlobalAudio::BusSetVolume(MixBus0, mixBus0Volume);
        }

        // 각각 다른 믹싱 버스로 재생
        if (SimpleGUI::Button(U"C Bus0", Vec2{20, 100}))
        {
            pianoC.playOneShot(MixBus0, 0.5);
        }

        if (SimpleGUI::Button(U"C Bus1", Vec2{300, 100}))
        {
            pianoC.playOneShot(MixBus1, 0.5);
        }
    }
}
```

### 2.8 오디오 필터

실시간으로 오디오 파형을 처리하는 필터들을 적용할 수 있습니다.

#### 피치 시프트 (음정 변경)
```cpp
void Main()
{
    const Audio audio{U"example/test.mp3"};
    audio.play();

    double pitchShift = 0.0;  // 반음 단위

    while (System::Update())
    {
        if (SimpleGUI::Slider(U"pitchShift: {:.2f}"_fmt(pitchShift), 
                              pitchShift, -12.0, 12.0, Vec2{40, 40}, 160, 300))
        {
            // MixBus0의 0번 필터에 피치 시프트 설정
            GlobalAudio::BusSetPitchShiftFilter(MixBus0, 0, pitchShift);
        }
    }
}
```

#### 기타 필터들
- `BusSetLowPassFilter()` - 저역통과 필터
- `BusSetHighPassFilter()` - 고역통과 필터  
- `BusSetEchoFilter()` - 에코 필터
- `BusSetReverbFilter()` - 리버브 필터

### 2.9 파형 데이터 접근

```cpp
void Main()
{
    const Audio audio{U"example/test.mp3"};
    audio.play();

    const float* pSamples = audio.getSamples(0);  // 왼쪽 채널
    LineString lines(800);

    while (System::Update())
    {
        const int64 posSample = audio.posSample();

        // 현재 재생 위치부터 800개 샘플의 파형 가져오기
        for (int64 i = posSample; i < (posSample + 800); ++i)
        {
            if (i < audio.samples())
            {
                lines[i - posSample].set((i - posSample), (300 + pSamples[i] * 200));
            }
        }

        // 파형 그리기
        lines.draw(2);
    }
}
```

---

## 3. 설정 파일 처리
OpenSiv3D는 CSV, INI, JSON, TOML, XML 포맷의 설정 파일을 읽고 쓸 수 있습니다.

### 3.1 CSV 파일 처리

#### CSV 읽기
```cpp
struct Item
{
    String label;
    Point pos;
};

void Main()
{
    // CSV 파일 로드
    const CSV csv{U"example/csv/config.csv"};

    if (not csv)  // 로딩 실패 시
    {
        throw Error{U"Failed to load `config.csv`"};
    }

    // 각 행 처리
    for (size_t row = 0; row < csv.rows(); ++row)
    {
        String line;
        for (size_t col = 0; col < csv.columns(row); ++col)
        {
            line += (csv[row][col] + U'\t');
        }
        Print << line;
    }

    // 특정 값들 추출
    const String title = csv[1][1];
    const int32 width = Parse<int32>(csv[2][1]);
    const int32 height = Parse<int32>(csv[3][1]);
    const bool sizable = Parse<bool>(csv[4][1]);
    const ColorF background = Parse<ColorF>(csv[5][1]);

    // 창 설정 적용
    Window::SetTitle(title);
    Window::Resize(width, height);
    Window::SetStyle(sizable ? WindowStyle::Sizable : WindowStyle::Fixed);
    Scene::SetBackground(background);
}
```

#### CSV 쓰기
```cpp
void Main()
{
    CSV csv;

    // 한 행씩 작성
    csv.writeRow(U"item", U"price", U"count");
    csv.writeRow(U"Sword", 500, 1);

    // 항목별 작성
    csv.write(U"Arrow");
    csv.write(400);
    csv.write(2);
    csv.newLine();

    csv.writeRow(U"Shield", 300, 3);
    csv.writeRow(U"Carrot Seed", 20, 4);

    // 파일 저장
    csv.save(U"tutorial.csv");
}
```

### 3.2 INI 파일 처리

#### INI 읽기
```cpp
void Main()
{
    // INI 파일 로드
    const INI ini{U"example/ini/config.ini"};

    if (not ini)
    {
        throw Error{U"Failed to load `config.ini`"};
    }

    // 모든 섹션 나열
    for (const auto& section : ini.sections())
    {
        Print << U"[{}]"_fmt(section.section);
        
        for (auto&& [key, value] : section.keys)
        {
            Print << U"{} = {}"_fmt(key, value);
        }
    }

    // 특정 값 가져오기
    const String title = ini[U"Window.title"];
    const int32 width = Parse<int32>(ini[U"Window.width"]);
    const int32 height = Parse<int32>(ini[U"Window.height"]);
}
```

#### INI 쓰기
```cpp
void Main()
{
    INI ini;

    // 섹션 추가
    ini.addSection(U"Item");
    ini.addSection(U"Setting");

    // 값 설정
    ini.write(U"Item", U"Sword", 500);
    ini.write(U"Item", U"Arrow", 400);
    ini.write(U"Setting", U"pos", Point{20, 30});
    ini.write(U"Setting", U"color", Palette::Red);

    // 파일 저장
    ini.save(U"tutorial.ini");
}
```

### 3.3 JSON 파일 처리

#### JSON 읽기
```cpp
void ShowObject(const JSON& value)
{
    switch (value.getType())
    {
    case JSONValueType::Object:
        for (const auto& object : value)
        {
            Console << U"[{}]"_fmt(object.key);
            ShowObject(object.value);
        }
        break;
    case JSONValueType::Array:
        for (auto&& [index, object] : value)
        {
            ShowObject(object);
        }
        break;
    case JSONValueType::String:
        Console << value.getString();
        break;
    case JSONValueType::Number:
        Console << value.get<double>();
        break;
    case JSONValueType::Bool:
        Console << value.get<bool>();
        break;
    }
}

void Main()
{
    // JSON 파일 로드
    const JSON json = JSON::Load(U"example/json/config.json");

    if (not json)
    {
        throw Error{U"Failed to load `config.json`"};
    }

    // 모든 JSON 데이터 표시
    ShowObject(json);

    // 특정 값 가져오기
    const String title = json[U"Window"][U"title"].getString();
    const int32 width = json[U"Window"][U"width"].get<int32>();
    const int32 height = json[U"Window"][U"height"].get<int32>();
    const bool sizable = json[U"Window"][U"sizable"].get<bool>();

    // 배열 처리
    Array<int32> values;
    for (auto&& [index, object] : json[U"Array"][U"values"])
    {
        values << object.get<int32>();
    }
}
```

#### JSON 쓰기
```cpp
void Main()
{
    JSON json;

    // 데이터 설정
    json[U"Item"][U"Sword"][U"price"] = 500;
    json[U"Item"][U"Arrow"][U"price"] = 400;
    json[U"Setting"][U"pos"] = Point{20, 30};
    json[U"Setting"][U"color"] = Palette::Red;

    // 배열 요소 추가
    json[U"Array"].push_back(10);
    json[U"Array"].push_back(20);
    json[U"Array"].push_back(30);

    // 파일 저장
    json.save(U"tutorial.json");
}
```

#### JSON 리터럴 사용
```cpp
void Main()
{
    // JSON 리터럴로 직접 작성
    const JSON json = UR"({
        "name": "Albert",
        "age": 42,
        "object": {
            "string": "test"
        }
    })"_json;

    // 또는 String에서 파싱
    const String jsonString = U"...";
    const JSON parsedJson = JSON::Parse(jsonString);
}
```

### 3.4 TOML 파일 읽기

```cpp
void Main()
{
    // TOML 파일 로드
    const TOMLReader toml{U"example/toml/config.toml"};

    if (not toml)
    {
        throw Error{U"Failed to load `config.toml`"};
    }

    // 값 가져오기
    const String title = toml[U"Window.title"].getString();
    const int32 width = toml[U"Window.width"].get<int32>();
    const bool sizable = toml[U"Window.sizable"].get<bool>();

    // 배열 처리
    Array<int32> values;
    for (const auto& object : toml[U"Array.values"].arrayView())
    {
        values << object.get<int32>();
    }
}
```

### 3.5 XML 파일 읽기

```cpp
void ShowElements(const XMLElement& element)
{
    for (auto e = element.firstChild(); e; e = e.nextSibling())
    {
        Console << U"<{}>"_fmt(e.name());

        if (const auto attributes = e.attributes())
        {
            Console << attributes;
        }

        if (const auto text = e.text())
        {
            Console << text;
        }

        ShowElements(e);  // 재귀 호출

        Console << U"</{}>"_fmt(e.name());
    }
}

void Main()
{
    // XML 파일 로드
    const XMLReader xml(U"example/xml/config.xml");

    if (not xml)
    {
        throw Error{U"Failed to load `config.xml`"};
    }

    // 모든 XML 요소 표시
    ShowElements(xml);

    // 특정 요소 탐색
    for (auto elem = xml.firstChild(); elem; elem = elem.nextSibling())
    {
        const String name = elem.name();

        if (name == U"Window")
        {
            for (auto elem2 = elem.firstChild(); elem2; elem2 = elem2.nextSibling())
            {
                if (elem2.name() == U"title")
                {
                    String title = elem2.text();
                }
                else if (elem2.name() == U"width")
                {
                    int32 width = Parse<int32>(elem2.text());
                }
            }
        }
    }
}
```

---

## 4. 효과 시스템
효과(Effect) 시스템을 사용하여 시간 기반의 짧은 모션 표현을 효율적으로 만들 수 있습니다.

### 4.1 기본 효과 구현

#### IEffect 클래스 상속
```cpp
#include <Siv3D.hpp>

struct RingEffect : IEffect
{
    Vec2 m_pos;
    ColorF m_color;

    // 생성자의 인수들이 .add<RingEffect>()의 인수가 됨
    explicit RingEffect(const Vec2& pos)
        : m_pos{pos}
        , m_color{RandomColorF()} {}

    bool update(double t) override
    {
        // 시간에 따라 커지는 링 그리기
        Circle{m_pos, (t * 100)}.drawFrame(4, m_color);

        // 1초 미만이면 계속
        return (t < 1.0);
    }
};

void Main()
{
    Effect effect;

    while (System::Update())
    {
        ClearPrint();
        Print << U"Active effects: {}"_fmt(effect.num_effects());

        if (MouseL.down())
        {
            // 효과 추가
            effect.add<RingEffect>(Cursor::Pos());
        }

        // 관리하는 모든 효과들의 IEffect::update() 실행
        effect.update();
    }
}
```

### 4.2 람다식으로 효과 구현

간단한 효과는 람다식으로 더 편리하게 작성할 수 있습니다.

```cpp
void Main()
{
    Effect effect;

    while (System::Update())
    {
        ClearPrint();
        Print << U"Active effects: {}"_fmt(effect.num_effects());

        if (MouseL.down())
        {
            // 람다식으로 효과 추가
            effect.add([pos = Cursor::Pos(), color = RandomColorF()](double t)
            {
                // 시간에 따라 커지는 링 그리기
                Circle{pos, (t * 100)}.drawFrame(4, color);

                // 1초 미만이면 계속
                return (t < 1.0);
            });
        }

        effect.update();
    }
}
```

### 4.3 이징 활용

이징을 사용하면 모션의 인상을 바꿀 수 있습니다.

```cpp
struct RingEffect : IEffect
{
    Vec2 m_pos;
    ColorF m_color;

    explicit RingEffect(const Vec2& pos)
        : m_pos{pos}
        , m_color{RandomColorF()} {}

    bool update(double t) override
    {
        // 이징 적용
        const double e = EaseOutExpo(t);

        Circle{m_pos, (e * 100)}.drawFrame((20.0 * (1.0 - e)), m_color);

        return (t < 1.0);
    }
};
```

### 4.4 효과 제어

#### 일시정지, 속도 제어, 클리어
```cpp
void Main()
{
    Effect effect;
    double accumulatedTime = 0.0;
    Vec2 pos{200, 300};
    Vec2 velocity{320, 360};

    while (System::Update())
    {
        if (not effect.isPaused())
        {
            const double deltaTime = (Scene::DeltaTime() * effect.getSpeed());
            accumulatedTime += deltaTime;
            pos += (deltaTime * velocity);

            // 벽 충돌 시 반사
            if (((0 < velocity.x) && (800 < pos.x)) ||
                ((velocity.x < 0) && (pos.x < 0)))
            {
                velocity.x = -velocity.x;
            }
        }

        pos.asCircle(10).draw();
        effect.update();

        // 제어 버튼들
        if (effect.isPaused())
        {
            if (SimpleGUI::Button(U"Resume", Vec2{600, 20}, 100))
            {
                effect.resume();  // 효과 업데이트 재개
            }
        }
        else
        {
            if (SimpleGUI::Button(U"Pause", Vec2{600, 20}, 100))
            {
                effect.pause();   // 효과 업데이트 일시정지
            }
        }

        if (SimpleGUI::Button(U"x2.0", Vec2{600, 60}, 100))
        {
            effect.setSpeed(2.0);  // 2배속 설정
        }

        if (SimpleGUI::Button(U"Clear", Vec2{600, 180}, 100))
        {
            effect.clear();  // 모든 활성 효과 클리어
        }
    }
}
```

### 4.5 샘플 효과들

#### 상승하는 텍스트 효과
```cpp
struct ScoreEffect : IEffect
{
    Vec2 m_start;
    int32 m_score;
    Font m_font;

    ScoreEffect(const Vec2& start, int32 score, const Font& font)
        : m_start{start}, m_score{score}, m_font{font} {}

    bool update(double t) override
    {
        const HSV color{(180 - m_score * 1.8), (1.0 - (t * 2.0))};

        m_font(m_score).drawAt(TextStyle::Outline(0.2, ColorF{0.0, color.a}),
            60, m_start.movedBy(0, t * -120), color);

        return (t < 0.5);
    }
};

void Main()
{
    Scene::SetBackground(ColorF{0.6, 0.8, 0.7});
    const Font font{FontMethod::MSDF, 48, Typeface::Heavy, FontStyle::Italic};
    Effect effect;

    while (System::Update())
    {
        if (MouseL.down())
        {
            effect.add<ScoreEffect>(Cursor::Pos(), Random(0, 100), font);
        }

        effect.update();
    }
}
```

#### 흩어지는 파편 효과
```cpp
struct Particle
{
    Vec2 start;
    Vec2 velocity;
};

struct Spark : IEffect
{
    Array<Particle> m_particles;

    explicit Spark(const Vec2& start) : m_particles(50)
    {
        for (auto& particle : m_particles)
        {
            particle.start = (start + RandomVec2(12.0));
            particle.velocity = (RandomVec2(1.0) * Random(100.0));
        }
    }

    bool update(double t) override
    {
        for (const auto& particle : m_particles)
        {
            const Vec2 pos = (particle.start + particle.velocity * t + 
                             0.5 * t * t * Vec2{0, 240});

            Triangle{pos, (20.0 * (1.0 - t)), (pos.x * 10_deg)}
                .draw(HSV{pos.y - 40});
        }

        return (t < 1.0);
    }
};
```

#### 별 효과
```cpp
struct StarEffect : IEffect
{
    static constexpr Vec2 Gravity{0, 160};

    struct Star
    {
        Vec2 start;
        Vec2 velocity;
        ColorF color;
    };

    Array<Star> m_stars;

    StarEffect(const Vec2& pos, double baseHue)
    {
        for (int32 i = 0; i < 6; ++i)
        {
            const Vec2 velocity = RandomVec2(Circle{60});
            Star star{
                .start = (pos + velocity * 0.5),
                .velocity = velocity,
                .color = HSV{baseHue + Random(-20.0, 20.0)},
            };
            m_stars << star;
        }
    }

    bool update(double t) override
    {
        t /= 0.4;

        for (auto& star : m_stars)
        {
            const Vec2 pos = (star.start + star.velocity * t + 
                             0.5 * t * t * Gravity);
            const double angle = (pos.x * 3_deg);

            Shape2D::Star((36 * (1.0 - t)), pos, angle).draw(star.color);
        }

        return (t < 1.0);
    }
};
```

### 4.6 고급 효과 기법

#### 시간 지연을 활용한 버블 효과
```cpp
struct BubbleEffect : IEffect
{
    struct Bubble
    {
        Vec2 offset;
        double startTime;    // 등장 시간 지연
        double scale;
        ColorF color;
    };

    Vec2 m_pos;
    Array<Bubble> m_bubbles;

    BubbleEffect(const Vec2& pos, double baseHue) : m_pos{pos}
    {
        for (int32 i = 0; i < 8; ++i)
        {
            Bubble bubble{
                .offset = RandomVec2(Circle{30}),
                .startTime = Random(-0.3, 0.1),  // 등장 시간 지연
                .scale = Random(0.1, 1.2),
                .color = HSV{baseHue + Random(-30.0, 30.0)}
            };
            m_bubbles << bubble;
        }
    }

    bool update(double t) override
    {
        for (const auto& bubble : m_bubbles)
        {
            const double t2 = (bubble.startTime + t);

            if (not InRange(t2, 0.0, 1.0))
            {
                continue;
            }

            const double e = EaseOutExpo(t2);

            Circle{(m_pos + bubble.offset + (bubble.offset * 4 * t)), 
                   (e * 40 * bubble.scale)}
                .draw(ColorF{bubble.color, 0.15})
                .drawFrame((30.0 * (1.0 - e) * bubble.scale), bubble.color);
        }

        return (t < 1.3);
    }
};

void Main()
{
    Scene::SetBackground(ColorF{0.6, 0.8, 0.7});
    Effect effect;

    while (System::Update())
    {
        if (MouseL.down())
        {
            effect.add<BubbleEffect>(Cursor::Pos(), Random(0.0, 360.0));
        }

        // 가산 블렌딩 모드 적용
        {
            const ScopedRenderStates2D blend{BlendState::Additive};
            effect.update();
        }
    }
}
```

#### 효과 재귀 (새로운 효과 생성)
```cpp
struct Firework : IEffect
{
    const Effect& m_parent;  // 순환 참조 방지를 위해 참조 또는 포인터 사용
    Vec2 m_center;
    int32 m_generation;  // 세대 번호 [0, 1, 2]

    Firework(const Effect& parent, const Vec2& center, int32 gen, const Vec2& v0)
        : m_parent{parent}, m_center{center}, m_generation{gen}
    {
        // 초기화 코드...
    }

    bool update(double t) override
    {
        // 불꽃 그리기...

        if (m_generation < 2)  // 0세대 또는 1세대라면
        {
            // 특정 시간이 되면 자식 효과 생성
            if (shouldCreateChild && (nextFireSec <= t))
            {
                const Vec2 pos = calculateChildPosition(t);
                m_parent.add<Firework>(m_parent, pos, (m_generation + 1), velocity);
                hasChild = true;
            }
        }

        return (t < 1.5);
    }
};
```

---

## 마무리
OpenSiv3D의 에셋 관리, 오디오 재생, 설정 파일 처리, 효과 시스템은 게임과 멀티미디어 애플리케이션 개발에 핵심적인 기능들입니다. 각 시스템은 독립적으로 사용할 수도 있고, 서로 연계하여 더욱 풍부한 표현을 만들어낼 수도 있습니다.

### 학습 팁
1. **작은 예제부터 시작** - 각 기능의 기본 사용법을 먼저 익히세요
2. **실제 프로젝트에 적용** - 간단한 게임이나 애플리케이션을 만들어보며 실습하세요
3. **문서 참조** - 공식 문서와 샘플 코드를 자주 참조하세요
4. **실험과 변형** - 샘플 코드를 변형해보며 다양한 효과를 실험해보세요

OpenSiv3D의 강력한 기능들을 활용하여 멋진 작품을 만들어보시길 바랍니다!
   

---  
  
<br>  
<br>  

---  

## 1. 파일 시스템 (File System)

### 1.1 기본 개념

#### 경로 표현
파일이나 디렉토리 경로를 나타낼 때는 `FilePath` 타입을 사용합니다. 이는 `String`의 별칭이지만 용도를 명확히 해줍니다.

```cpp
const FilePath path = U"example/windmill.png";
const Texture texture{ path };

// 디렉토리 경로는 끝에 '/'를 붙입니다
const FilePath videoDirectory = U"example/video/";
```

### 1.2 파일/디렉토리 존재 확인

| 함수 | 설명 |
|------|------|
| `FileSystem::Exists(path)` | 파일이나 디렉토리가 존재하는지 확인 |
| `FileSystem::IsFile(path)` | 파일이 존재하는지 확인 |
| `FileSystem::IsDirectory(path)` | 디렉토리가 존재하는지 확인 |

```cpp
// 사용 예시
Print << FileSystem::Exists(U"example/windmill.png");     // true
Print << FileSystem::IsFile(U"example/windmill.png");     // true
Print << FileSystem::IsDirectory(U"example/video/");      // true
```

### 1.3 경로 변환 및 조작

#### 절대 경로와 상대 경로
```cpp
// 절대 경로 얻기
const FilePath fullPath = FileSystem::FullPath(U"example/windmill.png");

// 상대 경로로 변환
const FilePath relativePath = FileSystem::RelativePath(fullPath);
```

#### 파일명과 확장자
```cpp
const FilePath path = U"example/windmill.png";

// 파일명만 얻기 (확장자 포함)
Print << FileSystem::FileName(path);     // "windmill.png"

// 파일명만 얻기 (확장자 제외)
Print << FileSystem::BaseName(path);     // "windmill"

// 확장자만 얻기 (소문자로 변환)
Print << FileSystem::Extension(path);    // "png"
```

#### 부모 디렉토리
```cpp
// 부모 디렉토리 경로 얻기
Print << FileSystem::ParentPath(U"example/windmill.png");  
// 출력: C:/path/to/project/example/
```

### 1.4 특수 디렉토리

#### 현재 디렉토리
```cpp
// 현재 디렉토리 확인
Print << FileSystem::CurrentDirectory();

// 현재 디렉토리 변경
FileSystem::ChangeCurrentDirectory(U"example/");
```

#### 특수 폴더 경로
```cpp
// 특수 폴더들의 경로 얻기
Print << FileSystem::GetFolderPath(SpecialFolder::Desktop);     // 바탕화면
Print << FileSystem::GetFolderPath(SpecialFolder::Documents);   // 문서
Print << FileSystem::GetFolderPath(SpecialFolder::Pictures);    // 사진
Print << FileSystem::GetFolderPath(SpecialFolder::Music);       // 음악
```

### 1.5 파일 정보

#### 파일 크기
```cpp
// 파일 크기 (바이트)
const int64 fileSize = FileSystem::FileSize(U"example/windmill.png");

// 읽기 쉬운 형태로 변환
Print << FormatDataSize(fileSize);  // "247KiB" 형태
```

#### 파일 시간 정보
```cpp
// 파일 생성 시간
if (const auto time = FileSystem::CreationTime(path))
{
    Print << U"생성시간: " << *time;
}

// 마지막 수정 시간
if (const auto time = FileSystem::WriteTime(path))
{
    Print << U"수정시간: " << *time;
}
```

### 1.6 디렉토리 조작

#### 디렉토리 생성
```cpp
// 디렉토리 생성 (중간 경로도 자동 생성)
FileSystem::CreateDirectories(U"test/subfolder/");

// 파일 경로의 부모 디렉토리 생성
FileSystem::CreateParentDirectories(U"test/data/file.txt");
```

#### 파일/디렉토리 복사 및 삭제
```cpp
// 파일 복사
FileSystem::Copy(U"source.png", U"destination.png");

// 파일 삭제
FileSystem::Remove(U"file.txt");

// 휴지통으로 이동 (Windows)
FileSystem::Remove(U"file.txt", AllowUndo::Yes);
```

### 1.7 파일 변경 감지

파일 시스템의 변경사항을 실시간으로 감지할 수 있습니다.

```cpp
// 디렉토리 감시 시작
DirectoryWatcher watcher{ U"test/" };

while (System::Update())
{
    // 변경사항 확인
    for (auto&& [path, action] : watcher.retrieveChanges())
    {
        if (action == FileAction::Added)
        {
            Print << U"파일 추가: " << path;
        }
        else if (action == FileAction::Modified)
        {
            Print << U"파일 수정: " << path;
        }
        else if (action == FileAction::Removed)
        {
            Print << U"파일 삭제: " << path;
        }
    }
}
```

---

## 2. 게임패드 입력 처리 (Gamepad)

### 2.1 XInput 컨트롤러
Windows에서 Xbox 컨트롤러와 호환되는 패드를 사용할 수 있습니다 (최대 4개).

```cpp
// 플레이어 0의 컨트롤러 얻기
auto controller = XInput(0);

if (controller.isConnected())
{
    // 버튼 입력 확인
    if (controller.buttonA.pressed())
    {
        Print << U"A 버튼이 눌려있음";
    }
    
    // 스틱 입력 확인
    Vec2 leftStick = Vec2{ controller.leftThumbX, controller.leftThumbY };
    
    // 트리거 입력 확인 (0.0 ~ 1.0)
    double leftTrigger = controller.leftTrigger;
    
    // 진동 설정
    XInputVibration vibration;
    vibration.leftMotor = 0.5;   // 좌측 모터 강도
    vibration.rightMotor = 0.3;  // 우측 모터 강도
    controller.setVibration(vibration);
}
```

#### 데드존 설정
```cpp
// 기본 데드존 적용
controller.setLeftThumbDeadZone();
controller.setRightThumbDeadZone();

// 데드존 비활성화
controller.setLeftThumbDeadZone(DeadZone{});
```

### 2.2 Joy-Con 컨트롤러
Nintendo Switch의 Joy-Con을 Bluetooth로 연결하여 사용할 수 있습니다.

```cpp
// Joy-Con (L) 사용
if (const auto joy = JoyConL(0))
{
    // 방향키 입력
    if (auto direction = joy.povD8())
    {
        Vec2 movement = Circular{ 4, *direction * 45_deg };
        position += movement;
    }
    
    // 버튼 입력
    if (joy.button2.down())  // 버튼 2가 눌림
    {
        // 효과 생성
        CreateEffect(position);
    }
}

// Joy-Con (R) 사용
if (const auto joy = JoyConR(0))
{
    // 화면에 그리기 (크기, 회전 등 설정 가능)
    joy.drawAt(Vec2{ 640, 360 }, scale, angle, covered);
}
```

### 2.3 Pro Controller
Nintendo Switch Pro Controller도 사용할 수 있습니다.

```cpp
if (const auto pro = ProController(0))
{
    Print << U"A: " << pro.buttonA.pressed();
    Print << U"B: " << pro.buttonB.pressed();
    Print << U"왼쪽 스틱: " << pro.LStick();
    Print << U"오른쪽 스틱: " << pro.RStick();
    Print << U"방향키: " << pro.povD8();
}
```

### 2.4 일반 게임패드
모든 종류의 게임패드를 통합적으로 처리할 수 있습니다.

```cpp
// 플레이어 0의 게임패드
if (const auto gamepad = Gamepad(0))
{
    const auto& info = gamepad.getInfo();
    Print << U"패드명: " << info.name;
    
    // 모든 버튼 상태 확인
    for (auto&& [i, button] : Indexed(gamepad.buttons))
    {
        if (button.pressed())
        {
            Print << U"버튼 " << i << U" 눌림";
        }
    }
    
    // 모든 축 값 확인
    for (auto&& [i, axis] : Indexed(gamepad.axes))
    {
        Print << U"축 " << i << U": " << axis;
    }
}
```

### 2.5 Input 시스템과 통합
게임패드 입력을 키보드, 마우스와 통합하여 사용할 수 있습니다.

```cpp
// 여러 입력을 하나로 통합
const InputGroup jumpInput = (KeySpace | MouseL | XInput(0).buttonA);
const InputGroup moveLeftInput = (KeyLeft | XInput(0).buttonLeft);

while (System::Update())
{
    if (jumpInput.down())
    {
        player.Jump();
    }
    
    if (moveLeftInput.pressed())
    {
        player.MoveLeft();
    }
}
```

---

## 3. 형태 교차 검출 (Shape Intersection)

### 3.1 마우스 오버 검출
형태 위에 마우스 커서가 올라가 있는지 확인할 수 있습니다.

```cpp
const Circle circle{ 200, 150, 100 };
const Rect rect{ 400, 300, 200, 100 };

// 마우스가 형태 위에 있을 때 색상 변경
circle.draw(circle.mouseOver() ? Palette::Red : Palette::White);
rect.draw(rect.mouseOver() ? ColorF{ 0.8 } : ColorF{ 0.6 });
```

### 3.2 형태 클릭 검출
형태를 클릭했는지 감지할 수 있습니다.

| 함수 | 설명 |
|------|------|
| `.leftClicked()` | 왼쪽 버튼으로 클릭한 순간 |
| `.leftPressed()` | 왼쪽 버튼을 누르고 있는 동안 |
| `.leftReleased()` | 왼쪽 버튼을 놓은 순간 |
| `.rightClicked()` | 오른쪽 버튼으로 클릭한 순간 |

```cpp
const Rect button{ 100, 100, 200, 50 };

if (button.leftClicked())
{
    Print << U"버튼이 클릭됨!";
}

if (button.leftPressed())
{
    button.draw(Palette::Gray);  // 눌린 동안 회색으로
}
else
{
    button.draw(Palette::White);
}
```

### 3.3 형태 간 교차 판정
두 형태가 겹치는지 확인할 수 있습니다.

```cpp
const Circle playerCircle{ Cursor::Pos(), 30 };
const Rect obstacle{ 200, 200, 100, 100 };

// 형태끼리 겹치는지 확인
if (playerCircle.intersects(obstacle))
{
    obstacle.draw(Palette::Red);    // 겹칠 때 빨간색
}
else
{
    obstacle.draw(Palette::White);  // 평상시 흰색
}
```

### 3.4 포함 관계 판정
한 형태가 다른 형태를 완전히 포함하는지 확인할 수 있습니다.

```cpp
const Circle bigCircle{ 400, 300, 150 };
const Circle smallCircle{ Cursor::Pos(), 20 };

// 큰 원이 작은 원을 완전히 포함하는지 확인
if (bigCircle.contains(smallCircle))
{
    bigCircle.draw(Palette::Blue);
}
else
{
    bigCircle.draw(Palette::White);
}
```

### 3.5 교차점 계산
형태들이 교차하는 지점의 좌표를 얻을 수 있습니다.

#### 선분 교차점
```cpp
const Line line1{ 100, 100, 600, 500 };
const Line line2{ 400, 200, Cursor::Pos() };

// 두 선분의 교차점
if (const auto intersection = line1.intersectsAt(line2))
{
    Circle{ *intersection, 10 }.draw(Palette::Red);
}
```

#### 형태와 선분의 교차점
```cpp
const Circle circle{ 300, 300, 100 };
const Line line{ 100, 300, 500, 300 };

// 원과 직선의 교차점들
if (const auto points = circle.intersectsAt(line))
{
    for (const auto& point : *points)
    {
        Circle{ point, 5 }.draw(Palette::Red);
    }
}
```

### 3.6 폴리곤 연산
복잡한 다각형들 간의 불린 연산을 수행할 수 있습니다.

#### 교집합 (AND)
```cpp
const Polygon star = Shape2D::Star(200, Vec2{ 400, 300 });
const Polygon rect = Rect{ Cursor::Pos(), 300, 200 }.asPolygon();

// 두 폴리곤의 교집합
const Array<Polygon> intersection = Geometry2D::And(star, rect);
for (const auto& poly : intersection)
{
    poly.draw(Palette::Green);
}
```

#### 합집합 (OR)
```cpp
// 두 폴리곤의 합집합
const Array<Polygon> union = Geometry2D::Or(star, rect);
for (const auto& poly : union)
{
    poly.draw(Palette::Blue);
}
```

#### 차집합 (Subtract)
```cpp
// 첫 번째에서 두 번째를 뺀 결과
const Array<Polygon> difference = Geometry2D::Subtract(star, rect);
for (const auto& poly : difference)
{
    poly.draw(Palette::Yellow);
}
```

#### 볼록 껍질 (Convex Hull)
```cpp
const Polygon star = Shape2D::Star(200, Vec2{ 400, 300 });

// 별 모양의 볼록 껍질
const Polygon hull = star.computeConvexHull();
hull.drawFrame(3, Palette::Red);
```

---

## 4. 임베디드 리소스 (Embedded Resources)

### 4.1 개념 이해
임베디드 리소스는 이미지, 음성, 텍스트 파일 등을 실행 파일(.exe, .app) 안에 포함시키는 기능입니다.

**장점:**
- 애플리케이션을 단일 파일로 배포 가능
- 사용자가 필요한 파일을 실수로 삭제하거나 수정하는 것을 방지
- 배포와 관리가 간편함

**제한사항:**
- 임베디드 리소스는 읽기 전용
- 런타임에 수정이나 삭제 불가능

### 4.2 플랫폼별 임베딩 방법

#### Windows에서 임베딩
1. Visual Studio에서 `App/Resource.rc` 파일을 엽니다
2. 파일 끝에 임베드할 파일 경로를 추가합니다

```rc
// App/Resource.rc 파일에 추가
Resource(example/windmill.png)
Resource(sounds/bgm.mp3)
Resource(data/config.txt)
```

3. 프로젝트를 다시 빌드하면 파일들이 .exe에 포함됩니다

#### macOS에서 임베딩
1. Xcode 프로젝트 네비게이터에 폴더를 드래그
2. "Create folder references" 선택
3. 파란색 폴더 아이콘으로 표시되면 해당 폴더의 모든 파일이 .app에 포함됩니다

#### Linux에서 임베딩
Linux에서는 임베디드 리소스를 지원하지 않습니다. 대신 `resources/` 폴더에 필요한 파일들을 저장하고 실행 파일과 함께 배포해야 합니다.

### 4.3 임베디드 리소스 로딩
임베드된 리소스를 로드할 때는 경로를 `Resource()`로 감싸줍니다.

```cpp
// 일반 파일 로딩
const Texture normalTexture{ U"example/windmill.png" };

// 임베디드 리소스 로딩
const Texture embeddedTexture{ Resource(U"example/windmill.png") };

// 음성 파일도 동일하게
const Audio bgm{ Resource(U"sounds/bgm.mp3") };

// 텍스트 파일도 동일하게
const TextReader config{ Resource(U"data/config.txt") };
```

### 4.4 임베디드 리소스 목록 확인
현재 임베드된 리소스들의 목록을 확인할 수 있습니다.

```cpp
// 모든 임베디드 리소스 목록 출력
for (const auto& path : EnumResourceFiles())
{
    Console << path;
}
```

이 함수는 Siv3D 엔진이 내부적으로 사용하는 파일들과 사용자가 임베드한 파일들을 모두 보여줍니다.

### 4.5 실제 사용 예시

```cpp
# include <Siv3D.hpp>

void Main()
{
    // 임베디드 리소스에서 텍스처 로딩
    const Texture playerTexture{ Resource(U"images/player.png") };
    const Texture enemyTexture{ Resource(U"images/enemy.png") };
    
    // 임베디드 리소스에서 오디오 로딩
    const Audio jumpSound{ Resource(U"sounds/jump.wav") };
    const Audio bgm{ Resource(U"sounds/background.mp3") };
    
    // 배경음악 재생 시작
    bgm.play();
    
    Vec2 playerPos{ 400, 300 };
    
    while (System::Update())
    {
        // 플레이어 그리기
        playerTexture.drawAt(playerPos);
        
        // 스페이스바로 점프
        if (KeySpace.down())
        {
            jumpSound.playOneShot();
            // 점프 로직...
        }
        
        // 방향키로 이동
        if (KeyLeft.pressed()) playerPos.x -= 200 * Scene::DeltaTime();
        if (KeyRight.pressed()) playerPos.x += 200 * Scene::DeltaTime();
    }
}
```

### 4.6 주의사항

1. **Windows 제한사항**: 일부 파일 형태(동영상 등)는 임베드했을 때 제대로 로드되지 않을 수 있습니다.

2. **보안**: 임베디드 파일도 특수한 방법으로 추출할 수 있으므로, 중요한 파일은 암호화나 난독화 처리를 해야 합니다.

3. **파일 크기**: 많은 파일을 임베드하면 실행 파일 크기가 커집니다.

4. **개발 중 확인**: 임베드가 제대로 되었는지 확인하려면 빌드된 실행 파일만 다른 폴더로 복사해서 실행해보세요.

---

## 마무리

이 가이드는 OpenSiv3D의 핵심적인 4가지 기능을 다뤘습니다. 각 기능들은 게임 개발에서 자주 사용되는 중요한 요소들입니다.

- **파일 시스템**: 게임 설정 파일, 세이브 데이터 관리
- **게임패드**: 다양한 컨트롤러 지원으로 사용자 경험 향상
- **형태 교차 검출**: 충돌 감지, UI 상호작용
- **임베디드 리소스**: 간편한 배포와 리소스 관리

이 기능들을 익혀두면 OpenSiv3D로 더욱 완성도 높은 애플리케이션을 만들 수 있을 것입니다.


---  
  
<br>  
<br>  

---  


## 1. 키보드 입력 처리

### 1.1 기본 키 입력 상태
OpenSiv3D에서는 각 키에 대응하는 `Input` 타입의 상수들이 제공됩니다.

#### 주요 키 상수들

| 키 | 상수명 | 예시 |
|---|---|---|
| 알파벳 | `KeyA`, `KeyB`, `KeyC`, ... | `KeyA` (A키) |
| 숫자 | `Key1`, `Key2`, `Key3`, ... | `Key1` (1키) |
| 방향키 | `KeyUp`, `KeyDown`, `KeyLeft`, `KeyRight` | `KeyUp` (위쪽 화살표) |
| 기능키 | `KeyF1`, `KeyF2`, `KeyF3`, ... | `KeyF1` (F1키) |
| 특수키 | `KeySpace`, `KeyEnter`, `KeyEscape` | `KeySpace` (스페이스바) |

#### 입력 상태 확인 함수들
`Input` 타입은 다음과 같은 멤버 함수들로 현재 프레임에서의 상태를 `bool` 타입으로 반환합니다:

| 함수 | 설명 | 언제 true가 되나요? |
|---|---|---|
| `.down()` | 키가 방금 눌렸는지 | 키를 누른 그 순간만 |
| `.pressed()` | 키가 눌려있는지 | 키를 누르고 있는 동안 계속 |
| `.up()` | 키가 방금 떼어졌는지 | 키를 뗀 그 순간만 |

#### 실습 예제: 캐릭터 이동

```cpp
#include <Siv3D.hpp>

Vec2 GetMove(double deltaTime)
{
    const double delta = (deltaTime * 200);
    Vec2 move{ 0, 0 };

    if (KeyLeft.pressed())  // 왼쪽 키가 눌려있으면
    {
        move.x -= delta;
    }
    if (KeyRight.pressed()) // 오른쪽 키가 눌려있으면
    {
        move.x += delta;
    }
    if (KeyUp.pressed())    // 위쪽 키가 눌려있으면
    {
        move.y -= delta;
    }
    if (KeyDown.pressed())  // 아래쪽 키가 눌려있으면
    {
        move.y += delta;
    }

    return move;
}

void Main()
{
    Scene::SetBackground(ColorF{ 0.6, 0.8, 0.7 });
    Vec2 pos{ 400, 300 };

    while (System::Update())
    {
        // 방향키로 이동
        const Vec2 move = GetMove(Scene::DeltaTime());
        pos += move;

        // C키를 누르면 중앙으로 이동
        if (KeyC.down())
        {
            pos = Vec2{ 400, 300 };
        }

        pos.asCircle(50).draw(ColorF{ 0.2 });
    }
}
```

### 1.2 특수 기능이 할당된 키들
일부 키들은 Siv3D에서 특별한 기능이 기본적으로 할당되어 있습니다.

#### Escape 키
- 기본적으로 애플리케이션 종료 기능이 할당됨
- 종료 기능을 비활성화하려면: `System::SetTerminationTriggers(UserAction::CloseButtonClicked)`

#### F12 키와 PrintScreen 키
- 스크린샷 저장 기능이 할당됨
- F12 기능만 비활성화하려면: `ScreenCapture::SetShortcutKeys({ KeyPrintScreen })`

#### F1 키
- 라이선스 정보 표시 기능이 할당됨
- 비활성화하려면: `LicenseManager::DisableDefaultTrigger()`

### 1.3 키 입력 지속시간 측정

#### 현재 키가 눌린 시간 확인

```cpp
#include <Siv3D.hpp>

void Main()
{
    while (System::Update())
    {
        ClearPrint();
        
        // A키가 눌린 시간
        Print << KeyA.pressedDuration();
        
        // 스페이스 키가 1초 이상 눌렸는지 확인
        if (1s <= KeySpace.pressedDuration())
        {
            Print << U"스페이스 키를 1초 이상 누르고 있습니다!";
        }
    }
}
```

#### 키를 뗐을 때 얼마나 오래 눌렸는지 확인

```cpp
#include <Siv3D.hpp>

void Main()
{
    while (System::Update())
    {
        // 스페이스 키를 뗐을 때 얼마나 오래 눌렸는지 표시
        if (KeySpace.up())
        {
            Print << U"스페이스 키를 " << KeySpace.pressedDuration() << U" 동안 눌렀습니다.";
        }
    }
}
```

### 1.4 키 조합 사용하기

#### OR 조합 (A 또는 B)
여러 키 중 하나라도 눌리면 true가 되도록 하려면 `|` 연산자를 사용합니다.

```cpp
// 스페이스 또는 엔터가 눌렸는지 확인
if ((KeySpace | KeyEnter).pressed())
{
    Print << U"확인!";
}
```

#### 동시 조합 (A를 누른 상태에서 B)
한 키를 누른 상태에서 다른 키를 누르는 조합은 `+` 연산자를 사용합니다.

```cpp
// Ctrl+C 또는 Command+C (맥)가 눌렸는지 확인
if ((KeyControl + KeyC).down() || (KeyCommand + KeyC).down())
{
    Print << U"복사 명령!";
}
```

### 1.5 InputGroup으로 키 설정 관리하기
`InputGroup`을 사용하면 키 조합을 변수로 저장해서 나중에 쉽게 변경할 수 있습니다.

```cpp
#include <Siv3D.hpp>

void Main()
{
    // 키 그룹 정의
    InputGroup inputOK = (KeyZ | KeySpace | KeyEnter);     // 확인: Z, 스페이스, 엔터 중 아무거나
    InputGroup inputCopy = ((KeyControl + KeyC) | (KeyCommand + KeyC)); // 복사: Ctrl+C 또는 Cmd+C

    while (System::Update())
    {
        if (inputOK.down())
        {
            Print << U"확인 명령 실행!";
        }

        if (inputCopy.down())
        {
            Print << U"복사 명령 실행!";
        }
    }
}
```

### 1.6 텍스트 입력 처리
키보드로부터 텍스트를 입력받을 때는 `TextInput::UpdateText()`를 사용합니다.

```cpp
#include <Siv3D.hpp>

void Main()
{
    Scene::SetBackground(ColorF{ 0.6, 0.8, 0.7 });
    const Font font{ FontMethod::MSDF, 48 };
    String text;  // 입력된 텍스트를 저장할 변수
    const Rect box{ 50, 50, 700, 300 };

    while (System::Update())
    {
        // 키보드로부터 텍스트 입력 받기
        TextInput::UpdateText(text);
        
        // 변환 중인 텍스트 (일본어/한국어 입력 시)
        const String editingText = TextInput::GetEditingText();

        box.draw(ColorF{ 0.3 });
        font(text + U'|' + editingText).draw(30, box.stretched(-20));
    }
}
```

---

## 2. 마우스 입력 처리

### 2.1 마우스 커서 위치 확인
마우스 커서의 좌표는 다음 함수들로 얻을 수 있습니다:

| 함수 | 반환 타입 | 설명 |
|---|---|---|
| `Cursor::Pos()` | `Point` | 정수 좌표 |
| `Cursor::PosF()` | `Vec2` | 실수 좌표 (소수점 포함) |

```cpp
#include <Siv3D.hpp>

void Main()
{
    Scene::SetBackground(ColorF{ 0.6, 0.8, 0.7 });

    while (System::Update())
    {
        ClearPrint();
        Print << U"정수 좌표: " << Cursor::Pos();
        Print << U"실수 좌표: " << Cursor::PosF();
        
        // 마우스 위치에 원 그리기
        Circle{ Cursor::PosF(), 50 }.draw(ColorF{ 0.2 });
    }
}
```

### 2.2 마우스 이동 감지하기
마우스의 이동량을 감지할 수 있습니다:

| 함수 | 설명 |
|---|---|
| `Cursor::PreviousPos()` / `Cursor::PreviousPosF()` | 이전 프레임의 마우스 위치 |
| `Cursor::Delta()` / `Cursor::DeltaF()` | 이전 프레임부터의 이동량 |

#### 실습 예제: 드래그로 원 이동하기

```cpp
#include <Siv3D.hpp>

void Main()
{
    Scene::SetBackground(ColorF{ 0.6, 0.8, 0.7 });
    
    bool grab = false;  // 원을 잡고 있는지 여부
    Circle circle{ 400, 300, 50 };

    while (System::Update())
    {
        if (grab)
        {
            // 마우스 이동량만큼 원도 이동
            circle.moveBy(Cursor::Delta());
        }

        if (circle.leftClicked())  // 원을 클릭했을 때
        {
            grab = true;
        }
        else if (MouseL.up())      // 마우스 버튼을 뗐을 때
        {
            grab = false;
        }

        // 마우스가 원 위에 있거나 잡고 있을 때 커서를 손 모양으로
        if (grab || circle.mouseOver())
        {
            Cursor::RequestStyle(CursorStyle::Hand);
        }

        circle.draw(ColorF{ 0.2 });
    }
}
```

### 2.3 마우스 버튼 입력 상태
마우스 버튼들에도 키보드와 같은 `Input` 타입 상수들이 할당되어 있습니다:

| 상수 | 해당 버튼 |
|---|---|
| `MouseL` | 왼쪽 버튼 |
| `MouseR` | 오른쪽 버튼 |
| `MouseM` | 중간 버튼 (휠 버튼) |
| `MouseX1`, `MouseX2`, ... | 확장 버튼들 |

키보드와 동일하게 `.down()`, `.pressed()`, `.up()` 함수를 사용할 수 있습니다.

```cpp
#include <Siv3D.hpp>

void Main()
{
    while (System::Update())
    {
        ClearPrint();
        
        if (MouseL.pressed()) Print << U"왼쪽 버튼 누르는 중";
        if (MouseR.pressed()) Print << U"오른쪽 버튼 누르는 중";
        if (MouseM.pressed()) Print << U"중간 버튼 누르는 중";
    }
}
```

### 2.4 마우스 휠 입력 처리
마우스 휠의 회전량을 감지할 수 있습니다:

| 함수 | 설명 |
|---|---|
| `Mouse::Wheel()` | 세로 방향 휠 회전량 |
| `Mouse::WheelH()` | 가로 방향 휠 회전량 |

```cpp
#include <Siv3D.hpp>

void Main()
{
    Scene::SetBackground(ColorF{ 0.6, 0.8, 0.7 });
    Vec2 pos{ 400, 300 };

    while (System::Update())
    {
        ClearPrint();
        Print << U"세로 휠: " << Mouse::Wheel();
        Print << U"가로 휠: " << Mouse::WheelH();

        // 휠로 위치 조절
        pos.y -= (Mouse::Wheel() * 10);    // 세로 휠로 상하 이동
        pos.x += (Mouse::WheelH() * 10);   // 가로 휠로 좌우 이동

        RectF{ Arg::center = pos, 100 }.draw(ColorF{ 0.2 });
    }
}
```

### 2.5 마우스 커서 스타일 변경
마우스 커서의 모양을 바꿀 수 있습니다. `Cursor::RequestStyle()`을 사용합니다.

#### 기본 제공 스타일들

| 스타일 | 설명 |
|---|---|
| `CursorStyle::Arrow` | 기본 화살표 |
| `CursorStyle::Hand` | 손 모양 |
| `CursorStyle::IBeam` | 텍스트 입력용 I빔 |
| `CursorStyle::Cross` | 십자가 |
| `CursorStyle::NotAllowed` | 금지 표시 |
| `CursorStyle::Hidden` | 숨김 |

```cpp
#include <Siv3D.hpp>

void Main()
{
    Scene::SetBackground(Palette::White);
    const Circle button{ 400, 300, 60 };

    while (System::Update())
    {
        // 원 위에 마우스가 있으면 손 모양으로 변경
        if (button.mouseOver())
        {
            Cursor::RequestStyle(CursorStyle::Hand);
        }

        // 버튼 그리기
        button.draw(button.mouseOver() ? ColorF{ 0.2, 0.6, 1.0 } : ColorF{ 0.8 });
        
        if (button.leftClicked())
        {
            Print << U"버튼 클릭됨!";
        }
    }
}
```

### 2.6 유용한 팁들

#### 1. 더블 클릭 감지
더블 클릭을 감지하려면 클릭 간격과 시간을 체크하는 별도의 클래스를 만들어야 합니다.

#### 2. 마우스 커서 이동 제한 (Windows)
```cpp
// 마우스 커서를 윈도우 안에만 머물도록 제한
Cursor::ClipToWindow(true);

// 제한 해제
Cursor::ClipToWindow(false);
```

#### 3. 마우스 커서 강제 이동
```cpp
// 마우스 커서를 화면 중앙으로 이동
Cursor::SetPos(Point{ 400, 300 });
```

#### 4. 마우스가 윈도우 안에 있는지 확인
```cpp
if (Cursor::OnClientRect())
{
    Print << U"마우스가 윈도우 안에 있습니다";
}
```

---

## 🎯 정리
이 가이드에서는 OpenSiv3D에서 키보드와 마우스 입력을 처리하는 방법을 배웠습니다:

### 키보드 입력
- **기본 입력**: `.down()`, `.pressed()`, `.up()` 함수 사용
- **키 조합**: `|` (OR), `+` (동시 조합) 연산자 사용
- **텍스트 입력**: `TextInput::UpdateText()` 함수 사용
- **입력 그룹**: `InputGroup`으로 키 설정 관리

### 마우스 입력
- **위치 확인**: `Cursor::Pos()`, `Cursor::PosF()` 함수 사용
- **이동 감지**: `Cursor::Delta()` 함수로 이동량 확인
- **버튼 입력**: `MouseL`, `MouseR`, `MouseM` 등의 상수 사용
- **휠 입력**: `Mouse::Wheel()` 함수 사용
- **커서 스타일**: `Cursor::RequestStyle()` 함수로 모양 변경

이제 이 지식을 바탕으로 사용자와 상호작용하는 다양한 프로그램을 만들어보세요!


---  
  
<br>  
<br>  

---  

---

## 1. 클립보드 기능
클립보드를 통해 텍스트, 이미지, 파일 경로 정보에 접근하는 방법을 배운다.

### 1.1 텍스트 복사 및 붙여넣기

**주요 함수**
- `Clipboard::GetText()`: 클립보드에서 텍스트 가져오기
- `Clipboard::SetText()`: 클립보드에 텍스트 복사하기

**예제 코드**
```cpp
#include <Siv3D.hpp>

void Main()
{
    Scene::SetBackground(ColorF{ 0.6, 0.8, 0.7 });

    while (System::Update())
    {
        // 붙여넣기 버튼
        if (SimpleGUI::Button(U"Paste", Vec2{ 640, 80 }, 100))
        {
            String text;
            if (Clipboard::GetText(text))  // 성공하면 true 반환
            {
                ClearPrint();
                Print << text;
            }
        }

        // 복사 버튼
        if (SimpleGUI::Button(U"Copy", Vec2{ 640, 120 }, 100))
        {
            const String text = U"Hello, Siv3D!";
            Clipboard::SetText(text);
        }
    }
}
```

### 1.2 이미지 복사 및 붙여넣기
**주요 함수**
- `Clipboard::GetImage()`: 클립보드에서 이미지 가져오기
- `Clipboard::SetImage()`: 클립보드에 이미지 복사하기

**예제 코드**
```cpp
#include <Siv3D.hpp>

void Main()
{
    Scene::SetBackground(ColorF{ 0.6, 0.8, 0.7 });

    const Image image{ U"example/windmill.png" };
    Texture texture;

    while (System::Update())
    {
        // 이미지 붙여넣기
        if (SimpleGUI::Button(U"Paste", Vec2{ 640, 80 }, 100))
        {
            Image clipboardImage;
            if (Clipboard::GetImage(clipboardImage))
            {
                texture = Texture{ clipboardImage };
            }
            else
            {
                texture.release();  // 텍스처 해제
            }
        }

        // 이미지 복사
        if (SimpleGUI::Button(U"Copy", Vec2{ 640, 120 }, 100))
        {
            Clipboard::SetImage(image);
        }

        if (texture)
        {
            texture.fitted(Scene::Size()).drawAt(400, 300);
        }
    }
}
```

### 1.3 파일 경로 가져오기

**주요 함수**
- `Clipboard::GetFilePaths()`: 클립보드에서 파일 경로들 가져오기

**예제 코드**
```cpp
#include <Siv3D.hpp>

void Main()
{
    Scene::SetBackground(ColorF{ 0.6, 0.8, 0.7 });

    while (System::Update())
    {
        if (SimpleGUI::Button(U"Paste", Vec2{ 640, 80 }, 100))
        {
            Array<FilePath> paths;
            if (Clipboard::GetFilePaths(paths))
            {
                ClearPrint();
                for (const auto& path : paths)
                {
                    Print << path;
                }
            }
        }
    }
}
```

### 1.4 클립보드 내용 지우기

**주요 함수**
- `Clipboard::Clear()`: 클립보드 내용 지우기

```cpp
if (SimpleGUI::Button(U"Clear", Vec2{ 640, 80 }, 100))
{
    Clipboard::Clear();
}
```

---

## 2. 드래그 앤 드롭 기능

애플리케이션 창으로 드래그 앤 드롭된 파일 정보를 가져오는 방법을 배운다.

### 2.1 드롭된 파일 가져오기

**주요 함수**
- `DragDrop::HasNewFilePaths()`: 새로 드롭된 파일이 있는지 확인
- `DragDrop::GetDroppedFilePaths()`: 드롭된 파일 경로들 가져오기

**DroppedFilePath 구조체**
| 멤버 변수 | 설명 |
|---|---|
| `FilePath path` | 파일/디렉터리의 절대 경로 |
| `Point pos` | 드롭된 위치 (씬 좌표) |
| `uint64 timeMillisec` | 드롭된 시점 |

**예제 코드**
```cpp
#include <Siv3D.hpp>

void Main()
{
    while (System::Update())
    {
        if (DragDrop::HasNewFilePaths())
        {
            for (auto&& [path, pos, timeMillisec] : DragDrop::GetDroppedFilePaths())
            {
                Print << U"{} @{} :{}"_fmt(path, pos, timeMillisec);
            }
        }
    }
}
```

### 2.2 파일 드롭 비활성화

**주요 함수**
- `DragDrop::AcceptFilePaths(false)`: 파일 드롭 거부

```cpp
#include <Siv3D.hpp>

void Main()
{
    DragDrop::AcceptFilePaths(false);  // 파일 드롭 비활성화
    
    while (System::Update())
    {
        // 파일이 드롭되지 않는다
    }
}
```

### 2.3 텍스트 드롭 받기

**주요 함수**
- `DragDrop::AcceptText(true)`: 텍스트 드롭 활성화 (기본적으로 비활성화됨)
- `DragDrop::HasNewText()`: 새로 드롭된 텍스트 확인
- `DragDrop::GetDroppedText()`: 드롭된 텍스트 가져오기

**DroppedText 구조체**
| 멤버 변수 | 설명 |
|---|---|
| `String text` | 드롭된 텍스트 |
| `Point pos` | 드롭된 위치 |
| `uint64 timeMillisec` | 드롭된 시점 |

**예제 코드**
```cpp
#include <Siv3D.hpp>

void Main()
{
    DragDrop::AcceptText(true);  // 텍스트 드롭 활성화

    while (System::Update())
    {
        if (DragDrop::HasNewText())
        {
            for (auto&& [text, pos, timeMillisec] : DragDrop::GetDroppedText())
            {
                Print << U"{} @{} :{}"_fmt(text, pos, timeMillisec);
            }
        }
    }
}
```

### 2.4 드래그 상태 확인

**주요 함수**
- `DragDrop::DragOver()`: 창 위로 드래그 중인 항목 정보 반환

**DragStatus 구조체**
| 멤버 변수 | 설명 |
|---|---|
| `DragItemType itemType` | 드래그 중인 항목 타입 |
| `Point cursorPos` | 드래그 중인 커서 위치 |

**예제 코드**
```cpp
#include <Siv3D.hpp>

void Main()
{
    Scene::SetBackground(ColorF{ 0.6, 0.8, 0.7 });
    const Texture icon{ 0xf15b_icon, 40 };

    while (System::Update())
    {
        // 드래그 중인 항목이 있을 때
        if (const auto status = DragDrop::DragOver())
        {
            if (status->itemType == DragItemType::FilePaths)
            {
                icon.drawAt(status->cursorPos, ColorF{ 0.1 });
            }
        }

        if (DragDrop::HasNewFilePaths())
        {
            for (auto&& [path, pos, timeMillisec] : DragDrop::GetDroppedFilePaths())
            {
                Print << U"{} @{} :{}"_fmt(path, pos, timeMillisec);
            }
        }
    }
}
```

---

## 3. 파일 대화상자 기능

파일을 열거나 저장하기 위한 대화상자를 여는 방법을 배운다.

### 3.1 이미지 파일 열기

**주요 함수**
- `Dialog::OpenTexture()`: 이미지 파일 선택 후 텍스처 생성
- `Dialog::OpenTexture(TextureDesc::Mipped)`: 밉맵 적용된 텍스처 생성

**예제 코드**
```cpp
#include <Siv3D.hpp>

void Main()
{
    Texture texture = Dialog::OpenTexture();  // 초기 이미지 열기

    while (System::Update())
    {
        if (texture)
        {
            texture.fitted(Scene::Size()).drawAt(400, 300);
        }

        if (SimpleGUI::Button(U"Open", Vec2{ 20, 20 }))
        {
            // 밉맵 적용된 텍스처로 열기
            texture = Dialog::OpenTexture(TextureDesc::Mipped);
        }
    }
}
```

**초기 폴더 지정**
```cpp
// example/ 폴더를 초기 폴더로 설정
Texture texture = Dialog::OpenTexture(U"example/");
```

### 3.2 오디오 파일 열기

**주요 함수**
- `Dialog::OpenAudio()`: 오디오 파일 선택 후 오디오 생성
- `Dialog::OpenAudio(Audio::Stream)`: 스트리밍 오디오로 생성

**예제 코드**
```cpp
#include <Siv3D.hpp>

void Main()
{
    Audio audio = Dialog::OpenAudio();

    while (System::Update())
    {
        if (audio && (not audio.isPlaying()))
        {
            audio.play();
        }

        if (SimpleGUI::Button(U"Open", Vec2{ 20, 20 }))
        {
            audio = Dialog::OpenAudio(Audio::Stream);
        }
    }
}
```

### 3.3 폴더 선택

**주요 함수**
- `Dialog::SelectFolder()`: 폴더 선택 대화상자

**예제 코드**
```cpp
#include <Siv3D.hpp>

void Main()
{
    Optional<FilePath> path = Dialog::SelectFolder();

    while (System::Update())
    {
        ClearPrint();

        if (path)
        {
            Print << *path;
        }

        if (SimpleGUI::Button(U"Open", Vec2{ 20, 80 }))
        {
            path = Dialog::SelectFolder(U"./");  // 현재 디렉터리를 초기 폴더로
        }
    }
}
```

### 3.4 파일 선택 (단일)

**주요 함수**
- `Dialog::OpenFile()`: 단일 파일 선택
- 파일 필터를 통해 특정 확장자만 표시 가능

**파일 필터 종류**
- `FileFilter::AllFiles()`: 모든 파일
- `FileFilter::AllImageFiles()`: 모든 이미지 파일
- `FileFilter::Text()`: 텍스트 파일 (.txt)
- `FileFilter::PNG()`: PNG 파일
- 커스텀 필터: `{ U"Binary file", { U"bin" } }`

**예제 코드**
```cpp
#include <Siv3D.hpp>

void Main()
{
    Optional<FilePath> path = Dialog::OpenFile({ FileFilter::AllFiles() });

    while (System::Update())
    {
        ClearPrint();

        if (path)
        {
            Print << *path;
        }

        if (SimpleGUI::Button(U"Open image", Vec2{ 20, 80 }))
        {
            path = Dialog::OpenFile({ FileFilter::AllImageFiles() });
        }

        if (SimpleGUI::Button(U"Open text", Vec2{ 20, 120 }))
        {
            path = Dialog::OpenFile({ FileFilter::Text() });
        }

        if (SimpleGUI::Button(U"Open .bin", Vec2{ 20, 160 }))
        {
            path = Dialog::OpenFile({ { U"Binary file", { U"bin" } } });
        }
    }
}
```

### 3.5 파일 선택 (다중)

**주요 함수**
- `Dialog::OpenFiles()`: 다중 파일 선택

**예제 코드**
```cpp
#include <Siv3D.hpp>

void Main()
{
    Array<FilePath> paths = Dialog::OpenFiles({ FileFilter::AllFiles() });

    while (System::Update())
    {
        ClearPrint();

        for (const auto& path : paths)
        {
            Print << path;
        }

        if (SimpleGUI::Button(U"Open images", Vec2{ 600, 80 }))
        {
            paths = Dialog::OpenFiles({ FileFilter::AllImageFiles() });
        }
    }
}
```

### 3.6 이미지 저장

**주요 함수**
- `Image.saveWithDialog()`: 이미지를 파일 대화상자로 저장

**예제 코드**
```cpp
#include <Siv3D.hpp>

void Main()
{
    Scene::SetBackground(ColorF{ 0.6, 0.8, 0.7 });

    Image image{ U"example/windmill.png" };
    image.mirror();  // 이미지를 좌우 반전

    while (System::Update())
    {
        if (SimpleGUI::Button(U"Save", Vec2{ 40, 40 }))
        {
            const bool result = image.saveWithDialog();

            if (result)
            {
                Print << U"저장 완료!";
            }
        }
    }
}
```

### 3.7 파일 저장

**주요 함수**
- `Dialog::SaveFile()`: 저장할 파일 경로 선택

**예제 코드**
```cpp
#include <Siv3D.hpp>

void Main()
{
    Optional<FilePath> path;

    while (System::Update())
    {
        ClearPrint();

        if (path)
        {
            Print << *path;
        }

        if (SimpleGUI::Button(U"Save PNG", Vec2{ 20, 80 }))
        {
            path = Dialog::SaveFile({ FileFilter::PNG() });
        }

        if (SimpleGUI::Button(U"Save Text", Vec2{ 20, 120 }))
        {
            path = Dialog::SaveFile({ FileFilter::Text() });
        }
    }
}
```

---

## 4. HTTP 클라이언트 기능

HTTP 요청을 통해 웹페이지에 접근하고 파일을 다운로드하는 방법을 배운다.

### 4.1 기본 설정

**URL 타입 사용**
```cpp
#include <Siv3D.hpp>

void Main()
{
    const URL url = U"https://example.com";  // URL 타입 사용
    
    while (System::Update())
    {
        // HTTP 요청 처리
    }
}
```

### 4.2 인터넷 연결 확인

**주요 함수**
- `Network::IsConnected()`: 인터넷 연결 상태 확인

**예제 코드**
```cpp
#include <Siv3D.hpp>

void Main()
{
    if (Network::IsConnected())
    {
        Print << U"인터넷에 연결됨";
    }
    else
    {
        Print << U"인터넷에 연결되지 않음";
    }

    while (System::Update())
    {
        // 메인 루프
    }
}
```

### 4.3 웹브라우저에서 URL 열기

**주요 함수**
- `System::LaunchBrowser(url)`: 웹브라우저에서 URL 열기

**예제 코드**
```cpp
#include <Siv3D.hpp>

void Main()
{
    Scene::SetBackground(ColorF{ 0.6, 0.8, 0.7 });

    while (System::Update())
    {
        if (SimpleGUI::Button(U"웹사이트 방문", Vec2{ 40, 40 }))
        {
            System::LaunchBrowser(U"https://siv3d.github.io/ja-jp/");
        }
    }
}
```

### 4.4 파일 다운로드 (동기)

**주요 함수**
- `SimpleHTTP::Save(url, saveFilePath)`: 동기 파일 다운로드
- `HTTPResponse.isOK()`: 요청 성공 여부 확인

**예제 코드**
```cpp
#include <Siv3D.hpp>

void Main()
{
    Scene::SetBackground(ColorF{ 0.6, 0.8, 0.7 });

    const URL url = U"https://raw.githubusercontent.com/Siv3D/siv3d.docs.images/master/logo/logo.png";
    const FilePath saveFilePath = U"logo.png";

    Texture texture;

    // 파일을 동기적으로 다운로드
    if (SimpleHTTP::Save(url, saveFilePath).isOK())
    {
        texture = Texture{ saveFilePath };
    }
    else
    {
        Print << U"다운로드 실패";
    }

    while (System::Update())
    {
        if (texture)
        {
            texture.draw();
        }
    }
}
```

### 4.5 파일 다운로드 (비동기)

**주요 함수**
- `SimpleHTTP::SaveAsync(url, saveFilePath)`: 비동기 파일 다운로드
- `AsyncHTTPTask.isReady()`: 작업 완료 확인
- `AsyncHTTPTask.isDownloading()`: 다운로드 중인지 확인
- `AsyncHTTPTask.getResponse()`: 응답 받기

**예제 코드**
```cpp
#include <Siv3D.hpp>

void Main()
{
    Scene::SetBackground(ColorF{ 0.6, 0.8, 0.7 });

    const URL url = U"https://raw.githubusercontent.com/Siv3D/siv3d.docs.images/master/logo/logo.png";
    const FilePath saveFilePath = U"logo2.png";

    Texture texture;

    // 비동기 파일 다운로드 시작
    AsyncHTTPTask task = SimpleHTTP::SaveAsync(url, saveFilePath);

    while (System::Update())
    {
        // 비동기 작업 완료됨
        if (task.isReady())
        {
            if (task.getResponse().isOK())
            {
                texture = Texture{ saveFilePath };
            }
            else
            {
                Print << U"다운로드 실패";
            }
        }

        // 다운로드 중이면 로딩 표시
        if (task.isDownloading())
        {
            Circle{ 400, 300, 50 }.drawArc((Scene::Time() * 120_deg), 300_deg, 4, 4);
        }

        if (texture)
        {
            texture.draw();
        }
    }
}
```

### 4.6 다운로드 진행률 확인 및 취소

**HTTPProgress 구조체**
| 멤버 변수 | 설명 |
|---|---|
| `HTTPAsyncStatus status` | 진행 상태 |
| `int64 downloaded_bytes` | 다운로드된 크기 (바이트) |
| `Optional<int64> download_total_bytes` | 전체 파일 크기 (바이트) |

**주요 함수**
- `AsyncHTTPTask.getProgress()`: 다운로드 진행률 확인
- `AsyncHTTPTask.cancel()`: 다운로드 취소

**예제 코드**
```cpp
#include <Siv3D.hpp>

String ToString(HTTPAsyncStatus status)
{
    switch (status)
    {
    case HTTPAsyncStatus::None_:
        return U"None_";
    case HTTPAsyncStatus::Downloading:
        return U"다운로드 중";
    case HTTPAsyncStatus::Failed:
        return U"실패";
    case HTTPAsyncStatus::Canceled:
        return U"취소됨";
    case HTTPAsyncStatus::Succeeded:
        return U"성공";
    default:
        return U"알 수 없음";
    }
}

void Main()
{
    Scene::SetBackground(ColorF{ 0.6, 0.8, 0.7 });

    const Font font{ FontMethod::MSDF, 48, Typeface::Bold };
    const URL url = U"https://httpbin.org/drip?duration=4&numbytes=1024&code=200&delay=0";
    const FilePath saveFilePath = U"drip.txt";

    AsyncHTTPTask task;

    while (System::Update())
    {
        if (SimpleGUI::Button(U"다운로드", Vec2{ 20, 20 }, 140, task.isEmpty()))
        {
            task = SimpleHTTP::SaveAsync(url, saveFilePath);
        }

        if (SimpleGUI::Button(U"취소", Vec2{ 180, 20 }, 140, 
            (task.getStatus() == HTTPAsyncStatus::Downloading)))
        {
            task.cancel();  // 작업 취소
        }

        // 작업 진행률
        const HTTPProgress progress = task.getProgress();

        font(U"상태: {}"_fmt(ToString(progress.status)))
            .draw(24, Vec2{ 20, 60 }, ColorF{ 0.1 });

        if (progress.status == HTTPAsyncStatus::Downloading)
        {
            const int64 downloaded = progress.downloaded_bytes;

            if (const Optional<int64> total = progress.download_total_bytes)
            {
                font(U"다운로드: {} bytes / {} bytes"_fmt(downloaded, *total))
                    .draw(24, Vec2{ 20, 100 }, ColorF{ 0.1 });

                // 진행률 바 표시
                const double progress0_1 = (static_cast<double>(downloaded) / *total);
                const RectF rect{ 20, 140, 500, 30 };
                rect.drawFrame(2, 0, ColorF{ 0.1 });
                RectF{ rect.pos, (rect.w * progress0_1), rect.h }.draw(ColorF{ 0.1 });
            }
            else
            {
                font(U"다운로드: {} bytes"_fmt(downloaded))
                    .draw(24, Vec2{ 20, 100 }, ColorF{ 0.1 });
            }
        }

        // 다운로드 중이면 로딩 표시
        if (task.isDownloading())
        {
            Circle{ 400, 300, 50 }.drawArc((Scene::Time() * 120_deg), 300_deg, 4, 4);
        }
    }
}
```

### 4.7 GET 요청

**주요 함수**
- `SimpleHTTP::Get(url, headers, saveFilePath)`: 동기 GET 요청
- `SimpleHTTP::GetAsync(url, headers, saveFilePath)`: 비동기 GET 요청

**예제 코드 (동기)**
```cpp
#include <Siv3D.hpp>

void Main()
{
    const URL url = U"https://httpbin.org/bearer";
    const HashTable<String, String> headers = { 
        { U"Authorization", U"Bearer TOKEN123456abcdef" } 
    };
    const FilePath saveFilePath = U"auth_result.json";

    if (SimpleHTTP::Get(url, headers, saveFilePath).isOK())
    {
        const JSON json = JSON::Load(saveFilePath);
        Print << U"인증됨: " << json[U"authenticated"].get<bool>();
        Print << U"토큰: " << json[U"token"].getString();
    }
    else
    {
        Print << U"요청 실패";
    }

    while (System::Update())
    {
        // 메인 루프
    }
}
```

### 4.8 POST 요청

**주요 함수**
- `SimpleHTTP::Post(url, headers, data, dataSize, saveFilePath)`: 동기 POST 요청
- `SimpleHTTP::PostAsync(url, headers, data, dataSize, saveFilePath)`: 비동기 POST 요청

**예제 코드 (동기)**
```cpp
#include <Siv3D.hpp>

void Main()
{
    const URL url = U"https://httpbin.org/post";
    const HashTable<String, String> headers = { 
        { U"Content-Type", U"application/json" } 
    };
    
    const std::string data = JSON
    {
        { U"body", U"Hello, Siv3D!" },
        { U"date", DateTime::Now().format() },
    }.formatUTF8();

    const FilePath saveFilePath = U"post_result.json";

    if (SimpleHTTP::Post(url, headers, data.data(), data.size(), saveFilePath).isOK())
    {
        const JSON json = JSON::Load(saveFilePath);
        Print << json.format();
    }
    else
    {
        Print << U"POST 요청 실패";
    }

    while (System::Update())
    {
        // 메인 루프
    }
}
```

---

## 학습 팁

1. **클립보드 기능**: 사용자가 외부에서 복사한 데이터를 게임에서 활용할 때 유용하다
2. **드래그 앤 드롭**: 파일을 게임으로 직접 끌어다 놓아 불러오는 기능을 만들 때 활용한다
3. **파일 대화상자**: 사용자 친화적인 파일 선택 인터페이스를 제공할 때 사용한다
4. **HTTP 클라이언트**: 온라인 게임에서 서버와의 통신이나 외부 API 연동에 활용할 수 있다

각 기능들은 독립적으로 사용할 수 있으며, 필요에 따라 조합해서 사용할 수 있다.


---  
  
<br>  
<br>  

---  


## 이미지 처리

### 개요
- **Image 클래스**: 메인 메모리에 저장되어 C++ 프로그램이 직접 접근할 수 있는 이미지 데이터
- **Texture 클래스**: GPU 메모리에 저장되어 화면 그리기에 최적화된 이미지 데이터
- **DynamicTexture 클래스**: 내용을 동적으로 변경할 수 있는 텍스처

| 클래스 | 데이터 위치 | 내용 업데이트 | 화면 그리기 | CPU 접근 | GPU 접근 |
|--------|------------|-------------|------------|----------|----------|
| Image | 메인 메모리 | ✅ | ❌ | ✅ | ❌ |
| Texture | GPU 메모리 | ❌ | ✅ | ❌ | ✅ |
| DynamicTexture | GPU 메모리 | ✅ | ✅ | ❌ | ✅ |

### 기본 사용법

#### 이미지 생성 및 픽셀 조작

```cpp
#include <Siv3D.hpp>

void Main()
{
    // 400x300 크기의 흰색 이미지 생성
    Image image{ Size{ 400, 300 }, Palette::White };
    
    // 특정 영역을 파란색으로 채우기
    for (int32 y = 0; y < 60; ++y)
    {
        for (int32 x = 0; x < 120; ++x)
        {
            image[y][x] = Color{ 0, 127, 255 };
        }
    }
    
    // 이미지를 텍스처로 변환하여 화면에 그리기
    const Texture texture{ image };
    
    while (System::Update())
    {
        texture.draw();
    }
}
```

#### 이미지 파일 로드 및 픽셀 정보 확인

```cpp
#include <Siv3D.hpp>

void Main()
{
    const Image image{ U"example/windmill.png" };
    const Texture texture{ image };
    
    while (System::Update())
    {
        texture.draw();
        
        const Point pos = Cursor::Pos();
        
        // 마우스 위치의 픽셀 색상 표시
        if (InRange(pos.x, 0, (image.width() - 1)) &&
            InRange(pos.y, 0, (image.height() - 1)))
        {
            const Color color = image[pos];
            Circle{ 640, 160, 40 }.draw(color);
        }
    }
}
```

### 이미지 처리 기능

#### 다양한 필터 효과

```cpp
// 색상 반전
image.negate();

// 그레이스케일 변환  
image.grayscale();

// 세피아 톤
image.sepia();

// 가우시안 블러
image.gaussianBlur(20);

// 모자이크 효과
image.mosaic(10);
```

#### 그림 그리기 앱 만들기

```cpp
#include <Siv3D.hpp>

void Main()
{
    constexpr Size CanvasSize{ 600, 600 };
    constexpr int32 PenThickness = 8;
    constexpr Color PenColor = Palette::Orange;
    
    Image image{ CanvasSize, Palette::White };
    DynamicTexture texture{ image };
    
    while (System::Update())
    {
        if (MouseL.pressed())
        {
            const Point from = (MouseL.down() ? Cursor::Pos() : Cursor::PreviousPos());
            const Point to = Cursor::Pos();
            
            Line{ from, to }.overwrite(image, PenThickness, PenColor);
            texture.fill(image);
        }
        
        if (SimpleGUI::Button(U"지우기", Vec2{ 620, 40 }, 160))
        {
            image.fill(Palette::White);
            texture.fill(image);
        }
        
        if (SimpleGUI::Button(U"저장", Vec2{ 620, 100 }, 160))
        {
            image.saveWithDialog();
        }
        
        texture.draw();
    }
}
```

---

## 2D 물리 시뮬레이션

### 주요 클래스 개요

| 클래스 | 설명 |
|--------|------|
| `P2World` | 2D 물리 시뮬레이션 월드 (보통 하나만 생성) |
| `P2Body` | 월드에 존재하는 물체 |
| `P2BodyType` | 물체 타입 (동적/정적) |
| `P2Material` | 물체의 물리적 속성 (밀도, 마찰, 반발) |

### 기본 설정

```cpp
#include <Siv3D.hpp>

void Main()
{
    Window::Resize(1280, 720);
    
    // 물리 시뮬레이션 스텝 시간 (초)
    constexpr double StepTime = (1.0 / 200.0);
    double accumulatedTime = 0.0;
    
    // 2D 물리 월드 생성
    P2World world;
    
    // 지면 생성 (정적 물체)
    const P2Body ground = world.createRect(P2Static, Vec2{ 0, 0 }, SizeF{ 1000, 10 });
    
    // 떨어지는 공 생성 (동적 물체)
    P2Body ball = world.createCircle(P2Dynamic, Vec2{ 0, -300 }, 10);
    
    // 2D 카메라
    Camera2D camera{ Vec2{ 0, -300 }, 1.0 };
    
    while (System::Update())
    {
        // 물리 시뮬레이션 업데이트
        for (accumulatedTime += Scene::DeltaTime(); 
             StepTime <= accumulatedTime; 
             accumulatedTime -= StepTime)
        {
            world.update(StepTime);
        }
        
        // 카메라 업데이트 및 그리기
        camera.update();
        {
            const auto t = camera.createTransformer();
            
            ground.draw(Palette::Gray);
            ball.draw(Palette::Red);
        }
        
        camera.draw(Palette::Orange);
    }
}
```

### 다양한 모양의 물체 생성

```cpp
// 원형
P2Body circle = world.createCircle(P2Dynamic, Vec2{ 0, -300 }, 20);

// 사각형
P2Body rect = world.createRect(P2Dynamic, Vec2{ 100, -300 }, Size{ 40, 60 });

// 삼각형
P2Body triangle = world.createTriangle(P2Dynamic, Vec2{ 200, -300 }, Triangle{ 40 });

// 다각형
const Polygon polygon = Shape2D::NStar(5, 30, 20);
P2Body star = world.createPolygon(P2Dynamic, Vec2{ 300, -300 }, polygon);
```

### 물체 속성 설정

```cpp
// 재질 속성
P2Material material{
    .density = 2.0,        // 밀도 (kg/m²)
    .restitution = 0.8,    // 반발계수 (0.0~1.0)
    .friction = 0.3        // 마찰계수 (0.0~1.0)
};

P2Body body = world.createCircle(P2Dynamic, Vec2{ 0, -300 }, 20, material);

// 초기 속도 설정
body.setVelocity(Vec2{ 100, -200 });

// 회전 각도 설정
body.setAngle(45_deg);

// 각속도 설정
body.setAngularVelocity(180_deg);
```

### 힘과 충격량 적용

```cpp
// 지속적인 힘 적용
body.applyForce(Vec2{ 50, 0 });

// 순간적인 충격량 적용
body.applyLinearImpulse(Vec2{ 100, -100 });

// 회전 충격량 적용
body.applyAngularImpulse(1000);
```

### 조인트 (관절) 사용

#### 피벗 조인트 (회전 관절)

```cpp
// 플리퍼 생성
const Vec2 flipperAnchor = Vec2{ 150, -150 };
P2Body flipper = world.createRect(P2Dynamic, flipperAnchor, RectF{ -100, -5, 100, 10 });

// 피벗 조인트로 연결
const P2PivotJoint flipperJoint = world.createPivotJoint(ground, flipper, flipperAnchor)
    .setLimits(-10_deg, 30_deg)  // 회전 각도 제한
    .setLimitsEnabled(true);     // 제한 활성화
```

#### 거리 조인트 (줄)

```cpp
// 진자 생성
P2Body pendulum = world.createCircle(P2Dynamic, Vec2{ 0, -100 }, 20);

// 거리 조인트로 천장에 연결
const P2DistanceJoint joint = world.createDistanceJoint(
    ground, Vec2{ 0, -300 },     // 고정점
    pendulum, Vec2{ 0, -100 },   // 진자 연결점
    200                          // 거리
);
```

---

## QR 코드

### QR 코드 생성

#### 문자열로 QR 코드 만들기

```cpp
#include <Siv3D.hpp>

void Main()
{
    const String text = U"Hello, Siv3D!";
    
    // 텍스트를 QR 코드로 인코딩하고 이미지로 저장
    QR::MakeImage(QR::EncodeText(text), 10).save(U"qr.png");
    
    while (System::Update())
    {
        // 프로그램 실행 후 qr.png 파일 확인
    }
}
```

#### 실시간 QR 코드 생성기

```cpp
#include <Siv3D.hpp>

void Main()
{
    Window::Resize(1280, 720);
    
    TextEditState textEdit{ U"Hello, World!" };
    String previous;
    DynamicTexture texture;
    
    while (System::Update())
    {
        // 텍스트 입력
        SimpleGUI::TextBox(textEdit, Vec2{ 20, 20 }, 1240);
        
        // 텍스트가 변경되면 QR 코드 재생성
        if (const String current = textEdit.text; current != previous)
        {
            if (const auto qr = QR::EncodeText(current))
            {
                texture.fill(QR::MakeImage(qr).scaled(
                    Size{ 500, 500 }, 
                    InterpolationAlgorithm::Nearest
                ));
            }
            previous = current;
        }
        
        texture.drawAt(640, 400);
    }
}
```

### QR 코드 읽기

#### 이미지에서 QR 코드 읽기

```cpp
#include <Siv3D.hpp>

void Main()
{
    // QR 코드 스캐너
    const QRScanner scanner;
    
    // 이미지에서 QR 코드 읽기
    const QRContent content = scanner.scanOne(Image{ U"qr.png" });
    
    if (content)
    {
        Print << U"읽은 내용: " << content.text;
    }
    
    while (System::Update())
    {
        // 결과 확인
    }
}
```

#### 바이너리 데이터 QR 코드

```cpp
struct GameData
{
    double score;
    int32 level;
    int32 lives;
};

void Main()
{
    // 게임 데이터를 QR 코드로 저장
    const GameData data{ 12345.6, 5, 3 };
    QR::MakeImage(QR::EncodeBinary(&data, sizeof(data)), 10).save(U"gamedata.png");
    
    // 저장된 QR 코드에서 데이터 읽기
    const QRScanner scanner;
    const QRContent content = scanner.scanOne(Image{ U"gamedata.png" });
    
    if (content && (content.binary.size() == sizeof(GameData)))
    {
        GameData loadedData;
        std::memcpy(&loadedData, content.binary.data(), sizeof(loadedData));
        
        Print << U"점수: " << loadedData.score;
        Print << U"레벨: " << loadedData.level;
        Print << U"생명: " << loadedData.lives;
    }
    
    while (System::Update())
    {
        
    }
}
```

---

## TCP 통신

### 기본 개념
- **TCPServer**: 지정된 포트에서 연결을 대기
- **TCPClient**: 지정된 IP 주소와 포트에 연결
- 양쪽 모두 `.send()`와 `.read()`를 사용해 데이터 송수신

### 서버 프로그램

```cpp
#include <Siv3D.hpp>

void Main()
{
    Scene::SetBackground(ColorF{ 0.6, 0.8, 0.7 });
    
    const uint16 port = 50000;
    bool connected = false;
    
    TCPServer server;
    server.startAccept(port);
    
    Window::SetTitle(U"서버: 연결 대기 중...");
    
    Point clientPlayerPos{ 0, 0 };
    
    while (System::Update())
    {
        const Point serverPlayerPos = Cursor::Pos();
        
        // 클라이언트 연결 확인
        if (server.hasSession())
        {
            if (not connected)
            {
                connected = true;
                Window::SetTitle(U"서버: 연결됨!");
            }
            
            // 서버 플레이어 좌표 전송
            server.send(serverPlayerPos);
            
            // 클라이언트 플레이어 좌표 수신
            while (server.read(clientPlayerPos));
        }
        
        // 연결 해제 처리
        if (connected && (not server.hasSession()))
        {
            server.disconnect();
            connected = false;
            
            Window::SetTitle(U"서버: 연결 대기 중...");
            server.startAccept(port);
        }
        
        // 플레이어 그리기
        Circle{ serverPlayerPos, 30 }.draw(ColorF{ 0.1 });      // 서버 (검정)
        Circle{ clientPlayerPos, 10 }.draw();                   // 클라이언트 (흰색)
    }
}
```

### 클라이언트 프로그램

```cpp
#include <Siv3D.hpp>

void Main()
{
    const IPv4Address ip = IPv4Address::Localhost();  // 로컬호스트
    constexpr uint16 port = 50000;
    
    bool connected = false;
    TCPClient client;
    client.connect(ip, port);
    
    Window::SetTitle(U"클라이언트: 연결 시도 중...");
    
    Point serverPlayerPos{ 0, 0 };
    
    while (System::Update())
    {
        const Point clientPlayerPos = Cursor::Pos();
        
        // 서버 연결 확인
        if (client.isConnected())
        {
            if (not connected)
            {
                connected = true;
                Window::SetTitle(U"클라이언트: 연결됨!");
            }
            
            // 클라이언트 플레이어 좌표 전송
            client.send(clientPlayerPos);
            
            // 서버 플레이어 좌표 수신
            while (client.read(serverPlayerPos));
        }
        
        // 연결 오류 처리
        if (client.hasError())
        {
            client.disconnect();
            connected = false;
            
            Window::SetTitle(U"클라이언트: 재연결 시도 중...");
            client.connect(ip, port);
        }
        
        // 플레이어 그리기
        Circle{ clientPlayerPos, 30 }.draw();                   // 클라이언트 (흰색)
        Circle{ serverPlayerPos, 10 }.draw(ColorF{ 0.1 });      // 서버 (검정)
    }
}
```

### 게임 서버 개발 팁

#### 데이터 구조체 전송

```cpp
struct PlayerData
{
    Vec2 position;
    double angle;
    int32 health;
    int32 score;
};

// 서버에서
PlayerData playerData{ Cursor::PosF(), 0.0, 100, 1500 };
server.send(playerData);

// 클라이언트에서
PlayerData receivedData;
if (client.read(receivedData))
{
    // 받은 데이터 사용
    Circle{ receivedData.position, 20 }.draw();
}
```

---

## 웹캠 사용

### 연결된 웹캠 목록 확인

```cpp
#include <Siv3D.hpp>

void Main()
{
    for (const auto& info : System::EnumerateWebcams())
    {
        Print << U"[{}] {} {}"_fmt(info.cameraIndex, info.name, info.uniqueName);
    }
    
    while (System::Update())
    {
        
    }
}
```

### 기본 웹캠 사용

```cpp
#include <Siv3D.hpp>

void Main()
{
    Window::Resize(1280, 720);
    
    // 웹캠 초기화 (장치 인덱스 0, 1280x720 해상도)
    Webcam webcam{ 0, Size{ 1280, 720 }, StartImmediately::Yes };
    
    DynamicTexture texture;
    
    while (System::Update())
    {
        // macOS에서는 카메라 권한 요청으로 첫 생성이 실패할 수 있음
        #if SIV3D_PLATFORM(MACOS)
        if (not webcam)
        {
            if (SimpleGUI::Button(U"재시도", Vec2{ 20, 20 }))
            {
                webcam = Webcam{ 0, Size{ 1280, 720 }, StartImmediately::Yes };
            }
        }
        #endif
        
        // 새 프레임이 있으면 텍스처 업데이트
        if (webcam.hasNewFrame())
        {
            webcam.getFrame(texture);
        }
        
        if (texture)
        {
            texture.draw();
        }
    }
}
```

### 비동기 웹캠 초기화

```cpp
#include <Siv3D.hpp>

void Main()
{
    Window::Resize(1280, 720);
    
    // 비동기 작업으로 웹캠 초기화
    AsyncTask<Webcam> task{ []() { 
        return Webcam{ 0, Size{ 1280, 720 }, StartImmediately::Yes }; 
    }};
    
    Webcam webcam;
    DynamicTexture texture;
    
    while (System::Update())
    {
        #if SIV3D_PLATFORM(MACOS)
        if ((not webcam) && (not task.valid()))
        {
            if (SimpleGUI::Button(U"재시도", Vec2{ 20, 20 }))
            {
                task = AsyncTask{ []() { 
                    return Webcam{ 0, Size{ 1280, 720 }, StartImmediately::Yes }; 
                }};
            }
        }
        #endif
        
        // 비동기 작업 완료 확인
        if (task.isReady())
        {
            webcam = task.get();
            
            if (webcam)
            {
                Print << U"해상도: " << webcam.getResolution();
            }
        }
        
        if (webcam.hasNewFrame())
        {
            webcam.getFrame(texture);
        }
        
        // 로딩 중일 때 표시
        if (not webcam)
        {
            Circle{ 640, 360, 50 }.drawArc((Scene::Time() * 120_deg), 300_deg, 4, 4);
        }
        
        if (texture)
        {
            texture.draw();
        }
    }
}
```

### 얼굴 인식

```cpp
#include <Siv3D.hpp>

void Main()
{
    Window::Resize(1280, 720);
    
    AsyncTask<Webcam> task{ []() { 
        return Webcam{ 0, Size{ 1280, 720 }, StartImmediately::Yes }; 
    }};
    
    Webcam webcam;
    Image image;
    DynamicTexture texture;
    
    // 얼굴 인식기 (정면 얼굴용 모델)
    const CascadeClassifier faceDetector{ 
        U"example/objdetect/haarcascade/frontal_face_alt2.xml" 
    };
    
    Array<Rect> faces;
    
    while (System::Update())
    {
        if (task.isReady())
        {
            webcam = task.get();
        }
        
        if (webcam.hasNewFrame())
        {
            webcam.getFrame(image);
            
            // 얼굴 인식 (엄격도: 3, 최소 크기: 100x100)
            faces = faceDetector.detectObjects(image, 3, Size{ 100, 100 });
            
            // 얼굴 영역에 모자이크 효과
            for (const auto& face : faces)
            {
                image(face).mosaic(20);
            }
            
            texture.fill(image);
        }
        
        if (texture)
        {
            texture.draw();
        }
        
        // 인식된 얼굴에 테두리 표시
        for (const auto& face : faces)
        {
            face.drawFrame(4, Palette::Red);
        }
    }
}
```

### 웹캠으로 QR 코드 인식

```cpp
#include <Siv3D.hpp>

void Main()
{
    Window::Resize(1280, 720);
    const Font font{ FontMethod::MSDF, 48 };
    
    AsyncTask<Webcam> task{ []() { 
        return Webcam{ 0, Size{ 1280, 720 }, StartImmediately::Yes }; 
    }};
    
    Webcam webcam;
    Image image;
    DynamicTexture texture;
    
    const QRScanner qrScanner;
    Array<QRContent> qrContents;
    
    while (System::Update())
    {
        if (task.isReady())
        {
            webcam = task.get();
        }
        
        if (webcam.hasNewFrame())
        {
            webcam.getFrame(image);
            texture.fill(image);
            
            // QR 코드 스캔
            qrContents = qrScanner.scan(image);
        }
        
        if (texture)
        {
            texture.draw();
        }
        
        // 인식된 QR 코드 표시
        for (const auto& content : qrContents)
        {
            content.quad.drawFrame(4, Palette::Red);
            
            if (content.text)
            {
                const String& text = content.text;
                
                // 배경 박스
                font(text).region(20, content.quad.p0).stretched(10)
                    .draw(ColorF{ 1.0, 0.8 });
                
                // 텍스트
                font(text).draw(20, content.quad.p0, ColorF{ 0.1 });
            }
        }
    }
}
```

---

## 학습 팁

### 1. 단계적 학습 접근
- 간단한 예제부터 시작해서 점진적으로 복잡한 기능을 추가하라
- 각 기능을 개별적으로 테스트해본 후 조합하라

### 2. 디버깅 방법
- `Print << ` 문을 활용해서 변수 값을 확인하라
- 이미지나 물리 객체의 상태를 시각적으로 표시하라
- 단계별로 기능을 추가하면서 문제점을 찾아라

### 3. 최적화 고려사항
- 이미지 처리는 CPU 집약적이므로 필요할 때만 수행하라
- 물리 시뮬레이션의 업데이트 주기를 적절히 조절하라
- 네트워크 통신에서는 데이터 크기를 최소화하라

### 4. 게임 개발 활용
- 물리 엔진을 이용한 퍼즐 게임
- 웹캠을 이용한 인터랙티브 게임
- TCP 통신을 이용한 멀티플레이어 게임
- QR 코드를 이용한 데이터 공유 시스템

이 가이드를 참고해서 OpenSiv3D의 다양한 기능들을 익혀보시기 바란다. 각 섹션의 예제 코드를 직접 실행해보면서 동작을 확인하고, 필요에 따라 수정해보면 더욱 이해가 깊어질 것이다.






