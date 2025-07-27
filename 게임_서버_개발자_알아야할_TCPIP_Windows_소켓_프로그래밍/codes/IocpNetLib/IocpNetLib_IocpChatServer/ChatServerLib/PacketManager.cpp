#include <utility>
#include <cstring>

#include "Packet.h"
#include "PacketManager.h"


using namespace Common;

namespace ChatServerLib
{
	void PacketManager::Init(UserManager* pUserManager, RoomManager* pRoomManager) 
	{
		m_RecvFuntionDictionary = std::unordered_map<int, PROCESS_RECV_PACKET_FUNCTION>();

		m_RecvFuntionDictionary[(int)Common::PACKET_ID::LOGIN_REQUEST] = &PacketManager::ProcessLogin;
		m_RecvFuntionDictionary[(int)PACKET_ID::ROOM_ENTER_REQUEST] = &PacketManager::ProcessEnterRoom;
		m_RecvFuntionDictionary[(int)PACKET_ID::ROOM_LEAVE_REQUEST] = &PacketManager::ProcessLeaveRoom;
		m_RecvFuntionDictionary[(int)PACKET_ID::ROOM_CHAT_REQUEST] = &PacketManager::ProcessRoomChatMessage;
				
		m_pUserManager = pUserManager;
		m_pRoomManager = pRoomManager;
	}



	void PacketManager::ProcessRecvPacket(INT32 connectionIndex, char* pBuf, INT16 copySize) 
	{
		PACKET_HEADER* pHeader = reinterpret_cast<PACKET_HEADER*>(pBuf);

		auto iter = m_RecvFuntionDictionary.find(pHeader->PacketId);
		if (iter != m_RecvFuntionDictionary.end())
		{
			(this->*(iter->second))(connectionIndex, pBuf, copySize);
		}

	}
		
	void PacketManager::ProcessLogin(const INT32 connIndex, char* pBuf, INT16 copySize) 
	{ 
		if (LOGIN_REQUEST_PACKET_SZIE != copySize)
		{
			return;
		}

		auto pLoginReqPacket = reinterpret_cast<LOGIN_REQUEST_PACKET*>(pBuf);

		auto pUserID = pLoginReqPacket->UserID;
		printf("requested user id = %s\n", pUserID);

		LOGIN_RESPONSE_PACKET loginResPacket;
		loginResPacket.PacketId = (UINT16)PACKET_ID::LOGIN_RESPONSE;
		loginResPacket.PacketLength = sizeof(LOGIN_RESPONSE_PACKET);

		if (m_pUserManager->GetCurrentUserCnt() >= m_pUserManager->GetMaxUserCnt()) 
		{ 
			//�����ڼ��� �ִ���� �����ؼ� ���ӺҰ�
			loginResPacket.Result = (UINT16)ERROR_CODE::LOGIN_USER_USED_ALL_OBJ;
			SendPacketFunc(connIndex, &loginResPacket, sizeof(LOGIN_RESPONSE_PACKET));
			return;
		}

		//���⿡�� �̹� ���ӵ� �������� Ȯ���ϰ�, ���ӵ� ������� �����Ѵ�.
		if (m_pUserManager->FindUserIndexByID(pUserID) == -1) 
		{ 
			//�������� �ƴ� �������
			//������ �����ϴ� ��ü�� �ִ´�.
			m_pUserManager->AddUser(pUserID, connIndex);
			loginResPacket.Result = (UINT16)ERROR_CODE::NONE;
		}
		else 
		{
			//�������� �������� ���и� ��ȯ�Ѵ�.
			loginResPacket.Result = (UINT16)ERROR_CODE::LOGIN_USER_ALREADY;
			SendPacketFunc(connIndex, &loginResPacket, sizeof(LOGIN_RESPONSE_PACKET));
			return;
		}

		SendPacketFunc(connIndex, &loginResPacket, sizeof(LOGIN_RESPONSE_PACKET));				
	}



