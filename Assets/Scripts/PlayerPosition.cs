using UnityEngine;

public class PlayerPosition : MonoBehaviour
{
    [Header("Fixed Position (can override in Inspector)")]
    [Tooltip("Posisi tetap yang akan digunakan Player. Jika Vector3.zero, akan menggunakan posisi awal Scene.")]
    public Vector3 fixedPosition;

    void Awake()
    {
        // 1. Ambil posisi awal di scene jika belum di-set di Inspector
        if (fixedPosition == Vector3.zero)
        {
            // PENTING: Gunakan posisi awal transform jika fixedPosition masih default (0,0,0)
            fixedPosition = transform.position;
        }

        // 2. Nonaktifkan skrip PlayerMovement supaya player tidak bisa digerakkan
        // Ini memastikan tidak ada input yang memicu pergerakan bebas.
        var movement = GetComponent<PlayerMovement>();
        if (movement != null)
            movement.enabled = false;

        // Catatan: Jika PlayerMovement tidak ada, code ini aman.
    }

    void Start()
    {
        // Set player ke posisi fix
        transform.position = fixedPosition;
    }

    void Update()
    {
        // 3. KUNCI PENTING: Setiap frame, paksa posisi Player kembali ke fixedPosition.
        // Ini mengatasi potensi pergerakan akibat fisika (collider/rigidbody) atau skrip lain.
        if (transform.position != fixedPosition)
        {
            transform.position = fixedPosition;
        }
    }

    // Opsional: update posisi via skrip
    public void SetPosition(Vector3 newPos)
    {
        fixedPosition = newPos;
        // Langsung terapkan posisi baru
        transform.position = fixedPosition;
    }
}