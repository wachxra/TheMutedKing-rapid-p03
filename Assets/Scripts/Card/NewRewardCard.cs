using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class NewRewardCard : MonoBehaviour
{
    public static NewRewardCard Instance;

    [Header("Panel References")]
    public GameObject panel;
    public Transform[] slots;
    public GameObject cardButtonPrefab;

    private List<Card> currentCards = new List<Card>();
    private int selectedIndex = 0;
    private System.Action<Card> onCardChosen;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    void Update()
    {
        if (!panel.activeSelf || currentCards.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            selectedIndex = (selectedIndex - 1 + currentCards.Count) % currentCards.Count;
            UpdateSlotHighlight();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            selectedIndex = (selectedIndex + 1) % currentCards.Count;
            UpdateSlotHighlight();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            ChooseCard(currentCards[selectedIndex]);
        }
    }

    public void Show(List<Card> cardChoices, System.Action<Card> callback)
    {
        if (slots == null || slots.Length != 3 || cardButtonPrefab == null)
        {
            Debug.LogError("Slots or prefab not set correctly!");
            return;
        }

        if (cardChoices == null || cardChoices.Count == 0)
        {
            Debug.LogWarning("No cards to show");
            return;
        }

        onCardChosen = callback;
        currentCards.Clear();

        for (int i = 0; i < Mathf.Min(3, cardChoices.Count); i++)
            currentCards.Add(cardChoices[i]);

        for (int i = 0; i < slots.Length; i++)
        {
            foreach (Transform child in slots[i])
                Destroy(child.gameObject);

            if (i < currentCards.Count)
            {
                GameObject cardObj = Instantiate(cardButtonPrefab, slots[i]);

                RectTransform rt = cardObj.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.localScale = Vector3.one;
                    rt.localPosition = Vector3.zero;
                }

                Image img = cardObj.GetComponent<Image>();
                Text txt = cardObj.GetComponentInChildren<Text>();

                if (img != null && currentCards[i].cardSprite != null)
                {
                    img.sprite = currentCards[i].cardSprite;
                    img.preserveAspect = true;
                }
                if (txt != null)
                    txt.text = currentCards[i].cardName;
            }
        }

        selectedIndex = 0;
        UpdateSlotHighlight();

        panel.SetActive(true);
        Time.timeScale = 0f;
    }

    private void UpdateSlotHighlight()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            Image slotImage = slots[i].GetComponent<Image>();
            if (slotImage != null)
                slotImage.color = (i == selectedIndex) ? Color.yellow : Color.white;
        }
    }

    void ChooseCard(Card card)
    {
        panel.SetActive(false);
        Time.timeScale = 1f;
        onCardChosen?.Invoke(card);
        currentCards.Clear();
    }
}
