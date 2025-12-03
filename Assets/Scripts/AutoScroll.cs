using UnityEngine;

public class AutoScroll : MonoBehaviour
{
    public float scrollSpeed = 2f;

    void Update()
    {
        // move left every frame
        transform.Translate(Vector3.left * scrollSpeed * Time.deltaTime);
    }
}
