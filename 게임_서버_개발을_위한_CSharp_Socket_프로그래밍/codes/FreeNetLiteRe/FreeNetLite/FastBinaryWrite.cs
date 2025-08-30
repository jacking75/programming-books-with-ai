using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace FreeNetLite;

public class FastBinaryWrite
{
    public static void Boolean(Span<byte> span, bool value)
    {
        span[0] = (byte)(value ? 1 : 0);
    }

    public static void Byte(Span<byte> span, byte value)
    {
        span[0] = value;
    }

    public static int Bytes(Span<byte> destination, ReadOnlySpan<byte> value)
    {
        value.CopyTo(destination);
        return value.Length;
    }

    public static int SByte(Span<byte> span, sbyte value)
    {
        span[0] = (byte)value;
        return 1;
    }

    public static int Single(Span<byte> span, in float value)
    {
        MemoryMarshal.Write(span, in value);
        return sizeof(float);
    }

    public static int Double(Span<byte> span, in double value)
    {
        MemoryMarshal.Write(span, in value);
        return sizeof(double);
    }

    public static int Int16(Span<byte> span, short value)
    {
        BinaryPrimitives.WriteInt16LittleEndian(span, value);
        return sizeof(short);
    }

    public static int Int32(Span<byte> span, int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(span, value);
        return sizeof(int);
    }

    public static int Int64(Span<byte> span, long value)
    {
        BinaryPrimitives.WriteInt64LittleEndian(span, value);
        return sizeof(long);
    }

    public static int UInt16(Span<byte> span, ushort value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(span, value);
        return sizeof(ushort);
    }

    public static int UInt32(Span<byte> span, uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(span, value);
        return sizeof(uint);
    }

    public static int UInt64(Span<byte> span, ulong value)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(span, value);
        return sizeof(ulong);
    }

    public static int String(Span<byte> span, string value)
    {
        return Encoding.UTF8.GetBytes(value, span);
    }
}