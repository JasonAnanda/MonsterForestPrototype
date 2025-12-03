using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class CommandIconUI : MonoBehaviour
{
    [Header("Per-command sprites (assign per prefab instance)")]
    public Sprite defaultSprite;        // e.g. A_default
    public Sprite perfectSprite;        // e.g. A_perfect (green)
    public Sprite earlyOrLateSprite;    // e.g. A_early (orange)
    public Sprite emptySprite;          // transparent / empty

    private Image img;

    void Awake()
    {
        img = GetComponent<Image>();
        if (defaultSprite != null) img.sprite = defaultSprite;
    }

    public void SetPerfect()
    {
        if (perfectSprite != null) img.sprite = perfectSprite;
    }

    public void SetEarly()
    {
        if (earlyOrLateSprite != null) img.sprite = earlyOrLateSprite;
    }

    public void SetLate()
    {
        if (earlyOrLateSprite != null) img.sprite = earlyOrLateSprite;
    }

    public void SetMiss()
    {
        if (emptySprite != null) img.sprite = emptySprite;
    }

    public void ResetToDefault()
    {
        if (defaultSprite != null) img.sprite = defaultSprite;
    }
}
