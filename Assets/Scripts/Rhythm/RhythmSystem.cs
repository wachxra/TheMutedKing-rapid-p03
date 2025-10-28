using UnityEngine;
using System.Collections.Generic;

public class RhythmSystem : MonoBehaviour
{
    public static RhythmSystem Instance;

    [Header("Beat UI References")]
    public Transform beatPanel;
    public GameObject beatIconPrefab;
    public RectTransform spawnPoint;
    public RectTransform endPoint;

    [Header("Trigger Settings")]
    public RectTransform triggerPoint;
    public float triggerDistance = 30f;

    [Header("Icon Sprites")]
    public Sprite upSprite;
    public Sprite downSprite;
    public Sprite leftSprite;
    public Sprite rightSprite;
    public Sprite ultimateSprite;

    [Header("Timing Settings")]
    public float perfectTimingWindow = 0.08f;
    public float minTravelTime = 1.0f;
    public float maxTravelTime = 2.5f;

    [Header("Ultimate Beat Settings")]
    public float dashRangeCheck = 1f;
    public float ultimateTravelDuration = 1.5f;
    public float ultimateSoundDamage = 30f;

    [Header("Enemy Combo Settings")]
    public int minHitsInCombo = 3;
    public int maxHitsInCombo = 8;
    public float minTimeBetweenHits = 0.3f;
    public float maxTimeBetweenHits = 1.0f;
    public float attackCooldown = 3f;

    private List<BeatData> activeCombo;
    private List<BeatIcon> activeIcons = new List<BeatIcon>();
    private float timeUntilNextAttack;
    private bool isSpawning = false;

