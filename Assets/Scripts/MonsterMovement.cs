using UnityEngine;

public class MonsterMovement : MonoBehaviour
{
    public float moveSpeed = 1f;  // kecepatan jalan

    void Update()
    {
        transform.Translate(Vector2.left * moveSpeed * Time.deltaTime);
    }
}
