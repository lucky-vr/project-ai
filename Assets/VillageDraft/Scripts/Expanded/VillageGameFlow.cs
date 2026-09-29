using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>Village completion display and a VR-selectable scene replay control.</summary>
public sealed class VillageGameFlow : MonoBehaviour
{
    public VillageProgressBoard progressBoard;
    public TextMesh statusText;
    public XRSimpleInteractable replayButton;

    void OnEnable()
    {
        if (progressBoard != null)
        {
            progressBoard.onProgressChanged.AddListener(Refresh);
            progressBoard.onVillageComplete.AddListener(OnVillageComplete);
        }
        if (replayButton != null)
            replayButton.selectEntered.AddListener(OnReplaySelected);
        Refresh();
    }

    void OnDisable()
    {
        if (progressBoard != null)
        {
            progressBoard.onProgressChanged.RemoveListener(Refresh);
            progressBoard.onVillageComplete.RemoveListener(OnVillageComplete);
        }
        if (replayButton != null)
            replayButton.selectEntered.RemoveListener(OnReplaySelected);
    }

    void OnVillageComplete() => Refresh();

    public void Refresh()
    {
        if (statusText == null || progressBoard == null) return;
        statusText.text = progressBoard.IsVillageComplete
            ? "VILLAGE COMPLETE!\nAll 15 places explored.\nPress REPLAY to start again."
            : "VILLAGE QUEST\n" + progressBoard.CompletedObjectiveCount +
              "/" + progressBoard.objectives.Length + " places complete\nExplore, grab, place and press.";
    }

    void OnReplaySelected(SelectEnterEventArgs _) => Replay();

    public void Replay()
    {
        foreach (var menu in Object.FindObjectsByType<VillageControllerMenu>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            menu.ResetCues();
        var scene = SceneManager.GetActiveScene();
        if (scene.buildIndex >= 0)
            SceneManager.LoadScene(scene.buildIndex);
        else
            SceneManager.LoadScene(scene.name);
    }
}
