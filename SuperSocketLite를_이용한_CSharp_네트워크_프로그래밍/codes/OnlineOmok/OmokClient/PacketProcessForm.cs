using CSCommon;
using MemoryPack;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace OmokClient
{
    public partial class mainForm
    {
        Dictionary<PACKETID, Action<byte[]>> PacketFuncDic = new Dictionary<PACKETID, Action<byte[]>>(); // 패킷 처리 함수 딕셔너리

        void SetPacketHandler() // 패킷 핸들러 설정 
        {
            //PacketFuncDic.Add(PACKET_ID.PACKET_ID_ERROR_NTF, PacketProcess_ErrorNotify);
            PacketFuncDic.Add(PACKETID.ResLogin, PacketProcess_Loginin);

            PacketFuncDic.Add(PACKETID.ResRoomEnter, PacketProcess_RoomEnterResponse);
            PacketFuncDic.Add(PACKETID.NtfRoomUserList, PacketProcess_RoomUserListNotify);
            PacketFuncDic.Add(PACKETID.NtfRoomNewUser, PacketProcess_RoomNewUserNotify);
            PacketFuncDic.Add(PACKETID.ResRoomLeave, PacketProcess_RoomLeaveResponse);
            PacketFuncDic.Add(PACKETID.NtfRoomLeaveUser, PacketProcess_RoomLeaveUserNotify);
            PacketFuncDic.Add(PACKETID.NtfRoomChat, PacketProcess_RoomChatNotify);
            //PacketFuncDic.Add(PACKETID.REQ_ROOM_CHAT, PacketProcess_RoomChatResponse);


            PacketFuncDic.Add(PACKETID.ResReadyOmok, PacketProcess_ReadyOmokResponse);
            PacketFuncDic.Add(PACKETID.NtfReadyOmok, PacketProcess_ReadyOmokNotify);
            PacketFuncDic.Add(PACKETID.NtfStartOmok, PacketProcess_StartOmokNotify);
            PacketFuncDic.Add(PACKETID.ResPutMok, PacketProcess_PutMokResponse);
            PacketFuncDic.Add(PACKETID.NTFPutMok, PacketProcess_PutMokNotify);
            PacketFuncDic.Add(PACKETID.NTFEndOmok, PacketProcess_EndOmokNotify);
            PacketFuncDic.Add(PACKETID.NtfChangeTurn, PacketProcess_ChangeTurn); // 턴 변경 처리 함수 추가
        }

        void PacketProcess(byte[] packet) // 패킷 처리
        {
            var header = new MemoryPackPacketHeadInfo(); // 패킷 헤더 정보
            header.Read(packet); // 패킷 헤더 읽기

            var packetID = (PACKETID)header.Id; // 패킷 ID

            if (PacketFuncDic.ContainsKey(packetID)) // 패킷 ID가 딕셔너리에 있으면
            {
                PacketFuncDic[packetID](packet); // 해당 패킷 처리 함수 호출
            }
            else
            {
                DevLog.Write("Unknown Packet Id: " + packetID);
            }
        }

        void PacketProcess_PutStoneInfoNotifyResponse(byte[] bodyData)
        {
            /*var responsePkt = new PutStoneNtfPacket();
            responsePkt.FromBytes(bodyData);

            DevLog.Write($"'{responsePkt.userID}' Put Stone  : [{responsePkt.xPos}] , [{responsePkt.yPos}] ");*/

        }

        void PacketProcess_GameStartResultResponse(byte[] bodyData)
        {
            /*var responsePkt = new GameStartResPacket();
            responsePkt.FromBytes(bodyData);

            if ((ERROR_CODE)responsePkt.Result == ERROR_CODE.NOT_READY_EXIST)
            {
                DevLog.Write($"모두 레디상태여야 시작합니다.");
            }
            else
            {
                DevLog.Write($"게임시작 !!!! '{responsePkt.UserID}' turn  ");
            }*/
        }

        void PacketProcess_GameEndResultResponse(byte[] bodyData)
        {
            /*var responsePkt = new GameResultResPacket();
            responsePkt.FromBytes(bodyData);
            
            DevLog.Write($"'{responsePkt.UserID}' WIN , END GAME ");*/

        }

        void PacketProcess_PutStoneResponse(byte[] bodyData)
        {
            /*var responsePkt = new MatchUserResPacket();
            responsePkt.FromBytes(bodyData);

            if((ERROR_CODE)responsePkt.Result != ERROR_CODE.ERROR_NONE)
            {
                DevLog.Write($"Put Stone Error : {(ERROR_CODE)responsePkt.Result}");
            }

            DevLog.Write($"다음 턴 :  {(ERROR_CODE)responsePkt.Result}");*/

        }




        void PacketProcess_ErrorNotify(byte[] packetData)
        {
            /*var notifyPkt = new ErrorNtfPacket();
            notifyPkt.FromBytes(bodyData);

            DevLog.Write($"에러 통보 받음:  {notifyPkt.Error}");*/
        }


        void PacketProcess_Loginin(byte[] packetData) // 로그인 결과 처리
        {
            var responsePkt = MemoryPackSerializer.Deserialize<PKTResLogin>(packetData); // 패킷 데이터를 PKTResLogin 클래스로 역직렬화
            DevLog.Write($"로그인 결과: {(ERROR_CODE)responsePkt.Result}");

            // 로그인 성공 후 타이머 시작
            heartBeatTimer.Start();
        }

        void PacketProcess_RoomEnterResponse(byte[] packetData)
        {
            var responsePkt = MemoryPackSerializer.Deserialize<PKTResRoomEnter>(packetData);
            DevLog.Write($"방 입장 결과:  {(ERROR_CODE)responsePkt.Result}");
        }

        void PacketProcess_RoomUserListNotify(byte[] packetData)
        {
            var notifyPkt = MemoryPackSerializer.Deserialize<PKTNtfRoomUserList>(packetData);

            for (int i = 0; i < notifyPkt.UserIDList.Count; ++i)
            {
                AddRoomUserList(notifyPkt.UserIDList[i]);
            }

            DevLog.Write($"방의 기존 유저 리스트 받음");
        }

        void PacketProcess_RoomNewUserNotify(byte[] packetData)
        {
            var notifyPkt = MemoryPackSerializer.Deserialize<PKTNtfRoomNewUser>(packetData);

            AddRoomUserList(notifyPkt.UserID);

            DevLog.Write($"방에 새로 들어온 유저 받음");
        }


        void PacketProcess_RoomLeaveResponse(byte[] packetData)
        {
            var responsePkt = MemoryPackSerializer.Deserialize<PKTResRoomLeave>(packetData);

            DevLog.Write($"방 나가기 결과:  {(ERROR_CODE)responsePkt.Result}");
        }

        void PacketProcess_RoomLeaveUserNotify(byte[] packetData)
        {
            var notifyPkt = MemoryPackSerializer.Deserialize<PKTNtfRoomLeaveUser>(packetData);

            RemoveRoomUserList(notifyPkt.UserID);

            DevLog.Write($"방에서 나간 유저 받음");
        }



        void PacketProcess_RoomChatResponse(byte[] packetData)
        {
            var responsePkt = MemoryPackSerializer.Deserialize<PKTResRoomChat>(packetData);

            //DevLog.Write($"방 채팅 결과:  {(ERROR_CODE)responsePkt.Result}");
        }


        void PacketProcess_RoomChatNotify(byte[] packetData)
        {
            var notifyPkt = MemoryPackSerializer.Deserialize<PKTNtfRoomChat>(packetData);

            AddRoomChatMessageList(notifyPkt.UserID, notifyPkt.ChatMessage);
        }

        void AddRoomChatMessageList(string userID, string message)
        {
            if (listBoxRoomChatMsg.Items.Count > 512)
            {
                listBoxRoomChatMsg.Items.Clear();
            }

            listBoxRoomChatMsg.Items.Add($"[{userID}]: {message}");
            listBoxRoomChatMsg.SelectedIndex = listBoxRoomChatMsg.Items.Count - 1;
        }

        void PacketProcess_ReadyOmokResponse(byte[] packetData)
        {
            var responsePkt = MemoryPackSerializer.Deserialize<PKTResReadyOmok>(packetData);

            DevLog.Write($"게임 준비 완료 요청");
        }

        void PacketProcess_ReadyOmokNotify(byte[] packetData)
        {
            var notifyPkt = MemoryPackSerializer.Deserialize<PKTNtfReadyOmok>(packetData);

            if (notifyPkt.IsReady)
            {
                DevLog.Write($"[{notifyPkt.UserID}]님은 게임 준비 완료");
            }
            else
            {
                DevLog.Write($"[{notifyPkt.UserID}]님이 게임 준비 완료 취소");
            }

        }

        void PacketProcess_LoginResponse(byte[] packetData)
        {
            var responsePkt = MemoryPackSerializer.Deserialize<PKTResLogin>(packetData);

            DevLog.Write($"로그인 결과:  {(ERROR_CODE)responsePkt.Result}");
        }

        void PacketProcess_StartOmokNotify(byte[] packetData)
        {
            var isMyTurn = false;

            var notifyPkt = MemoryPackSerializer.Deserialize<PKTNtfStartOmok>(packetData);

            if (notifyPkt.FirstUserID == UserIDTextBox.Text)
            {
                isMyTurn = true;
            }

            StartGame(isMyTurn, UserIDTextBox.Text, GetOtherPlayer(UserIDTextBox.Text));

            DevLog.Write($"게임 시작. 흑돌 플레이어: {notifyPkt.FirstUserID}");
        }

        void PacketProcess_ChangeTurn(byte[] packetData)
        {
            var turnInfo = MemoryPackSerializer.Deserialize<PKTNtfChangeTurn>(packetData); // 패킷 데이터를 PKTNtfChangeTurn 클래스로 역직렬화
            IsMyTurn = !IsMyTurn; // 턴 전환
            DevLog.Write("턴이 변경되었습니다. 현재 턴: " + (IsMyTurn ? "나" : "상대"));
            //panel1.Invalidate(); // UI 업데이트
            ChangeTurn();
            // 
        }
        void PacketProcess_PutMokResponse(byte[] packetData)
        {
            var responsePkt = MemoryPackSerializer.Deserialize<PKTResPutMok>(packetData);

            DevLog.Write($"오목 놓기 실패 ");

            //TODO 방금 놓은 오목 정보를 취소 시켜야 한다
        }


        void PacketProcess_PutMokNotify(byte[] packetData)
        {
            var notifyPkt = MemoryPackSerializer.Deserialize<PKTNtfPutMok>(packetData);
            플레이어_돌두기(true, notifyPkt.PosX, notifyPkt.PosY);

            DevLog.Write($"오목 정보: X: {notifyPkt.PosX},  Y: {notifyPkt.PosY},   알:{notifyPkt.Mok}");
        }


        void PacketProcess_EndOmokNotify(byte[] packetData)
        {
            var notifyPkt = MemoryPackSerializer.Deserialize<PKTNtfEndOmok>(packetData);
            EndGame();

            DevLog.Write($"오목 GameOver: Win: {notifyPkt.WinUserID}");

            if (notifyPkt.WinUserID == null)
            {
                MessageBox.Show("무승부.", "게임 종료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (notifyPkt.WinUserID == UserIDTextBox.Text)
            {
                MessageBox.Show("승리하였습니다.", "게임 종료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("패배하였습니다.", "게임 종료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            //MessageBox.Show($"{notifyPkt.WinUserID}가 승리했습니다!", "게임 종료", MessageBoxButtons.OK, MessageBoxIcon.Information); // 승리 팝업 표시
        }
    }
}
