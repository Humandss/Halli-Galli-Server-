using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
   
   
    public static GameManager Instance;

    public TCPClient tcpClient;
    public UIManager uiManager;
    public DeckManager deckManager;


    private Dictionary<int, Vector3> playerCardPositions = new();


    private float inputCooldown = 0.3f;
    private float lastInputTime = 0f;

    private bool playingGame = false;
    private int playerCount = 0;
    private int ownPlayerIndex = 0;

  
    void Awake()
    {
     
        if(uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
        }
        if(tcpClient == null)
        {
            tcpClient = FindObjectOfType<TCPClient>();
 
        }
        if(deckManager == null)
        {
            deckManager = FindObjectOfType<DeckManager>();
        }
        Instance = this;
    }

    void Update()
    {
        if (Time.time - lastInputTime < inputCooldown)
            return;

        if (Input.GetKeyDown(KeyCode.Space)&& playingGame)
        {
            lastInputTime = Time.time;
            SendDrawCardRequest();
        }

    }
    public void OnReceiveCardInfos(List<(Card.FruitType type, int count, int amount)> cardDataList)
    {
        if (deckManager != null)
        {
            deckManager.InitializeDeck(cardDataList, playerCount);
            
        }
        else
        {
            Debug.LogError("DeckManager 참조가 없습니다!");
        }
    }
    public void LoginSuccess(bool success, int playerIndex)
    {
        if (success)
        {
            ownPlayerIndex = playerIndex;
            Debug.Log($"플레이어 로그인 성공, 할당된 인덱스:" + ownPlayerIndex);
    
        }
        else
        {
            Debug.LogWarning("로그인 실패");
  
        }
    }
    //게임 시작 시그널
    public void GameStart(bool isSuccess)
    {
        playingGame = isSuccess;
        uiManager.TurnOffPanel();

    }
   public void GetPlayerCount(int count)
    {
        playerCount = count;
    }
    public int SendPlayerCount()
    {
        return playerCount;
    }
    public int SendMyIndex()
    {
        return ownPlayerIndex;
    }
    //서버로 게임 시작 가능한지 요청
    public void SendGameStartRequest()
    {
        tcpClient.SendStartReqPacket();
    }
    // 서버로 카드 요청
    private void SendDrawCardRequest()
    {
       tcpClient.SendDrawCardReqPacket();
  
    }
    //서버로부터 카드 내기 결과 받았을 때 호출
    public void OnCardDrawResult(Card.FruitType type, Vector3 spawnPos, int count, int playerIndex)
    {
   
        deckManager.DrawCard(type, spawnPos, count, playerIndex);
    }

    //서버로 벨 누름 요청
    public void SendBellRequest()
    {
        tcpClient.SendBellReqPacket(0); //임시로 0번 인덱스로 표시
    }
    // 서버로부터 벨 결과 받았을 때 호출
    public void OnBellResult(bool success)
    {
        if (success)
        {
            deckManager.ClearAllTableCard(playerCount);
        }
       
    }

    // 서버로부터 게임 오버 받으면 호출
    public void GameOver()
    {
        uiManager.TurnOnGameOverPanel();
        playingGame = false;
       
    }
    public void WinGame()
    {
        uiManager.TurnOnGameWinPanel();
        playingGame = false;
    }
}
