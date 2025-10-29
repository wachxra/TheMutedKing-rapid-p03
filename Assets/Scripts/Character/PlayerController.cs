using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance;

    [Header("Movement")]
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    public float moveInput;
    public bool canMove = true;
    public float dashDistance = 5f;
    public float dashCooldown = 1f;
    private float timeUntilNextDash = 0f;

    [Header("Attack")]
    public Collider2D attackHitbox;
    public float defaultAttackDamage = 1f;
    public float defaultSilenceDamage = 0f;
    public float attackCooldown = 0.5f;
    private float timeUntilNextAttack = 0f;

    [Header("Other")]
    public bool isInCardMode = false;
    public bool isAttacking = false;
    public bool isParrying = false;
    public Animator animator;
    public GameObject soundMeterUI;
    public CardManager cardManager;
    public RhythmSystem rhythmSystem;

    [Header("UI References")]
    public ParryStackUI parryStackUI;

    [Header("Perfect Parry Combo Reward")]
    public bool healPlayerHPOnPerfectCombo = true;
    public bool healPlayerSoundOnPerfectCombo = false;
    public int perfectComboHealAmount = 5;

    [Header("Dash Settings")]
    public LayerMask dashStopLayer;

    [Header("Audio")]
    public AudioSource bgmSource;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);

        if (cardManager == null)
            cardManager = CardManager.Instance;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (soundMeterUI != null) soundMeterUI.SetActive(false);
        if (attackHitbox != null) attackHitbox.enabled = false;

        if (bgmSource == null)
            bgmSource = Camera.main.GetComponent<AudioSource>();

        if (parryStackUI == null)
            parryStackUI = FindFirstObjectByType<ParryStackUI>();

        if (parryStackUI != null)
        {
            parryStackUI.playerController = this;
            parryStackUI.UpdateUI();
        }

        if (stealthSlider != null)
            stealthSlider.gameObject.SetActive(false);
    }

    void Update()
    {
        HandleInput();
        if (timeUntilNextAttack > 0f)
            timeUntilNextAttack -= Time.deltaTime;
        if (timeUntilNextDash > 0f)
            timeUntilNextDash -= Time.deltaTime;

        if (isInStealth)
        {
            UpdateStealthTimer();
        }
    }

    void FixedUpdate()
    {
        Move();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (CardFusionSystem.Instance != null && CardFusionSystem.Instance.isInRewardMode)
                return;

            isInCardMode = !isInCardMode;

            AudioManager.Instance?.PlaySFX("Toggle");

            if (isInCardMode)
            {
                CardFusionSystem.Instance?.OpenCardMode();
            }
            else
            {
                CardFusionSystem.Instance?.CloseCardMode();
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            AudioManager.Instance?.PlaySFX("Toggle");

            SoundMeterSystem.Instance?.ToggleMeter();
        }

        if (isInCardMode)
        {
            moveInput = 0f;
            if (animator != null) animator.SetFloat("Speed", 0f);
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && canMove && timeUntilNextDash <= 0f)
        {
            if (RhythmSystem.Instance != null && RhythmSystem.Instance.activeIcons.Count > 0)
            {
                TryDash();
            }
            else
            {
                Debug.Log("Cannot dash outside Rhythm Mode!");
            }
            return;
        }

        if (canMove)
        {
            moveInput = 0f;
            if (Input.GetKey(KeyCode.A)) moveInput = -1f;
            if (Input.GetKey(KeyCode.D)) moveInput = 1f;
        }
        else
        {
            moveInput = 0f;
        }

        if (Input.GetKeyDown(KeyCode.Space))
            AttackWithWeapon();

        if (Input.GetKeyDown(KeyCode.UpArrow)) TryParryWith(Direction.Up);
        if (Input.GetKeyDown(KeyCode.DownArrow)) TryParryWith(Direction.Down);
        if (Input.GetKeyDown(KeyCode.LeftArrow)) TryParryWith(Direction.Left);
        if (Input.GetKeyDown(KeyCode.RightArrow)) TryParryWith(Direction.Right);

        if (Input.GetKeyDown(KeyCode.Q) && !isInStealth)
        {
            TryEnterStealthMode();
        }

        if (moveInput != 0f)
        {
            Vector3 localScale = transform.localScale;
            localScale.x = Mathf.Abs(localScale.x) * (moveInput > 0 ? 1 : -1);
            transform.localScale = localScale;
        }

        if (animator != null) animator.SetFloat("Speed", Mathf.Abs(moveInput));
    }

    void Move()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    void TryDash()
    {
        if (timeUntilNextDash > 0f) return;

        if (RhythmSystem.Instance == null || RhythmSystem.Instance.activeIcons.Count == 0)
        {
            Debug.Log("Cannot dash outside Rhythm Mode!");
            return;
        }

        float dashDirection = transform.localScale.x > 0 ? 1f : -1f;
        Vector2 directionVector = new Vector2(dashDirection, 0f);

        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionVector, dashDistance, dashStopLayer);

        Vector3 targetPosition;
        float actualDashDistance;

        if (hit.collider != null && hit.collider.CompareTag("Gate"))
        {
            actualDashDistance = hit.distance - (GetComponent<Collider2D>()?.bounds.extents.x ?? 0.5f) - 0.05f;

            if (actualDashDistance <= 0f)
            {
                return;
            }

            targetPosition = transform.position + new Vector3(dashDirection * actualDashDistance, 0f, 0f);
        }
        else
        {
            targetPosition = transform.position + new Vector3(dashDirection * dashDistance, 0f, 0f);
        }

        timeUntilNextDash = dashCooldown;
        AudioManager.Instance?.PlaySFX("Dash");
        transform.position = targetPosition;
        RhythmSystem.Instance?.CheckUltimateAfterDash(transform.position);
    }

    void AttackWithWeapon()
    {
        if (timeUntilNextAttack > 0f) return;
        timeUntilNextAttack = attackCooldown;

        float attackDamage = defaultAttackDamage;
        float attackSound = defaultSilenceDamage;

        if (isInStealth)
        {
            attackDamage = 9999f;
        }

        if (CardFusionSystem.Instance != null)
        {
            foreach (var slot in CardFusionSystem.Instance.slots)
            {
                if (slot.slotType == CardSlotType.WeaponSlot && slot.currentCard != null && slot.remainingUses > 0)
                {
                    attackDamage = slot.currentCard.damage;
                    attackSound = slot.currentCard.silence;

                    slot.remainingUses--;
                    if (slot.remainingUses <= 0)
                    {
                        if (slot.slotTransform != null && slot.slotTransform.childCount > 0)
                            Destroy(slot.slotTransform.GetChild(0).gameObject);

                        slot.currentCard = null;
                    }
                    break;
                }
            }
        }

        AudioManager.Instance?.PlaySFX("Attack");

        if (attackHitbox != null)
            StartCoroutine(HandleAttackHitbox(attackDamage, attackSound));
        else
            SoundMeterSystem.Instance?.AddSound(Mathf.Max(0f, attackSound));

        if (animator != null)
            animator.SetTrigger("Attack");
    }

    private IEnumerator HandleAttackHitbox(float damage, float attackSound)
    {
        attackHitbox.enabled = true;

        Collider2D[] hits = Physics2D.OverlapBoxAll(attackHitbox.bounds.center, attackHitbox.bounds.size, 0f);
        foreach (var hit in hits)
        {
            EnemyController enemy = hit.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }

        SoundMeterSystem.Instance?.AddSound(Mathf.Max(0f, attackSound));

        yield return new WaitForSeconds(0.05f);
        attackHitbox.enabled = false;
    }

    void TryParryWith(Direction dir)
    {
        if (isParrying) return;
        isParrying = true;

        if (animator != null)
        {
            animator.SetTrigger("Parry");
        }

        if (RhythmSystem.Instance != null) RhythmSystem.Instance.TryParry(dir);

        Invoke(nameof(ResetParry), 0.25f);
    }

    void ResetParry()
    {
        isParrying = false;
    }

    public void ExitCardMode()
    {
        if (isInCardMode)
        {
            isInCardMode = false;
            CardFusionSystem.Instance?.CloseCardMode();
        }
    }

    public bool awaitingReward = false;
    private EnemyController lastEnemyHit;
    private bool CardSelectionActive = false;
    private List<Card> randomChoices = new List<Card>();

    public void OnPerfectParry(EnemyController enemy)
    {
        if (enemy == null) return;
        parryStackUI?.AddStack();
        lastEnemyHit = enemy;
        RhythmSystem.Instance?.NotifyPerfectParry(enemy);
    }

    public void ApplyPerfectComboReward()
    {
        if (SoundMeterSystem.Instance != null)
        {
            SoundMeterSystem.Instance.HealHPOrSound(
                perfectComboHealAmount,
                healPlayerHPOnPerfectCombo,
                healPlayerSoundOnPerfectCombo
            );
            Debug.Log($"Perfect Combo Reward Applied: Heal by {perfectComboHealAmount} (Noise Reduced).");
        }
    }

    [Header("Stealth Settings")]
    public bool isInStealth = false;
    public float stealthDuration = 5f;
    private float stealthTimer = 0f;
    public SpriteRenderer spriteRenderer;
    public Slider stealthSlider;

    void TryEnterStealthMode()
    {
        if (parryStackUI == null) return;
        if (parryStackUI.parryStack < parryStackUI.requiredParryStacks) return;

        StartCoroutine(EnterStealthMode());
    }

    IEnumerator EnterStealthMode()
    {
        isInStealth = true;
        stealthTimer = stealthDuration;

        bgmSource?.Pause();
        AudioManager.Instance?.StopAllSFX();

        if (stealthSlider != null)
        {
            stealthSlider.maxValue = stealthDuration;
            stealthSlider.value = stealthDuration;
            stealthSlider.gameObject.SetActive(true);
        }

        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 0.4f;
            spriteRenderer.color = c;
        }

        Debug.Log("Entered Stealth Mode");

        yield return null;
    }

    void UpdateStealthTimer()
    {
        stealthTimer -= Time.deltaTime;

        if (stealthSlider != null)
            stealthSlider.value = stealthTimer;

        if (stealthTimer <= 0f)
        {
            ExitStealthMode();
        }
    }

    void ExitStealthMode()
    {
        isInStealth = false;

        parryStackUI?.ResetStack();

        if (stealthSlider != null)
            stealthSlider.gameObject.SetActive(false);

        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }

        bgmSource?.UnPause();

        Debug.Log("Exited Stealth Mode");
    }

    public IEnumerator OpenRewardOnEnemyDeath()
    {
        awaitingReward = true;

        yield return new WaitForSeconds(0.2f);

        randomChoices.Clear();
        Card typeA = cardManager.GetRandomCardByType(CardType.TypeA);
        Card typeB = cardManager.GetRandomCardByType(CardType.TypeB);
        Card any = cardManager.GetRandomCard();

        if (typeA != null) randomChoices.Add(typeA);
        if (typeB != null) randomChoices.Add(typeB);
        if (any != null) randomChoices.Add(any);

        CardSelectionActive = true;
        NewRewardCard.Instance.Show(randomChoices, (chosen) =>
        {
            if (chosen != null)
            {
                cardManager.hand.Add(chosen.Clone());
                CardFusionSystem.Instance?.RefreshHandUI();
                Debug.Log($"Received new card: {chosen.cardName}");

                awaitingReward = false;
                CardSelectionActive = false;
            }
        });

        yield return new WaitUntil(() => !CardSelectionActive);
    }
}