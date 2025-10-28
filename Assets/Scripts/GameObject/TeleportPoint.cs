using UnityEngine;
using System.Collections.Generic;
using System;

public class TeleportPoint : MonoBehaviour
{
    public static event Action<int> OnTeleport;

    [Header("Teleport Settings")]
    [Tooltip("ID ของชั้นปลายทางหลังจากการวาร์ป (เช่น 0=ชั้น1, 1=ชั้น2, 2=ชั้น3)")]
    public int destinationLevelID;
    public List<Transform> destinationPoints;

    [Tooltip("UI text")]
    public GameObject interactUI;

    private bool playerInRange = false;

    void Start()
    {
        if (interactUI != null)
        {
            interactUI.SetActive(false);
        }
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (PlayerController.Instance != null)
            {
                TryTeleport(PlayerController.Instance.transform);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() != null)
        {
            playerInRange = true;
            if (interactUI != null)
            {
                interactUI.SetActive(true);
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() != null)
        {
            playerInRange = false;
            if (interactUI != null)
            {
                interactUI.SetActive(false);
            }
        }
    }

    void TryTeleport(Transform playerTransform)
    {
        if (destinationPoints == null || destinationPoints.Count == 0)
        {
            Debug.LogWarning("Destination Points is empty on TeleportPoint: " + gameObject.name);
            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, destinationPoints.Count);
        Transform destination = destinationPoints[randomIndex];

        playerTransform.position = destination.position;

        Rigidbody2D rb = playerTransform.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        AudioManager.Instance?.PlaySFX("Teleport");

        OnTeleport?.Invoke(destinationLevelID);

        if (interactUI != null)
        {
            interactUI.SetActive(false);
        }

        playerInRange = false;
    }
}