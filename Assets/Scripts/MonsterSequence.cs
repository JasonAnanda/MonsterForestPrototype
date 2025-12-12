using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class MonsterSequence : MonoBehaviour
{
    [Header("UI Settings")]
    public Transform uiParent;
    public GameObject iconPrefab;
    // Disimpan hanya sprite yang relevan untuk gameplay: A dan Empty (untuk pause/・)
    public Sprite spriteA, spriteEmpty;

    [Header("Sequence Settings")]
    public float moveSpeed = 2f;
    public Vector3 moveDirection = Vector3.left;
    public Transform deathLine;

    [Header("Sound")]
    public MonsterSoundPlayer soundPlayer;
    public AudioClip spawnClip;
    public AudioClip cueSoundClip; // Suara Cue "GO!" untuk memulai turn player
    public AudioClip hitFeedbackClip; // Suara Feedback jika player berhasil input

    // --- NEW: Audio Pola ---
    [Header("Monster Pattern Sound")]
    public int patternID; // 1, 2, atau 3
    [Tooltip("Array klip suara untuk pola (Indeks 0=Pola 1, 1=Pola 2, 2=Pola 3).")]
    public AudioClip[] patternAudioClips;
    private AudioSource audioSource;
    // -------------------------

    [Header("Irregular Movement")]
    public bool isIrregular = false;
    public float irregularAmplitude = 0.3f;
    public float irregularFrequency = 3f;

    [Header("Target Highlight")]
    public GameObject highlightPrefab;
    private GameObject highlightInstance;

    [Header("Highlight Components")]
    public SpriteRenderer highlightRenderer;

    [Header("Highlight Fade Settings")]
    public float baseAlpha = 0.15f;
    public float flashAlpha = 1f;
    public float fadeDuration = 0.15f;

    // --- STATE ---
    private bool isTarget = false;
    private Coroutine highlightFadeCoroutine = null;

    // Command sequence for the player (reduces with correct input)
    private List<string> sequence = new List<string>();
    // UI icons corresponding to the sequence
    private List<GameObject> icons = new List<GameObject>();
    private bool isActive = false;

    // --- SEQUENCE STATE FOR RHYTHM (REVISED) ---
    // Flag untuk kuantisasi: Menunggu 120 BPM (Main Pulse) untuk memulai.
    public bool isQuantizing = false;
    private bool hasCued = false; // Melacak apakah suara Cue sudah dimainkan

    // Index untuk melacak beat saat fase prompting/echo monster
    // Catatan: Ini berjalan pada kecepatan 240 BPM, dari 0 hingga 6 (total 7 langkah)
    private int currentBeatIndex = 0;
    // Flag untuk memisahkan giliran monster (prompt) dan giliran player (input)
    private bool isPromptingPhase = true;

    // --- POLA (Menggunakan pola spesifik dari skrip Python: total 6 ketukan) ---
    // Catatan: Setiap pola memiliki 6 elemen (6 ketukan visual)
    private readonly string[] commandPatterns = new string[]
    {
		// ["A", "-", "A", "-", "A", "-"] -> '・' adalah '-' (pause)
		"A ・ A ・ A ・",
		// ["A", "-", "-", "A", "A", "-"]
		"A ・ ・ A A ・",
		// ["A", "A", "-", "A", "A", "-"]
		"A A ・ A A ・"
    };

    // ============================================================
    // INIT & SETUP
    // ============================================================
    void Awake()
    {
        // Dapatkan AudioSource. JIKA TIDAK ADA, TAMBAHKAN SECARA OTOMATIS.
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            Debug.LogWarning($"AudioSource tidak ditemukan pada Monster {gameObject.name}. Komponen baru telah ditambahkan secara otomatis.");
        }

        // Pastikan konfigurasi AudioSource untuk PlayOneShot
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.clip = null; // Pastikan tidak ada clip default yang terpasang
        }

        if (highlightPrefab != null)
        {
            highlightInstance = Instantiate(highlightPrefab, transform);
            highlightInstance.transform.localPosition = Vector3.zero;
            SetGameObjectAlpha(highlightInstance, baseAlpha);
            highlightInstance.SetActive(false);
        }
        if (highlightRenderer != null)
        {
            SetSpriteAlpha(highlightRenderer, baseAlpha);
            highlightRenderer.enabled = false;
        }

        if (TargetManager.Instance != null)
            TargetManager.Instance.RegisterMonster(this);
    }

    void OnDestroy()
    {
        if (TargetManager.Instance != null)
            TargetManager.Instance.DeregisterMonster(this);

        // Hapus ikon UI saat monster dihancurkan
        foreach (var ic in icons)
            Destroy(ic);
    }

    void OnEnable()
    {
        // Dengarkan 240 BPM Beat untuk progres turn (System BPM)
        BeatManager.OnSystemBeat += RunNextCommandStep;
    }

    void OnDisable()
    {
        BeatManager.OnSystemBeat -= RunNextCommandStep;
    }

    void Update()
    {
        if (!isActive) return;
        HandleMovement();
        HandleInputUpdate();
    }

    // --- Highlight & Target Management Functions ---

    public void SetTarget(bool value)
    {
        isTarget = value;

        if (value)
        {
            isQuantizing = true; // Mulai proses kuantisasi 120 BPM
            hasCued = false;
            currentBeatIndex = 0;       // Reset beat index
            isPromptingPhase = true;    // Mulai dari fase prompt
        }

        if (highlightInstance != null)
            highlightInstance.SetActive(value);

        if (highlightRenderer != null)
            highlightRenderer.enabled = value;

        if (!value)
        {
            isQuantizing = false; // Hentikan kuantisasi

            if (soundPlayer != null)
                soundPlayer.StopCurrentSound();
        }
    }

    // Helper function to set alpha of a SpriteRenderer
    void SetSpriteAlpha(SpriteRenderer renderer, float alpha)
    {
        if (renderer != null)
        {
            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }
    }

    // Helper function to set alpha for all SpriteRenderers in a GameObject (used for the highlight prefab)
    void SetGameObjectAlpha(GameObject go, float alpha)
    {
        if (go == null) return;
        SpriteRenderer[] renderers = go.GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers)
        {
            Color color = r.color;
            color.a = alpha;
            r.color = color;
        }
    }

    // Flashes the visual highlight
    public void FlashHighlight()
    {
        if (!isTarget) return;

        // Visual flash (instantaneous)
        if (highlightRenderer != null)
            SetSpriteAlpha(highlightRenderer, flashAlpha);
        if (highlightInstance != null)
            SetGameObjectAlpha(highlightInstance, flashAlpha);

        // Start fade coroutine if not already running
        if (highlightFadeCoroutine != null)
            StopCoroutine(highlightFadeCoroutine);

        highlightFadeCoroutine = StartCoroutine(FadeHighlightToBase());
    }

    IEnumerator FadeHighlightToBase()
    {
        float timer = 0f;
        float startAlpha = flashAlpha;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / fadeDuration;
            float currentAlpha = Mathf.Lerp(startAlpha, baseAlpha, t);

            if (highlightRenderer != null)
                SetSpriteAlpha(highlightRenderer, currentAlpha);
            if (highlightInstance != null)
                SetGameObjectAlpha(highlightInstance, currentAlpha);

            yield return null;
        }

        // Ensure it ends exactly at baseAlpha
        if (highlightRenderer != null)
            SetSpriteAlpha(highlightRenderer, baseAlpha);
        if (highlightInstance != null)
            SetGameObjectAlpha(highlightInstance, baseAlpha);

        highlightFadeCoroutine = null;
    }


    public void ActivateSequence()
    {
        if (isActive) return;
        isActive = true;

        // --- NEW: Tentukan Pattern ID dan Sequence ---
        int patternIndex = UnityEngine.Random.Range(0, commandPatterns.Length);
        patternID = patternIndex + 1; // 1, 2, atau 3
        string pattern = commandPatterns[patternIndex];

        sequence.Clear();
        // Membagi pola
        sequence.AddRange(pattern.Split(' ').Where(s => !string.IsNullOrEmpty(s)));

        CreateUI();

        // Reset status
        isPromptingPhase = true;
        currentBeatIndex = 0;
        hasCued = false;

        // Play spawn sound (if any)
        if (soundPlayer != null && spawnClip != null)
            soundPlayer.PlayCustomSound(spawnClip);
    }

    // Memutar klip audio penuh yang sesuai dengan pola
    private void PlayPatternAudio()
    {
        // Logika ini dipanggil sekali saat kuantisasi selesai.
        int index = patternID - 1;

        if (audioSource != null && patternAudioClips != null && index >= 0 && index < patternAudioClips.Length)
        {
            AudioClip clipToPlay = patternAudioClips[index];

            if (clipToPlay != null)
            {
                audioSource.PlayOneShot(clipToPlay);
                Debug.Log($"Monster plays Pattern {patternID} audio clip (Durasi {clipToPlay.length:F2}s) tepat pada Main Pulse (120 BPM).");
                Debug.LogWarning("Jika audio pola terasa terlalu cepat, pastikan klip audio tersebut dirancang dengan durasi 6 ketukan pada 240 BPM (yaitu 1.5 detik total).");
            }
            else
            {
                Debug.LogWarning($"AudioClip untuk Pola {patternID} (Indeks {index}) tidak ditetapkan di Inspector.");
            }
        }
    }


    // Handles non-rhythm movement (e.g., scrolling across the screen)
    void HandleMovement()
    {
        if (!isActive) return;

        Vector3 finalDirection = moveDirection;

        // Tambahkan gerakan tak teratur jika diaktifkan
        if (isIrregular)
        {
            float yOffset = Mathf.Sin(Time.time * irregularFrequency) * irregularAmplitude;
            finalDirection += new Vector3(0, yOffset, 0);
        }

        transform.Translate(finalDirection * moveSpeed * Time.deltaTime);

        // Cek jika sudah melewati deathLine
        if (deathLine != null && moveDirection.x < 0 && transform.position.x < deathLine.position.x)
        {
            // Trigger DeathLine logic
            OnTriggerEnter2D(deathLine.GetComponent<Collider2D>());
        }
    }

    // ============================================================
    // BEAT AND SOUND SEQUENCING (TURN LOGIC) - Menggunakan 240 BPM
    // ============================================================
    void RunNextCommandStep(int rhythmType)
    {
        if (!isTarget || !isActive) return;
        if (BeatManager.Instance == null) return;

        // Cek apakah ini Main Pulse (120 BPM)
        bool isMainPulse = rhythmType == (int)BeatManager.Instance.mainBPM;

        // 1. Quantization: TUNGGU Main Pulse (120 BPM) untuk memulai urutan
        if (isQuantizing)
        {
            if (isMainPulse)
            {
                isQuantizing = false; // Kuantisasi Selesai
                PlayPatternAudio();   // MONSTER SOUND DIPUTAR TEPAT PADA 120 BPM PULSE PERTAMA.
                // Eksekusi dilanjutkan ke logika prompting di bawah untuk menjalankan langkah visual pertama (Beat 0)
            }
            else
            {
                return; // Lewati beat 240 BPM yang bukan pulsa utama.
            }
        }

        // --- PHASE 1: MONSTER PROMPT / ECHO (6 Beats Visual + 1 Beat Cue) ---
        if (isPromptingPhase)
        {
            // Beat 0 hingga 5 (Indeks 0-5) - Visual Flash. (sequence.Count = 6)
            if (currentBeatIndex < sequence.Count)
            {
                // A. Visual Prompt (Flash) 
                FlashHighlight();

                // B. Advance index
                currentBeatIndex++;
                return;
            }

            // Beat 6 (currentBeatIndex == 6) - Play CUE SOUND dan Transisi
            // **Ini adalah perbaikan bug overlap. Cue diputar 1 beat setelah pattern visual selesai.**
            else if (currentBeatIndex == sequence.Count)
            {
                // A. Play Cue Sound "GO!" 
                if (cueSoundClip != null && soundPlayer != null && !hasCued)
                {
                    soundPlayer.PlayCustomSound(cueSoundClip);
                    hasCued = true;
                    Debug.Log($"Cue Sound diputar pada Beat ke-{currentBeatIndex + 1} (1 beat setelah pattern selesai).");
                }

                // B. Transisi ke fase Player Input
                isPromptingPhase = false;

                // C. Beat selesai. Player input dimulai pada beat berikutnya (Beat 7).
                return;
            }
            // currentBeatIndex > sequence.Count (i.e., 7 atau lebih)
            else
            {
                // Transisi sudah selesai, do nothing.
                return;
            }
        }

        // --- PHASE 2: PLAYER INPUT (Auto-consume Pause '・' only) ---
        // Logika ini berjalan setelah Cue dimainkan (isPromptingPhase == false).
        if (!isPromptingPhase && hasCued && sequence.Count > 0)
        {
            // Kita hanya mengonsumsi '・' jika command berikutnya adalah '・'
            if (sequence[0] == "・")
            {
                // Hapus command PAUSE ('・')
                if (icons.Count > 0)
                {
                    Destroy(icons[0]);
                    icons.RemoveAt(0);
                }
                sequence.RemoveAt(0);

                if (sequence.Count == 0)
                {
                    OnSequenceComplete();
                    return;
                }
            }

            // JIKA command adalah 'A', itu tetap di depan dan menunggu input pemain.
        }
    }

    // ============================================================
    // INPUT HANDLING
    // ============================================================
    void HandleInputUpdate()
    {
        // Input hanya valid jika fase Prompting sudah selesai DAN Cue sudah dimainkan
        if (!isTarget || isPromptingPhase || !hasCued) return;
        if (sequence.Count == 0) return;

        // Hanya cek input "A" karena gameplay hanya menggunakan tombol ini
        if (InputManager.Instance != null && InputManager.Instance.IsCommandPressed("A"))
            HandleInput("A");
    }

    void HandleInput(string pressed)
    {
        if (sequence.Count == 0) return;

        // Input tidak valid jika masih di fase Prompting (belum ada Cue)
        if (isPromptingPhase || !hasCued)
        {
            if (TrustMeterManager.Instance != null)
                TrustMeterManager.Instance.AddMiss();

            TargetManager.Instance?.SetManualTarget(null); // Reset target
            return;
        }

        string expected = sequence[0];

        // Player input saat harus menunggu PAUSE ('・')
        if (expected == "・")
        {
            if (TrustMeterManager.Instance != null)
                TrustMeterManager.Instance.AddMiss();

            TargetManager.Instance?.SetManualTarget(null); // Reset target
            return;
        }

        // Check Timing Window from BeatManager
        bool isInputWindowOpen = BeatManager.Instance != null && BeatManager.Instance.isInputWindowOpen;

        if (pressed == expected)
        {
            // Correct Input: Successful Hit
            bool isPerfect = isInputWindowOpen;

            if (TrustMeterManager.Instance != null)
                TrustMeterManager.Instance.AddHit(isPerfect);

            // Hapus command yang sudah selesai
            if (icons.Count > 0)
            {
                Destroy(icons[0]);
                icons.RemoveAt(0);
            }
            sequence.RemoveAt(0);

            FlashHighlight(); // Beri feedback visual

            // Play generic Hit Feedback sound 
            if (soundPlayer != null && hitFeedbackClip != null) // Menggunakan hitFeedbackClip yang baru
                soundPlayer.PlayCustomSound(hitFeedbackClip);
            else if (soundPlayer != null && spawnClip != null) // Fallback ke spawnClip jika hitFeedbackClip kosong
                soundPlayer.PlayCustomSound(spawnClip);

            if (sequence.Count == 0)
                OnSequenceComplete();
        }
        else
        {
            // Incorrect Input: Miss
            if (TrustMeterManager.Instance != null)
                TrustMeterManager.Instance.AddMiss();

            TargetManager.Instance?.SetManualTarget(null); // Reset target
        }
    }

    void OnSequenceComplete()
    {
        // Handle monster defeat/damage logic here
        Destroy(gameObject);
    }

    void CreateUI()
    {
        if (uiParent == null || iconPrefab == null) return;

        // Clear existing icons
        for (int i = uiParent.childCount - 1; i >= 0; i--)
            Destroy(uiParent.GetChild(i).gameObject);

        icons.Clear();

        // Instantiate new icons based on the command sequence
        foreach (string cmd in sequence)
        {
            GameObject iconGO = Instantiate(iconPrefab, uiParent);
            Image img = iconGO.GetComponent<Image>();

            Sprite defaultSprite = spriteEmpty;
            switch (cmd)
            {
                case "A": defaultSprite = spriteA; break;
                case "・": defaultSprite = spriteEmpty; break;
                default:
                    Debug.LogWarning($"Command '{cmd}' tidak memiliki sprite yang ditetapkan.");
                    break;
            }

            if (img != null)
                img.sprite = defaultSprite;

            icons.Add(iconGO);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("DeathLine"))
        {
            // Death Line Miss
            if (TrustMeterManager.Instance != null)
                TrustMeterManager.Instance.AddMiss();

            Destroy(gameObject);
        }
    }
}