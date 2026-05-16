# 온라인 게임 클라이언트 개발자를 위한 네트워크 지식

저자: 최흥배, Claude AI   
    
-----  
  
## 목차 

## 1. 네트워크 기초 지식
### 1.1 네트워크 통신의 기본 개념
- 클라이언트-서버 모델
- P2P(Peer-to-Peer) 모델
- 패킷(Packet)의 개념과 구조

### 1.2 핵심 프로토콜
- TCP vs UDP: 특징과 게임에서의 활용
- HTTP/HTTPS: 웹 기반 통신
- WebSocket: 실시간 양방향 통신

### 1.3 필수 네트워크 용어
- IP 주소와 포트(Port)
- 지연시간(Latency)과 핑(Ping)
- 대역폭(Bandwidth)과 처리량(Throughput)
- 패킷 손실(Packet Loss)
- 지터(Jitter)

## 2. 게임 네트워크 아키텍처
### 2.1 동기화 방식의 이해
- 클라이언트-서버 동기화
- 상태 동기화 vs 명령 동기화
- 권한 있는 서버(Authoritative Server) 개념

### 2.2 네트워크 토폴로지
- 전용 서버(Dedicated Server)
- 리슨 서버(Listen Server)
- 클라우드 기반 서버

## 3. 클라이언트 개발자가 다루는 네트워크 코드
### 3.1 연결 관리
- 서버 연결 및 재연결 로직
- 연결 상태 모니터링
- 타임아웃 처리

### 3.2 데이터 송수신
- 직렬화(Serialization)와 역직렬화
- 메시지 큐(Message Queue) 관리
- 프로토콜 버퍼, JSON 등 데이터 포맷

### 3.3 네트워크 이벤트 처리
- 비동기 통신 처리
- 콜백과 이벤트 리스너
- 스레드 안전성(Thread Safety)

## 4. 온라인 게임의 주요 이슈와 해결 방법
### 4.1 지연 시간(Latency) 문제
- 클라이언트 측 예측(Client-side Prediction)
- 보간(Interpolation)과 외삽(Extrapolation)
- 입력 버퍼링(Input Buffering)

### 4.2 동기화 문제
- 플레이어 위치 동기화
- 애니메이션 동기화
- 게임 상태 불일치 해결

### 4.3 치팅 방지를 위한 클라이언트 설계
- 서버 검증의 중요성
- 클라이언트에서 보내는 데이터 최소화
- 안티-치트 시스템과의 협력

### 4.4 패킷 손실 대응
- 중요 데이터의 재전송 로직
- UDP에서의 신뢰성 구현
- 데이터 우선순위 관리

### 4.5 대역폭 최적화
- 델타 압축(Delta Compression)
- 관심 영역 관리(Area of Interest)
- 업데이트 빈도 조절

## 5. 실시간 게임플레이 구현
### 5.1 틱(Tick)과 프레임 개념
- 서버 틱 레이트(Tick Rate)
- 클라이언트 프레임 레이트와의 분리
- 틱 동기화

### 5.2 스냅샷 시스템
- 게임 상태 스냅샷
- 스냅샷 보간
- 시간 되감기(Rewinding)

### 5.3 랙 보상(Lag Compensation)
- 서버 측 히트 검증
- 타임스탬프 활용
- 공정성과 반응성의 균형

## 6. 장르별 네트워크 특성
### 6.1 FPS/TPS 게임
- 빠른 반응 속도 요구사항
- 히트스캔과 발사체 동기화
- 시야 체크와 가시성

### 6.2 MMORPG
- 대규모 플레이어 관리
- 인스턴스와 채널링

### 6.3 전략/카드 게임
- 턴 기반 통신
- 상태 검증
- 낮은 업데이트 빈도 활용

### 6.4 모바일 게임
- 불안정한 네트워크 환경 대응
- 배터리 및 데이터 사용량 고려
- 끊김 없는 재연결
  
---  


# 1. 네트워크 기초 지식

온라인 게임 클라이언트 개발자로서 첫걸음을 내딛기 위해서는 네트워크의 기본 개념을 확실히 이해해야 한다. 이 장에서는 게임 개발에 필수적인 네트워크 통신 모델, 프로토콜, 그리고 핵심 용어들을 다룬다. 서버 개발이 아닌 클라이언트 개발자의 관점에서 이러한 지식들이 왜 중요한지, 그리고 실제 게임 개발에서 어떻게 활용되는지 살펴본다.

## 1.1 네트워크 통신의 기본 개념

### 클라이언트-서버 모델

클라이언트-서버 모델은 현대 온라인 게임에서 가장 널리 사용되는 네트워크 아키텍처다. 이 모델에서 서버는 게임의 중앙 권한을 가지고 게임 상태를 관리하며, 클라이언트는 플레이어의 입력을 받아 서버에 전달하고 서버로부터 받은 정보를 화면에 표시한다.

클라이언트는 플레이어가 실행하는 게임 프로그램이다. 플레이어가 캐릭터를 움직이거나 공격 버튼을 누르면, 클라이언트는 이 입력을 처리하여 서버에 전송한다. 서버는 모든 클라이언트로부터 받은 입력을 처리하고, 게임 규칙에 따라 게임 세계의 상태를 업데이트한 뒤, 변경된 정보를 다시 각 클라이언트에게 전송한다.

이 모델의 가장 큰 장점은 서버가 게임의 진실된 상태를 관리한다는 점이다. 모든 중요한 계산과 검증이 서버에서 이루어지므로, 클라이언트 측에서 치트 프로그램을 사용하더라도 게임의 공정성을 어느 정도 보장할 수 있다. 예를 들어, FPS 게임에서 플레이어가 적을 맞췄다고 주장하더라도, 서버가 실제로 탄환이 적중했는지 검증한다.

클라이언트 개발자는 서버와의 통신을 관리하고, 서버로부터 받은 데이터를 기반으로 화면을 렌더링하는 역할을 담당한다. 또한 네트워크 지연을 숨기기 위한 다양한 기법들을 구현해야 한다. 예를 들어, 플레이어가 이동 명령을 내리면 서버의 응답을 기다리지 않고 즉시 화면상에서 캐릭터를 움직이는 것처럼 보이게 한다. 이를 클라이언트 측 예측이라고 하며, 나중에 서버의 실제 결과와 차이가 있다면 보정한다.

### P2P(Peer-to-Peer) 모델

P2P 모델에서는 중앙 서버 없이 각 클라이언트가 직접 다른 클라이언트와 통신한다. 모든 참여자가 동등한 위치에서 서로 데이터를 주고받는 구조다. 과거 많은 온라인 게임들이 이 방식을 사용했으며, 특히 소규모 멀티플레이어 게임에서 여전히 활용된다.

P2P 모델의 주된 장점은 별도의 서버 인프라가 필요 없다는 점이다. 플레이어들끼리 직접 연결되므로 서버 운영 비용이 들지 않으며, 중앙 서버가 다운되는 문제도 없다. 또한 지연 시간 측면에서도 유리할 수 있다. 두 플레이어가 서로 가까운 지역에 있다면, 중앙 서버를 거치지 않고 직접 통신하므로 더 빠른 반응 속도를 얻을 수 있다.

하지만 P2P 모델에는 치명적인 단점들이 있다. 가장 큰 문제는 보안과 치팅 방지다. 중앙 서버가 없으므로 게임 상태를 검증할 권위 있는 주체가 없다. 악의적인 플레이어가 자신의 클라이언트를 수정하여 부정행위를 할 수 있으며, 다른 플레이어들이 이를 막기 어렵다. 또한 네트워크 연결 관리가 복잡하다. 플레이어 한 명이 연결이 끊기면 게임 전체에 영향을 줄 수 있으며, NAT(Network Address Translation) 환경에서 직접 연결을 수립하는 것도 기술적으로 까다롭다.

현대 온라인 게임에서 순수한 P2P 모델은 거의 사용되지 않는다. 대신 하이브리드 방식이 사용되기도 하는데, 게임 매치메이킹이나 로비는 서버를 통하지만 실제 게임플레이는 P2P로 진행하는 식이다. 격투 게임이나 레이싱 게임 같은 1대1 또는 소규모 대전 게임에서 이런 방식을 볼 수 있다.

클라이언트 개발자가 P2P 게임을 개발할 때는 NAT 통과(NAT traversal) 기술, 피어 간 동기화 로직, 그리고 연결이 불안정한 피어를 처리하는 방법 등을 이해해야 한다.

### 패킷(Packet)의 개념과 구조

패킷은 네트워크를 통해 전송되는 데이터의 기본 단위다. 게임 클라이언트와 서버가 주고받는 모든 정보는 패킷의 형태로 전송된다. 예를 들어, "플레이어가 앞으로 이동했다"는 정보나 "적 캐릭터의 현재 위치는 (100, 50, 200)이다"와 같은 데이터가 모두 패킷으로 만들어져 네트워크를 통해 전달된다.

패킷은 크게 두 부분으로 구성된다. 헤더(Header)와 페이로드(Payload)다. 헤더에는 패킷을 목적지까지 전달하는 데 필요한 메타데이터가 들어있다. 출발지 IP 주소, 목적지 IP 주소, 포트 번호, 패킷 타입, 순서 번호 등의 정보가 포함된다. 페이로드는 실제로 전송하려는 데이터, 즉 게임 정보가 담긴 부분이다.

게임 개발에서는 일반적으로 애플리케이션 계층의 패킷을 다룬다. 예를 들어, "캐릭터 이동" 패킷을 만든다면 다음과 같은 구조를 가질 수 있다.

```
[패킷 타입: 2바이트] [플레이어 ID: 4바이트] [X 좌표: 4바이트] [Y 좌표: 4바이트] [Z 좌표: 4바이트] [회전: 4바이트]
```

이 패킷은 총 22바이트의 크기를 가지며, 서버나 다른 클라이언트가 이 패킷을 받으면 첫 2바이트를 읽어 "아, 이건 캐릭터 이동 패킷이구나"라고 인식하고, 나머지 데이터를 순서대로 파싱하여 해당 플레이어의 위치를 업데이트한다.

패킷 크기는 네트워크 성능에 직접적인 영향을 준다. 패킷이 너무 크면 전송 시간이 길어지고, 일부 네트워크 장비에서 패킷이 분할(fragmentation)될 수 있다. 반대로 패킷을 너무 작게 만들면 헤더의 오버헤드가 커진다. 일반적으로 UDP 패킷의 경우 MTU(Maximum Transmission Unit, 보통 1500바이트) 이하로 유지하는 것이 좋다.

클라이언트 개발자는 게임 데이터를 효율적으로 패킷에 담는 방법을 고민해야 한다. 예를 들어, 좌표 값을 32비트 float 대신 16비트 정수로 변환하여 전송하거나, 변경되지 않은 데이터는 보내지 않는 등의 최적화 기법을 사용한다. 또한 패킷이 손실되거나 순서가 뒤바뀔 수 있다는 점을 항상 고려하여 코드를 작성해야 한다.

## 1.2 핵심 프로토콜

### TCP vs UDP: 특징과 게임에서의 활용

TCP(Transmission Control Protocol)와 UDP(User Datagram Protocol)는 인터넷에서 데이터를 전송하는 두 가지 주요 프로토콜이다. 두 프로토콜은 전혀 다른 특성을 가지고 있으며, 게임 개발에서 각각 다른 용도로 사용된다.

TCP는 신뢰성 있는 연결 지향 프로토콜이다. "신뢰성"이란 전송한 데이터가 반드시 목적지에 도달하고, 보낸 순서대로 전달됨을 보장한다는 의미다. TCP는 연결을 수립하는 과정(3-way handshake)을 거친 후 데이터를 전송하며, 수신 측에서 데이터를 받았다는 확인(ACK)을 보내야 한다. 만약 패킷이 손실되면 자동으로 재전송한다.

이러한 특성 때문에 TCP는 정확성이 중요한 데이터 전송에 적합하다. 게임에서는 채팅 메시지, 아이템 구매, 캐릭터 정보 저장 등 절대 손실되어서는 안 되는 데이터를 전송할 때 TCP를 사용한다. 예를 들어, 플레이어가 게임 내 상점에서 유료 아이템을 구매했는데 그 정보를 담은 패킷이 손실된다면 심각한 문제가 발생할 것이다. TCP를 사용하면 이런 상황을 방지할 수 있다.

하지만 TCP의 신뢰성은 공짜가 아니다. 확인과 재전송 메커니즘 때문에 오버헤드가 크고, 패킷 하나가 손실되면 그 뒤의 모든 패킷이 대기하는 head-of-line blocking 현상이 발생한다. 또한 연결 수립과 유지에도 비용이 든다. 이러한 이유로 실시간성이 중요한 게임 데이터 전송에는 TCP가 적합하지 않다.

UDP는 비연결성 프로토콜로, 신뢰성을 보장하지 않는다. 패킷을 그냥 보내기만 할 뿐, 도착했는지 확인하지 않으며, 손실되어도 재전송하지 않는다. 순서도 보장하지 않아서 나중에 보낸 패킷이 먼저 도착할 수도 있다. 이렇게 들으면 형편없는 프로토콜 같지만, 실시간 게임에서는 UDP가 더 적합한 경우가 많다.

FPS 게임을 예로 들어보자. 플레이어의 위치 정보는 초당 수십 번씩 업데이트된다. 만약 0.1초 전의 위치 정보 패킷이 손실되었다면, 굳이 그것을 재전송받을 필요가 없다. 왜냐하면 그보다 더 최신의 위치 정보가 이미 전송되고 있기 때문이다. 오히려 재전송을 기다리느라 최신 데이터의 전달이 지연되는 것이 더 문제다. UDP는 이런 상황에서 낮은 지연 시간으로 최신 데이터를 빠르게 전달할 수 있다.

실제 게임 개발에서는 두 프로토콜을 적재적소에 활용한다. 실시간 게임플레이 데이터(위치, 회전, 애니메이션 상태 등)는 UDP로 전송하고, 중요한 이벤트나 트랜잭션 데이터는 TCP로 전송한다. 혹은 UDP 위에 필요한 부분만 신뢰성을 추가한 커스텀 프로토콜을 구현하기도 한다.

클라이언트 개발자는 UDP를 사용할 때 패킷 손실에 대비한 코드를 작성해야 한다. 예를 들어, 여러 프레임 동안 특정 플레이어의 위치 업데이트가 오지 않으면 보간법을 사용하여 부드럽게 움직이는 것처럼 보이게 하거나, 중요한 이벤트(플레이어 사망, 아이템 획득 등)는 UDP로 전송하더라도 애플리케이션 레벨에서 확인과 재전송 로직을 구현해야 한다.

### HTTP/HTTPS: 웹 기반 통신

HTTP(HyperText Transfer Protocol)는 웹에서 사용되는 프로토콜이지만, 게임 개발에서도 다양한 용도로 활용된다. HTTPS는 HTTP에 보안 계층(TLS/SSL)을 추가한 것으로, 데이터가 암호화되어 전송된다.

HTTP는 요청-응답(request-response) 모델을 따른다. 클라이언트가 서버에 요청을 보내면, 서버가 그에 대한 응답을 보내는 방식이다. 예를 들어, 게임 클라이언트가 서버에 "현재 랭킹 정보를 주세요"라고 요청하면, 서버가 랭킹 데이터를 담은 응답을 보낸다.

게임에서 HTTP/HTTPS는 주로 다음과 같은 용도로 사용된다. 첫째, 게임 초기화 데이터 로딩이다. 게임이 시작될 때 필요한 설정 파일, 공지사항, 이벤트 정보 등을 서버로부터 다운로드한다. 둘째, 비동기적인 데이터 전송이다. 플레이어의 통계 정보 업로드, 스크린샷 업로드, 버그 리포트 전송 등 실시간성이 중요하지 않은 데이터를 전송할 때 사용한다. 셋째, RESTful API를 통한 게임 서비스 연동이다. 친구 목록 조회, 아이템 상점 정보 가져오기, 업적 시스템 등 게임 외적인 서비스들과 통신할 때 HTTP API를 많이 사용한다.

HTTP의 장점은 사용이 간편하고, 방화벽이나 프록시를 쉽게 통과하며, 다양한 라이브러리와 도구가 존재한다는 점이다. 또한 캐싱, 압축 등의 기능을 쉽게 활용할 수 있다. 하지만 실시간 게임플레이에는 적합하지 않다. 각 요청마다 연결을 새로 수립하는 오버헤드가 있고(HTTP/1.1에서는 keep-alive로 어느 정도 완화), 요청-응답 구조상 서버에서 클라이언트로 먼저 데이터를 푸시할 수 없다.

HTTPS는 보안이 중요한 데이터를 전송할 때 필수적이다. 로그인 정보, 결제 정보, 개인 정보 등은 반드시 HTTPS를 통해 암호화하여 전송해야 한다. 암호화로 인한 성능 오버헤드가 있지만, 현대의 하드웨어에서는 무시할 수 있는 수준이며, 보안의 중요성을 고려하면 당연히 감수해야 할 비용이다.

클라이언트 개발자는 HTTP 요청을 비동기적으로 처리하는 방법을 알아야 한다. 게임의 메인 스레드를 블로킹하면서 HTTP 응답을 기다리면 게임이 멈춘 것처럼 보이므로, 별도 스레드나 비동기 콜백을 사용하여 처리한다. 또한 네트워크 오류나 타임아웃 상황을 적절히 처리하고, 필요하다면 재시도 로직을 구현해야 한다.

### WebSocket: 실시간 양방향 통신

WebSocket은 클라이언트와 서버 간에 양방향 실시간 통신을 가능하게 하는 프로토콜이다. HTTP로 초기 연결을 수립한 후, 프로토콜을 WebSocket으로 업그레이드하여 지속적인 연결을 유지한다. 연결이 수립되면 클라이언트와 서버 양쪽에서 언제든지 메시지를 보낼 수 있다.

WebSocket은 HTTP의 단점을 보완하면서도 웹 환경에서 작동한다는 장점이 있다. HTTP는 요청이 있어야만 응답할 수 있지만, WebSocket은 서버가 먼저 클라이언트에게 데이터를 푸시할 수 있다. 예를 들어, 다른 플레이어가 채팅 메시지를 보냈을 때 서버가 즉시 모든 클라이언트에게 그 메시지를 전송할 수 있다.

WebSocket은 특히 웹 브라우저 기반 게임에서 많이 사용된다. HTML5 게임이나 브라우저 기반 멀티플레이어 게임은 네이티브 소켓을 사용할 수 없으므로, WebSocket이 실시간 통신의 주요 수단이 된다. 또한 WebSocket은 표준 포트(80, 443)를 사용하므로 방화벽 문제가 적고, 프록시를 쉽게 통과한다.

WebSocket의 메시지는 텍스트(UTF-8 문자열)나 바이너리 데이터를 담을 수 있다. 게임 데이터를 JSON 형식으로 인코딩하여 텍스트 메시지로 보내거나, 더 효율적으로 바이너리 형식(예: Protocol Buffers)으로 인코딩하여 전송할 수 있다. 바이너리 형식이 데이터 크기를 줄이고 파싱 속도를 높일 수 있지만, 디버깅이 어렵다는 단점이 있다.

WebSocket을 사용할 때 주의할 점은 연결 관리다. 네트워크 상태가 불안정하거나 서버가 재시작될 때 연결이 끊어질 수 있으므로, 재연결 로직을 구현해야 한다. 또한 하트비트(heartbeat) 메커니즘을 통해 연결이 살아있는지 주기적으로 확인하는 것이 좋다. 일정 시간 동안 데이터가 오가지 않으면 ping/pong 메시지를 보내 연결 상태를 체크한다.

