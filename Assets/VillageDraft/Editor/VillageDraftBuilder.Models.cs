using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VillageDraft.Editor
{
    public static partial class VillageDraftBuilder
    {
        private static readonly string[] ModelVenuePrefixes =
        {
            "01 MARKET", "02 CAFE", "03 WORKSHOP", "04 GREENHOUSE", "05 POOL",
            "06 BAKERY", "07 POST OFFICE", "08 CLINIC", "09 LIBRARY", "10 MUSIC HALL",
            "11 GARAGE", "12 OBSERVATORY", "13 HARBOR", "14 ART STUDIO", "15 RECYCLING"
        };

        private static readonly string[] ModelVenueIds =
        {
            "market", "cafe", "workshop", "greenhouse", "pool", "bakery", "post", "clinic",
            "library", "music", "garage", "observatory", "harbor", "art", "recycling"
        };

        private static void BuildModels()
        {
            if (ModelVenueIds.Length != ModelVenuePrefixes.Length)
                throw new InvalidOperationException("Venue/model mapping is incomplete.");
            for (var i = 0; i < ModelVenueIds.Length; i++)
            {
                var entry = VillageModelCatalog.Get(ModelVenueIds[i]);
                var venue = root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name.StartsWith(ModelVenuePrefixes[i], StringComparison.Ordinal) &&
                                         t.Find("Sign backing") != null);
                if (venue == null) throw new InvalidOperationException("Model venue missing: " + entry.Id);
                if (entry.PropPaths.Length != 7)
                    throw new InvalidOperationException("Expected seven Blender props for " + entry.Id);
                var originalTransforms = venue.GetComponentsInChildren<Transform>(true);

                var models = Group("Blender reference models (AI assisted)", venue);
                var exterior = PlaceModel(entry.ExteriorPath, "Blender exterior", models, Vector3.zero, 1f);
                AssertExteriorBounds(exterior, entry.Id);
                HidePrimitiveShell(venue);
                var freeDisplays = new System.Collections.Generic.Dictionary<string, GameObject>();
                for (var p = 0; p < entry.PropPaths.Length; p++)
                {
                    var actionName = entry.PreferredActionObjects[p];
                    var action = string.IsNullOrEmpty(actionName) ? null :
                        originalTransforms.FirstOrDefault(t => t.name == actionName);
                    if (action != null)
                    {
                        AttachModelToAction(entry.PropPaths[p], action, entry.ActionVisualScales[p]);
                        continue;
                    }
                    var displayPosition = DisplayPosition(entry.Id, entry.PropRoles[p],
                        entry.SuggestedLocalPositions[p]);
                    var instance = PlaceModel(entry.PropPaths[p], "Blender prop " + (p + 1) + " - " +
                        entry.PropRoles[p], models, displayPosition,
                        entry.SuggestedScales[p] * DisplayScale(entry.Id, entry.PropRoles[p]));
                    instance.transform.localRotation = Quaternion.Euler(0,
                                                       DisplayYaw(entry.Id, entry.PropRoles[p], entry.SuggestedYaws[p]), 0) *
                                                       Quaternion.Euler(-90, 180, 0);
                    FitDisplayFootprint(instance, DisplayFootprint(entry.Id, entry.PropRoles[p]));
                    freeDisplays[entry.PropRoles[p]] = instance;
                }
                SeatSmallDisplays(entry.Id, freeDisplays);
            }
            Debug.Log("[VillageDraft] Placed 15 Blender exteriors and 105 distinct Blender props.");
        }

        private static void AssertExteriorBounds(GameObject exterior, string venueId)
        {
            var renderers = exterior.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Exterior has no renderer: " + venueId);
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            if (bounds.size.x < 8f || bounds.size.z < 8f || bounds.size.y < 3.5f ||
                bounds.min.y < -1f || bounds.max.y > 20f)
                throw new InvalidOperationException($"Exterior scale/axis mismatch for {venueId}: {bounds}");
        }

        private static void AttachModelToAction(string path, Transform action, float visualScale)
        {
            var collider = action.GetComponent<Collider>();
            var desiredCenter = collider != null ? collider.bounds.center : action.position;
            foreach (var renderer in action.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;
            var instance = PlaceModel(path, "Blender action visual", action, Vector3.zero, 1f);
            var inheritedScale = action.lossyScale;
            instance.transform.localScale = new Vector3(
                visualScale / Mathf.Max(.001f, inheritedScale.x),
                visualScale / Mathf.Max(.001f, inheritedScale.y),
                visualScale / Mathf.Max(.001f, inheritedScale.z));
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Action model has no renderer: " + path);
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            instance.transform.position += desiredCenter - bounds.center;
        }

        private static GameObject PlaceModel(string path, string name, Transform parent, Vector3 localPosition,
            float scale)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source == null) throw new InvalidOperationException("Missing imported Blender FBX: " + path);
            var instance = PrefabUtility.InstantiatePrefab(source, parent) as GameObject;
            if (instance == null) throw new InvalidOperationException("Could not instantiate Blender FBX: " + path);
            instance.name = name + " - " + source.name;
            instance.transform.localPosition = localPosition;
            // Blender's FBX mesh is Z-up. Convert its basis and face the entrance toward local -Z.
            instance.transform.localRotation = Quaternion.Euler(-90, 180, 0);
            instance.transform.localScale = Vector3.one * scale;
            return instance;
        }

        private static void FitDisplayFootprint(GameObject instance, float maxWidth)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            var footprint = Mathf.Max(bounds.size.x, bounds.size.z);
            if (footprint > maxWidth)
                instance.transform.localScale *= Mathf.Max(.35f, maxWidth / footprint);
        }

        private static float DisplayFootprint(string venueId, string role)
        {
            // Preserve the intended scale of visual landmarks. The previous 2 m blanket cap
            // shrank a boat, crane and windmill into counter-sized props.
            if (role == "windmill") return 4.2f;
            if (role == "boat") return 5.2f;
            if (role == "crane") return 4.2f;
            if (role == "lift") return 3.5f;
            if (role == "piano") return 3.3f;
            if (role == "telescope") return 3.1f;
            if (role == "speaker") return 2.7f;
            if (role == "counter" || role == "cart" || role == "table" ||
                role == "display" || role == "bed") return 3.2f;
            return 2.3f;
        }

        private static float DisplayScale(string venueId, string role)
        {
            // Blender source dimensions were measured before choosing these scene scales.
            if (role == "windmill") return 2.1f;
            if (role == "boat") return 1.4f;
            if (role == "crane") return 1.5f;
            if (role == "lift") return 1.2f;
            if (role == "speaker") return 1.5f;
            if (role == "telescope") return 1.2f;
            return 1f;
        }

        private static Vector3 DisplayPosition(string venueId, string role, Vector3 fallback)
        {
            // Give oversized landmarks their own space instead of forcing them into the
            // same seven indoor display slots used by small furniture and hand props.
            if (venueId == "workshop" && role == "windmill")
                return new Vector3(8.2f, 0.1f, 1.0f);
            if (venueId == "harbor" && role == "crane")
                return new Vector3(3.5f, 0.1f, 4.0f);
            // Small items need a surface and a deliberate partner, rather than a
            // position on the floor copied from every other venue.
            if (venueId == "cafe" && role == "grinder") return new Vector3(-3.85f, 1.16f, .18f);
            if (venueId == "cafe" && role == "tray") return new Vector3(-4.83f, 1.16f, -.12f);
            if (venueId == "workshop" && role == "gear") return new Vector3(-4.78f, 1.18f, .08f);
            if (venueId == "workshop" && role == "vise") return new Vector3(-3.85f, 1.18f, .08f);
            if (venueId == "greenhouse" && role == "can") return new Vector3(-4.30f, 1.12f, 2.75f);
            if (venueId == "bakery" && role == "mixer") return new Vector3(-4.35f, 1.18f, .10f);
            if (venueId == "post" && role == "press") return new Vector3(-4.78f, 1.16f, 2.75f);
            if (venueId == "post" && role == "scale") return new Vector3(-3.83f, 1.16f, 2.75f);
            if (venueId == "clinic" && role == "monitor") return new Vector3(-4.35f, 1.30f, .10f);
            if (venueId == "clinic" && role == "tray") return new Vector3(4.35f, 1.08f, 2.75f);
            if (venueId == "library" && role == "lamp") return new Vector3(-4.80f, .95f, .10f);
            if (venueId == "library" && role == "book") return new Vector3(-3.86f, .95f, .10f);
            if (venueId == "observatory" && role == "globe") return new Vector3(-3.86f, 1.02f, .10f);
            if (venueId == "observatory" && role == "lamp") return new Vector3(-4.80f, 1.02f, .10f);
            if (venueId == "harbor" && role == "ring") return new Vector3(-4.35f, .94f, .10f);
            if (venueId == "art" && role == "canvas") return new Vector3(-4.35f, 1.30f, 2.52f);
            if (venueId == "art" && role == "rack") return new Vector3(0, .92f, 3.45f);
            if (venueId == "recycling" && role == "scale") return new Vector3(-4.35f, 1.10f, -2.55f);
            return fallback;
        }

        private static float DisplayYaw(string venueId, string role, float fallback)
        {
            if (venueId == "art" && role == "canvas") return -90f; // Parallel with its easel.
            if (venueId == "art" && role == "rack") return 0f;
            return fallback;
        }

        private static void SeatSmallDisplays(string venueId,
            System.Collections.Generic.Dictionary<string, GameObject> displays)
        {
            void Seat(string itemRole, string supportRole)
            {
                if (!displays.TryGetValue(itemRole, out var item) ||
                    !displays.TryGetValue(supportRole, out var support)) return;
                var itemBounds = RenderBounds(item);
                var supportBounds = RenderBounds(support);
                item.transform.position += Vector3.up * (supportBounds.max.y + .012f - itemBounds.min.y);
            }

            switch (venueId)
            {
                case "cafe": Seat("grinder", "counter"); Seat("tray", "counter"); break;
                case "workshop": Seat("gear", "counter"); Seat("vise", "counter"); break;
                case "greenhouse": Seat("can", "counter"); break;
                case "bakery": Seat("mixer", "counter"); break;
                case "post": Seat("press", "counter"); Seat("scale", "counter"); break;
                case "clinic": Seat("monitor", "cabinet"); Seat("tray", "cart"); break;
                case "library": Seat("lamp", "table"); Seat("book", "table"); break;
                case "observatory": Seat("globe", "table"); Seat("lamp", "table"); break;
                case "art": Seat("rack", "table"); break;
                case "recycling": Seat("scale", "conveyor"); break;
            }
        }

        private static Bounds RenderBounds(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException("Blender display has no renderer: " + instance.name);
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void HidePrimitiveShell(Transform venue)
        {
            foreach (var childName in new[]
            {
                "Floor", "Back wall", "Left wall", "Right wall", "Roof", "Front left post",
                "Front right post", "Front fascia", "Sign backing", "Rear left pergola post",
                "Rear right pergola post"
            })
            {
                var child = venue.Find(childName);
                if (child != null && child.TryGetComponent<Renderer>(out var renderer))
                    renderer.enabled = false;
            }
            foreach (Transform child in venue)
            {
                if (child.name.StartsWith("Text - ", StringComparison.Ordinal) &&
                    child.TryGetComponent<Renderer>(out var textRenderer))
                    textRenderer.enabled = false;
                if (child.name == "Open pergola beam" && child.TryGetComponent<Renderer>(out var renderer))
                    renderer.enabled = false;
            }
        }
    }
}
