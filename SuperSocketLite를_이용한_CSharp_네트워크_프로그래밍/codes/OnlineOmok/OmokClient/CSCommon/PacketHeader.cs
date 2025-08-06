using MemoryPack;
using System;
using System.Collections.Generic;
using System.Text;

namespace CSCommon
{
    public struct MemoryPackPacketHeadInfo
    {
        const int PacketHeaderMemoryPackStartPos = 1;
        public const int HeadSize = 6;

        public UInt16 TotalSize;
        public UInt16 Id;
        public byte Type;


        public static UInt16 GetTotalSize(byte[] data, int startPos)
        {
            return FastBinaryRead.UInt16(data, startPos + PacketHeaderMemoryPackStartPos);
        }

        public static void WritePacketId(byte[] data, UInt16 packetId)
        {
            FastBinaryWrite.UInt16(data, PacketHeaderMemoryPackStartPos + 2, packetId);
        }

        public void Read(byte[] headerData)
        {
            var pos = PacketHeaderMemoryPackStartPos;

            TotalSize = FastBinaryRead.UInt16(headerData, pos);
            pos += 2;

            Id = FastBinaryRead.UInt16(headerData, pos);
            pos += 2;

            Type = headerData[pos];
            pos += 1;
        }

        public void Write(byte[] binary)
        {
            var pos = PacketHeaderMemoryPackStartPos;

            FastBinaryWrite.UInt16(binary, pos, TotalSize);
            pos += 2;

            FastBinaryWrite.UInt16(binary, pos, Id);
            pos += 2;

            binary[pos] = Type;
            pos += 1;
        }

        public void DebugConsolOutHeaderInfo()
        {
            Console.WriteLine("DebugConsolOutHeaderInfo");
            Console.WriteLine("TotalSize : " + TotalSize);
            Console.WriteLine("Id : " + Id);
            Console.WriteLine("Type : " + Type);
        }
    }

    [MemoryPackable]
    public partial class PkHeader
    {
        public UInt16 TotalSize { get; set; } = 0;
        public UInt16 Id { get; set; } = 0;
        public byte Type { get; set; } = 0; // 속성 : 암호화? 압축? 
    }
}
