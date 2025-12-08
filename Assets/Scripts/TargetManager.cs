using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TargetManager : MonoBehaviour
{
    public static TargetManager Instance;

    [Header("Detection Settings")]
    public float detectionRadius = 15f; // Radius deteksi target
    public LayerMask monsterLayer;

    private MonsterSequence currentTarget;
    private readonly List<MonsterSequence> detectedMonsters = new();
    private readonly List<MonsterSequence> registeredMonsters = new();
    private int currentTargetIndex = -1;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    void OnEnable()
    {
        // Langganan ke event System Beat dari BeatManager (240 BPM) untuk kedip target
        BeatManager.OnSystemBeat += SystemBeatFlash;
    }

    void OnDisable()
    {
        BeatManager.OnSystemBeat -= SystemBeatFlash;
    }

    // Mengganti MainPulseFlash menjadi SystemBeatFlash untuk memicu 240 BPM
    private void SystemBeatFlash(int rhythmType) // Menerima rhythmType dari OnSystemBeat
    {
        // Memicu visual flash pada target saat System Beat (240 BPM)
        if (currentTarget != null)
            currentTarget.FlashHighlight();
    }

    void Update()
    {
        DetectMonsters(); // Memperbarui daftar monster yang berada dalam radius
        HandleTargetCyclingInput(); // Mengelola input Q/E untuk berpindah target
    }

    private void HandleTargetCyclingInput()
    {
        List<MonsterSequence> validTargets = detectedMonsters;

        if (validTargets.Count == 0)
        {
            if (currentTarget != null) SetManualTarget(null);
            return;
        }

        int direction = 0;

        if (Input.GetKeyDown(KeyCode.E))
        {
            direction = 1;
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            direction = -1;
        }

        if (direction != 0)
        {
            int currentValidIndex = validTargets.IndexOf(currentTarget);

            if (currentValidIndex == -1)
            {
                currentValidIndex = (direction > 0) ? -1 : validTargets.Count;
            }

            int newIndex = currentValidIndex + direction;

            // Handle wrap-around (kembali ke awal/akhir daftar)
            if (newIndex >= validTargets.Count)
            {
                newIndex = 0;
            }
            else if (newIndex < 0)
            {
                newIndex = validTargets.Count - 1;
            }

            SetManualTarget(validTargets[newIndex]);
        }
    }

    public void SetManualTarget(MonsterSequence target)
    {
        if (currentTarget == target) return;

        // Menonaktifkan status target pada monster lama
        if (currentTarget != null)
        {
            currentTarget.SetSelected(false);
            currentTarget.SetTarget(false);
        }

        currentTarget = target;

        if (currentTarget != null)
        {
            // Mengaktifkan status target pada monster baru
            currentTarget.SetSelected(true);
            currentTarget.SetTarget(true);
            currentTarget.ActivateSequence(); // Memulai urutan command monster baru

            currentTargetIndex = detectedMonsters.IndexOf(target);
        }
        else
        {
            currentTargetIndex = -1;
        }
    }

    void DetectMonsters()
    {
        detectedMonsters.Clear();

        // Deteksi menggunakan Physics2D.OverlapCircleAll
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, monsterLayer);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out MonsterSequence seq))
            {
                if (!detectedMonsters.Contains(seq))
                    detectedMonsters.Add(seq);
            }
        }

        // Memastikan monster yang baru spawn juga terdeteksi jika dalam radius
        for (int i = registeredMonsters.Count - 1; i >= 0; i--)
        {
            var m = registeredMonsters[i];
            if (m == null) { registeredMonsters.RemoveAt(i); continue; }

            float d = Vector2.Distance(transform.position, m.transform.position);
            if (d <= detectionRadius && !detectedMonsters.Contains(m))
                detectedMonsters.Add(m);
        }
    }

    public MonsterSequence GetCurrentTarget() => currentTarget;

    // Dipanggil oleh monster saat spawn
    public void RegisterMonster(MonsterSequence m)
    {
        if (m != null && !registeredMonsters.Contains(m))
            registeredMonsters.Add(m);
    }

    // Dipanggil oleh monster saat dihancurkan/mati
    public void DeregisterMonster(MonsterSequence m)
    {
        if (m == null) return;

        registeredMonsters.Remove(m);
        detectedMonsters.Remove(m);

        // Mereset target jika monster yang hilang adalah target saat ini
        if (currentTarget == m)
        {
            SetManualTarget(null);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Menampilkan radius deteksi di editor Unity
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}