// 파일: samples/src/Chapter07/Program.cs
// 7장 — Async socket wrapper
// loopback에서 echo 서버를 띄우고 1회 ping-pong을 한다.

using System.Net;
using System.Net.Sockets;
using System.Text;
using AsyncAwaitLab.Common;

ConsoleHelpers.Banner("Chapter 07 — Async socket wrapper");

using var listener = new TcpListener(IPAddress.Loopback, 0);
listener.Start();
int port = ((IPEndPoint)listener.LocalEndpoint).Port;
ConsoleHelpers.Log($"서버 listen → 127.0.0.1:{port}");

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

var serverTask = Task.Run(async () =>
{
    using var client = await listener.AcceptTcpClientAsync(cts.Token);
    client.NoDelay = true;
    var sock = client.Client;
    var buffer = new byte[1024];
    int n = await sock.ReceiveAsync(buffer.AsMemory(), SocketFlags.None, cts.Token);
    if (n > 0)
    {
        await sock.SendAsync(buffer.AsMemory(0, n), SocketFlags.None, cts.Token);
    }
});

using (var client = new TcpClient())
{
    await client.ConnectAsync(IPAddress.Loopback, port, cts.Token);
    var sock = client.Client;
    var send = Encoding.UTF8.GetBytes("hello, async socket!");
    await sock.SendAsync(send.AsMemory(), SocketFlags.None, cts.Token);

    var buf = new byte[1024];
    int n = await sock.ReceiveAsync(buf.AsMemory(), SocketFlags.None, cts.Token);
    ConsoleHelpers.Log($"echoed back: {Encoding.UTF8.GetString(buf, 0, n)}");
}

await serverTask;
ConsoleHelpers.Log("Chapter 07 done.");
