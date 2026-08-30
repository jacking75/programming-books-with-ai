namespace MMORPG2D.ApiServer.Models;

// SqlKata 매핑용. 컬럼 이름을 그대로 따라간다.
public class UserRow
{
    public long Id { get; set; }
    public string Login_Id { get; set; } = "";
    public string Pw_Hash { get; set; } = "";
    public DateTime Created_At { get; set; }
    public DateTime? Last_Login_At { get; set; }
}

public class WorldRow
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool Is_Open { get; set; }
    public int Capacity { get; set; }
}

public class CharacterRow
{
    public long Id { get; set; }
    public long Account_Id { get; set; }
    public int World_Id { get; set; }
    public string Name { get; set; } = "";
    public int Level { get; set; }
    public long Exp { get; set; }
    public int Pos_X { get; set; }
    public int Pos_Y { get; set; }
}
