using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance;

    [Header("Movement")]
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    public float moveInput;
    public bool canMove = true;

    [Header("Attack")]
    public Collider2D attackHitbox;
    public float defaultAttackDamage = 1f;
    public float defaultSilenceDamage = 0f;
    public float attackCooldown = 0.5f;
    private float timeUntilNextAttack = 0f;

    [Header("Backtracking Control")]
    public float backtrackTimeLimit = 2.0f;
    private float backtrackTimer = 0f;
    private bool isMovingForward = true;

    [Header("Other")]
    public bool isInCardMode = false;
    public bool isAttacking = false;
    public bool isParrying = false;
    public Animator animator;
    public GameObject soundMeterUI;
    public CardManager cardManager;
    public RhythmSystem rhythmSystem;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (soundMeterUI != null) soundMeterUI.SetActive(false);
        if (attackHitbox != null) attackHitbox.enabled = false;
    }

    void Update()
    {
        HandleInput();
        MoveWhenIdle();
        UpdateBacktrackTimer();
        if (timeUntilNextAttack > 0f)
            timeUntilNextAttack -= Time.deltaTime;
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            isInCardMode = !isInCardMode;
            if (isInCardMode)
                CardFusionSystem.Instance?.OpenCardMode();
            else
                CardFusionSystem.Instance?.CloseCardMode();
            return;
        }

        if (Input.GetKeyDown(KeyCode.V))
            SoundMeterSystem.Instance?.ToggleMeter();

        if (isInCardMode) return;

        if (canMove)
        {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
                Move();
        }

        if (Input.GetKeyDown(KeyCode.Space))
            AttackWithWeapon();

        if (Input.GetKeyDown(KeyCode.UpArrow)) TryParryWith(Direction.Up);
        if (Input.GetKeyDown(KeyCode.DownArrow)) TryParryWith(Direction.Down);
        if (Input.GetKeyDown(KeyCode.LeftArrow)) TryParryWith(Direction.Left);
        if (Input.GetKeyDown(KeyCode.RightArrow)) TryParryWith(Direction.Right);
    }

    void Move()
    {
        float m = 0f;
        if (Input.GetKey(KeyCode.A)) m = -1f;
        if (Input.GetKey(KeyCode.D)) m = 1f;
        moveInput = m;

        if (moveInput > 0) isMovingForward = true;
        else if (moveInput < 0) isMovingForward = false;

        if (GroundController.Instance != null)
        {
            GroundController.Instance.SetMovement(moveInput * moveSpeed);
        }

        if (animator != null) animator.SetFloat("Speed", Mathf.Abs(moveInput));

        if (moveInput != 0f)
        {
            Vector3 localScale = transform.localScale;
            localScale.x = Mathf.Abs(localScale.x) * (moveInput > 0 ? 1 : -1);
            transform.localScale = localScale;
        }
    }

    void MoveWhenIdle()
    {
        if (!canMove)
        {
            if (GroundController.Instance != null)
            {
                GroundController.Instance.SetMovement(0f);
            }
            if (animator != null) animator.SetFloat("Speed", 0f);
            return;
        }

        bool isMovingInput = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D);

        if (!isMovingInput)
        {
            if (GroundController.Instance != null && GroundController.Instance.currentMoveSpeed != 0f)
            {
                GroundController.Instance.SetMovement(0f);
            }

            if (animator != null) animator.SetFloat("Speed", 0f);
        }
    }

    void UpdateBacktrackTimer()
    {
        if (isInCardMode) return;

        if (isMovingForward || Input.GetKey(KeyCode.D))
        {
            backtrackTimer = 0f;
            canMove = true;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            backtrackTimer += Time.deltaTime;

            if (backtrackTimer >= backtrackTimeLimit)
            {
                GroundController.Instance?.SpawnWall();
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        EnemyController enemy = collision.gameObject.GetComponent<EnemyController>();

        if (enemy != null)
        {
            Vector2 pushDirection = (transform.position - enemy.transform.position).normalized;
            float pushForce = 5f;

            rb.AddForce(pushDirection * pushForce, ForceMode2D.Force);
        }

        if (GroundController.Instance != null && GroundController.Instance.currentWall != null &&
            collision.gameObject == GroundController.Instance.currentWall)
        {
            canMove = false;
            GroundController.Instance.SetMovement(0f);
            if (animator != null) animator.SetFloat("Speed", 0f);
        }
    }

    void AttackWithWeapon()
    {
        if (timeUntilNextAttack > 0f) return;
        timeUntilNextAttack = attackCooldown;

        float attackDamage = defaultAttackDamage;
        float attackSound = defaultSilenceDamage;

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

    [Header("Perfect Parry Stack")]
    public int parryStack = 0;
    public int requiredParryStacks = 3;
    private bool awaitingReward = false;
    private EnemyController lastEnemyHit;

    public void OnPerfectParry(EnemyController enemy)
    {
        if (enemy == null) return;
        if (awaitingReward) return;

        parryStack++;
        lastEnemyHit = enemy;

        if (parryStack >= requiredParryStacks)
        {
            StartCoroutine(WaitForEnemyDeathAndOpenReward());
        }
    }

    private IEnumerator WaitForEnemyDeathAndOpenReward()
    {
        awaitingReward = true;

        if (lastEnemyHit != null)
        {
            yield return new WaitUntil(() => lastEnemyHit == null || lastEnemyHit.Equals(null));
        }

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

                parryStack = 0;

                awaitingReward = false;

                CardSelectionActive = false;
            }
        });

        yield return new WaitUntil(() => !CardSelectionActive);
    }

    private bool CardSelectionActive = false;
    private List<Card> randomChoices = new List<Card>();
    private int selectedIndex = 0;

    private void OpenNewCardSelection()
    {
        if (cardManager == null) return;

        randomChoices.Clear();
        Card typeA = cardManager.GetRandomCardByType(CardType.TypeA);
        Card typeB = cardManager.GetRandomCardByType(CardType.TypeB);
        Card any = cardManager.GetRandomCard();

        if (typeA != null) randomChoices.Add(typeA);
        if (typeB != null) randomChoices.Add(typeB);
        if (any != null) randomChoices.Add(any);

        selectedIndex = 0;
        CardSelectionActive = true;
        StartCoroutine(HandleCardSelection());
    }

    private IEnumerator HandleCardSelection()
    {
        CardFusionSystem.Instance?.handPanel.SetActive(true);
        CardFusionSystem.Instance?.slotsPanel.SetActive(false);

        CardFusionSystem.Instance?.cardManager.hand.AddRange(randomChoices);

        CardFusionSystem.Instance?.RefreshHandUI();

        while (CardSelectionActive)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
                selectedIndex = (selectedIndex - 1 + randomChoices.Count) % randomChoices.Count;
            if (Input.GetKeyDown(KeyCode.RightArrow))
                selectedIndex = (selectedIndex + 1) % randomChoices.Count;

            CardFusionSystem.Instance?.MoveSelection(selectedIndex - CardFusionSystem.Instance.cardManager.hand.IndexOf(CardFusionSystem.Instance.cardManager.hand[selectedIndex]));

            yield return null;
        }
    }

    private void ChooseRewardCard(Card chosen)
    {
        if (cardManager == null || chosen == null) return;

        cardManager.hand.Add(chosen.Clone());
        CardFusionSystem.Instance?.RefreshHandUI();
        Debug.Log($"Received new card: {chosen.cardName}");
    }
}