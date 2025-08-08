using SuperSocketLite.Common;
using SuperSocketLite.SocketBase.Protocol;
using SuperSocketLite.SocketEngine.Protocol;


#pragma warning disable CS8618
#pragma warning disable CS8604


namespace OmokServer;

public class MemoryPackBinaryRequestInfo : BinaryRequestInfo
{
    public string SessionID;
    public byte[] Data;

    public const int PACKET_HEADER_MEMORYPACK_START_POS = 1; // 패킷 헤더 시작 위치
    public const int HEADERE_SIZE = 5 + PACKET_HEADER_MEMORYPACK_START_POS; // 전체 헤더 크기
            
    public MemoryPackBinaryRequestInfo(byte[] packetData)
        : base(null, packetData)
    {
        Data = packetData;
    }

}

public class ReceiveFilter : FixedHeaderReceiveFilter<MemoryPackBinaryRequestInfo>
{
    public ReceiveFilter() : base(MemoryPackBinaryRequestInfo.HEADERE_SIZE)
    {
    }

    protected override int GetBodyLengthFromHeader(byte[] header, int offset, int length) // header 읽고, body 길이 반환
    {
        if (!BitConverter.IsLittleEndian)
			{
            Array.Reverse(header, offset, 2);
			}
			
        var totalSize = BitConverter.ToUInt16(header, offset + MemoryPackBinaryRequestInfo.PACKET_HEADER_MEMORYPACK_START_POS);
        return totalSize - MemoryPackBinaryRequestInfo.HEADERE_SIZE;
    }

    // offset: header 데이터까지 있는 readBuffer 에서 body가 시작되는 위치를 가리킨다
    // length: body 데이터의 크기 
    protected override MemoryPackBinaryRequestInfo ResolveRequestInfo(ArraySegment<byte> header, byte[] readBuffer, int offset, int length) 
    {
        if (!BitConverter.IsLittleEndian)
		{
            Array.Reverse(header.Array, 0, MemoryPackBinaryRequestInfo.HEADERE_SIZE); // 위와 동일한 이유로 리버스 (근데 이게 반복이 아닌가? 생각하기)
        }
			
        // body 데이터가 있는 경우
		if (length > 0)
		{		
			if(offset >= MemoryPackBinaryRequestInfo.HEADERE_SIZE) // 헤더와 본문 데이터가 연속적으로 들어왔을 때
			{
				var packetStartPos = offset - MemoryPackBinaryRequestInfo.HEADERE_SIZE;
				var packetSize = length + MemoryPackBinaryRequestInfo.HEADERE_SIZE;

				return new MemoryPackBinaryRequestInfo(readBuffer.CloneRange(packetStartPos, packetSize)); // 시작과 끝을 계산하여 새로운 객체로 복제하여 생성
			}
			else // 데이터가 분할되어 도착했을 때 (헤더와 바디가 분리되어)
			{
				//offset 이 헤더 크기보다 작으므로 헤더와 보디를 직접 합쳐야 한다.
				var packetData = new Byte[length + MemoryPackBinaryRequestInfo.HEADERE_SIZE];
				header.CopyTo(packetData, 0);
				Array.Copy(readBuffer, offset, packetData, MemoryPackBinaryRequestInfo.HEADERE_SIZE, length);
					
				return new MemoryPackBinaryRequestInfo(packetData);
			}
		}	
			
        // body 데이터가 없는 경우
		return new MemoryPackBinaryRequestInfo(header.CloneRange(header.Offset, header.Count));
    }
}
