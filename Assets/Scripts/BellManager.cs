using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BellManager : MonoBehaviour
{ /*
    private List<PlayerManager> playerList;

    //플레이어 리스트 초기화
    public void Initialize(List<PlayerManager> players)
    {
        playerList = players;
    }
    public bool CheckBellPressed(out Dictionary<Card.FruitType, int> fruitCounts)
    {
        

        fruitCounts = new Dictionary<Card.FruitType, int>();

        foreach(var player in playerList)
        {
            List<GameObject> cards = player.GetAllCardsOnTable();

            //현재 드로우한 카드 리스트들을 순회하면서 setoff된 카드는 제외하고 보여진 카드들만 계산
            foreach (GameObject cardObj in cards)
            {
                if (!cardObj.activeSelf) continue; // 꺼진 카드 무시

                //fruitCount에서 각 과일 종류 키 값에 대해 count value값을 넣음
                Card card = cardObj.GetComponent<Card>();
                if (fruitCounts.ContainsKey(card.type))
                    fruitCounts[card.type] += card.count;
                else
                    fruitCounts[card.type] = card.count;
            }
        }
       
        // 디버그: 현재 딕셔너리 상태 출력
        foreach (var kvp in fruitCounts)
        {
            Debug.Log($"과일: {kvp.Key}, 개수 합: {kvp.Value}");
        }
        // 이후 딕셔너리 키(과일)가 정확히 5개인 게 있는지 확인
        foreach (var card in fruitCounts)
        {
            if (card.Value == 5)
            {
                return true;
            }
        }
        return false;

    }
    //승리한 플레이어에게 카드 제공
    public void GiveAllCardsToPlayer(int winnerPlayerIndex)
    {
        //넘겨줄 카드를 리스트 형태로 저장
        List<GameObject> collectedCards = new List<GameObject>();
  
        for (int i = 0; i < playerList.Count; i++)
        {
            //각 플레이어가 냈던 카드를 리스트로 불러와서 리스트 합침
            List<GameObject> table = playerList[i].GetAllCardsOnTable();
            collectedCards.AddRange(table);

            // 플레이어가 낸 카드 리스트 초기화 함수 호출
            playerList[i].ClearCardsOnTable();
        }

        // 카드 GameObject를 Card로 변환하여 리스트로 만듦.(AddCards 함수에게 넘겨주기 위함)
        List<Card> cardList = new List<Card>();
        foreach (GameObject obj in collectedCards)
        {
            Card card = obj.GetComponent<Card>();
            if (card != null)
                cardList.Add(card);
        }

        //해당 플레이어에게 카드 더해줌
        AddCards(cardList, winnerPlayerIndex);

        // 그 다음 시각적으로 제거
        foreach (var obj in collectedCards)
        {
            GameObject.Destroy(obj);
        }
        //Debug.Log($"[승리 처리] Player {winnerPlayerIndex} 가 {collectedCards.Count}장의 카드를 가져감.");

    }
    //플레이어가 잘못눌렀을 때 패널티를 주는 함수
    public void GivePenaltyToPlayer(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerList.Count)
        {
            Debug.LogWarning($"[패널티 실패] 유효하지 않은 플레이어 인덱스: {playerIndex}");
            return;
        }

        PlayerManager player = playerList[playerIndex];

        // 덱에 카드가 있는지 확인
        if (player.GetDeck().Count > 0)
        {
            // 덱에서 한 장 제거
            Card removedCard = player.GetDeck()[0];
            player.GetDeck().RemoveAt(0);

            Debug.Log($"[패널티] Player {playerIndex} 덱에서 카드 제거: {removedCard.type} {removedCard.count}");
        }
        else
        {
            Debug.LogWarning($"[패널티 실패] Player {playerIndex}의 덱에 카드가 없음");
        }
    }
    //이긴 승자에게 카드를 더해주는 함수
    public void AddCards(List<Card> cards, int winnerIndex)
    {
        if (winnerIndex >= 0 && winnerIndex < playerList.Count)
        {
            PlayerManager winner = playerList[winnerIndex];

            winner.GetDeck().AddRange(cards);

            Debug.Log($"[승리 처리] Player {winnerIndex} 가 {cards.Count}장의 카드를 가져감.");
            LogAllPlayersDeckCount();
        }
        else
        {
            Debug.LogError($"[승리 처리 오류] 유효하지 않은 플레이어 인덱스: {winnerIndex}");
        }
                
    }
    public void LogAllPlayersDeckCount()
    {
        for (int i = 0; i < playerList.Count; i++)
        {
            int cardCount = playerList[i].GetDeck().Count;
            Debug.Log($"[카드 수] Player {i} : {cardCount}장");
        }
    }
    */

    public void OnBellButtonClicked()
    {
        
    }

}

