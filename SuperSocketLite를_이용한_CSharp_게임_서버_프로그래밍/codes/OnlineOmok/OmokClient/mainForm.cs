using CSCommon;
using MemoryPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.RegularExpressions;

#pragma warning disable CA1416

namespace OmokClient
{
    [SupportedOSPlatform("windows10.0.177630")]
    public partial class mainForm : Form
    {
        ClientSimpleTcp Network = new ClientSimpleTcp();

        bool IsNetworkThreadRunning = false;
        bool IsBackGroundProcessRunning = false;

        System.Threading.Thread NetworkReadThread = null;
        System.Threading.Thread NetworkSendThread = null;

        PacketBufferManager PacketBuffer = new PacketBufferManager();
        //ConcurrentQueue<byte[]> RecvPacketQueue = new ConcurrentQueue<byte[]>();
        //ConcurrentQueue<byte[]> SendPacketQueue = new ConcurrentQueue<byte[]>();
        Queue<byte[]> RecvPacketQueue = new Queue<byte[]>();
        Queue<byte[]> SendPacketQueue = new Queue<byte[]>();

        System.Windows.Forms.Timer dispatcherUITimer = new();

        private System.Windows.Forms.Timer heartBeatTimer;
        private HttpClient httpClient = new HttpClient();

        // 매칭
        private System.Windows.Forms.Timer matchTimer;
        private bool isMatching = false;

        private string HeaderGameToken;
        private long HeaderUserId;

        public mainForm()
        {
            InitializeComponent();
            //InitializeHttpClient();

            heartBeatTimer = new System.Windows.Forms.Timer();
            heartBeatTimer.Interval = 1000; // 1초마다
            heartBeatTimer.Tick += HeartBeatTimer_Tick;
        }

        //private void InitializeHttpClient()
        //{
        //    // HttpClient 기본 설정
        //    httpClient.BaseAddress = new Uri($"{HiveIPTextBox.Text}:{HivePortTextBox.Text}");
        //    httpClient.DefaultRequestHeaders.Accept.Clear();
        //    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        //}

        private void mainForm_Load(object sender, EventArgs e)
        {
            PacketBuffer.Init((8096 * 10), MemoryPackPacketHeadInfo.HeadSize, 2048);

            IsNetworkThreadRunning = true;
            NetworkReadThread = new System.Threading.Thread(this.NetworkReadProcess);
            NetworkReadThread.Start();
            NetworkSendThread = new System.Threading.Thread(this.NetworkSendProcess);
            NetworkSendThread.Start();

            IsBackGroundProcessRunning = true;
            dispatcherUITimer.Tick += new EventHandler(BackGroundProcess);
            dispatcherUITimer.Interval = 100;
            dispatcherUITimer.Start();

            btnSocketDisconnect.Enabled = false;

            SetPacketHandler(); // 패킷 핸들러 설정


            Omok_Init();
            DevLog.Write("프로그램 시작 !!!", LOG_LEVEL.INFO);
        }

        private void mainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            IsNetworkThreadRunning = false;
            IsBackGroundProcessRunning = false;

            Network.Close();
        }

        private void btnConnect_Click(object sender, EventArgs e) // [오목 대전 서버 접속]
        {
            string address = socketIPTextBox.Text;

            if (checkBoxLocalHostIPSocket.Checked) // LocalHost 체크 상태 확인
            {
                address = "127.0.0.1";
            }

            int port = Convert.ToInt32(socketPortTextBox.Text);

            if (Network.Connect(address, port))
            {
                labelStatus.Text = string.Format("{0}. 서버에 접속 중", DateTime.Now);
                btnSocketConnect.Enabled = false;
                btnSocketDisconnect.Enabled = true;

                DevLog.Write($"서버에 접속 중", LOG_LEVEL.INFO);

                // 서버 연결 성공 후 타이머 시작
                // heartBeatTimer.Start();
            }
            else
            {
                labelStatus.Text = string.Format("{0}. 서버에 접속 실패", DateTime.Now);
            }

            PacketBuffer.Clear();
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            SetDisconnectd();
            Network.Close();

            // 연결 해제 시 타이머 중지
            heartBeatTimer.Stop();
        }



