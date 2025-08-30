using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetServerLib;


public class LengthPrefixedMessageFramer : IMessageFramer
{
    private readonly MemoryStream _receiveBuffer = new MemoryStream();
    private const int HeaderSize = 4; // int32

    public event Action<byte[]> MessageReceived;

    public void ProcessReceivedData(ArraySegment<byte> data)
    {
        // 수신된 데이터를 내부 버퍼에 추가합니다.
        _receiveBuffer.Write(data.Array, data.Offset, data.Count);

        // 버퍼에 완전한 메시지가 있는지 반복해서 확인합니다.
        while (true)
        {
            // 버퍼의 시작 위치로 포지션을 옮깁니다.
            _receiveBuffer.Position = 0;

            // 헤더(길이 정보)를 읽을 만큼 데이터가 충분하지 않으면 중단합니다.
            if (_receiveBuffer.Length < HeaderSize)
            {
                break;
            }

            // 메시지 길이를 읽습니다.
            byte[] lengthBuffer = new byte[HeaderSize];
            _receiveBuffer.Read(lengthBuffer, 0, HeaderSize);
            int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

            // 메시지 본문을 포함한 전체 패킷을 읽을 만큼 데이터가 충분하지 않으면 중단합니다.
            if (_receiveBuffer.Length < HeaderSize + messageLength)
            {
                break;
            }

            // 메시지 본문을 읽습니다.
            byte[] message = new byte[messageLength];
            _receiveBuffer.Read(message, 0, messageLength);

            // 완전한 메시지를 받았으므로 이벤트를 발생시킵니다.
            MessageReceived?.Invoke(message);

            // 처리된 데이터를 버퍼에서 제거합니다. (가장 효율적인 방법)
            long remainingLength = _receiveBuffer.Length - _receiveBuffer.Position;
            if (remainingLength > 0)
            {
                byte[] remainingData = new byte[remainingLength];
                _receiveBuffer.Read(remainingData, 0, (int)remainingLength);
                _receiveBuffer.SetLength(0); // 버퍼를 비웁니다.
                _receiveBuffer.Write(remainingData, 0, remainingData.Length);
            }
            else
            {
                _receiveBuffer.SetLength(0); // 모든 데이터를 처리했으면 버퍼를 비웁니다.
            }
        }

        // 처리 후 남은 데이터가 있다면, 버퍼의 시작 부분으로 데이터를 옮깁니다.
        // 위 while 루프 안에서 이미 처리되었으므로 추가 작업이 필요 없습니다.
    }

    public byte[] FrameMessage(byte[] message)
    {
        byte[] lengthPrefix = BitConverter.GetBytes(message.Length);
        byte[] framedMessage = new byte[HeaderSize + message.Length];

        Buffer.BlockCopy(lengthPrefix, 0, framedMessage, 0, HeaderSize);
        Buffer.BlockCopy(message, 0, framedMessage, HeaderSize, message.Length);

        return framedMessage;
    }
}
