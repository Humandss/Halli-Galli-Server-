using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{   
    //과일 갯수에 따른 좌표를 딕셔너리 형태로 제작
    private static readonly Dictionary<int, Vector2[]> FruitPositions = new Dictionary<int, Vector2[]>
    {   // 과일 갯수가 1일 때 포지션 값
        { 1, new Vector2[] {
            new Vector2(0f, 0f)
        }},
        // 과일 갯수가 2일 때 포지션 값
        { 2, new Vector2[] {
            new Vector2(0.0f, 0.4f),
            new Vector2(0.0f, -0.4f)
        }},
        // 과일 갯수가 3일 때 포지션 값
        { 3, new Vector2[] {
            new Vector2(-0.6f, 0.4f),
            new Vector2(0.6f, 0.4f),
            new Vector2(0f, -0.5f)
        }},
        //과일 갯수가 4일 때 포지션 값
        { 4, new Vector2[] {
            new Vector2(-0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(-0.5f, -0.5f),
            new Vector2(0.5f, -0.5f)
        }},
        //과일 갯수가 5일 때 포지션 값
        { 5, new Vector2[] {
            new Vector2(-0.6f, 0.6f),
            new Vector2(0.6f, 0.6f),
            new Vector2(0f, 0f),
            new Vector2(-0.6f, -0.6f),
            new Vector2(0.6f, -0.6f)
        }},
    };
    //과일 종류 정의
    public enum FruitType
    {
        Strawberry=0,
        Banana=1,
        Kiwi=2,
        Grape=3
 
    }
   
    public FruitType type;
    public int count;

    public GameObject fruitPrefab;         // 과일 하나용 프리팹
    public Transform fruitContainer;       // 과일들이 들어갈 부모 오브젝트
    public SpriteRenderer cardRenderer;    // 카드 배경 이미지 표시용
    //카드 배경용
    public Sprite cardSprite;
    //과일 스프라이트
    public Sprite strawberrySprite;
    public Sprite bananaSprite;
    public Sprite kiwiSprite;
    public Sprite grapeSprite;

    private SpriteRenderer spriteRenderer;
    // Card constructor
    public void Initialize(FruitType type, int count, Sprite cardSprite)
    {
        this.type = type;
        this.count = count;
        this.cardSprite = cardSprite;

        cardRenderer.sprite = cardSprite;
        Sprite fruitSprite = GetFruitSprite(type);

        // 기존 과일 오브젝트 제거
        foreach (Transform child in fruitContainer)
            Destroy(child.gameObject);

        // 과일 생성
        if (!FruitPositions.ContainsKey(count))
        {
            Debug.LogWarning($"과일 위치 정의 안됨: {count}개");
            return;
        }

        Vector2[] positions = FruitPositions[count];

        for (int i = 0; i < count; i++)
        {
            GameObject fruit = Instantiate(fruitPrefab, fruitContainer);
            fruit.GetComponent<SpriteRenderer>().sprite = fruitSprite;

            Vector2 pos = positions[i];
            fruit.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            //테스트 코드
           // Debug.Log($"Card pos: {transform.position}");
           // Debug.Log($"cardRenderer GameObject pos: {cardRenderer.transform.position}");
            //Debug.Log($"cardRenderer GameObject localPos: {cardRenderer.transform.localPosition}");
            // Debug.Log($"카드 초기화: 타입={type}, 개수={count}, 배경스프라이트={(cardSprite != null ? cardSprite.name : "null")}");
            // Debug.Log($"Card type: {type}, count: {count}, fruitContainer pos: {fruitContainer.localPosition}");
        }
    }
    private Sprite GetFruitSprite(FruitType type)
    {
        return type switch
        {
            FruitType.Strawberry => strawberrySprite,
            FruitType.Banana => bananaSprite,
            FruitType.Kiwi => kiwiSprite,
            FruitType.Grape => grapeSprite,
            _ => null
        };
    }

}