실시간 액션 게임보다는 턴 기반 게임, 카드 게임, 캐주얼 멀티플레이어 게임 등에서 WebSocket을 많이 사용한다. UDP만큼 지연 시간이 낮지는 않지만, TCP 기반이므로 신뢰성 있는 메시지 전달이 보장되며, 웹 환경에서의 호환성이 뛰어나다.

클라이언트 개발자는 WebSocket 라이브러리의 이벤트 핸들러를 구현하여 연결 수립, 메시지 수신, 연결 종료, 에러 발생 등의 상황을 처리한다. 메시지 포맷을 정의하고 파싱하는 코드를 작성하며, 게임 로직과 네트워크 레이어를 적절히 분리하여 유지보수가 쉬운 코드를 만들어야 한다.

## 1.3 필수 네트워크 용어

### IP 주소와 포트(Port)

IP 주소는 네트워크상에서 각 장치를 식별하는 고유한 번호다. 마치 집 주소처럼, 데이터 패킷이 목적지를 찾아가기 위해 필요한 정보다. 현재 널리 사용되는 IPv4는 32비트 주소 체계로, 0.0.0.0부터 255.255.255.255까지 약 43억 개의 주소를 제공한다. 예를 들어, 게임 서버의 IP 주소가 203.251.100.50이라면, 클라이언트는 이 주소로 연결을 시도한다.

IP 주소만으로는 부족하다. 한 컴퓨터에서 여러 개의 네트워크 애플리케이션이 동시에 실행될 수 있기 때문이다. 게임 서버, 웹 서버, 데이터베이스 서버가 모두 같은 컴퓨터에서 돌아가고 있다면, IP 주소만으로는 어느 서비스와 통신할지 구분할 수 없다. 이때 필요한 것이 포트 번호다.

포트는 0부터 65535까지의 숫자로, 특정 애플리케이션이나 서비스를 식별한다. 예를 들어, 웹 서버는 보통 80번 포트(HTTP) 또는 443번 포트(HTTPS)를 사용한다. 게임 서버는 개발자가 임의로 정한 포트를 사용하는데, 보통 10000번 이상의 포트를 사용한다. 만약 게임 서버가 203.251.100.50:7777에서 실행 중이라면, IP 주소 203.251.100.50의 7777번 포트로 연결해야 한다.

클라이언트 개발자가 알아야 할 중요한 개념은 포트 바인딩이다. 서버는 특정 포트를 "듣고(listen)" 있다가 클라이언트의 연결 요청을 받아들인다. 반면 클라이언트는 보통 운영체제가 자동으로 할당한 임시 포트(ephemeral port)를 사용하여 서버에 연결한다. 서버에 연결할 때는 서버의 IP와 포트를 명시하지만, 클라이언트 자신의 포트는 신경 쓸 필요가 없다.

IPv4 주소 고갈 문제를 해결하기 위해 IPv6가 도입되었다. IPv6는 128비트 주소 체계로 사실상 무한대의 주소를 제공한다. 게임 개발 시 IPv6를 고려하는 것이 좋지만, 아직은 IPv4가 주류이며, 많은 게임이 IPv4만 지원한다. 양쪽을 모두 지원하려면 듀얼 스택(dual stack) 구현이 필요하다.

실제 게임 개발에서는 IP 주소를 하드코딩하지 않고 도메인 네임(예: game.example.com)을 사용한다. DNS(Domain Name System)를 통해 도메인을 IP 주소로 변환한다. 이렇게 하면 서버 IP가 바뀌어도 클라이언트 코드를 수정할 필요가 없다.

### 지연시간(Latency)과 핑(Ping)

지연시간, 즉 레이턴시는 데이터가 출발지에서 목적지까지 도달하는 데 걸리는 시간이다. 게임에서는 주로 밀리초(ms) 단위로 측정한다. 레이턴시가 낮을수록 반응이 빠르고 쾌적한 게임 경험을 제공한다.

핑은 레이턴시를 측정하는 방법이다. 클라이언트가 서버에 작은 메시지를 보내고, 서버가 그것을 받아 다시 보내는 데 걸리는 왕복 시간(RTT, Round Trip Time)을 핑이라고 한다. 예를 들어, 핑이 50ms라면 클라이언트에서 서버로 데이터를 보내고 응답을 받는 데 50ms가 걸린다는 뜻이다. 단방향 지연시간은 대략 그 절반인 25ms 정도로 추정할 수 있다.

게임 장르에 따라 요구되는 레이턴시 수준이 다르다. FPS나 격투 게임처럼 빠른 반응이 필요한 게임은 50ms 이하의 낮은 핑이 중요하다. 100ms를 넘어가면 눈에 띄게 반응이 느려지고, 200ms 이상이면 쾌적한 플레이가 어렵다. 반면 턴 기반 게임이나 전략 게임은 수백 ms의 레이턴시도 큰 문제가 되지 않는다.

레이턴시는 여러 요인에 의해 결정된다. 물리적 거리가 가장 큰 요인이다. 빛의 속도는 유한하므로, 한국에서 미국 서버에 접속하면 물리적으로 최소 100ms 이상의 지연이 발생한다. 네트워크 혼잡도, 라우팅 경로, 서버의 처리 시간 등도 영향을 준다.

클라이언트 개발자는 레이턴시를 숨기기 위한 다양한 기법을 사용한다. 가장 일반적인 것이 클라이언트 측 예측이다. 플레이어가 앞으로 이동 명령을 내리면 서버의 응답을 기다리지 않고 즉시 화면상에서 캐릭터를 움직인다. 서버의 응답이 오면 실제 위치와 비교하여 차이가 있다면 보정한다. 이렇게 하면 플레이어는 즉각적인 반응을 느끼면서도 서버의 권위는 유지된다.

또한 게임 UI에 핑을 표시하는 것도 중요하다. 플레이어가 현재 네트워크 상태를 알 수 있게 하여, 반응이 느린 것이 게임 버그가 아니라 네트워크 문제임을 이해하도록 돕는다. 핑을 색상으로 표시하기도 하는데, 예를 들어 50ms 이하는 녹색, 50-100ms는 노란색, 100ms 이상은 빨간색으로 표시한다.

### 대역폭(Bandwidth)과 처리량(Throughput)

대역폭은 네트워크 연결이 이론적으로 전송할 수 있는 최대 데이터량을 의미한다. 보통 초당 비트 수(bps, bits per second)로 표현한다. 예를 들어, 100Mbps 인터넷 연결은 이론상 초당 100메가비트의 데이터를 전송할 수 있다.

처리량은 실제로 전송되는 데이터량이다. 대역폭이 고속도로의 차선 수라면, 처리량은 실제로 그 고속도로를 통과하는 차량의 수에 비유할 수 있다. 네트워크 혼잡, 프로토콜 오버헤드, 패킷 손실 등의 이유로 처리량은 항상 대역폭보다 낮다.

게임 개발에서 대역폭 관리는 중요한 최적화 포인트다. 특히 모바일 게임의 경우 플레이어의 데이터 요금제를 고려해야 하고, 많은 플레이어가 동시에 접속하는 게임은 서버의 네트워크 비용을 절감하기 위해 대역폭을 최적화해야 한다.

대역폭 사용량을 줄이는 방법은 여러 가지가 있다. 첫째, 전송하는 데이터의 양을 줄인다. 필요한 정보만 보내고, 데이터를 압축하며, 변경된 부분만 전송하는 델타 인코딩을 사용한다. 둘째, 업데이트 빈도를 조절한다. 모든 객체를 매 프레임마다 업데이트할 필요는 없다. 중요한 객체(플레이어 캐릭터)는 자주 업데이트하고, 멀리 있는 객체나 움직이지 않는 객체는 덜 자주 업데이트한다. 셋째, 관심 영역 관리(Area of Interest)를 사용한다. 플레이어 주변의 객체 정보만 전송하고, 시야 밖의 객체는 업데이트를 보내지 않는다.

클라이언트 개발자는 네트워크 사용량을 모니터링할 수 있는 도구를 구현하는 것이 좋다. 개발 중에는 디버그 UI에 초당 송수신 바이트 수를 표시하여 대역폭 사용량을 확인한다. 예상보다 많은 데이터가 전송되고 있다면, 프로파일링을 통해 어떤 데이터가 많은 대역폭을 차지하는지 분석하고 최적화한다.

대역폭과 레이턴시는 서로 다른 개념이다. 대역폭이 높다고 레이턴시가 낮은 것은 아니다. 위성 인터넷은 높은 대역폭을 제공할 수 있지만 레이턴시가 매우 높다. 반대로 레이턴시가 낮아도 대역폭이 부족하면 많은 데이터를 전송할 수 없다. 게임 개발에서는 둘 다 중요하며, 장르에 따라 우선순위가 다를 수 있다.

### 패킷 손실(Packet Loss)

패킷 손실은 전송된 패킷이 목적지에 도달하지 못하는 현상이다. 네트워크 혼잡, 하드웨어 오류, 무선 네트워크의 간섭 등 다양한 원인으로 발생한다. 패킷 손실률은 보통 퍼센트로 표시하는데, 1% 패킷 손실이라면 100개의 패킷 중 1개가 손실된다는 의미다.

TCP는 패킷 손실을 자동으로 처리한다. 손실된 패킷을 감지하면 재전송하므로, 애플리케이션 레벨에서는 패킷 손실을 신경 쓰지 않아도 된다. 하지만 재전송으로 인한 지연이 발생하며, 심한 패킷 손실 상황에서는 성능이 크게 저하된다.

반면 UDP는 패킷 손실을 감지하지도, 재전송하지도 않는다. 애플리케이션이 직접 처리해야 한다. 실시간 게임에서는 이것이 오히려 장점이 될 수 있다. 오래된 위치 정보 패킷이 손실되었다면 굳이 재전송받을 필요가 없기 때문이다.

클라이언트 개발자는 UDP를 사용할 때 패킷 손실에 강건한 코드를 작성해야 한다. 몇 가지 전략이 있다. 첫째, 중복 전송이다. 중요한 이벤트는 여러 번 전송하여 최소한 하나는 도착하도록 한다. 둘째, 확인 메커니즘이다. 중요한 메시지에는 시퀀스 번호를 붙이고, 수신자가 확인 메시지를 보내게 한다. 확인을 받지 못하면 재전송한다. 셋째, 상태 기반 업데이트다. 변경 사항만 보내는 대신 전체 상태를 주기적으로 보낸다. 일부 패킷이 손실되어도 다음 패킷으로 올바른 상태를 복구할 수 있다.

보간(interpolation)도 패킷 손실에 대응하는 좋은 방법이다. 플레이어의 위치 업데이트가 몇 프레임 동안 오지 않더라도, 이전 위치 데이터를 기반으로 현재 위치를 추정하여 부드럽게 움직이는 것처럼 보이게 한다.

패킷 손실률을 모니터링하는 것도 중요하다. 게임 클라이언트에서 전송한 패킷 수와 수신한 확인 응답 수를 추적하여 손실률을 계산할 수 있다. 손실률이 일정 수준을 넘어가면 플레이어에게 네트워크 상태가 불안정하다고 알려주거나, 게임플레이의 품질을 낮춰서라도 안정성을 확보하는 등의 조치를 취할 수 있다.

### 지터(Jitter)

지터는 패킷 지연 시간의 변동성을 의미한다. 핑이 일정하다면 문제가 없지만, 어떤 때는 50ms이고 어떤 때는 200ms라면 지터가 높은 것이다. 지터는 네트워크 경로의 혼잡도 변화, 라우팅 변경, 무선 네트워크의 불안정성 등으로 발생한다.

높은 지터는 게임 경험을 크게 해친다. 플레이어의 움직임이 때로는 부드럽다가 갑자기 끊기거나, 반응 속도가 일관되지 않아 게임플레이가 예측 불가능해진다. 특히 타이밍이 중요한 게임(리듬 게임, 격투 게임 등)에서는 치명적이다.

클라이언트 개발자는 지터 버퍼(jitter buffer)를 사용하여 지터의 영향을 완화할 수 있다. 지터 버퍼는 받은 패킷을 즉시 처리하지 않고 짧은 시간 동안 버퍼에 모아두었다가 순서대로 처리한다. 이렇게 하면 패킷 도착 시간의 불규칙성을 평준화할 수 있다. 물론 버퍼 시간만큼 추가 지연이 발생하므로, 버퍼 크기는 적절히 조절해야 한다.

또 다른 방법은 적응형 업데이트 레이트다. 네트워크 상태가 불안정할 때는 업데이트 빈도를 낮추고 더 많은 보간을 사용하여 부드러운 움직임을 유지한다. 반대로 네트워크가 안정적일 때는 업데이트 빈도를 높여 더 정확하고 반응성 좋은 경험을 제공한다.

지터를 측정하는 방법은 일정 시간 동안의 핑 값들의 표준편차를 계산하는 것이다. 예를 들어, 10개의 핑 샘플이 [50, 52, 51, 49, 50, 51, 50, 52, 49, 51]이라면 지터가 매우 낮다. 반면 [50, 150, 60, 200, 55, 180, 65, 190, 70, 150]이라면 지터가 매우 높은 것이다.

게임 UI에서 네트워크 품질을 표시할 때 핑뿐만 아니라 지터 정보도 함께 보여주면 플레이어가 현재 네트워크 상태를 더 잘 이해할 수 있다. 또한 서버 선택 시 평균 핑이 낮은 서버보다 지터가 낮은 서버를 선택하는 것이 더 나은 경험을 제공할 수 있다.

---

이상으로 네트워크 기초 지식을 살펴보았다. 클라이언트-서버와 P2P 모델의 차이, TCP와 UDP의 특성과 활용, HTTP/WebSocket 같은 웹 프로토콜의 역할을 이해했다. 또한 IP 주소와 포트, 레이턴시, 대역폭, 패킷 손실, 지터 같은 핵심 용어들의 의미와 게임 개발에서의 중요성을 배웠다. 이러한 기초 지식은 온라인 게임 클라이언트를 개발하는 모든 순간에 필요하며, 더 복잡한 네트워크 기법들을 이해하는 토대가 된다. 다음 장에서는 이러한 기초 위에서 게임 네트워크 아키텍처를 살펴본다.



# 2. 게임 네트워크 아키텍처

네트워크 기초 지식을 바탕으로 이제 게임에서 실제로 사용되는 네트워크 아키텍처를 살펴본다. 게임 네트워크 아키텍처는 클라이언트와 서버가 어떻게 데이터를 동기화하고, 어떤 구조로 통신하는지를 정의한다. 클라이언트 개발자는 서버가 어떤 방식으로 동작하는지 이해해야 효과적으로 클라이언트 코드를 작성할 수 있다. 이 장에서는 동기화 방식과 서버 토폴로지의 종류를 다루며, 각각의 장단점과 클라이언트 개발자가 고려해야 할 사항들을 설명한다.

## 2.1 동기화 방식의 이해

### 클라이언트-서버 동기화

클라이언트-서버 동기화는 모든 온라인 게임의 핵심이다. 여러 플레이어가 같은 게임 세계를 공유하려면 각 클라이언트가 보는 게임 상태가 일치해야 한다. 하지만 네트워크를 통해 데이터를 주고받는 데는 시간이 걸리고, 각 클라이언트는 서로 다른 네트워크 환경에 있으므로 완벽한 동기화는 불가능하다. 따라서 게임 네트워크 아키텍처는 "충분히 좋은" 동기화를 달성하는 것을 목표로 한다.

동기화의 기본 원리는 서버가 게임의 권위 있는 상태를 유지하고, 클라이언트는 그 상태의 근사치를 표시하는 것이다. 클라이언트는 플레이어의 입력을 서버에 전송하고, 서버는 모든 클라이언트로부터 받은 입력을 처리하여 게임 상태를 업데이트한 후, 변경된 상태를 다시 클라이언트들에게 전송한다.

간단한 예를 들어보자. 플레이어 A가 앞으로 이동하는 상황을 생각해본다.

```
// 클라이언트 A의 의사 코드
function OnPlayerPressForward():
    // 로컬에서 즉시 위치 업데이트 (예측)
    localPlayer.position += forward * speed * deltaTime
    
    // 서버에 입력 전송
    SendToServer({
        type: "PlayerInput",
        playerId: myId,
        input: "MoveForward",
        timestamp: currentTime
    })
```

서버는 이 입력을 받아 처리한다.

```
// 서버의 의사 코드
function OnReceivePlayerInput(packet):
    player = GetPlayer(packet.playerId)
    
    // 입력에 따라 서버 측 플레이어 위치 업데이트
    if packet.input == "MoveForward":
        player.position += forward * speed * deltaTime
    
    // 모든 클라이언트에게 업데이트된 위치 브로드캐스트
    BroadcastToAllClients({
        type: "PlayerUpdate",
        playerId: packet.playerId,
        position: player.position,
        timestamp: currentTime
    })
```

다른 클라이언트들은 이 업데이트를 받아 플레이어 A의 위치를 갱신한다.

```
// 클라이언트 B의 의사 코드
function OnReceivePlayerUpdate(packet):
    otherPlayer = GetPlayer(packet.playerId)
    otherPlayer.position = packet.position
```

실제로는 이보다 훨씬 복잡하다. 네트워크 지연 때문에 클라이언트가 보는 화면은 항상 과거의 상태이며, 각 클라이언트가 보는 시점이 다르다. 이를 해결하기 위해 클라이언트 측 예측, 서버 조정, 보간 등의 기법을 사용하는데, 이는 4장에서 자세히 다룬다.

클라이언트 개발자가 이해해야 할 핵심은 클라이언트가 보는 게임 상태는 근사치일 뿐이며, 서버의 상태가 진실이라는 점이다. 따라서 클라이언트는 항상 서버의 수정을 받아들일 준비가 되어 있어야 한다.

### 상태 동기화 vs 명령 동기화

게임 상태를 동기화하는 방법에는 크게 두 가지가 있다. 상태 동기화(State Synchronization)와 명령 동기화(Command Synchronization)다.

상태 동기화는 게임 객체의 현재 상태를 직접 전송하는 방식이다. 예를 들어, 캐릭터의 위치, 회전, 속도, 애니메이션 상태 등을 주기적으로 전송한다. 이 방식의 장점은 구현이 간단하고, 패킷 손실에 강하다는 것이다. 한 프레임의 업데이트가 손실되어도 다음 프레임의 업데이트로 올바른 상태를 복구할 수 있다.

```
// 상태 동기화 예제
function SendStateUpdate():
    for each entity in gameWorld:
        SendToClients({
            type: "StateUpdate",
            entityId: entity.id,
            position: entity.position,
            rotation: entity.rotation,
            velocity: entity.velocity,
            health: entity.health,
            animationState: entity.currentAnimation
        })
```

하지만 상태 동기화는 대역폭을 많이 사용한다. 특히 객체가 많은 게임에서는 모든 객체의 상태를 매번 전송하면 네트워크가 감당하기 어렵다. 따라서 최적화가 필요하다. 변경된 속성만 전송하거나, 중요한 객체만 자주 업데이트하고 덜 중요한 객체는 가끔 업데이트하는 방식을 사용한다.

명령 동기화는 상태 자체가 아니라 상태를 변경하는 명령이나 이벤트를 전송하는 방식이다. 예를 들어, "플레이어가 앞으로 이동 시작", "플레이어가 점프", "플레이어가 발사" 같은 명령을 전송한다.

```
// 명령 동기화 예제
function OnPlayerJump():
    // 로컬에서 점프 실행
    localPlayer.Jump()
    
    // 서버와 다른 클라이언트에 명령 전송
    SendToServer({
        type: "Command",
        command: "Jump",
        playerId: myId,
        timestamp: currentTime
    })
```

