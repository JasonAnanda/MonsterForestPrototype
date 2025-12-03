using UnityEngine;
using UnityEngine.InputSystem;

public enum InputMode
{
    Keyboard,
    Controller
}

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Tentukan mode input saat ini
    public InputMode CurrentMode
    {
        get
        {
            if (Gamepad.current != null) return InputMode.Controller;
            return InputMode.Keyboard;
        }
    }

    // Map command internal ke keyboard key
    public Key GetKeyForCommand(string cmd)
    {
        switch (cmd)
        {
            case "A": return Key.A;
            case "S": return Key.S;
            case "J": return Key.J;
            case "K": return Key.K;
            case "・": return Key.Space; // tambahan untuk empty
        }
        return Key.None;
    }

    // Cek apakah command ditekan
    public bool IsCommandPressed(string cmd)
    {
        if (CurrentMode == InputMode.Controller && Gamepad.current != null)
        {
            switch (cmd)
            {
                case "X": return Gamepad.current.buttonWest.wasPressedThisFrame;
                case "A": return Gamepad.current.buttonSouth.wasPressedThisFrame;
                case "Y": return Gamepad.current.buttonNorth.wasPressedThisFrame;
                case "B": return Gamepad.current.buttonEast.wasPressedThisFrame;
                case "・": return Gamepad.current.startButton.wasPressedThisFrame; // empty
            }
        }
        else // keyboard
        {
            Key key = GetKeyForCommand(cmd);
            if (key == Key.None) return false;
            return Keyboard.current[key] != null && Keyboard.current[key].wasPressedThisFrame;
        }

        return false;
    }
}
