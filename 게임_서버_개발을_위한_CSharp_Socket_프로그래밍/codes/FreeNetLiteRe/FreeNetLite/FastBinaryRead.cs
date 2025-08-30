using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;


namespace FreeNetLite;

public class FastBinaryRead
{
    public static bool Boolean(ReadOnlySpan<byte> span)
    {
        return span[0] != 0;
    }

    public static byte Byte(ReadOnlySpan<byte> span)
    {
        return span[0];
    }

    public static ReadOnlySpan<byte> Bytes(ReadOnlySpan<byte> span, int count)
    {
        return span.Slice(0, count);
    }

    public static sbyte SByte(ReadOnlySpan<byte> span)
    {
        return (sbyte)span[0];
    }

    public static float Single(ReadOnlySpan<byte> span)
    {
        return MemoryMarshal.Read<float>(span);
    }

    public static double Double(ReadOnlySpan<byte> span)
    {
        return MemoryMarshal.Read<double>(span);
    }

    public static short Int16(ReadOnlySpan<byte> span)
    {
        return BinaryPrimitives.ReadInt16LittleEndian(span);
    }

    public static int Int32(ReadOnlySpan<byte> span)
    {
        return BinaryPrimitives.ReadInt32LittleEndian(span);
    }

    public static long Int64(ReadOnlySpan<byte> span)
    {
        return BinaryPrimitives.ReadInt64LittleEndian(span);
    }

    public static ushort UInt16(ReadOnlySpan<byte> span)
    {
        return BinaryPrimitives.ReadUInt16LittleEndian(span);
    }

    public static uint UInt32(ReadOnlySpan<byte> span)
    {
        return BinaryPrimitives.ReadUInt32LittleEndian(span);
    }

    public static ulong UInt64(ReadOnlySpan<byte> span)
    {
        return BinaryPrimitives.ReadUInt64LittleEndian(span);
    }

    public static string String(ReadOnlySpan<byte> span)
    {
        return Encoding.UTF8.GetString(span);
    }
}