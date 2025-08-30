using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeNetLite;


/// <summary>
/// 네트워크 통신에 사용되는 데이터 패킷을 나타냅니다.
/// </summary>
public record Packet(Session Owner, ushort Id, ReadOnlyMemory<byte>? BodyData);

