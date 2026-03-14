using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Linq;
using System.Collections.Generic;

public class TCPClient : MonoBehaviour
{
    public static TCPClient Instance { get; private set; }

    public string serverIP = "127.0.0.1";
    public int port = 50400;
    public string clientID = "Player";

    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool running = false;
    private readonly List<byte> receiveBuffer = new List<byte>();
    private readonly object streamLock = new object();

    public int AssignedPlayerIndex { get; private set; } = -1;
    public int LastKnownPlayerCount { get; private set; } = 0;
    public int LastKnownReadyPlayerCount { get; private set; } = 0;
    public int LastKnownCurrentTurnPlayerIndex { get; private set; } = -1;
    public string LastKnownCurrentTurnPlayerId { get; private set; } = string.Empty;

    public enum PacketType : ushort
    {
        LOGIN = 1,
        LOGIN_RESULT = 2,
        START_GAME_REQ = 3,
        START_GAME_ACK = 4,
        CLICK_BELL_REQ = 5,
        CLICK_BELL_ACK = 6,
        GAME_OVER = 7,
        WIN_GAME = 8,
        PLAYER_COUNT = 9,
        CARD_DISTRIBUTE = 10,
        CARD_DRAW_REQ = 11,
        CARD_DRAW_RESULT = 12,
        READY_COUNT = 13,
        MY_CARD_COUNT = 14,
        PLAYER_NAME_LAYOUT = 15,
        CURRENT_TURN = 16,
        TABLE_STATE_SYNC = 17,
        ERROR_WARN = 99
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketHeader
    {
        public ushort size;
        public ushort type;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PlayerCountPacket
    {
        public PacketHeader header;
        public int playerCount;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ReadyCountPacket
    {
        public PacketHeader header;
        public int readyCount;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MyCardCountPacket
    {
        public PacketHeader header;
        public int remainingCardCount;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PlayerNameLayoutPacket
    {
        public PacketHeader header;
        public int playerCount;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.ByValArray, SizeConst = 4)]
        public FixedPlayerId[] playerIDs;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CurrentTurnPacket
    {
        public PacketHeader header;
        public int playerIndex;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] playerID;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TableStateSyncPacket
    {
        public PacketHeader header;
        public int visibleCardCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public CardInfo[] cards;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] posX;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] posY;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] posZ;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public int[] playerIndices;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FixedPlayerId
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] bytes;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CardDistributePacket
    {
        public PacketHeader header;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] playerID;
        public int cardCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
        public CardInfo[] cards;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LoginReqPacket
    {
        public PacketHeader header;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] playerID;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LoginAckPacket
    {
        public PacketHeader header;
        public int playerIndex;
        [MarshalAs(UnmanagedType.I1)]
        public bool success;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StartGameReqPacket
    {
        public PacketHeader header;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StartGameAckPacket
    {
        public PacketHeader header;
    }

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

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BellReqPacket
    {
        public PacketHeader header;
        public int playerIndex;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BellAckPacket
    {
        public PacketHeader header;
        [MarshalAs(UnmanagedType.I1)]
        public bool result;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GameOverPacket
    {
        public PacketHeader header;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GameWinPacket
    {
        public PacketHeader header;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        UnityMainThreadDispatcher.EnsureInstance();
        ConnectToServer();
    }

    void OnDestroy()
    {
        Disconnect();
    }

    public void ConnectToServer()
    {
        if (client != null && client.Connected)
        {
            return;
        }

        try
        {
            client = new TcpClient();
            client.Connect(serverIP, port);
            stream = client.GetStream();
            running = true;

            receiveThread = new Thread(ReceiveLoop);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            Debug.Log("Connected to server.");
        }
        catch (Exception e)
        {
            Debug.LogError("Server connection failed: " + e.Message);
        }
    }

    private void ReceiveLoop()
    {
        try
        {
            UnityMainThreadDispatcher.EnsureInstance();
        }
        catch (Exception ex)
        {
            Debug.LogError("Dispatcher init failed: " + ex.Message);
        }

        byte[] buffer = new byte[1024];
        while (running)
        {
            try
            {
                if (stream == null || !stream.CanRead)
                {
                    running = false;
                    break;
                }

                int length = stream.Read(buffer, 0, buffer.Length);
                if (length == 0)
                {
                    Debug.Log("Server connection closed.");
                    running = false;
                    break;
                }

                lock (receiveBuffer)
                {
                    receiveBuffer.AddRange(buffer.Take(length));
                }

                ProcessReceivedPackets();
            }
            catch (Exception e)
            {
                string errorMessage = "Receive error: " + e.Message;
                UnityMainThreadDispatcher.Instance()?.Enqueue(() =>
                {
                    Debug.LogError(errorMessage);
                });
                running = false;
            }
        }
    }

    private void ProcessReceivedPackets()
    {
        int headerSize = Marshal.SizeOf(typeof(PacketHeader));

        while (running)
        {
            byte[] packetBytes;
            PacketHeader header;

            lock (receiveBuffer)
            {
                if (receiveBuffer.Count < headerSize)
                {
                    return;
                }

                header = BytesToStruct<PacketHeader>(receiveBuffer.Take(headerSize).ToArray());
                if (header.size < headerSize)
                {
                    throw new InvalidOperationException($"Invalid packet size: {header.size}");
                }

                if (receiveBuffer.Count < header.size)
                {
                    return;
                }

                packetBytes = receiveBuffer.Take(header.size).ToArray();
                receiveBuffer.RemoveRange(0, header.size);
            }

            HandlePacket(packetBytes);
        }
    }

    private void HandlePacket(byte[] packetBytes)
    {
        PacketHeader header = BytesToStruct<PacketHeader>(packetBytes);
        PacketType type = (PacketType)header.type;

        switch (type)
        {
            case PacketType.CLICK_BELL_ACK:
            {
                BellAckPacket bellAck = BytesToStruct<BellAckPacket>(packetBytes);
                bool isSuccess = bellAck.result;
                UnityMainThreadDispatcher.Instance()?.Enqueue(() =>
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.OnBellResult(isSuccess);
                    }
                });
                break;
            }

            case PacketType.LOGIN_RESULT:
            {
                LoginAckPacket loginAck = BytesToStruct<LoginAckPacket>(packetBytes);
                AssignedPlayerIndex = loginAck.success ? loginAck.playerIndex : -1;
                UnityMainThreadDispatcher.Instance()?.Enqueue(() =>
                {
                    Debug.Log("Login result received: " + loginAck.success);

                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.LoginSuccess(loginAck.success, loginAck.playerIndex);
                    }

                    if (LoginManager.Instance != null)
                    {
                        LoginManager.Instance.isLoginSuccess(loginAck.success);
                    }
                });
                break;
            }

            case PacketType.PLAYER_COUNT:
            {
                PlayerCountPacket pCount = BytesToStruct<PlayerCountPacket>(packetBytes);
                int playerCount = pCount.playerCount;
                LastKnownPlayerCount = playerCount;
                UnityMainThreadDispatcher.Instance()?.Enqueue(() =>
                {
                    Debug.Log($"Player count updated: {playerCount}");
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.GetPlayerCount(playerCount);
                    }
                });
                break;
            }

            case PacketType.READY_COUNT:
            {
                ReadyCountPacket readyPacket = BytesToStruct<ReadyCountPacket>(packetBytes);
                int readyCount = readyPacket.readyCount;
                LastKnownReadyPlayerCount = readyCount;
                UnityMainThreadDispatcher.Instance()?.Enqueue(() =>
                {
                    Debug.Log($"Ready count updated: {readyCount}");
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.GetReadyPlayerCount(readyCount);
                    }
                });
                break;
            }

            case PacketType.MY_CARD_COUNT:
            {
                MyCardCountPacket myCardCountPacket = BytesToStruct<MyCardCountPacket>(packetBytes);
                int remainingCardCount = myCardCountPacket.remainingCardCount;
                UnityMainThreadDispatcher.Instance()?.Enqueue(() =>
                {
                    Debug.Log($"My remaining cards updated: {remainingCardCount}");
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.GetMyRemainingCardCount(remainingCardCount);
                    }
                });
                break;
            }

            case PacketType.PLAYER_NAME_LAYOUT:
            {
                PlayerNameLayoutPacket layoutPacket = BytesToStruct<PlayerNameLayoutPacket>(packetBytes);
                List<string> playerNames = new List<string>();
                int nameCount = Math.Min(layoutPacket.playerCount, layoutPacket.playerIDs?.Length ?? 0);

                for (int i = 0; i < nameCount; i++)
                {
                    byte[] rawName = layoutPacket.playerIDs[i].bytes ?? Array.Empty<byte>();
                    int nullIndex = Array.IndexOf(rawName, (byte)0);
                    int actualLength = nullIndex >= 0 ? nullIndex : rawName.Length;
                    string playerName = Encoding.UTF8.GetString(rawName, 0, actualLength).Trim();
                    if (!string.IsNullOrEmpty(playerName))
                    {
                        playerNames.Add(playerName);
                    }
                }

                UnityMainThreadDispatcher.Instance()?.Enqueue(() =>
                {
                    Debug.Log("Player name layout updated.");
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.SetPlayerNames(playerNames);
                    }
                });
                break;
            }

