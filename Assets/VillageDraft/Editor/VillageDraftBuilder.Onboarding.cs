using System;
using System.Linq;
using UnityEngine;

namespace VillageDraft.Editor
{
    public static partial class VillageDraftBuilder
    {
        private static void WireVenueTaskGuides()
        {
            var guides = root.GetComponentsInChildren<VillageVenueTaskGuide>(true)
                .ToDictionary(x => x.objectiveId, StringComparer.Ordinal);
            if (guides.Count != 15)
                throw new InvalidOperationException("First-run guidance needs one task kiosk at each venue.");
            foreach (var button in root.GetComponentsInChildren<VillagePressButton>(true))
                button.taskGuide = guides[button.objectiveId];
            foreach (var target in root.GetComponentsInChildren<VillagePlaceTarget>(true))
                target.taskGuide = guides[target.objectiveId];
        }

        private static void BuildFirstMarketCue(Transform market)
        {
            var bread = market.Find("Grab bread");
            var basket = market.Find("Bread basket socket");
            if (bread == null || basket == null)
                throw new InvalidOperationException("Market bread or first basket target is missing.");
            var cues = Group("First task - bread to basket guidance", market);
            var sourceLight = TutorialSpot("Warm light on bread", cues,
                bread.localPosition + new Vector3(0, .80f, 0), new Color(1f, .67f, .24f));
            var targetLight = TutorialSpot("Cool light on basket", cues,
                basket.localPosition + new Vector3(0, .80f, 0), new Color(.18f, .80f, 1f));
            // Mount small plaques on the counter face, directly below the two logical targets.
            // The former full-size signs floated above the goods and their captions overlapped.
            var sourceMarker = FirstTaskMarker("GRAB BREAD", cues,
                new Vector3(bread.localPosition.x, 1.24f, .76f));
            var targetMarker = FirstTaskMarker("BASKET", cues,
                new Vector3(basket.localPosition.x, 1.24f, .76f));
            var controller = cues.gameObject.AddComponent<VillageFirstMarketCue>();
            controller.progressBoard = board;
            controller.sourceLight = sourceLight;
            controller.targetLight = targetLight;
            controller.sourceMarker = sourceMarker.gameObject;
            controller.targetMarker = targetMarker.gameObject;
        }

        private static Transform FirstTaskMarker(string caption, Transform parent, Vector3 localPosition)
        {
            var marker = Group("First task marker - " + caption, parent);
            marker.localPosition = localPosition;
            Kit("tutorial_marker", marker, Vector3.zero, Vector3.one * .55f);
            LocalLabel(caption, marker, new Vector3(0, .40f, -.085f), 0, .035f, Color.white);
            return marker;
        }

        private static Light TutorialSpot(string name, Transform parent, Vector3 localPosition, Color color)
        {
            var point = Group(name, parent);
            point.localPosition = localPosition;
            point.localRotation = Quaternion.Euler(90, 0, 0);
            var light = point.gameObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = color;
            light.spotAngle = 58f;
            light.range = 1.8f;
            light.intensity = 3.3f;
            light.shadows = LightShadows.None;
            return light;
        }
    }
}