명령 동기화의 장점은 대역폭 효율이 좋다는 것이다. 특히 변화가 적은 게임에서 효과적이다. 턴 기반 게임이나 전략 게임에서 많이 사용된다. 플레이어가 행동할 때만 데이터를 전송하면 되므로 네트워크 사용량이 적다.

단점은 모든 클라이언트가 정확히 같은 방식으로 명령을 실행해야 한다는 것이다. 게임 로직이 결정론적(deterministic)이어야 하며, 조금이라도 차이가 나면 시간이 지남에 따라 상태가 점점 어긋난다. 또한 패킷 손실에 취약하다. 중요한 명령이 손실되면 게임 상태가 완전히 달라질 수 있으므로 신뢰성 있는 전송이 필요하다.

실제 게임에서는 두 방식을 혼합하여 사용한다. 지속적으로 변하는 데이터(위치, 회전)는 상태 동기화로, 이벤트성 데이터(아이템 획득, 스킬 사용)는 명령 동기화로 처리한다.

```
// 혼합 방식 예제
function Update():
    // 매 프레임 위치 상태 동기화
    if frameCount % 3 == 0:  // 3프레임마다
        SendStateUpdate(player.position, player.rotation)
    
    // 이벤트는 발생 시 즉시 전송
    if player.UsedSkill():
        SendCommand("UseSkill", skillId)
```

클라이언트 개발자는 어떤 데이터가 상태 동기화로, 어떤 데이터가 명령 동기화로 전송되는지 이해하고, 각각을 올바르게 처리하는 코드를 작성해야 한다.

### 권한 있는 서버(Authoritative Server) 개념

권한 있는 서버는 게임의 진정한 상태를 관리하고, 모든 중요한 결정을 내리는 서버를 의미한다. 이는 현대 온라인 게임에서 치팅을 방지하고 공정성을 유지하기 위한 핵심 원칙이다.

권한 있는 서버 모델에서 클라이언트는 절대 신뢰받지 못한다. "클라이언트는 모두 거짓말쟁이다"라는 가정 하에 시스템을 설계한다. 클라이언트가 "내가 적을 맞췄다", "내 체력은 100이다", "나는 벽을 통과할 수 있다"라고 주장하더라도, 서버는 이를 그대로 받아들이지 않고 검증한다.

예를 들어, FPS 게임에서 플레이어가 총을 발사하는 상황을 보자.

```
// 잘못된 방식: 클라이언트를 신뢰하는 경우
// 클라이언트
function OnFireWeapon():
    hitPlayer = RaycastHit()
    if hitPlayer != null:
        SendToServer({
            type: "PlayerHit",
            targetId: hitPlayer.id,
            damage: weaponDamage
        })

// 서버
function OnPlayerHit(packet):
    target = GetPlayer(packet.targetId)
    target.health -= packet.damage  // 위험! 클라이언트가 보낸 데미지를 그대로 적용
```

이 방식은 치팅에 매우 취약하다. 악의적인 플레이어가 클라이언트를 수정하여 엄청난 데미지를 보내거나, 실제로 맞히지 않았는데도 맞혔다고 보낼 수 있다.

올바른 방식은 서버가 모든 것을 검증하는 것이다.

```
// 올바른 방식: 서버가 권한을 가지는 경우
// 클라이언트
function OnFireWeapon():
    SendToServer({
        type: "FireWeapon",
        playerId: myId,
        aimDirection: cameraDirection,
        timestamp: currentTime
    })
    
    // 로컬에서 즉시 시각/청각 효과 표시 (예측)
    PlayFireAnimation()
    PlayFireSound()

// 서버
function OnFireWeapon(packet):
    shooter = GetPlayer(packet.playerId)
    
    // 서버 측에서 실제로 레이캐스트 수행
    hit = PerformRaycast(shooter.position, packet.aimDirection)
    
    if hit.isValid:
        target = hit.entity
        
        // 서버가 데미지 계산
        damage = CalculateDamage(shooter.weapon, hit.distance)
        
        // 유효성 검증
        if IsValidShot(shooter, target, hit.distance):
            target.health -= damage
            
            // 결과를 모든 클라이언트에 전송
            BroadcastToAllClients({
                type: "PlayerDamaged",
                shooterId: shooter.id,
                targetId: target.id,
                damage: damage,
                hitPosition: hit.position
            })
```

이 방식에서 서버는 클라이언트가 보낸 조준 방향을 받지만, 실제로 히트 판정과 데미지 계산은 서버에서 수행한다. 클라이언트가 치트를 사용하여 잘못된 데이터를 보내더라도, 서버의 검증을 통과할 수 없다.

권한의 수준은 게임 요소에 따라 다를 수 있다. 일반적으로 다음과 같이 분류한다.

**서버가 완전한 권한을 가져야 하는 요소:**
- 체력, 경험치, 레벨 등 플레이어 상태
- 아이템 획득 및 사용
- 전투 결과 (데미지, 치명타 등)
- 게임 규칙 검증 (쿨다운, 자원 소모 등)

**클라이언트가 예측하고 서버가 검증하는 요소:**
- 플레이어 이동
- 발사체 궤적
- 물리 시뮬레이션

**클라이언트에만 존재하는 요소:**
- 시각/청각 효과
- UI 애니메이션
- 카메라 움직임

클라이언트 개발자는 어떤 로직이 클라이언트에서만 실행되고, 어떤 것이 서버의 검증을 받아야 하는지 명확히 구분해야 한다. 중요한 게임 로직을 클라이언트에만 구현하면 치팅에 취약해지고, 모든 것을 서버에 의존하면 반응성이 떨어진다. 적절한 균형을 찾는 것이 핵심이다.

또한 서버로부터 수정 메시지를 받았을 때 이를 부드럽게 처리하는 것도 중요하다. 예를 들어, 클라이언트가 예측한 위치와 서버가 보낸 실제 위치가 다를 때, 갑자기 순간이동하는 것처럼 보이면 플레이어 경험이 나빠진다. 대신 짧은 시간 동안 부드럽게 보정하는 방식을 사용한다.

```
// 서버 조정 처리 예제
function OnServerPositionUpdate(serverPosition):
    currentPosition = player.position
    
    // 오차가 작으면 부드럽게 보정
    error = Distance(currentPosition, serverPosition)
    
    if error < smallThreshold:
        // 여러 프레임에 걸쳐 서서히 보정
        player.targetPosition = serverPosition
        player.interpolationSpeed = 0.1
    else if error < largeThreshold:
        // 오차가 중간 정도면 빠르게 보정
        player.targetPosition = serverPosition
        player.interpolationSpeed = 0.3
    else:
        // 오차가 너무 크면 즉시 이동 (순간이동)
        player.position = serverPosition
```

## 2.2 네트워크 토폴로지

### 전용 서버(Dedicated Server)

전용 서버는 게임 로직만 실행하는 독립적인 서버를 의미한다. 플레이어가 직접 조작하는 클라이언트가 아니라, 오직 게임 상태를 관리하고 클라이언트들 간의 통신을 중재하는 역할만 수행한다.

전용 서버 구조에서 게임이 시작되는 흐름은 다음과 같다.

```
// 게임 시작 흐름
1. 플레이어들이 매치메이킹 서버에 접속
2. 매치메이킹 서버가 플레이어들을 그룹화
3. 전용 게임 서버 인스턴스를 생성하거나 할당
4. 각 클라이언트가 할당된 게임 서버에 접속
5. 게임 시작
```

전용 서버의 가장 큰 장점은 공정성이다. 어떤 플레이어도 서버를 직접 조작할 수 없으므로, 호스트 우위(host advantage)가 존재하지 않는다. 모든 플레이어는 동등한 조건에서 게임을 플레이한다. 또한 서버의 성능이 플레이어의 컴퓨터 성능에 영향받지 않으므로, 일관된 게임 경험을 제공할 수 있다.

또 다른 장점은 안정성이다. 플레이어 중 한 명이 게임을 종료하거나 연결이 끊어져도 게임은 계속된다. 서버가 독립적으로 실행되고 있기 때문이다.

```
// 전용 서버에서 플레이어 연결 관리
function OnPlayerDisconnect(playerId):
    player = GetPlayer(playerId)
    
    // 짧은 시간 동안 재연결 대기
    player.disconnectedTime = currentTime
    player.state = "Disconnected"
    
    // 일정 시간 후에도 재연결하지 않으면 제거
    ScheduleTimeout(30 seconds, function():
        if player.state == "Disconnected":
            RemovePlayer(playerId)
            NotifyAllClients("PlayerLeft", playerId)
    )
```

전용 서버는 대규모 멀티플레이어 게임, 경쟁 게임, e-스포츠 등에 필수적이다. 하지만 단점도 있다. 가장 큰 문제는 비용이다. 서버 하드웨어를 구매하거나 클라우드 서비스를 임대해야 하며, 많은 플레이어가 동시에 접속하면 그만큼 많은 서버 인스턴스가 필요하다. 또한 서버를 유지보수하고 관리하는 인력도 필요하다.

클라이언트 개발자 입장에서 전용 서버 구조는 비교적 단순하다. 클라이언트는 서버에 연결하여 입력을 보내고, 서버로부터 게임 상태 업데이트를 받는다. 서버의 내부 동작을 신경 쓸 필요가 없다.

```
// 전용 서버 연결 의사 코드
function ConnectToGameServer(serverAddress, serverPort):
    connection = CreateConnection(serverAddress, serverPort)
    
    if connection.Connect():
        // 인증 정보 전송
        SendAuthenticationData(connection, playerToken)
        
        // 서버 응답 대기
        response = WaitForResponse(connection, timeout=5 seconds)
        
        if response.success:
            gameState = "Connected"
            StartReceivingUpdates(connection)
        else:
            ShowError("Failed to join game: " + response.errorMessage)
    else:
        ShowError("Could not connect to server")
```

### 리슨 서버(Listen Server)

리슨 서버는 플레이어 중 한 명이 서버 역할을 겸하는 구조다. 호스트 플레이어의 게임 클라이언트가 서버 기능도 함께 수행한다. 다른 플레이어들은 이 호스트에 접속하여 게임을 플레이한다.

리슨 서버의 구조를 보면 호스트는 두 가지 역할을 동시에 수행한다.

```
// 리슨 서버 구조
Host Client:
    - 서버 로직 실행 (게임 상태 관리, 다른 클라이언트와의 통신)
    - 클라이언트 로직 실행 (렌더링, 입력 처리, 로컬 플레이어 제어)
    
Other Clients:
    - 클라이언트 로직만 실행
    - 호스트 서버에 연결
```

리슨 서버의 가장 큰 장점은 비용이 들지 않는다는 것이다. 별도의 서버 인프라가 필요 없으며, 플레이어들끼리 모여서 바로 게임을 시작할 수 있다. 소규모 친구들과의 게임, 협동 게임, LAN 파티 등에 적합하다.

또한 호스트의 레이턴시가 0이라는 장점이 있다. 호스트 플레이어는 서버와 같은 컴퓨터에 있으므로 네트워크 지연 없이 즉각적인 반응을 얻을 수 있다.

```
// 리슨 서버에서 호스트의 입력 처리
function OnLocalPlayerInput(input):
    if IsHost:
        // 호스트는 즉시 서버 로직 호출 (네트워크 지연 없음)
        ProcessInputOnServer(input)
        
        // 다른 클라이언트들에게 결과 브로드캐스트
        BroadcastToClients(gameStateUpdate)
    else:
        // 일반 클라이언트는 서버에 전송
        SendToServer(input)
```

하지만 리슨 서버에는 심각한 단점들이 있다. 첫째, 호스트 우위가 존재한다. 호스트는 레이턴시가 0이므로 다른 플레이어들보다 유리하다. 경쟁 게임에서는 이것이 공정성 문제를 일으킨다.

둘째, 호스트의 컴퓨터 성능에 게임이 의존한다. 호스트의 컴퓨터가 느리면 서버 로직도 느려져서 모든 플레이어의 경험이 나빠진다. 또한 호스트가 그래픽 설정을 높게 하면 프레임 드롭이 발생하여 서버 업데이트 레이트도 떨어진다.

```
// 리슨 서버의 성능 문제 예시
function Update():
    // 호스트는 클라이언트와 서버 로직을 모두 실행
    UpdateServerLogic()        // CPU 사용
    UpdateClientLogic()        // CPU 사용
    RenderGame()               // GPU 사용
    
    // 만약 렌더링이 느려서 프레임이 30fps로 떨어지면
    // 서버 업데이트도 30Hz로 떨어짐
    // 다른 클라이언트들도 영향을 받음
```

셋째, 호스트가 게임을 종료하면 게임이 끝난다. 호스트가 나가면 서버가 사라지므로 모든 플레이어가 게임을 계속할 수 없다. 이를 해결하기 위해 호스트 마이그레이션(host migration)이라는 기법을 사용하기도 하는데, 구현이 복잡하다.

```
// 호스트 마이그레이션 의사 코드
function OnHostDisconnect():
    // 새로운 호스트 선출 (보통 핑이 가장 낮은 플레이어)
    newHost = SelectNewHost(connectedPlayers)
    
    // 현재 게임 상태를 새 호스트에게 전송
    TransferGameState(newHost, currentGameState)
    
    // 모든 클라이언트를 새 호스트에 재연결
    for each client in connectedPlayers:
        if client != newHost:
            client.ReconnectToNewHost(newHost.address)
    
    // 새 호스트가 서버 역할 시작
    newHost.BecomeServer()
```

클라이언트 개발자는 리슨 서버 구조에서 호스트 클라이언트와 일반 클라이언트의 코드 경로가 다르다는 점을 이해해야 한다. 호스트는 서버 로직도 실행하므로, 일부 코드는 호스트일 때만 실행되고, 일부는 클라이언트일 때만 실행된다.

```
// 리슨 서버 코드 구조 예제
function InitializeGame():
    if IsHost:
        InitializeServerComponents()
        StartListeningForConnections()
    
    InitializeClientComponents()
    
    if IsHost:
        // 호스트는 자기 자신에게 로컬 연결
        ConnectToLocalServer()
    else:
        // 일반 클라이언트는 원격 호스트에 연결
        ConnectToRemoteServer(hostAddress)
```

### 클라우드 기반 서버

클라우드 기반 서버는 전용 서버의 일종이지만, 온프레미스(자체 데이터센터) 서버 대신 클라우드 서비스를 활용한다. AWS, Google Cloud, Azure 같은 클라우드 플랫폼에서 제공하는 컴퓨팅 자원을 사용하여 게임 서버를 실행한다.

클라우드 서버의 가장 큰 장점은 확장성(scalability)이다. 플레이어 수가 급증하면 자동으로 서버 인스턴스를 추가하고, 플레이어 수가 줄면 서버를 줄여서 비용을 절감할 수 있다.

```
// 클라우드 오토스케일링 개념
function MonitorServerLoad():
    currentPlayers = GetTotalPlayerCount()
    availableServers = GetActiveServerCount()
    
    averagePlayersPerServer = currentPlayers / availableServers
    
    if averagePlayersPerServer > 40:  // 서버당 평균 40명 초과
        // 서버 추가
        SpinUpNewServerInstance()
    else if averagePlayersPerServer < 10 and availableServers > minServers:
        // 여유 서버 제거
        ShutdownIdleServer()
```

또 다른 장점은 지리적 분산이다. 전 세계 여러 지역에 서버를 배치하여 각 플레이어가 가까운 서버에 접속하도록 할 수 있다. 이를 통해 레이턴시를 최소화한다.

```
// 지역 기반 서버 선택
function SelectBestServer(playerLocation):
    availableRegions = ["us-east", "us-west", "eu-west", "asia-northeast"]
    bestServer = null
    lowestPing = infinity
    
    for each region in availableRegions:
        ping = MeasurePing(region)
        if ping < lowestPing:
            lowestPing = ping
            bestServer = region
    
    return bestServer
```

클라우드 기반 서버는 또한 서버 관리를 단순화한다. 하드웨어 고장, 네트워크 문제 등을 클라우드 제공자가 처리한다. 또한 컨테이너 기술(Docker, Kubernetes)을 활용하여 서버 배포와 업데이트를 자동화할 수 있다.

단점은 여전히 비용이다. 특히 트래픽(네트워크 데이터 전송량)에 따라 비용이 증가하므로, 플레이어가 많거나 대역폭 사용량이 높은 게임은 비용이 매우 높아질 수 있다. 또한 클라우드 서비스에 종속(vendor lock-in)될 수 있으며, 서비스 장애 시 게임이 중단된다.

클라이언트 개발자 관점에서 클라우드 서버는 일반 전용 서버와 크게 다르지 않다. 다만 매치메이킹 과정에서 여러 지역 중 최적의 서버를 선택하는 로직이 추가될 수 있다.

```
// 클라우드 매치메이킹 흐름
function JoinMatchmaking():
    // 1. 매치메이킹 서버에 요청
    matchmakingRequest = {
        playerId: myId,
        gameMode: selectedMode,
        skillRating: myRating,
        preferredRegion: DetectPlayerRegion()
    }
    
    SendToMatchmakingServer(matchmakingRequest)
    
    // 2. 매치메이킹 완료 대기
    OnMatchFound(matchInfo):
        gameServerAddress = matchInfo.serverAddress
        gameServerRegion = matchInfo.region
        
        // 3. 할당된 게임 서버에 연결
        ConnectToGameServer(gameServerAddress)
```

또한 클라우드 서버는 동적으로 생성되고 제거되므로, 서버 리스트가 계속 변한다. 클라이언트는 중앙 매치메이킹 서비스나 로비 시스템을 통해 서버 정보를 받아야 한다.

최근에는 서버리스(serverless) 아키텍처도 등장했다. AWS Lambda, Google Cloud Functions 같은 서비스를 사용하여 게임 로직의 일부를 실행한다. 실시간 게임플레이보다는 비동기적인 작업(리더보드 업데이트, 아이템 상점, 소셜 기능 등)에 적합하다.

```
// 서버리스 함수 호출 예제
function UpdateLeaderboard(score):
    // 클라우드 함수 호출
    response = CallCloudFunction("updateLeaderboard", {
        playerId: myId,
        score: score,
        timestamp: currentTime
    })
    
    if response.success:
        ShowMessage("Leaderboard updated!")
        newRank = response.rank
        UpdateUI(newRank)
```

---

이 장에서는 게임 네트워크 아키텍처의 핵심 개념들을 살펴보았다. 동기화 방식에서는 상태 동기화와 명령 동기화의 차이, 그리고 서버 권한의 중요성을 배웠다. 네트워크 토폴로지에서는 전용 서버, 리슨 서버, 클라우드 서버의 특징과 장단점을 이해했다. 클라이언트 개발자는 자신이 개발하는 게임이 어떤 아키텍처를 사용하는지 이해하고, 그에 맞는 클라이언트 코드를 작성해야 한다. 서버의 권한을 존중하면서도 플레이어에게 즉각적인 반응을 제공하는 것, 이것이 온라인 게임 클라이언트 개발의 핵심 과제다. 다음 장에서는 이러한 아키텍처 위에서 실제로 클라이언트 개발자가 작성하는 네트워크 코드를 구체적으로 살펴본다.


# 3. 클라이언트 개발자가 다루는 네트워크 코드

클라이언트 개발자는 게임 플레이 로직뿐만 아니라 서버와의 통신을 담당하는 네트워크 코드를 작성하고 관리해야 한다. 이 장에서는 클라이언트에서 실제로 구현해야 하는 네트워크 코드의 핵심 요소들을 다룬다.

## 3.1 연결 관리

### 서버 연결 및 재연결 로직