            case PacketType.CURRENT_TURN:
            {
                CurrentTurnPacket currentTurnPacket = BytesToStruct<CurrentTurnPacket>(packetBytes);
                int currentTurnPlayerIndex = currentTurnPacket.playerIndex;
                byte[] rawCurrentTurnName = currentTurnPacket.playerID ?? Array.Empty<byte>();
                int nullIndex = Array.IndexOf(rawCurrentTurnName, (byte)0);
                int actualLength = nullIndex >= 0 ? nullIndex : rawCurrentTurnName.Length;
                string currentTurnPlayerId = Encoding.UTF8.GetString(rawCurrentTurnName, 0, actualLength).Trim();
                LastKnownCurrentTurnPlayerIndex = currentTurnPlayerIndex;
                LastKnownCurrentTurnPlayerId = currentTurnPlayerId;

                UnityMainThreadDispatcher.Instance()?.Enqueue(() =>
                {
                    Debug.Log($"Current turn updated: {currentTurnPlayerId} ({currentTurnPlayerIndex})");
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.SetCurrentTurnPlayer(currentTurnPlayerId, currentTurnPlayerIndex);
                    }
                });
                break;
            }

            case PacketType.TABLE_STATE_SYNC:
            {
                TableStateSyncPacket tableStatePacket = BytesToStruct<TableStateSyncPacket>(packetBytes);
                int visibleCardCount = Mathf.Min(
                    tableStatePacket.visibleCardCount,
                    tableStatePacket.cards?.Length ?? 0,
                    tableStatePacket.posX?.Length ?? 0,
                    tableStatePacket.posY?.Length ?? 0,
                    tableStatePacket.posZ?.Length ?? 0,
                    tableStatePacket.playerIndices?.Length ?? 0);

                UnityMainThreadDispatcher.Instance()?.Enqueue(() =>
                {
                    if (GameManager.Instance == null)
                    {
                        return;
                    }

                    List<(Card.FruitType type, Vector3 position, int count, int playerIndex)> syncedCards = new List<(Card.FruitType, Vector3, int, int)>();
                    for (int i = 0; i < visibleCardCount; i++)
                    {
                        syncedCards.Add((
                            tableStatePacket.cards[i].type,
                            new Vector3(tableStatePacket.posX[i], tableStatePacket.posY[i], tableStatePacket.posZ[i]),
                            tableStatePacket.cards[i].count,
                            tableStatePacket.playerIndices[i]));
                    }

                    GameManager.Instance.SyncTableState(syncedCards);
                });
                break;
            }

