using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0f, 2f, -10f);

    [Header("Decoration")]
    public Transform kingObject;
    public Vector3 kingOffset = new Vector3(0f, 0f, 0f);

    void Start()
    {
        if (target == null)
        {
            if (PlayerController.Instance != null)
                target = PlayerController.Instance.transform;
            else
            {
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

        if (kingObject != null)
        {
            Vector3 newKingPos = transform.position + kingOffset;
            newKingPos.z = kingObject.position.z;
            kingObject.position = newKingPos;
        }
    }
}