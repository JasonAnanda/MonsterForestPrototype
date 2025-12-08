using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    // Dictionary untuk memetakan nama command ke KeyCode yang sesuai
    private readonly Dictionary<string, KeyCode> commandKeyMap = new()
    {
        // --- Key Mapping untuk Command Ritme ---
        {"A", KeyCode.A},
        {"S", KeyCode.S},
        {"J", KeyCode.J},
        {"K", KeyCode.K},
        
        // --- Key Mapping untuk Jeda/Istirahat ---
        // Simbol '・' dipetakan ke Spacebar (Spasi)
        {"・", KeyCode.Space}, 

        // --- Key Mapping untuk Cycling Target (Q/E tetap) ---
        // Q/E ditangani di TargetManager, tapi tetap dicatat di sini jika diperlukan
        {"CyclePrev", KeyCode.Q},
        {"CycleNext", KeyCode.E}
    };

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    /// <summary>
    /// Mengecek apakah tombol fisik untuk command tertentu (e.g., "A", "・") baru saja ditekan.
    /// Ini dipanggil oleh MonsterSequence.cs untuk memproses input pemain.
    /// </summary>
    /// <param name="commandName">Nama command (e.g., "A", "S", "J", "K", "・").</param>
    /// <returns>True jika tombol yang dipetakan baru saja ditekan.</returns>
    public bool IsCommandPressed(string commandName)
    {
        if (commandKeyMap.TryGetValue(commandName, out KeyCode key))
        {
            // PENTING: Gunakan GetKeyDown agar input hanya terdeteksi sekali per frame
            return Input.GetKeyDown(key);
        }
        return false;
    }

    /// <summary>
    /// Mengecek apakah tombol fisik untuk command tertentu (e.g., "A", "・") sedang ditahan.
    /// (Tidak digunakan saat ini, tapi bisa berguna untuk mekanik 'hold').
    /// </summary>
    /// <param name="commandName">Nama command (e.g., "A", "S", "J", "K", "・").</param>
    /// <returns>True jika tombol yang dipetakan sedang ditahan.</returns>
    public bool IsCommandDown(string commandName)
    {
        if (commandKeyMap.TryGetValue(commandName, out KeyCode key))
        {
            return Input.GetKey(key);
        }
        return false;
    }
}