        void NetworkReadProcess()
        {
            while (IsNetworkThreadRunning)
            {
                if (Network.IsConnected() == false)
                {
                    System.Threading.Thread.Sleep(1);
                    continue;
                }

                var recvData = Network.Receive();

                if (recvData != null)
                {
                    PacketBuffer.Write(recvData.Item2, 0, recvData.Item1);

                    while (true)
                    {
                        var data = PacketBuffer.Read();
                        if (data == null)
                        {
                            break;
                        }

                        RecvPacketQueue.Enqueue(data);
                    }
                    //DevLog.Write($"받은 데이터: {recvData.Item2}", LOG_LEVEL.INFO);
                }
                else
                {
                    Network.Close();
                    SetDisconnectd();
                    DevLog.Write("서버와 접속 종료 !!!", LOG_LEVEL.INFO);
                }
            }
        }

        void NetworkSendProcess()
        {
            while (IsNetworkThreadRunning)
            {
                System.Threading.Thread.Sleep(1);

                if (Network.IsConnected() == false)
                {
                    continue;
                }

                lock (((System.Collections.ICollection)SendPacketQueue).SyncRoot)
                {
                    if (SendPacketQueue.Count > 0)
                    {
                        var packet = SendPacketQueue.Dequeue();
                        Network.Send(packet);
                    }
                }
                //if (SendPacketQueue.TryDequeue(out var packet))
                //{
                //    Network.Send(packet);
                //}
            }
        }


        void BackGroundProcess(object sender, EventArgs e)
        {
            ProcessLog();

            try
            {
                byte[] packet = null;

                lock (((System.Collections.ICollection)RecvPacketQueue).SyncRoot)
                {
                    if (RecvPacketQueue.Count() > 0)
                    {
                        packet = RecvPacketQueue.Dequeue();
                    }
                }

                if (packet != null)
                {
                    PacketProcess(packet);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("ReadPacketQueueProcess. error:{0}", ex.Message));
            }
        }

        private void ProcessLog()
        {
            // 너무 이 작업만 할 수 없으므로 일정 작업 이상을 하면 일단 패스한다.
            int logWorkCount = 0;

            while (IsBackGroundProcessRunning)
            {
                System.Threading.Thread.Sleep(1);

                string msg;

                if (DevLog.GetLog(out msg))
                {
                    ++logWorkCount;

                    if (listBoxLog.Items.Count > 512)
                    {
                        listBoxLog.Items.Clear();
                    }

                    listBoxLog.Items.Add(msg);
                    listBoxLog.SelectedIndex = listBoxLog.Items.Count - 1;
                }
                else
                {
                    break;
                }

                if (logWorkCount > 8)
                {
                    break;
                }
            }
        }


        public void SetDisconnectd()
        {
            if (btnSocketConnect.Enabled == false)
            {
                btnSocketConnect.Enabled = true; // TODO 서버 강제 종료 시 ?
                btnSocketDisconnect.Enabled = false;
            }

            //while (true)
            //{
            //    if (SendPacketQueue.TryDequeue(out var temp) == false)
            //    {
            //        break;
            //    }
            //}

            listBoxRoomChatMsg.Items.Clear(); // ?
            listBoxRoomUserList.Items.Clear();

            EndGame();

            labelStatus.Text = "서버 접속이 끊어짐";
        }

        void PostSendPacket(PACKETID packetID, byte[] packetData)
        {
            if (Network.IsConnected() == false)
            {
                DevLog.Write("서버 연결이 되어 있지 않습니다", LOG_LEVEL.ERROR);
                return;
            }

            // packetData가 null인 경우 비어 있는 바이트 배열로 처리
            // new byte[MemoryPackPacketHeadInfo.HeadSize]
            //if (packetData == null)
            //{
            //    packetData = new byte[0];
            //}

            var header = new MemoryPackPacketHeadInfo();
            header.TotalSize = (UInt16)packetData.Length;
            header.Id = (UInt16)packetID;
            header.Type = 0;
            header.Write(packetData);

            SendPacketQueue.Enqueue(packetData);
        }


