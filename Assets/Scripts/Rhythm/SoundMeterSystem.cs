using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SoundMeterSystem : MonoBehaviour
{
    public static SoundMeterSystem Instance;

    public Transform player;

    [Header("Player HP (0-100)")]
    public Image hpImage;
    public TextMeshProUGUI hpText;
    public Sprite[] hpLevelSprites = new Sprite[11];

    public int currentSoundPlayerHP = 0;
    public int maxSoundPlayerHP = 100;
    public float maxSoundDetector = 10f;
    public float decayRate = 0f;
    public bool isGameOver = false;

    [Header("Detected Sound (0-10)")]
    public Image detectedSoundImage;
    public TextMeshProUGUI detectedSoundText;
    public Sprite[] soundLevelSprites = new Sprite[11];

    public float detectRadius = 5f;
    private int currentDetectedLevel = 0;
    public int maxDetectedSoundLevel = 10;

    private bool isVisible = false;

    void Awake()
    {
        Instance = this;

        if (hpImage != null)
            hpImage.gameObject.SetActive(true);
        if (hpText != null)
            hpText.gameObject.SetActive(true);

        if (detectedSoundImage != null)
            detectedSoundImage.gameObject.SetActive(false);
        if (detectedSoundText != null)
            detectedSoundText.gameObject.SetActive(false);

        UpdateAccumulatedUI();
        UpdateDetectedUI();
    }

    void Update()
    {
        if (isVisible && player != null)
        {
            CalculateDetectedSound();
            UpdateDetectedUI();
        }

        if (decayRate > 0 && !isGameOver)
        {
            currentSoundPlayerHP -= Mathf.FloorToInt(decayRate * Time.deltaTime);
            currentSoundPlayerHP = Mathf.Clamp(currentSoundPlayerHP, 0, maxSoundPlayerHP);
            UpdateAccumulatedUI();
        }

        if (CardFusionSystem.Instance != null && PlayerController.Instance != null && !PlayerController.Instance.isInCardMode)
        {
            foreach (var slot in CardFusionSystem.Instance.slots)
            {
                if (slot.slotType == CardSlotType.SilenceGearSlot && slot.currentCard != null && slot.remainingDuration > 0f)
                {
                    slot.remainingDuration -= Time.deltaTime;

                    if (slot.remainingDuration <= 0f)
                    {
                        if (slot.slotTransform != null && slot.slotTransform.childCount > 0)
                            Destroy(slot.slotTransform.GetChild(0).gameObject);

                        slot.currentCard = null;
                    }
                }
            }
        }
    }

    public void ToggleMeter()
    {
        isVisible = !isVisible;

        if (detectedSoundImage != null)
            detectedSoundImage.gameObject.SetActive(isVisible);
        if (detectedSoundText != null)
            detectedSoundText.gameObject.SetActive(isVisible);

        if (isVisible)
        {
            CalculateDetectedSound();
            UpdateDetectedUI();
        }
    }

    void CalculateDetectedSound()
    {
        if (player == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(player.position, detectRadius);
        int totalSound = 0;

        foreach (var hit in hits)
        {
            SoundObstacle obstacle = hit.GetComponent<SoundObstacle>();
            if (obstacle != null)
                totalSound += obstacle.soundValue;
        }

        currentDetectedLevel = Mathf.Clamp(totalSound, 0, maxDetectedSoundLevel);
    }

    public void AddSound(float amount)
    {
        if (isGameOver) return;

        float finalSoundAmount = amount;
        float silenceBuff = 0f;

        if (CardFusionSystem.Instance != null)
        {
            silenceBuff = CardFusionSystem.Instance.GetSilenceBuff();
        }

        finalSoundAmount = Mathf.Max(0f, amount - silenceBuff);

        currentSoundPlayerHP += Mathf.RoundToInt(finalSoundAmount);
        currentSoundPlayerHP = Mathf.Clamp(currentSoundPlayerHP, 0, maxSoundPlayerHP);
        UpdateAccumulatedUI();

        if (currentSoundPlayerHP >= maxSoundPlayerHP)
            TriggerGameOver();
    }

    void UpdateDetectedUI()
    {
        int spriteIndex = currentDetectedLevel;

        if (detectedSoundImage != null && soundLevelSprites != null && soundLevelSprites.Length > spriteIndex)
        {
            detectedSoundImage.sprite = soundLevelSprites[spriteIndex];
        }

        if (detectedSoundText != null)
        {
            detectedSoundText.text = $"{spriteIndex}";
        }
    }

    void UpdateAccumulatedUI()
    {
        int hpSpriteIndex = Mathf.FloorToInt((float)currentSoundPlayerHP / 10f);

        hpSpriteIndex = Mathf.Clamp(hpSpriteIndex, 0, 10);

        if (hpImage != null && hpLevelSprites != null && hpLevelSprites.Length > hpSpriteIndex)
        {
            hpImage.sprite = hpLevelSprites[hpSpriteIndex];
        }

        if (hpText != null)
        {
            hpText.text = $"{currentSoundPlayerHP}";
        }
    }

    void TriggerGameOver()
    {
        isGameOver = true;

        UpdateAccumulatedUI();

        if (PlayerController.Instance != null)
            PlayerController.Instance.enabled = false;

        if (GameResult.Instance != null)
        {
            GameResult.Instance.HandleLose();
        }
    }

    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(player.position, detectRadius);
        }
    }
}