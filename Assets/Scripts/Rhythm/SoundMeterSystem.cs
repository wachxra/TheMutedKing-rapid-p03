using UnityEngine;
using UnityEngine.UI;

public class SoundMeterSystem : MonoBehaviour
{
    public static SoundMeterSystem Instance;

    public Transform player;
    public Slider soundDetectorSlider;
    public Slider playerHPSlider;

    public float detectRadius = 5f;
    public int currentSound = 0;
    public int maxSoundPlayerHP = 100;
    public float maxSoundDetector = 10f;
    public float decayRate = 0f;
    public bool isGameOver = false;

    private bool isVisible = false;

    void Awake()
    {
        Instance = this;
        if (soundDetectorSlider != null)
        {
            soundDetectorSlider.gameObject.SetActive(false);
            soundDetectorSlider.minValue = 0f;
            soundDetectorSlider.maxValue = maxSoundDetector;
            soundDetectorSlider.value = 0f;
        }

        if (playerHPSlider != null)
        {
            playerHPSlider.minValue = 0f;
            playerHPSlider.maxValue = maxSoundPlayerHP;
            playerHPSlider.value = 0f;
        }
    }

    void Update()
    {
        if (isVisible && player != null)
            CalculateSoundAroundPlayer();

        if (decayRate > 0 && !isGameOver)
        {
            currentSound -= Mathf.FloorToInt(decayRate * Time.deltaTime);
            currentSound = Mathf.Clamp(currentSound, 0, maxSoundPlayerHP);
            UpdateUI();
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
        if (soundDetectorSlider != null)
            soundDetectorSlider.gameObject.SetActive(isVisible);
        if (isVisible)
            CalculateSoundAroundPlayer();
    }

    void CalculateSoundAroundPlayer()
    {
        if (player == null || soundDetectorSlider == null) return;
        Collider2D[] hits = Physics2D.OverlapCircleAll(player.position, detectRadius);
        float totalSound = 0f;
        foreach (var hit in hits)
        {
            SoundObstacle obstacle = hit.GetComponent<SoundObstacle>();
            if (obstacle != null)
                totalSound += obstacle.soundValue;
        }
        soundDetectorSlider.value = Mathf.Clamp(totalSound, 0f, maxSoundDetector);
    }

    public void AddSound(float amount)
    {
        if (isGameOver) return;

        currentSound += Mathf.RoundToInt(amount);
        currentSound = Mathf.Clamp(currentSound, 0, maxSoundPlayerHP);
        UpdateUI();
        if (currentSound >= maxSoundPlayerHP)
            TriggerGameOver();
    }

    void UpdateUI()
    {
        if (playerHPSlider != null)
            playerHPSlider.value = currentSound;
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
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(player.position, detectRadius);
        }
    }
}