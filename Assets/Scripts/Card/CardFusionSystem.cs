using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardFusionSystem : MonoBehaviour
{
    public static CardFusionSystem Instance;

    [Header("References")]
    public CardManager cardManager;
    public GameObject handPanel;
    public GameObject slotsPanel;
    public GameObject cardPrefab;
    public List<CardSlot> slots;

    [Header("Hand Settings")]
    public int maxHandCount = 5;
    public float cardSpacing = 150f;
    public float hoverHeight = 30f;
    public float selectedHeight = 60f;
    public float spawnAnimationTime = 0.3f;

    [Header("Fusion Stats Range")]
    public int minWeaponUses = 2;
    public int maxWeaponUses = 10;
    public int minSilenceDuration = 5;
    public int maxSilenceDuration = 15;

    private Card activeFusionCard = null;
    private bool isMovingFusionCard = false;
    private int currentSlotIndex = 0;

    public Dictionary<Card, GameObject> handCardUI = new Dictionary<Card, GameObject>();
    private List<Card> selectedCards = new List<Card>();
    private int currentIndex = 0;
    private bool active = false;

    void Awake()
    {
        Instance = this;
        if (handPanel != null) handPanel.SetActive(false);
        if (slotsPanel != null) slotsPanel.SetActive(false);

        if (cardManager.hand.Count == 0)
            cardManager.DrawStartingHand();
    }

    void Update()
    {
        if (!active) return;

        if (isMovingFusionCard && activeFusionCard != null)
        {
            HandleSlotMovement();
        }
        else
        {
            HandleInput();
        }
    }

    void HandleSlotMovement()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            currentSlotIndex = (currentSlotIndex - 1 + slots.Count) % slots.Count;
        if (Input.GetKeyDown(KeyCode.RightArrow))
            currentSlotIndex = (currentSlotIndex + 1) % slots.Count;

        GameObject fusedUI = null;
        if (handCardUI.ContainsKey(activeFusionCard))
        {
            fusedUI = handCardUI[activeFusionCard];
        }

        if (fusedUI != null && slots[currentSlotIndex].slotTransform != null)
            fusedUI.transform.position = slots[currentSlotIndex].slotTransform.position;

        if (Input.GetKeyDown(KeyCode.Return))
        {
            PlaceFusionCard();
        }
    }

    void PlaceFusionCard()
    {
        if (activeFusionCard == null) return;

        CardSlot currentSlot = slots[currentSlotIndex];

        GameObject fusedUI = null;
        if (handCardUI.ContainsKey(activeFusionCard))
        {
            fusedUI = handCardUI[activeFusionCard];
        }

        if (HandleCardPlacement(currentSlot, activeFusionCard))
        {
            if (fusedUI != null && currentSlot.slotTransform != null)
            {
                fusedUI.transform.SetParent(currentSlot.slotTransform);
                fusedUI.transform.localPosition = Vector3.zero;

                handCardUI.Remove(activeFusionCard);

                if (currentSlot.slotType == CardSlotType.KingSlot)
                {
                    Destroy(fusedUI);
                }
            }
        }

        activeFusionCard = null;
        isMovingFusionCard = false;

        RefreshHandUI();
    }

    private bool HandleCardPlacement(CardSlot slot, Card card)
    {
        if (slot.currentCard != null)
        {
            if (slot.slotTransform != null && slot.slotTransform.childCount > 0)
            {
                Destroy(slot.slotTransform.GetChild(0).gameObject);
            }
        }

        if (slot.slotType == CardSlotType.KingSlot)
        {
            Debug.Log($"King Slot used: {card.cardName}. (Damage/Silence: {card.damage}/{card.silence}).");

            if (KingController.Instance != null)
                KingController.Instance.TakeDamage(card.damage);

            if (SoundMeterSystem.Instance != null && card.silence > 0)
                SoundMeterSystem.Instance.AddSound(card.silence);

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.ExitCardMode();
            }

            slot.currentCard = null;
            return true;
        }

        else if (slot.slotType == CardSlotType.WeaponSlot)
        {
            int randomUses = Random.Range(minWeaponUses, maxWeaponUses);

            slot.currentCard = card;
            slot.remainingUses = randomUses;
            Debug.Log($"Weapon Slot set: {card.cardName}. Starting Uses: {slot.remainingUses}.");
            return true;
        }

        else if (slot.slotType == CardSlotType.SilenceGearSlot)
        {
            float randomDuration = Random.Range(minSilenceDuration, maxSilenceDuration);
            slot.currentCard = card;
            slot.remainingDuration = randomDuration;
            Debug.Log($"Silence Gear Slot set: {card.cardName}. Duration: {slot.remainingDuration:F1} seconds.");
            return true;
        }

        return false;
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            MoveSelection(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow))
            MoveSelection(1);
        if (Input.GetKeyDown(KeyCode.F))
            SelectCard();
        if (Input.GetKeyDown(KeyCode.Return))
            ConfirmFusion();
    }

    public void OpenCardMode()
    {
        if (cardManager == null) return;

        active = true;
        if (handPanel != null) handPanel.SetActive(true);
        if (slotsPanel != null) slotsPanel.SetActive(true);

        RefreshHandUI();

        StopAllCoroutines();
        StartCoroutine(AnimateCardSpreadFromCenter());
    }

    public void CloseCardMode()
    {
        active = false;

        StopAllCoroutines();
        if (handPanel != null) handPanel.SetActive(false);
        if (slotsPanel != null) slotsPanel.SetActive(false);

        foreach (Card c in selectedCards)
        {
            c.isSelected = false;
        }
        selectedCards.Clear();
        UpdateCardPositions();
    }

    public void MoveSelection(int dir)
    {
        if (cardManager.hand.Count == 0) return;

        currentIndex = (currentIndex + dir) % cardManager.hand.Count;
        if (currentIndex < 0)
            currentIndex += cardManager.hand.Count;

        UpdateCardPositions();
    }

    public void SelectCard()
    {
        if (cardManager.hand.Count == 0) return;

        if (currentIndex >= cardManager.hand.Count) currentIndex = 0;
        if (cardManager.hand.Count == 0) return;

        Card c = cardManager.hand[currentIndex];

        if (selectedCards.Contains(c))
        {
            selectedCards.Remove(c);
            c.isSelected = false;
        }
        else
        {
            if (selectedCards.Count >= 2) return;
            selectedCards.Add(c);
            c.isSelected = true;
        }

        UpdateCardPositions();
    }

    public void ConfirmFusion()
    {
        if (selectedCards.Count != 2) return;

        selectedCards[0].isSelected = false;
        selectedCards[1].isSelected = false;

        Card fused = cardManager.FuseCards(selectedCards[0], selectedCards[1]);

        if (fused == null)
        {
            Debug.Log("Fusion failed: Cards are the same type!");
            selectedCards.Clear();
            UpdateCardPositions();
            return;
        }

        cardManager.hand.Remove(selectedCards[0]);
        cardManager.hand.Remove(selectedCards[1]);

        if (handCardUI.ContainsKey(selectedCards[0]))
        {
            Destroy(handCardUI[selectedCards[0]]);
            handCardUI.Remove(selectedCards[0]);
        }
        if (handCardUI.ContainsKey(selectedCards[1]))
        {
            Destroy(handCardUI[selectedCards[1]]);
            handCardUI.Remove(selectedCards[1]);
        }

        selectedCards.Clear();
        currentIndex = 0;

        activeFusionCard = fused;
        isMovingFusionCard = true;
        currentSlotIndex = 0;

        RefreshHandUI();
    }

    public void RefreshHandUI(bool keepExistingHandUI = false)
    {
        if (handPanel == null) return;

        StopAllCoroutines();

        List<Card> cardsToRemove = new List<Card>();
        foreach (var kvp in handCardUI)
        {
            if (!(isMovingFusionCard && kvp.Key == activeFusionCard) && !cardManager.hand.Contains(kvp.Key))
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value);
                cardsToRemove.Add(kvp.Key);
            }
        }
        foreach (var card in cardsToRemove)
        {
            handCardUI.Remove(card);
        }

        if (isMovingFusionCard && activeFusionCard != null && !handCardUI.ContainsKey(activeFusionCard))
        {
            GameObject go = Instantiate(cardPrefab, handPanel.transform);
            go.name = activeFusionCard.cardName + "_Fused";

            Image img = go.GetComponent<Image>();
            if (img != null) img.sprite = activeFusionCard.cardSprite;

            handCardUI.Add(activeFusionCard, go);
        }

        if (cardManager.hand.Count > 0)
        {
            int handCount = cardManager.hand.Count;
            float totalWidth = (handCount - 1) * cardSpacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < handCount; i++)
            {
                Card c = cardManager.hand[i];

                if (!handCardUI.ContainsKey(c))
                {
                    GameObject go = Instantiate(cardPrefab, handPanel.transform);
                    go.name = c.cardName + "_" + i;

                    Image img = go.GetComponent<Image>();
                    if (img != null) img.sprite = c.cardSprite;

                    handCardUI.Add(c, go);

                    Vector3 targetPos = new Vector3(startX + i * cardSpacing, 0, 0);
                    StartCoroutine(AnimateCardSpawn(go, targetPos));
                }
            }
        }

        if (currentIndex >= cardManager.hand.Count) currentIndex = 0;

        UpdateCardPositions();
    }

    public IEnumerator AnimateCardSpawn(GameObject card, Vector3 targetPos)
    {
        if (card == null) yield break;

        float t = 0;
        Vector3 startPos = card.transform.localPosition + Vector3.down * 500f;

        while (t < spawnAnimationTime)
        {
            if (card == null) yield break;
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / spawnAnimationTime);
            card.transform.localPosition = Vector3.Lerp(startPos, targetPos, Mathf.SmoothStep(0, 1, progress));
            yield return null;
        }

        if (card != null)
            card.transform.localPosition = targetPos;

        UpdateCardPositions();
    }

    private IEnumerator AnimateCardSpreadFromCenter()
    {
        if (cardManager == null || cardManager.hand.Count == 0) yield break;

        List<Card> handCards = cardManager.hand;
        float totalWidth = (handCards.Count - 1) * cardSpacing;
        float startX = -totalWidth / 2f;
        float duration = 0.4f;
        float elapsed = 0f;

        foreach (var kvp in handCardUI)
        {
            if (kvp.Value != null)
                kvp.Value.transform.localPosition = Vector3.zero;
        }

        yield return null;

        Dictionary<Card, Vector3> targetPositions = new Dictionary<Card, Vector3>();
        for (int i = 0; i < handCards.Count; i++)
        {
            float x = startX + (i * cardSpacing);
            Vector3 targetPos = new Vector3(x, 0f, 0f);
            targetPositions[handCards[i]] = targetPos;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);

            foreach (var card in handCards)
            {
                if (handCardUI.TryGetValue(card, out GameObject cardUI))
                {
                    Vector3 target = targetPositions[card];
                    cardUI.transform.localPosition = Vector3.Lerp(Vector3.zero, target, t);
                }
            }

            yield return null;
        }

        foreach (var card in handCards)
        {
            if (handCardUI.TryGetValue(card, out GameObject cardUI))
                cardUI.transform.localPosition = targetPositions[card];
        }
    }

    void UpdateCardPositions()
    {
        int handCount = cardManager.hand.Count;
        if (handCount == 0 && !isMovingFusionCard) return;

        List<Card> handCards = cardManager.hand;

        float totalWidth = (handCount - 1) * cardSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < handCount; i++)
        {
            Card card = handCards[i];
            if (!handCardUI.ContainsKey(card) || handCardUI[card] == null) continue;

            GameObject cardUI = handCardUI[card];
            Vector3 basePos = new Vector3(startX + i * cardSpacing, 0, 0);
            float lift = 0f;

            if (card.isSelected) lift = selectedHeight;
            else if (i == currentIndex) lift = hoverHeight;

            cardUI.transform.localPosition = basePos + Vector3.up * lift;

            Outline outline = cardUI.GetComponent<Outline>();
            if (outline == null)
                outline = cardUI.AddComponent<Outline>();

            if (i == currentIndex)
            {
                outline.effectColor = Color.yellow;
                outline.effectDistance = new Vector2(5f, 5f);
                outline.enabled = true;
            }
            else
            {
                outline.enabled = false;
            }
        }

        if (isMovingFusionCard && activeFusionCard != null && handCardUI.ContainsKey(activeFusionCard))
        {
            GameObject fusedUI = handCardUI[activeFusionCard];
            if (fusedUI != null && fusedUI.transform.parent != handPanel.transform)
            {
                fusedUI.transform.SetParent(handPanel.transform);
            }
        }
    }

    public float GetSilenceBuff()
    {
        foreach (var slot in slots)
        {
            if (slot.slotType == CardSlotType.SilenceGearSlot && slot.currentCard != null && slot.remainingDuration > 0f)
            {
                return slot.currentCard.silence;
            }
        }
        return 0f;
    }
}