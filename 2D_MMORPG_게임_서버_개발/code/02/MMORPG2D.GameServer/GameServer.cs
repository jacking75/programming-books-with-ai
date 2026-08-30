using SuperSocketLite.SocketBase;
using SuperSocketLite.SocketBase.Protocol;

namespace MMORPG2D.GameServer;

public class GameServer : AppServer<GameSession, GameRequestInfo>
{
    public GameServer()
        : base(new DefaultReceiveFilterFactory<GamePacketFilter, GameRequestInfo>())
    {
        NewSessionConnected += HandleSessionConnected;
        SessionClosed += HandleSessionClosed;
    }

    private void HandleSessionConnected(GameSession session)
    {
        Logger.Info($"[+] {session.SessionID} from {session.RemoteEndPoint}");
    }

    private void HandleSessionClosed(GameSession session, CloseReason reason)
    {
        Logger.Info($"[-] {session.SessionID} closed ({reason})");
    }
}
