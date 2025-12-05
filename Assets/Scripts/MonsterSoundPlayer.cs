using UnityEngine;
using System.Collections.Generic;

public class MonsterSoundPlayer : MonoBehaviour
{
    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Command Clips")]
    public AudioClip clipA;
    public AudioClip clipS;
    public AudioClip clipJ;
    public AudioClip clipK;

    // optional: spawn sound bisa juga ditaruh di sini
    // tapi kalau sudah ada di MonsterSequence, bisa langsung pakai itu

    private Dictionary<string, AudioClip> commandClips;

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // setup mapping command -> clip
        commandClips = new Dictionary<string, AudioClip>
        {
            { "A", clipA },
            { "S", clipS },
            { "J", clipJ },
            { "K", clipK }
        };
    }

    /// <summary>
    /// Mainkan suara command. Command "ÅE" tidak akan bersuara.
    /// </summary>
    public void PlaySound(string command)
    {
        if (command == "ÅE") return;

        if (commandClips != null && commandClips.ContainsKey(command))
        {
            AudioClip clip = commandClips[command];
            if (clip != null)
                audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Mainkan sound custom, misal spawnClip.
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