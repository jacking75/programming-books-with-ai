#nullable enable

using System;
using CommandLine;
using FreeNetLite;


var serverOpt = ParseCommandLine(args);

// IoThreadPacketDispatcher 에서 바로 패킷을 처리한다. 즉 멀티스레드로 패킷을 처리한다.
var packetDispatcher = new EchoServerIOThreadPacketProcess.IoThreadPacketDispatcher();
packetDispatcher.Init(EchoServerIOThreadPacketProcess.PacketDef.HeaderSize);

var service = new FreeNetLite.NetworkService(serverOpt, packetDispatcher);
service.Initialize();

bool isNoDelay = true;
service.Start("0.0.0.0", serverOpt.Port, 100, isNoDelay);


while (true)
{
	string? input = Console.ReadLine();

    if (input == null)
    {
        continue;
    }
    else if (input.Equals("users"))
	{
        Console.WriteLine($"Current connected sessions: {service.ConnectedSessionCount}");
    }
	else if (input.Equals("exit"))
	{
		service.Stop();

		Console.WriteLine("Exit Process !!!");
		break;
	}

	System.Threading.Thread.Sleep(500);
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


