#include <SDKDDKVer.h>
#include <iostream>
#include <format>
#include <boost/bind/bind.hpp>
#include <boost/asio.hpp>


const char UDP_IP[] = "127.0.0.1";
const unsigned short SERVER_PORT_NUMBER = 31400;
const unsigned short CLIENT_PORT_NUMBER = 31401;


class UDP_Client 
{
public:
    UDP_Client(boost::asio::io_context& io_context)
        : m_Socket(io_context, boost::asio::ip::udp::endpoint(boost::asio::ip::udp::v4(), CLIENT_PORT_NUMBER)),
		  m_nSeqNumber(0)
    {
	}

  	void PostWrite()
	{
		if( m_nSeqNumber >= 7 )
		{
			return;
		}


		++m_nSeqNumber;

		m_WriteMessage = std::format("{} - Send Message", m_nSeqNumber);

		m_Socket.async_send_to( boost::asio::buffer(m_WriteMessage),
								boost::asio::ip::udp::endpoint( boost::asio::ip::make_address(UDP_IP), SERVER_PORT_NUMBER ),
								boost::bind( &UDP_Client::handle_write, 
											this,
											boost::asio::placeholders::error,
											boost::asio::placeholders::bytes_transferred )
							);
					
		PostReceive();
	}

	void PostReceive()
	{
		memset( &m_ReceiveBuffer, '\0', sizeof(m_ReceiveBuffer) );

		m_Socket.async_receive_from( boost::asio::buffer(m_ReceiveBuffer), 
									 m_SenderEndpoint, 	
									 boost::bind( &UDP_Client::handle_receive, 
												this, 
												boost::asio::placeholders::error, 
												boost::asio::placeholders::bytes_transferred ) 
								); 
							
	}

	
	void handle_write(const boost::system::error_code& /*error*/, size_t /*bytes_transferred*/)
	{
	}

	void handle_receive( const boost::system::error_code& error, size_t bytes_transferred )
	{
		if( error )
		{
			std::cout << std::format("error No: {}, error Message: {}", error.value(), error.message()) << std::endl;
		}
		else
		{
			const std::string strRecvMessage = m_ReceiveBuffer.data();
			std::cout << std::format("클라이언트에서 받은 메시지: {}, 받은 크기: {}", strRecvMessage, bytes_transferred) << std::endl;

			PostWrite(); 
		}
	}

	

	boost::asio::ip::udp::socket m_Socket;
	boost::asio::ip::udp::endpoint m_SenderEndpoint;

	int m_nSeqNumber;
	std::array<char, 128> m_ReceiveBuffer;
	std::string m_WriteMessage;
};



int main()
{
    boost::asio::io_context io_context;

	UDP_Client client(io_context);

    client.PostWrite();

    io_context.run();

	
	std:: cout << "네트웍 종료" << std::endl;

	getchar();
	return 0;
}
