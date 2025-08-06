using CommandLine;


#pragma warning disable CS8618

namespace OmokServer;

public class ConnectionStrings
{
    [Option("MySqlHiveDb", Required = true, HelpText = "MySqlHiveDb")]
    public string MySqlHiveDb { get; set; }

    [Option("MySqlGameDb", Required = true, HelpText = "MySqlGameDb")]
    public string MySqlGameDb { get; set; }

    [Option("HiveRedis", Required = true, HelpText = "HiveRedis")]
    public string HiveRedis { get; set; }

    [Option("GameRedis", Required = true, HelpText = "GameRedis")]
    public string GameRedis { get; set; }
}
