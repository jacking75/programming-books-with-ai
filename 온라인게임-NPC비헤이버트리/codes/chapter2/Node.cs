using System;
using System.Collections.Generic;
using System.Text;

namespace chapter2;


/// <summary>
/// 비헤이버트리 노드의 실행 결과 상태
/// </summary>
public enum NodeState
{
    Running,  // 아직 실행 중
    Success,  // 성공적으로 완료됨
    Failure   // 실패함
}



/// <summary>
/// 비헤이버트리의 모든 노드가 상속받는 기본 클래스
/// </summary>
public abstract class Node
{
    /// <summary>
    /// 현재 노드의 실행 상태
    /// </summary>
    protected NodeState _state;

    /// <summary>
    /// 노드를 실행한다. 모든 하위 클래스는 이 메서드를 구현해야 한다.
    /// </summary>
    /// <returns>실행 결과 상태</returns>
    public abstract NodeState Execute();

    /// <summary>
    /// 노드의 현재 상태를 반환한다.
    /// </summary>
    public NodeState State => _state;
}