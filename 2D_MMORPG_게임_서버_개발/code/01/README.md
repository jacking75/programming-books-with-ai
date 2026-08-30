# 1주차 코드

## 빌드

```bash
dotnet restore
dotnet build
```

## 실행 순서

1. MySQL 서버, Redis 서버가 떠 있는지 확인한다.
2. `MMORPG2D.GameServer/appsettings.json`의 연결 문자열을 본인 환경에 맞춘다.
3. GameServer 실행:
   ```bash
   cd MMORPG2D.GameServer
   dotnet run
   ```
4. 다른 터미널에서 Client 실행:
   ```bash
   cd MMORPG2D.Client
   dotnet run
   ```

## 1주차 구성 요약

- `MMORPG2D.Shared` — 패킷 정의, 인코더 (서버/클라 공통)
- `MMORPG2D.GameServer` — SuperSocketLite TCP 서버 (포트 7777, 에코 처리)
- `MMORPG2D.ApiServer` — 2주차에 사용 시작 (지금은 빈 껍데기)
- `MMORPG2D.Client` — 콘솔 기반 에코 클라이언트 (2주차부터 MonoGame UI 추가)

## 데이터베이스

```sql
CREATE DATABASE mmorpg2d
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;
```

1주차에는 테이블이 없다. 다음 주에 users / worlds / characters를 추가한다.
