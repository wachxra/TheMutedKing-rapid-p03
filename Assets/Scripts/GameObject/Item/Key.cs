/*using UnityEngine;

public class Key : MonoBehaviour, IInteractable
{
    [Header("Key Settings")]
    public string keyID;
    public string keyName;

    public void OnInteract(PlayerController player)
    {
        if (string.IsNullOrEmpty(keyID)) return;

        player.AddKey(keyID);
        Debug.Log($"Picked up key: {keyName}");

        Destroy(gameObject);
    }
}*/