using UnityEngine;
using System.Collections.Generic;

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

    private float timeUntilNextAttack;
    private Rigidbody2D rb;

    private bool isColliding = false;

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
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Enemy"))
        {
            isColliding = true;
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Enemy"))
        {
            isColliding = true;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Enemy"))
        {
            isColliding = false;
        }
    }

    void FixedUpdate()
    {
        if (player == null || PlayerController.Instance == null) return;

        if (PlayerController.Instance.isInStealth)
        {
            timeUntilNextAttack = attackCooldown;
            if (RhythmSystem.Instance != null)
            {
                RhythmSystem.Instance.DestroyIconsOfEnemy(this);
                RhythmSystem.Instance.EndCombo(false);
            }
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
        }
        else if (deltaX <= detectRangeX && deltaY <= detectRangeY)
        {
            if (timeUntilNextAttack > 0)
                timeUntilNextAttack -= Time.fixedDeltaTime;
            else
            {
                StartRhythmAttack();
                timeUntilNextAttack = attackCooldown;
            }
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