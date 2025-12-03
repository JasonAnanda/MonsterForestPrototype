using UnityEngine;
using UnityEngine.UI;

public class TrustMeterManager : MonoBehaviour
{
    public static TrustMeterManager Instance;

    [Header("Meter Settings")]
    public int currentMiss = 0;
    public int maxMiss = 5;

    [Header("UI")]
    public Image meterBar; // fill image

    [Header("Game Over")]
    public GameObject gameOverPanel; // Drag UI Panel here (Inactive default)

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        UpdateUI();
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    public void AddMiss()
    {
        currentMiss++;
        UpdateUI();

        if (currentMiss >= maxMiss)
        {
            TriggerGameOver();
        }
    }

    void UpdateUI()
    {
        if (meterBar != null)
            meterBar.fillAmount = (float)currentMiss / maxMiss;
    }

    void TriggerGameOver()
    {
        Debug.Log("GAME OVER ÅEKepercayaan penuh!");

        // Stop time (opsional)
        Time.timeScale = 0f;

        // Tampilkan panel game over
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }
}
