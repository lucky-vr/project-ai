using UnityEngine;

/// <summary>Shows a modest celebration when every venue objective is finished.</summary>
public sealed class VillageCelebration : MonoBehaviour
{
    public VillageProgressBoard progressBoard;
    public ParticleSystem confetti;

    void OnEnable()
    {
        if (progressBoard != null)
            progressBoard.onVillageComplete.AddListener(Celebrate);
    }

    void OnDisable()
    {
        if (progressBoard != null)
            progressBoard.onVillageComplete.RemoveListener(Celebrate);
    }

    public void Celebrate()
    {
        if (confetti == null) return;
        confetti.Clear();
        confetti.Play();
    }
}
