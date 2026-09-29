using System;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace VillageDraft.Editor
{
    public static partial class VillageDraftBuilder
    {
        private const string ControllerFramePath =
            "Assets/VillageDraft/Models/GameplayFestival/FBX/controller_menu_frame.fbx";
        private const string SimulatorPath =
            "Assets/Samples/XR Interaction Toolkit/3.5.1/XR Interaction Simulator/XR Interaction Simulator.prefab";

        private static readonly string[] ControllerVenueIds =
        {
            "market", "cafe", "workshop", "greenhouse", "pool",
            "bakery", "post", "clinic", "library", "music",
            "garage", "observatory", "harbor", "art", "recycling"
        };

        private static readonly string[] ControllerVenueLabels =
        {
            "MARKET", "CAFE", "WORKSHOP", "GREENHOUSE", "POOL",
            "BAKERY", "POST", "CLINIC", "LIBRARY", "MUSIC",
            "GARAGE", "OBSERVATORY", "HARBOR", "ART", "RECYCLING"
        };

        /// <summary>Wrist-mounted XR UI that cues one distinct physical mechanism per venue.</summary>
        private static void BuildControllerFestivalMenu()
        {
            var rig = UnityEngine.Object.FindFirstObjectByType<XROrigin>();
            if (rig == null) throw new InvalidOperationException("XR Origin is missing for the festival menu.");
            var descendants = rig.GetComponentsInChildren<Transform>(true);
            var left = descendants.FirstOrDefault(t => t.name == "Left Controller");
            var right = descendants.FirstOrDefault(t => t.name == "Right Controller");
            if (left == null || right == null)
                throw new InvalidOperationException("The XR Origin needs both controller transforms.");
            // The template stores controller poses at the HMD origin. Give the saved scene
            // a sensible hand pose; tracked devices and the simulator override it in Play.
            if (left.localPosition.sqrMagnitude < .001f)
                left.localPosition = new Vector3(-.45f, -.40f, .45f);
            if (right.localPosition.sqrMagnitude < .001f)
                right.localPosition = new Vector3(.32f, -.32f, .45f);
            var previousMenu = left.Find("Festival controller menu");
            if (previousMenu != null)
                UnityEngine.Object.DestroyImmediate(previousMenu.gameObject);

            var rightRay = right.GetComponentInChildren<NearFarInteractor>(true);
            if (rightRay == null)
                throw new InvalidOperationException("Right Controller needs its Near-Far Interactor for XR UI.");
            rightRay.enableUIInteraction = true;
            var systems = UnityEngine.Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (systems.Length != 1 || systems[0].GetComponent<XRUIInputModule>() == null)
                throw new InvalidOperationException("The scene needs exactly one EventSystem with XRUIInputModule.");

            var simulators = UnityEngine.Object.FindObjectsByType<XRInteractionSimulator>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 1; i < simulators.Length; i++)
                UnityEngine.Object.DestroyImmediate(simulators[i].gameObject);
            XRInteractionSimulator activeSimulator;
            if (simulators.Length == 0)
            {
                var simulatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SimulatorPath);
                if (simulatorPrefab == null) throw new InvalidOperationException("Missing XRI Simulator prefab.");
                var simulator = PrefabUtility.InstantiatePrefab(simulatorPrefab) as GameObject;
                if (simulator == null) throw new InvalidOperationException("Could not instantiate XRI Simulator.");
                simulator.name = "XR Interaction Simulator";
                simulator.tag = "EditorOnly"; // Available in Editor Play Mode; stripped from Quest builds.
                activeSimulator = simulator.GetComponent<XRInteractionSimulator>();
            }
            else
            {
                simulators[0].gameObject.tag = "EditorOnly";
                activeSimulator = simulators[0];
            }
            if (activeSimulator == null)
                throw new InvalidOperationException("XR Interaction Simulator component is missing.");
            var defaults = activeSimulator.GetComponent<VillageSimulatorDefaults>();
            if (defaults == null)
                defaults = activeSimulator.gameObject.AddComponent<VillageSimulatorDefaults>();
            defaults.simulator = activeSimulator;

            var experiences = UnityEngine.Object.FindObjectsByType<VillageVenueExperience>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (experiences.Length != ControllerVenueIds.Length)
                throw new InvalidOperationException("Build all fifteen venue mechanisms before the controller menu.");
            var orderedVenues = ControllerVenueIds.Select(id =>
                experiences.Single(x => x.objectiveId == id)).ToArray();

            var frameAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ControllerFramePath);
            if (frameAsset == null)
                throw new InvalidOperationException("Missing Blender controller menu frame: " + ControllerFramePath);
            var wrist = Group("Festival controller menu", left);
            wrist.localPosition = new Vector3(-.03f, .05f, .14f);
            wrist.localRotation = Quaternion.Euler(35f, -15f, 0);
            wrist.localScale = Vector3.one * .52f;
            var hmdCamera = rig.Camera != null ? rig.Camera : Camera.main;
            if (hmdCamera == null) throw new InvalidOperationException("XR Origin has no HMD camera.");
            var menuPose = wrist.gameObject.AddComponent<VillageControllerMenuPose>();
            menuPose.head = hmdCamera.transform;
            menuPose.simulator = activeSimulator;
            var frame = PrefabUtility.InstantiatePrefab(frameAsset, wrist) as GameObject;
            if (frame == null) throw new InvalidOperationException("Could not instantiate Blender menu frame.");
            frame.name = "Blender festival wrist menu frame";
            frame.transform.localPosition = Vector3.zero;
            frame.transform.localRotation = Quaternion.Euler(-90, 180, 0);
            frame.transform.localScale = Vector3.one;
            menuPose.frame = frame;
            foreach (var collider in frame.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collider); // The UI ray must reach the Canvas.

            var canvasObject = new GameObject("Festival controller world-space Canvas",
                typeof(RectTransform), typeof(Canvas), typeof(TrackedDeviceGraphicRaycaster));
            var rect = canvasObject.GetComponent<RectTransform>();
            rect.SetParent(wrist, false);
            rect.localPosition = new Vector3(0, 0, -.035f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * .001f;
            rect.sizeDelta = new Vector2(500, 610);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.sortingOrder = 30;
            menuPose.canvasRoot = canvasObject;

            if (signFont == null)
                signFont = AssetDatabase.LoadAssetAtPath<Font>(
                    "Assets/VRTemplateAssets/Fonts/Inter/Inter-Regular.ttf");
            if (signFont == null) throw new InvalidOperationException("Menu font is missing.");
            MenuText("FESTIVAL FIELD GUIDE", rect, new Vector2(0, 265),
                new Vector2(455, 40), 27, Color.white);

            var buttons = new Button[ControllerVenueIds.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                int column = i % 3;
                int row = i / 3;
                var cell = new GameObject("Cue " + ControllerVenueLabels[i],
                    typeof(RectTransform), typeof(Image), typeof(Button));
                var cellRect = cell.GetComponent<RectTransform>();
                cellRect.SetParent(rect, false);
                cellRect.sizeDelta = new Vector2(144, 73);
                cellRect.anchoredPosition = new Vector2((column - 1) * 155, 178 - row * 87);
                var image = cell.GetComponent<Image>();
                image.color = i % 3 == 0 ? new Color(.32f, .48f, .49f, .96f) :
                    i % 3 == 1 ? new Color(.54f, .35f, .28f, .96f) :
                    new Color(.46f, .38f, .56f, .96f);
                var button = cell.GetComponent<Button>();
                button.targetGraphic = image;
                var colors = button.colors;
                colors.normalColor = image.color;
                colors.highlightedColor = new Color(.78f, .71f, .52f, 1f);
                colors.pressedColor = new Color(1f, .78f, .35f, 1f);
                colors.selectedColor = colors.highlightedColor;
                button.colors = colors;
                buttons[i] = button;
                MenuText(ControllerVenueLabels[i], cellRect, Vector2.zero,
                    new Vector2(136, 58), 18, Color.white);
            }

            var status = MenuText("POINT + TRIGGER  •  CUE A VENUE ITEM", rect,
                new Vector2(0, -265), new Vector2(465, 35), 17,
                new Color(1f, .84f, .50f));
            var menu = wrist.gameObject.AddComponent<VillageControllerMenu>();
            menu.venueIds = ControllerVenueIds.ToArray();
            menu.buttons = buttons;
            menu.venues = orderedVenues;
            menu.statusText = status;
        }

        private static Text MenuText(string value, RectTransform parent, Vector2 position,
            Vector2 size, int fontSize, Color color)
        {
            var go = new GameObject("Text - " + value, typeof(RectTransform), typeof(Text));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            var text = go.GetComponent<Text>();
            text.font = signFont;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.text = value;
            text.raycastTarget = false;
            return text;
        }
    }
}
