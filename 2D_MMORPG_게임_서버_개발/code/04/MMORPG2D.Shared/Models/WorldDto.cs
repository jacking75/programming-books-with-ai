namespace MMORPG2D.Shared.Models;

public class WorldDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsOpen { get; set; }
    public int Capacity { get; set; }
}

public class CharacterDto
{
    public long Id { get; set; }
    public int WorldId { get; set; }
    public string Name { get; set; } = "";
    public int Level { get; set; }
    public long Exp { get; set; }
    public int PosX { get; set; }
    public int PosY { get; set; }
}
