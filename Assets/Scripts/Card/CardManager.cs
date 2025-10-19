using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FusionResult
{
    public string cardAName;
    public string cardBName;
    public Sprite resultSprite;
}

public class CardManager : MonoBehaviour
{
    public List<Card> deck = new List<Card>();
    public List<Card> hand = new List<Card>();
    public int startingHandCount = 5;

    [Header("Fusion Table")]
    public List<FusionResult> fusionTable;

    public Card GetRandomCardByType(CardType type)
    {
        List<Card> cardsOfType = deck.FindAll(c => c.cardType == type);

        if (cardsOfType.Count == 0) return null;

        Card originalCard = cardsOfType[Random.Range(0, cardsOfType.Count)];
        return originalCard.Clone();
    }


    public Card GetRandomCard()
    {
        if (deck.Count == 0) return null;

        Card originalCard = deck[Random.Range(0, deck.Count)];
        return originalCard.Clone();
    }

    public void DrawStartingHand()
    {
        hand.Clear();

        Card typeA_card = GetRandomCardByType(CardType.TypeA);
        if (typeA_card != null)
        {
            hand.Add(typeA_card);
        }
        else
        {
            Debug.LogError("Deck does not contain any CardType.TypeA cards!");
        }

        Card typeB_card = GetRandomCardByType(CardType.TypeB);
        if (typeB_card != null)
        {
            hand.Add(typeB_card);
        }
        else
        {
            Debug.LogError("Deck does not contain any CardType.TypeB cards!");
        }

        int remainingDraws = startingHandCount - hand.Count;

        for (int i = 0; i < remainingDraws; i++)
        {
            Card c = GetRandomCard();
            if (c != null)
                hand.Add(c);
        }

        if (hand.Count > startingHandCount)
        {
            hand.RemoveRange(startingHandCount, hand.Count - startingHandCount);
        }

        Debug.Log($"Starting hand: {hand.Count} cards");
    }

    public Card FuseCards(Card a, Card b)
    {
        if (a.cardType == b.cardType)
        {
            Debug.Log($"Cannot fuse cards of the same type: {a.cardType}");
            return null;
        }

        Card fused = new Card();
        fused.cardName = a.cardName + "+" + b.cardName;
        fused.damage = a.damage + b.damage;
        fused.silence = a.silence + b.silence;

        Sprite resultSprite = null;
        foreach (var fr in fusionTable)
        {
            if ((fr.cardAName == a.cardName && fr.cardBName == b.cardName) ||
                (fr.cardAName == b.cardName && fr.cardBName == a.cardName))
            {
                resultSprite = fr.resultSprite;
                break;
            }
        }

        fused.cardSprite = resultSprite != null ? resultSprite : a.cardSprite;

        return fused;
    }

    public void ShowRewardSelection()
    {
        List<Card> rewardCards = new List<Card>();
        for (int i = 0; i < 3; i++)
        {
            Card randomCard = GetRandomCard();
            if (randomCard != null)
                rewardCards.Add(randomCard);
        }

        NewRewardCard.Instance.Show(rewardCards, (chosenCard) =>
        {
            hand.Add(chosenCard);
            Debug.Log($"Reward added: {chosenCard.cardName}");
            CardFusionSystem.Instance.RefreshHandUI();
        });
    }
}