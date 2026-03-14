using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    GameManager gameManager;
    public GameObject cardPrefab;
    public List<Sprite> cardSprites;
    //생성된 카드 리스트
    public List<Card> cards = new List<Card>();
  
    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager=FindObjectOfType<GameManager>();
        }
    }
   
    private Dictionary<int, GameObject> lastPlayedCards = new Dictionary<int, GameObject>();

   
    public void InitializeDeck(List<(Card.FruitType type, int count, int amount)> cardInfo, int playerCount)
    {
  
        // 기존 카드 정리
        foreach (Transform child in transform)
            Destroy(child.gameObject);
        cards.Clear();

        int index = 0;
        foreach (var info in cardInfo)
        {
            for (int i = 0; i < info.amount; i++)
            {
                Vector3 spawnPos = new Vector3(index * 0.5f, 0, 0); // 카드 위치 생성
                GameObject cardObj = Instantiate(cardPrefab, spawnPos, Quaternion.identity, transform);
                Card card = cardObj.GetComponent<Card>();
                if (card != null)
                {
                    Sprite sprite = GetCardSprite(info.type, info.count);
                    card.Initialize(info.type, info.count, sprite);
                    cardObj.SetActive(false);
                    cards.Add(card);
                }
                index++;
            }
        }
    }
    
    public void Shuffle()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            Card temp = cards[i];
            int randomIndex = Random.Range(i, cards.Count);
            cards[i] = cards[randomIndex];
            cards[randomIndex] = temp;
        }
    }

    private Sprite GetCardSprite(Card.FruitType type, int count)
    {
        int index = ((int)type) * 5 + (count - 1);
       

        if (index >= 0 && index < cardSprites.Count)
        {
            return cardSprites[index];
        }
        else
        {
            return null;
        }
    }

    public void DrawCard(Card.FruitType cardType, Vector3 cardPos, int cardCount, int senderIndex)
    {

        // 기존 카드 오브젝트가 있다면 비활성화 또는 제거
        if (lastPlayedCards.ContainsKey(senderIndex))
        {
            lastPlayedCards[senderIndex].SetActive(false);
        }

        //서버에서 받은 위치를 그대로 사용
        Vector3 spawnPos = cardPos;

        // 카드 프리팹 생성
        GameObject cardObj = Instantiate(cardPrefab, spawnPos, Quaternion.identity);
        Card card = cardObj.GetComponent<Card>();
        if (card != null)
        {
            Sprite sprite = GetCardSprite(cardType, cardCount);
            card.Initialize((Card.FruitType)cardType, cardCount, sprite);
        }
        else
        {
            Debug.LogError("Card 컴포넌트를 찾을 수 없습니다.");
        }
        lastPlayedCards[senderIndex] = cardObj;
    }
    public void ClearAllTableCard(int totalPlayer)
    {
        Debug.Log(totalPlayer);
        //제출한 모든 카드를 삭제
        for (int i = 0; i < totalPlayer; i++)
        {
            if (lastPlayedCards.ContainsKey(i))
            {
                Debug.Log($"[카드 제거] 인덱스 {i} - 존재 여부: {lastPlayedCards.ContainsKey(i)}");
                Destroy(lastPlayedCards[i]);
            }
        }
        lastPlayedCards.Clear();
    }
}

