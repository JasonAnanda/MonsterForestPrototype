using System;
using UnityEngine;

public class BeatManager : MonoBehaviour
{
    public static BeatManager Instance;

    // --- Events untuk Sinkronisasi ---
    // Ditembakkan setiap beat System (240 BPM)
    public static event Action<int> OnSystemBeat;
    // Ditembakkan setiap Main Pulse (120 BPM)
    public static event Action OnMainPulse;

    [Header("BPM & Timing")]
    [Tooltip("BPM utama lagu (biasanya 120).")]
    public float mainBPM = 120f;
    [Tooltip("Offset dalam detik untuk mengkalibrasi waktu lagu.")]
    public float audioOffset = 0f;

    [Header("Input Timing Window")]
    [Tooltip("Jendela waktu (dalam detik) di sekitar beat 120 BPM di mana input dianggap sempurna.")]
    public float inputWindowTolerance = 0.08f; // Toleransi lebih kecil, 80ms
    [HideInInspector]
    public bool isInputWindowOpen = false;

    [Header("Audio")]
    public AudioSource bgmAudioSource;
    public AudioClip bgmClip;

    // --- Private Timing Variables ---
    private float systemBeatInterval; // Interval 240 BPM
    private float mainPulseInterval;  // Interval 120 BPM
    private double nextSystemBeatTime;
    private double nextMainPulseTime;
    private int systemBeatCounter = 0; // Menghitung 240 BPM (0 = Main Pulse 120 BPM)

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;

        // Interval dihitung berdasarkan 240 BPM dan 120 BPM
        systemBeatInterval = 60f / (mainBPM * 2f); // 240 BPM interval
        mainPulseInterval = 60f / mainBPM;         // 120 BPM interval

        if (bgmAudioSource == null)
        {
            Debug.LogError("BeatManager membutuhkan AudioSource untuk BGM.");
        }
    }

    void Start()
    {
        if (bgmAudioSource != null && bgmClip != null)
        {
            bgmAudioSource.clip = bgmClip;
            // *PENTING*: Pastikan properti .loop dicentang di Inspector untuk BGM Audio Source Anda.
            bgmAudioSource.Play();

            // Inisialisasi waktu beat pertama menggunakan waktu audio
            nextSystemBeatTime = AudioSettings.dspTime + audioOffset;
            nextMainPulseTime = nextSystemBeatTime; // Main Pulse terjadi pada beat pertama
        }
    }

    void Update()
    {
        // š KRITIKAL: Menggunakan AudioSettings.dspTime untuk sinkronisasi yang presisi
        double currentTime = AudioSettings.dspTime;

        // 1. Cek System Beat (240 BPM)
        if (currentTime >= nextSystemBeatTime)
        {
            // Koreksi waktu untuk mencegah drift (tambahan beat berikutnya)
            nextSystemBeatTime += systemBeatInterval;

            // Update Counter (0 -> 1 -> 0 -> 1...)
            systemBeatCounter = (systemBeatCounter + 1) % 2;
            int rhythmType = (systemBeatCounter == 0) ? (int)mainBPM : (int)(mainBPM * 2f);

            // Panggil event 240 BPM (untuk Monster Voice Sequencing)
            OnSystemBeat?.Invoke(rhythmType);

            // 2. Cek Main Pulse (120 BPM) - Hanya terjadi jika counter = 0
            if (systemBeatCounter == 0)
            {
                // Panggil event 120 BPM (Untuk Kuantisasi & Flash Visual)
                OnMainPulse?.Invoke();

                // Hitung waktu pulse utama berikutnya (digunakan untuk Input Timing Window)
                nextMainPulseTime = nextSystemBeatTime;
            }
        }

        // 3. Input Timing Window (Jendela Perfect Hit)
        // Cek apakah waktu saat ini dekat dengan beat 120 BPM (Main Pulse)
        // Kita bandingkan dengan waktu Main Pulse berikutnya (nextMainPulseTime) dan yang terakhir (timeSinceLastPulse)
        double timeUntilNextPulse = nextMainPulseTime - currentTime;
        double timeSinceLastPulse = currentTime - (nextSystemBeatTime - mainPulseInterval);

        // Input dianggap sempurna jika berada dalam toleransi di kedua sisi beat 120 BPM
        if (timeUntilNextPulse <= inputWindowTolerance || (timeSinceLastPulse > 0 && timeSinceLastPulse <= inputWindowTolerance))
        {
            isInputWindowOpen = true;
        }
        else
        {
            isInputWindowOpen = false;
        }
    }
}