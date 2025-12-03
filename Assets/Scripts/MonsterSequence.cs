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

    private List<string> sequence = new List<string>();
    private List<CommandIconUI> icons = new List<CommandIconUI>();
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

        // assign ke PlayerInputHandler
        StartCoroutine(AssignToPlayerInput());
    }

    IEnumerator AssignToPlayerInput()
    {
        yield return null; // tunggu 1 frame agar PlayerInputHandler aktif
        var playerInput = FindObjectOfType<PlayerInputHandler>();
        if (playerInput != null)
            playerInput.activeMonster = this;
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

            CommandIconUI iconUI = iconGO.GetComponent<CommandIconUI>();
            if (iconUI == null)
                iconUI = iconGO.AddComponent<CommandIconUI>();

            iconUI.defaultSprite = defaultSprite;
            iconUI.emptySprite = spriteEmpty;

            if (img != null) img.sprite = iconUI.defaultSprite;

            icons.Add(iconUI);
        }
    }

    public void HandlePlayerInput(string pressed)
    {
        if (!isActive) return;
        if (sequence.Count == 0 || icons.Count == 0) return;

        string expected = sequence[0];
        CommandIconUI currentIcon = icons[0];

        if (pressed == expected)
        {
            currentIcon.SetPerfect();
        }
        else
        {
            currentIcon.SetMiss();
            if (TrustMeterManager.Instance != null)
                TrustMeterManager.Instance.AddMiss();
        }

        sequence.RemoveAt(0);
        icons.RemoveAt(0);

        if (pressed != "・" && soundPlayer != null)
            soundPlayer.PlaySound(pressed);

        if (sequence.Count == 0)
            OnSequenceComplete();
    }

    void OnSequenceComplete()
    {
        foreach (var ic in icons)
            if (ic != null) Destroy(ic.gameObject);
        Destroy(gameObject);
    }
}
