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

    private bool isTarget = false;
    private bool flashState = false;

    [Header("Highlight Components")]
    public SpriteRenderer highlightRenderer;

    [Header("Highlight Fade Settings")]
    [Tooltip("Alpha value when idle (not just flashed).")]
    public float baseAlpha = 0.3f;
    [Tooltip("Alpha value when flash ON.")]
    public float flashAlpha = 1f;
    [Tooltip("Duration (seconds) of fade-out after a flash — keep small (e.g. 0.15).")]
    public float fadeDuration = 0.15f;

    private Coroutine highlightFadeCoroutine = null;

    // Sequence asli monster (untuk dimainkan oleh monster)
    private List<string> sequence = new List<string>();
    // Icons untuk diproses oleh pemain (berkurang seiring input benar)
    private List<GameObject> icons = new List<GameObject>();
    private bool isActive = false;

    // --- SEQUENCE STATE BARU (PENTING UNTUK RITME) ---
    // Index untuk memutar suara command monster (240 BPM)
    private int nextSoundIndex = 0;
    // Flag Kuantisasi: True jika monster baru saja jadi target dan menunggu Main Pulse (120 BPM)
    public bool waitingForBeat = false;

    private readonly string[] commandPatterns = new string[]
    {
        // Pola baru yang menggunakan tombol 'A' dan jeda '・' (6 langkah per pola)
        "A ・ A ・ A ・",
        "A ・ ・ A A ・",
        "A A ・ A A ・"
    };

    // ============================================================
    // INIT
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
    }

    void OnEnable()
    {
        // ★ PERUBAHAN: Dengarkan Event OnSystemBeat (240 BPM) untuk menjalankan urutan suara
        BeatManager.OnSystemBeat += RunNextCommandStep;
    }

    void OnDisable()
    {
        // ★ PERUBAHAN: Hapus langganan
        BeatManager.OnSystemBeat -= RunNextCommandStep;
    }

    void Update()
    {
        if (!isActive) return;

        HandleMovement();
        HandleInputUpdate();
    }

    // ============================================================
    // TARGET FLAG VISUAL & KUANTISASI
    // ============================================================
    public void SetTarget(bool value)
    {
        isTarget = value;

        if (value)
        {
            // ★ Kuantisasi: Saat menjadi target, atur flag untuk menunggu Main Pulse
            waitingForBeat = true;
            nextSoundIndex = 0; // Pastikan sequence suara mulai dari awal
        }

        if (highlightInstance != null)
            highlightInstance.SetActive(value);

        if (highlightRenderer != null)
            highlightRenderer.enabled = value;

        if (!value)
        {
            flashState = false;
            waitingForBeat = false; // Hentikan kuantisasi

            if (soundPlayer != null)
                soundPlayer.StopCurrentSound();
        }
    }

    void OnBeatFlash()
    {
        if (!isTarget) return;
        if (highlightInstance == null) return;

        flashState = !flashState;
        highlightInstance.SetActive(flashState);
    }

    public void SetSelected(bool state)
    {
        if (highlightRenderer != null)
            highlightRenderer.enabled = state;
    }

    // ============================================================
    // FLASH HIGHLIGHT
    // ============================================================
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

        // Hitung durasi fade berdasarkan BPM saat ini (Main Pulse, biasanya 120 BPM)
        float bpm = (BeatManager.Instance != null) ? BeatManager.Instance.mainBPM : 120f;
        float beatInterval = 60f / Mathf.Max(0.0001f, bpm);
        // Pastikan fade time tidak lebih lama dari Main Pulse (120 BPM)
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

    // ============================================================
    // WRAPPERS UNTUK TARGETMANAGER & INPUT
    // ============================================================
    public void ReceivePlayerInput(string cmd) { HandleInput(cmd); }
    public void HandleCommand(string cmd) { HandleInput(cmd); }
    public void SetAutoTarget(bool state) { SetTarget(state); }
    public void SetManualTarget(bool state) { SetTarget(state); }
    public void SetTargetVisual(bool state) { SetTarget(state); }

    // ============================================================
    // ACTIVATE SEQUENCE
    // ============================================================
    public void ActivateSequence()
    {
        if (isActive) return;
        isActive = true;

        string pattern = commandPatterns[Random.Range(0, commandPatterns.Length)];
        sequence.Clear();
        // ★ PERUBAHAN: Pastikan membagi string dan menghilangkan entri kosong
        sequence.AddRange(pattern.Split(' ').Where(s => !string.IsNullOrEmpty(s)));

        CreateUI();

        if (soundPlayer != null && spawnClip != null)
            soundPlayer.PlayCustomSound(spawnClip);
    }

    // ============================================================
    // MOVEMENT
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
            if ((moveDirection.x < 0 && transform.position.x <= deathLine.position.x) ||
                (moveDirection.x > 0 && transform.position.x >= deathLine.position.x))
            {
                if (TrustMeterManager.Instance != null)
                    TrustMeterManager.Instance.AddMiss();

                Destroy(gameObject);
            }
        }
    }

    // ============================================================
    // COMMAND SOUND PER BEAT (Sequence 240 BPM)
    // ============================================================
    // ★ PERUBAHAN: Menggantikan PlayNextCommand() yang lama
    void RunNextCommandStep(int rhythmType)
    {
        // Jika monster bukan target, abaikan
        if (!isTarget) return;

        // 1. Logika Kuantisasi (Menunggu Main Pulse/120 BPM untuk start)
        // Kita hanya mulai bermain sequence saat Main Pulse (120 BPM) DITANGANI.

        // Cek apakah sedang menunggu (waitingForBeat).
        if (waitingForBeat)
        {
            // Cek apakah ini adalah Main Pulse (120 BPM) untuk mengakhiri kuantisasi.
            if (BeatManager.Instance != null && rhythmType == (int)BeatManager.Instance.mainBPM)
            {
                // Main Pulse tercapai, kuantisasi selesai, monster mulai bicara pada beat ini.
                waitingForBeat = false;
            }
            else
            {
                // Masih menunggu, abaikan command step ini.
                return;
            }
        }

        // 2. Jalankan sequence jika sudah tidak kuantisasi
        if (nextSoundIndex >= sequence.Count)
        {
            // Monster telah selesai memainkan polanya, kini menunggu input pemain.
            return;
        }

        string cmd = sequence[nextSoundIndex];

        // Putar suara jika bukan jeda
        if (cmd != "・" && soundPlayer != null)
        {
            soundPlayer.PlaySound(cmd);
        }

        nextSoundIndex++;
    }

    // ============================================================
    // INPUT HANDLING
    // ============================================================
    void HandleInputUpdate()
    {
        if (!isTarget) return;
        if (sequence.Count == 0) return;

        // ★ PENTING: Cek apakah Input Manager berada di Jendela Waktu yang valid
        if (BeatManager.Instance != null && !BeatManager.Instance.isInputWindowOpen)
        {
            // Tidak dalam jendela input, jangan proses input tombol untuk Echo.
            return;
        }

        // Proses input hanya jika jendela input terbuka
        foreach (string cmd in new string[] { "A", "S", "J", "K" })
        {
            // Asumsi InputManager.IsCommandPressed() mendeteksi GetKeyDown (ini sudah benar)
            if (InputManager.Instance != null && InputManager.Instance.IsCommandPressed(cmd))
                HandleInput(cmd);
        }

        // Input untuk Jeda/Kosong (・)
        if (Input.GetKeyDown(KeyCode.Space))
            HandleInput("・");
    }

    void HandleInput(string pressed)
    {
        if (sequence.Count == 0) return;

        // ★ PERUBAHAN PENTING: Abaikan/Penalti input jika monster sedang kuantisasi
        if (waitingForBeat)
        {
            // Pemain mencoba input sebelum monster mulai bicara (miss).
            if (TrustMeterManager.Instance != null)
                TrustMeterManager.Instance.AddMiss();

            // Setelah miss, batalkan targeting (tergantung aturan game Anda, ini bisa opsional)
            TargetManager.Instance?.SetManualTarget(null);

            return;
        }

        string expected = sequence[0];
        if (pressed == expected)
        {
            // Input Benar: Hapus icon dan sequence pertama
            if (icons.Count > 0)
            {
                Destroy(icons[0]);
                icons.RemoveAt(0);
            }
            sequence.RemoveAt(0);

            // Play feedback sound
            if (pressed != "・" && soundPlayer != null)
                soundPlayer.PlaySound(pressed);

            if (sequence.Count == 0)
                OnSequenceComplete();
        }
        else
        {
            // Input Salah (miss)
            if (TrustMeterManager.Instance != null)
                TrustMeterManager.Instance.AddMiss();

            // Setelah miss, batalkan targeting (opsional, tergantung aturan game)
            TargetManager.Instance?.SetManualTarget(null);
        }
    }

    void OnSequenceComplete()
    {
        // Hapus sisa icon (jika ada) dan monster
        foreach (var ic in icons)
            Destroy(ic);

        Destroy(gameObject);
    }

    void CreateUI()
    {
        if (uiParent == null || iconPrefab == null) return;

        // Bersihkan UI lama
        for (int i = uiParent.childCount - 1; i >= 0; i--)
            Destroy(uiParent.GetChild(i).gameObject);

        icons.Clear();

        // Buat icon baru
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
            if (TrustMeterManager.Instance != null)
                TrustMeterManager.Instance.AddMiss();

            Destroy(gameObject);
        }
    }
}