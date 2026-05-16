using System;
using System.Collections.Generic;
using System.Text;

namespace chapter2;

/// <summary>
/// 3D 벡터 (간단한 구현)
/// </summary>
public struct Vector3
{
    public float X;
    public float Y;
    public float Z;

    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    /// 두 벡터 사이의 거리를 계산한다.
    /// </summary>
    public static float Distance(Vector3 a, Vector3 b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        float dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>
    /// 벡터의 길이를 계산한다.
    /// </summary>
    public float Length()
    {
        return MathF.Sqrt(X * X + Y * Y + Z * Z);
    }

    /// <summary>
    /// 정규화된 벡터를 반환한다 (길이 1).
    /// </summary>
    public Vector3 Normalized()
    {
        float length = Length();
        if (length > 0.0001f)
            return new Vector3(X / length, Y / length, Z / length);
        return new Vector3(0, 0, 0);
    }

    public static Vector3 operator +(Vector3 a, Vector3 b)
        => new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector3 operator -(Vector3 a, Vector3 b)
        => new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Vector3 operator *(Vector3 a, float scalar)
        => new Vector3(a.X * scalar, a.Y * scalar, a.Z * scalar);
}
