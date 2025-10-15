using UnityEngine;

[System.Serializable]
public class Card
{
    public string cardName;
    public int damage;
    public int silence;
    public Sprite cardSprite;
    public CardSlotType slotType;

    public CardType cardType;

    [HideInInspector]
    public bool isSelected = false;

    public Card Clone()
    {
        return new Card
        {
            cardName = this.cardName,
            damage = this.damage,
            silence = this.silence,
            cardSprite = this.cardSprite,
            slotType = this.slotType,
            cardType = this.cardType,
            isSelected = false
        };
    }
}

public enum CardType
{
    TypeA,
    TypeB
}