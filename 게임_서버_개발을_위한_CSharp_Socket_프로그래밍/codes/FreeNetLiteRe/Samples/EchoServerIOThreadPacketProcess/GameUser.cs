using System;
using FreeNetLite;

namespace EchoServerIOThreadPacketProcess;

/// <summary>
/// 하나의 session객체를 나타낸다.
/// </summary>
class GameUser
{
	Session _session;

	public GameUser(Session session)
	{
		_session = session;
	}
			
	
}
