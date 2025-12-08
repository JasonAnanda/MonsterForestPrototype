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
    public float baseAlpha = 0.3f;
    public float flashAlpha = 1f;
    public float fadeDuration = 0.15f;

    // --- STATE ---
    private bool isTarget = false;
    private bool flashState = false;
    private Coroutine highlightFadeCoroutine = null;

    // Command sequence for the player (reduces with correct input)
    private List<string> sequence = new List<string>();
    // UI icons corresponding to the sequence
    private List<GameObject> icons = new List<GameObject>();
    private bool isActive = false;

    // --- SEQUENCE STATE FOR RHYTHM ---
    private int nextSoundIndex = 0;
    public bool waitingForBeat = false; // Waiting for 120 BPM quantization start

    // --- POLA BARU (Variasi 2 hingga 6 ketukan, max 3 'A' rapid berturut-turut) ---
    private readonly string[] commandPatterns = new string[]
    {
        // Panjang 2 ketukan (Max 2 A rapid)
        "A A",
        "A ・",

        // Panjang 3 ketukan (Max 3 A rapid)
        "A A A",
        "A ・ A",
        "・ A ・",

        // Panjang 4 ketukan (Max 3 A rapid)
        "A A A ・", // Mengganti AAAA, 3 rapid A di awal
        "A ・ A A",
        "A ・ ・ A",
        "・ A A ・", 
        
        // Panjang 5 ketukan (Max 3 A rapid)
        "A A A ・ A", // Mengganti AAAAA, 3 rapid A diselingi jeda
        "A ・ A ・ A",
        "A A ・ A A",
        "A ・ ・ ・ A", 
        
        // Panjang 6 ketukan (Max 3 A rapid)
        "A A A ・ A A", // Mengganti AAAAAA, 3 rapid A di awal, diselingi jeda
        "A ・ A ・ A ・",
        "A A ・ ・ A A"
    };

    // ============================================================
    // INIT & SETUP
    // ============================================================
    void Awake()
    {
        // [Highlight Initialization]
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

        // Destroy UI icons when monster is destroyed
        foreach (var ic in icons)
            Destroy(ic);
    }

    void OnEnable()
    {
        // Listen to 240 BPM Beat for sound sequencing
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
        HandleInputUpdate(); // Called every frame to check for GetKeyDown
    }

    // --- Highlight & Target Management Functions (Not significantly changed) ---

    public void SetTarget(bool value)
    {
        isTarget = value;

        if (value)
        {
            waitingForBeat = true;
            nextSoundIndex = 0;
        }

        if (highlightInstance != null)
            highlightInstance.SetActive(value);

        if (highlightRenderer != null)
            highlightRenderer.enabled = value;

        if (!value)
        {
            flashState = false;
            waitingForBeat = false;

            if (soundPlayer != null)
                soundPlayer.StopCurrentSound();
        }
    }

    public void SetSelected(bool state)
    {
        if (highlightRenderer != null)
            highlightRenderer.enabled = state;
    }

    public void FlashHighlight()
    {
        if (!isTarget) return;

        if (highlightInstance != null)
            highlightInstance.SetActive(true);
        if (highlightRenderer != null)
            highlightRenderer.enabled = true;

        ApplyAlphaToAll(flashAlpha);

        if (highlightFadeCoroutine != null)
            StopCoroutine(highlightFadeCoroutine);

        float bpm = (BeatManager.Instance != null) ? BeatManager.Instance.mainBPM : 120f;
        float beatInterval = 60f / Mathf.Max(0.0001f, bpm);
        float fadeTime = Mathf.Clamp(fadeDuration, 0.05f, beatInterval * 0.9f);

        highlightFadeCoroutine = StartCoroutine(FadeHighlightToBase(fadeTime));
    }

    IEnumerator FadeHighlightToBase(float duration)
    {
        float elapsed = 0f;
        float startA = flashAlpha;
        float endA = baseAlpha;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float a = Mathf.Lerp(startA, endA, t);
            ApplyAlphaToAll(a);
            yield return null;
        }

        ApplyAlphaToAll(endA);
    }

    void ApplyAlphaToAll(float a)
    {
        if (highlightRenderer != null)
            SetSpriteAlpha(highlightRenderer, a);

        if (highlightInstance != null)
            SetGameObjectAlpha(highlightInstance, a);
    }

    void SetSpriteAlpha(SpriteRenderer sr, float a)
    {
        Color c = sr.color;
        c.a = a;
        sr.color = c;
    }

    void SetGameObjectAlpha(GameObject go, float a)
    {
        if (go == null) return;
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) { SetSpriteAlpha(sr, a); return; }

        SpriteRenderer childSr = go.GetComponentInChildren<SpriteRenderer>();
        if (childSr != null) { SetSpriteAlpha(childSr, a); return; }
    }

    public void ActivateSequence()
    {
        if (isActive) return;
        isActive = true;

        string pattern = commandPatterns[UnityEngine.Random.Range(0, commandPatterns.Length)];
        sequence.Clear();
        // Split the pattern, ensuring '・' characters are handled correctly
        sequence.AddRange(pattern.Split(' ').Where(s => !string.IsNullOrEmpty(s)));

        CreateUI();

        if (soundPlayer != null && spawnClip != null)
            soundPlayer.PlayCustomSound(spawnClip);
    }

    // ============================================================
    // MOVEMENT & DEATH LINE (FIX for Bug 3: Miss meter on Death)
    // ============================================================
    void HandleMovement()
    {
        if (isIrregular)
        {
            Vector3 baseMove = moveDirection * moveSpeed * Time.deltaTime;
            float offset = Mathf.Sin(Time.time * irregularFrequency) * irregularAmplitude;
            Vector3 zigzag = new Vector3(0, offset * Time.deltaTime, 0);

            transform.position += baseMove + zigzag;
        }
        else
        {
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }

        if (deathLine != null)
        {
            // Check if it crosses the death line
            if ((moveDirection.x < 0 && transform.position.x <= deathLine.position.x) ||
                (moveDirection.x > 0 && transform.position.x >= deathLine.position.x))
            {
                // FIX BUG 3: Add Miss when monster hits the Death Line
                if (TrustMeterManager.Instance != null)
                    TrustMeterManager.Instance.AddMiss();

                Destroy(gameObject);
            }
        }
    }

    // ============================================================
    // BEAT AND SOUND SEQUENCING (FIXED for 240 BPM Sound)
    // ============================================================
    void RunNextCommandStep(int rhythmType)
    {
        if (!isTarget || !isActive) return;
        if (BeatManager.Instance == null) return;

        // Quantization: tunggu Main Pulse (120 BPM) untuk memulai urutan
        if (waitingForBeat)
        {
            // Pastikan ini adalah Main Pulse (120 BPM)
            if (rhythmType == (int)BeatManager.Instance.mainBPM)
            {
                waitingForBeat = false; // Urutan suara dimulai
            }
            else
            {
                return; // Tunggu Main Pulse 120 BPM
            }
        }

        // --- LOGIC BARU: Konsumsi PAUSE ('・') secara OTOMATIS pada beat tick ---
        // Jika command pertama yang diharapkan adalah PAUSE ('・'), konsumsi dan lanjutkan.
        if (sequence.Count > 0 && sequence[0] == "・")
        {
            // Hapus command PAUSE ('・') dari daftar yang diharapkan
            if (icons.Count > 0)
            {
                Destroy(icons[0]);
                icons.RemoveAt(0);
            }
            sequence.RemoveAt(0);

            // Jika ini adalah langkah terakhir, selesai.
            if (sequence.Count == 0)
            {
                OnSequenceComplete();
                return;
            }
        }
        // --- END LOGIC BARU ---

        // RunNextCommandStep dilanggan ke BeatManager.OnSystemBeat (240 BPM).

        if (nextSoundIndex >= sequence.Count)
        {
            // Urutan suara monster selesai
            return;
        }

        string cmd = sequence[nextSoundIndex];

        // Mainkan suara kecuali itu adalah Pause command '・'
        if (cmd != "・" && soundPlayer != null)
        {
            soundPlayer.PlaySound(cmd);
        }

        nextSoundIndex++;
    }

    // ============================================================
    // INPUT HANDLING (FIX for Bug 2: Input Registration & Bug 3: Miss Call)
    // ============================================================
    void HandleInputUpdate()
    {
        if (!isTarget) return;
        if (sequence.Count == 0) return;

        // Cek hanya untuk tombol input yang valid (A, S, J, K). 
        // Tombol '・' (Pause/Space) tidak lagi memerlukan input.
        // Perubahan: Hapus "・" dari array yang dicek.
        foreach (string cmd in new string[] { "A", "S", "J", "K" })
        {
            // Check GetKeyDown from InputManager
            if (InputManager.Instance != null && InputManager.Instance.IsCommandPressed(cmd))
                HandleInput(cmd);
        }
    }

    void HandleInput(string pressed)
    {
        if (sequence.Count == 0) return;

        // Quantization Check (Early Miss)
        if (waitingForBeat)
        {
            // Player pressed a button before the monster sequence started (early miss)
            if (TrustMeterManager.Instance != null)
                TrustMeterManager.Instance.AddMiss(); // FIX BUG 3

            TargetManager.Instance?.SetManualTarget(null); // Reset target
            return;
        }

        string expected = sequence[0];

        // --- LOGIC BARU: Player input saat harus menunggu PAUSE ('・') ---
        if (expected == "・")
        {
            // Player menekan tombol saat seharusnya menunggu (Miss)
            if (TrustMeterManager.Instance != null)
                TrustMeterManager.Instance.AddMiss();

            TargetManager.Instance?.SetManualTarget(null); // Reset target
            return; // Jangan hapus '・', biarkan RunNextCommandStep yang menghapusnya di beat berikutnya
        }
        // --- END LOGIC BARU ---

        // Check Timing Window from BeatManager
        bool isInputWindowOpen = BeatManager.Instance != null && BeatManager.Instance.isInputWindowOpen;

        if (pressed == expected)
        {
            // Correct Input: Successful Hit
            bool isPerfect = isInputWindowOpen;

            if (TrustMeterManager.Instance != null)
                TrustMeterManager.Instance.AddHit(isPerfect); // FIX BUG 3: Call AddHit

            // Remove the completed command (UI)
            if (icons.Count > 0)
            {
                Destroy(icons[0]);
                icons.RemoveAt(0);
            }
            sequence.RemoveAt(0);

            // Play feedback sound
            if (pressed != "・" && soundPlayer != null) // '・' tidak pernah dicapai di sini
                soundPlayer.PlaySound(pressed);

            if (sequence.Count == 0)
                OnSequenceComplete();
        }
        else
        {
            // Incorrect Input: Miss
            if (TrustMeterManager.Instance != null)
                TrustMeterManager.Instance.AddMiss(); // FIX BUG 3: Call AddMiss

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