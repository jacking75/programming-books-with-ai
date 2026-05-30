// code/ch05/ex06-transaction/TransactionHelper.cs
using DuckDB.NET.Data;

public static class DuckDBTransactionHelper
{
    /// <summary>
    /// 트랜잭션 내에서 주어진 작업을 실행한다.
    /// 성공 시 커밋, 실패 시 롤백한다.
    /// </summary>
    public static void ExecuteInTransaction(
        DuckDBConnection connection,
        Action<DuckDBConnection> action)
    {
        using var transaction = connection.BeginTransaction();
        try
        {
            action(connection);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;  // 예외를 다시 던져서 호출자가 처리하도록 한다.
        }
    }
}
