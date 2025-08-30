#nullable enable

using System;
using CommandLine;
using EchoServer;
using FreeNetLite;


// 1. 명령줄 인수 파싱
var serverOpt = ParseCommandLine(args);
if (serverOpt.Port == 0)
{
    // 파싱 실패 시 프로그램 종료
    return 1;
}

// 2. 패킷 디스패처 및 네트워크 서비스 초기화
var packetDispatcher = new DefaultPacketDispatcher();
packetDispatcher.Init(4); // 헤더 크기: 패킷 길이(2) + ID(2) = 4

var service = new NetworkService(serverOpt, packetDispatcher);
service.Initialize();

// 3. 서버 시작
const bool isNoDelay = true;
service.Start("0.0.0.0", serverOpt.Port, 100, isNoDelay);
Console.WriteLine("EchoServer Started!");

// 4. 패킷 처리 로직 시작
var packetProcess = new PacketProcess(service);
packetProcess.Start();

// 5. 서버 제어 루프
while (true)
{
    string? input = Console.ReadLine();
    switch (input?.ToLower())
    {
        case "users":
            Console.WriteLine($"Current connected sessions: {service.ConnectedSessionCount}");
            break;
        case "exit":
            Console.WriteLine("Server shutting down...");
            packetProcess.Stop();
            service.Stop();
            Console.WriteLine("Server stopped.");
            return 0;
    }
}

//--port 32451 --max_conn_count 100 --recv_buff_size 4012 --max_packet_size 1024
FreeNetLite.ServerOption ParseCommandLine(string[] args)
{
    return Parser.Default.ParseArguments<CommandLineOption>(args)
        .MapResult(
            (CommandLineOption opts) =>
            {
                // 파싱 성공 시 FreeNetLite.ServerOption 레코드를 생성합니다.
                var option = new ServerOption
                {
                    Port = opts.Port,
                    MaxConnectionCount = opts.MaxConnectionCount,
                    ReceiveBufferSize = opts.ReceiveBufferSize,
                    MaxPacketSize = opts.MaxPacketSize
                };
                Console.WriteLine(option); // 재정의된 ToString() 호출
                return option;
            },
            errs =>
            {
                // 파싱 실패 시 null을 반환합니다.
                Console.WriteLine("Failed to parse command line arguments.");
                return new ServerOption();
            });
}


/// <summary>
/// CommandLineParser 라이브러리에서 사용할 옵션 클래스입니다.
/// </summary>
public class CommandLineOption
{
    [Option('p', "port", Required = false, HelpText = "Port Number")]
    public int Port { get; set; } = 11021;

    [Option('c', "max_conn_count", Required = false, HelpText = "MaxConnectionCount")]
    public int MaxConnectionCount { get; set; } = 100;

    [Option('r', "recv_buff_size", Required = false, HelpText = "ReceiveBufferSize")]
    public int ReceiveBufferSize { get; set; } = 4096;

    [Option('m', "max_packet_size", Required = false, HelpText = "MaxPacketSize")]
    public int MaxPacketSize { get; set; } = 1024;
}
/*FreeNetLite.ServerOption ParseCommandLine(string[] args)
{
	var result = Parser.Default.ParseArguments<ServerOption>(args) as Parsed<ServerOption>;
	if (result == null)
	{
		Console.WriteLine("Failed Command Line");
		return null;
	}

	var option = new FreeNetLite.ServerOption
	{
		Port = result.Value.Port,
		MaxConnectionCount = result.Value.MaxConnectionCount,
		ReceiveBufferSize = result.Value.ReceiveBufferSize,
		MaxPacketSize = result.Value.MaxPacketSize
	};
	option.ToString();

	return option;
}*/

