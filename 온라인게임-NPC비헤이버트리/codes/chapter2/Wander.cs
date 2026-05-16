using System;
using System.Collections.Generic;
using System.Text;

namespace chapter2;

/// <summary>
/// 무작위 위치로 배회하는 행동 노드
/// </summary>
public class Wander : ActionNode
{
    private Random _random = new Random();

    public Wander(Slime slime, Func<Player> getPlayer)
        : base(slime, getPlayer)
    {
    }

    public override NodeState Execute()
    {
        // 배회 목표가 없으면 새로 설정
        if (_slime.WanderTarget == null)
        {
            // 현재 위치 기준 랜덤한 방향
            float angle = (float)(_random.NextDouble() * Math.PI * 2);
            float distance = (float)(_random.NextDouble() * _slime.WanderRadius);

            Vector3 offset = new Vector3(
                MathF.Cos(angle) * distance,
                0,
                MathF.Sin(angle) * distance
            );

            _slime.WanderTarget = _slime.Position + offset;
            Console.WriteLine($"[{_slime.Name}] 배회 시작: ({_slime.WanderTarget.Value.X:F1}, {_slime.WanderTarget.Value.Z:F1})");
        }

        // 목표 지점으로 이동
        Vector3 direction = _slime.WanderTarget.Value - _slime.Position;
        float distanceToTarget = direction.Length();

        if (distanceToTarget < 0.5f)
        {
            // 목표 지점에 도착, 잠시 대기하고 새 목표 설정
            Console.WriteLine($"[{_slime.Name}] 배회 지점 도착");
            _slime.WanderTarget = null;
            _state = NodeState.Success;
            return _state;
        }

        // 아직 이동 중
        _slime.Move(direction, 0.1f);
        _state = NodeState.Running;
        return _state;
    }
}