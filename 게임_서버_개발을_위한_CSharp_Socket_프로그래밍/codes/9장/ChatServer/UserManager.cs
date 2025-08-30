using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetServerLib;

namespace ChatServer;

public class UserManager
{
    private readonly ConcurrentDictionary<Guid, User> _users = new();

    public void OnSessionConnected(ISession session)
    {
        var user = new User(session);
        _users.TryAdd(session.Id, user);
        Console.WriteLine($"{user.Name} connected. Total users: {_users.Count}");
    }

    public void OnSessionDisconnected(ISession session)
    {
        if (_users.TryRemove(session.Id, out var user))
        {
            Console.WriteLine($"{user.Name} disconnected. Total users: {_users.Count}");
        }
    }

    public User GetUser(Guid sessionId)
    {
        _users.TryGetValue(sessionId, out var user);
        return user;
    }

    public IEnumerable<User> GetAllUsers()
    {
        return _users.Values;
    }
}
