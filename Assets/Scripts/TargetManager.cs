using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Bertanggung jawab untuk mengelola monster mana yang saat ini menjadi target input pemain.
/// Sekarang menggunakan input klik mouse.
/// </summary>
public class TargetManager : MonoBehaviour
{
    public static TargetManager Instance;

    [Header("Detection Settings")]
    [Tooltip("Radius deteksi target (hanya untuk visualisasi Gizmos/penargetan otomatis).")]
    public float detectionRadius = 15f;
    public LayerMask monsterLayer;

    private MonsterSequence currentTarget;
    // detectedMonsters sekarang hanya digunakan untuk fungsi deteksi otomatis jika diperlukan
    private readonly List<MonsterSequence> detectedMonsters = new();
    // registeredMonsters tetap menyimpan semua monster aktif di scene
    private readonly List<MonsterSequence> registeredMonsters = new();
    private int currentTargetIndex = -1; // Tidak terlalu relevan lagi dengan input mouse

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

    // Memicu visual flash pada target saat System Beat (240 BPM)
    private void SystemBeatFlash(int rhythmType)
    {
        if (currentTarget != null)
            currentTarget.FlashHighlight();
    }

    void Update()
    {
        DetectMonsters(); // Memperbarui daftar monster yang berada dalam radius (opsional)
        HandleMouseInput(); // Mengelola input mouse
    }

    /// <summary>
    /// Menangani input klik mouse untuk memilih monster baru.
    /// </summary>
    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0)) // Cek klik kiri mouse
        {
            // Konversi posisi mouse di layar menjadi posisi di dunia game (World Space)
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0; // Pastikan Z=0 untuk 2D

            // Gunakan Physics2D.OverlapPoint untuk mendeteksi apakah ada Collider2D monster
            Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos, monsterLayer);

            if (hit != null)
            {
                // Coba dapatkan komponen MonsterSequence dari objek yang diklik
                if (hit.TryGetComponent(out MonsterSequence seq))
                {
                    // Hanya pilih monster yang terdaftar (aktif)
                    if (registeredMonsters.Contains(seq))
                    {
                        SetManualTarget(seq);
                    }
                }
            }
            // Tambahkan logika di sini jika Anda ingin klik di luar monster mereset target
            /*
            else
            {
                 // Jika mengklik area kosong, nonaktifkan target
                 SetManualTarget(null);
            }
            */
        }
    }

    // Fungsi `HandleTargetCyclingInput` yang lama telah dihapus atau dikosongkan
    // untuk mengutamakan input mouse.

    public void SetManualTarget(MonsterSequence target)
    {
        if (currentTarget == target) return;

        // Menonaktifkan status target pada monster lama
        if (currentTarget != null)
        {
            currentTarget.SetTarget(false);
        }

        currentTarget = target;

        if (currentTarget != null)
        {
            // Mengaktifkan status target pada monster baru
            currentTarget.SetTarget(true);

            // Jika monster baru dipilih, pastikan urutan dimulai
            currentTarget.ActivateSequence();

            // Update indeks (walaupun tidak digunakan untuk cycling, ini bisa untuk debugging)
            currentTargetIndex = registeredMonsters.IndexOf(target);
        }
        else
        {
            currentTargetIndex = -1;
        }
    }

    void DetectMonsters()
    {
        // Fungsi ini sekarang lebih opsional, hanya untuk mendeteksi monster dalam radius
        // jika kita ingin fungsi 'SetTarget' otomatis (misal: jika tidak ada target, pilih yang terdekat)

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

        // Opsional: Jika tidak ada target, target monster yang baru di-register
        // if (currentTarget == null)
        // {
        //     SetManualTarget(m);
        // }
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
            // [LOGIKA DIHAPUS]: Kami tidak lagi memilih target baru secara otomatis.
            // Setelah monster mati/hilang, currentTarget menjadi null.
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