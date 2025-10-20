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

    [Header("Timing Settings")]
    public float perfectTimingWindow = 0.08f;
    public float minTravelTime = 1.0f;
    public float maxTravelTime = 2.5f;

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

            // เช็กว่า enemy ตายหรือถูกทำลาย
            if (icon.enemy == null || icon.enemy.currentHP <= 0)
            {
                RemoveAndDestroyIcon(icon); // ฟังก์ชันนี้ลบออกจาก activeIcons และ Destroy gameObject
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
            _ => null
        };
    }

    public void StartEnemyComboFromList(List<BeatData> combo, EnemyController enemy)
    {
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

            beat.travelDuration = Random.Range(minTravelTime, maxTravelTime);

            GameObject go = Instantiate(beatIconPrefab, beatPanel);
            BeatIcon icon = go.GetComponent<BeatIcon>();

            if (icon == null)
            {
                Debug.LogError("BeatIcon component missing on prefab");
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
            icon.OnMissed += () => NotifyIconMissed(icon);
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
            PlayerController.Instance?.OnPerfectParry(target.enemy);
        }
        else
        {
            Debug.Log("Miss Parry!");

            if (target.enemy != null)
                SoundMeterSystem.Instance?.AddSound(target.enemy.damage);
        }

        RemoveAndDestroyIcon(target);
    }

    public void NotifyIconMissed(BeatIcon icon)
    {
        if (activeIcons.Contains(icon))
        {
            if (!icon.IsWithinTrigger() && icon.enemy != null)
                SoundMeterSystem.Instance?.AddSound(icon.enemy.damage);

            RemoveAndDestroyIcon(icon);
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

        activeCombo = null;
        activeIcons.Clear();
        isSpawning = false;
    }
}