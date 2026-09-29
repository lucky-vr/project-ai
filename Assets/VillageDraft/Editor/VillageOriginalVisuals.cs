using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VillageDraft.Editor
{
    public static partial class VillageDraftBuilder
    {
        private const string GameplayFbx = "Assets/VillageDraft/Models/GameplayFestival/FBX/";

        /// <summary>Keep the original five XR actions while replacing their visible blockout geometry.</summary>
        private static void ReplaceOriginalVenueVisuals()
        {
            var venues = root.Find("04 Five activity places");
            if (venues == null) throw new InvalidOperationException("Original village venues are missing.");

            foreach (var button in venues.GetComponentsInChildren<VillagePressButton>(true))
                button.buttonVisual = SkinOriginalAction(button.gameObject, "press_button.fbx", .55f);

            foreach (var socket in venues.GetComponentsInChildren<VillagePlaceTarget>(true))
            {
                var marker = socket.transform.Find("Placement marker");
                if (marker == null) throw new InvalidOperationException("Original socket marker missing: " + socket.name);
                socket.targetVisual = SkinOriginalAction(marker.gameObject, "placement_socket.fbx", 1f);
            }

            var greenhouse = VenueByPrefix(venues, "04 GREENHOUSE");
            SkinOriginalAction(RequireChild(greenhouse, "Seed tray").gameObject, "grab_pedestal.fbx", 1f);
            SkinOriginalAction(RequireChild(greenhouse, "Grab seed packet").gameObject,
                "Assets/VillageDraft/Models/FBX/greenhouse/greenhouse_seed_packet.fbx", .85f);

            var workshop = VenueByPrefix(venues, "03 WORKSHOP");
            foreach (var indicator in workshop.Cast<Transform>().Where(t => t.name == "Switch indicator"))
                SkinOriginalAction(indicator.gameObject, "status_lamp.fbx", 1f);

            var pool = VenueByPrefix(venues, "05 POOL");
            SkinOriginalAction(RequireChild(pool, "Ring stand").gameObject, "grab_pedestal.fbx", 1f);

            foreach (var objective in board.objectives.Take(5))
            {
                var lamp = root.Find("03 Welcome plaza/Task lamp - " + objective.id);
                if (lamp == null) throw new InvalidOperationException("Progress lamp missing: " + objective.id);
                objective.indicator = SkinOriginalAction(lamp.gameObject, "status_lamp.fbx", 1f);
            }

            // These are remnants of the first blockout. The venues, their action colliders,
            // and their scripts remain active; only their old built-in mesh art is hidden.
            var hidden = 0;
            foreach (Transform venue in venues)
                foreach (var renderer in venue.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!renderer.enabled || renderer.GetComponent<TextMesh>() != null) continue;
                    var filter = renderer.GetComponent<MeshFilter>();
                    var assetPath = filter == null ? string.Empty : AssetDatabase.GetAssetPath(filter.sharedMesh);
                    if (assetPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) ||
                        assetPath.EndsWith(".blend", StringComparison.OrdinalIgnoreCase)) continue;
                    renderer.enabled = false;
                    hidden++;
                }
            Debug.Log("[VillageDraft] Replaced original venue control art with Blender FBX; hidden " +
                      hidden + " remaining original blockout mesh renderers.");
        }

        private static Transform RequireChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child == null) throw new InvalidOperationException("Original venue object missing: " + name);
            return child;
        }

        private static Renderer SkinOriginalAction(GameObject action, string fileName, float footprintScale)
        {
            var oldRenderer = action.GetComponent<Renderer>();
            if (oldRenderer == null) throw new InvalidOperationException("Original visual missing: " + action.name);
            var oldBounds = oldRenderer.bounds;
            oldRenderer.enabled = false;

            var path = fileName.StartsWith("Assets/", StringComparison.Ordinal) ? fileName : GameplayFbx + fileName;
            var instance = PlaceModel(path, "Blender control visual", action.transform,
                                      Vector3.zero, 1f);
            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collider);
            var lossy = action.transform.lossyScale;
            instance.transform.localScale = new Vector3(
                1f / Mathf.Max(.001f, Mathf.Abs(lossy.x)),
                1f / Mathf.Max(.001f, Mathf.Abs(lossy.y)),
                1f / Mathf.Max(.001f, Mathf.Abs(lossy.z)));
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Blender control has no renderer: " + fileName);
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            var currentFootprint = Mathf.Max(bounds.size.x, bounds.size.z);
            var targetFootprint = Mathf.Max(.12f, Mathf.Max(oldBounds.size.x, oldBounds.size.z) * footprintScale);
            instance.transform.localScale *= targetFootprint / Mathf.Max(.001f, currentFootprint);
            bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            instance.transform.position += oldBounds.center - bounds.center;
            return renderers[0];
        }
    }
}
