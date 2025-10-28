using UnityEngine;

public enum ChestType
{
    Card,
    Torch
}

public class Chest : MonoBehaviour
{
    [Header("Chest Settings")]
    public ChestType chestType = ChestType.Card;
    public Sprite closedSprite;
    public Sprite openSprite;
    private bool isOpened = false;

    [Header("Torch Settings")]
    public bool hasTorch = false;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && closedSprite != null)
            spriteRenderer.sprite = closedSprite;
    }

    void Update()
    {
        if (isOpened) return;

        if (Vector3.Distance(transform.position, PlayerController.Instance.transform.position) < 1.5f)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                OpenChest();
            }
        }
    }

    void OpenChest()
    {
        isOpened = true;

        if (spriteRenderer != null && openSprite != null)
            spriteRenderer.sprite = openSprite;

        switch (chestType)
        {
            case ChestType.Card:
                GiveCard();
                break;
            case ChestType.Torch:
                GiveTorch();
                break;
        }
    }

    void GiveCard()
    {
        if (CardManager.Instance != null)
        {
            Card randomCard = CardManager.Instance.GetRandomCard();
            if (randomCard != null)
            {
                PlayerController.Instance.cardManager.hand.Add(randomCard.Clone());
                CardFusionSystem.Instance.RefreshHandUI(false);
                Debug.Log($"Received card from chest: {randomCard.cardName}");
            }
        }
    }

    void GiveTorch()
    {
        hasTorch = true;
        OnUseTorch();
        Debug.Log("Torch acquired from chest!");
    }

    void OnUseTorch()
    {
    }
}