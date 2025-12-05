using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Perlu ditambahkan untuk fungsi List

public class TargetManager : MonoBehaviour
{
    public static TargetManager Instance;

    [Header("Detection Settings")]
    public float detectionRadius = 15f;
    public LayerMask monsterLayer;

    [Header("Flash Settings")]
    public float bpm = 120f;

    private MonsterSequence currentTarget;

    // detectedMonsters akan digunakan sebagai daftar monster yang dapat di-cycle
    private readonly List<MonsterSequence> detectedMonsters = new();

    // registeredMonsters tetap dipertahankan untuk mengelola monster yang aktif di scene
    private readonly List<MonsterSequence> registeredMonsters = new();

    // Index untuk cycling target (menggantikan manualIndex)
    private int currentTargetIndex = -1;

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
        // 1. Deteksi monster di radius (Logika ini tetap aman)
        DetectMonsters();

        // 2. Handle input Q/E untuk manual targeting (menggantikan HandleManualOverrideInput)
        HandleTargetCyclingInput();

        // --- PENTING: HAPUS PickClosestMonster() DI SINI ---
    }

    /// <summary>
    /// Menangani input Q/E untuk berpindah target secara manual.
    /// Ini menggantikan logika HandleManualOverrideInput().
    /// </summary>
    private void HandleTargetCyclingInput()
    {
        // PENTING: Hanya gunakan monster yang terdeteksi (detectedMonsters) untuk cycling
        List<MonsterSequence> validTargets = detectedMonsters;

        if (validTargets.Count == 0)
        {
            // Jika tidak ada target, pastikan currentTarget di-reset
            if (currentTarget != null) SetManualTarget(null);
            return;
        }

        int direction = 0;

        // Input untuk target berikutnya (E)
        if (Input.GetKeyDown(KeyCode.E))
        {
            direction = 1;
        }
        // Input untuk target sebelumnya (Q)
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            direction = -1;
        }

        if (direction != 0)
        {
            // 1. Cari index target saat ini di daftar validTargets
            int currentValidIndex = validTargets.IndexOf(currentTarget);

            // Jika target saat ini tidak ada atau keluar radius, reset index
            if (currentValidIndex == -1)
            {
                // Mulai cycling dari index 0 atau akhir, tergantung arah
                currentValidIndex = (direction > 0) ? -1 : validTargets.Count;
            }

            // 2. Hitung index baru (wrap-around)
            int newIndex = currentValidIndex + direction;

            // Handle wrap-around
            if (newIndex >= validTargets.Count)
            {
                newIndex = 0;
            }
            else if (newIndex < 0)
            {
                newIndex = validTargets.Count - 1;
            }

            // 3. Tetapkan target baru
            SetManualTarget(validTargets[newIndex]);
        }
    }

    /// <summary>
    /// Fungsi inti yang digunakan untuk menetapkan target baru.
    /// Ini menggantikan logika setting target di PickClosestMonster() dan HandleManualOverrideInput().
    /// </summary>
    /// <param name="target">MonsterSequence yang dipilih sebagai target. Null untuk mereset.</param>
    public void SetManualTarget(MonsterSequence target)
    {
        if (currentTarget == target) return; // Tidak perlu diubah jika target sama

        // 1. Nonaktifkan status target pada monster yang lama (jika ada)
        if (currentTarget != null)
        {
            // PENTING: Panggil fungsi yang sama seperti di PickClosestMonster
            currentTarget.SetSelected(false);
            currentTarget.SetTarget(false);
        }

        // 2. Set target baru
        currentTarget = target;

        // 3. Aktifkan status target pada monster yang baru (jika tidak null)
        if (currentTarget != null)
        {
            // PENTING: Panggil fungsi yang sama seperti di PickClosestMonster
            currentTarget.SetSelected(true);
            currentTarget.SetTarget(true);

            // Update index untuk cycling, hanya jika target ini ada di daftar detectedMonsters
            currentTargetIndex = detectedMonsters.IndexOf(target);
        }
        else
        {
            currentTargetIndex = -1;
        }
    }

    // --- FUNGSI LAIN DIBIARKAN SAMA ---

    private void HalfBeatFlash()
    {
        if (currentTarget != null)
            currentTarget.FlashHighlight();
    }

    void DetectMonsters()
    {
        // Logika ini tetap aman
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

    // --- PENTING: HAPUS FUNGSI PickClosestMonster() SEPENUHNYA ---
    // Pastikan fungsi ini DIBUANG dari skrip:
    /*
    void PickClosestMonster()
    {
        // ... (kode auto-pick)
    }
    */

    // --- FUNGSI HELPER LAIN DIBIARKAN SAMA ---

    public MonsterSequence GetCurrentTarget() => currentTarget;

    public void RegisterMonster(MonsterSequence m)
    {
        if (m != null && !registeredMonsters.Contains(m))
            registeredMonsters.Add(m);
    }

    public void DeregisterMonster(MonsterSequence m)
    {
        // Logika ini tetap aman, hanya pastikan memanggil SetManualTarget(null) jika target hilang
        if (m == null) return;

        registeredMonsters.Remove(m);
        detectedMonsters.Remove(m);

        if (currentTarget == m)
        {
            // Ganti ini
            // currentTarget.SetSelected(false);
            // currentTarget = null;

            // Dengan ini (lebih clean)
            SetManualTarget(null);
        }
        // Jika bukan currentTarget, index cycling mungkin perlu diatur ulang, tapi biarkan saja
        // karena IndexOf akan mencari ulang di HandleTargetCyclingInput()
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