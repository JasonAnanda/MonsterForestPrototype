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
        // Pada sistem Miss Counter sederhana, hit berhasil mereset hitungan Miss
        // (Anda bisa mengabaikan 'isPerfect' jika tidak ingin membedakan skor, 
        // tapi logikanya tetap reset Miss)
        currentMiss = 0;
        UpdateUI();
    }

    // Dipanggil dari MonsterSequence saat input salah, early press, atau monster melewati Death Line
    public void AddMiss()
    {
        // Pastikan game belum berakhir
        if (currentMiss >= maxMiss) return;

        currentMiss++;
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