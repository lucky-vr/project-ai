using System;
using System.Linq;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Management;
using Unity.XR.CoreUtils;

namespace VillageDraft.Editor
{
    public static class VillageDraftValidator
    {
        [MenuItem("Tools/VR Village/Use Desktop Preview Without OpenXR")]
        public static void UseDesktopPreviewWithoutOpenXR()
        {
            var desktop = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            var android = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            Require(desktop != null && android != null, "XR settings for desktop and Android are required.");
            desktop.InitManagerOnStart = false;
            EditorUtility.SetDirty(desktop);
            AssetDatabase.SaveAssets();
            Require(android.InitManagerOnStart, "Quest Android XR startup must remain enabled.");
            Debug.Log("[VillageDraft] Desktop Editor Play uses a non-XR preview; Android XR startup remains enabled.");
        }

        [MenuItem("Tools/VR Village/Restore Desktop OpenXR")]
        public static void RestoreDesktopOpenXR()
        {
            var desktop = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            Require(desktop != null, "Desktop XR settings are missing.");
            desktop.InitManagerOnStart = true;
            EditorUtility.SetDirty(desktop);
            AssetDatabase.SaveAssets();
            Debug.Log("[VillageDraft] Desktop OpenXR startup restored.");
        }

        [MenuItem("Tools/VR Village/Validate Draft Wiring")]
        public static void Validate()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            Require(scene.path == VillageDraftBuilder.ScenePath, "Open the VillageDraft scene first.");
            var rigs = UnityEngine.Object.FindObjectsByType<XROrigin>(FindObjectsSortMode.None);
            Require(rigs.Length == 1, "Expected one XR Origin.");
            Require(UnityEngine.Object.FindObjectsByType<TeleportationArea>(FindObjectsSortMode.None).Length >= 1,
                "A teleportable walkable plane is required.");

            var board = UnityEngine.Object.FindFirstObjectByType<VillageProgressBoard>();
            Require(board != null && board.objectives.Length == 15, "Fifteen configured objectives are required.");
            Require(board.boardText != null, "Progress board text is not linked.");
            Require(board.objectives.All(o => o != null && o.stepsRequired == 10),
                "Every place needs ten configured action steps.");
            Require(board.objectives.Select(o => o.id).Distinct().Count() == 15,
                "Every place needs a unique objective ID.");

            var buttons = UnityEngine.Object.FindObjectsByType<VillagePressButton>(FindObjectsSortMode.None);
            var targets = UnityEngine.Object.FindObjectsByType<VillagePlaceTarget>(FindObjectsSortMode.None);
            var grabs = UnityEngine.Object.FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None);
            Require(buttons.Length + targets.Length == 150,
                "Expected 150 XRI action controls, found " + (buttons.Length + targets.Length));
            Require(grabs.Length >= 15, "Expected at least 15 grabbable items, found " + grabs.Length);
            var guides = UnityEngine.Object.FindObjectsByType<VillageVenueTaskGuide>(FindObjectsSortMode.None);
            Require(guides.Length == 15, "Expected one task UI for each place, found " + guides.Length);
            Require(UnityEngine.Object.FindObjectsByType<VillageMovingProp>(FindObjectsSortMode.None).Length >= 1,
                "Workshop windmill needs scripted motion.");
            foreach (var button in buttons)
            {
                Require(button.progressBoard == board, "Unlinked button: " + button.name);
                Require(button.GetComponent<XRSimpleInteractable>() != null, "Missing XRI on " + button.name);
                Require(board.objectives.Any(o => o.id == button.objectiveId), "Unknown button objective " + button.name);
            }
            foreach (var target in targets)
            {
                Require(target.progressBoard == board, "Unlinked socket: " + target.name);
                Require(target.requiredObject != null, "Missing required item on " + target.name);
                Require(target.GetComponent<XRSocketInteractor>() != null, "Missing XRI socket on " + target.name);
                Require(board.objectives.Any(o => o.id == target.objectiveId), "Unknown target objective " + target.name);
            }
            foreach (var objective in board.objectives)
            {
                var id = objective.id;
                Require(buttons.Count(x => x.objectiveId == id) + targets.Count(x => x.objectiveId == id) == 10,
                    "Expected ten XRI controls for " + id);
                Require(targets.Any(x => x.objectiveId == id), "Missing grab-and-place interaction for " + id);
                Require(guides.Count(x => x.objectiveId == id) == 1, "Missing task UI for " + id);
            }
            Require(EditorBuildSettings.scenes.Length > 0 &&
                    EditorBuildSettings.scenes[0].path == VillageDraftBuilder.ScenePath &&
                    EditorBuildSettings.scenes[0].enabled,
                "VillageDraft is not the first enabled build scene.");
            Debug.Log($"[VillageDraft] Wiring valid: 1 XR rig, 15 places, {buttons.Length} buttons, " +
                      $"{targets.Length} sockets, {grabs.Length} grabbable items, 150 action steps.");
        }

        [MenuItem("Tools/VR Village/Exercise Objectives (Play Mode)")]
        public static void ExerciseObjectives()
        {
            Require(EditorApplication.isPlaying, "Enter Play Mode to exercise objectives.");
            var board = UnityEngine.Object.FindFirstObjectByType<VillageProgressBoard>();
            Require(board != null, "Progress board missing.");
            board.ResetProgress();

            var buttons = UnityEngine.Object.FindObjectsByType<VillagePressButton>(FindObjectsSortMode.None);
            var targets = UnityEngine.Object.FindObjectsByType<VillagePlaceTarget>(FindObjectsSortMode.None);
            foreach (var objective in board.objectives)
            {
                for (int step = 0; step < objective.stepsRequired; step++)
                {
                    var completed = false;
                    foreach (var button in buttons.Where(x => x.objectiveId == objective.id && x.requiredCurrentStep == step))
                        if (button.Press()) { completed = true; break; }
                    if (!completed)
                        foreach (var target in targets.Where(x => x.objectiveId == objective.id && x.requiredCurrentStep == step))
                            if (target.TryPlace(target.requiredObject)) { completed = true; break; }
                    if (!completed)
                        foreach (var button in buttons.Where(x => x.objectiveId == objective.id && x.requiredCurrentStep == -1))
                            if (button.Press()) { completed = true; break; }
                    if (!completed)
                        foreach (var target in targets.Where(x => x.objectiveId == objective.id && x.requiredCurrentStep == -1))
                            if (target.TryPlace(target.requiredObject)) { completed = true; break; }
                    Require(completed, $"No usable action for {objective.id} step {step + 1}.");
                }
            }

            Require(board.IsVillageComplete, "Objectives did not all complete.");
            Require(board.CompletedStepCount == 150, "All 150 steps did not complete.");
            Debug.Log("[VillageDraft] Play check passed: all 150 steps completed across 15 places.");
        }

        private static void Press(string name)
        {
            var button = UnityEngine.Object.FindObjectsByType<VillagePressButton>(FindObjectsSortMode.None)
                .FirstOrDefault(x => x.name == name);
            Require(button != null && button.Press(), "Button did not advance: " + name);
        }

        private static void Place(string name)
        {
            var target = UnityEngine.Object.FindObjectsByType<VillagePlaceTarget>(FindObjectsSortMode.None)
                .FirstOrDefault(x => x.name == name);
            Require(target != null && target.TryPlace(target.requiredObject), "Socket did not advance: " + name);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("[VillageDraft] " + message);
        }
    }
}
