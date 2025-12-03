using UnityEngine;

public class BGLooper : MonoBehaviour
{
    public Transform otherBG; // drag BG2 here if this is BG1
    private float width;

    void Start()
    {
        // estimate width from sprite renderer bounds
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        width = sr.bounds.size.x;
    }

    void Update()
    {
        if (transform.position.x <= -width)
        {
            // move this BG to the right of the otherBG
            transform.position = new Vector3(otherBG.position.x + width, transform.position.y, transform.position.z);
        }
    }
}
