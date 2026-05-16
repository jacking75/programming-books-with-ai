using System;
using System.Collections.Generic;
using System.Text;

namespace chapter2;

/// <summary>
/// Selector 노드: 자식 노드들 중 하나라도 성공하면 성공
/// 왼쪽에서 오른쪽으로 순서대로 실행하며, 첫 번째 성공 시 즉시 반환
/// </summary>
public class Selector : Node
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
    /// Selector 로직 실행
    /// </summary>
    public override NodeState Execute()
    {
        // 자식이 없으면 실패
        if (_children.Count == 0)
        {
            _state = NodeState.Failure;
            return _state;
        }

        // 자식들을 순서대로 실행
        foreach (var child in _children)
        {
            NodeState childState = child.Execute();

            switch (childState)
            {
                case NodeState.Success:
                    // 하나라도 성공하면 즉시 성공 반환
                    _state = NodeState.Success;
                    return _state;

                case NodeState.Running:
                    // 실행 중이면 Running 반환 (다음 프레임에 계속)
                    _state = NodeState.Running;
                    return _state;

                case NodeState.Failure:
                    // 실패하면 다음 자식을 시도
                    continue;
            }
        }

        // 모든 자식이 실패하면 실패
        _state = NodeState.Failure;
        return _state;
    }
}
