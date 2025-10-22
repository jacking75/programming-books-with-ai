#include <SDKDDKVer.h>
#include <iostream>
#include <format>
#include <boost/asio.hpp>

const char SERVER_IP[] = "127.0.0.1";
const unsigned short PORT_NUMBER = 31400;

int main()
{
	boost::asio::io_context io_context;
		
	boost::asio::ip::tcp::endpoint endpoint( boost::asio::ip::tcp::v4(), PORT_NUMBER );
	boost::asio::ip::tcp::acceptor acceptor( io_context, endpoint );
		
	boost::asio::ip::tcp::socket socket(io_context);
	acceptor.accept(socket);
	
	std::cout << "클라이언트 접속" << std::endl;
	
	for (;;)
	{
		std::array<char, 128> buf;
		buf.fill(0);
		boost::system::error_code error;

		auto len = (int)socket.read_some(boost::asio::buffer(buf), error);

		if( error )
		{
			if( error == boost::asio::error::eof )
			{
				std::cout << "클라이언트와 연결이 끊어졌습니다" << std::endl;
			}
			else 
			{
				std::cout << std::format("error No: {}, error Message: {}", error.value(), error.message()) << std::endl;
			}

			break;
		}

		std::cout << std::format("클라이언트에서 받은 메시지. 길이: {}, data: {}", len, &buf[0]) << std::endl;
				
		char szMessage[128] = {0,};
		sprintf_s( szMessage, 128-1, "Re:%s", &buf[0] );
		auto nMsgLen = strnlen_s( szMessage, 128-1 );

		boost::system::error_code ignored_error;
		socket.write_some(boost::asio::buffer(szMessage, nMsgLen), ignored_error);

		std::cout << std::format("클라이언트에 보낸 메시지: {}", szMessage) << std::endl;
	}
    
	getchar();
	return 0;
}

