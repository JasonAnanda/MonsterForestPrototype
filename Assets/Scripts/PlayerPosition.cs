using UnityEngine;

public class PlayerPosition : MonoBehaviour
{
    // Instance Singleton diperlukan jika TargetManager memanggil fungsi SetPosition via Instance
    public static PlayerPosition Instance { get; private set; }

    [Header("Fixed Position (can override in Inspector)")]
    [Tooltip("Posisi tetap yang akan digunakan Player.")]
    public Vector3 fixedPosition;

    void Awake()
    {
        // Implementasi Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        // === PERUBAHAN PENTING: MENGUNCI POSISI ===
        // 1. Abaikan nilai Inspector. Paksa posisi menjadi yang diminta user.
        fixedPosition = new Vector3(-7.5f, -3.22f, 0f);
        // ==========================================

        // Nonaktifkan skrip PlayerMovement supaya player tidak bisa digerakkan
        var movement = GetComponent<PlayerMovement>();
        if (movement != null)
            movement.enabled = false;
    }

    void Start()
    {
        // Set player ke posisi fix
        transform.position = fixedPosition;
    }

    void Update()
    {
        // KUNCI PENTING: Setiap frame, paksa posisi Player kembali ke fixedPosition.
        // Ini mengatasi potensi pergerakan akibat fisika (collider/rigidbody) atau skrip lain.
        if (transform.position != fixedPosition)
        {
            transform.position = fixedPosition;
        }
    }

    /// <summary>
    /// Dipanggil oleh TargetManager. Input posisi baru diabaikan,
    /// Player tetap dikunci pada fixedPosition yang sudah ditetapkan.
    /// </summary>
    public void SetPosition(Vector3 newPos)
    {
        // Kami mengabaikan newPos. Player tetap di fixedPosition yang hardcode.
        transform.position = fixedPosition;
    }
}