        void AddRoomUserList(string userID)
        {
            listBoxRoomUserList.Items.Add(userID);
        }

        void RemoveRoomUserList(string userID)
        {
            object removeItem = null;

            foreach (var user in listBoxRoomUserList.Items)
            {
                if ((string)user == userID)
                {
                    removeItem = user;
                    break;
                }
            }

            if (removeItem != null)
            {
                listBoxRoomUserList.Items.Remove(removeItem);
            }
        }

        static public string ToReadableByteArray(byte[] bytes)
        {
            return string.Join(", ", bytes);
        }

        string GetOtherPlayer(string myName)
        {
            if (listBoxRoomUserList.Items.Count != 2)
            {
                return null;
            }

            var firstName = (string)listBoxRoomUserList.Items[0];
            if (firstName == myName)
            {
                return firstName;
            }
            else
            {
                return (string)listBoxRoomUserList.Items[1];
            }
        }

        private void HeartBeatTimer_Tick(object sender, EventArgs e) // HeartBeat
        {
            var HeartBeatReq = new PKTReqHeartBeat();

            var packetData = MemoryPackSerializer.Serialize(HeartBeatReq);
            PostSendPacket(PACKETID.ReqHeartBeat, packetData);

            //DevLog.Write("HeartBeat 요청");
        }

