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
            AudioManager.Instance?.PlaySFX("Card");
            UpdateSlotHighlight();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            selectedIndex = (selectedIndex + 1) % currentCards.Count;
            AudioManager.Instance?.PlaySFX("Card");
            UpdateSlotHighlight();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            ChooseCard(currentCards[selectedIndex]);
        }
    }

    public void Show(List<Card> cardChoices, System.Action<Card> callback)
    {
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
                    rt.sizeDelta = new Vector2(1200, 800);
                }

                Image img = cardObj.GetComponent<Image>();
                Text txt = cardObj.GetComponentInChildren<Text>();

                if (img != null && currentCards[i].cardSprite != null)
                {
                    img.sprite = currentCards[i].cardSprite;
                    img.preserveAspect = false;
                }
                if (txt != null)
                    txt.text = currentCards[i].cardName;
            }
        }

        selectedIndex = 0;
        UpdateSlotHighlight();

        panel.SetActive(true);
        Time.timeScale = 0f;

        if (CardFusionSystem.Instance != null)
        {
            CardFusionSystem.Instance.isInRewardMode = true;
            CardFusionSystem.Instance.OpenHandPanel(true);
            CardFusionSystem.Instance.RefreshHandUI(true);
        }
    }

    private void UpdateSlotHighlight()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            Image slotImage = slots[i].GetComponent<Image>();
            if (slotImage != null)
                slotImage.color = (i == selectedIndex) ? Color.yellow : new Color(0.3f, 0.3f, 0.3f);
        }
    }

    void ChooseCard(Card card)
    {
        panel.SetActive(false);
        Time.timeScale = 1f;

        AudioManager.Instance?.PlaySFX("Slot");

        if (CardFusionSystem.Instance != null)
        {
            CardFusionSystem.Instance.isInRewardMode = false;
            CardFusionSystem.Instance.CloseHandPanel();
        }

        onCardChosen?.Invoke(card);
        currentCards.Clear();
    }
}