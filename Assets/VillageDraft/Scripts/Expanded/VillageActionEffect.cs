using UnityEngine;
using UnityEngine.Events;

/// <summary>Small visible result for a player action, independent of the progress logic.</summary>
public sealed class VillageActionEffect : MonoBehaviour
{
    public enum EffectKind { Raise, Slide, Turn, Grow, Reveal }

    public EffectKind effectKind;
    public VillagePressButton sourceButton;
    public Transform target;
    public Vector3 direction = Vector3.up;
    public float distance = 0.35f;
    public float degrees = 90f;
    public float sizeMultiplier = 1.5f;
    public GameObject revealObject;

    bool played;
    public bool HasPlayed => played;

    void Awake()
    {
        if (effectKind == EffectKind.Reveal && revealObject != null)
            revealObject.SetActive(false);
    }

    void OnEnable()
    {
        if (sourceButton == null) return;
        if (sourceButton.onActivated == null) sourceButton.onActivated = new UnityEvent();
        sourceButton.onActivated.AddListener(Play);
    }

    void OnDisable()
    {
        if (sourceButton != null && sourceButton.onActivated != null)
            sourceButton.onActivated.RemoveListener(Play);
    }

    public void Play()
    {
        if (played) return;
        played = true;
        if (effectKind == EffectKind.Reveal)
        {
            if (revealObject != null) revealObject.SetActive(true);
            return;
        }
        if (target == null) return;
        switch (effectKind)
        {
            case EffectKind.Raise:
            case EffectKind.Slide:
                target.localPosition += direction.normalized * distance;
                break;
            case EffectKind.Turn:
                target.Rotate(direction.normalized, degrees, Space.Self);
                break;
            case EffectKind.Grow:
                target.localScale *= sizeMultiplier;
                break;
        }
    }
}
