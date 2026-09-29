using UnityEngine;

/// <summary>Each completed trade adds its finished contribution to the shared festival square.</summary>
public sealed class VillageFestivalSquare : MonoBehaviour
{
    public VillageProgressBoard progressBoard;
    public string[] objectiveIds;
    public GameObject[] contributions;
    public TextMesh bannerText;

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
        if (progressBoard == null || objectiveIds == null || contributions == null) return;
        int ready = 0;
        for (int i = 0; i < objectiveIds.Length && i < contributions.Length; i++)
        {
            bool complete = progressBoard.GetCompletedSteps(objectiveIds[i]) >= 10;
            if (contributions[i] != null) contributions[i].SetActive(complete);
            if (complete) ready++;
        }
        if (bannerText != null)
            bannerText.text = ready == objectiveIds.Length
                ? "THE VILLAGE FESTIVAL IS READY!"
                : "PREPARING THE FESTIVAL  " + ready + "/" + objectiveIds.Length;
    }
}
