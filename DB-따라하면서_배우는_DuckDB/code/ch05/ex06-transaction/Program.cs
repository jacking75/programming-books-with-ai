// code/ch05/ex06-transaction/Program.cs
using DuckDB.NET.Data;

using var connection = new DuckDBConnection("DataSource=game_analytics.duckdb");
connection.Open();

// 테이블 및 테스트 데이터 준비
{
    using var setupCmd = connection.CreateCommand();
    setupCmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS player (
            player_id       BIGINT       PRIMARY KEY,
            player_name     VARCHAR(50)  NOT NULL,
            level           INTEGER      NOT NULL DEFAULT 1,
            experience      BIGINT       NOT NULL DEFAULT 0,
            gold            BIGINT       NOT NULL DEFAULT 0,
            server_id       INTEGER      NOT NULL DEFAULT 1,
            created_at      TIMESTAMP    NOT NULL DEFAULT current_timestamp,
            last_login_at   TIMESTAMP
        )";
    setupCmd.ExecuteNonQuery();

    setupCmd.CommandText = @"
        CREATE SEQUENCE IF NOT EXISTS seq_battle_log_id START 1";
    setupCmd.ExecuteNonQuery();

    setupCmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS battle_log (
            log_id          BIGINT       PRIMARY KEY DEFAULT nextval('seq_battle_log_id'),
            player_id       BIGINT       NOT NULL,
            monster_id      INTEGER      NOT NULL,
            damage_dealt    INTEGER      NOT NULL DEFAULT 0,
            damage_taken    INTEGER      NOT NULL DEFAULT 0,
            is_victory      BOOLEAN      NOT NULL DEFAULT false,
            battle_duration DOUBLE,
            logged_at       TIMESTAMP    NOT NULL DEFAULT current_timestamp
        )";
    setupCmd.ExecuteNonQuery();

    setupCmd.CommandText = @"
        INSERT OR IGNORE INTO player (player_id, player_name, level, experience, gold, server_id)
        VALUES
            (1001, '홍길동',   51, 130000, 31000, 1),
            (1002, '이순신',   75, 280000, 56000, 1)";
    setupCmd.ExecuteNonQuery();
}

// -------------------------------------------------------
// 기본 트랜잭션 패턴
// -------------------------------------------------------

// BeginTransaction()으로 트랜잭션 시작
using var transaction = connection.BeginTransaction();

try
{
    using var command = connection.CreateCommand();

    // 트랜잭션 내에서 실행할 명령에 트랜잭션을 연결한다.
    command.Transaction = transaction;

    // 1단계: 골드 송금 - 보내는 플레이어 골드 차감
    command.CommandText = @"
        UPDATE player
        SET gold = gold - $amount
        WHERE player_id = $senderId AND gold >= $amount";
    command.Parameters.Add(new DuckDBParameter("amount",   5000L));
    command.Parameters.Add(new DuckDBParameter("senderId", 1001L));

    int rowsAffected = command.ExecuteNonQuery();
    if (rowsAffected == 0)
    {
        // 골드가 부족하거나 플레이어가 없는 경우
        throw new InvalidOperationException("골드가 부족하거나 플레이어를 찾을 수 없습니다.");
    }

    // 2단계: 골드 송금 - 받는 플레이어 골드 증가
    command.CommandText = @"
        UPDATE player
        SET gold = gold + $amount
        WHERE player_id = $receiverId";
    command.Parameters.Clear();
    command.Parameters.Add(new DuckDBParameter("amount",     5000L));
    command.Parameters.Add(new DuckDBParameter("receiverId", 1002L));

    rowsAffected = command.ExecuteNonQuery();
    if (rowsAffected == 0)
    {
        throw new InvalidOperationException("수신 플레이어를 찾을 수 없습니다.");
    }

    // 모든 작업이 성공하면 커밋 — 변경사항을 영구적으로 저장
    transaction.Commit();
    Console.WriteLine("골드 송금 완료: 1001 → 1002, 5,000골드");
}
catch (Exception ex)
{
    // 오류가 발생하면 롤백 — 모든 변경사항을 되돌림
    transaction.Rollback();
    Console.WriteLine($"오류로 인해 롤백: {ex.Message}");
}

// 배치 삽입 예제 실행
BatchInsertExample.Run(connection);

// TransactionHelper 사용 예제
DuckDBTransactionHelper.ExecuteInTransaction(connection, conn =>
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "UPDATE player SET gold = gold - 500 WHERE player_id = 1001";
    cmd.ExecuteNonQuery();
    Console.WriteLine("TransactionHelper를 통한 골드 차감 완료");
});