	void PacketManager::ProcessEnterRoom(INT32 connIndex, char* pBuf, INT16 copySize) 
	{
		UNREFERENCED_PARAMETER(copySize);

		auto pRoomEnterReqPacket = reinterpret_cast<ROOM_ENTER_REQUEST_PACKET*>(pBuf);
		auto pReqUser = m_pUserManager->GetUserByConnIdx(connIndex);

		if (!pReqUser || pReqUser == nullptr) 
		{
			return;
		}
				
		ROOM_ENTER_RESPONSE_PACKET roomEnterResPacket;
		roomEnterResPacket.PacketId = (UINT16)PACKET_ID::ROOM_ENTER_RESPONSE;
		roomEnterResPacket.PacketLength = sizeof(ROOM_ENTER_RESPONSE_PACKET);
				
		roomEnterResPacket.Result = m_pRoomManager->EnterUser(pRoomEnterReqPacket->RoomNumber, pReqUser);

		SendPacketFunc(connIndex, &roomEnterResPacket, sizeof(ROOM_ENTER_RESPONSE_PACKET));
		printf("Response Packet Sended");
	}


	void PacketManager::ProcessLeaveRoom(INT32 connIndex, char* pBuf, INT16 copySize) 
	{
		UNREFERENCED_PARAMETER(pBuf);
		UNREFERENCED_PARAMETER(copySize);

		ROOM_LEAVE_RESPONSE_PACKET roomLeaveResPacket;
		roomLeaveResPacket.PacketId = (UINT16)PACKET_ID::ROOM_LEAVE_RESPONSE;
		roomLeaveResPacket.PacketLength = sizeof(ROOM_LEAVE_RESPONSE_PACKET);

		auto reqUser = m_pUserManager->GetUserByConnIdx(connIndex);
		auto roomNum = reqUser->GetCurrentRoom();
				
		//TODO Room���� UserList��ü�� �� Ȯ���ϱ�
		roomLeaveResPacket.Result = m_pRoomManager->LeaveUser(roomNum, reqUser);
		SendPacketFunc(connIndex, &roomLeaveResPacket, sizeof(ROOM_LEAVE_RESPONSE_PACKET));
	}


	void PacketManager::ProcessRoomChatMessage(INT32 connIndex, char* pBuf, INT16 copySize) 
	{
		UNREFERENCED_PARAMETER(copySize);

		auto pRoomChatReqPacketet = reinterpret_cast<ROOM_CHAT_REQUEST_PACKET*>(pBuf);
		
		ROOM_CHAT_RESPONSE_PACKET roomChatResPacket;
		roomChatResPacket.PacketId = (UINT16)PACKET_ID::ROOM_CHAT_RESPONSE;
		roomChatResPacket.PacketLength = sizeof(ROOM_CHAT_RESPONSE_PACKET);
		roomChatResPacket.Result = (INT16)ERROR_CODE::NONE;

		auto reqUser = m_pUserManager->GetUserByConnIdx(connIndex);
		auto roomNum = reqUser->GetCurrentRoom();
				
		auto pRoom = m_pRoomManager->GetRoomByNumber(roomNum);
		if (pRoom == nullptr)
		{
			roomChatResPacket.Result = (INT16)ERROR_CODE::CHAT_ROOM_INVALID_ROOM_NUMBER;
			SendPacketFunc(connIndex, &roomChatResPacket, sizeof(ROOM_CHAT_RESPONSE_PACKET));
			return;
		}
		
		SendPacketFunc(connIndex, &roomChatResPacket, sizeof(ROOM_CHAT_RESPONSE_PACKET));

		pRoom->NotifyChat(connIndex, reqUser->GetUserId().c_str(), pRoomChatReqPacketet->Message);		
	}		   

	void PacketManager::ClearConnectionInfo(INT32 connIndex) 
	{
		auto pReqUser = m_pUserManager->GetUserByConnIdx(connIndex);

		if (pReqUser->GetDomainState() == User::DOMAIN_STATE::ROOM) 
		{
			auto roomNum = pReqUser->GetCurrentRoom();
			m_pRoomManager->LeaveUser(roomNum, pReqUser);
		}

		if (pReqUser->GetDomainState() != User::DOMAIN_STATE::NONE) 
		{
			m_pUserManager->DeleteUserInfo(pReqUser);
		}
	}




}