#include "redis_asio/one-shot-client.h"
#include "redis_asio/multi-thread-client.h"


int main()
{
	return 0;
}

void SingleThreadClient()
{
    boost::asio::io_context io;
    boost::asio::ip::tcp::endpoint ep(
        boost::asio::ip::address::from_string("127.0.0.1"),
        6379);

    auto p = std::make_shared<redis_asio::OneShotClient>(io, ep);
    //p->ExecuteCmd("*3\r\n$3\r\nset\r\n$3\r\naaa\r\n$3\r\nbbb\r\n");
    //p->ExecuteCmd("*2\r\n$3\r\nget\r\n$3\r\naaa\r\n");
    p->ExecuteCmd("*2\r\n$4\r\nkeys\r\n$1\r\n*\r\n");

    io.run();
}


void MultiThreadClient()
{
    boost::asio::io_context io_context;
    boost::asio::ip::tcp::endpoint server_endpoint(
        boost::asio::ip::address::from_string("127.0.0.1"), 6379);
    auto client = std::make_shared<redis_asio::RedisClient>(
        io_context, server_endpoint);
    client->Start();
    std::thread t([&io_context]()
        {
            io_context.run();
        });

    //std::cout<<"main thread:"<<std::this_thread::get_id()<<"\n";

    std::cout << server_endpoint.address().to_string() << ":"
        << server_endpoint.port() << "> " << std::flush;
    std::string line;
    while (std::getline(std::cin, line))
    {
        std::string bulk_string = redis_asio::InputComposer::ComposeInputToBulk(line);
        client->Send(bulk_string);
    }

    t.join();
}
