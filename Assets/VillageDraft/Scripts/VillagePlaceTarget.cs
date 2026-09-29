using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>Counts a correct object placed in an XR socket.</summary>
[RequireComponent(typeof(XRSocketInteractor))]
public sealed class VillagePlaceTarget : MonoBehaviour
{
    public VillageProgressBoard progressBoard;
    public string objectiveId;
    public XRGrabInteractable requiredObject;
    [Tooltip("-1 accepts any unfinished step. Set 1 for a second step, for example.")]
    public int requiredCurrentStep = -1;
    public bool oneShot = true;
    public Renderer targetVisual;
    public VillageVenueTaskGuide taskGuide;
    public Color activatedColor = new Color(0.28f, 0.9f, 0.56f);
    public UnityEvent onPlaced;

    XRSocketInteractor socket;
    bool activated;

    void Awake() => socket = GetComponent<XRSocketInteractor>();

    void OnEnable() => socket.selectEntered.AddListener(OnSelected);

    void OnDisable() => socket.selectEntered.RemoveListener(OnSelected);

    void OnSelected(SelectEnterEventArgs args)
    {
        if (args.interactableObject is XRGrabInteractable grab)
        {
            if (!TryPlace(grab))
                taskGuide?.ShowRetryHint();
        }
    }

    public bool TryPlace(XRGrabInteractable item)
    {
        if (item == null || (requiredObject != null && item != requiredObject) ||
            (oneShot && activated) || progressBoard == null ||
            !progressBoard.TryAdvanceObjective(objectiveId, requiredCurrentStep))
            return false;

        activated = true;
        if (targetVisual != null)
        {
            foreach (var material in targetVisual.materials)
            {
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", activatedColor);
                if (material.HasProperty("_Color")) material.SetColor("_Color", activatedColor);
            }
        }
        onPlaced?.Invoke();
        return true;
    }
}
