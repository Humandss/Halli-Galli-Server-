using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    // Local fruit positions for 1-5 icons on a single card.
    private static readonly Dictionary<int, Vector2[]> FruitPositions = new Dictionary<int, Vector2[]>
    {
        { 1, new[] { new Vector2(0f, 0f) } },
        { 2, new[] { new Vector2(0.0f, 0.4f), new Vector2(0.0f, -0.4f) } },
        { 3, new[] { new Vector2(-0.6f, 0.4f), new Vector2(0.6f, 0.4f), new Vector2(0f, -0.5f) } },
        { 4, new[] { new Vector2(-0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-0.5f, -0.5f), new Vector2(0.5f, -0.5f) } },
        { 5, new[] { new Vector2(-0.6f, 0.6f), new Vector2(0.6f, 0.6f), new Vector2(0f, 0f), new Vector2(-0.6f, -0.6f), new Vector2(0.6f, -0.6f) } },
    };

    public enum FruitType
    {
        Strawberry = 0,
        Banana = 1,
        Kiwi = 2,
        Grape = 3
    }

    public FruitType type;
    public int count;

    public GameObject fruitPrefab;
    public Transform fruitContainer;
    public SpriteRenderer cardRenderer;
    public Sprite cardSprite;
    public Sprite strawberrySprite;
    public Sprite bananaSprite;
    public Sprite kiwiSprite;
    public Sprite grapeSprite;

    public void Initialize(FruitType type, int count, Sprite cardSprite)
    {
        this.type = type;
        this.count = count;

        if (cardSprite != null)
        {
            this.cardSprite = cardSprite;
        }

        if (cardRenderer != null)
        {
            cardRenderer.sprite = this.cardSprite;
        }

        Sprite fruitSprite = GetFruitSprite(type);

        foreach (Transform child in fruitContainer)
        {
            Destroy(child.gameObject);
        }

        if (!FruitPositions.ContainsKey(count))
        {
            Debug.LogWarning($"Unsupported fruit count: {count}");
            return;
        }

        Vector2[] positions = FruitPositions[count];
        for (int i = 0; i < count; i++)
        {
            GameObject fruit = Instantiate(fruitPrefab, fruitContainer);
            fruit.GetComponent<SpriteRenderer>().sprite = fruitSprite;

            Vector2 pos = positions[i];
            fruit.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
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