            case PacketType.START_GAME_ACK:
            {
                UnityMainThreadDispatcher.Instance()?.Enqueue(() =>
                {
                    Debug.Log("Game start ack received.");
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.GameStart(true);
                    }
                });
                break;
            }

            case PacketType.CARD_DISTRIBUTE:
            {
                CardDistributePacket distributePacket = BytesToStruct<CardDistributePacket>(packetBytes);
                int cardCount = Math.Min(distributePacket.cardCount, distributePacket.cards?.Length ?? 0);

                UnityMainThreadDispatcher.Instance()?.Enqueue(() =>
                {
                    List<(Card.FruitType type, int count, int amount)> cardDataList = new List<(Card.FruitType, int, int)>();
                    for (int i = 0; i < cardCount; i++)
                    {
                        CardInfo card = distributePacket.cards[i];
                        cardDataList.Add((card.type, card.count, 1));
                    }

                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.OnReceiveCardInfos(cardDataList);
                    }
                });
                break;
            }

            case PacketType.CARD_DRAW_RESULT:
            {
                DrawCardResultPacket resultPacket = BytesToStruct<DrawCardResultPacket>(packetBytes);
                Card.FruitType fruitType = resultPacket.card.type;
                int count = resultPacket.card.count;
                int playerIndex = resultPacket.index;
                Vector3 spawnPosition = resultPacket.GetPosition();

                UnityMainThreadDispatcher.Instance()?.Enqueue(() =>
                {
                    Debug.Log($"Card draw received: {fruitType}, count {count}");
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.OnCardDrawResult(fruitType, spawnPosition, count, playerIndex);
                    }
                });
                break;
            }

            case PacketType.GAME_OVER:
            {
                UnityMainThreadDispatcher.Instance()?.Enqueue(() =>
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.GameOver();
                    }
                });
                break;
            }

            case PacketType.WIN_GAME:
            {
                UnityMainThreadDispatcher.Instance()?.Enqueue(() =>
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.WinGame();
                    }
                });
                break;
            }

            default:
                Debug.LogWarning("Unknown packet type: " + type);
                break;
        }
    }

    public void Disconnect()
    {
        running = false;
        try
        {
            stream?.Close();
            client?.Close();
        }
        catch (Exception e)
        {
            Debug.LogWarning("Disconnect warning: " + e.Message);
        }

        if (receiveThread != null && receiveThread.IsAlive && Thread.CurrentThread != receiveThread)
        {
            receiveThread.Join(500);
        }

        Debug.Log("Client closed.");
    }

    public void SendStartReqPacket()
    {
        StartGameReqPacket packet = new StartGameReqPacket();
        packet.header.size = (ushort)Marshal.SizeOf(typeof(StartGameReqPacket));
        packet.header.type = (ushort)PacketType.START_GAME_REQ;

        byte[] data = StructToBytes(packet);
        SendBytes(data);
    }

    public void SendBellReqPacket(int playerIndex)
    {
        BellReqPacket packet = new BellReqPacket();
        packet.header.size = (ushort)Marshal.SizeOf(typeof(BellReqPacket));
        packet.header.type = (ushort)PacketType.CLICK_BELL_REQ;
        packet.playerIndex = playerIndex;

        byte[] data = StructToBytes(packet);
        SendBytes(data);
    }

    public void SendLoginReqPacket(string playerID)
    {
        LoginReqPacket loginPacket = new LoginReqPacket();
        loginPacket.header.size = (ushort)Marshal.SizeOf(typeof(LoginReqPacket));
        loginPacket.header.type = (ushort)PacketType.LOGIN;
        loginPacket.playerID = new byte[256];

        byte[] idBytes = Encoding.UTF8.GetBytes(playerID);
        int copyLength = Math.Min(idBytes.Length, 255);
        Array.Copy(idBytes, loginPacket.playerID, copyLength);
        loginPacket.playerID[copyLength] = 0;

        byte[] data = StructToBytes(loginPacket);
        SendBytes(data);
    }

    public void SendDrawCardReqPacket()
    {
        DrawCardReqPacket drawCardPacket = new DrawCardReqPacket();
        drawCardPacket.header.size = (ushort)Marshal.SizeOf(typeof(DrawCardReqPacket));
        drawCardPacket.header.type = (ushort)PacketType.CARD_DRAW_REQ;

        byte[] data = StructToBytes(drawCardPacket);
        SendBytes(data);
    }

    private void SendBytes(byte[] data)
    {
        try
        {
            if (client != null && client.Connected)
            {
                lock (streamLock)
                {
                    stream.Write(data, 0, data.Length);
                    stream.Flush();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Send error: " + e.Message);
        }
    }

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

    public static T BytesToStruct<T>(byte[] bytes) where T : struct
    {
        int size = Marshal.SizeOf(typeof(T));
        if (bytes.Length < size)
        {
            throw new ArgumentException("Byte array is smaller than the target struct.");
        }

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
