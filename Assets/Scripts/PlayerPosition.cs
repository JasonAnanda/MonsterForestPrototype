using UnityEngine;

public class PlayerPosition : MonoBehaviour
{
    [Header("Fixed Position (can override in Inspector)")]
    public Vector3 fixedPosition;

    void Awake()
    {
        // Ambil posisi awal di scene jika belum di-set di Inspector
        if (fixedPosition == Vector3.zero)
        {
            fixedPosition = transform.position;
        }

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

    // Opsional: update posisi via skrip
    public void SetPosition(Vector3 newPos)
    {
        fixedPosition = newPos;
        transform.position = fixedPosition;
    }
}
