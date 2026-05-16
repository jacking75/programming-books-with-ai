using System;
using System.Collections.Generic;
using System.Text;

namespace chapter2;

/// <summary>
/// 플레이어가 일정 거리 내에 있는지 확인하는 조건 노드
/// </summary>
public class CheckPlayerNearby : ActionNode
{
    private float _detectionRange;

    public CheckPlayerNearby(Slime slime, Func<Player> getPlayer, float detectionRange)
        : base(slime, getPlayer)
    {
        _detectionRange = detectionRange;
    }

    public override NodeState Execute()
    {
        Player player = _getPlayer();

        if (player == null)
        {
            _state = NodeState.Failure;
            return _state;
        }

        float distance = Vector3.Distance(_slime.Position, player.Position);

        if (distance <= _detectionRange)
        {
            Console.WriteLine($"[{_slime.Name}] 플레이어 발견! (거리: {distance:F2}m)");
            _state = NodeState.Success;
        }
        else
        {
            _state = NodeState.Failure;
        }

        return _state;
    }
}
