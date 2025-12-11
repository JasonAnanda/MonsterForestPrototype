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
    public AudioClip cueSoundClip; // <--- SUARA CUE "GO!" UNTUK MEMULAI TURN PLAYER
    public AudioClip hitFeedbackClip; // <--- NEW: SUARA FEEDBACK JIKA PLAYER BERHASIL INPUT

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
    public bool waitingForBeat = false; // Waiting for 120 BPM quantization start
    private bool hasCued = false; // Melacak apakah suara Cue sudah dimainkan
    private bool hasPatternAudioPlayed = false; // NEW: Melacak apakah klip pola sudah dimainkan

    // Index untuk melacak beat saat fase prompting/echo monster
    private int currentBeatIndex = 0;
    // Flag untuk memisahkan giliran monster (prompt) dan giliran player (input)
    private bool isPromptingPhase = true;

    // --- POLA (Menggunakan pola spesifik dari skrip Python: total 6 ketukan) ---
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
            waitingForBeat = true;
            hasCued = false;
            hasPatternAudioPlayed = false; // NEW: Reset flag audio
            currentBeatIndex = 0;       // Reset beat index
            isPromptingPhase = true;    // Mulai dari fase prompt
        }

        if (highlightInstance != null)
            highlightInstance.SetActive(value);

        if (highlightRenderer != null)
            highlightRenderer.enabled = value;

        if (!value)
        {
            waitingForBeat = false;

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
        hasPatternAudioPlayed = false; // Reset flag audio

        // Play spawn sound (if any)
        if (soundPlayer != null && spawnClip != null)
            soundPlayer.PlayCustomSound(spawnClip);
    }

    // NEW: Memutar klip audio penuh yang sesuai dengan pola
    private void PlayPatternAudio()
    {
        if (hasPatternAudioPlayed) return;

        // patternID (1, 2, 3) dikonversi ke indeks array (0, 1, 2)
        int index = patternID - 1;

        if (audioSource != null && patternAudioClips != null && index >= 0 && index < patternAudioClips.Length)
        {
            AudioClip clipToPlay = patternAudioClips[index];

            if (clipToPlay != null)
            {
                // JANGAN menggunakan PlayOneShot jika AudioSource sudah memainkan klip lain.
                // Namun, karena ini dipicu pada beat yang terkuantisasi, PlayOneShot aman.
                audioSource.PlayOneShot(clipToPlay);
                Debug.Log($"Monster plays Pattern {patternID} audio clip.");
                hasPatternAudioPlayed = true;
            }
            else
            {
                Debug.LogWarning($"AudioClip untuk Pola {patternID} (Indeks {index}) tidak ditetapkan di Inspector. Pastikan klip telah diseret ke array.");
            }
        }
        else
        {
            Debug.LogWarning($"Gagal memutar suara pola. AudioSource null: {audioSource == null}, PatternID: {patternID}, Array Size: {patternAudioClips?.Length ?? 0}");
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

        // 1. Quantization: tunggu Main Pulse (120 BPM) untuk memulai urutan
        if (waitingForBeat)
        {
            // ASUMSI: rhythmType adalah BPM. Cek 120 BPM untuk memulai.
            if (rhythmType == (int)BeatManager.Instance.mainBPM)
            {
                waitingForBeat = false; // Urutan dimulai
                PlayPatternAudio(); // Play full pattern audio saat kuantisasi selesai
            }
            return; // Tunggu Main Pulse 120 BPM
        }

        // --- PHASE 1: MONSTER PROMPT / ECHO (240 BPM) ---
        if (isPromptingPhase)
        {
            if (currentBeatIndex < sequence.Count)
            {
                // B. Visual Prompt (Flash) - TETAP DIJAGA SEBAGAI VISUAL CUE
                FlashHighlight();

                // C. Advance to the next beat
                currentBeatIndex++;
                return;
            }
            else // Pola sudah selesai dimainkan.
            {
                // D. Sinyalkan bahwa fase prompt telah berakhir
                isPromptingPhase = false;
                // E. JANGAN return, FALL THROUGH ke logika Cue Sound di bawah.
            }
        }

        // --- PHASE 1.5: CUE SOUND (Hanya berjalan sekali, dipicu pada 240 BPM berikutnya) ---
        if (!isPromptingPhase && !hasCued)
        {
            // Cue Sound dipicu pada ketukan 240 BPM pertama setelah prompt selesai.
            if (cueSoundClip != null && soundPlayer != null)
            {
                soundPlayer.PlayCustomSound(cueSoundClip);
            }

            hasCued = true;
            currentBeatIndex = 0; // Reset index untuk player consumption

            // Kita harus return di sini untuk mencegah Phase 2 (Auto-consume Pause) 
            // berjalan pada beat yang sama saat cue dimainkan.
            return;
        }

        // --- PHASE 2: PLAYER INPUT (Auto-consume Pause '・' only) ---
        // Logika ini hanya berjalan jika hasCued=true
        if (hasCued && sequence.Count > 0 && sequence[0] == "・")
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
    }

    // ============================================================
    // INPUT HANDLING
    // ============================================================
    void HandleInputUpdate()
    {
        // Input hanya valid jika monster sudah selesai prompting DAN cue sound sudah dimainkan
        // isPromptingPhase=false, hasCued=true
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
            // MEMPERBAIKI TYPO TrustMeterManagerManager
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