using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Torch : MonoBehaviour
{
    public Sprite unlitSprite;
    public Sprite litSprite;
    public Light2D torchLight;
    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null && unlitSprite != null)
            sr.sprite = unlitSprite;

        if (torchLight != null)
            torchLight.enabled = false;
    }

    public void LightUp()
    {
        if (sr != null && litSprite != null)
            sr.sprite = litSprite;

        if (torchLight != null)
            torchLight.enabled = true;

        Debug.Log("Torch lit!");
    }
}