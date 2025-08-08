using CommandLine;

#pragma warning disable CS8618

namespace OmokServer;

public class ServerOption
{
    [Option("uniqueID", Required = true, HelpText = "Server UniqueID")]
    public int ChatServerUniqueID { get; set; }

    [Option("name", Required = true, HelpText = "Server Name")]
    public string Name { get; set; }

    // 슈퍼소켓라이브러리 사용을 위한 옵션
    [Option("maxConnectionNumber", Required = true, HelpText = "MaxConnectionNumber")]
    public int MaxConnectionNumber { get; set; }

    [Option("port", Required = true, HelpText = "Port")]
    public int Port { get; set; }

    [Option("maxRequestLength", Required = true, HelpText = "maxRequestLength")]
    public int MaxRequestLength { get; set; }

    [Option("receiveBufferSize", Required = true, HelpText = "receiveBufferSize")]
    public int ReceiveBufferSize { get; set; }

    [Option("sendBufferSize", Required = true, HelpText = "sendBufferSize")]
    public int SendBufferSize { get; set; }


    // 룸 매칭을 위한 옵션
    [Option("roomMaxCount", Required = true, HelpText = "Max Romm Count")]
    public int RoomMaxCount { get; set; } = 0; // 최대 방 수

    [Option("roomMaxUserCount", Required = true, HelpText = "RoomMaxUserCount")]
    public int RoomMaxUserCount { get; set; } = 0; // 방에 들어갈 수 있는 최대 유저 수

    [Option("roomStartNumber", Required = true, HelpText = "RoomStartNumber")]
    public int RoomStartNumber { get; set; } = 0; // (서버 별) 방의 시작번호

    [Option("roomIntervalMilliseconds", Required = true, HelpText = "RoomIntervalMilliseconds")]
    public int RoomIntervalMilliseconds { get; set; } = 0; // 방을 체크하는 주기

    // Connection String
    [Option("MySqlHiveDb", Required = true, HelpText = "MySqlHiveDb")]
    public string MySqlHiveDb { get; set; }

    [Option("MySqlGameDb", Required = true, HelpText = "MySqlGameDb")]
    public string MySqlGameDb { get; set; }

    [Option("HiveRedis", Required = true, HelpText = "HiveRedis")]
    public string HiveRedis { get; set; }

    [Option("GameRedis", Required = true, HelpText = "GameRedis")]
    public string GameRedis { get; set; }
}
