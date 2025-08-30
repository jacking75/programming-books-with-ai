using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetServerLib;

namespace ChatServer;


public class User
{
    public ISession Session { get; }
    public string Name { get; }

    public User(ISession session)
    {
        Session = session;
        Name = $"User-{session.Id.ToString().Substring(0, 4)}";
    }
}