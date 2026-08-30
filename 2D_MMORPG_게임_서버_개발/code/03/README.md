# 3주차 코드

2주차에서 출발해 다음이 추가되었다.

## 추가된 부분

- **Shared**: `Packets/GamePackets.cs` (ReqEnterGame, ResEnterGame, ReqMove, ResMove)
- **GameServer**: `Game/PlayerCharacter.cs`, `Game/World.cs`, `Handlers/EnterGameHandler.cs`, `Handlers/MoveHandler.cs`. 세션 종료 시점 좌표 자동 저장.
- **Client**: `Net/GameNetClient.cs` 추가 + `Program.cs` 가 API 로그인 + 게임 서버 입장 + WASD 콘솔 이동 데모로 변경.

## 실행

1. ApiServer 와 GameServer 를 실행한다.
2. Client 를 실행해 2주차에서 만든 계정으로 로그인한다.
3. WASD 입력 시마다 콘솔에 새 좌표가 찍히면 OK이다.
