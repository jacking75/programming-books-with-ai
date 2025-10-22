/*
 one shot redis client. connect to server, send cmd, recv response, and then disconnect.
 the client should be destroyed after each time.
 frank Jul 1, 2018
 */
#include <iostream>
#include <string>
#include <memory>
#include <functional>
#include <map>

#include <boost/bind/bind.hpp>
#include <boost/asio.hpp>

#include "response_parser.h"


namespace redis_asio
{
    // one shot async client, exe one cmd, get the result, then destroy
    class OneShotClient : public std::enable_shared_from_this<OneShotClient>
    {
    public:
        OneShotClient(boost::asio::io_context& io, boost::asio::ip::tcp::endpoint ep) :
            ep_(ep), socket_(io)
        {
        }

        void ExecuteCmd(const std::string& cmd)
        {
            send_buff_ = cmd; // copy and save
            socket_.async_connect(ep_,
                [this](const boost::system::error_code& err)
                {
                    if (err)
                    {
                        std::cout << "connect err: " << err.message() << "\n";
                        return;
                    }

                    boost::asio::async_write(socket_, boost::asio::buffer(send_buff_),
                        [this](const boost::system::error_code& err, size_t len)
                        {
                            if (err)
                            {
                                std::cout << "send err: " << err.message() << "\n";
                                return;
                            }

                            socket_.async_read_some(boost::asio::buffer(recv_buff_),
                                boost::bind(&OneShotClient::ReceiveResponse,
                                    this,
                                    boost::asio::placeholders::error,
                                    boost::asio::placeholders::bytes_transferred));
                        });
                });
        }
    private:
        void ReceiveResponse(const boost::system::error_code& err, size_t len)
        {
            if (err)
            {
                std::cout << "recv err: " << err.message() << "\n";
                return;
            }

            //cout<<"got result: "<<string(recv_buff_, len)<<"\n";

            for (char* p = recv_buff_; p < recv_buff_ + len; ++p)
            {
                if (!parse_item_)
                {
                    parse_item_ = AbstractReplyItem::CreateItem(*p);
                    continue;
                }

                ParseResult pr = parse_item_->Feed(*p);
                if (pr == ParseResult::PR_FINISHED)
                {
                    std::cout << "get parse result: " << parse_item_->ToString() << "\n";
                    return;
                }
                else if (pr == ParseResult::PR_ERROR)
                {
                    std::cout << "parse error at " << *p << ", pos=" << (p - recv_buff_)
                        << "\n";
                    return;
                }
            }

            // not finished, recv again
            socket_.async_read_some(boost::asio::buffer(recv_buff_),
                boost::bind(&OneShotClient::ReceiveResponse, this, boost::asio::placeholders::error,
                    boost::asio::placeholders::bytes_transferred));
        }
    private:

        boost::asio::ip::tcp::endpoint ep_; // server address
        boost::asio::ip::tcp::socket socket_;
        std::string send_buff_;
        char recv_buff_[1024];
        std::shared_ptr<AbstractReplyItem> parse_item_;
    };
}