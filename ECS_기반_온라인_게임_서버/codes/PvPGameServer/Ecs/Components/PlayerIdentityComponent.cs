namespace PvPGameServer.Ecs.Components;

public sealed class PlayerIdentityComponent : IComponent
{
    public PlayerIdentityComponent(ulong sequenceNumber, string userId)
    {
        SequenceNumber = sequenceNumber;
        UserId = userId;
    }

    public ulong SequenceNumber { get; private set; }

    public string UserId { get; private set; }

    public void Update(ulong sequenceNumber, string userId)
    {
        SequenceNumber = sequenceNumber;
        UserId = userId;
    }
}
