using System;
using System.Collections.Generic;
using System.Text;

namespace chapter2;

/// <summary>
/// 플레이어 반대 방향으로 도망가는 행동 노드
/// </summary>
public class FleeFromPlayer : ActionNode
{
    private float _fleeDistance;
    private Vector3? _fleeTarget;

    public FleeFromPlayer(Slime slime, Func<Player> getPlayer, float fleeDistance = 8f)
        : base(slime, getPlayer)
    {
        _fleeDistance = fleeDistance;
    }

    public override NodeState Execute()
    {
        Player player = _getPlayer();

        if (player == null)
        {
            _state = NodeState.Failure;
            return _state;
        }

        // 도망갈 목표 지점을 아직 설정하지 않았으면 설정
        if (_fleeTarget == null)
        {
            // 플레이어 반대 방향으로 도망갈 지점 계산
            Vector3 fleeDirection = (_slime.Position - player.Position).Normalized();
            _fleeTarget = _slime.Position + fleeDirection * _fleeDistance;
            Console.WriteLine($"[{_slime.Name}] 도망간다! 목표: ({_fleeTarget.Value.X:F1}, {_fleeTarget.Value.Z:F1})");
        }

        // 목표 지점으로 이동
        Vector3 direction = _fleeTarget.Value - _slime.Position;
        float distanceToTarget = direction.Length();

        if (distanceToTarget < 0.5f)
        {
            // 목표 지점에 도착
            Console.WriteLine($"[{_slime.Name}] 도망 완료!");
            _fleeTarget = null;
            _state = NodeState.Success;
            return _state;
        }

        // 아직 이동 중
        _slime.Move(direction, 0.1f); // deltaTime을 0.1로 가정
        _state = NodeState.Running;
        return _state;
    }
}