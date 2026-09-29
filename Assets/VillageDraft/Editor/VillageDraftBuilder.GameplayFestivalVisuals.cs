using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VillageDraft.Editor
{
    public static partial class VillageDraftBuilder
    {
        private const string GameplayKitFolder = "Assets/VillageDraft/Models/GameplayFestival/FBX/";

        /// <summary>
        /// Keep Unity primitive geometry only as invisible interaction/physics shapes. Every
        /// visible expanded gameplay mesh is instantiated from the editable Blender kit.
        /// </summary>
        private static void SkinExpandedGameplay()
        {
            if (expandedRoot == null) throw new InvalidOperationException("Expanded village is missing.");
            SkinActionYards();
            SkinVenueMachines();
            SkinTaskKiosks();
            SkinFestivalSquare();
            SkinReplayConsole();
            SkinVenueLampsAndSigns();
            board.RefreshBoard();
        }

        private static GameObject Kit(string assetName, Transform parent, Vector3 position, Vector3 scale)
        {
            var path = GameplayKitFolder + assetName + ".fbx";
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source == null) throw new InvalidOperationException("Missing Blender gameplay asset: " + path);
            var instance = PrefabUtility.InstantiatePrefab(source, parent) as GameObject;
            if (instance == null) throw new InvalidOperationException("Could not instantiate " + path);
            instance.name = "Blender gameplay - " + assetName;
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.Euler(-90, 180, 0);
            instance.transform.localScale = scale;
            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collider);
            return instance;
        }

        private static Renderer SkinPrimitive(Transform logical, string assetName)
        {
            if (logical == null) throw new InvalidOperationException("Gameplay visual target is missing: " + assetName);
            var original = logical.GetComponent<Renderer>();
            if (original != null) original.enabled = false;
            var filter = logical.GetComponent<MeshFilter>();
            var isCylinder = filter != null && filter.sharedMesh != null &&
                             filter.sharedMesh.name.IndexOf("Cylinder", StringComparison.OrdinalIgnoreCase) >= 0;
            var skin = Kit(assetName, logical, Vector3.zero,
                isCylinder ? new Vector3(1, 2, 1) : Vector3.one);
            var renderer = skin.GetComponentInChildren<Renderer>(true);
            if (renderer == null) throw new InvalidOperationException(assetName + " has no renderer.");
            return renderer;
        }

        private static void SkinActionYards()
        {
            var stations = expandedRoot.GetComponentsInChildren<VillageStepBeacon>(true);
            foreach (var step in stations)
            {
                var station = step.transform;
                int layout = step.stepIndex % 5;
                string id = step.objectiveId;
                // The five station shapes use distinct construction and a trade-specific palette.
                foreach (Transform child in station)
                {
                    var renderer = child.GetComponent<MeshRenderer>();
                    if (renderer == null) continue;
                    if (child.GetComponent<VillagePressButton>() != null ||
                        child.GetComponent<XRGrabInteractable>() != null ||
                        child == step.band.transform || child == step.beacon.transform ||
                        child.name == "Step number plate" || child.name == "Scripted action result")
                        continue;
                    renderer.enabled = false; // worktop collider remains in place
                }
                Kit("station_" + id + "_" + layout, station,
                    new Vector3(0, .53f, 0), new Vector3(1.55f, 1.05f, 1.30f));
                foreach (Transform child in station)
                {
                    if (child.name == "Elevated shelf" || child.name == "Wall rack")
                        SkinPrimitive(child, "shelf_board");
                    else if (child.name == "Inspection riser")
                        SkinPrimitive(child, "grab_pedestal");
                }
                var plate = station.Find("Step number plate");
                SkinPrimitive(plate, "number_plate");
                step.band = SkinPrimitive(step.band.transform, "status_strip");
                step.beacon = SkinPrimitive(step.beacon.transform, "current_light");
                step.beacon.enabled = step.stepIndex == 0;

                var press = station.GetComponentInChildren<VillagePressButton>(true);
                if (press != null)
                    press.buttonVisual = SkinPrimitive(press.transform, "press_button");
                var grab = station.GetComponentInChildren<XRGrabInteractable>(true);
                if (grab != null)
                {
                    var key = step.stepIndex % 3 == 0 ? "grab_token" :
                        step.stepIndex % 3 == 1 ? "machine_focus_" + id : "artifact_" + id;
                    SkinPrimitive(grab.transform, key);
                }
                var place = station.GetComponentInChildren<VillagePlaceTarget>(true);
                if (place != null && place.targetVisual != null)
                    place.targetVisual = SkinPrimitive(place.targetVisual.transform, "placement_socket");
                var result = station.Find("Scripted action result");
                if (result != null) SkinPrimitive(result, "scripted_result");
            }

            foreach (var group in expandedRoot.GetComponentsInChildren<Transform>(true)
                         .Where(x => x.name == "Ten-step interaction stations").ToArray())
            {
                var floor = group.Cast<Transform>().FirstOrDefault(x => x.name.EndsWith(" activity floor"));
                if (floor != null)
                {
                    floor.GetComponent<Renderer>().enabled = false;
                    Kit("yard_floor", group, floor.localPosition, Vector3.one);
                }
                foreach (Transform line in group)
                    if (line.name.StartsWith("Process lane", StringComparison.Ordinal))
                        line.GetComponent<Renderer>().enabled = false;
            }
        }

        private static void SkinVenueMachines()
        {
            foreach (var venue in expandedRoot.GetComponentsInChildren<VillageVenueExperience>(true))
            {
                var machine = venue.transform;
                foreach (var renderer in machine.GetComponentsInChildren<MeshRenderer>(true))
                    if (renderer.GetComponent<TextMesh>() == null) renderer.enabled = false;
                Kit("machine_core_" + venue.objectiveId, machine, Vector3.zero, Vector3.one);
                Kit("status_strip", machine, new Vector3(0, 2.79f, -.49f),
                    new Vector3(1.55f, .055f, .66f));
                Kit("number_plate", machine, new Vector3(0, 3.30f, -.49f),
                    new Vector3(2.10f, .055f, .65f));
                if (venue.outcomeText != null)
                    venue.outcomeText.transform.localPosition = new Vector3(0, 3.30f, -.57f);
                SkinPrimitive(venue.focus, "machine_focus_" + venue.objectiveId);
                SkinPrimitive(venue.secondary, "machine_secondary_" + venue.objectiveId);
                for (int i = 0; i < venue.milestones.Length; i++)
                    if (venue.milestones[i] != null)
                        venue.milestones[i] = SkinPrimitive(venue.milestones[i].transform, "status_lamp");
                if (venue.revealPieces != null)
                    foreach (var piece in venue.revealPieces)
                        if (piece != null) SkinPrimitive(piece.transform, "art_stroke");
                foreach (Transform child in machine)
                {
                    if (child.name.EndsWith(" mounted drive wheel", StringComparison.Ordinal))
                        SkinPrimitive(child, "machine_wheel");
                    else if (child.name.EndsWith(" drive axle", StringComparison.Ordinal))
                        SkinPrimitive(child, "machine_axle");
                }
            }
        }

        private static void SkinTaskKiosks()
        {
            foreach (var guide in expandedRoot.GetComponentsInChildren<VillageVenueTaskGuide>(true))
            {
                var kiosk = guide.transform.parent;
                foreach (var renderer in kiosk.GetComponentsInChildren<MeshRenderer>(true))
                    if (renderer.GetComponent<TextMesh>() == null) renderer.enabled = false;
                Kit("task_kiosk", kiosk, Vector3.zero, Vector3.one);
                if (guide.promptText != null)
                    guide.promptText.transform.localPosition = new Vector3(0, 1.80f, -.15f);
            }
        }

        private static void SkinFestivalSquare()
        {
            var festival = expandedRoot.GetComponentInChildren<VillageFestivalSquare>(true);
            if (festival == null) throw new InvalidOperationException("Festival square is missing.");
            for (int i = 0; i < festival.contributions.Length; i++)
            {
                var item = festival.contributions[i];
                if (item == null) continue;
                var plinth = item.transform.parent.Find("Contribution plinth");
                SkinPrimitive(plinth, "festival_plinth");
                var artifact = item.GetComponentInChildren<MeshRenderer>(true);
                if (artifact != null)
                    SkinPrimitive(artifact.transform, "artifact_" + festival.objectiveIds[i]);
            }
        }

        private static void SkinReplayConsole()
        {
            var flow = expandedRoot.GetComponentInChildren<VillageGameFlow>(true);
            if (flow == null) throw new InvalidOperationException("Village replay control is missing.");
            SkinPrimitive(flow.transform, "replay_panel");
            SkinPrimitive(flow.replayButton.transform, "replay_button");
        }

        private static void SkinVenueLampsAndSigns()
        {
            foreach (var objective in board.objectives)
            {
                if (objective?.indicator != null && objective.indicator.transform.IsChildOf(expandedRoot))
                    objective.indicator = SkinPrimitive(objective.indicator.transform, "status_lamp");
            }
            foreach (var transform in expandedRoot.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name == "Activity yard direction sign")
                    SkinPrimitive(transform, "direction_sign");
                else if (transform.name.EndsWith(" activity-yard paving spur", StringComparison.Ordinal))
                {
                    var original = transform.GetComponent<Renderer>();
                    if (original != null) original.enabled = false;
                    Kit("yard_spur", transform.parent, transform.localPosition, Vector3.one);
                }
            }
        }
    }
}