게임 클라이언트는 서버와의 연결을 수립하고, 연결이 끊어졌을 때 자동으로 재연결하는 로직을 구현해야 한다. 안정적인 연결 관리는 사용자 경험에 직접적인 영향을 미친다.

**기본 연결 흐름:**

```pseudocode
class NetworkManager:
    state = DISCONNECTED
    connection = null
    reconnectAttempts = 0
    maxReconnectAttempts = 5
    
    function Connect(serverAddress, port):
        state = CONNECTING
        try:
            connection = CreateConnection(serverAddress, port)
            connection.OnConnected = HandleConnected
            connection.OnDisconnected = HandleDisconnected
            connection.Connect()
        catch error:
            state = DISCONNECTED
            HandleConnectionError(error)
    
    function HandleConnected():
        state = CONNECTED
        reconnectAttempts = 0
        SendLoginPacket()
        StartHeartbeat()
    
    function HandleDisconnected(reason):
        state = DISCONNECTED
        StopHeartbeat()
        
        if reason == USER_REQUESTED:
            return
            
        if reconnectAttempts < maxReconnectAttempts:
            reconnectAttempts++
            delay = CalculateBackoffDelay(reconnectAttempts)
            ScheduleReconnect(delay)
        else:
            ShowConnectionFailedDialog()
```

재연결 시 지수 백오프(Exponential Backoff) 전략을 사용하면 서버 부하를 줄이면서 안정적인 재연결을 시도할 수 있다. 첫 번째 재연결은 1초 후, 두 번째는 2초 후, 세 번째는 4초 후와 같이 점진적으로 대기 시간을 늘린다.

### 연결 상태 모니터링

클라이언트는 서버와의 연결 상태를 지속적으로 모니터링해야 한다. 하트비트(Heartbeat) 또는 핑(Ping) 메커니즘을 통해 연결이 살아있는지 확인한다.

**하트비트 구현:**

```pseudocode
class HeartbeatSystem:
    heartbeatInterval = 5.0  // 5초마다 전송
    timeoutDuration = 15.0   // 15초 동안 응답 없으면 타임아웃
    lastHeartbeatSent = 0
    lastHeartbeatReceived = 0
    
    function Update(currentTime):
        // 하트비트 전송
        if currentTime - lastHeartbeatSent > heartbeatInterval:
            SendHeartbeat()
            lastHeartbeatSent = currentTime
        
        // 타임아웃 체크
        if currentTime - lastHeartbeatReceived > timeoutDuration:
            HandleTimeout()
    
    function SendHeartbeat():
        packet = CreatePacket(HEARTBEAT)
        packet.timestamp = GetCurrentTime()
        SendToServer(packet)
    
    function OnHeartbeatResponse(packet):
        lastHeartbeatReceived = GetCurrentTime()
        rtt = lastHeartbeatReceived - packet.timestamp
        UpdatePingStatistics(rtt)
```

하트비트를 통해 얻은 RTT(Round Trip Time) 정보는 네트워크 품질 지표로 활용되며, UI에 핑 수치를 표시하거나 네트워크 상태에 따른 동작을 조정하는 데 사용된다.

### 타임아웃 처리

네트워크 연결에서는 다양한 이유로 응답이 지연되거나 오지 않을 수 있다. 각 요청에 대해 적절한 타임아웃을 설정하고 처리하는 것이 중요하다.

**타임아웃 관리:**

```pseudocode
class RequestManager:
    pendingRequests = Map<RequestID, PendingRequest>
    nextRequestID = 1
    
    function SendRequest(requestType, data, timeoutSeconds):
        requestID = nextRequestID++
        
        pendingRequest = new PendingRequest()
        pendingRequest.id = requestID
        pendingRequest.type = requestType
        pendingRequest.sentTime = GetCurrentTime()
        pendingRequest.timeoutTime = GetCurrentTime() + timeoutSeconds
        
        pendingRequests[requestID] = pendingRequest
        
        packet = CreatePacket(requestType, requestID, data)
        SendToServer(packet)
        
        return requestID
    
    function Update(currentTime):
        foreach request in pendingRequests:
            if currentTime > request.timeoutTime:
                HandleRequestTimeout(request)
                pendingRequests.Remove(request.id)
    
    function OnResponse(packet):
        if pendingRequests.Contains(packet.requestID):
            request = pendingRequests[packet.requestID]
            HandleResponse(request, packet)
            pendingRequests.Remove(packet.requestID)
```

타임아웃은 요청의 중요도와 네트워크 상황에 따라 다르게 설정한다. 로그인 요청은 10초, 일반 게임 액션은 3초, 하트비트는 5초 등으로 구분하여 관리한다.

## 3.2 데이터 송수신

### 직렬화(Serialization)와 역직렬화

네트워크를 통해 데이터를 전송하려면 메모리 상의 객체를 바이트 스트림으로 변환하는 직렬화 과정이 필요하다. 반대로 받은 데이터를 객체로 복원하는 것이 역직렬화다.

**기본 직렬화 예제:**

```pseudocode
class PlayerPosition:
    x: float
    y: float
    z: float
    rotation: float
    timestamp: long
    
    function Serialize():
        buffer = ByteBuffer()
        buffer.WriteFloat(x)
        buffer.WriteFloat(y)
        buffer.WriteFloat(z)
        buffer.WriteFloat(rotation)
        buffer.WriteLong(timestamp)
        return buffer.GetBytes()
    
    function Deserialize(bytes):
        buffer = ByteBuffer(bytes)
        x = buffer.ReadFloat()
        y = buffer.ReadFloat()
        z = buffer.ReadFloat()
        rotation = buffer.ReadFloat()
        timestamp = buffer.ReadLong()
```

직렬화 시 바이트 순서(Endianness)를 일관되게 유지해야 한다. 일반적으로 네트워크 바이트 순서인 빅 엔디안을 사용하거나, 프로젝트 전체에서 통일된 규칙을 적용한다.

**최적화된 직렬화:**

크기를 줄이기 위해 데이터 타입을 효율적으로 선택하고, 불필요한 정밀도를 제거할 수 있다.

```pseudocode
// 위치를 16비트 정수로 압축 (범위: 0~65535)
function CompressPosition(worldPos, minBound, maxBound):
    normalized = (worldPos - minBound) / (maxBound - minBound)
    compressed = ClampInt(normalized * 65535, 0, 65535)
    return compressed

// 각도를 8비트로 압축 (0~360도를 0~255로)
function CompressRotation(angleDegrees):
    normalized = angleDegrees / 360.0
    return ClampInt(normalized * 255, 0, 255)
```

### 메시지 큐(Message Queue) 관리

네트워크에서 받은 패킷들을 즉시 처리하지 않고 큐에 저장했다가 게임 루프의 적절한 시점에 처리하는 것이 안전하다. 이는 스레드 안전성과 처리 순서 보장을 위해 중요하다.

**메시지 큐 구현:**

```pseudocode
class NetworkMessageQueue:
    receiveQueue = Queue<NetworkMessage>
    sendQueue = Queue<NetworkMessage>
    queueLock = Mutex()
    
    // 네트워크 스레드에서 호출
    function EnqueueReceivedMessage(message):
        queueLock.Lock()
        receiveQueue.Enqueue(message)
        queueLock.Unlock()
    
    // 게임 스레드에서 호출
    function ProcessReceivedMessages():
        queueLock.Lock()
        messages = receiveQueue.DequeueAll()
        queueLock.Unlock()
        
        foreach message in messages:
            ProcessMessage(message)
    
    function ProcessMessage(message):
        switch message.type:
            case PLAYER_MOVE:
                HandlePlayerMove(message.data)
            case SPAWN_OBJECT:
                HandleSpawnObject(message.data)
            case DAMAGE:
                HandleDamage(message.data)
```

메시지 큐는 수신된 순서대로 처리되어야 하므로 FIFO(First In First Out) 구조를 사용한다. 다만 우선순위가 높은 메시지(예: 연결 종료, 킥 메시지)는 별도의 우선순위 큐로 관리할 수 있다.

### 프로토콜 버퍼, JSON 등 데이터 포맷

데이터를 어떤 형식으로 주고받을지 결정하는 것은 성능과 개발 편의성에 큰 영향을 미친다.

**JSON 방식:**

```pseudocode
// 장점: 사람이 읽기 쉬움, 디버깅 용이, 유연함
// 단점: 크기가 크고 파싱 비용이 높음

playerData = {
    "id": 12345,
    "name": "Player1",
    "position": {
        "x": 100.5,
        "y": 200.3,
        "z": 50.0
    },
    "health": 80
}

jsonString = JSON.Stringify(playerData)
SendToServer(jsonString)
```

**바이너리 직렬화 (Protocol Buffers 스타일):**

```pseudocode
// 장점: 크기가 작고 파싱이 빠름, 타입 안정성
// 단점: 사람이 읽기 어려움, 스키마 정의 필요

message PlayerData:
    required int32 id = 1
    required string name = 2
    required Vector3 position = 3
    required int32 health = 4

player = new PlayerData()
player.id = 12345
player.name = "Player1"
player.position = Vector3(100.5, 200.3, 50.0)
player.health = 80

bytes = player.SerializeToBytes()
SendToServer(bytes)
```

실시간 게임에서는 일반적으로 바이너리 포맷을 사용하여 패킷 크기를 최소화하고 처리 속도를 높인다. 반면 로비 시스템이나 비실시간 통신에서는 JSON을 사용하여 개발과 디버깅의 편의성을 높일 수 있다.

## 3.3 네트워크 이벤트 처리

### 비동기 통신 처리

네트워크 통신은 본질적으로 비동기적이다. 요청을 보낸 후 응답이 언제 올지 알 수 없으므로, 블로킹하지 않고 비동기적으로 처리해야 한다.

**콜백 기반 비동기 처리:**

```pseudocode
class NetworkClient:
    function RequestPlayerData(playerID, onSuccess, onFailure):
        request = CreateRequest(GET_PLAYER_DATA)
        request.playerID = playerID
        
        requestID = SendRequest(request)
        
        RegisterCallback(requestID, function(response):
            if response.success:
                playerData = ParsePlayerData(response.data)
                onSuccess(playerData)
            else:
                onFailure(response.errorCode)
        )
    
    // 사용 예
    function LoadPlayerProfile(playerID):
        RequestPlayerData(playerID,
            onSuccess: function(data):
                DisplayPlayerProfile(data)
            ,
            onFailure: function(errorCode):
                ShowErrorMessage("플레이어 정보를 불러올 수 없습니다")
        )
```

**Promise/Future 패턴:**

```pseudocode
class NetworkClient:
    function RequestPlayerDataAsync(playerID):
        promise = new Promise()
        
        request = CreateRequest(GET_PLAYER_DATA)
        request.playerID = playerID
        
        requestID = SendRequest(request)
        
        RegisterCallback(requestID, function(response):
            if response.success:
                promise.Resolve(ParsePlayerData(response.data))
            else:
                promise.Reject(response.errorCode)
        )
        
        return promise
    
    // 사용 예
    async function LoadPlayerProfile(playerID):
        try:
            playerData = await RequestPlayerDataAsync(playerID)
            DisplayPlayerProfile(playerData)
        catch error:
            ShowErrorMessage("플레이어 정보를 불러올 수 없습니다")
```

### 콜백과 이벤트 리스너

네트워크 이벤트를 처리하는 방식은 크게 콜백 방식과 이벤트 리스너 방식으로 나뉜다.

**이벤트 리스너 패턴:**

```pseudocode
class NetworkEventSystem:
    listeners = Map<EventType, List<Callback>>
    
    function RegisterListener(eventType, callback):
        if not listeners.Contains(eventType):
            listeners[eventType] = new List<Callback>()
        listeners[eventType].Add(callback)
    
    function UnregisterListener(eventType, callback):
        if listeners.Contains(eventType):
            listeners[eventType].Remove(callback)
    
    function TriggerEvent(eventType, data):
        if listeners.Contains(eventType):
            foreach callback in listeners[eventType]:
                callback(data)

// 사용 예
networkEvents = new NetworkEventSystem()

// 여러 곳에서 동일한 이벤트를 구독
networkEvents.RegisterListener(PLAYER_JOINED, function(data):
    UpdatePlayerList(data.playerID)
)

networkEvents.RegisterListener(PLAYER_JOINED, function(data):
    ShowNotification(data.playerName + "님이 입장했습니다")
)

networkEvents.RegisterListener(PLAYER_JOINED, function(data):
    PlaySound("player_join.wav")
)
```

이벤트 리스너 방식은 여러 시스템이 동일한 네트워크 이벤트에 관심이 있을 때 유용하다. 각 시스템이 독립적으로 이벤트를 구독하고 처리할 수 있어 코드의 결합도가 낮아진다.

### 스레드 안전성(Thread Safety)

네트워크 통신은 보통 별도의 스레드에서 처리되므로, 게임 로직을 실행하는 메인 스레드와 데이터를 공유할 때 스레드 안전성을 보장해야 한다.

**스레드 안전한 데이터 접근:**

```pseudocode
class NetworkState:
    playerPositions = Map<PlayerID, Vector3>
    stateLock = Mutex()
    
    // 네트워크 스레드에서 호출
    function UpdatePlayerPosition(playerID, position):
        stateLock.Lock()
        playerPositions[playerID] = position
        stateLock.Unlock()
    
    // 게임 스레드에서 호출
    function GetPlayerPosition(playerID):
        stateLock.Lock()
        position = playerPositions[playerID]
        stateLock.Unlock()
        return position
    
    // 게임 스레드에서 호출 - 모든 위치를 복사
    function GetAllPlayerPositions():
        stateLock.Lock()
        snapshot = playerPositions.Copy()
        stateLock.Unlock()
        return snapshot
```

**Lock-Free 큐를 활용한 패턴:**

뮤텍스를 사용한 락은 성능 저하를 일으킬 수 있다. Lock-Free 자료구조를 사용하면 더 나은 성능을 얻을 수 있다.

```pseudocode
class ThreadSafeQueue:
    // 내부적으로 lock-free 구조 사용
    internalQueue = ConcurrentQueue<Message>
    
    function Enqueue(message):
        internalQueue.Enqueue(message)  // 락 없이 안전하게 추가
    
    function TryDequeue():
        return internalQueue.TryDequeue()  // 락 없이 안전하게 제거

// 사용 예
receivedMessages = new ThreadSafeQueue()

// 네트워크 스레드
function OnNetworkDataReceived(data):
    message = ParseMessage(data)
    receivedMessages.Enqueue(message)

// 게임 스레드
function UpdateNetwork():
    while message = receivedMessages.TryDequeue():
        ProcessMessage(message)
```

스레드 안전성을 위한 또 다른 방법은 모든 네트워크 처리를 메인 스레드에서 하되, 논블로킹 소켓을 사용하는 것이다. 이 방식은 멀티스레딩의 복잡성을 피할 수 있지만, 네트워크 처리가 게임 프레임 레이트에 영향을 줄 수 있다.

**중요한 원칙들:**

1. **명확한 소유권**: 데이터가 어느 스레드에 속하는지 명확히 한다
2. **최소 공유**: 스레드 간 공유하는 데이터를 최소화한다
3. **불변 데이터**: 가능한 경우 불변 객체를 사용하여 공유한다
4. **명시적 동기화**: 동기화가 필요한 부분을 명확히 표시하고 문서화한다

클라이언트 개발자는 이러한 네트워크 코드의 기본 요소들을 이해하고 구현할 수 있어야 한다. 연결 관리, 데이터 송수신, 이벤트 처리는 모든 온라인 게임 클라이언트의 기반이 되며, 이를 견고하게 구현하는 것이 안정적인 멀티플레이 경험을 제공하는 첫걸음이다.


# 4. 온라인 게임의 주요 이슈와 해결 방법

온라인 게임 개발에서 네트워크는 가장 도전적인 부분 중 하나다. 완벽한 네트워크 환경은 존재하지 않으며, 지연, 패킷 손실, 대역폭 제한 등의 문제는 항상 발생한다. 클라이언트 개발자는 이러한 문제들을 이해하고, 플레이어가 부드러운 게임 경험을 느낄 수 있도록 적절한 해결책을 구현해야 한다.

## 4.1 지연 시간(Latency) 문제

네트워크 지연은 온라인 게임에서 피할 수 없는 현상이다. 플레이어가 입력을 보내고 서버의 응답을 받기까지 시간이 걸리며, 이는 게임의 반응성에 직접적인 영향을 미친다. 클라이언트 개발자는 이 지연을 숨기거나 최소화하는 기술들을 구현해야 한다.

### 클라이언트 측 예측(Client-side Prediction)

클라이언트 측 예측은 서버의 응답을 기다리지 않고 플레이어의 입력에 즉시 반응하는 기법이다. 플레이어가 이동 키를 누르면 서버 응답을 기다리지 않고 즉시 캐릭터를 움직이며, 서버로부터 실제 결과가 오면 필요시 보정한다.

**기본 예측 구조:**

```pseudocode
class PlayerController:
    localPlayer = null
    pendingInputs = List<InputCommand>
    lastProcessedInputID = 0
    
    function Update(deltaTime):
        // 입력 수집
        input = GatherInput()
        
        if input.HasAction():
            // 입력에 고유 ID 부여
            input.id = GenerateInputID()
            input.timestamp = GetCurrentTime()
            
            // 즉시 로컬에서 예측 실행
            ApplyInputLocally(input)
            
            // 서버로 전송
            SendInputToServer(input)
            
            // 서버 응답 대기를 위해 보관
            pendingInputs.Add(input)
    
    function ApplyInputLocally(input):
        if input.moveForward:
            localPlayer.position += localPlayer.forward * moveSpeed * deltaTime
        if input.moveRight:
            localPlayer.position += localPlayer.right * moveSpeed * deltaTime
        if input.jump and localPlayer.isGrounded:
            localPlayer.velocity.y = jumpForce
    
    function OnServerStateUpdate(serverState):
        // 서버가 확인한 마지막 입력 ID
        lastProcessedInputID = serverState.lastProcessedInputID
        
        // 처리된 입력들 제거
        pendingInputs.RemoveWhere(input => input.id <= lastProcessedInputID)
        
        // 서버 위치로 초기화
        localPlayer.position = serverState.position
        localPlayer.velocity = serverState.velocity
        
        // 아직 서버에서 처리되지 않은 입력들을 다시 적용 (재예측)
        foreach input in pendingInputs:
            ApplyInputLocally(input)
```

이 방식의 핵심은 서버의 권한을 인정하면서도 로컬에서 즉각적인 피드백을 제공하는 것이다. 대부분의 경우 예측이 정확하지만, 서버에서 다른 결과가 오면(예: 벽에 막힘) 위치를 보정한다.

**예측 오류 보정:**

예측이 틀렸을 때 갑자기 위치가 변하면 플레이어가 불편함을 느낀다. 부드럽게 보정하는 방법이 필요하다.

```pseudocode
class PredictionCorrection:
    correctionThreshold = 0.5  // 0.5 유닛 이상 차이나면 보정
    correctionSpeed = 10.0     // 초당 보정 속도
    
    function OnServerStateUpdate(serverState):
        predictedPosition = localPlayer.position
        serverPosition = serverState.position
        
        positionError = Distance(predictedPosition, serverPosition)
        
        if positionError > correctionThreshold:
            if positionError > 5.0:
                // 오차가 너무 크면 즉시 보정 (순간이동)
                localPlayer.position = serverPosition
            else:
                // 작은 오차는 부드럽게 보정
                localPlayer.correctionTarget = serverPosition
                localPlayer.isCorrectingPosition = true
        
        // 재예측 로직...
    
    function Update(deltaTime):
        if localPlayer.isCorrectingPosition:
            direction = localPlayer.correctionTarget - localPlayer.position
            distance = Length(direction)
            
            if distance < 0.01:
                localPlayer.position = localPlayer.correctionTarget
                localPlayer.isCorrectingPosition = false
            else:
                moveAmount = correctionSpeed * deltaTime
                localPlayer.position += Normalize(direction) * Min(moveAmount, distance)
```