        private bool ValidateInputs()
        {
            // 이메일 유효성 검사
            if (!Regex.IsMatch(UserIDTextBox.Text, @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$"))
            {
                MessageBox.Show("EMAIL IS NOT IN A VALID FORMAT OR IS TOO LONG.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // 비밀번호 유효성 검사
            if (UserPWTextBox.Text.Length < 6)
            {
                MessageBox.Show("Password must be at least 6 characters long.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (UserPWTextBox.Text.Length > 30)
            {
                MessageBox.Show("PASSWORD IS TOO LONG", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        // 회원가입 요청
        private async void btnRegister_Click(object sender, EventArgs e)
        {
            // 입력 값 검증
            if (!ValidateInputs())
            {
                return; // 검증 실패 시 더 이상 진행하지 않음
            }
            string email = UserIDTextBox.Text;
            string password = UserPWTextBox.Text;
            var registerResponse = await RegisterAsync(email, password);

            if (registerResponse != null && registerResponse.result == 0)
            {
                labelStatus.Text = "회원가입 성공";
                DevLog.Write("회원가입에 성공했습니다.");
            }
            else
            {
                labelStatus.Text = "회원가입 실패";
                MessageBox.Show("회원가입에 실패했습니다.");
            }
        }
        private async Task<RegisterResponse> RegisterAsync(string email, string password)
        {
            var loginUrl = "http://34.22.95.236:5092/account/register";
            if (checkBoxLocalHostIPHive.Checked) // LocalHost 체크 상태 확인
            {
                loginUrl = "http://localhost:5092/account/register";
            }
            var loginData = new { UserId = email, Password = password };
            var content = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(loginUrl, content);
                Debug.WriteLine($"response : {response}");
                var responseBody = await response.Content.ReadAsStringAsync();


                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<RegisterResponse>(responseBody);
                }
            }
            catch (Exception ex)
            {
                // 회원가입 실패 이유 로그로 보여주기

                Debug.WriteLine($"회원가입 중 오류가 발생했습니다: {ex.Message}");
            }

            return null;
        }

        // 로그인 요청
        private async void button_Login_Click(object sender, EventArgs e)
        {
            // 대전 서버에 로그인 정보 전송 [패킷 전송] - 로그인
            string userID = UserIDTextBox.Text;
            string password = UserPWTextBox.Text;

            var loginReq = new PKTReqLogin();
            loginReq.AuthToken = password;
            loginReq.UserID = userID;
            var packet = MemoryPackSerializer.Serialize(loginReq);
            PostSendPacket(PACKETID.ReqLogin, packet);
            DevLog.Write($"로그인 요청: {userID}, {password}");


            /*string email = UserIDTextBox.Text;
            string password = UserPWTextBox.Text;
            var loginResponse = await LoginAsync(email, password);
            DevLog.Write($" loginResponse : {loginResponse} ");


            if (loginResponse != null && loginResponse.result == 0)
            {
                DevLog.Write($"{loginResponse.userNum} , {loginResponse.hiveToken}");
                labelStatus.Text = "하이브 로그인 성공";
                DevLog.Write("하이브 로그인에 성공했습니다.");

                // 추가 API Game 로그인 과정
                if (email == null) { return; }
                var gameLoginResponse = await GameLoginAsync(loginResponse.userNum, email, loginResponse.hiveToken);
                if (gameLoginResponse != null && gameLoginResponse.result == 0)
                {
                    DevLog.Write("API Game 서버 로그인 성공");
                    DataUserId.Text = gameLoginResponse.userGameData.userNum.ToString();
                    DataLevel.Text = gameLoginResponse.userGameData.level.ToString();
                    DataExp.Text = gameLoginResponse.userGameData.exp.ToString();
                    DataWin.Text = gameLoginResponse.userGameData.win.ToString();
                    DataLose.Text = gameLoginResponse.userGameData.lose.ToString();
                    DataDraw.Text = gameLoginResponse.userGameData.draw.ToString();

                    // 대전 서버에 로그인 정보 전송 [패킷 전송] - 로그인
                    //var loginReq = new PKTReqLogin();
                    //loginReq.AuthToken = password;
                    //loginReq.UserID = email;
                    //var packet = MemoryPackSerializer.Serialize(loginReq);
                    //PostSendPacket(PACKETID.ReqLogin, packet);
                    //DevLog.Write($"로그인 요청: {email}, {password}");
                }
                else
                {
                    DevLog.Write("API Game 서버 인증 실패");
                    if (gameLoginResponse == null)
                    {
                        DevLog.Write("gameLoginResponse == null");
                    }
                    else if (gameLoginResponse.result != 0)
                    {
                        DevLog.Write($"gameLoginResponse.Result: {gameLoginResponse.result}");
                    }
                    else
                    {
                        DevLog.Write("?????");
                    }
                }
            }
            else
            {
                labelStatus.Text = "로그인 실패";
                MessageBox.Show("로그인에 실패했습니다.");
            }*/
        }

        private async Task<LoginResponse> LoginAsync(string email, string password)
        {
            var loginUrl = "http://34.22.95.236:5092/login";
            if (checkBoxLocalHostIPHive.Checked) // LocalHost 체크 상태 확인
            {
                loginUrl = "http://localhost:5092/login";
            }

            var loginData = new { UserId = email, Password = password };
            var content = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(loginUrl, content);
                Debug.WriteLine($"response : {response}");

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"responseBody : {responseBody}");
                    return JsonSerializer.Deserialize<LoginResponse>(responseBody);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"하이브 로그인 중 오류가 발생했습니다: {ex.Message}");
            }

            return null;
        }

        private async Task<GameLoginResponse> GameLoginAsync(long userId, string email, string hiveToken)
        {
            var loginUrl = "http://34.22.95.236:5022/login";
            if (checkBoxLocalHostIPGame.Checked) // LocalHost 체크 상태 확인
            {
                loginUrl = "http://localhost:5022/login";
            }
            var loginData = new { UserNum = userId, UserId = email, HiveToken = hiveToken };
            var content = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");

            HeaderUserId = userId;

            try
            {
                var response = await httpClient.PostAsync(loginUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();

                    var LoginResponse = JsonSerializer.Deserialize<GameLoginResponse>(responseBody);
                    HeaderGameToken = LoginResponse.token;

                    return LoginResponse;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"API Game 서버 로그인 중 오류가 발생했습니다: {ex.Message}");
            }

            return null;
        }

        private async Task<VerifyResponse> VerifyTokenAsync(string userId, string hiveToken)
        {
            //var verifyUrl = "http://localhost:5092/VerifyToken";
            var verifyUrl = "http://34.22.95.236:5092/VerifyToken";
            if (checkBoxLocalHostIPHive.Checked) // LocalHost 체크 상태 확인
            {
                verifyUrl = "http://localhost:5092/VerifyToken";
            }

            var verifyData = new { UserID = userId, HiveToken = hiveToken }; // Email
            var content = new StringContent(JsonSerializer.Serialize(verifyData), Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(verifyUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<VerifyResponse>(responseBody);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hive 토큰 검증 중 오류가 발생했습니다: {ex.Message}");
            }

            return null;
        }

        /*
         private void button_Login_Click(object sender, EventArgs e)
        {
            var loginReq = new PKTReqLogin();
            loginReq.AuthToken = UserPWTextBox.Text;
            loginReq.UserID = UserIDTextBox.Text;
            var packet = MemoryPackSerializer.Serialize(loginReq);

            PostSendPacket(PACKETID.ReqLogin, packet);
            DevLog.Write($"로그인 요청:  {UserIDTextBox.Text}, {UserPWTextBox.Text}");
            DevLog.Write($"로그인 요청: {ToReadableByteArray(packet)}");
        }
         */
        ///////////////
        // 매칭요청버튼
        private async void btnMatching_Click_1(object sender, EventArgs e)
        {
            DevLog.Write("btnMatching_Click_1");

            var email = UserIDTextBox.Text;

            var matchUrl = "http://34.22.95.236:5022/match/request";
            if (checkBoxLocalHostIPGame.Checked) // LocalHost 체크 상태 확인
            {
                matchUrl = "http://localhost:5022/match/request";
            }

            // 매칭 요청
            var matchRequest = new MatchRequest { UserId = email };
            var content = new StringContent(JsonSerializer.Serialize(matchRequest), Encoding.UTF8, "application/json");

            HttpClient httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Add("UserId", HeaderUserId.ToString());
            httpClient.DefaultRequestHeaders.Add("GameToken", HeaderGameToken);

            var response = await httpClient.PostAsync(matchUrl, content);
            DevLog.Write("매칭 요청 완료 : 기다리는 중 ...");

            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show("매칭 요청에 실패했습니다.");
                return;
            }

            // 매칭 상태 확인 타이머 시작
            isMatching = true;
            matchTimer = new System.Windows.Forms.Timer();
            matchTimer.Interval = 2000; // 2초마다
            matchTimer.Tick += (s, args) => CheckMatchStatus(email);
            matchTimer.Start();
        }
        private async Task CheckMatchStatus(string email)
        {
            DevLog.Write("CheckMatchStatus");

            if (!isMatching)
                return;

            var matchRequest = new MatchRequest { UserId = email };

            var matchUrl = "http://34.22.95.236:5022/match/ismatched";
            if (checkBoxLocalHostIPGame.Checked) // LocalHost 체크 상태 확인
            {
                matchUrl = "http://localhost:5022/match/ismatched";
            }

            HttpClient httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Add("UserId", HeaderUserId.ToString());
            httpClient.DefaultRequestHeaders.Add("GameToken", HeaderGameToken);

            var content = new StringContent(JsonSerializer.Serialize(matchRequest), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(matchUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                DevLog.Write(responseBody);
                var matchCompleteResponse = JsonSerializer.Deserialize<MatchCompleteResponse>(responseBody);

                if (matchCompleteResponse != null && matchCompleteResponse.success == 1)
                {
                    DevLog.Write("[매칭 완료]");
                    // 매칭 완료 시 타이머 중지
                    matchTimer.Stop();
                    isMatching = false;

                    textBoxRoomNumber.Text = matchCompleteResponse.roomNum.ToString(); // 방 번호 설정

                    var userId = matchCompleteResponse.userId;
                    var serverAddress = matchCompleteResponse.serverAddress;

                    // 소켓 서버와 연결
                    var addressParts = serverAddress.Split(':');
                    var socketIp = addressParts[0];
                    var socketPort = int.Parse(addressParts[1]);

                    // socketIp가 localhost라면 127.0.0.1로 변경
                    if (socketIp == "localhost")
                    {
                        socketIp = "127.0.0.1";
                    }

                    if (Network.Connect(socketIp, socketPort))
                    {
                        DevLog.Write($"소켓 서버에 접속 성공: {serverAddress}", LOG_LEVEL.INFO);

                        // 유저 로그인 처리
                        var loginReq = new PKTReqLogin();
                        loginReq.AuthToken = HeaderGameToken;
                        loginReq.UserID = email;
                        var loginPacket = MemoryPackSerializer.Serialize(loginReq);
                        PostSendPacket(PACKETID.ReqLogin, loginPacket);

                        DevLog.Write($"유저 로그인 요청: {email}, {HeaderGameToken}");

                        // 방 입장 패킷 전송
                        EnterRoom(matchCompleteResponse.roomNum); // 방 입장 요청

                        //// 매칭 완료 시 타이머 중지
                        //matchTimer.Stop();
                        //isMatching = false;
                    }
                    else
                    {
                        DevLog.Write("소켓 서버에 접속 실패", LOG_LEVEL.ERROR);
                    }
                }
            }
            else
            {
                DevLog.Write("매칭 상태 확인에 실패했습니다.");
            }
        }
        //private async Task CheckMatchStatus(string email)
        //{
        //    var matchRequest = new MatchRequest { Email = email };

        //    var matchUrl = "http://34.22.95.236:5167/match/ismatched";
        //    if (checkBoxLocalHostIPGame.Checked) // LocalHost 체크 상태 확인
        //    {
        //        matchUrl = "http://localhost:5167/match/ismatched";
        //    }

        //    while (true)
        //    {
        //        await Task.Delay(1000); // 1초 대기

        //        var content = new StringContent(JsonSerializer.Serialize(matchRequest), Encoding.UTF8, "application/json");
        //        var response = await httpClient.PostAsync(matchUrl, content);

        //        if (response.IsSuccessStatusCode)
        //        {
        //            var responseBody = await response.Content.ReadAsStringAsync();
        //            var matchCompleteResponse = JsonSerializer.Deserialize<MatchCompleteResponse>(responseBody);

        //            if (matchCompleteResponse != null && matchCompleteResponse.Success == 1)
        //            {
        //                textBoxRoomNumber.Text = matchCompleteResponse.RoomNum.ToString(); // 방 번호 설정
        //                EnterRoom(matchCompleteResponse.RoomNum);
        //                break;
        //            }
        //        }
        //        else
        //        {
        //            DevLog.Write("매칭 상태 확인에 실패했습니다.");
        //        }
        //    }
        //}
        private async void btnCancelMatching_Click(object sender, EventArgs e)
        {
            var email = UserIDTextBox.Text;

            var matchUrl = "http://34.22.95.236:5022/match/cancel";
            if (checkBoxLocalHostIPGame.Checked) // LocalHost 체크 상태 확인
            {
                matchUrl = "http://localhost:5022/match/cancel";
            }

            // 매칭 취소 요청
            var matchRequest = new MatchRequest { UserId = email };
            var content = new StringContent(JsonSerializer.Serialize(matchRequest), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(matchUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var cancelResponse = JsonSerializer.Deserialize<MatchCancelResponse>(responseBody);

                if (cancelResponse != null && cancelResponse.Result == ErrorCode.None)
                {
                    // 매칭 취소 완료 시 타이머 중지
                    matchTimer.Stop();
                    isMatching = false;
                    MessageBox.Show("[매칭 취소 완료]");
                }
                else if (cancelResponse != null && cancelResponse.Result == ErrorCode.MatchNotFound)
                {
                    MessageBox.Show($"매칭 요청을 찾을 수 없습니다. 이유: {cancelResponse.Message}");
                }
                else
                {
                    MessageBox.Show($"[매칭 취소 실패] 이유: {cancelResponse.Message}");
                }
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var cancelResponse = JsonSerializer.Deserialize<MatchCancelResponse>(responseBody);

                if (cancelResponse != null)
                {
                    MessageBox.Show($"[매칭 취소 실패] 이유: {cancelResponse.Message}");
                }
                else
                {
                    MessageBox.Show("[매칭 취소 실패] 서버와의 통신 오류입니다.");
                }
            }
        }

        // 대전 서버에 방 입장 정보 전송 [패킷 전송] - 방입장
        private void EnterRoom(int roomNum)
        {
            var requestPkt = new PKTReqRoomEnter(); // 방 입장 요청
            requestPkt.RoomNumber = roomNum; // 방 번호

            var sendPacketData = MemoryPackSerializer.Serialize(requestPkt); // 직렬화
            PostSendPacket(PACKETID.ReqRoomEnter, sendPacketData); // 패킷 전송

            DevLog.Write($"방 입장 요청:  {roomNum} 번");
        }


        private void btn_RoomEnter_Click(object sender, EventArgs e)
        {
            var requestPkt = new PKTReqRoomEnter(); // 방 입장 요청
            requestPkt.RoomNumber = textBoxRoomNumber.Text.ToInt32(); // 방 번호

            var sendPacketData = MemoryPackSerializer.Serialize(requestPkt); // 직렬화

            PostSendPacket(PACKETID.ReqRoomEnter, sendPacketData); // 패킷 전송
            DevLog.Write($"방 입장 요청:  {textBoxRoomNumber.Text} 번");
        }

        private void btn_RoomLeave_Click(object sender, EventArgs e)
        {
            //PostSendPacket(PACKET_ID.ROOM_LEAVE_REQ,  null);
            PostSendPacket(PACKETID.ReqRoomLeave, new byte[MemoryPackPacketHeadInfo.HeadSize]);
            DevLog.Write($"방 퇴장 요청:  {textBoxRoomNumber.Text} 번");
        }

        //private void btnRoomChat_Click(object sender, EventArgs e)
        //{
        //    if (textBoxRoomSendMsg.Text.IsEmpty())
        //    {
        //        MessageBox.Show("채팅 메시지를 입력하세요");
        //        return;
        //    }

        //    var requestPkt = new RoomChatReqPacket();
        //    requestPkt.SetValue(textBoxRoomSendMsg.Text);

        //    PostSendPacket(PACKETID.REQ_ROOM_CHAT, requestPkt.ToBytes());
        //    DevLog.Write($"방 채팅 요청");
        //}

        ////private void btnMatching_Click(object sender, EventArgs e)
        ////{
        ////    PostSendPacket(PACKETID.MATCH_USER_REQ, null);
        ////    DevLog.Write($"매칭 요청");
        ////}

        private void btnRoomChat_Click(object sender, EventArgs e)
        {
            if (textBoxRoomSendMsg.Text.IsEmpty())
            {
                MessageBox.Show("채팅 메시지를 입력하세요");
                return;
            }

            var requestPkt = new PKTReqRoomChat();
            requestPkt.ChatMessage = textBoxRoomSendMsg.Text;

            var sendPacketData = MemoryPackSerializer.Serialize(requestPkt);

            PostSendPacket(PACKETID.ReqRoomChat, sendPacketData);
            DevLog.Write($"방 채팅 요청");
        }

        //private void btnRoomRelay_Click(object sender, EventArgs e)
        //{
        //    if (textBoxRelay.Text.IsEmpty())
        //    {
        //        MessageBox.Show("릴레이 할 데이터가 없습니다");
        //        return;
        //    }

        //    /*var bodyData = Encoding.UTF8.GetBytes(textBoxRelay.Text);
        //    PostSendPacket(PACKET_ID.PACKET_ID_ROOM_RELAY_REQ, bodyData);
        //    DevLog.Write($"방 릴레이 요청");*/
        //}


        private void listBoxRoomChatMsg_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void textBoxRelay_TextChanged(object sender, EventArgs e)
        {

        }


        void SendPacketOmokPut(int x, int y)
        {
            // 현재 돌의 종류를 확인
            int stoneType = OmokLogic.Is흑돌차례() ? (int)CSCommon.OmokRule.돌종류.흑돌 : (int)CSCommon.OmokRule.돌종류.백돌;

            var requestPkt = new PKTReqPutMok
            {
                PosX = x,
                PosY = y
            };

            var packet = MemoryPackSerializer.Serialize(requestPkt);
            PostSendPacket(PACKETID.ReqPutMok, packet);

            DevLog.Write($"put stone 요청 : x  [ {x} ], y: [ {y} ], 돌: [{stoneType}]");
        }

        private void btn_GameStartClick(object sender, EventArgs e)
        {
            DevLog.Write("게임 시작 버튼 없음 - 둘다 Ready 하면 알아서 시작함");
            //PostSendPacket(PACKETID.REQ_GAME_START, new byte[MemoryPackPacketHeadInfo.HeadSize]);
            //StartGame(true, "My", "Other");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            AddUser("test1");
            AddUser("test2");
        }

        void AddUser(string userID)
        {
            var value = new PvPMatchingResult
            {
                IP = "127.0.0.1",
                Port = 32451,
                RoomNumber = 0,
                Index = 1,
                Token = "123qwe"
            };
            var saveValue = MemoryPackSerializer.Serialize(value);

            var key = "ret_matching_" + userID;

            var redisConfig = new CloudStructures.RedisConfig("omok", "127.0.0.1");
            var RedisConnection = new CloudStructures.RedisConnection(redisConfig);

            var v = new CloudStructures.Structures.RedisString<byte[]>(RedisConnection, key, null);
            var ret = v.SetAsync(saveValue).Result;
        }

        //[MemoryPackable]
        public class PvPMatchingResult
        {
            public string IP;
            public UInt16 Port;
            public Int32 RoomNumber;
            public Int32 Index;
            public string Token;
        }

        // 게임 준비 요청
        private void button3_Click(object sender, EventArgs e)
        {
            PostSendPacket(PACKETID.ReqReadyOmok, new byte[MemoryPackPacketHeadInfo.HeadSize]);

            DevLog.Write($"게임 준비 완료 요청");
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void socketPortTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void socketIPTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void HivePortTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void checkBoxLocalHostIPHive_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxLocalHostIPHive.Checked)
            {
                HiveIPTextBox.Text = "127.0.0.1";
            }
        }

        private void checkBoxLocalHostIPGame_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxLocalHostIPGame.Checked)
            {
                GameIPTextBox.Text = "127.0.0.1";
            }
        }

        private void checkBoxLocalHostIPSocket_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxLocalHostIPSocket.Checked)
            {
                socketIPTextBox.Text = "127.0.0.1";
            }
        }

        private void UserPWText_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBoxRoomSendMsg_TextChanged(object sender, EventArgs e)
        {

        }

        private void GameDataGroup_Enter(object sender, EventArgs e)
        {

        }
    }

