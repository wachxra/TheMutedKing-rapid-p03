using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP = 50;
    public int currentHP;
    public int damage = 5;

    [Header("Rhythm Settings")]
    public int minHitsInCombo = 3;
    public int maxHitsInCombo = 8;
    public float minTravelTime = 1f;
    public float maxTravelTime = 2.5f;
    public float attackCooldown = 3f;

    [Header("Movement & Detection")]
    public float moveSpeed = 2f;
    public float detectRange = 5f;
    public float spawnY = 0f;
    [HideInInspector] public Transform player;

    [Header("Other")]
    public Animator enemyAnimator;

    private float timeUntilNextAttack;
    private Rigidbody2D rb;

    void Awake()
    {
        currentHP = maxHP;
        timeUntilNextAttack = attackCooldown;

        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (enemyAnimator == null)
            enemyAnimator = GetComponent<Animator>();
    }

    void FixedUpdate()
    {
        if (player == null || PlayerController.Instance == null) return;

        if (player.position.x > transform.position.x)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (player.position.x < transform.position.x)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }

        if (PlayerController.Instance.isInStealth)
        {
            timeUntilNextAttack = attackCooldown;
            if (RhythmSystem.Instance != null)
            {
                RhythmSystem.Instance.DestroyIconsOfEnemy(this);
                RhythmSystem.Instance.EndCombo(false);
            }
            if (enemyAnimator != null) enemyAnimator.SetFloat("Speed", 0f);
            return;
        }

        float deltaX = Mathf.Abs(player.position.x - transform.position.x);
        float deltaY = Mathf.Abs(player.position.y - transform.position.y);

        float detectRangeX = detectRange;
        float detectRangeY = 2f;

        if (deltaX > detectRangeX)
        {
            float dirX = player.position.x > transform.position.x ? 1f : -1f;
            Vector2 newPos = rb.position + new Vector2(dirX * moveSpeed * Time.fixedDeltaTime, 0f);
            rb.MovePosition(newPos);

            if (enemyAnimator != null) enemyAnimator.SetFloat("Speed", moveSpeed);
        }
        else if (deltaX <= detectRangeX && deltaY <= detectRangeY)
        {
            if (enemyAnimator != null) enemyAnimator.SetFloat("Speed", 0f);

            if (timeUntilNextAttack > 0)
                timeUntilNextAttack -= Time.fixedDeltaTime;
            else
            {
                StartRhythmAttack();
                timeUntilNextAttack = attackCooldown;
            }
        }
        else
        {
            if (enemyAnimator != null) enemyAnimator.SetFloat("Speed", 0f);
        }
    }

    void StartRhythmAttack()
    {
        List<BeatData> combo = new List<BeatData>();
        int numHits = Random.Range(minHitsInCombo, maxHitsInCombo + 1);

        for (int i = 0; i < numHits; i++)
        {
            Direction randomDir = (Direction)Random.Range(0, 5);

            float travelDur = Random.Range(minTravelTime, maxTravelTime);

            if (randomDir == Direction.Ultimate && RhythmSystem.Instance != null)
            {
                travelDur = RhythmSystem.Instance.ultimateTravelDuration;
            }

            BeatData beat = new BeatData
            {
                requiredDirection = randomDir,
                travelDuration = travelDur
            };
            combo.Add(beat);
        }

        if (RhythmSystem.Instance != null)
            RhythmSystem.Instance.StartEnemyComboFromList(combo, this);
    }

    public void TakeDamage(float dmg)
    {
        int intDmg = Mathf.RoundToInt(dmg);
        currentHP -= intDmg;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        if (currentHP <= 0)
        {
            if (RhythmSystem.Instance != null)
            {
                RhythmSystem.Instance.DestroyIconsOfEnemy(this);
                RhythmSystem.Instance.EndCombo(false);
            }

            if (PlayerController.Instance != null && !PlayerController.Instance.awaitingReward)
            {
                PlayerController.Instance.StartCoroutine(PlayerController.Instance.OpenRewardOnEnemyDeath());
            }

            Destroy(gameObject);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
#endif
}