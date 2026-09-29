using UnityEngine;

/// <summary>Non-spatial cues keep village task feedback audible anywhere in the VR scene.</summary>
public sealed class VillageAudioDirector : MonoBehaviour
{
    public VillageProgressBoard progressBoard;
    public AudioSource cueSource;
    public AudioSource ambienceSource;
    public AudioClip actionCue;
    public AudioClip venueCompleteCue;
    public AudioClip villageCompleteCue;
    public AudioClip ambienceLoop;

    int lastCompletedPlaces;
    int lastCompletedSteps;

    void OnEnable()
    {
        if (progressBoard == null) return;
        lastCompletedPlaces = progressBoard.CompletedObjectiveCount;
        lastCompletedSteps = progressBoard.CompletedStepCount;
        progressBoard.onProgressChanged.AddListener(OnProgressChanged);
        progressBoard.onVillageComplete.AddListener(OnVillageComplete);
    }

    void Start()
    {
        if (ambienceSource == null || ambienceLoop == null) return;
        ambienceSource.clip = ambienceLoop;
        ambienceSource.loop = true;
        ambienceSource.Play();
    }

    void OnDisable()
    {
        if (progressBoard == null) return;
        progressBoard.onProgressChanged.RemoveListener(OnProgressChanged);
        progressBoard.onVillageComplete.RemoveListener(OnVillageComplete);
    }

    void OnProgressChanged()
    {
        if (cueSource == null || progressBoard == null) return;
        var steps = progressBoard.CompletedStepCount;
        if (steps <= lastCompletedSteps)
        {
            lastCompletedSteps = steps;
            lastCompletedPlaces = progressBoard.CompletedObjectiveCount;
            return;
        }
        lastCompletedSteps = steps;
        if (progressBoard.IsVillageComplete) return;
        var completed = progressBoard.CompletedObjectiveCount;
        cueSource.PlayOneShot(completed > lastCompletedPlaces ? venueCompleteCue : actionCue);
        lastCompletedPlaces = completed;
    }

    void OnVillageComplete()
    {
        if (cueSource != null && villageCompleteCue != null)
            cueSource.PlayOneShot(villageCompleteCue);
        lastCompletedPlaces = progressBoard.CompletedObjectiveCount;
    }
}
