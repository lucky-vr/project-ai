using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

/// <summary>Starts the Editor's XR simulator with mouse control of the player's view.</summary>
public sealed class VillageSimulatorDefaults : MonoBehaviour
{
    public XRInteractionSimulator simulator;

    void Start()
    {
#if UNITY_EDITOR
        if (simulator == null)
            simulator = GetComponent<XRInteractionSimulator>();
        if (simulator != null)
            simulator.targetedDeviceInput = TargetedDevices.HMD;
#endif
    }
}
