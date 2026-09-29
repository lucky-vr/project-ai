using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

/// <summary>
/// Keeps the controller menu at the wrist in VR. The Editor simulator sometimes
/// parks its idle left controller at the HMD, so that case gets a readable pose
/// to the left of the view while remaining a child of the controller.
/// </summary>
[DefaultExecutionOrder(100)]
public sealed class VillageControllerMenuPose : MonoBehaviour
{
    public Transform head;
    public XRInteractionSimulator simulator;
    public GameObject frame;
    public GameObject canvasRoot;
    public Vector3 wristLocalPosition = new Vector3(-.03f, .05f, .14f);
    public Vector3 wristLocalEuler = new Vector3(35f, -15f, 0);
    public Vector3 simulatorHeadOffset = new Vector3(-.20f, .08f, .62f);
    public Vector3 simulatorHeadEuler = new Vector3(3f, -16f, 0);

    void Start() => UpdatePose();

    void LateUpdate() => UpdatePose();

    void UpdatePose()
    {
#if UNITY_EDITOR
        // Mouse-look mode is for exploring the village. Reveal the wrist menu
        // when a simulated controller is selected for pointing at its buttons.
        bool show = simulator == null || simulator.targetedDeviceInput != TargetedDevices.HMD;
        if (frame != null && frame.activeSelf != show) frame.SetActive(show);
        if (canvasRoot != null && canvasRoot.activeSelf != show) canvasRoot.SetActive(show);
        if (!show) return;
        if (simulator != null && head != null && transform.parent != null &&
            Vector3.Distance(transform.parent.position, head.position) < .50f)
        {
            transform.SetPositionAndRotation(
                head.TransformPoint(simulatorHeadOffset),
                head.rotation * Quaternion.Euler(simulatorHeadEuler));
            return;
        }
#else
        if (frame != null && !frame.activeSelf) frame.SetActive(true);
        if (canvasRoot != null && !canvasRoot.activeSelf) canvasRoot.SetActive(true);
#endif
        transform.localPosition = wristLocalPosition;
        transform.localRotation = Quaternion.Euler(wristLocalEuler);
    }
}
