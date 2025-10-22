using UnityEngine;
using UnityEngine.SceneManagement;

public class GameResult : MonoBehaviour
{
    public static GameResult Instance;

    [Header("UI References (Optional)")]
    public GameObject winScreen;
    public GameObject loseScreen;

    [Header("Scene Settings")]
    public string mainMenuSceneName = "MainMenu";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (winScreen != null) winScreen.SetActive(false);
        if (loseScreen != null) loseScreen.SetActive(false);
    }

    public void HandleWin()
    {
        if (SoundMeterSystem.Instance != null && SoundMeterSystem.Instance.isGameOver)
        {
            return;
        }

        Debug.Log("Game Won! King Defeated.");
        if (winScreen != null) winScreen.SetActive(true);
        Time.timeScale = 0f;

        if (PlayerController.Instance != null) PlayerController.Instance.enabled = false;
    }

    public void HandleLose()
    {
        Debug.Log("Game Over! Sound Meter Maxed Out.");
        if (loseScreen != null) loseScreen.SetActive(true);
        Time.timeScale = 0f;

        if (PlayerController.Instance != null) PlayerController.Instance.enabled = false;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    public void BackToMainMenu()
    {
        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogError("Main Menu Scene Name is not set in the Inspector!");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}