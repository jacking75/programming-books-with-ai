using NetServerLib;
using System.IO;

namespace ChatServer.Packets
{
    /// <summary>
    /// チャットメッセージパケット (クライアント <-> サーバー)
    /// </summary>
    public class ChatMessagePacket : IPacket
    {
        public ushort Id => 2;
        public string Username { get; set; } // 送信者の名前
        public string Message { get; set; }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Username);
            writer.Write(Message);
        }

        public void Deserialize(BinaryReader reader)
        {
            Username = reader.ReadString();
            Message = reader.ReadString();
        }
    }
}