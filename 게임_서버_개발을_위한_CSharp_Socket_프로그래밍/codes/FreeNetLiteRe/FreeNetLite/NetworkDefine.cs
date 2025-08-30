using System;
using System.Collections.Generic;
using System.Text;

namespace FreeNetLite;

/// <summary>
/// 네트워크 시스템에서 사용하는 상수들을 정의합니다.
/// </summary>
public static class NetworkDefine
{
    public const ushort SysNtfConnected = 1;
    public const ushort SysNtfClosed = 2;
    public const ushort SysStartHeartbeat = 3;

    /// <summary>
    /// 시스템 패킷 ID의 최대값입니다. 이 값 이하의 ID는 클라이언트에서 전송할 수 없습니다.
    /// </summary>
    public const ushort SysNtfMax = 100;
}




