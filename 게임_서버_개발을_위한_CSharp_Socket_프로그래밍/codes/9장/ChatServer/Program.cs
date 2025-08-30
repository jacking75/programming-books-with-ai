// See https://aka.ms/new-console-template for more information

var server = new ChatServer.ChattingServer();
server.Start();

Console.WriteLine("Server Started. Press ENTER to stop.");
Console.ReadLine();

server.Stop();