### 보간(Interpolation)과 외삽(Extrapolation)

다른 플레이어의 움직임을 표현할 때는 예측 대신 보간을 사용한다. 서버로부터 받은 위치 업데이트 사이를 부드럽게 연결하여 자연스러운 움직임을 만든다.

**보간 구현:**

```pseudocode
class RemotePlayer:
    positionBuffer = CircularBuffer<PositionSnapshot>
    interpolationDelay = 0.1  // 100ms 지연
    
    class PositionSnapshot:
        position: Vector3
        rotation: Quaternion
        timestamp: float
    
    function OnReceivePosition(serverPosition, serverTimestamp):
        snapshot = new PositionSnapshot()
        snapshot.position = serverPosition
        snapshot.rotation = serverRotation
        snapshot.timestamp = serverTimestamp
        
        positionBuffer.Add(snapshot)
    
    function Update():
        currentTime = GetCurrentTime()
        renderTime = currentTime - interpolationDelay
        
        // 렌더링 시간에 해당하는 두 스냅샷 찾기
        from = null
        to = null
        
        for i in range(positionBuffer.Count - 1):
            if positionBuffer[i].timestamp <= renderTime and 
               positionBuffer[i + 1].timestamp >= renderTime:
                from = positionBuffer[i]
                to = positionBuffer[i + 1]
                break
        
        if from and to:
            // 두 스냅샷 사이를 보간
            duration = to.timestamp - from.timestamp
            t = (renderTime - from.timestamp) / duration
            
            interpolatedPosition = Lerp(from.position, to.position, t)
            interpolatedRotation = Slerp(from.rotation, to.rotation, t)
            
            remotePlayer.position = interpolatedPosition
            remotePlayer.rotation = interpolatedRotation
        
        // 오래된 스냅샷 제거
        positionBuffer.RemoveOlderThan(renderTime - 1.0)
```

보간은 항상 과거의 위치를 표시하므로(interpolationDelay만큼) 부드럽지만 실제보다 약간 뒤처진다. 대부분의 게임에서는 100-200ms 정도의 지연을 사용한다.

**외삽(Extrapolation):**

외삽은 마지막으로 받은 정보를 바탕으로 미래를 예측하는 방법이다. 보간보다 반응성이 좋지만 부정확할 수 있다.

```pseudocode
class RemotePlayerExtrapolation:
    lastPosition: Vector3
    lastVelocity: Vector3
    lastUpdateTime: float
    maxExtrapolationTime = 0.5  // 최대 500ms까지만 외삽
    
    function OnReceiveUpdate(position, velocity, timestamp):
        lastPosition = position
        lastVelocity = velocity
        lastUpdateTime = timestamp
    
    function GetCurrentPosition():
        currentTime = GetCurrentTime()
        timeSinceUpdate = currentTime - lastUpdateTime
        
        if timeSinceUpdate > maxExtrapolationTime:
            // 너무 오래되면 외삽 중단
            return lastPosition
        
        // 속도 기반 외삽
        extrapolatedPosition = lastPosition + lastVelocity * timeSinceUpdate
        
        return extrapolatedPosition
```

외삽은 네트워크가 일시적으로 끊겼을 때 캐릭터가 멈추지 않고 계속 움직이는 것처럼 보이게 한다. 하지만 방향 전환이나 정지를 예측할 수 없어서 서버 업데이트가 오면 순간이동처럼 보일 수 있다.

### 입력 버퍼링(Input Buffering)

입력 버퍼링은 플레이어의 입력을 일정 시간 동안 저장했다가 처리하는 기법이다. 특히 격투 게임이나 빠른 반응이 필요한 액션 게임에서 유용하다.

```pseudocode
class InputBuffer:
    bufferedInputs = Queue<BufferedInput>
    bufferWindow = 0.15  // 150ms 입력 버퍼
    
    class BufferedInput:
        inputType: InputType
        timestamp: float
        consumed: bool
    
    function OnInput(inputType):
        buffered = new BufferedInput()
        buffered.inputType = inputType
        buffered.timestamp = GetCurrentTime()
        buffered.consumed = false
        
        bufferedInputs.Enqueue(buffered)
    
    function TryConsumeInput(inputType):
        currentTime = GetCurrentTime()
        
        foreach input in bufferedInputs:
            if input.consumed:
                continue
                
            // 입력이 버퍼 윈도우 내에 있는지 확인
            if input.inputType == inputType and 
               (currentTime - input.timestamp) <= bufferWindow:
                input.consumed = true
                return true
        
        return false
    
    function Update():
        currentTime = GetCurrentTime()
        
        // 오래된 입력 제거
        while bufferedInputs.Count > 0:
            oldest = bufferedInputs.Peek()
            if (currentTime - oldest.timestamp) > bufferWindow:
                bufferedInputs.Dequeue()
            else:
                break

// 사용 예: 콤보 공격
class CombatSystem:
    function Update():
        if player.currentAction == IDLE:
            if inputBuffer.TryConsumeInput(ATTACK_BUTTON):
                player.StartAttack()
        
        else if player.currentAction == ATTACK_1:
            // 첫 번째 공격 중에도 다음 입력 받기
            if inputBuffer.TryConsumeInput(ATTACK_BUTTON):
                player.queuedAction = ATTACK_2
        
        // 공격 1이 끝나면 큐에 있는 공격 2 실행
        if player.currentAction == ATTACK_1 and player.actionFinished:
            if player.queuedAction == ATTACK_2:
                player.StartAttack2()
```

입력 버퍼링은 네트워크 지연과 무관하게 플레이어의 의도를 정확히 반영하여 조작감을 향상시킨다. 타이밍이 중요한 게임에서 필수적인 기술이다.

## 4.2 동기화 문제

온라인 게임에서는 여러 클라이언트가 동일한 게임 상태를 공유해야 한다. 하지만 네트워크 지연과 각 클라이언트의 프레임 레이트 차이로 인해 동기화 문제가 발생한다.

### 플레이어 위치 동기화

플레이어의 위치는 가장 기본적이면서도 중요한 동기화 대상이다. 앞서 설명한 클라이언트 측 예측과 보간을 조합하여 처리한다.

**효율적인 위치 전송:**

```pseudocode
class PositionSyncSystem:
    lastSentPosition: Vector3
    lastSentRotation: float
    positionThreshold = 0.1    // 10cm 이상 변화 시 전송
    rotationThreshold = 5.0    // 5도 이상 변화 시 전송
    maxUpdateInterval = 0.1    // 최대 100ms마다 강제 전송
    lastSendTime = 0
    
    function Update(deltaTime):
        currentTime = GetCurrentTime()
        timeSinceLastSend = currentTime - lastSendTime
        
        currentPosition = player.position
        currentRotation = player.rotation.yaw
        
        positionChanged = Distance(currentPosition, lastSentPosition) > positionThreshold
        rotationChanged = Abs(currentRotation - lastSentRotation) > rotationThreshold
        timeExpired = timeSinceLastSend > maxUpdateInterval
        
        if positionChanged or rotationChanged or timeExpired:
            SendPositionUpdate(currentPosition, currentRotation)
            
            lastSentPosition = currentPosition
            lastSentRotation = currentRotation
            lastSendTime = currentTime
    
    function SendPositionUpdate(position, rotation):
        packet = CreatePacket(POSITION_UPDATE)
        packet.position = CompressPosition(position)
        packet.rotation = CompressRotation(rotation)
        packet.velocity = CompressVelocity(player.velocity)
        packet.timestamp = GetCurrentTime()
        
        SendToServer(packet)
```

변화가 있을 때만 전송하는 방식으로 불필요한 네트워크 트래픽을 줄인다. 정지 상태에서는 위치를 계속 보낼 필요가 없다.

**데드 레코닝(Dead Reckoning):**

서버 업데이트 사이의 시간을 메우기 위해 속도 정보를 활용한다.

```pseudocode
class DeadReckoning:
    lastKnownPosition: Vector3
    lastKnownVelocity: Vector3
    lastUpdateTime: float
    
    function OnServerUpdate(position, velocity, timestamp):
        lastKnownPosition = position
        lastKnownVelocity = velocity
        lastUpdateTime = timestamp
    
    function GetEstimatedPosition():
        currentTime = GetCurrentTime()
        deltaTime = currentTime - lastUpdateTime
        
        // 속도를 이용한 위치 추정
        estimatedPosition = lastKnownPosition + lastKnownVelocity * deltaTime
        
        // 물리 법칙 적용 (예: 중력)
        if not isGrounded:
            estimatedPosition.y += 0.5 * gravity * deltaTime * deltaTime
        
        return estimatedPosition
```

### 애니메이션 동기화

애니메이션은 위치만큼 자주 업데이트할 필요가 없지만, 다른 플레이어의 행동을 올바르게 표현하려면 동기화가 필요하다.

**상태 기반 애니메이션 동기화:**

```pseudocode
class AnimationSyncSystem:
    currentAnimState: AnimationState
    animationParameters = Map<string, float>
    
    function OnServerUpdate(serverData):
        newAnimState = serverData.animationState
        
        if newAnimState != currentAnimState:
            // 상태 변화 시 애니메이션 전환
            TransitionToAnimation(newAnimState, serverData.transitionTime)
            currentAnimState = newAnimState
        
        // 파라미터 업데이트 (속도, 방향 등)
        animationParameters["Speed"] = serverData.movementSpeed
        animationParameters["Direction"] = serverData.movementDirection
    
    function TransitionToAnimation(newState, blendTime):
        animator.CrossFade(newState, blendTime)
    
    // 클라이언트에서 서버로 전송
    function SendAnimationUpdate():
        // 중요한 상태 변화만 전송
        if HasSignificantStateChange():
            packet = CreatePacket(ANIMATION_UPDATE)
            packet.state = currentAnimState
            packet.speed = GetMovementSpeed()
            packet.direction = GetMovementDirection()
            SendToServer(packet)
```

**이벤트 기반 애니메이션:**

특정 액션(공격, 점프, 스킬 사용 등)은 이벤트로 전송하여 정확한 타이밍에 재생한다.

```pseudocode
class AnimationEventSystem:
    function OnPlayerAttack():
        // 로컬에서 즉시 재생
        PlayAttackAnimation()
        
        // 서버에 이벤트 전송
        packet = CreatePacket(ANIMATION_EVENT)
        packet.eventType = ATTACK
        packet.animationID = currentAttackAnimID
        packet.timestamp = GetCurrentTime()
        SendToServer(packet)
    
    function OnReceiveAnimationEvent(eventData):
        switch eventData.eventType:
            case ATTACK:
                PlayAttackAnimation(eventData.animationID)
            case SKILL:
                PlaySkillAnimation(eventData.skillID)
            case EMOTE:
                PlayEmoteAnimation(eventData.emoteID)
        
        // 이벤트에 사운드나 이펙트도 함께 재생
        PlayAssociatedEffects(eventData)
```

### 게임 상태 불일치 해결

클라이언트와 서버의 상태가 달라지는 상황은 자주 발생한다. 이를 감지하고 해결하는 메커니즘이 필요하다.

**체크섬을 이용한 상태 검증:**

```pseudocode
class StateValidation:
    function CalculateStateChecksum():
        checksum = 0
        
        // 중요한 게임 상태들을 해시
        checksum ^= Hash(player.position)
        checksum ^= Hash(player.health)
        checksum ^= Hash(player.inventory.GetItemIDs())
        checksum ^= Hash(gameMode.currentScore)
        
        return checksum
    
    function ValidateState(serverChecksum):
        clientChecksum = CalculateStateChecksum()
        
        if clientChecksum != serverChecksum:
            // 불일치 감지
            LogStateDesync()
            RequestFullStateUpdate()
    
    function RequestFullStateUpdate():
        packet = CreatePacket(REQUEST_FULL_STATE)
        SendToServer(packet)
    
    function OnFullStateUpdate(serverState):
        // 서버의 권한 있는 상태로 덮어쓰기
        player.position = serverState.playerPosition
        player.health = serverState.playerHealth
        player.inventory.Clear()
        player.inventory.AddItems(serverState.inventoryItems)
        gameMode.SetScore(serverState.score)
```

**점진적 보정:**

급격한 상태 변화를 피하기 위해 점진적으로 서버 상태에 수렴시킨다.

```pseudocode
class GradualStateCorrection:
    healthDiscrepancy = 0
    correctionRate = 5.0  // 초당 5 포인트씩 보정
    
    function OnServerHealthUpdate(serverHealth):
        clientHealth = player.health
        healthDiscrepancy = serverHealth - clientHealth
    
    function Update(deltaTime):
        if Abs(healthDiscrepancy) > 0.1:
            correction = correctionRate * deltaTime * Sign(healthDiscrepancy)
            
            if Abs(correction) > Abs(healthDiscrepancy):
                correction = healthDiscrepancy
            
            player.health += correction
            healthDiscrepancy -= correction
            
            UpdateHealthUI(player.health)
```

## 4.3 치팅 방지를 위한 클라이언트 설계

클라이언트는 본질적으로 신뢰할 수 없는 환경이다. 플레이어가 메모리를 수정하거나 패킷을 조작할 수 있으므로, 클라이언트 개발자는 치팅을 어렵게 만들고 서버 검증을 용이하게 하는 코드를 작성해야 한다.

### 서버 검증의 중요성

클라이언트에서 발생하는 모든 중요한 행동은 서버의 검증을 거쳐야 한다.

**검증이 필요한 액션:**

```pseudocode
class PlayerAction:
    function AttemptAttack(targetID):
        // 클라이언트에서 즉시 시각적 피드백
        PlayAttackAnimation()
        ShowAttackEffect()
        
        // 서버에 요청 전송 (결과를 주장하지 않음)
        packet = CreatePacket(ATTACK_REQUEST)
        packet.targetID = targetID
        packet.attackType = currentWeapon.attackType
        packet.timestamp = GetCurrentTime()
        SendToServer(packet)
        
        // 서버 응답을 기다림 (결과는 서버가 결정)
    
    function OnAttackResult(result):
        if result.success:
            // 서버가 공격 성공 확인
            if result.targetHit:
                ShowHitEffect(result.targetID, result.damageDealt)
        else:
            // 서버가 공격 거부 (거리, 쿨다운, 자원 부족 등)
            RevertAttackAnimation()
            ShowErrorMessage(result.failureReason)
```

서버는 클라이언트의 요청을 받으면 다음을 검증한다:
- 공격 가능한 거리인가?
- 쿨다운이 끝났는가?
- 충분한 자원(마나, 스태미나 등)이 있는가?
- 대상이 유효한가?
- 시간이 합리적인가? (속도 핵 방지)

### 클라이언트에서 보내는 데이터 최소화

클라이언트는 입력과 의도만 전송하고, 결과는 서버에서 계산하도록 해야 한다.

**잘못된 방식 (취약함):**

```pseudocode
// 나쁜 예: 클라이언트가 결과를 계산하여 전송
function OnAttackButtonPressed():
    target = GetTargetEnemy()
    damage = CalculateDamage(player.attackPower, target.defense)
    
    packet = CreatePacket(ATTACK)
    packet.targetID = target.id
    packet.damage = damage  // 조작 가능!
    SendToServer(packet)
```

**올바른 방식:**

```pseudocode
// 좋은 예: 입력만 전송
function OnAttackButtonPressed():
    target = GetTargetEnemy()
    
    packet = CreatePacket(ATTACK_REQUEST)
    packet.targetID = target.id
    // 데미지는 서버가 계산
    SendToServer(packet)
```

**이동 검증:**

```pseudocode
class MovementSystem:
    function SendMovementInput(input):
        packet = CreatePacket(MOVE_INPUT)
        packet.forward = input.forward
        packet.right = input.right
        packet.jump = input.jump
        packet.timestamp = GetCurrentTime()
        // 위치를 보내지 않음, 입력만 보냄
        SendToServer(packet)
    
    function OnServerPositionCorrection(serverPosition):
        // 서버가 계산한 결과로 보정
        errorDistance = Distance(player.position, serverPosition)
        
        if errorDistance > acceptableThreshold:
            // 차이가 크면 서버 위치 적용
            player.position = serverPosition
```

### 안티-치트 시스템과의 협력

클라이언트 코드는 안티-치트 시스템이 작동할 수 있도록 설계되어야 한다.

**타임스탬프 활용:**

```pseudocode
class AntiCheatHelper:
    function SendGameAction(actionType, actionData):
        packet = CreatePacket(GAME_ACTION)
        packet.actionType = actionType
        packet.actionData = actionData
        packet.clientTimestamp = GetClientTime()
        packet.sequenceNumber = GetNextSequenceNumber()
        
        SendToServer(packet)
    
    // 서버는 타임스탬프와 시퀀스를 검증하여
    // 패킷 재전송 공격이나 시간 조작을 탐지
```

**클라이언트 측 샌티티 체크:**

서버 검증이 주된 방어선이지만, 클라이언트에서도 기본적인 검증을 수행하여 명백한 치팅 시도를 조기에 차단한다.

```pseudocode
class ClientSideValidation:
    function ValidateAction(action):
        // 기본적인 규칙 검증
        if action.type == ATTACK:
            if not HasWeaponEquipped():
                return false
            if IsOnCooldown(action.attackType):
                return false
            if not HasLineOfSight(action.targetID):
                return false
        
        return true
    
    // 하지만 서버는 클라이언트 검증을 신뢰하지 않고
    // 모든 것을 다시 검증한다
```

## 4.4 패킷 손실 대응

UDP를 사용하는 게임에서는 패킷 손실이 발생한다. 중요한 데이터는 손실되어서는 안 되므로, 신뢰성 메커니즘을 직접 구현해야 한다.

### 중요 데이터의 재전송 로직

모든 패킷을 재전송할 필요는 없지만, 중요한 이벤트는 확인될 때까지 재전송해야 한다.

**신뢰성 있는 메시지 전송:**

```pseudocode
class ReliableMessageSystem:
    pendingAcks = Map<MessageID, ReliableMessage>
    nextMessageID = 1
    resendInterval = 0.3  // 300ms마다 재전송
    maxResendAttempts = 10
    
    class ReliableMessage:
        id: int
        data: bytes
        sendTime: float
        resendCount: int
    
    function SendReliable(messageType, data):
        messageID = nextMessageID++
        
        message = new ReliableMessage()
        message.id = messageID
        message.data = SerializeMessage(messageType, data)
        message.sendTime = GetCurrentTime()
        message.resendCount = 0
        
        pendingAcks[messageID] = message
        SendMessageToServer(messageID, message.data, needsAck: true)
    
    function Update():
        currentTime = GetCurrentTime()
        
        foreach message in pendingAcks.Values:
            timeSinceSend = currentTime - message.sendTime
            
            if timeSinceSend > resendInterval:
                message.resendCount++
                
                if message.resendCount > maxResendAttempts:
                    // 재전송 한계 도달, 연결 끊김으로 간주
                    HandleConnectionLost()
                    pendingAcks.Remove(message.id)
                else:
                    // 재전송
                    SendMessageToServer(message.id, message.data, needsAck: true)
                    message.sendTime = currentTime
    
    function OnAckReceived(messageID):
        if pendingAcks.Contains(messageID):
            pendingAcks.Remove(messageID)
```

### UDP에서의 신뢰성 구현

UDP는 기본적으로 신뢰성을 보장하지 않지만, 필요한 부분에만 선택적으로 신뢰성을 추가할 수 있다.

