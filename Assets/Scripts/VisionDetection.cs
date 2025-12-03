// VisionDetector.cs
using UnityEngine;

public class VisionDetector : MonoBehaviour
{
    private void Start()
    {
        // Semua monster awalnya invisible
        GameObject[] monsters = GameObject.FindGameObjectsWithTag("Monster");
        foreach (GameObject m in monsters)
        {
            SpriteRenderer sr = m.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.enabled = false;

            // Disable MonsterSequence script supaya tidak jalan dulu
            MonsterSequence seq = m.GetComponent<MonsterSequence>();
            if (seq != null)
                seq.enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Monster"))
        {
            SpriteRenderer sr = collision.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.enabled = true;

            MonsterSequence seq = collision.GetComponent<MonsterSequence>();
            if (seq != null)
                seq.ActivateSequence(); // ⬅️ Tampilkan UI command
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Monster"))
        {
            SpriteRenderer sr = collision.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.enabled = false;
        }
    }
}
