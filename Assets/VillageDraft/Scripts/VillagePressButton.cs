using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>A ray or direct select of an XRSimpleInteractable counts as a button press.</summary>
[RequireComponent(typeof(XRSimpleInteractable))]
public sealed class VillagePressButton : MonoBehaviour
{
    public VillageProgressBoard progressBoard;
    public string objectiveId;
    [Tooltip("-1 accepts any unfinished step. Set 0, 1, etc. to enforce order.")]
    public int requiredCurrentStep = -1;
    public bool oneShot = true;
    public Renderer buttonVisual;
    public VillageVenueTaskGuide taskGuide;
    public Color activatedColor = new Color(0.28f, 0.9f, 0.56f);
    public UnityEvent onActivated;

    XRSimpleInteractable interactable;
    bool activated;

    void Awake() => interactable = GetComponent<XRSimpleInteractable>();

    void OnEnable() => interactable.selectEntered.AddListener(OnSelected);

    void OnDisable() => interactable.selectEntered.RemoveListener(OnSelected);

    void OnSelected(SelectEnterEventArgs _)
    {
        if (!Press())
            taskGuide?.ShowRetryHint();
    }

    /// <summary>Also permits mouse or scripted demos to activate the same task.</summary>
    public bool Press()
    {
        if (oneShot && activated)
            return false;
        if (progressBoard == null || !progressBoard.TryAdvanceObjective(objectiveId, requiredCurrentStep))
            return false;

        activated = true;
        if (buttonVisual != null)
        {
            foreach (var material in buttonVisual.materials)
            {
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", activatedColor);
                if (material.HasProperty("_Color")) material.SetColor("_Color", activatedColor);
            }
        }
        onActivated?.Invoke();
        return true;
    }
}
