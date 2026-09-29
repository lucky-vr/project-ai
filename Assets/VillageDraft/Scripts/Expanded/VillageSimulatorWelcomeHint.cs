using UnityEngine;

/// <summary>Shows simulator keys on the arrival board only in Editor Play Mode.</summary>
public sealed class VillageSimulatorWelcomeHint : MonoBehaviour
{
    public TextMesh label;

    void Start()
    {
#if UNITY_EDITOR
        if (label != null)
            label.text = "SIM: H view  •  ] right hand  •  Click select\n" +
                         "VR: grip grab  •  trigger press  •  wrist cues items";
#endif
    }
}
