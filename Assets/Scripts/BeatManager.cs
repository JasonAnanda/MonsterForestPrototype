using UnityEngine;
using System;

public class BeatManager : MonoBehaviour
{
    public static BeatManager Instance;
    public float bpm = 120f;
    public AudioSource beatAudio; // optional
    private float beatInterval;
    private float timer;

    public static event Action OnHalfBeat; // early
    public static event Action OnBeat;     // perfect
    public static event Action OnLateBeat; // late

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        beatInterval = 60f / Mathf.Max(0.0001f, bpm);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= beatInterval)
        {
            timer -= beatInterval;

            // invoke in this order: early, perfect, late (you may adapt timing inside player)
            OnHalfBeat?.Invoke();
            OnBeat?.Invoke();
            OnLateBeat?.Invoke();

            if (beatAudio != null) beatAudio.Play();
            Debug.Log("[BeatManager] Beat fired at: " + Time.time);
        }
    }
}
