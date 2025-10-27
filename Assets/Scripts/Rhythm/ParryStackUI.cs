using UnityEngine;
using UnityEngine.UI;

public class ParryStackUI : MonoBehaviour
{
    public static ParryStackUI Instance;

    [Header("UI References")]
    public Image stackImage;
    public Sprite[] stackSprites;

    [Header("Parry Stack Settings")]
    public int parryStack = 0;
    public int requiredParryStacks = 12;
    public int parriesPerSprite = 3;

    [HideInInspector]
    public PlayerController playerController;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();

        UpdateUI();
    }

    public void AddStack()
    {
        parryStack++;
        UpdateUI();

        if (parryStack >= requiredParryStacks)
        {
            
        }
    }

    public void ResetStack()
    {
        parryStack = 0;
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (stackImage == null || stackSprites == null || stackSprites.Length == 0) return;

        int maxSpriteLevel = stackSprites.Length - 1;

        int calculatedIndex = 0;
        if (parriesPerSprite > 0)
        {
            calculatedIndex = parryStack / parriesPerSprite;
        }

        int spriteIndex = Mathf.Clamp(calculatedIndex, 0, maxSpriteLevel);

        stackImage.sprite = stackSprites[spriteIndex];
    }
}