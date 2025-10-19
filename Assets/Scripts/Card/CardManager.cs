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
    public static CardManager Instance;

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
            hand.Add(typeA_card);

        Card typeB_card = GetRandomCardByType(CardType.TypeB);
        if (typeB_card != null)
            hand.Add(typeB_card);

        int remainingDraws = startingHandCount - hand.Count;
        List<Card> availableCards = new List<Card>(deck);
        availableCards.RemoveAll(c => hand.Exists(h => h.cardName == c.cardName));

        for (int i = 0; i < remainingDraws && availableCards.Count > 0; i++)
        {
            int index = Random.Range(0, availableCards.Count);
            Card c = availableCards[index].Clone();
            hand.Add(c);
            availableCards.RemoveAt(index);
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
            CardFusionSystem.Instance.RefreshHandUI(true);
        });
    }
}