using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetServerLib;

namespace ChatServer;


public class PacketHandler
{
    private readonly BlockingCollection<(ISession, byte[])> _packetQueue = new();
    private readonly PacketProcessor _packetProcessor;
    private readonly DBHandler _dbHandler;
    private readonly UserManager _userManager;

    public PacketHandler(PacketProcessor packetProcessor, DBHandler dbHandler, UserManager userManager)
    {
        _packetProcessor = packetProcessor;
        _dbHandler = dbHandler;
        _userManager = userManager;
    }

    public void EnqueuePacket(ISession session, byte[] data)
    {
        if (!_packetQueue.IsAddingCompleted)
        {
            try
            {
                _packetQueue.Add((session, data));
            }
            catch (InvalidOperationException) { /* Queue is closed */ }
        }
    }

    public void Process()
    {
        while (!_packetQueue.IsCompleted)
        {
            try
            {
                var (session, data) = _packetQueue.Take();
                _packetProcessor.ProcessPacket(data, session);
            }
            catch (InvalidOperationException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Packet processing error: {ex.Message}");
            }
        }
    }

    public void Stop()
    {
        _packetQueue.CompleteAdding();
    }

    public void HandleChatReq(ChatReq packet, ISession session)
    {
        var user = _userManager.GetUser(session.Id);
        if (user == null) return;

        Console.WriteLine($"Received from {user.Name}: {packet.Message}");

        var broadcastPacket = new ChatNtf
        {
            SenderInfo = user.Name,
            Message = packet.Message
        };

        foreach (var otherUser in _userManager.GetAllUsers())
        {
            if (otherUser.Session.IsConnected)
            {
                otherUser.Session.Send(broadcastPacket);
            }
        }
    }

    public void HandleUserInfoReq(UserInfoReq packet, ISession session)
    {
        Console.WriteLine($"Received user info request for {packet.UserId}");
        _dbHandler.EnqueueRequest(() =>
        {
            Thread.Sleep(500); // DB I/O 지연 흉내
            var found = packet.UserId == "test";
            var nickname = found ? "TestUserNickname" : "Unknown";

            var resultPacket = new InternalUserInfoRes
            {
                ResponseSessionId = session.Id,
                Found = found,
                UserNickname = nickname
            };
            _dbHandler.EnqueueResult(session, resultPacket);
        });
    }

    public void HandleInternalUserInfoRes(InternalUserInfoRes packet, ISession originalSession)
    {
        var user = _userManager.GetUser(packet.ResponseSessionId);
        if (user == null || !user.Session.IsConnected) return;

        Console.WriteLine($"Sending DB result to {user.Name}");

        var responsePacket = new ChatNtf
        {
            SenderInfo = "SYSTEM",
            Message = packet.Found
                ? $"User found. Nickname: {packet.UserNickname}"
                : "User not found."
        };

        user.Session.Send(responsePacket);
    }
}