    private int perfectParryCounter = 0;
    private int totalBeatsInCurrentCombo = 0;
    private bool isComboPerfect = true;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        timeUntilNextAttack = attackCooldown;
    }

    void Update()
    {
        for (int i = activeIcons.Count - 1; i >= 0; i--)
        {
            BeatIcon icon = activeIcons[i];
            if (icon == null)
            {
                activeIcons.RemoveAt(i);
                continue;
            }

            if (icon.enemy == null || icon.enemy.currentHP <= 0)
            {
                RemoveAndDestroyIcon(icon);
            }
        }
    }

    private Sprite GetSpriteForDirection(Direction dir)
    {
        return dir switch
        {
            Direction.Up => upSprite,
            Direction.Down => downSprite,
            Direction.Left => leftSprite,
            Direction.Right => rightSprite,
            Direction.Ultimate => ultimateSprite,
            _ => null
        };
    }

    public void StartEnemyComboFromList(List<BeatData> combo, EnemyController enemy)
    {
        perfectParryCounter = 0;
        totalBeatsInCurrentCombo = combo.Count;
        isComboPerfect = true;

        StartCoroutine(SpawnBeatsSequentially(combo, enemy));
    }

    private System.Collections.IEnumerator SpawnBeatsSequentially(List<BeatData> combo, EnemyController enemy)
    {
        if (isSpawning) yield break;
        isSpawning = true;

        activeCombo = combo;

        float triggerDistanceRatio = 0.5f;
        float safeMargin = 0.15f;

        for (int i = 0; i < combo.Count; i++)
        {
            BeatData beat = combo[i];

            if (beat.requiredDirection == Direction.Ultimate)
            {
                beat.travelDuration = ultimateTravelDuration;
                beat.ultimateMissSoundDamage = ultimateSoundDamage;
            }
            else
            {
                beat.travelDuration = Random.Range(minTravelTime, maxTravelTime);
            }

            GameObject go = Instantiate(beatIconPrefab, beatPanel);
            BeatIcon icon = go.GetComponent<BeatIcon>();

            if (icon == null)
            {
                Destroy(go);
                EndCombo(false);
                isSpawning = false;
                yield break;
            }

            Vector2 localSpawn, localTrigger, localEnd;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)beatPanel,
                RectTransformUtility.WorldToScreenPoint(null, spawnPoint.position),
                null, out localSpawn);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)beatPanel,
                RectTransformUtility.WorldToScreenPoint(null, triggerPoint.position),
                null, out localTrigger);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)beatPanel,
                RectTransformUtility.WorldToScreenPoint(null, endPoint.position),
                null, out localEnd);

            icon.Initialize(this, beat, localSpawn, localEnd, localTrigger, GetSpriteForDirection(beat.requiredDirection));
            icon.enemy = enemy;

            activeIcons.Add(icon);

            icon.StartMove();

            float timeUntilReachTrigger = beat.travelDuration * triggerDistanceRatio;
            float spawnDelay = timeUntilReachTrigger + safeMargin;

            yield return new WaitForSeconds(spawnDelay);
        }

        yield return new WaitUntil(() => activeIcons.Count == 0);
        EndCombo(true);
        isSpawning = false;
    }

    public void TryParry(Direction pressedDirection)
    {
        if (activeIcons.Count == 0) return;

        BeatIcon target = null;
        float closestDist = float.MaxValue;

        foreach (var icon in activeIcons)
        {
            if (icon == null || icon.hasBeenTriggered) continue;
            if (icon.requiredDirection == Direction.Ultimate) continue;

            float dist = Vector3.Distance(icon.RectTransform.localPosition, icon.TriggerPosition);
            if (dist < closestDist)
            {
                closestDist = dist;
                target = icon;
            }
        }

        if (target == null) return;

        if (target.IsWithinTrigger() && pressedDirection == target.requiredDirection)
        {
            Debug.Log("Perfect Parry!");
            AudioManager.Instance?.PlaySFX("PerfectParry");

            PlayerController.Instance?.OnPerfectParry(target.enemy);

        }
        else
        {
            Debug.Log("Miss Parry!");
            AudioManager.Instance?.PlaySFX("Attack");

            if (target.enemy != null)
                SoundMeterSystem.Instance?.AddSound(target.enemy.damage);

            isComboPerfect = false;
        }

        RemoveAndDestroyIcon(target);
    }

    public void NotifyPerfectParry(EnemyController enemy)
    {
        perfectParryCounter++;
    }

    public void NotifyIconMissed(BeatIcon icon)
    {
        if (activeIcons.Contains(icon))
        {
            if (!icon.IsWithinTrigger() && icon.enemy != null)
                SoundMeterSystem.Instance?.AddSound(icon.enemy.damage);

            isComboPerfect = false;

            RemoveAndDestroyIcon(icon);
        }
    }

    public void CheckUltimateAfterDash(Vector3 playerPosition)
    {
        for (int i = activeIcons.Count - 1; i >= 0; i--)
        {
            BeatIcon icon = activeIcons[i];
            if (icon == null || icon.requiredDirection != Direction.Ultimate || icon.hasBeenTriggered)
                continue;

            if (icon.IsWithinTrigger())
            {
                float distance = Vector3.Distance(playerPosition, icon.enemy.transform.position);

                if (distance > dashRangeCheck)
                {
                    AudioManager.Instance?.PlaySFX("PerfectParry");

                    NotifyPerfectParry(icon.enemy);
                    RemoveAndDestroyIcon(icon);
                }
                else
                {
                    AudioManager.Instance?.PlaySFX("Attack");
                    SoundMeterSystem.Instance?.AddSound(icon.ultimateMissDamage);

                    isComboPerfect = false;
                    RemoveAndDestroyIcon(icon);
                }
            }
        }
    }

    public void DestroyIconsOfEnemy(EnemyController enemy)
    {
        for (int i = activeIcons.Count - 1; i >= 0; i--)
        {
            if (activeIcons[i] != null && activeIcons[i].enemy == enemy)
            {
                Destroy(activeIcons[i].gameObject);
                activeIcons.RemoveAt(i);
            }
        }
    }

    private void RemoveAndDestroyIcon(BeatIcon icon)
    {
        if (icon == null) return;
        activeIcons.Remove(icon);
        icon.hasBeenTriggered = true;
        Destroy(icon.gameObject);
    }

    public void EndCombo(bool success)
    {
        Debug.Log(success ? "Combo Complete!" : "Combo Ended");

        if (success && isComboPerfect && perfectParryCounter == totalBeatsInCurrentCombo && totalBeatsInCurrentCombo > 0)
        {
            PlayerController.Instance?.ApplyPerfectComboReward();
            Debug.Log("PERFECT COMBO ACHIEVED! Player Healed.");
        }

        perfectParryCounter = 0;
        totalBeatsInCurrentCombo = 0;
        isComboPerfect = true;

        activeCombo = null;

        for (int i = activeIcons.Count - 1; i >= 0; i--)
        {
            if (activeIcons[i] != null) Destroy(activeIcons[i].gameObject);
        }
        activeIcons.Clear();

        isSpawning = false;
    }
}