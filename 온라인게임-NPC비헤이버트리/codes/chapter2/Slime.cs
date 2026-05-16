using System;
using System.Collections.Generic;
using System.Text;

namespace chapter2;

/// <summary>
/// 슬라임 엔티티
/// </summary>
public class Slime
{
    public Vector3 Position { get; set; }
    public float Speed { get; set; } = 2.0f;
    public string Name { get; set; }

    // 배회 관련
    public Vector3? WanderTarget { get; set; }
    public float WanderRadius { get; set; } = 10f;

    public Slime(string name, Vector3 position)
    {
        Name = name;
        Position = position;
    }

    /// <summary>
    /// 특정 방향으로 이동한다.
    /// </summary>
    public void Move(Vector3 direction, float deltaTime)
    {
        Position += direction.Normalized() * Speed * deltaTime;
    }
}