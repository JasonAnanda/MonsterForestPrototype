using UnityEngine;
using UnityEngine.UI;

public class TrustMeterManager : MonoBehaviour
{
    public static TrustMeterManager Instance;

    [Header("Meter Settings")]
    [Tooltip("Jumlah Miss saat ini.")]
    public int currentMiss = 0;
    [Tooltip("Jumlah Miss maksimum sebelum Game Over.")]
    public int maxMiss = 5;

    [Header("UI")]
    public Image meterBar; // fill image (merepresentasikan jumlah Miss)

    [Header("Game Over")]
    public GameObject gameOverPanel; // Drag UI Panel here (Inactive default)

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    void Start()
    {
        UpdateUI();
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    // Dipanggil dari MonsterSequence saat input benar (Hit berhasil)
    public void AddHit(bool isPerfect)
    {
        // FIX BUG 1: Meter tidak boleh reset ke 0 pada setiap hit.
        // Hit berhasil mengurangi akumulasi Miss / kerusakan sebanyak 1.
        if (currentMiss > 0)
        {
            currentMiss--;
        }

        Debug.Log($"Hit berhasil. Miss Counter berkurang: {currentMiss}/{maxMiss}");
        UpdateUI();
    }

    // Dipanggil dari MonsterSequence saat input salah, early press, miss timing, atau monster melewati Death Line
    public void AddMiss()
    {
        // Pastikan game belum berakhir
        if (currentMiss >= maxMiss) return;

        currentMiss++;
        Debug.Log($"Miss terdeteksi. Miss Counter bertambah: {currentMiss}/{maxMiss}");
        UpdateUI();

        if (currentMiss >= maxMiss)
        {
            TriggerGameOver();
        }
    }

    void UpdateUI()
    {
        // Bar meter menunjukkan seberapa dekat Anda dengan Game Over (Miss penuh)
        if (meterBar != null)
            meterBar.fillAmount = (float)currentMiss / maxMiss;
    }

    void TriggerGameOver()
    {
        Debug.Log("GAME OVER ÅEKepercayaan penuh!");

        // Stop time 
        Time.timeScale = 0f;

        // Tampilkan panel game over
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }
}