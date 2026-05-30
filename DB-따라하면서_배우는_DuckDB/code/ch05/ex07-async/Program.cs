// code/ch05/ex07-async/Program.cs
using DuckDB.NET.Data;

// -------------------------------------------------------
// 비동기 패턴 기본 예제
// -------------------------------------------------------
await RunAsyncExampleAsync();

// -------------------------------------------------------
// PlayerService를 사용한 비동기 패턴 예제
// -------------------------------------------------------
await RunPlayerServiceExampleAsync();


async Task RunAsyncExampleAsync()
{
    using var connection = new DuckDBConnection("DataSource=game_analytics.duckdb");

    // OpenAsync()로 비동기 연결 열기
    await connection.OpenAsync();
    Console.WriteLine("비동기 연결 완료");

    using var command = connection.CreateCommand();

    // 1. ExecuteNonQueryAsync — 비동기 DDL/DML 실행
    command.CommandText = @"
        CREATE TABLE IF NOT EXISTS async_test (
            id      INTEGER PRIMARY KEY,
            message VARCHAR
        )";
    await command.ExecuteNonQueryAsync();

    // 2. 비동기 데이터 삽입
    command.CommandText = "INSERT OR IGNORE INTO async_test VALUES ($id, $message)";
    command.Parameters.Add(new DuckDBParameter("id",      1));
    command.Parameters.Add(new DuckDBParameter("message", "비동기 테스트"));
    await command.ExecuteNonQueryAsync();

    // 3. ExecuteScalarAsync — 비동기 단일 값 조회
    command.CommandText = "SELECT COUNT(*) FROM async_test";
    command.Parameters.Clear();
    long count = (long)(await command.ExecuteScalarAsync())!;
    Console.WriteLine($"비동기 COUNT: {count}");

    // 4. ExecuteReaderAsync — 비동기 쿼리 결과 읽기
    command.CommandText = "SELECT id, message FROM async_test";
    using var reader = (DuckDBDataReader)await command.ExecuteReaderAsync();

    Console.WriteLine("=== 비동기 조회 결과 ===");
    while (await reader.ReadAsync())  // ReadAsync()로 비동기 행 읽기
    {
        Console.WriteLine($"  [{reader.GetInt32(0)}] {reader.GetString(1)}");
    }
}

async Task RunPlayerServiceExampleAsync()
{
    // PlayerService 초기화
    using var service = new PlayerService("game_analytics.duckdb");
    await service.InitializeAsync();

    // 플레이어 조회
    var player = await service.GetPlayerByIdAsync(1001);
    if (player is not null)
    {
        Console.WriteLine($"\n플레이어 조회: {player.PlayerName} (Lv.{player.Level})");
    }

    // 상위 플레이어 목록 조회
    var topPlayers = await service.GetTopPlayersAsync(serverId: 1, topN: 5);
    Console.WriteLine("\n=== 서버 1 TOP 5 플레이어 ===");
    for (int i = 0; i < topPlayers.Count; i++)
    {
        Console.WriteLine($"  {i + 1}위. {topPlayers[i].PlayerName} (Lv.{topPlayers[i].Level})");
    }

    // 골드 업데이트
    bool success = await service.UpdatePlayerGoldAsync(1001, -500);
    Console.WriteLine($"\n골드 차감 {(success ? "성공" : "실패")}: 플레이어 1001, -500골드");

    // 단일 연결에서 순차 실행
    var p1001 = await service.GetPlayerByIdAsync(1001);
    var p1002 = await service.GetPlayerByIdAsync(1002);

    Console.WriteLine($"\n순차 조회 완료: {p1001?.PlayerName}, {p1002?.PlayerName}");
}
