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

    private List<GameObject> handUI = new List<GameObject>();
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

        GameObject fusedUI = handUI.Count > 0 ? handUI[0] : null;

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

        if (HandleCardPlacement(currentSlot, activeFusionCard))
        {
            GameObject fusedUI = handUI.Find(go => go.name == activeFusionCard.cardName);

            if (fusedUI != null && currentSlot.slotTransform != null)
            {
                fusedUI.transform.SetParent(currentSlot.slotTransform);
                fusedUI.transform.localPosition = Vector3.zero;

                handUI.Remove(fusedUI);

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
        GameObject fusedUI = handUI.Count > 0 ? handUI[0] : null;

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

        bool shouldClearUI = !isMovingFusionCard || !keepExistingHandUI;

        if (shouldClearUI)
        {
            foreach (GameObject go in handUI)
            {
                if (go != null)
                {
                    Destroy(go);
                }
            }
            handUI.Clear();
        }

        if (isMovingFusionCard && activeFusionCard != null && !handUI.Exists(go => go.name == activeFusionCard.cardName))
        {
            GameObject go = Instantiate(cardPrefab, handPanel.transform);
            go.name = activeFusionCard.cardName;
            Image img = go.GetComponent<Image>();
            if (img != null) img.sprite = activeFusionCard.cardSprite;

            go.transform.localPosition = Vector3.zero;
            handUI.Add(go);
        }

        if (cardManager.hand.Count == 0 && !isMovingFusionCard) return;

        float totalWidth = (cardManager.hand.Count - 1) * cardSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < cardManager.hand.Count; i++)
        {
            Card c = cardManager.hand[i];

            if (!handUI.Exists(go => go.name == c.cardName))
            {
                GameObject go = Instantiate(cardPrefab, handPanel.transform);
                go.name = c.cardName;
                Image img = go.GetComponent<Image>();
                if (img != null) img.sprite = c.cardSprite;

                handUI.Add(go);

                Vector3 targetPos = new Vector3(startX + i * cardSpacing, 0, 0);
                StartCoroutine(AnimateCardSpawn(go, targetPos));
            }
        }

        if (currentIndex >= cardManager.hand.Count) currentIndex = 0;

        UpdateCardPositions();
    }

    IEnumerator AnimateCardSpawn(GameObject card, Vector3 targetPos)
    {
        if (card == null) yield break;

        float t = 0;
        Vector3 startPos = card.transform.localPosition;

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

    void UpdateCardPositions()
    {
        if (handUI.Count == 0) return;

        int startIndex = isMovingFusionCard ? 1 : 0;
        int handCount = cardManager.hand.Count;

        if (handCount == 0 && !isMovingFusionCard) return;

        float totalWidth = (handCount - 1) * cardSpacing;
        float startX = -totalWidth / 2f;

        for (int i = startIndex; i < handUI.Count; i++)
        {
            int cardDataIndex = i - startIndex;

            if (handUI[i] == null || cardDataIndex >= handCount) continue;

            Vector3 basePos = new Vector3(startX + cardDataIndex * cardSpacing, 0, 0);
            Card card = cardManager.hand[cardDataIndex];
            float lift = 0f;

            if (card.isSelected) lift = selectedHeight;
            else if (cardDataIndex == currentIndex) lift = hoverHeight;

            handUI[i].transform.localPosition = basePos + Vector3.up * lift;

            Outline outline = handUI[i].GetComponent<Outline>();
            if (outline == null)
                outline = handUI[i].AddComponent<Outline>();

            if (cardDataIndex == currentIndex)
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