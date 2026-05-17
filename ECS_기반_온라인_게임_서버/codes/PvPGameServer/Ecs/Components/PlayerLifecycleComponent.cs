namespace PvPGameServer.Ecs.Components;

public enum PlayerLifecycleState
{
    Disconnected = 0,
    Connected = 1,
    LoggedIn = 2,
    InRoom = 3,
}

public sealed class PlayerLifecycleComponent : IComponent
{
    public PlayerLifecycleComponent(PlayerLifecycleState initialState)
    {
        State = initialState;
    }

    public PlayerLifecycleState State { get; private set; }

    public void SetState(PlayerLifecycleState newState)
    {
        State = newState;
    }
}
