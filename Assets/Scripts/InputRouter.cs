using UnityEngine;

public class InputRouter : MonoBehaviour
{
    // contoh: kita kirim string command "A", "B", "C" saat tombol ditekan
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
            TargetManager.Instance?.ForwardInput("A");
        if (Input.GetKeyDown(KeyCode.S))
            TargetManager.Instance?.ForwardInput("S");
        if (Input.GetKeyDown(KeyCode.D))
            TargetManager.Instance?.ForwardInput("D");
    }
}
