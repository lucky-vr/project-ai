using UnityEngine;

/// <summary>Pairs the first Market pickup and basket with temporary shelf markers.</summary>
public sealed class VillageFirstMarketCue : MonoBehaviour
{
    public VillageProgressBoard progressBoard;
    public Light sourceLight;
    public Light targetLight;
    public GameObject sourceMarker;
    public GameObject targetMarker;

    Vector3 sourceMarkerScale;
    Vector3 targetMarkerScale;

    void Awake()
    {
        if (sourceMarker != null) sourceMarkerScale = sourceMarker.transform.localScale;
        if (targetMarker != null) targetMarkerScale = targetMarker.transform.localScale;
    }

    void OnEnable()
    {
        if (progressBoard != null)
            progressBoard.onProgressChanged.AddListener(Refresh);
        Refresh();
    }

    void OnDisable()
    {
        if (progressBoard != null)
            progressBoard.onProgressChanged.RemoveListener(Refresh);
    }

    void Refresh()
    {
        bool firstActionPending = progressBoard != null &&
                                  progressBoard.GetCompletedSteps("market") == 0;
        if (sourceLight != null) sourceLight.enabled = firstActionPending;
        if (targetLight != null) targetLight.enabled = firstActionPending;
        if (sourceMarker != null) sourceMarker.SetActive(firstActionPending);
        if (targetMarker != null) targetMarker.SetActive(firstActionPending);
    }

    void Update()
    {
        if (sourceLight == null || !sourceLight.enabled) return;
        float phase = Time.time * 3f;
        sourceLight.intensity = 3.0f + .75f * Mathf.Sin(phase);
        if (targetLight != null)
            targetLight.intensity = 3.0f + .75f * Mathf.Sin(phase + Mathf.PI);
        if (sourceMarker != null)
            sourceMarker.transform.localScale = sourceMarkerScale * (1f + .04f * Mathf.Sin(phase));
        if (targetMarker != null)
            targetMarker.transform.localScale = targetMarkerScale * (1f + .04f * Mathf.Sin(phase + Mathf.PI));
    }
}
