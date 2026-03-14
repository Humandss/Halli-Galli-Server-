using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    GameManager gameManager;
    public GameObject cardPrefab;
    public List<Sprite> cardSprites;
    public List<Card> cards = new List<Card>();

    private Dictionary<int, GameObject> lastPlayedCards = new Dictionary<int, GameObject>();

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
    }

    public void InitializeDeck(List<(Card.FruitType type, int count, int amount)> cardInfo, int playerCount)
    {
        // Clear the previous deck view before rebuilding it.
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        cards.Clear();

        int index = 0;
        foreach (var info in cardInfo)
        {
            for (int i = 0; i < info.amount; i++)
            {
                Vector3 spawnPos = new Vector3(index * 0.5f, 0, 0);
                GameObject cardObj = Instantiate(cardPrefab, spawnPos, Quaternion.identity, transform);
                Card card = cardObj.GetComponent<Card>();
                if (card != null)
                {
                    card.Initialize(info.type, info.count, null);
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

    public void DrawCard(Card.FruitType cardType, Vector3 cardPos, int cardCount, int senderIndex)
    {
        // Hide the previous table card for this player.
        if (lastPlayedCards.ContainsKey(senderIndex))
        {
            Destroy(lastPlayedCards[senderIndex]);
        }

        GameObject cardObj = Instantiate(cardPrefab, cardPos, Quaternion.identity);
        Card card = cardObj.GetComponent<Card>();
        if (card != null)
        {
            card.Initialize(cardType, cardCount, null);
        }
        else
        {
            Debug.LogError("Card component is missing on the prefab.");
        }

        lastPlayedCards[senderIndex] = cardObj;
    }

    public void ClearAllTableCard(int totalPlayer)
    {
        foreach (GameObject cardObject in lastPlayedCards.Values)
        {
            if (cardObject != null)
            {
                Destroy(cardObject);
            }
        }

        lastPlayedCards.Clear();
    }

    public void SyncTableState(List<(Card.FruitType type, Vector3 position, int count, int playerIndex)> tableCards)
    {
        ClearAllTableCard(lastPlayedCards.Count);

        foreach (var tableCard in tableCards)
        {
            DrawCard(tableCard.type, tableCard.position, tableCard.count, tableCard.playerIndex);
        }
    }
}
