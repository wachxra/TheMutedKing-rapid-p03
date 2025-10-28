/*using UnityEngine;
using UnityEngine.SceneManagement;

public class Door : MonoBehaviour, IInteractable
{
    [Header("Key Settings")]
    public bool requiresKey = false;
    public string keyID;

    [Header("Warp Settings (Next Scene)")]
    public bool isSceneDoor = false;
    public string sceneName;

    [Header("Warp Settings (Same Scene)")]
    public bool warpInScene = false;
    public Transform warpTarget;

    private bool isOpen = false;

    public void OnInteract(PlayerController player)
    {
        if (isOpen) return;

        if (requiresKey)
        {
            if (!player.HasKey(keyID))
            {
                Debug.Log("Door is locked!");
                return;
            }
            player.UseKey(keyID);
        }

        ToggleDoor(player);
    }

    private void ToggleDoor(PlayerController player)
    {
        if (isSceneDoor && !string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else if (warpInScene && warpTarget != null)
        {
            player.transform.position = warpTarget.position;

            var cameraFollow = Camera.main.GetComponent<CameraController>();
            if (cameraFollow != null)
            {
                 
            }
        }
    }
}*/