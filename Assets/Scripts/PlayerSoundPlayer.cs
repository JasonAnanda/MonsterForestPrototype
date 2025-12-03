using UnityEngine;

public class PlayerSoundPlayer : MonoBehaviour
{
    public AudioSource audioSource;

    [Header("Controller Clips")]
    public AudioClip clipA;
    public AudioClip clipS;
    public AudioClip clipJ;
    public AudioClip clipK;

    [Header("Keyboard Clips")]
    public AudioClip clipKeyboardA;
    public AudioClip clipKeyboardS;
    public AudioClip clipKeyboardJ;
    public AudioClip clipKeyboardK;

    void Update()
    {
        if (InputManager.Instance == null) return;

        string[] commands = new string[] { "A", "S", "J", "K", "ÅE" };
        foreach (var cmd in commands)
        {
            if (InputManager.Instance.IsCommandPressed(cmd))
            {
                PlaySound(cmd);
            }
        }
    }

    public void PlaySound(string command)
    {
        if (audioSource == null) return;

        bool isController = InputManager.Instance.CurrentMode == InputMode.Controller;

        if (command == "ÅE") return; // silent untuk empty

        if (isController)
        {
            switch (command)
            {
                case "A": audioSource.PlayOneShot(clipA); break;
                case "S": audioSource.PlayOneShot(clipS); break;
                case "J": audioSource.PlayOneShot(clipJ); break;
                case "K": audioSource.PlayOneShot(clipK); break;
            }
        }
        else // keyboard mode
        {
            switch (command)
            {
                case "A": audioSource.PlayOneShot(clipKeyboardA); break;
                case "S": audioSource.PlayOneShot(clipKeyboardS); break;
                case "J": audioSource.PlayOneShot(clipKeyboardJ); break;
                case "K": audioSource.PlayOneShot(clipKeyboardK); break;
            }
        }
    }
}
