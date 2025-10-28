using UnityEngine;
using UnityEngine.UI;

public class BeatIcon : MonoBehaviour
{
    public float travelDuration;
    public Direction requiredDirection;
    public float ultimateMissDamage;

    public bool hasBeenTriggered = false;
    public bool ReachedTrigger { get; private set; } = false;
    public float TimeToReachEnd { get; private set; }

    public EnemyController enemy;

    public RectTransform RectTransform => rectTransform;
    public Vector3 TriggerPosition { get; private set; }

    private RectTransform rectTransform;
    private Vector3 startPosition;
    private Vector3 endPosition;
    private Image iconImage;
    private RhythmSystem parentSystem;
    private float moveStartTime;
    private bool isMoving = false;

    public event System.Action OnMissed;

    public void Initialize(RhythmSystem system, BeatData data, Vector3 startPos, Vector3 endPos, Vector3 triggerPos, Sprite iconSprite)
    {
        parentSystem = system;
        requiredDirection = data.requiredDirection;
        travelDuration = data.travelDuration;
        ultimateMissDamage = data.ultimateMissSoundDamage;

        rectTransform = GetComponent<RectTransform>();
        iconImage = GetComponent<Image>();

        startPosition = startPos;
        endPosition = endPos;
        TriggerPosition = triggerPos;

        rectTransform.localPosition = startPosition;
        if (iconImage != null) iconImage.sprite = iconSprite;
    }

    public bool IsWithinTrigger()
    {
        return Vector3.Distance(rectTransform.localPosition, TriggerPosition) <= parentSystem.triggerDistance;
    }

    public void StartMove()
    {
        moveStartTime = Time.time;
        TimeToReachEnd = moveStartTime + travelDuration;
        isMoving = true;
        ReachedTrigger = false;
    }

    void Update()
    {
        if (!isMoving || rectTransform == null || hasBeenTriggered) return;

        float elapsed = Time.time - moveStartTime;
        float t = Mathf.Clamp01(elapsed / travelDuration);
        rectTransform.localPosition = Vector3.Lerp(startPosition, endPosition, t);

        if (!ReachedTrigger && Vector3.Distance(rectTransform.localPosition, TriggerPosition) <= parentSystem.triggerDistance)
            ReachedTrigger = true;

        if (t >= 1f && !hasBeenTriggered)
        {
            hasBeenTriggered = true;

            if (requiredDirection == Direction.Ultimate)
            {
                AudioManager.Instance?.PlaySFX("Attack");
                SoundMeterSystem.Instance?.AddSound(ultimateMissDamage);
                Destroy(gameObject, 0.05f);
                return;
            }

            AudioManager.Instance?.PlaySFX("Attack");
            OnMissed?.Invoke();
            Destroy(gameObject, 0.05f);
        }
    }
}