**확인 응답(ACK) 시스템:**

```pseudocode
class UDPReliability:
    sentPackets = Map<SequenceNumber, PacketInfo>
    receivedPackets = Set<SequenceNumber>
    localSequence = 0
    remoteSequence = 0
    
    function SendPacket(data, reliable):
        packet = new Packet()
        packet.sequence = localSequence++
        packet.ack = remoteSequence
        packet.ackBits = GenerateAckBits()  // 최근 수신한 32개 패킷 정보
        packet.reliable = reliable
        packet.data = data
        
        if reliable:
            sentPackets[packet.sequence] = new PacketInfo(packet, GetCurrentTime())
        
        SendUDP(packet)
    
    function GenerateAckBits():
        bits = 0
        for i in range(1, 33):
            if receivedPackets.Contains(remoteSequence - i):
                bits |= (1 << (i - 1))
        return bits
    
    function OnPacketReceived(packet):
        // 중복 패킷 무시
        if receivedPackets.Contains(packet.sequence):
            return
        
        receivedPackets.Add(packet.sequence)
        
        // 가장 최근 시퀀스 업데이트
        if packet.sequence > remoteSequence:
            remoteSequence = packet.sequence
        
        // ACK 처리
        ProcessAck(packet.ack, packet.ackBits)
        
        // 데이터 처리
        ProcessPacketData(packet.data)
    
    function ProcessAck(ack, ackBits):
        // 가장 최근 패킷 확인
        if sentPackets.Contains(ack):
            OnPacketAcknowledged(ack)
            sentPackets.Remove(ack)
        
        // ackBits로 이전 패킷들 확인
        for i in range(1, 33):
            if (ackBits & (1 << (i - 1))) != 0:
                seq = ack - i
                if sentPackets.Contains(seq):
                    OnPacketAcknowledged(seq)
                    sentPackets.Remove(seq)
```

### 데이터 우선순위 관리

모든 데이터가 동일한 중요도를 가지지 않는다. 우선순위를 정하여 제한된 대역폭을 효율적으로 사용한다.

**우선순위 큐 시스템:**

```pseudocode
class PriorityPacketQueue:
    highPriorityQueue = Queue<Packet>     // 중요 이벤트
    normalPriorityQueue = Queue<Packet>   // 일반 업데이트
    lowPriorityQueue = Queue<Packet>      // 배경 정보
    
    maxPacketsPerFrame = 10
    
    function EnqueuePacket(packet, priority):
        switch priority:
            case HIGH:
                highPriorityQueue.Enqueue(packet)
            case NORMAL:
                normalPriorityQueue.Enqueue(packet)
            case LOW:
                lowPriorityQueue.Enqueue(packet)
    
    function SendQueuedPackets():
        sentCount = 0
        
        // 높은 우선순위부터 전송
        while sentCount < maxPacketsPerFrame and highPriorityQueue.Count > 0:
            packet = highPriorityQueue.Dequeue()
            SendPacket(packet)
            sentCount++
        
        // 남은 할당량으로 일반 우선순위 전송
        while sentCount < maxPacketsPerFrame and normalPriorityQueue.Count > 0:
            packet = normalPriorityQueue.Dequeue()
            SendPacket(packet)
            sentCount++
        
        // 여유가 있으면 낮은 우선순위 전송
        while sentCount < maxPacketsPerFrame and lowPriorityQueue.Count > 0:
            packet = lowPriorityQueue.Dequeue()
            SendPacket(packet)
            sentCount++

// 사용 예
function SendPlayerAction(action):
    packet = CreateActionPacket(action)
    packetQueue.EnqueuePacket(packet, HIGH)  // 플레이어 액션은 높은 우선순위

function SendPositionUpdate(position):
    packet = CreatePositionPacket(position)
    packetQueue.EnqueuePacket(packet, NORMAL)  // 위치는 일반 우선순위

function SendChatMessage(message):
    packet = CreateChatPacket(message)
    packetQueue.EnqueuePacket(packet, LOW)  // 채팅은 낮은 우선순위
```

## 4.5 대역폭 최적화

제한된 대역폭을 효율적으로 사용하는 것은 온라인 게임의 필수 요소다. 특히 모바일 환경이나 많은 플레이어가 동시에 접속하는 게임에서 중요하다.

### 델타 압축(Delta Compression)

이전 상태와의 차이만 전송하여 데이터 양을 줄인다.

**델타 인코딩:**

```pseudocode
class DeltaCompression:
    lastSentState = null
    
    function SendState(currentState):
        if lastSentState == null:
            // 첫 전송은 전체 상태
            SendFullState(currentState)
            lastSentState = currentState.Clone()
        else:
            // 이후는 델타만 전송
            delta = CalculateDelta(lastSentState, currentState)
            
            if delta.GetSize() < currentState.GetSize() * 0.7:
                // 델타가 충분히 작으면 델타 전송
                SendDeltaState(delta)
                lastSentState = currentState.Clone()
            else:
                // 변화가 크면 전체 상태 전송
                SendFullState(currentState)
                lastSentState = currentState.Clone()
    
    function CalculateDelta(oldState, newState):
        delta = new DeltaState()
        
        if oldState.position != newState.position:
            delta.positionChanged = true
            delta.position = newState.position
        
        if oldState.health != newState.health:
            delta.healthChanged = true
            delta.healthDelta = newState.health - oldState.health  // 차이만 저장
        
        if oldState.rotation != newState.rotation:
            delta.rotationChanged = true
            delta.rotation = newState.rotation
        
        return delta
```

**비트 플래그를 이용한 필드 선택:**

```pseudocode
class OptimizedStateUpdate:
    function SerializeUpdate(state, previousState):
        buffer = ByteBuffer()
        flags = 0
        
        // 변경된 필드만 플래그 설정
        if state.position != previousState.position:
            flags |= FLAG_POSITION
        if state.rotation != previousState.rotation:
            flags |= FLAG_ROTATION
        if state.velocity != previousState.velocity:
            flags |= FLAG_VELOCITY
        if state.health != previousState.health:
            flags |= FLAG_HEALTH
        
        buffer.WriteByte(flags)
        
        // 플래그가 설정된 필드만 직렬화
        if (flags & FLAG_POSITION) != 0:
            buffer.WriteVector3(state.position)
        if (flags & FLAG_ROTATION) != 0:
            buffer.WriteQuaternion(state.rotation)
        if (flags & FLAG_VELOCITY) != 0:
            buffer.WriteVector3(state.velocity)
        if (flags & FLAG_HEALTH) != 0:
            buffer.WriteInt16(state.health)
        
        return buffer.GetBytes()
```

### 관심 영역 관리(Area of Interest)

플레이어 주변의 제한된 영역에 대한 정보만 받아 대역폭을 절약한다.

**관심 영역 필터링:**

```pseudocode
class AreaOfInterest:
    interestRadius = 50.0  // 50 유닛 반경
    relevantEntities = Set<EntityID>
    
    function UpdateRelevantEntities():
        playerPosition = GetPlayerPosition()
        newRelevantEntities = Set<EntityID>()
        
        foreach entity in allEntities:
            distance = Distance(playerPosition, entity.position)
            
            if distance <= interestRadius:
                newRelevantEntities.Add(entity.id)
        
        // 새로 관심 영역에 들어온 엔티티
        entered = newRelevantEntities - relevantEntities
        foreach entityID in entered:
            RequestEntityData(entityID)
            OnEntityEntered(entityID)
        
        // 관심 영역을 벗어난 엔티티
        exited = relevantEntities - newRelevantEntities
        foreach entityID in exited:
            OnEntityExited(entityID)
            RemoveEntityData(entityID)
        
        relevantEntities = newRelevantEntities
    
    function SendInterestRegionToServer():
        packet = CreatePacket(UPDATE_INTEREST_REGION)
        packet.centerPosition = GetPlayerPosition()
        packet.radius = interestRadius
        SendToServer(packet)
```

**계층적 거리 기반 업데이트:**

거리에 따라 업데이트 빈도를 다르게 한다.

```pseudocode
class LODNetworkUpdate:
    function GetUpdateInterval(distance):
        if distance < 10.0:
            return 0.05  // 매우 가까움: 20 Hz
        else if distance < 30.0:
            return 0.1   // 가까움: 10 Hz
        else if distance < 50.0:
            return 0.2   // 보통: 5 Hz
        else:
            return 0.5   // 멀리: 2 Hz
    
    entityLastUpdate = Map<EntityID, float>
    
    function ShouldUpdateEntity(entityID, distance):
        currentTime = GetCurrentTime()
        lastUpdate = entityLastUpdate.GetOrDefault(entityID, 0)
        interval = GetUpdateInterval(distance)
        
        if currentTime - lastUpdate >= interval:
            entityLastUpdate[entityID] = currentTime
            return true
        
        return false
```

### 업데이트 빈도 조절

네트워크 상태에 따라 업데이트 빈도를 동적으로 조절한다.

**적응형 업데이트 레이트:**

```pseudocode
class AdaptiveUpdateRate:
    currentUpdateRate = 20.0  // 초당 20번
    minUpdateRate = 5.0
    maxUpdateRate = 30.0
    
    avgLatency = 0.0
    avgPacketLoss = 0.0
    
    function AdjustUpdateRate():
        // 네트워크 품질 측정
        avgLatency = GetAverageLatency()
        avgPacketLoss = GetPacketLossRate()
        
        if avgLatency > 200 or avgPacketLoss > 0.1:
            // 네트워크 상태 나쁨: 빈도 감소
            currentUpdateRate = Max(currentUpdateRate * 0.9, minUpdateRate)
        
        else if avgLatency < 50 and avgPacketLoss < 0.01:
            // 네트워크 상태 좋음: 빈도 증가
            currentUpdateRate = Min(currentUpdateRate * 1.1, maxUpdateRate)
    
    function GetUpdateInterval():
        return 1.0 / currentUpdateRate

// 사용 예
class NetworkUpdateLoop:
    lastUpdateTime = 0
    
    function Update():
        currentTime = GetCurrentTime()
        updateInterval = adaptiveRate.GetUpdateInterval()
        
        if currentTime - lastUpdateTime >= updateInterval:
            SendNetworkUpdate()
            lastUpdateTime = currentTime
            
            // 주기적으로 업데이트 레이트 조정
            if ShouldAdjustRate():
                adaptiveRate.AdjustUpdateRate()
```

**대역폭 예산 관리:**

```pseudocode
class BandwidthBudget:
    maxBytesPerSecond = 10000  // 10 KB/s
    currentSecondBytes = 0
    currentSecondStart = 0
    
    function CanSendPacket(packetSize):
        currentTime = GetCurrentTime()
        
        // 새로운 초가 시작되면 리셋
        if Floor(currentTime) != Floor(currentSecondStart):
            currentSecondBytes = 0
            currentSecondStart = currentTime
        
        // 예산 내에 있는지 확인
        if currentSecondBytes + packetSize <= maxBytesPerSecond:
            currentSecondBytes += packetSize
            return true
        
        return false
    
    function SendPacketIfPossible(packet):
        if CanSendPacket(packet.GetSize()):
            SendPacket(packet)
            return true
        else:
            // 예산 초과, 큐에 넣거나 드롭
            if packet.priority == HIGH:
                queuedPackets.Enqueue(packet)
            return false
```

온라인 게임의 주요 이슈들은 서로 연관되어 있으며, 하나의 완벽한 해결책은 존재하지 않는다. 클라이언트 개발자는 게임의 장르, 대상 플랫폼, 예상되는 네트워크 환경을 고려하여 적절한 기법들을 조합해야 한다. 지연 시간 감추기, 동기화 유지, 치팅 방지, 패킷 손실 대응, 대역폭 최적화는 모두 균형있게 다루어져야 하며, 실제 플레이 테스트를 통해 지속적으로 개선해야 한다.


# 5. 실시간 게임플레이 구현

실시간 온라인 게임은 여러 플레이어가 동시에 상호작용하면서도 일관된 게임 세계를 경험해야 한다. 이를 위해서는 시간 관리, 상태 동기화, 지연 보상 등의 고급 기술이 필요하다. 이 장에서는 실시간 게임플레이를 구현하기 위한 핵심 개념과 기법들을 다룬다.

## 5.1 틱(Tick)과 프레임 개념

게임에서 '틱(Tick)'은 게임 로직이 실행되는 고정된 시간 간격을 의미한다. 프레임은 화면을 렌더링하는 단위이며, 틱과 프레임은 서로 독립적으로 동작해야 한다.

### 서버 틱 레이트(Tick Rate)

서버 틱 레이트는 서버가 초당 몇 번 게임 상태를 업데이트하는지를 나타낸다. 일반적으로 FPS 게임은 64 tick(초당 64회), MOBA 게임은 30 tick, MMO는 10-20 tick을 사용한다.

**서버 틱 구현:**

```pseudocode
class GameServer:
    tickRate = 64
    tickInterval = 1.0 / tickRate  // 약 15.6ms
    currentTick = 0
    accumulator = 0.0
    lastTime = 0.0
    
    function Run():
        lastTime = GetCurrentTime()
        
        while serverRunning:
            currentTime = GetCurrentTime()
            deltaTime = currentTime - lastTime
            lastTime = currentTime
            
            accumulator += deltaTime
            
            // 고정 시간 간격으로 틱 실행
            while accumulator >= tickInterval:
                ProcessTick()
                currentTick++
                accumulator -= tickInterval
            
            // 네트워크 메시지 처리
            ProcessNetworkMessages()
            
            // CPU 사용률 조절
            Sleep(1)
    
    function ProcessTick():
        // 입력 처리
        ProcessPlayerInputs()
        
        // 물리 시뮬레이션
        UpdatePhysics(tickInterval)
        
        // 게임 로직
        UpdateGameLogic(tickInterval)
        
        // 충돌 검사
        ProcessCollisions()
        
        // 상태 스냅샷 생성
        CreateSnapshot(currentTick)
```

서버는 틱마다 모든 플레이어의 입력을 처리하고, 게임 월드를 시뮬레이션하며, 그 결과를 클라이언트들에게 전송한다. 고정된 틱 레이트를 사용하면 게임 로직이 예측 가능하고 일관되게 동작한다.

### 클라이언트 프레임 레이트와의 분리

클라이언트의 렌더링 프레임 레이트는 유동적이지만, 게임 로직은 고정된 틱으로 업데이트되어야 한다. 이를 통해 게임플레이가 프레임 레이트에 영향을 받지 않게 된다.

**고정 틱과 가변 프레임 분리:**

```pseudocode
class GameClient:
    logicTickRate = 64
    logicTickInterval = 1.0 / logicTickRate
    logicAccumulator = 0.0
    
    renderFrameTime = 0.0
    lastFrameTime = 0.0
    
    function GameLoop():
        lastFrameTime = GetCurrentTime()
        
        while gameRunning:
            currentTime = GetCurrentTime()
            deltaTime = currentTime - lastFrameTime
            lastFrameTime = currentTime
            
            // 입력은 매 프레임 수집
            ProcessInput()
            
            // 고정 틱으로 게임 로직 업데이트
            logicAccumulator += deltaTime
            
            while logicAccumulator >= logicTickInterval:
                UpdateGameLogic(logicTickInterval)
                logicAccumulator -= logicTickInterval
            
            // 네트워크 업데이트
            UpdateNetwork()
            
            // 보간 계수 계산 (남은 시간 비율)
            interpolationAlpha = logicAccumulator / logicTickInterval
            
            // 가변 프레임으로 렌더링
            Render(deltaTime, interpolationAlpha)
```

**보간을 이용한 부드러운 렌더링:**

게임 로직은 64 tick으로 업데이트되지만, 렌더링은 144Hz 모니터에서 144 FPS로 동작할 수 있다. 이때 보간을 사용하여 틱 사이를 부드럽게 연결한다.

```pseudocode
class InterpolatedTransform:
    previousPosition: Vector3
    currentPosition: Vector3
    previousRotation: Quaternion
    currentRotation: Quaternion
    
    function OnLogicTick(newPosition, newRotation):
        // 이전 현재 값을 이전 값으로
        previousPosition = currentPosition
        previousRotation = currentRotation
        
        // 새 값을 현재 값으로
        currentPosition = newPosition
        currentRotation = newRotation
    
    function GetRenderTransform(alpha):
        // alpha는 0.0 ~ 1.0 사이의 보간 계수
        renderPosition = Lerp(previousPosition, currentPosition, alpha)
        renderRotation = Slerp(previousRotation, currentRotation, alpha)
        
        return Transform(renderPosition, renderRotation)

// 사용 예
function Render(deltaTime, interpolationAlpha):
    foreach entity in visibleEntities:
        renderTransform = entity.GetRenderTransform(interpolationAlpha)
        DrawEntity(entity, renderTransform)
```

이렇게 하면 60 tick 게임도 144 FPS로 부드럽게 렌더링될 수 있다. 게임 로직은 일관된 속도로 실행되면서도, 시각적으로는 높은 프레임 레이트의 이점을 누릴 수 있다.

### 틱 동기화

클라이언트와 서버의 틱을 동기화하여 일관된 시간 기준을 유지해야 한다.

**타임스탬프 기반 동기화:**

```pseudocode
class TickSynchronization:
    serverTick = 0
    clientTick = 0
    tickOffset = 0  // 클라이언트와 서버 틱 차이
    
    lastSyncTime = 0.0
    syncInterval = 5.0  // 5초마다 동기화
    
    function OnServerSnapshot(snapshot):
        serverTick = snapshot.tick
        
        // 예상 서버 틱 계산
        expectedServerTick = serverTick + tickOffset
        
        // 오차가 크면 조정
        tickError = expectedServerTick - clientTick
        
        if Abs(tickError) > 5:
            // 큰 오차는 즉시 보정
            clientTick = expectedServerTick
        else if Abs(tickError) > 1:
            // 작은 오차는 점진적 보정
            clientTick += Sign(tickError)
    
    function SynchronizeClock():
        currentTime = GetCurrentTime()
        
        if currentTime - lastSyncTime > syncInterval:
            SendClockSyncRequest()
            lastSyncTime = currentTime
    
    function OnClockSyncResponse(response):
        // RTT 기반 서버 시간 추정
        rtt = GetCurrentTime() - response.requestTime
        estimatedServerTime = response.serverTime + (rtt / 2.0)
        
        localTime = GetCurrentTime()
        timeOffset = estimatedServerTime - localTime
        
        // 틱 오프셋 계산
        tickOffset = Floor(timeOffset / tickInterval)
```

**지터 보정:**

네트워크 지터로 인해 패킷 도착 시간이 불규칙할 수 있다. 이를 보정하여 안정적인 틱 동기화를 유지한다.

```pseudocode
class JitterBuffer:
    bufferSize = 3  // 3 틱 분량 버퍼링
    snapshotBuffer = Queue<Snapshot>
    
    function OnSnapshotReceived(snapshot):
        snapshotBuffer.Enqueue(snapshot)
        
        // 버퍼가 충분히 차면 소비 시작
        if snapshotBuffer.Count >= bufferSize:
            ConsumeSnapshot()
    
    function ConsumeSnapshot():
        if snapshotBuffer.Count > 0:
            snapshot = snapshotBuffer.Dequeue()
            ApplySnapshot(snapshot)
            
            // 버퍼 크기 동적 조정
            AdjustBufferSize()
    
    function AdjustBufferSize():
        jitter = CalculateJitter()
        
        if jitter > threshold:
            bufferSize = Min(bufferSize + 1, maxBufferSize)
        else if jitter < lowThreshold and bufferSize > minBufferSize:
            bufferSize = Max(bufferSize - 1, minBufferSize)
```

