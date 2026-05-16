using System;
using System.Collections.Generic;
using System.Text;

namespace chapter2;

/// <summary>
/// 플레이어를 나타내는 클래스
/// </summary>
public class Player
{
    public Vector3 Position { get; set; }
    public string Name { get; set; }

    public Player(string name, Vector3 position)
    {
        Name = name;
        Position = position;
    }
}