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
    public int SequenceCount => sequence.Count;

    [Header("Sound")]
    public MonsterSoundPlayer soundPlayer;
    public AudioClip spawnClip;

    // internal
    private List<string> sequence = new List<string>();
    private List<GameObject> icons = new List<GameObject>();
    private bool isActive = false;
    private float halfBeatTimer = 0f;
    private int nextSoundIndex = 0;
    private float beatDuration = 0.5f;

    private readonly string[] commandPatterns = new string[]
    {
        "A S J ・ ・ K",
        "A ・ S ・ J ・",
        "・ A S ・ J K",
        "A S ・ J ・ K",
        "A ・ ・ S ・ J"
    };

    void Update()
    {
        if (!isActive) return;

        HandleMovement();
        HandleHalfBeatSound();
        HandleInputUpdate();
    }

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

    void HandleMovement()
    {
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

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

    void HandleHalfBeatSound()
    {
        halfBeatTimer += Time.deltaTime;
        if (halfBeatTimer >= beatDuration / 2f)
        {
            if (nextSoundIndex < sequence.Count)
            {
                string cmd = sequence[nextSoundIndex];
                if (cmd != "・" && soundPlayer != null)
                    soundPlayer.PlaySound(cmd);
                nextSoundIndex++;
            }
            halfBeatTimer = 0f;
        }
    }

    void HandleInputUpdate()
    {
        if (sequence.Count == 0) return;

        // hanya cek A, S, J, K di InputManager
        foreach (string cmd in new string[] { "A", "S", "J", "K" })
        {
            if (InputManager.Instance != null && InputManager.Instance.IsCommandPressed(cmd))
            {
                HandleInput(cmd);
            }
        }

        // dot (・) hanya di-handle via space key
        if (Input.GetKeyDown(KeyCode.Space))
        {
            HandleInput("・");
        }
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
            Debug.Log("[MonsterSequence] Wrong input");
            if (TrustMeterManager.Instance != null)
                TrustMeterManager.Instance.AddMiss();
        }
    }

    void OnSequenceComplete()
    {
        foreach (var ic in icons) Destroy(ic);
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

            if (img != null) img.sprite = defaultSprite;
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