## 5.2 스냅샷 시스템

스냅샷은 특정 시점의 전체 게임 상태를 담은 데이터 구조다. 서버는 매 틱마다 스냅샷을 생성하고, 클라이언트는 이를 받아서 게임 상태를 재구성한다.

### 게임 상태 스냅샷

스냅샷은 게임 월드의 모든 중요한 정보를 포함한다.

**스냅샷 구조:**

```pseudocode
class WorldSnapshot:
    tick: int
    timestamp: float
    
    players: List<PlayerSnapshot>
    projectiles: List<ProjectileSnapshot>
    dynamicObjects: List<ObjectSnapshot>
    events: List<GameEvent>
    
    class PlayerSnapshot:
        playerID: int
        position: Vector3
        rotation: Quaternion
        velocity: Vector3
        health: int
        animationState: int
        weaponState: int
    
    class ProjectileSnapshot:
        projectileID: int
        position: Vector3
        velocity: Vector3
        type: int
    
    class GameEvent:
        eventType: EventType
        timestamp: float
        data: bytes

// 서버에서 스냅샷 생성
class SnapshotGenerator:
    function CreateSnapshot(tick):
        snapshot = new WorldSnapshot()
        snapshot.tick = tick
        snapshot.timestamp = GetCurrentTime()
        
        // 모든 플레이어 상태 수집
        foreach player in activePlayers:
            playerSnap = new PlayerSnapshot()
            playerSnap.playerID = player.id
            playerSnap.position = player.position
            playerSnap.rotation = player.rotation
            playerSnap.velocity = player.velocity
            playerSnap.health = player.health
            playerSnap.animationState = player.currentAnimation
            playerSnap.weaponState = player.weaponState
            
            snapshot.players.Add(playerSnap)
        
        // 발사체 상태 수집
        foreach projectile in activeProjectiles:
            projSnap = new ProjectileSnapshot()
            projSnap.projectileID = projectile.id
            projSnap.position = projectile.position
            projSnap.velocity = projectile.velocity
            projSnap.type = projectile.type
            
            snapshot.projectiles.Add(projSnap)
        
        // 이벤트 수집 (플레이어 사망, 점수 등)
        snapshot.events = CollectGameEvents(tick)
        
        return snapshot
```

**스냅샷 압축:**

스냅샷을 효율적으로 전송하기 위해 압축한다.

```pseudocode
class SnapshotCompression:
    lastSentSnapshot = null
    
    function CompressSnapshot(currentSnapshot):
        if lastSentSnapshot == null:
            // 첫 스냅샷은 전체 전송
            compressed = FullSnapshotCompression(currentSnapshot)
            compressed.isDelta = false
        else:
            // 델타 스냅샷 생성
            compressed = DeltaSnapshotCompression(lastSentSnapshot, currentSnapshot)
            compressed.isDelta = true
        
        lastSentSnapshot = currentSnapshot
        return compressed
    
    function DeltaSnapshotCompression(oldSnap, newSnap):
        delta = new CompressedSnapshot()
        delta.baseTick = oldSnap.tick
        delta.currentTick = newSnap.tick
        
        // 변경된 플레이어만 포함
        foreach newPlayer in newSnap.players:
            oldPlayer = oldSnap.FindPlayer(newPlayer.playerID)
            
            if oldPlayer == null:
                // 새 플레이어는 전체 데이터
                delta.AddFullPlayer(newPlayer)
            else:
                // 변경 사항만 인코딩
                playerDelta = CalculatePlayerDelta(oldPlayer, newPlayer)
                if playerDelta.hasChanges:
                    delta.AddPlayerDelta(playerDelta)
        
        // 삭제된 플레이어
        foreach oldPlayer in oldSnap.players:
            if not newSnap.HasPlayer(oldPlayer.playerID):
                delta.AddRemovedPlayer(oldPlayer.playerID)
        
        return delta
    
    function CalculatePlayerDelta(old, new):
        delta = new PlayerDelta()
        delta.playerID = new.playerID
        delta.hasChanges = false
        
        if Distance(old.position, new.position) > positionThreshold:
            delta.position = new.position
            delta.hasPosition = true
            delta.hasChanges = true
        
        if AngleDiff(old.rotation, new.rotation) > rotationThreshold:
            delta.rotation = new.rotation
            delta.hasRotation = true
            delta.hasChanges = true
        
        if old.health != new.health:
            delta.health = new.health
            delta.hasHealth = true
            delta.hasChanges = true
        
        return delta
```

### 스냅샷 보간

클라이언트는 받은 스냅샷들 사이를 보간하여 부드러운 움직임을 만든다.

**스냅샷 보간 시스템:**

```pseudocode
class SnapshotInterpolation:
    snapshotHistory = CircularBuffer<WorldSnapshot>
    interpolationDelay = 100  // 100ms 지연
    
    function OnSnapshotReceived(snapshot):
        snapshotHistory.Add(snapshot)
        
        // 오래된 스냅샷 제거 (1초 이상)
        snapshotHistory.RemoveOlderThan(GetCurrentTime() - 1.0)
    
    function GetInterpolatedState():
        currentTime = GetCurrentTime()
        renderTime = currentTime - interpolationDelay
        
        // renderTime에 해당하는 두 스냅샷 찾기
        from = null
        to = null
        
        for i in range(snapshotHistory.Count - 1):
            snap1 = snapshotHistory[i]
            snap2 = snapshotHistory[i + 1]
            
            if snap1.timestamp <= renderTime and snap2.timestamp >= renderTime:
                from = snap1
                to = snap2
                break
        
        if from == null or to == null:
            // 보간할 스냅샷이 없으면 가장 최근 것 사용
            return snapshotHistory.GetLatest()
        
        // 보간 계수 계산
        duration = to.timestamp - from.timestamp
        t = (renderTime - from.timestamp) / duration
        t = Clamp(t, 0.0, 1.0)
        
        // 상태 보간
        interpolated = InterpolateSnapshots(from, to, t)
        return interpolated
    
    function InterpolateSnapshots(from, to, t):
        result = new WorldSnapshot()
        result.tick = Lerp(from.tick, to.tick, t)
        result.timestamp = Lerp(from.timestamp, to.timestamp, t)
        
        // 각 플레이어 보간
        foreach toPlayer in to.players:
            fromPlayer = from.FindPlayer(toPlayer.playerID)
            
            if fromPlayer != null:
                interpolatedPlayer = new PlayerSnapshot()
                interpolatedPlayer.playerID = toPlayer.playerID
                interpolatedPlayer.position = Lerp(fromPlayer.position, toPlayer.position, t)
                interpolatedPlayer.rotation = Slerp(fromPlayer.rotation, toPlayer.rotation, t)
                interpolatedPlayer.velocity = Lerp(fromPlayer.velocity, toPlayer.velocity, t)
                
                // 이산적 값은 보간하지 않음
                interpolatedPlayer.health = toPlayer.health
                interpolatedPlayer.animationState = toPlayer.animationState
                
                result.players.Add(interpolatedPlayer)
            else:
                // 새로 나타난 플레이어는 그대로 사용
                result.players.Add(toPlayer)
        
        return result
```

**에르미트 보간(Hermite Interpolation):**

단순 선형 보간보다 부드러운 움직임을 위해 속도 정보를 활용한 에르미트 보간을 사용할 수 있다.

```pseudocode
class HermiteInterpolation:
    function InterpolatePosition(p0, v0, p1, v1, t):
        // p0, p1: 위치, v0, v1: 속도
        // 에르미트 기저 함수
        h00 = 2*t*t*t - 3*t*t + 1
        h10 = t*t*t - 2*t*t + t
        h01 = -2*t*t*t + 3*t*t
        h11 = t*t*t - t*t
        
        // 에르미트 곡선
        result = h00 * p0 + h10 * v0 + h01 * p1 + h11 * v1
        
        return result

// 사용 예
function InterpolatePlayer(fromPlayer, toPlayer, t):
    interpolated = new PlayerSnapshot()
    
    // 에르미트 보간으로 위치 계산
    interpolated.position = HermiteInterpolation.InterpolatePosition(
        fromPlayer.position,
        fromPlayer.velocity,
        toPlayer.position,
        toPlayer.velocity,
        t
    )
    
    return interpolated
```

### 시간 되감기(Rewinding)

시간 되감기는 과거의 게임 상태를 재구성하는 기술이다. 주로 히트 검증에 사용된다.

**스냅샷 히스토리 관리:**

```pseudocode
class SnapshotHistory:
    maxHistoryDuration = 1.0  // 1초간 보관
    snapshots = List<WorldSnapshot>
    
    function AddSnapshot(snapshot):
        snapshots.Add(snapshot)
        
        // 오래된 스냅샷 제거
        currentTime = GetCurrentTime()
        snapshots.RemoveWhere(s => currentTime - s.timestamp > maxHistoryDuration)
    
    function GetSnapshotAtTime(timestamp):
        // 정확한 시간의 스냅샷 찾기
        for i in range(snapshots.Count - 1):
            snap1 = snapshots[i]
            snap2 = snapshots[i + 1]
            
            if snap1.timestamp <= timestamp and snap2.timestamp >= timestamp:
                // 보간하여 정확한 시간 재구성
                t = (timestamp - snap1.timestamp) / (snap2.timestamp - snap1.timestamp)
                return InterpolateSnapshots(snap1, snap2, t)
        
        return null
    
    function GetSnapshotAtTick(tick):
        foreach snapshot in snapshots:
            if snapshot.tick == tick:
                return snapshot
        
        return null
```

**시간 되감기 적용:**

```pseudocode
class TimeRewind:
    snapshotHistory: SnapshotHistory
    
    function ValidateHit(shooterID, targetID, shootTime):
        // 슈터의 시간대로 되감기
        shooter = GetPlayer(shooterID)
        shooterLatency = shooter.GetAverageLatency()
        
        // 슈터가 본 시간
        rewindTime = shootTime - shooterLatency
        
        // 해당 시간의 게임 상태 복원
        historicalSnapshot = snapshotHistory.GetSnapshotAtTime(rewindTime)
        
        if historicalSnapshot == null:
            // 너무 오래된 샷, 거부
            return false
        
        // 그 시점의 타겟 위치
        historicalTarget = historicalSnapshot.FindPlayer(targetID)
        
        if historicalTarget == null:
            return false
        
        // 히트 검사
        isHit = CheckHit(shooter.aimDirection, historicalTarget.position)
        
        return isHit
```

## 5.3 랙 보상(Lag Compensation)

랙 보상은 네트워크 지연이 있어도 플레이어가 공정하고 반응적인 게임 경험을 할 수 있도록 하는 기술이다.

### 서버 측 히트 검증

클라이언트는 자신이 본 화면에서 적을 맞췄다고 보고하고, 서버는 시간을 되감아서 검증한다.

**히트 검증 시스템:**

```pseudocode
class HitValidation:
    snapshotHistory: SnapshotHistory
    maxRewindTime = 200  // 최대 200ms까지 되감기
    
    function OnHitReport(hitData):
        shooterID = hitData.shooterID
        targetID = hitData.targetID
        hitPosition = hitData.position
        clientTimestamp = hitData.timestamp
        
        // 슈터의 RTT 조회
        shooter = GetPlayer(shooterID)
        rtt = shooter.GetRoundTripTime()
        
        // 되감기 시간 계산 (클라이언트 -> 서버 지연)
        rewindTime = rtt / 2.0
        
        if rewindTime > maxRewindTime:
            // 지연이 너무 크면 거부
            return RejectHit("Latency too high")
        
        // 서버 시간 기준 계산
        serverTime = GetCurrentTime()
        rewindTimestamp = serverTime - rewindTime
        
        // 해당 시점의 스냅샷 복원
        historicalState = snapshotHistory.GetSnapshotAtTime(rewindTimestamp)
        
        if historicalState == null:
            return RejectHit("Historical state not available")
        
        // 타겟의 과거 위치
        historicalTarget = historicalState.FindPlayer(targetID)
        
        if historicalTarget == null:
            return RejectHit("Target not found")
        
        // 히트박스 검사
        isValidHit = ValidateHitbox(
            hitPosition,
            historicalTarget.position,
            historicalTarget.rotation,
            targetID.characterModel
        )
        
        if isValidHit:
            ApplyDamage(targetID, hitData.damage)
            BroadcastHitConfirmation(hitData)
            return AcceptHit()
        else:
            return RejectHit("Hit validation failed")
    
    function ValidateHitbox(hitPos, targetPos, targetRot, model):
        // 타겟의 히트박스 재구성
        hitboxes = model.GetHitboxes(targetPos, targetRot)
        
        foreach hitbox in hitboxes:
            if hitbox.Contains(hitPos):
                return true
        
        return false
```

**레이캐스트 검증:**

히트스캔 무기의 경우 과거 시점에서 레이캐스트를 수행한다.

```pseudocode
class RaycastValidation:
    function ValidateRaycast(shooterID, shootDirection, shootTime):
        // 슈터의 지연 시간만큼 되감기
        shooter = GetPlayer(shooterID)
        rewindTime = shootTime - (shooter.GetRTT() / 2.0)
        
        // 과거 상태 복원
        historicalState = snapshotHistory.GetSnapshotAtTime(rewindTime)
        
        // 슈터의 과거 위치
        historicalShooter = historicalState.FindPlayer(shooterID)
        shootOrigin = historicalShooter.position + historicalShooter.eyeOffset
        
        // 레이캐스트 수행
        hitResult = Raycast(shootOrigin, shootDirection, maxRange)
        
        if hitResult.hit:
            // 맞은 대상이 과거 시점의 플레이어인지 확인
            foreach player in historicalState.players:
                if player.collider.Contains(hitResult.point):
                    return HitResult(true, player.playerID, hitResult.point)
        
        return HitResult(false, null, null)
```

### 타임스탬프 활용

모든 클라이언트 입력과 서버 이벤트에 타임스탬프를 부여하여 정확한 시간 기준을 유지한다.

**타임스탬프 관리:**

```pseudocode
class TimestampManager:
    serverTimeOffset = 0.0  // 서버와 클라이언트 시간 차이
    
    function GetServerTime():
        return GetLocalTime() + serverTimeOffset
    
    function SynchronizeTime(serverTimestamp, requestTime):
        responseTime = GetLocalTime()
        rtt = responseTime - requestTime
        
        // 왕복 시간의 절반을 서버 시간에 더함
        estimatedServerTime = serverTimestamp + (rtt / 2.0)
        
        localTime = GetLocalTime()
        newOffset = estimatedServerTime - localTime
        
        // 점진적으로 오프셋 조정
        serverTimeOffset = Lerp(serverTimeOffset, newOffset, 0.1)
    
    function CreateTimestampedInput(input):
        packet = new InputPacket()
        packet.input = input
        packet.clientTime = GetLocalTime()
        packet.serverTime = GetServerTime()
        packet.sequenceNumber = GetNextSequence()
        
        return packet

// 서버에서 타임스탬프 검증
class ServerTimestampValidation:
    function ValidateInputTiming(input, receivedTime):
        expectedReceiveTime = input.serverTime + (input.sender.GetRTT() / 2.0)
        timeDifference = Abs(receivedTime - expectedReceiveTime)
        
        if timeDifference > acceptableThreshold:
            // 타이밍이 의심스러움 (스피드 핵 가능성)
            LogSuspiciousActivity(input.sender)
            return false
        
        return true
```

### 공정성과 반응성의 균형

랙 보상은 공정성과 반응성 사이의 균형이 필요하다. 지연이 큰 플레이어에게 너무 많은 보상을 주면 다른 플레이어가 불이익을 받는다.

**적응형 랙 보상:**

```pseudocode
class AdaptiveLagCompensation:
    maxCompensation = 200  // 최대 200ms 보상
    minPing = 20
    maxPing = 150
    
    function CalculateCompensation(playerLatency):
        // 지연이 낮으면 전체 보상
        if playerLatency < minPing:
            return playerLatency
        
        // 지연이 높으면 부분 보상
        if playerLatency > maxPing:
            compensationRatio = 0.5  // 50%만 보상
            return Min(playerLatency * compensationRatio, maxCompensation)
        
        // 중간 지연은 점진적 보상
        ratio = (playerLatency - minPing) / (maxPing - minPing)
        compensationRatio = Lerp(1.0, 0.5, ratio)
        
        return playerLatency * compensationRatio
    
    function ValidateWithFairness(hitData):
        shooter = GetPlayer(hitData.shooterID)
        target = GetPlayer(hitData.targetID)
        
        // 슈터의 보상 시간
        shooterCompensation = CalculateCompensation(shooter.GetLatency())
        
        // 타겟의 현재 위치와 과거 위치 비교
        rewindTime = GetCurrentTime() - shooterCompensation
        historicalTarget = snapshotHistory.GetPlayerAtTime(target.id, rewindTime)
        currentTarget = GetCurrentPlayerState(target.id)
        
        movementDistance = Distance(historicalTarget.position, currentTarget.position)
        
        // 타겟이 크게 움직였다면 공정성 고려
        if movementDistance > fairnessThreshold:
            // 중간 지점에서 검증 (타겟에게도 기회 제공)
            adjustedTime = rewindTime + (GetCurrentTime() - rewindTime) * 0.3
            adjustedTarget = snapshotHistory.GetPlayerAtTime(target.id, adjustedTime)
            return ValidateHitbox(hitData.position, adjustedTarget)
        
        return ValidateHitbox(hitData.position, historicalTarget)
```

**플레이어 피드백:**

랙 보상으로 인한 이상한 경험을 최소화하기 위해 적절한 피드백을 제공한다.

```pseudocode
class LagCompensationFeedback:
    function OnHitRegistered(shooterID, targetID, isCompensated):
        if isCompensated:
            // 보상된 히트는 특별한 이펙트로 표시
            PlayHitEffect(targetID, "compensated_hit")
        else:
            PlayHitEffect(targetID, "normal_hit")
        
        // 슈터에게 즉시 피드백
        SendHitConfirmation(shooterID, targetID)
    
    function OnTakingCompensatedDamage(targetID, damageAmount):
        target = GetPlayer(targetID)
        
        // 타겟에게 설명 제공
        ShowDamageIndicator(targetID, damageAmount)
        
        // 킬캠에서 랙 보상 상황 표시
        if target.health <= 0:
            RecordKillcamWithLagInfo(targetID)
```

**지역별 서버 선택:**

최적의 경험을 위해 플레이어를 낮은 지연의 서버로 매칭한다.

```pseudocode
class ServerMatchmaking:
    function FindBestServer(player):
        playerLocation = player.GetLocation()
        availableServers = GetAvailableServers()
        
        bestServer = null
        lowestPing = Infinity
        
        foreach server in availableServers:
            ping = EstimatePing(playerLocation, server.location)
            
            if ping < lowestPing and server.hasSpace:
                lowestPing = ping
                bestServer = server
        
        // 핑이 너무 높으면 경고
        if lowestPing > 100:
            WarnPlayerAboutHighPing(player, lowestPing)
        
        return bestServer
    
    function BalanceTeamsByPing(players):
        // 팀을 핑 기준으로 균형있게 분배
        sortedPlayers = players.SortByPing()
        
        team1 = []
        team2 = []
        team1Ping = 0
        team2Ping = 0
        
        foreach player in sortedPlayers:
            if team1Ping <= team2Ping:
                team1.Add(player)
                team1Ping += player.averagePing
            else:
                team2.Add(player)
                team2Ping += player.averagePing
        
        return (team1, team2)
```

실시간 게임플레이 구현은 온라인 게임 개발에서 가장 복잡한 부분 중 하나다. 틱 시스템, 스냅샷, 랙 보상 등의 기술을 조합하여 네트워크 지연이 있어도 공정하고 반응적인 게임 경험을 만들어야 한다. 각 게임의 장르와 요구사항에 맞게 이러한 기술들을 조정하고 최적화하는 것이 클라이언트 개발자의 중요한 역할이다.


