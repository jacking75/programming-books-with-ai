using System;
using System.Collections.Generic;
using System.Text;

namespace chapter2;

/// <summary>
/// Sequence 노드: 모든 자식 노드가 성공해야 성공
/// 왼쪽에서 오른쪽으로 순서대로 실행하며, 첫 번째 실패 시 즉시 반환
/// </summary>
public class Sequence : Node
{
    private List<Node> _children = new List<Node>();

    /// <summary>
    /// 자식 노드를 추가한다.
    /// </summary>
    public void AddChild(Node child)
    {
        _children.Add(child);
    }

    /// <summary>
    /// Sequence 로직 실행
    /// </summary>
    public override NodeState Execute()
    {
        // 자식이 없으면 성공 (아무것도 실패하지 않았으므로)
        if (_children.Count == 0)
        {
            _state = NodeState.Success;
            return _state;
        }

        // 자식들을 순서대로 실행
        foreach (var child in _children)
        {
            NodeState childState = child.Execute();

            switch (childState)
            {
                case NodeState.Failure:
                    // 하나라도 실패하면 즉시 실패 반환
                    _state = NodeState.Failure;
                    return _state;

                case NodeState.Running:
                    // 실행 중이면 Running 반환
                    _state = NodeState.Running;
                    return _state;

                case NodeState.Success:
                    // 성공하면 다음 자식을 계속 실행
                    continue;
            }
        }

        // 모든 자식이 성공하면 성공
        _state = NodeState.Success;
        return _state;
    }
}
