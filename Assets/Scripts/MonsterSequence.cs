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
    public Sprite spriteA, spriteS, spriteJ, spriteK, spriteEmpty;

    [Header("Sequence Settings")]
    public float moveSpeed = 2f;
    public Vector3 moveDirection = Vector3.left;
    public Transform deathLine;

    [Header("Sound")]
    public MonsterSoundPlayer soundPlayer;
    public AudioClip spawnClip;
    public AudioClip cueSoundClip; // <--- SUARA CUE "GO!" UNTUK MEMULAI TURN PLAYER
    public AudioClip hitFeedbackClip; // <--- NEW: SUARA FEEDBACK JIKA PLAYER BERHASIL INPUT

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

    // Index untuk melacak beat saat fase prompting/echo monster
    private int currentBeatIndex = 0;
    // Flag untuk memisahkan giliran monster (prompt) dan giliran player (input)
    private bool isPromptingPhase = true;

    // VARIABEL waitingForCueDelay DIHAPUS UNTUK MEMPERCEPAT CUE

    // --- POLA (Variasi 2 hingga 6 ketukan, max 3 'A' rapid berturut-turut) ---
    private readonly string[] commandPatterns = new string[]
    {
        // Panjang 2 ketukan
        "A A", "A ・",
        // Panjang 3 ketukan
        "A A A", "A ・ A", "・ A ・",
        // Panjang 4 ketukan
        "A A A ・", "A ・ A A", "A ・ ・ A", "・ A A ・", 
        // Panjang 5 ketukan
        "A A A ・ A", "A ・ A ・ A", "A A ・ A A", "A ・ ・ ・ A", 
        // Panjang 6 ketukan
        "A A A ・ A A", "A ・ A ・ A ・", "A A ・ ・ A A"
    };

    // ============================================================
    // INIT & SETUP
    // ============================================================
    void Awake()
    {
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
            currentBeatIndex = 0;      // Reset beat index
            isPromptingPhase = true;   // Mulai dari fase prompt
            // waitingForCueDelay dihapus
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

        string pattern = commandPatterns[UnityEngine.Random.Range(0, commandPatterns.Length)];
        sequence.Clear();
        // Membagi pola, pastikan karakter '・' ditangani dengan benar
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
    // BEAT AND SOUND SEQUENCING (TURN LOGIC) - UPDATED FOR FASTER CUE
    // ============================================================
    void RunNextCommandStep(int rhythmType)
    {
        if (!isTarget || !isActive) return;
        if (BeatManager.Instance == null) return;

        // 1. Quantization: tunggu Main Pulse (120 BPM) untuk memulai urutan
        if (waitingForBeat)
        {
            // ASUMSI: rhythmType adalah BPM (120 atau 240). Cek 120 BPM untuk memulai.
            if (rhythmType == (int)BeatManager.Instance.mainBPM)
            {
                waitingForBeat = false; // Urutan dimulai
            }
            return; // Tunggu Main Pulse 120 BPM
        }

        // --- PHASE 1: MONSTER PROMPT / ECHO (240 BPM) ---
        if (isPromptingPhase)
        {
            if (currentBeatIndex < sequence.Count)
            {
                // A. Prompt Sound (Plays full pattern)
                string currentCmd = sequence[currentBeatIndex];

                if (currentCmd != "・" && soundPlayer != null)
                {
                    soundPlayer.PlaySound(currentCmd);
                }

                // B. Visual Prompt (Flash)
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

        // --- PHASE 1.5: CUE SOUND (Hanya berjalan sekali, menunggu 120 BPM) ---
        if (!isPromptingPhase && !hasCued)
        {
            // PERUBAHAN UTAMA: Hanya mainkan Cue Sound jika ketukan saat ini adalah ketukan 120 BPM (Main Pulse)
            if (rhythmType == (int)BeatManager.Instance.mainBPM)
            {
                if (cueSoundClip != null && soundPlayer != null)
                {
                    // Cue Sound sekarang akan SELALU selaras dengan downbeat (120 BPM)
                    soundPlayer.PlayCustomSound(cueSoundClip);
                }

                hasCued = true;
                currentBeatIndex = 0; // Reset index untuk player consumption
            }

            // Kita harus return di sini (terlepas dari apakah cue dimainkan atau tidak)
            // untuk mencegah Phase 2 berjalan sebelum cue dimainkan.
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

        foreach (string cmd in new string[] { "A", "S", "J", "K" })
        {
            if (InputManager.Instance != null && InputManager.Instance.IsCommandPressed(cmd))
                HandleInput(cmd);
        }
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
                case "S": defaultSprite = spriteS; break;
                case "J": defaultSprite = spriteJ; break;
                case "K": defaultSprite = spriteK; break;
                case "・": defaultSprite = spriteEmpty; break;
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