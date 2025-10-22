#include <SDKDDKVer.h>

#include <iostream>
#include <string>

#include <boost/asio.hpp>
#include <boost/thread.hpp>
#include <boost/bind/bind.hpp>

class BackGroundJobManager 
{
	boost::asio::io_context& m_io_context;
	boost::asio::executor_work_guard<boost::asio::io_context::executor_type> m_Work;
	boost::thread_group m_Group;

public:
	BackGroundJobManager( boost::asio::io_context& io_context, std::size_t size)
		: m_io_context(io_context),
		m_Work(boost::asio::make_work_guard(io_context))
	{
		for (std::size_t i = 0; i < size; ++i) 
		{
			m_Group.create_thread( boost::bind(&boost::asio::io_context::run, &io_context) );
		}
	}

	~BackGroundJobManager()
	{
		m_Work.reset();
		m_Group.join_all();
	}

	template <class F>
	void post(F f)
	{
		//boost::asio::post(f);
		boost::asio::post(m_io_context, f);
	}
};



boost::mutex g_mutex;

void Function( int nNumber )
{
	char szMessage[128] = {0,};
	sprintf_s( szMessage, 128-1, "%s(%d) | time(%d)", __FUNCTION__, nNumber, (int)time(NULL) );
	{
		boost::mutex::scoped_lock lock(g_mutex);
		std::cout << std::format("워커 스레드 ID: {}, Msg: {}", ::GetCurrentThreadId(), szMessage) << std::endl;
	}

	::Sleep(1000);
}

class TEST
{	
public:
	TEST() { }
 
	void Function( int nNumber ) 
	{ 
		::Function( nNumber );
	}
};

int main()
{
	std::cout << "메인 스레드 ID: " << ::GetCurrentThreadId() << std::endl;

	boost::asio::io_context io_context;
	BackGroundJobManager JobManager( io_context, 3 );

	JobManager.post( boost::bind(Function, 11) );
	JobManager.post( boost::bind(Function, 12) );
	::Sleep(3000);
	
	TEST test;
	JobManager.post(boost::bind(&TEST::Function, &test, 21));
	JobManager.post(boost::bind(&TEST::Function, &test, 22));
	::Sleep(3000);

	return 0;
}