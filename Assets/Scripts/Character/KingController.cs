using UnityEngine;
using UnityEngine.UI;

public class KingController : MonoBehaviour
{
    public static KingController Instance;

    public float maxHP = 500f;
    public float currentHP;
    public Slider kingHPSlider;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        currentHP = maxHP;

        if (kingHPSlider != null)
        {
            kingHPSlider.minValue = 0f;
            kingHPSlider.maxValue = maxHP;
            kingHPSlider.value = currentHP;
        }
    }

    public void AddHP(int amount)
    {
        currentHP += amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        UpdateSlider();
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        UpdateSlider();
        if (currentHP <= 0)
        {
            Die();
        }
    }

    void UpdateSlider()
    {
        if (kingHPSlider != null)
            kingHPSlider.value = currentHP;
    }

    void Die()
    {
        Debug.Log("King has been defeated!");

        if (GameResult.Instance != null)
        {
            GameResult.Instance.HandleWin();
        }
    }
}