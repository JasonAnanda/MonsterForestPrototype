using UnityEngine;

public class PlayerSoundPlayer : MonoBehaviour
{
    public AudioSource audioSource;

    [Header("Keyboard Clips")]
    public AudioClip clipA;
    public AudioClip clipS;
    public AudioClip clipJ;
    public AudioClip clipK;

    void Update()
    {
        if (InputManager.Instance == null) return;

        if (InputManager.Instance.IsCommandPressed("A"))
            PlaySound("A");

        if (InputManager.Instance.IsCommandPressed("S"))
            PlaySound("S");

        if (InputManager.Instance.IsCommandPressed("J"))
            PlaySound("J");

        if (InputManager.Instance.IsCommandPressed("K"))
            PlaySound("K");
    }

    public void PlaySound(string command)
    {
        if (audioSource == null) return;

        switch (command)
        {
            case "A": audioSource.PlayOneShot(clipA); break;
            case "S": audioSource.PlayOneShot(clipS); break;
            case "J": audioSource.PlayOneShot(clipJ); break;
            case "K": audioSource.PlayOneShot(clipK); break;
        }
    }
}
