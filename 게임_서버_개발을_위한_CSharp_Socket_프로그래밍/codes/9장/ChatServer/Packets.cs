using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetServerLib;

namespace ChatServer;


// C -> S : 채팅 메시지 요청
public class ChatReq : IPacket
{
    public ushort Id => 101;
    public string Message { get; set; }

    public void Serialize(BinaryWriter writer) => writer.Write(Message ?? "");
    public void Deserialize(BinaryReader reader) => Message = reader.ReadString();
}

// S -> C : 채팅 메시지 브로드캐스트
public class ChatNtf : IPacket
{
    public ushort Id => 102;
    public string SenderInfo { get; set; }
    public string Message { get; set; }

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(SenderInfo ?? "");
        writer.Write(Message ?? "");
    }
    public void Deserialize(BinaryReader reader)
    {
        SenderInfo = reader.ReadString();
        Message = reader.ReadString();
    }
}

// C -> S : DB 조회 요청 (예시)
public class UserInfoReq : IPacket
{
    public ushort Id => 201;
    public string UserId { get; set; }

    public void Serialize(BinaryWriter writer) => writer.Write(UserId ?? "");
    public void Deserialize(BinaryReader reader) => UserId = reader.ReadString();
}

// S -> C : DB 조회 결과 (내부 패킷)
public class InternalUserInfoRes : IPacket
{
    public ushort Id => 202;
    public Guid ResponseSessionId { get; set; }
    public bool Found { get; set; }
    public string UserNickname { get; set; }

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(ResponseSessionId.ToByteArray());
        writer.Write(Found);
        writer.Write(UserNickname ?? "");
    }

    public void Deserialize(BinaryReader reader)
    {
        ResponseSessionId = new Guid(reader.ReadBytes(16));
        Found = reader.ReadBoolean();
        UserNickname = reader.ReadString();
    }
}
