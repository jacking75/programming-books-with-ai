#include <SDKDDKVer.h>
#include <iostream>
#include <format>
#include <string>
#include <boost/bind/bind.hpp>
#include <boost/asio.hpp>



class Session 
{
public:
	Session(boost::asio::io_context& io_context)
		: m_Socket(io_context)
	{
	}

	boost::asio::ip::tcp::socket& Socket()
	{
		return m_Socket;
	}

	void PostReceive()
	{
		memset( &m_ReceiveBuffer, '\0', sizeof(m_ReceiveBuffer) );

		m_Socket.async_read_some
				( 
				boost::asio::buffer(m_ReceiveBuffer), 
				boost::bind( &Session::handle_receive, 
								this, 
								boost::asio::placeholders::error, 
								boost::asio::placeholders::bytes_transferred ) 				
			);
							
	}


private:
	void handle_write(const boost::system::error_code& /*error*/, size_t /*bytes_transferred*/)
	{
	}

	void handle_receive( const boost::system::error_code& error, size_t bytes_transferred )
	{
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
		}
		else
		{
			const std::string strRecvMessage = m_ReceiveBuffer.data();
			std::cout << std::format("클라이언트에서 받은 메시지: {], 받은 크기: {}", strRecvMessage, bytes_transferred) << std::endl;

			m_WriteMessage = std::format("Re:{}", strRecvMessage);
			
			boost::asio::async_write(m_Socket, boost::asio::buffer(m_WriteMessage),
								boost::bind( &Session::handle_write, this,
									boost::asio::placeholders::error,
									boost::asio::placeholders::bytes_transferred )
								);

			
			PostReceive(); 
		}
	}

	boost::asio::ip::tcp::socket m_Socket;
	std::string m_WriteMessage;
	std::array<char, 128> m_ReceiveBuffer;
};



const unsigned short PORT_NUMBER = 31400;

class TCP_Server
{
public:
	TCP_Server( boost::asio::io_context& io_context )
		: m_acceptor(io_context, boost::asio::ip::tcp::endpoint(boost::asio::ip::tcp::v4(), PORT_NUMBER))
	{
		m_pSession = nullptr;
		StartAccept();
	}

	~TCP_Server()
	{
		if( m_pSession != nullptr )
		{
			delete m_pSession;
		}
	}

private:
	void StartAccept()
	{
		std::cout << "클라이언트 접속 대기....." << std::endl;

		m_pSession = new Session((boost::asio::io_context&)m_acceptor.get_executor().context());
		
		m_acceptor.async_accept( m_pSession->Socket(),
								 boost::bind(&TCP_Server::handle_accept, 
												this, 
												m_pSession,
												boost::asio::placeholders::error)
								);
	}

	void handle_accept(Session* pSession, const boost::system::error_code& error)
	{
		if (!error)
		{	
			std::cout << "클라이언트 접속 성공" << std::endl;
			
			pSession->PostReceive();
		}
	}

	int m_nSeqNumber;
	boost::asio::ip::tcp::acceptor m_acceptor;
	Session* m_pSession;
};



int main()
{
	boost::asio::io_context io_context;
	
	TCP_Server server(io_context);
	
	io_context.run();
  

	std:: cout << "네트웍 접속 종료" << std::endl;

	getchar();
	return 0;
}