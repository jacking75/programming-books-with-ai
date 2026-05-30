// code/ch05/ex04-mapping/Extensions.cs
using DuckDB.NET.Data;

public static class DataReaderExtensions
{
    // DuckDBDataReader에서 Player 객체를 생성하는 확장 메서드
    public static Player ToPlayer(this DuckDBDataReader reader) => new(
        PlayerId:    reader.GetInt64(0),
        PlayerName:  reader.GetString(1),
        Level:       reader.GetInt32(2),
        Experience:  reader.GetInt64(3),
        Gold:        reader.GetInt64(4),
        ServerId:    reader.GetInt32(5),
        CreatedAt:   reader.GetDateTime(6),
        LastLoginAt: reader.IsDBNull(7) ? null : reader.GetDateTime(7)
    );

    // 제네릭 방식으로 전체 결과를 리스트로 변환
    public static List<T> ToList<T>(this DuckDBDataReader reader, Func<DuckDBDataReader, T> mapper)
    {
        var list = new List<T>();
        while (reader.Read())
            list.Add(mapper(reader));
        return list;
    }
}
