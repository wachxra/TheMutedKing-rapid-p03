using UnityEngine;

public enum CardSlotType
{
    KingSlot,
    WeaponSlot,
    SilenceGearSlot
}

[System.Serializable]
public class CardSlot
{
    public CardSlotType slotType;
    public Card currentCard;
    public Transform slotTransform;

    public int remainingUses;
    public float remainingDuration;

    public bool isActive = false;
}