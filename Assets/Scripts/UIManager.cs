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
    // Start is called before the first frame update
    void Start()
    {
        panel.SetActive(true);
        panel2.SetActive(false);
        panel3.SetActive(false);
        startButton.interactable = true;
         
    }
    private void Awake()
    {
        if(gameManager == null)
        {
            gameManager=FindObjectOfType<GameManager>();
        }
       
    }
    private void Update()
    {
        if (playerCount != 0)
        {
            CheckPlayerCount();
        }
    }
    public void CheckPlayerCount()
    {
         playerCount = gameManager.SendPlayerCount();
         playerListText.text = "player count : " + playerCount;

    }
    public void StartButtonClicked()
    {
        //시작 버튼을 누르면 비활성화
        startButton.interactable = false;
        gameManager.SendGameStartRequest();
    }
    public void TurnOffPanel()
    {
        panel.SetActive(false);
    }

    public void TurnOnGameOverPanel()
    {
        panel2.SetActive(true);
    }
    public void TurnOnGameWinPanel()
    {
        panel3.SetActive(true);
    }
}
