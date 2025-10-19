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
    private float moveInput;

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
        if (timeUntilNextAttack > 0f)
            timeUntilNextAttack -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        if (!isInCardMode && !isParrying)
            Move();
        else
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
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

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
            Move();

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
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        if (animator != null) animator.SetFloat("Speed", Mathf.Abs(moveInput));
        if (moveInput != 0f) transform.localScale = new Vector3(Mathf.Sign(moveInput), 1f, 1f);
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
            parryStack = 0;

            CardManager.Instance?.ShowRewardSelection();
        }
    }

    public void OnMissParry()
    {
        parryStack = 0;
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
            }
        });

        yield return new WaitUntil(() => !CardSelectionActive);

        awaitingReward = false;
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