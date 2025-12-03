using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public bool IsCommandPressed(string cmd)
    {
        switch (cmd)
        {
            case "A":
                return Input.GetKeyDown(KeyCode.A);

            case "S":
                return Input.GetKeyDown(KeyCode.S);

            case "J":
                return Input.GetKeyDown(KeyCode.J);

            case "K":
                return Input.GetKeyDown(KeyCode.K);

            case "・":   // untuk empty command
                return Input.GetKeyDown(KeyCode.Space);

            default:
                return false;
        }
    }
}
