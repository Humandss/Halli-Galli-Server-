using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections;
using UnityEngine;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using static TCPClient;
using JetBrains.Annotations;
using System.Linq;
using System.Collections.Generic;
using static Card;


public class TCPClient : MonoBehaviour
{
    public string serverIP = "127.0.0.1";
    public int port = 50400;
    public string clientID = "Player";

    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool running = false;


    //패킷 타입
    public enum PacketType: ushort
    {
        LOGIN = 1,
        LOGIN_RESULT = 2,
        START_GAME_REQ = 3,
        START_GAME_ACK = 4,
        CLICK_BELL_REQ = 5,
        CLICK_BELL_ACK = 6,
        GAME_OVER=7,
        WIN_GAME = 8,

        PLAYER_COUNT=9,
        CARD_DISTRIBUTE = 10,
        CARD_DRAW_REQ=11,
        CARD_DRAW_RESULT=12,
        ERROR_WARN = 99

    }
    //패킷 헤더 타입 정의(사이즈, 종류)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketHeader
    {
        public ushort size;
        public ushort type;
    }
    //플레이어 카운트 패킷
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PlayerCountPacket
    {
        public PacketHeader header;
        public int playerCount;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CardDistributePacket
    {
        public PacketHeader header;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] playerID;
        public int cardCount;
       
    }
    //로그인 req 패킷
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LoginReqPacket
    {
        public PacketHeader header;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] playerID;

    }
    //로그인 ack 패킷
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LoginAckPacket
    {
        public PacketHeader header;
        public int playerIndex;
        public bool success;
       
    }
    //게임 시작 req 패킷
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StartGameReqPacket
    {

        public PacketHeader header;

    }
    //게임 시작 ack 패킷
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StartGameAckPacket
    {
        public PacketHeader header;
 
    }
    //플레이어 Draw 패킷
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DrawCardReqPacket
    {
        public PacketHeader header;

    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DrawCardResultPacket
    {
        public PacketHeader header;
        public CardInfo card;
        public float posX;
        public float posY;
        public float posZ;
        public int index;
        public Vector3 GetPosition()
        {
            return new Vector3(posX, posY, posZ);
        }
    }
    //벨 눌렀을 때 서버로 전송할 패킷
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BellReqPacket
    {
        public PacketHeader header;
        public int playerIndex;
    }
    //벨 눌렀을 때 서버로한테 전송받을 패킷
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BellAckPacket
    {
        public PacketHeader header;
        public bool result; 
   
    }
    //게임 오버시 전송 받을 패킷
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GameOverPacket
    {
        public PacketHeader header;

    }
    //게임 승리시 전송 받을 패킷
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GameWinPacket
    {
        public PacketHeader header;

    }
    void Start()
    {
        ConnectToServer();

    }

    void OnDestroy()
    {
        Disconnect();
    }

    public void ConnectToServer()
    {
        try
        {
            client = new TcpClient();
            client.Connect(serverIP, port);
            stream = client.GetStream();
            running = true;

            // 서버에 ID 전송
           // SendMessageToServer(clientID);

            // 수신 스레드 시작
            receiveThread = new Thread(ReceiveLoop);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            Debug.Log("서버에 연결됨");
        }
        catch (Exception e)
        {
            Debug.LogError("서버 연결 실패: " + e.Message);
        }
    }

    //ReceiveLoop에서 UI 및 로그 처리는 메인 스레드에서 처리해줘야함.
    private void ReceiveLoop()
    {
        //디스페쳐가 Instance 제대로 됐는지 확인, Instance가 제대로 진행되지 않으면 예외처리
        try
        {
            var dispatcher = UnityMainThreadDispatcher.Instance();
            Debug.Log($"Dispatcher instance is {(dispatcher == null ? "null" : "not null")}");
        }
        catch (Exception ex)
        {
            Debug.LogError("Dispatcher 호출 중 예외 발생: " + ex.Message);
        }

        byte[] buffer = new byte[1024];
        while (running)
        {
            try
            {
                if (!stream.CanRead)
                {
                    running = false;
                    break;
                }
                int length = stream.Read(buffer, 0, buffer.Length);
                if (length == 0)
                {
                    Debug.Log("서버 연결 종료됨");
                    running = false;
                    break;
                }

                //header를 구조체로 변환하여 패킷 판별
                PacketHeader header = BytesToStruct<PacketHeader>(buffer);
                PacketType type = (PacketType)header.type;
                //패킷 종류에 따라 처리
                switch (type)
                {
                    //벨을 클릭했을 경우
                    case PacketType.CLICK_BELL_ACK:
                    {
                        BellAckPacket bellAck = BytesToStruct<BellAckPacket>(buffer);
                        bool isSuccess = bellAck.result;
                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                               GameManager.Instance.OnBellResult(isSuccess);
                        });
                            break;
                     }
                    
                    //로그인 결과
                    case PacketType.LOGIN_RESULT:
                    {
                           LoginAckPacket loginAck = BytesToStruct<LoginAckPacket>(buffer);
                           UnityMainThreadDispatcher.Instance().Enqueue(() =>
                           {
                            
                               Debug.Log("플레이어 인덱스 패킷 수신 완료! 로그인 상태 :"+ loginAck.success);
                               GameManager.Instance.LoginSuccess(loginAck.success,loginAck.playerIndex);
                               LoginManager.Instance.isLoginSuccess(loginAck.success);
                        
                           });
                           
                            break;

                    }
                    //플레이어 카운트 수신
                    case PacketType.PLAYER_COUNT:
                    {
                            PlayerCountPacket pCount=BytesToStruct<PlayerCountPacket>(buffer);
                            UnityMainThreadDispatcher.Instance().Enqueue(() =>
                            {
                                Debug.Log("플레이어 접속 확인! 현재 플레이어 수 :" + pCount.playerCount);
                                GameManager.Instance.GetPlayerCount(pCount.playerCount);
                             
                            });
                            break;

                    }
                    //게임 ack 수신
                    case PacketType.START_GAME_ACK:
                    {
                            StartGameAckPacket ack = BytesToStruct<StartGameAckPacket>(buffer);
                            UnityMainThreadDispatcher.Instance().Enqueue(() =>
                            {
                                Debug.Log("게임 ack 수신 완료!");
                                GameManager.Instance.GameStart(true);
                           
                            });
                            break;
                     }
                    case PacketType.CARD_DISTRIBUTE:
                     {
                           //패킷 헤더 부분 먼저 파싱
                            int headerSize = Marshal.SizeOf(typeof(CardDistributePacket));
                            CardDistributePacket cardHeader = BytesToStruct<CardDistributePacket>(buffer.Take(headerSize).ToArray());

                            // 카드 개수 및 카드 1개 크기 확인
                            int cardCount = cardHeader.cardCount;
                            int cardSize = Marshal.SizeOf(typeof(CardInfo));

                            //카드 데이터 배열 추출
                            CardInfo[] cards = new CardInfo[cardCount];
                            for (int i = 0; i < cardCount; i++)
                            {
                                byte[] cardBytes = new byte[cardSize];
                                Array.Copy(buffer, headerSize + i * cardSize, cardBytes, 0, cardSize);
                                cards[i] = BytesToStruct<CardInfo>(cardBytes);
                            }

                            // 메인 스레드에서 UI 및 게임 로직 처리
                            UnityMainThreadDispatcher.Instance().Enqueue(() =>
                            {
                                List<(Card.FruitType type, int count, int amount)> cardDataList = new List<(Card.FruitType, int, int)>();

                                foreach (var card in cards)
                                {
                                  
                                    cardDataList.Add((card.type, card.count, 1));
                                }
                                Debug.Log($"cardCount: {cardCount}, buffer length: {buffer.Length}");
                                GameManager.Instance.OnReceiveCardInfos(cardDataList);
                            });
                            break;
                     }

                    case PacketType.CARD_DRAW_RESULT:
                    {
                           
                            int size = Marshal.SizeOf(typeof(DrawCardResultPacket));
                            DrawCardResultPacket resultPacket = BytesToStruct<DrawCardResultPacket>(buffer.Take(size).ToArray());

                            Card.FruitType fruitType = resultPacket.card.type;
                            int count = resultPacket.card.count;
                            int playerIndex = resultPacket.index;
                            Vector3 spawnPosition=resultPacket.GetPosition();

                            // 메인 스레드에서 UI 및 게임 로직 처리
                            UnityMainThreadDispatcher.Instance().Enqueue(() =>
                            {
                                Debug.Log($"카드 draw 패킷 게임 메니저에 전송 완료! 카드 타입 : {fruitType}, 카드 갯수 : {count}");
                                GameManager.Instance.OnCardDrawResult(fruitType, spawnPosition, count, playerIndex);
                               
                            });

                            break;
                    }
                    case PacketType.GAME_OVER:
                    {
                            GameOverPacket pkt = BytesToStruct<GameOverPacket>(buffer);
                            // 메인 스레드에서 UI 및 게임 로직 처리
                            UnityMainThreadDispatcher.Instance().Enqueue(() =>
                            {
                                GameManager.Instance.GameOver();
                            });
                            break;
                    }
                    case PacketType.WIN_GAME:
                    {
                            GameWinPacket pkt = BytesToStruct<GameWinPacket>(buffer);
                            // 메인 스레드에서 UI 및 게임 로직 처리
                            UnityMainThreadDispatcher.Instance().Enqueue(() =>
                            {
                                GameManager.Instance.WinGame();
                            });
                            break;
                    }

                    default:
                        Debug.LogWarning("알 수 없는 패킷 타입: " + type);
                        break;
                }
            }
            catch (Exception e)
            {
                string errorMessage = "수신 오류: " + e.Message;
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    Debug.LogError(errorMessage); //  메인 스레드에서 실행
                });
                running = false;
            }
        }
    }

    public void Disconnect()
    {
        running = false;
        receiveThread?.Join();

        stream?.Close();
        client?.Close();

        Debug.Log("클라이언트 종료");
    }
    //게임 시작 req 패킷 전송
    public void SendStartReqPacket()
    {
        StartGameReqPacket packet = new StartGameReqPacket();
        packet.header.size=(ushort)Marshal.SizeOf(typeof(StartGameReqPacket));
        packet.header.type = (ushort)PacketType.START_GAME_REQ;
  
        byte[] data = StructToBytes(packet);
        SendBytes(data);
    }
    //벨을 눌렀을 때 서버에 처리 요쳥
    public void SendBellReqPacket(int playerIndex)
    {

        BellReqPacket packet = new BellReqPacket();
        packet.header.size = (ushort)Marshal.SizeOf(typeof(BellReqPacket));
        packet.header.type = (ushort)PacketType.CLICK_BELL_REQ; 
        packet.playerIndex = playerIndex;
        

        byte[] data = StructToBytes(packet);
        SendBytes(data);
    }
    //로그인 요청 패킷
    public void SendLoginReqPacket(string playerID)
    {

        LoginReqPacket loginPacket = new LoginReqPacket();
        loginPacket.header.size = (ushort)Marshal.SizeOf(typeof(LoginReqPacket));
        loginPacket.header.type = (ushort)PacketType.LOGIN;
        loginPacket.playerID = new byte[256];

        //문자열을 바이트로 변환 (UTF8)
        byte[] idBytes = Encoding.UTF8.GetBytes(playerID);

        //최대 255까지만 복사 (맨 뒤에 \0을 위한 자리)
        int copyLength = Math.Min(idBytes.Length, 255);
        Array.Copy(idBytes, loginPacket.playerID, copyLength);

        // 마지막에 null terminator 삽입 => 쓰레기 값 방지를 위함.
        loginPacket.playerID[copyLength] = 0;

        byte[] data = StructToBytes(loginPacket);
        SendBytes(data);
    }
    //카드 draw 요청 패킷
    public void SendDrawCardReqPacket()
    {
        DrawCardReqPacket drawCardPacket = new DrawCardReqPacket();
        drawCardPacket.header.size=(ushort)Marshal.SizeOf(typeof(DrawCardReqPacket));
        drawCardPacket.header.type = (ushort)PacketType.CARD_DRAW_REQ;

        byte[] data = StructToBytes(drawCardPacket);
        SendBytes(data);
    }
    //바이트 배열을 네트워크로 보내는 함수
    private void SendBytes(byte[] data)
    {
        try
        {
            if (client != null && client.Connected)
            {
                stream.Write(data, 0, data.Length);
                stream.Flush();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("전송 오류: " + e.Message);
        }
    }
    //구조체를 바이트 배열로 변환하는 함수(직렬화)
    public static byte[] StructToBytes<T>(T obj) where T : struct
    {
        int size = Marshal.SizeOf(obj);
        byte[] bytes = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(obj, ptr, true);
        Marshal.Copy(ptr, bytes, 0, size);
        Marshal.FreeHGlobal(ptr);
        return bytes;
    }
    //바이트를 구조체 배열로 변환하는 함수(역직렬화)
    public static T BytesToStruct<T>(byte[] bytes) where T : struct
    {
        int size = Marshal.SizeOf(typeof(T));
        if (bytes.Length < size)
            throw new ArgumentException("바이트 배열 크기가 구조체 크기보다 작습니다.");

        IntPtr ptr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.Copy(bytes, 0, ptr, size);
            T obj = Marshal.PtrToStructure<T>(ptr);
            return obj;
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

}
