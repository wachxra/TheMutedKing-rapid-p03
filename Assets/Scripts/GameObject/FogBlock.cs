using UnityEngine;

public class FogBlock : MonoBehaviour
{
    [Header("Fog Settings")]
    public GameObject fogVisual;
    public Collider2D fogCollider;
    public Transform torchPoint;

    private bool isLit = false;

    void Awake()
    {
        if (fogVisual == null)
            fogVisual = this.gameObject;

        if (fogCollider == null)
            fogCollider = GetComponent<Collider2D>();

        fogVisual.SetActive(true);
        if (fogCollider != null) fogCollider.enabled = true;
    }

    void Update()
    {
        if (isLit) return;

        if (Vector2.Distance(torchPoint.position, PlayerController.Instance.transform.position) < 1.5f)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                UseTorch();
            }
        }
    }

    void UseTorch()
    {
        isLit = true;

        if (fogVisual != null) fogVisual.SetActive(false);
        if (fogCollider != null) fogCollider.enabled = false;

        Torch torch = torchPoint.GetComponent<Torch>();
        if (torch != null)
        {
            torch.LightUp();
        }

        Debug.Log("Fog cleared!");
    }
}