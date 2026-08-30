using MMORPG2D.Shared;

namespace MMORPG2D.GameServer;

/// <summary>
/// PacketId → 처리 함수 매핑. 1주차 학습용으로 가장 단순한 dictionary 디스패처를 쓴다.
/// 8주차에서 attribute 기반 자동 등록으로 리팩토링한다.
/// </summary>
public class PacketHandler
{
    public delegate void Handle(GameSession session, byte[] body);

    private readonly Dictionary<PacketId, Handle> _map = new();

    public void Register(PacketId id, Handle action)
    {
        if (_map.ContainsKey(id))
            throw new InvalidOperationException($"duplicate handler for {id}");
        _map[id] = action;
    }

    public void Dispatch(GameSession session, GameRequestInfo req)
    {
        if (_map.TryGetValue(req.PacketId, out var action))
        {
            try
            {
                action(session, req.Body);
            }
            catch (Exception ex)
            {
                Logger.Error($"handler {req.PacketId} threw", ex);
            }
        }
        else
        {
            Logger.Warn($"unknown packet {req.PacketId} from {session.SessionID}");
        }
    }
}
