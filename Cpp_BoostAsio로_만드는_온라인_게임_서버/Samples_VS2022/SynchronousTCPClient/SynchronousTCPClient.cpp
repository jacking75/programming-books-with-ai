#include <SDKDDKVer.h>
#include <iostream>
#include <format>
#include <boost/asio.hpp>

const char SERVER_IP[] = "127.0.0.1";
const unsigned short PORT_NUMBER = 31400;


int main()
{
	boost::asio::io_context io_context;

	boost::asio::ip::tcp::endpoint endpoint(boost::asio::ip::make_address(SERVER_IP), PORT_NUMBER);
	
	boost::system::error_code connect_error;
	boost::asio::ip::tcp::socket socket(io_context);

	socket.connect(endpoint, connect_error);

	if (connect_error) 
	{
		std::cout << std::format("연결 실패. error No: {}, Message: {}", connect_error.value(), connect_error.message()) << std::endl;
		getchar();
		return 0;
    }
    else 
	{
        std::cout << "서버에 연결 성공" << std::endl;
    }


    for (int i = 0; i < 7; ++i ) 
    {
		char szMessage[128] = {0,};
		sprintf_s( szMessage, 128-1, "%d - Send Message", i );
		auto nMsgLen = (int)strnlen_s( szMessage, 128-1 );

		boost::system::error_code ignored_error;
		socket.write_some( boost::asio::buffer(szMessage, nMsgLen), ignored_error);

		std::cout << std::format("서버에 보낸 메시지: {}", szMessage) << std::endl;
	

		std::array<char, 128> buf;
		buf.fill(0);
		boost::system::error_code error;

		auto len = (int)socket.read_some(boost::asio::buffer(buf), error);

		if( error )
		{
			if( error == boost::asio::error::eof )
			{
				std::cout << "서버와 연결이 끊어졌습니다" << std::endl;
			}
			else 
			{
				std::cout << std::format("error No: {}, error Message: {}", error.value(), error.message()) << std::endl;
			}

			break;
		}

		std::cout << std::format("서버로부터 받은 메시지. 길이: {}, data: {}", len, &buf[0]) << std::endl;
	}
		
	if( socket.is_open() )
	{
		socket.close();
	}
  
	getchar();
	return 0;
}