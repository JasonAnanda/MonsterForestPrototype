// PENTING: Struktur ini sekarang diubah untuk memutar SATU klip audio utuh (Pattern/Lagu)
// alih-alih memutar klip individual per command key (A/S/J/K).
// Ini meniru logika "Echo Forest" di mana audio pola dimainkan secara utuh di awal fase Demo.

using UnityEngine;
using System.Collections.Generic;

public class MonsterSoundPlayer : MonoBehaviour
{
    [Header("Audio Source")]
    public AudioSource audioSource;

    // --- KLIP INDIVIDU LAMA (LOGIKA PER-BEAT, SEKARANG DIKOMENTARI/DIHAPUS) ---
    // Klip-klip ini tidak lagi digunakan untuk memutar suara monster per-beat.
    // Jika Anda masih membutuhkannya untuk suara input user/feedback, pertimbangkan 
    // memindahkannya ke script lain (misalnya, UserInputHandler).

    /*
    [Header("Command Clips (Legacy - Not used for Pattern Playback)")]
    public AudioClip clipA;
    public AudioClip clipS;
    public AudioClip clipJ;
    public AudioClip clipK;
    private Dictionary<string, AudioClip> commandClips; 
    */

    [Header("Pattern Audio Clips")]
    [Tooltip("Daftar klip audio yang berisi pola ritme UTUH. Indeks 0, 1, 2, dst. akan sesuai dengan Pattern ID.")]
    public List<AudioClip> patternClips;

    // Klip spawn/cue bisa ditaruh di sini jika MonsterSequence tidak mengelolanya
    // public AudioClip spawnClip; 

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Setup Dictionary lama dihilangkan karena kita tidak lagi mapping key ke klip individu.
        /*
        commandClips = new Dictionary<string, AudioClip>
        {
            { "A", clipA },
            { "S", clipS },
            { "J", clipJ },
            { "K", clipK }
        };
        */
    }

    /// <summary>
    /// Memainkan klip audio pola UTUH berdasarkan indeks (sesuai Pattern ID).
    /// Ini adalah implementasi dari Monster.play_voice() di skrip Python.
    /// </summary>
    /// <param name="patternIndex">Indeks klip pola dalam daftar 'patternClips' (misalnya, Pattern ID 1 adalah Index 0).</param>
    public void PlayPattern(int patternIndex)
    {
        if (audioSource == null)
        {
            Debug.LogError("AudioSource tidak ditemukan pada MonsterSoundPlayer.");
            return;
        }

        // Pastikan indeks valid
        // Kita menggunakan patternIndex - 1 karena di Unity List dimulai dari 0,
        // sementara di skrip Python Pattern ID dimulai dari 1.
        int listIndex = patternIndex - 1;

        if (patternClips != null && listIndex >= 0 && listIndex < patternClips.Count)
        {
            AudioClip clip = patternClips[listIndex];
            if (clip != null)
            {
                // Hentikan suara yang sedang berjalan (jika ada) dan mainkan klip pola utuh.
                audioSource.Stop();
                audioSource.PlayOneShot(clip);
                Debug.Log($"Memainkan Pattern Audio Clip ID: {patternIndex} (List Index: {listIndex})");
            }
            else
            {
                Debug.LogWarning($"Klip pola pada ID {patternIndex} (Index {listIndex}) tidak ditemukan. Pastikan sudah di-assign di Inspector.");
            }
        }
        else
        {
            Debug.LogWarning($"ID pola {patternIndex} di luar batas atau patternClips kosong. Jumlah klip terdaftar: {(patternClips != null ? patternClips.Count : 0)}");
        }
    }

    /// <summary>
    /// Mainkan suara command. Command "ÅE" tidak akan bersuara.
    /// CATATAN PENTING: Fungsi ini merupakan LOGIKA LAMA (per-beat).
    /// JANGAN gunakan ini untuk memutar pola suara monster. Gunakan PlayPattern().
    /// </summary>
    public void PlaySound(string command)
    {
        // Logika lama ini sudah tidak relevan untuk suara monster karena sudah menggunakan PlayPattern().
        Debug.LogWarning($"PlaySound({command}) dipanggil. Metode ini biasanya sudah tidak digunakan untuk pola monster. Gunakan PlayPattern().");

        // Hapus atau uncomment jika Anda ingin mempertahankan fungsionalitas per-beat untuk keperluan lain (misal, input pemain)
        /*
        if (command == "ÅE") return;
        if (commandClips != null && commandClips.ContainsKey(command))
        {
            AudioClip clip = commandClips[command];
            if (clip != null)
                audioSource.PlayOneShot(clip);
        }
        */
    }

    /// <summary>
    /// Mainkan sound custom, misal spawnClip atau cue.
    /// </summary>
    public void PlayCustomSound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Menghentikan suara yang sedang dimainkan. Dipanggil saat target dilepas.
    /// </summary>
    public void StopCurrentSound()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}