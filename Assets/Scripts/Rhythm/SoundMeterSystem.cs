using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SoundMeterSystem : MonoBehaviour
{
    public static SoundMeterSystem Instance;

    public Transform player;
    public Slider playerHPSlider;

    [Header("Player HP (0-100)")]
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

        if (playerHPSlider != null)
        {
            playerHPSlider.minValue = 0f;
            playerHPSlider.maxValue = maxSoundPlayerHP;
            playerHPSlider.value = 0f;
        }

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

            CalculateSoundAroundPlayer();
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

    void CalculateSoundAroundPlayer()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(player.position, detectRadius);
        float totalSound = 0f;
        foreach (var hit in hits)
        {
            SoundObstacle obstacle = hit.GetComponent<SoundObstacle>();
            if (obstacle != null)
                totalSound += obstacle.soundValue;
        }
    }

    public void AddSound(float amount)
    {
        if (isGameOver) return;

        currentSoundPlayerHP += Mathf.RoundToInt(amount);
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
        if (playerHPSlider != null)
            playerHPSlider.value = currentSoundPlayerHP;
    }

    void TriggerGameOver()
    {
        isGameOver = true;
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