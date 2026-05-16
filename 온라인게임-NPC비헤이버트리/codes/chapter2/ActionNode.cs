using System;
using System.Collections.Generic;
using System.Text;

namespace chapter2;

/// <summary>
/// 실제 행동을 수행하는 노드들의 기본 클래스
/// </summary>
public abstract class ActionNode : Node
{
    protected Slime _slime;
    protected Func<Player> _getPlayer;

    public ActionNode(Slime slime, Func<Player> getPlayer)
    {
        _slime = slime;
        _getPlayer = getPlayer;
    }
}