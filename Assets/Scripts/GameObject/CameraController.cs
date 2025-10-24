using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;

    [Tooltip("The speed at which the camera smoothly follows the target (higher = faster).")]
    public float smoothSpeed = 0.125f;

    [Tooltip("The offset position from the target. Z is crucial for 2D depth.")]
    public Vector3 offset = new Vector3(0f, 2f, -10f);

    void Start()
    {
        if (target == null)
        {
            if (PlayerController.Instance != null)
            {
                target = PlayerController.Instance.transform;
            }
            else
            {
                Debug.LogError("Camera target is not set and PlayerController instance not found. Disabling CameraController.");
                enabled = false;
                return;
            }
        }

        transform.position = target.position + offset;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime * 50f);

        transform.position = smoothedPosition;
    }
}