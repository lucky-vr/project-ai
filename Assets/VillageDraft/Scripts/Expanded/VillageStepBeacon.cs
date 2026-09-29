using UnityEngine;

/// <summary>Highlights the next physical worktop so a player can follow the task guide.</summary>
public sealed class VillageStepBeacon : MonoBehaviour
{
    public VillageProgressBoard progressBoard;
    public string objectiveId;
    public int stepIndex;
    public Renderer beacon;
    public Renderer band;

    MaterialPropertyBlock propertyBlock;

    void Awake() => propertyBlock = new MaterialPropertyBlock();

    void OnEnable()
    {
        if (progressBoard != null && progressBoard.onProgressChanged != null)
            progressBoard.onProgressChanged.AddListener(Refresh);
        Refresh();
    }

    void OnDisable()
    {
        if (progressBoard != null && progressBoard.onProgressChanged != null)
            progressBoard.onProgressChanged.RemoveListener(Refresh);
    }

    public void Refresh()
    {
        if (progressBoard == null) return;
        int current = progressBoard.GetCompletedSteps(objectiveId);
        if (current < 0) return;
        Color color = stepIndex < current ? new Color(.18f, .75f, .43f) :
            stepIndex == current ? new Color(1f, .72f, .14f) : new Color(.22f, .28f, .32f);
        Paint(band, color);
        if (beacon != null)
        {
            beacon.enabled = stepIndex == current;
            if (beacon.enabled) Paint(beacon, color);
        }
    }

    void Paint(Renderer target, Color color)
    {
        if (target == null) return;
        target.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", color);
        propertyBlock.SetColor("_Color", color);
        target.SetPropertyBlock(propertyBlock);
        propertyBlock.Clear();
    }
}
