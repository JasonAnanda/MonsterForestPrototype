using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    private List<string> sequence = new List<string>();
    private List<GameObject> icons = new List<GameObject>();
    private bool isActive = false;
    private int nextSoundIndex = 0;

    private readonly string[] commandPatterns = new string[]
    {
        "A S J ・ ・ K",
        "A ・ S ・ J ・",
        "・ A S ・ J K",
        "A S ・ J ・ K",
        "A ・ ・ S ・ J"
    };

    // ============================================================
    // INIT
    // ============================================================
    void Awake()
    {
        // Setup highlight prefab (instantiated child)
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
        BeatManager.OnBeat += PlayNextCommand;
    }

    void OnDisable()
    {
        BeatManager.OnBeat -= PlayNextCommand;
    }

    void Update()
    {
        if (!isActive) return;

        HandleMovement();
        HandleInputUpdate();
    }

    // ============================================================
    // TARGET FLAG VISUAL
    // ============================================================
    public void SetTarget(bool value)
    {
        isTarget = value;

        if (highlightInstance != null)
            highlightInstance.SetActive(value);

        if (highlightRenderer != null)
            highlightRenderer.enabled = value;

        if (!value)
            flashState = false;
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

        // akses bpm via instance
        float bpm = (BeatManager.Instance != null) ? BeatManager.Instance.bpm : 120f;
        float halfBeat = (60f / Mathf.Max(0.0001f, bpm)) * 0.5f;
        float fadeTime = Mathf.Clamp(fadeDuration, 0.05f, halfBeat * 0.9f);

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
        sequence.AddRange(pattern.Split(' '));

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
    // COMMAND SOUND PER BEAT
    // ============================================================
    void PlayNextCommand()
    {
        if (nextSoundIndex >= sequence.Count) return;

        string cmd = sequence[nextSoundIndex];
        if (cmd != "・" && soundPlayer != null)
            soundPlayer.PlaySound(cmd);

        nextSoundIndex++;
    }

    // ============================================================
    // INPUT HANDLING
    // ============================================================
    void HandleInputUpdate()
    {
        if (!isTarget) return;
        if (sequence.Count == 0) return;

        foreach (string cmd in new string[] { "A", "S", "J", "K" })
        {
            if (InputManager.Instance != null && InputManager.Instance.IsCommandPressed(cmd))
                HandleInput(cmd);
        }

        if (Input.GetKeyDown(KeyCode.Space))
            HandleInput("・");
    }

    void HandleInput(string pressed)
    {
        if (sequence.Count == 0) return;

        string expected = sequence[0];
        if (pressed == expected)
        {
            if (icons.Count > 0)
            {
                Destroy(icons[0]);
                icons.RemoveAt(0);
            }
            sequence.RemoveAt(0);

            if (pressed != "・" && soundPlayer != null)
                soundPlayer.PlaySound(pressed);

            if (sequence.Count == 0)
                OnSequenceComplete();
        }
        else
        {
            if (TrustMeterManager.Instance != null)
                TrustMeterManager.Instance.AddMiss();
        }
    }

    void OnSequenceComplete()
    {
        foreach (var ic in icons)
            Destroy(ic);

        Destroy(gameObject);
    }

    void CreateUI()
    {
        if (uiParent == null || iconPrefab == null) return;

        for (int i = uiParent.childCount - 1; i >= 0; i--)
            Destroy(uiParent.GetChild(i).gameObject);

        icons.Clear();

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
