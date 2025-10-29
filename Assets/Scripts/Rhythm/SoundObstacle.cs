using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SoundObstacle : MonoBehaviour
{
    [Header("Sound Settings")]
    [Range(0, 10)] public int soundValue = 0;
    public float radius = 1.5f;

    void Awake()
    {
        soundValue = Random.Range(1, 11);
    }

    void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            AudioManager.Instance?.PlaySFX("Attack");
            SoundMeterSystem.Instance?.AddSound(soundValue);
            Debug.Log($"Added {soundValue} sound from obstacle!");
        }
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}