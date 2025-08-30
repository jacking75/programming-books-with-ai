using NetServerLib;
using System.IO;

namespace ChatServer.Packets
{
    /// <summary>
    /// ログイン要求パケット (クライアント -> サーバー)
    /// </summary>
    public class LoginRequestPacket : IPacket
    {
        public ushort Id => 1;
        public string Username { get; set; }

        public void Serialize(BinaryWriter writer) => writer.Write(Username);
        public void Deserialize(BinaryReader reader) => Username = reader.ReadString();
    }
}