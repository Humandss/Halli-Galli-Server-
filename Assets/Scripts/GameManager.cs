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
    private int readyPlayerCount = 0;
    private int myRemainingCardCount = 0;
    private bool isLocallyReady = false;
    private int ownPlayerIndex = -1;
    private int currentTurnRelativeIndex = -1;
    private bool hasCurrentTurnInfo = false;
    private string currentTurnPlayerId = string.Empty;
    private readonly List<string> playerNames = new List<string>();

    void Awake()
    {
        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
        }

        if (tcpClient == null)
        {
            tcpClient = FindObjectOfType<TCPClient>();
        }

        if (deckManager == null)
        {
            deckManager = FindObjectOfType<DeckManager>();
        }

        Instance = this;

        if (tcpClient != null)
        {
            ownPlayerIndex = tcpClient.AssignedPlayerIndex;
            playerCount = tcpClient.LastKnownPlayerCount;
            readyPlayerCount = tcpClient.LastKnownReadyPlayerCount;
            currentTurnPlayerId = tcpClient.LastKnownCurrentTurnPlayerId;
            SetCurrentTurnPlayer(currentTurnPlayerId, tcpClient.LastKnownCurrentTurnPlayerIndex);
        }
    }

    void Update()
    {
        if (Time.time - lastInputTime < inputCooldown)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) && playingGame)
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
            Debug.LogError("DeckManager reference is missing.");
        }
    }

    public void LoginSuccess(bool success, int playerIndex)
    {
        if (success)
        {
            ownPlayerIndex = playerIndex;
            Debug.Log("Login success. Assigned player index: " + ownPlayerIndex);
        }
        else
        {
            Debug.LogWarning("Login failed.");
        }
    }

    public void GameStart(bool isSuccess)
    {
        playingGame = isSuccess;
        if (isSuccess)
        {
            isLocallyReady = false;
        }
        if (uiManager != null)
        {
            uiManager.TurnOffPanel();
        }
    }

    public void GetPlayerCount(int count)
    {
        playerCount = count;
        RecalculateCurrentTurnRelativeIndex();
    }

    public void GetReadyPlayerCount(int count)
    {
        readyPlayerCount = count;
    }

    public int SendPlayerCount()
    {
        return playerCount;
    }

    public int SendReadyPlayerCount()
    {
        return readyPlayerCount;
    }

    public void GetMyRemainingCardCount(int count)
    {
        myRemainingCardCount = count;
    }

    public int SendMyRemainingCardCount()
    {
        return myRemainingCardCount;
    }

    public void SetPlayerNames(IEnumerable<string> names)
    {
        playerNames.Clear();
        playerNames.AddRange(names);
        RecalculateCurrentTurnRelativeIndex();
    }

    public IReadOnlyList<string> SendPlayerNames()
    {
        return playerNames;
    }

    public bool HasCurrentTurnInfo()
    {
        return hasCurrentTurnInfo;
    }

    public int SendCurrentTurnRelativeIndex()
    {
        return currentTurnRelativeIndex;
    }

    public int SendMyIndex()
    {
        return ownPlayerIndex;
    }

    public void SetCurrentTurnPlayer(string playerId, int absolutePlayerIndex)
    {
        currentTurnPlayerId = playerId ?? string.Empty;

        if (!string.IsNullOrEmpty(currentTurnPlayerId))
        {
            int relativeIndexFromName = playerNames.IndexOf(currentTurnPlayerId);
            if (relativeIndexFromName >= 0)
            {
                currentTurnRelativeIndex = relativeIndexFromName;
                hasCurrentTurnInfo = true;
                return;
            }
        }

        if (playerCount <= 0 || ownPlayerIndex < 0 || absolutePlayerIndex < 0)
        {
            hasCurrentTurnInfo = false;
            currentTurnRelativeIndex = -1;
            return;
        }

        currentTurnRelativeIndex = GetRelativeIndex(absolutePlayerIndex);
        hasCurrentTurnInfo = true;
    }

    public void SendGameStartRequest()
    {
        if (!isLocallyReady)
        {
            readyPlayerCount = Mathf.Min(playerCount, readyPlayerCount + 1);
            isLocallyReady = true;
        }

        if (tcpClient != null)
        {
            tcpClient.SendStartReqPacket();
        }
    }

    private void SendDrawCardRequest()
    {
        if (tcpClient != null)
        {
            tcpClient.SendDrawCardReqPacket();
        }
    }

    public void OnCardDrawResult(Card.FruitType type, Vector3 spawnPos, int count, int playerIndex)
    {
        deckManager.DrawCard(type, spawnPos, count, playerIndex);
    }

    public void SyncTableState(List<(Card.FruitType type, Vector3 position, int count, int playerIndex)> tableCards)
    {
        if (deckManager == null)
        {
            Debug.LogError("DeckManager reference is missing.");
            return;
        }

        deckManager.SyncTableState(tableCards);
    }

    public void SendBellRequest()
    {
        if (tcpClient != null && ownPlayerIndex >= 0)
        {
            tcpClient.SendBellReqPacket(ownPlayerIndex);
        }
    }

    public void OnBellResult(bool success)
    {
        if (success)
        {
            deckManager.ClearAllTableCard(playerCount);
        }
    }

    public void GameOver()
    {
        if (uiManager != null)
        {
            uiManager.TurnOnGameOverPanel();
        }

        playingGame = false;
    }

    public void WinGame()
    {
        if (uiManager != null)
        {
            uiManager.TurnOnGameWinPanel();
        }

        playingGame = false;
    }

    private int GetRelativeIndex(int absoluteIndex)
    {
        int normalized = (absoluteIndex - ownPlayerIndex) % playerCount;
        if (normalized < 0)
        {
            normalized += playerCount;
        }

        return normalized;
    }

    private void RecalculateCurrentTurnRelativeIndex()
    {
        if (string.IsNullOrEmpty(currentTurnPlayerId))
        {
            hasCurrentTurnInfo = false;
            currentTurnRelativeIndex = -1;
            return;
        }

        int relativeIndex = playerNames.IndexOf(currentTurnPlayerId);
        if (relativeIndex >= 0)
        {
            currentTurnRelativeIndex = relativeIndex;
            hasCurrentTurnInfo = true;
        }
        else
        {
            hasCurrentTurnInfo = false;
            currentTurnRelativeIndex = -1;
        }
    }
}
