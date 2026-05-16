using System;
using System.Collections.Generic;
using System.Text;

namespace chapter2;

/// <summary>
/// 슬라임의 비헤이버트리를 구성하는 클래스
/// </summary>
public class SlimeAI
{
    private Slime _slime;
    private Node _rootNode;
    private Func<Player> _getPlayer;

    public SlimeAI(Slime slime, Func<Player> getPlayer)
    {
        _slime = slime;
        _getPlayer = getPlayer;

        BuildBehaviorTree();
    }

    /// <summary>
    /// 비헤이버트리 구조를 생성한다.
    /// </summary>
    private void BuildBehaviorTree()
    {
        /*
         * 트리 구조:
         * 
         * Selector (루트)
         * ├─ Sequence (플레이어가 가까우면 도망)
         * │  ├─ CheckPlayerNearby
         * │  └─ FleeFromPlayer
         * └─ Wander (기본 행동)
         */

        // 도망가기 시퀀스
        var fleeSequence = new Sequence();
        fleeSequence.AddChild(new CheckPlayerNearby(_slime, _getPlayer, 5f));
        fleeSequence.AddChild(new FleeFromPlayer(_slime, _getPlayer, 8f));

        // 루트 셀렉터
        var rootSelector = new Selector();
        rootSelector.AddChild(fleeSequence);
        rootSelector.AddChild(new Wander(_slime, _getPlayer));

        _rootNode = rootSelector;
    }

    /// <summary>
    /// AI를 한 프레임 업데이트한다.
    /// </summary>
    public void Update()
    {
        if (_rootNode != null)
        {
            _rootNode.Execute();
        }
    }
}