    internal class RegisterResponse
    {
        public int result { get; set; }
    }

    public class LoginResponse
    {
        public int result { get; set; }
        public long userNum { get; set; }
        public string hiveToken { get; set; }
    }

    public class VerifyResponse
    {
        public int result { get; set; }
    }
    public class GameLoginResponse
    {
        [Required]
        public int result { get; set; }

        public string token { get; set; }
        public long uid { get; set; }
        public UserGameData userGameData { get; set; }
    }

    public class UserGameData
    {
        public long userNum { get; set; }
        public int level { get; set; }
        public int exp { get; set; }
        public int win { get; set; }
        public int lose { get; set; }
        public int draw { get; set; }
    }
    public class MatchRequest
    {
        public string UserId { get; set; }
    }

    public class MatchResponse
    {
        public ErrorCode Result { get; set; }
    }

    public class MatchCompleteResponse
    {
        public ErrorCode result { get; set; }
        public int success { get; set; }
        public string userId { get; set; }
        public int roomNum { get; set; }
        public string serverAddress { get; set; }
    }
    public class MatchCancelResponse
    {
        public ErrorCode Result { get; set; }
        public string Message { get; set; }
    }

    public enum ErrorCode
    {
        None = 0,
        InvalidRequest = 2401,
        MatchNotFound = 2402
    }

}
