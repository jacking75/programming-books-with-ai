namespace PvPGameServer.Ecs.Components;

public sealed class NetworkSessionComponent : IComponent
{
    public NetworkSessionComponent(string sessionId)
    {
        SessionId = sessionId;
    }

    public string SessionId { get; private set; }

    public void UpdateSession(string sessionId)
    {
        SessionId = sessionId;
    }
}
