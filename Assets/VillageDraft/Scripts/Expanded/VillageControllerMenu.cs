using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Fifteen controller-mounted UI cues, each operating a different visible venue mechanism.</summary>
public sealed class VillageControllerMenu : MonoBehaviour
{
    public string[] venueIds = Array.Empty<string>();
    public Button[] buttons = Array.Empty<Button>();
    public VillageVenueExperience[] venues = Array.Empty<VillageVenueExperience>();
    public Text statusText;

    UnityAction[] callbacks;
    VillageProgressBoard progressBoard;
    int lastTotalSteps;

    public int VenueButtonCount => buttons == null ? 0 : buttons.Length;

    void OnEnable()
    {
        if (buttons == null) return;
        progressBoard = venues != null && venues.Length > 0 && venues[0] != null
            ? venues[0].progressBoard : null;
        if (progressBoard != null)
        {
            lastTotalSteps = progressBoard.CompletedStepCount;
            progressBoard.onProgressChanged.AddListener(OnProgressChanged);
        }
        callbacks = new UnityAction[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            callbacks[i] = () => Cue(index);
            if (buttons[i] != null) buttons[i].onClick.AddListener(callbacks[i]);
        }
        if (statusText != null) statusText.text = "POINT + TRIGGER  •  CUE A VENUE ITEM";
    }

    void OnDisable()
    {
        if (progressBoard != null && progressBoard.onProgressChanged != null)
            progressBoard.onProgressChanged.RemoveListener(OnProgressChanged);
        if (buttons == null || callbacks == null) return;
        for (int i = 0; i < buttons.Length && i < callbacks.Length; i++)
            if (buttons[i] != null && callbacks[i] != null)
                buttons[i].onClick.RemoveListener(callbacks[i]);
        callbacks = null;
    }

    public void Cue(int index)
    {
        if (venues == null || venueIds == null || index < 0 ||
            index >= venues.Length || index >= venueIds.Length || venues[index] == null)
            return;
        venues[index].ToggleRemoteCue();
        if (statusText != null)
            statusText.text = venueIds[index].ToUpperInvariant() +
                              (venues[index].RemoteCueActive ? " FESTIVAL ITEM ON" : " FESTIVAL ITEM OFF");
    }

    void OnProgressChanged()
    {
        if (progressBoard == null) return;
        int total = progressBoard.CompletedStepCount;
        if (total < lastTotalSteps) ResetCues();
        lastTotalSteps = total;
    }

    public void ResetCues()
    {
        if (venues != null)
            foreach (var venue in venues)
                if (venue != null) venue.ResetRemoteCue();
        if (statusText != null) statusText.text = "POINT + TRIGGER  •  CUE A VENUE ITEM";
    }
}
