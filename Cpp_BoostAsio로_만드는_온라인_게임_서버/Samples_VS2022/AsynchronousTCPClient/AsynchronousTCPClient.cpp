#include <SDKDDKVer.h>
#include <iostream>
#include <format>
#include <boost/bind/bind.hpp>
#include <boost/asio.hpp>


const char SERVER_IP[] = "127.0.0.1";
const unsigned short PORT_NUMBER = 31400;


class TCP_Client 
{
public:
    TCP_Client(boost::asio::io_context& io_context)
        : m_io_context(io_context),
          m_Socket(io_context),
		  m_nSeqNumber(0)
    {}

    void Connect( boost::asio::ip::tcp::endpoint& endpoint )
    {
        m_Socket.async_connect( endpoint,
								boost::bind(&TCP_Client::handle_connect, 
								this, 
								boost::asio::placeholders::error)
								);
    }
	   
	
private:
	void PostWrite()
	{
		if( m_Socket.is_open() == false )
		{
			return;
		}

		if( m_nSeqNumber > 7 )
		{
			m_Socket.close();
			return;
		}


		++m_nSeqNumber;

		m_WriteMessage = std::format("{} - Send Message", m_nSeqNumber);

		boost::asio::async_write(m_Socket, boost::asio::buffer(m_WriteMessage),
							boost::bind( &TCP_Client::handle_write, this,
								boost::asio::placeholders::error,
								boost::asio::placeholders::bytes_transferred )
							);
			
		PostReceive();
	}

	void PostReceive()
	{
		memset( &m_ReceiveBuffer, '\0', sizeof(m_ReceiveBuffer) );

		m_Socket.async_read_some( boost::asio::buffer(m_ReceiveBuffer), 
									boost::bind( &TCP_Client::handle_receive, 
												this, 
												boost::asio::placeholders::error, 
												boost::asio::placeholders::bytes_transferred ) 
								); 							
	}


	void handle_connect(const boost::system::error_code& error)
    {
        if (error) 
		{
            std::cout << std::format("connect failed : {}", error.message()) << std::endl;
        }
        else 
		{
            std::cout << "connected" << std::endl;
			
			PostWrite();
        }
    }

	void handle_write(const boost::system::error_code& /*error*/, size_t /*bytes_transferred*/)
	{
	}

	void handle_receive( const boost::system::error_code& error, size_t bytes_transferred )
	{
		if( error )
		{
			if( error == boost::asio::error::eof )
			{
				std::cout << "서버와 연결이 끊어졌습니다" << std::endl;
			}
			else 
			{
				std::cout << std::format("error No: {},  error Message: {}", error.value(), error.message()) << std::endl;
			}
		}
		else
		{
			const std::string strRecvMessage = m_ReceiveBuffer.data();
			std::cout << std::format("클라이언트에서 받은 메시지: {}, 받은 크기: {}", strRecvMessage, bytes_transferred) << std::endl;

			PostWrite(); 
		}
	}

	

	boost::asio::io_context& m_io_context;
    boost::asio::ip::tcp::socket m_Socket;

	int m_nSeqNumber;
	std::array<char, 128> m_ReceiveBuffer;
	std::string m_WriteMessage;
};



int main()
{
    boost::asio::io_context io_context;

	boost::asio::ip::tcp::endpoint endpoint(boost::asio::ip::make_address(SERVER_IP), PORT_NUMBER);

    TCP_Client client(io_context);

    client.Connect( endpoint );

	io_context.run();

	
	std:: cout << "네트웍 접속 종료" << std::endl;

	getchar();
	return 0;
}
