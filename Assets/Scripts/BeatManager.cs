using UnityEngine;
using System;

public class BeatManager : MonoBehaviour
{
    public static BeatManager Instance;

    // BPM Sistem Ditingkatkan ke 240 (High-Resolution Grid)
    [Header("Rhythm Core Settings")]
    public float systemBPM = 240f;
    [Tooltip("Ketukan Utama Game, biasanya 120 BPM (Setengah dari System BPM)")]
    public float mainBPM = 120f;

    private float beatInterval; // Interval 240 BPM
    private float nextBeatTime; // Waktu tepat beat berikutnya
    private int systemBeatCounter = 0; // Menghitung beat 240 BPM (0 = Main Beat 120 BPM)

    // Events Baru (Menggantikan OnBeat lama)
    public static event Action<int> OnSystemBeat; // Ditembakkan setiap 240 BPM (int: 240 atau 120)
    public static event Action OnMainPulse; // Ditembakkan setiap 120 BPM (Main Beat)

    // Variabel untuk pemeriksaan ketepatan waktu Input
    [HideInInspector] public bool isInputWindowOpen = false;

    // --- Initialization ---
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Interval dihitung berdasarkan 240 BPM
        beatInterval = 60f / Mathf.Max(0.0001f, systemBPM);

        // Start beat time (menggunakan Time.time untuk presisi)
        nextBeatTime = Time.time + beatInterval;

        Debug.Log($"[BeatManager] System Grid: {systemBPM} BPM | Main Pulse: {mainBPM} BPM.");
    }

    // --- Main Loop ---
    void Update()
    {
        // Cek jika sudah waktunya beat
        if (Time.time >= nextBeatTime)
        {
            // --- FIRE BEAT ---
            // Koreksi waktu untuk mencegah drift
            nextBeatTime += beatInterval;

            // Reset Input Window
            isInputWindowOpen = true;

            // Update Counter (0, 1, 0, 1, ...)
            systemBeatCounter = (systemBeatCounter + 1) % 2;

            int rhythmType = (systemBeatCounter == 0) ? (int)mainBPM : (int)systemBPM;

            // 1. Event 240 BPM (Untuk Monster Voice Sequence)
            OnSystemBeat?.Invoke(rhythmType);

            // 2. Event 120 BPM (Untuk Kuantisasi & Flash Visual)
            if (systemBeatCounter == 0)
            {
                OnMainPulse?.Invoke();
            }

            // (TODO: Pindahkan beatAudio.Play() di sini jika Anda ingin metronom 120 BPM)
        }
        else
        {
            // Jendela input hanya terbuka pada saat beat
            isInputWindowOpen = false;
        }
    }

    // --- Hapus semua Event lama (OnHalfBeat, OnBeat, OnLateBeat) jika ada ---
}