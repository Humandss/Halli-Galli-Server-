using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    private GameObject panel;
    [SerializeField]
    private GameObject panel2;
    [SerializeField]
    private GameObject panel3;
    [SerializeField]
    private Button startButton;
    [SerializeField]
    private TMP_Text playerListText;

    public GameManager gameManager;
    private int playerCount;
    private TMP_Text inGameInfoText;
    private readonly List<TMP_Text> playerNameLabels = new List<TMP_Text>();

    void Start()
    {
        panel.SetActive(true);
        panel2.SetActive(false);
        panel3.SetActive(false);
        startButton.interactable = true;
        EnsureInGameInfoText();
    }

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
    }

    private void Update()
    {
        if (gameManager != null)
        {
            CheckPlayerCount();
            UpdateInGameInfo();
            UpdatePlayerNameLabels();
        }
    }

    public void CheckPlayerCount()
    {
        playerCount = gameManager.SendPlayerCount();
        if (playerListText != null)
        {
            int readyPlayerCount = gameManager.SendReadyPlayerCount();
            playerListText.text = $"Waiting: {playerCount}\nReady: {readyPlayerCount}";
        }
    }

    public void StartButtonClicked()
    {
        if (startButton != null)
        {
            startButton.interactable = false;
        }

        if (gameManager != null)
        {
            gameManager.SendGameStartRequest();
        }
    }

    public void TurnOffPanel()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    public void TurnOnGameOverPanel()
    {
        if (panel2 != null)
        {
            panel2.SetActive(true);
        }
    }

    public void TurnOnGameWinPanel()
    {
        if (panel3 != null)
        {
            panel3.SetActive(true);
        }
    }

    private void EnsureInGameInfoText()
    {
        if (playerListText == null || playerListText.canvas == null)
        {
            return;
        }

        inGameInfoText = Instantiate(playerListText, playerListText.canvas.transform);
        inGameInfoText.name = "InGameInfoText";
        inGameInfoText.text = string.Empty;
        inGameInfoText.gameObject.SetActive(false);

        RectTransform rectTransform = inGameInfoText.rectTransform;
        rectTransform.SetParent(playerListText.canvas.transform, false);
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = new Vector2(24f, -24f);
        rectTransform.sizeDelta = new Vector2(420f, 120f);

        inGameInfoText.alignment = TextAlignmentOptions.TopLeft;
        inGameInfoText.fontSize = Mathf.Max(18f, playerListText.fontSize - 4f);
    }

    private void UpdateInGameInfo()
    {
        if (inGameInfoText == null)
        {
            return;
        }

        bool showInGameInfo = panel == null || !panel.activeSelf;
        inGameInfoText.gameObject.SetActive(showInGameInfo);
        if (!showInGameInfo)
        {
            return;
        }

        int myRemainingCardCount = gameManager.SendMyRemainingCardCount();
        inGameInfoText.text = $"My Cards: {myRemainingCardCount}";
    }

    private void UpdatePlayerNameLabels()
    {
        IReadOnlyList<string> playerNames = gameManager.SendPlayerNames();
        bool showLabels = panel == null || !panel.activeSelf;
        bool hasCurrentTurnInfo = gameManager.HasCurrentTurnInfo();
        int currentTurnRelativeIndex = gameManager.SendCurrentTurnRelativeIndex();

        EnsurePlayerNameLabels(playerNames.Count);

        for (int i = 0; i < playerNameLabels.Count; i++)
        {
            TMP_Text label = playerNameLabels[i];
            bool shouldShow = showLabels && i < playerNames.Count;
            label.gameObject.SetActive(shouldShow);
            if (!shouldShow)
            {
                continue;
            }

            string turnPrefix = hasCurrentTurnInfo && i == currentTurnRelativeIndex ? "> " : string.Empty;
            label.text = turnPrefix + playerNames[i];
            label.color = Color.black;
            ApplyPlayerLabelPosition(label.rectTransform, playerNames.Count, i);
        }
    }

    private void EnsurePlayerNameLabels(int count)
    {
        if (playerListText == null || playerListText.canvas == null)
        {
            return;
        }

        while (playerNameLabels.Count < count)
        {
            TMP_Text label = Instantiate(playerListText, playerListText.canvas.transform);
            label.name = $"PlayerNameLabel{playerNameLabels.Count}";
            label.text = string.Empty;
            label.gameObject.SetActive(false);
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = Mathf.Max(20f, playerListText.fontSize - 2f);
            label.fontStyle = FontStyles.Bold;
            label.color = Color.black;
            label.rectTransform.SetParent(playerListText.canvas.transform, false);
            label.rectTransform.sizeDelta = new Vector2(220f, 40f);
            playerNameLabels.Add(label);
        }
    }

    private void ApplyPlayerLabelPosition(RectTransform rectTransform, int playerCount, int relativeIndex)
    {
        Vector2 anchor = GetPlayerLabelAnchor(playerCount, relativeIndex);
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
    }

    private Vector2 GetPlayerLabelAnchor(int playerCount, int relativeIndex)
    {
        if (playerCount <= 1)
        {
            return new Vector2(0.5f, 0.12f);
        }

        return playerCount switch
        {
            2 => relativeIndex switch
            {
                0 => new Vector2(0.5f, 0.06f),
                1 => new Vector2(0.5f, 0.94f),
                _ => new Vector2(0.5f, 0.5f)
            },
            3 => relativeIndex switch
            {
                0 => new Vector2(0.5f, 0.06f),
                1 => new Vector2(0.14f, 0.86f),
                2 => new Vector2(0.86f, 0.86f),
                _ => new Vector2(0.5f, 0.5f)
            },
            _ => relativeIndex switch
            {
                0 => new Vector2(0.5f, 0.06f),
                1 => new Vector2(0.1f, 0.5f),
                2 => new Vector2(0.5f, 0.94f),
                3 => new Vector2(0.9f, 0.5f),
                _ => new Vector2(0.5f, 0.5f)
            }
        };
    }
}
