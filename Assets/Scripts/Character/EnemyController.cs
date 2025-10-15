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

        Vector3 pos = transform.position;
        pos.y = spawnY;
        transform.position = pos;
    }

    void FixedUpdate()
    {
        if (player == null) return;

        float distance = Mathf.Abs(player.position.x - transform.position.x);

        if (distance > detectRange)
        {
            float dirX = player.position.x > transform.position.x ? 1f : -1f;
            Vector2 newPos = rb.position + new Vector2(dirX * moveSpeed * Time.fixedDeltaTime, 0f);
            rb.MovePosition(newPos);
        }
        else
        {
            if (timeUntilNextAttack > 0)
            {
                timeUntilNextAttack -= Time.deltaTime;
            }
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
            BeatData beat = new BeatData
            {
                requiredDirection = (Direction)Random.Range(0, 4),
                travelDuration = Random.Range(minTravelTime, maxTravelTime)
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
                RhythmSystem.Instance.EndCombo(false);
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