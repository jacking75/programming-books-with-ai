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
            _receiveBuffer.Write(data.Array, data.Offset, data.Count);

            while (true)
            {
                _receiveBuffer.Position = 0;

                if (_receiveBuffer.Length < HeaderSize)
                {
                    break;
                }

                byte[] lengthBuffer = new byte[HeaderSize];
                _receiveBuffer.Read(lengthBuffer, 0, HeaderSize);
                int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

                if (_receiveBuffer.Length < HeaderSize + messageLength)
                {
                    break;
                }

                byte[] message = new byte[messageLength];
                _receiveBuffer.Read(message, 0, messageLength);

                MessageReceived?.Invoke(message);

                long remainingLength = _receiveBuffer.Length - _receiveBuffer.Position;
                if (remainingLength > 0)
                {
                    byte[] remainingData = new byte[remainingLength];
                    _receiveBuffer.Read(remainingData, 0, (int)remainingLength);
                    _receiveBuffer.SetLength(0);
                    _receiveBuffer.Write(remainingData, 0, remainingData.Length);
                }
                else
                {
                    _receiveBuffer.SetLength(0);
                }
            }
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