# 6. 장르별 네트워크 특성

게임 장르마다 요구되는 네트워크 특성이 크게 다르다. FPS는 밀리초 단위의 반응속도가 중요하고, MMORPG는 수천 명의 동시 접속자를 관리해야 하며, 카드 게임은 정확한 상태 검증이 핵심이다. 클라이언트 개발자는 각 장르의 특성을 이해하고 그에 맞는 네트워크 코드를 작성해야 한다.

## 6.1 FPS/TPS 게임

### 빠른 반응 속도 요구사항

FPS/TPS 게임은 모든 게임 장르 중 가장 낮은 지연시간을 요구한다. 플레이어가 총을 쏘거나 움직일 때, 그 결과가 50ms 이내에 화면에 반영되어야 자연스러운 게임플레이가 가능하다.

**클라이언트 측 예측의 적극 활용**

```pseudo
function OnPlayerInput(input):
    // 즉시 로컬에서 예측 실행
    predictedPosition = LocalSimulate(input)
    player.position = predictedPosition
    
    // 서버에 입력 전송 (타임스탬프 포함)
    packet = {
        inputType: input.type,
        timestamp: currentTime,
        sequenceNumber: nextSequence++
    }
    SendToServer(packet)
    
    // 예측 히스토리 저장 (서버 응답 시 검증용)
    predictionHistory.Add(sequenceNumber, predictedPosition)
```

클라이언트는 서버의 응답을 기다리지 않고 즉시 플레이어의 움직임을 화면에 표시한다. 서버로부터 실제 결과를 받으면 예측이 틀렸는지 확인하고 필요시 보정한다.

**높은 업데이트 빈도**

FPS 게임은 일반적으로 초당 20~60번의 상태 업데이트를 전송한다. 이는 다른 장르에 비해 매우 높은 빈도다.

```pseudo
const UPDATE_RATE = 30  // 초당 30번 업데이트

function NetworkUpdate():
    if (currentTime - lastUpdateTime) >= (1000 / UPDATE_RATE):
        SendPlayerState(position, rotation, velocity)
        lastUpdateTime = currentTime
```

### 히트스캔과 발사체 동기화

**즉시 적용 무기 (히트스캔)**

레이저나 권총 같은 즉시 타격 무기는 클라이언트에서 먼저 히트 판정을 표시하고, 서버에서 최종 검증한다.

```pseudo
function FireHitscanWeapon():
    // 클라이언트에서 즉시 레이캐스트
    ray = CreateRay(weaponPosition, aimDirection)
    hit = LocalRaycast(ray)
    
    if hit:
        // 즉시 시각 효과 표시
        ShowHitEffect(hit.position)
        ShowBloodEffect(hit.target)
    
    // 서버에 발사 정보 전송
    packet = {
        weaponId: currentWeapon,
        shootPosition: weaponPosition,
        shootDirection: aimDirection,
        timestamp: currentTime
    }
    SendToServer(packet)
```

서버는 랙 보상을 적용하여 과거 시점의 게임 상태를 재현하고 히트 여부를 검증한다.

**물리 기반 발사체**

로켓이나 수류탄 같은 발사체는 클라이언트와 서버 양쪽에서 시뮬레이션한다.

```pseudo
function FireProjectile():
    // 로컬에서 발사체 생성 및 시뮬레이션
    projectile = CreateLocalProjectile(position, velocity)
    activeProjectiles.Add(projectile)
    
    // 서버에 발사 알림
    packet = {
        projectileType: grenadeType,
        spawnPosition: position,
        initialVelocity: velocity,
        timestamp: currentTime
    }
    SendToServer(packet)

function OnServerProjectileUpdate(serverData):
    // 서버 권한 데이터로 보정
    localProjectile = FindProjectile(serverData.id)
    
    // 오차가 크면 위치 보정
    if Distance(localProjectile.position, serverData.position) > threshold:
        localProjectile.position = Lerp(
            localProjectile.position,
            serverData.position,
            correctionSpeed
        )
```

### 시야 체크와 가시성

FPS 게임에서는 플레이어가 볼 수 없는 적의 정보를 클라이언트에 전송하지 않는다. 이는 월핵(wallhack) 치트를 방지하는 중요한 방법이다.

```pseudo
function OnServerVisibilityUpdate(visiblePlayers):
    // 서버에서 보낸 가시 플레이어 목록
    newVisibleSet = Set(visiblePlayers)
    
    // 새로 보이게 된 플레이어
    for player in newVisibleSet - currentVisibleSet:
        SpawnPlayerModel(player)
        StartReceivingUpdates(player.id)
    
    // 더 이상 안 보이는 플레이어
    for player in currentVisibleSet - newVisibleSet:
        HidePlayerModel(player)
        StopReceivingUpdates(player.id)
    
    currentVisibleSet = newVisibleSet
```

클라이언트는 서버가 가시성 판정을 한 플레이어들의 정보만 받는다. 벽 뒤의 적은 클라이언트 메모리에 존재하지 않는다.

## 6.2 MMORPG

### 대규모 플레이어 관리

MMORPG는 하나의 월드에 수백, 수천 명의 플레이어가 동시에 접속한다. 모든 플레이어의 정보를 모든 클라이언트에 전송하는 것은 불가능하다.

**관심 영역(Area of Interest) 관리**

클라이언트는 자신의 캐릭터 주변의 제한된 영역 내 정보만 받는다.

```pseudo
const INTEREST_RADIUS = 100  // 미터 단위

function OnPositionUpdate(myPosition):
    // 서버에 위치 전송
    SendToServer({
        position: myPosition,
        timestamp: currentTime
    })

function OnServerAreaUpdate(nearbyEntities):
    // 관심 영역 내 엔티티 정보 수신
    for entity in nearbyEntities:
        if entity.id in loadedEntities:
            UpdateEntity(entity)
        else:
            CreateEntity(entity)
    
    // 영역 밖으로 나간 엔티티 제거
    for entityId in loadedEntities:
        if entityId not in nearbyEntities:
            RemoveEntity(entityId)
```

**엔티티 우선순위**

모든 엔티티가 동일한 빈도로 업데이트되지 않는다. 거리와 중요도에 따라 업데이트 빈도를 조절한다.

```pseudo
function DetermineUpdatePriority(entity, myPosition):
    distance = Distance(entity.position, myPosition)
    
    if entity.type == PLAYER:
        priority = HIGH
    else if entity.type == IMPORTANT_NPC:
        priority = MEDIUM
    else:
        priority = LOW
    
    // 거리에 따른 우선순위 조정
    if distance < 20:
        return priority * 2
    else if distance < 50:
        return priority
    else:
        return priority / 2

function OnServerUpdate(entities):
    // 우선순위에 따라 업데이트 적용
    sortedEntities = SortByPriority(entities)
    
    for entity in sortedEntities:
        UpdateEntity(entity)
```

**점진적 로딩**

새로운 영역에 진입할 때 모든 데이터를 한 번에 받으면 렉이 발생한다. 데이터를 우선순위에 따라 나눠서 받는다.

```pseudo
function OnEnterNewZone(zoneId):
    // 1단계: 필수 데이터 (지형, 충돌)
    RequestZoneData(zoneId, PRIORITY_CRITICAL)
    
    // 2단계: 주요 오브젝트 (건물, NPC)
    RequestZoneData(zoneId, PRIORITY_HIGH)
    
    // 3단계: 장식 요소
    RequestZoneData(zoneId, PRIORITY_LOW)

function OnZoneDataReceived(data, priority):
    if priority == PRIORITY_CRITICAL:
        LoadTerrainImmediate(data)
        EnablePlayerMovement()
    else:
        AddToLoadingQueue(data, priority)
```

### 인스턴스와 채널링

**인스턴스 던전**

플레이어 그룹이 독립된 공간에서 플레이한다. 각 인스턴스는 별도의 서버 세션이다.

```pseudo
function JoinDungeon(dungeonId, partyMembers):
    // 서버에 인스턴스 생성 요청
    request = {
        dungeonId: dungeonId,
        partyMembers: partyMembers,
        difficulty: selectedDifficulty
    }
    
    response = SendRequestToServer(request)
    
    if response.success:
        instanceId = response.instanceId
        serverAddress = response.serverAddress
        
        // 기존 월드 서버에서 연결 해제
        DisconnectFromWorldServer()
        
        // 인스턴스 서버에 연결
        ConnectToInstanceServer(serverAddress, instanceId)
```

클라이언트는 월드 서버에서 인스턴스 서버로 연결을 전환한다. 로딩 화면 동안 이 전환이 일어난다.

**채널 시스템**

같은 지역이 혼잡할 때 플레이어를 여러 채널로 분산한다.

```pseudo
function OnChannelListReceived(channels):
    // 사용 가능한 채널 목록 표시
    for channel in channels:
        DisplayChannel(
            channelId: channel.id,
            population: channel.playerCount,
            status: channel.status  // NORMAL, CROWDED, FULL
        )

function ChangeChannel(targetChannelId):
    if currentChannel == targetChannelId:
        return
    
    // 채널 이동 요청
    ShowLoadingScreen()
    
    request = {
        currentChannel: currentChannel,
        targetChannel: targetChannelId
    }
    
    SendToServer(request)
    
    // 서버가 새 채널 데이터를 보내면
    OnChannelChangeComplete(newChannelData):
        HideLoadingScreen()
        UpdateWorldState(newChannelData)
```

## 6.3 전략/카드 게임

### 턴 기반 통신

전략 게임과 카드 게임은 실시간 반응이 덜 중요하다. 턴이 넘어갈 때만 통신하면 된다.

**턴 제출 시스템**

```pseudo
function SubmitTurn(actions):
    turnData = {
        turnNumber: currentTurn,
        actions: actions,
        checksum: CalculateChecksum(actions)
    }
    
    // 서버에 턴 제출
    SendToServer(turnData)
    
    // UI 잠금 (다른 플레이어 대기)
    LockPlayerInput()
    ShowWaitingIndicator()

function OnAllPlayersReady(turnResults):
    // 모든 플레이어가 턴을 제출하면 결과 수신
    UnlockPlayerInput()
    HideWaitingIndicator()
    
    // 턴 결과 애니메이션 재생
    PlayTurnResults(turnResults)
    
    currentTurn++
```

**비동기 턴 처리**

일부 게임은 플레이어가 동시에 턴을 제출하지 않아도 된다.

```pseudo
function OnPlayerAction(playerId, action):
    // 다른 플레이어의 행동을 실시간으로 받음
    if playerId != myPlayerId:
        AnimateAction(playerId, action)
    
    // 내 턴 타이머는 계속 진행
    UpdateMyTurnTimer()

function OnTurnTimeout():
    // 시간 내에 행동하지 않으면 자동 제출
    if not turnSubmitted:
        SubmitDefaultAction()
```

### 상태 검증

카드 게임에서는 게임 상태의 정확성이 매우 중요하다. 클라이언트와 서버의 상태가 일치해야 한다.

**체크섬 검증**

```pseudo
function CalculateGameStateChecksum():
    checksum = 0
    
    // 모든 중요 상태 값 포함
    checksum += Hash(playerHand)
    checksum += Hash(opponentHandCount)
    checksum += Hash(boardState)
    checksum += Hash(playerMana)
    checksum += Hash(playerHealth)
    
    return checksum

function OnServerStateUpdate(serverState):
    localChecksum = CalculateGameStateChecksum()
    
    if localChecksum != serverState.checksum:
        // 상태 불일치 - 서버 상태로 완전히 재동기화
        LogError("State mismatch detected")
        RequestFullStateSync()
```

**행동 검증**

클라이언트에서 가능하다고 표시된 행동이 서버에서 거부될 수 있다.

```pseudo
function PlayCard(cardId):
    // 로컬 검증
    if not CanPlayCard(cardId):
        ShowError("Cannot play this card")
        return
    
    // 낙관적 UI 업데이트 (임시)
    tempCard = RemoveCardFromHand(cardId)
    ShowCardAnimation(cardId)
    
    // 서버에 요청
    request = {
        action: PLAY_CARD,
        cardId: cardId,
        targetId: selectedTarget
    }
    
    response = SendRequestToServer(request)
    
    if response.success:
        // 서버 승인 - 변경사항 확정
        ConfirmCardPlayed(cardId)
    else:
        // 서버 거부 - 롤백
        RollbackCardToHand(tempCard)
        ShowError(response.errorMessage)
```

### 낮은 업데이트 빈도 활용

턴 기반 게임은 초당 수십 번 업데이트할 필요가 없다. 이는 네트워크 대역폭과 배터리를 절약한다.

```pseudo
const HEARTBEAT_INTERVAL = 5000  // 5초마다 연결 확인

function NetworkLoop():
    while gameRunning:
        // 턴 중에는 입력 이벤트만 전송
        if hasNewInput:
            SendInput()
            hasNewInput = false
        
        // 주기적 연결 상태 확인
        if (currentTime - lastHeartbeat) > HEARTBEAT_INTERVAL:
            SendHeartbeat()
            lastHeartbeat = currentTime
        
        Sleep(100)  // 100ms 대기
```

## 6.4 모바일 게임

### 불안정한 네트워크 환경 대응

모바일 디바이스는 Wi-Fi와 모바일 데이터를 오가며, 지하철이나 터널에서 연결이 끊긴다.

**적응적 품질 조정**

```pseudo
function MonitorNetworkQuality():
    recentLatencies = []
    recentPacketLosses = []
    
    // 지속적으로 네트워크 상태 측정
    while true:
        latency = MeasurePing()
        packetLoss = CalculatePacketLoss()
        
        recentLatencies.Add(latency)
        recentPacketLosses.Add(packetLoss)
        
        avgLatency = Average(recentLatencies)
        avgPacketLoss = Average(recentPacketLosses)
        
        // 네트워크 상태에 따라 설정 조정
        if avgLatency > 200 or avgPacketLoss > 5%:
            ReduceUpdateRate()
            DisableNonEssentialEffects()
            ShowPoorConnectionWarning()
        else if avgLatency < 100 and avgPacketLoss < 1%:
            RestoreNormalUpdateRate()
            EnableAllEffects()
            HidePoorConnectionWarning()
```

**네트워크 타입별 최적화**

```pseudo
function OnNetworkTypeChanged(networkType):
    if networkType == WIFI:
        // Wi-Fi: 높은 품질 사용
        SetUpdateRate(30)
        EnableHighResTextures()
        EnableVoiceChat()
        
    else if networkType == MOBILE_4G:
        // 4G: 중간 품질
        SetUpdateRate(20)
        EnableMediumResTextures()
        EnableVoiceChat()
        
    else if networkType == MOBILE_3G:
        // 3G: 낮은 품질
        SetUpdateRate(10)
        EnableLowResTextures()
        DisableVoiceChat()
        ShowDataUsageWarning()
```

### 배터리 및 데이터 사용량 고려

모바일 게임은 배터리 소모와 데이터 요금에 민감하다.

**백그라운드 모드 처리**

```pseudo
function OnApplicationPause():
    // 앱이 백그라운드로 가면
    ReduceNetworkActivity()
    
    // 중요한 업데이트만 받기
    SetMinimalUpdateMode()
    
    // 업데이트 간격 크게 늘림
    SetUpdateInterval(10000)  // 10초
    
    // 불필요한 연결 종료
    DisconnectFromVoiceChat()
    StopBackgroundMusic()

function OnApplicationResume():
    // 앱이 포그라운드로 돌아오면
    RestoreNormalNetworkActivity()
    SetUpdateInterval(100)  // 0.1초
    ReconnectToVoiceChat()
```

**데이터 압축**

```pseudo
function SendPlayerUpdate(position, state):
    // 부동소수점 위치를 정수로 압축
    compressedX = CompressFloat(position.x, 0.1)  // 10cm 정밀도
    compressedY = CompressFloat(position.y, 0.1)
    
    // 비트 플래그로 상태 압축
    stateFlags = 0
    if state.isRunning: stateFlags |= 0x01
    if state.isCrouching: stateFlags |= 0x02
    if state.isAiming: stateFlags |= 0x04
    
    packet = {
        x: compressedX,
        y: compressedY,
        flags: stateFlags
    }
    
    SendCompressedPacket(packet)

function CompressFloat(value, precision):
    // 부동소수점을 정수로 변환하여 바이트 절약
    return int(value / precision)
```

### 끊김 없는 재연결

모바일 환경에서 짧은 연결 끊김은 빈번하다. 플레이어가 이를 의식하지 못하도록 처리해야 한다.

**자동 재연결 시스템**

```pseudo
const MAX_RECONNECT_ATTEMPTS = 5
const RECONNECT_DELAY_BASE = 1000  // 1초

function OnConnectionLost():
    reconnectAttempt = 0
    
    // UI에 연결 끊김 표시 (즉시 게임 종료 안 함)
    ShowReconnectingIndicator()
    
    // 게임 상태는 유지하되 입력 무효화
    DisablePlayerInput()
    
    // 재연결 시도
    AttemptReconnect()

function AttemptReconnect():
    if reconnectAttempt >= MAX_RECONNECT_ATTEMPTS:
        // 재연결 실패
        ShowConnectionFailedDialog()
        return
    
    reconnectAttempt++
    delay = RECONNECT_DELAY_BASE * (2 ^ reconnectAttempt)  // 지수 백오프
    
    ShowReconnectAttempt(reconnectAttempt, MAX_RECONNECT_ATTEMPTS)
    
    WaitFor(delay)
    
    success = TryConnect()
    
    if success:
        OnReconnectSuccess()
    else:
        AttemptReconnect()

function OnReconnectSuccess():
    HideReconnectingIndicator()
    
    // 서버에서 현재 게임 상태 받기
    gameState = RequestCurrentGameState()
    
    // 로컬 상태와 서버 상태 동기화
    SyncLocalStateWithServer(gameState)
    
    EnablePlayerInput()
    
    ShowNotification("연결이 복구되었다")
```

**연결 품질 예측**

```pseudo
function PredictConnectionStability():
    // 최근 연결 기록 분석
    connectionHistory = GetRecentConnectionHistory()
    
    disconnectCount = CountDisconnects(connectionHistory, last5Minutes)
    avgLatency = CalculateAverageLatency(connectionHistory)
    
    if disconnectCount > 3:
        // 연결이 매우 불안정
        SuggestOfflineMode()
        return UNSTABLE
    else if avgLatency > 300:
        // 연결이 느림
        WarnHighLatency()
        return SLOW
    else:
        return STABLE
```

**세션 지속성**

```pseudo
function MaintainSession():
    // 짧은 연결 끊김 후에도 게임 세션 유지
    sessionToken = GetSessionToken()
    
    reconnectData = {
        sessionToken: sessionToken,
        lastKnownState: currentGameState,
        timestamp: lastUpdateTime
    }
    
    response = SendReconnectRequest(reconnectData)
    
    if response.sessionValid:
        // 세션이 유효함 - 이어서 플레이
        ContinueFromLastState(response.currentState)
    else:
        // 세션 만료 - 새로 시작
        ShowSessionExpiredMessage()
        RestartGame()
```

장르별 네트워크 특성을 이해하면 각 게임 타입에 최적화된 클라이언트를 개발할 수 있다. FPS는 즉각적인 반응성을, MMORPG는 대규모 관리를, 턴제 게임은 정확성을, 모바일 게임은 안정성과 효율성을 우선시한다. 클라이언트 개발자는 자신이 만드는 게임의 장르 특성에 맞는 네트워크 전략을 선택해야 한다.
  