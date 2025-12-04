using System.Collections.Generic;
using UnityEngine;

public class TargetManager : MonoBehaviour
{
    public static TargetManager Instance;

    [Header("Detection Settings")]
    public float detectionRadius = 15f;
    public LayerMask monsterLayer;

    [Header("Flash Settings")]
    public float bpm = 120f;

    private MonsterSequence currentTarget;

    private readonly List<MonsterSequence> detectedMonsters = new();
    private readonly List<MonsterSequence> registeredMonsters = new();

    // Manual override
    private bool isManualOverride = false;
    private int manualIndex = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    void OnEnable()
    {
        BeatManager.OnHalfBeat += HalfBeatFlash;
    }

    void OnDisable()
    {
        BeatManager.OnHalfBeat -= HalfBeatFlash;
    }

    void Update()
    {
        DetectMonsters();

        HandleManualOverrideInput(); // <-- cek input manual dulu
        PickClosestMonster();
    }

    private void HandleManualOverrideInput()
    {
        if (detectedMonsters.Count == 0) return;

        // Toggle manual override (optional, bisa diganti key)
        if (Input.GetKeyDown(KeyCode.R))
        {
            isManualOverride = !isManualOverride;
            manualIndex = 0; // reset
        }

        if (!isManualOverride) return;

        // Forward/Backward
        if (Input.GetKeyDown(KeyCode.E))
        {
            manualIndex++;
            if (manualIndex >= detectedMonsters.Count) manualIndex = 0;
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            manualIndex--;
            if (manualIndex < 0) manualIndex = detectedMonsters.Count - 1;
        }

        // Set target sesuai manualIndex
        MonsterSequence target = detectedMonsters[manualIndex];
        if (target != currentTarget)
        {
            if (currentTarget != null)
            {
                currentTarget.SetSelected(false);
                currentTarget.SetTarget(false);
            }

            currentTarget = target;

            if (currentTarget != null)
            {
                currentTarget.SetSelected(true);
                currentTarget.SetTarget(true);
            }
        }
    }

    private void HalfBeatFlash()
    {
        if (currentTarget != null)
            currentTarget.FlashHighlight();
    }

    void DetectMonsters()
    {
        detectedMonsters.Clear();

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, monsterLayer);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out MonsterSequence seq))
            {
                if (!detectedMonsters.Contains(seq))
                    detectedMonsters.Add(seq);
            }
        }

        // include registered list
        for (int i = registeredMonsters.Count - 1; i >= 0; i--)
        {
            var m = registeredMonsters[i];
            if (m == null) { registeredMonsters.RemoveAt(i); continue; }

            float d = Vector2.Distance(transform.position, m.transform.position);
            if (d <= detectionRadius && !detectedMonsters.Contains(m))
                detectedMonsters.Add(m);
        }
    }

    void PickClosestMonster()
    {
        if (isManualOverride) return; // jangan auto pick kalau override aktif

        if (detectedMonsters.Count == 0)
        {
            if (currentTarget != null)
            {
                currentTarget.SetSelected(false);
                currentTarget.SetTarget(false);
                currentTarget = null;
            }
            return;
        }

        MonsterSequence closest = null;
        float min = float.MaxValue;

        foreach (var m in detectedMonsters)
        {
            if (m == null) continue;
            float d = Vector2.Distance(transform.position, m.transform.position);
            if (d < min) { min = d; closest = m; }
        }

        if (closest != currentTarget)
        {
            if (currentTarget != null)
            {
                currentTarget.SetSelected(false);
                currentTarget.SetTarget(false);
            }

            currentTarget = closest;

            if (currentTarget != null)
            {
                currentTarget.SetSelected(true);
                currentTarget.SetTarget(true);
            }
        }
    }

    public MonsterSequence GetCurrentTarget() => currentTarget;

    public void RegisterMonster(MonsterSequence m)
    {
        if (m != null && !registeredMonsters.Contains(m))
            registeredMonsters.Add(m);
    }

    public void DeregisterMonster(MonsterSequence m)
    {
        if (m == null) return;

        registeredMonsters.Remove(m);
        detectedMonsters.Remove(m);

        if (currentTarget == m)
        {
            currentTarget.SetSelected(false);
            currentTarget = null;
        }
    }

    public void ForwardInput(string cmd)
    {
        if (currentTarget == null) return;
        currentTarget.ReceivePlayerInput(cmd);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
