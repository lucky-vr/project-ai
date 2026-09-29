using System;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VillageDraft.Editor
{
    public static partial class VillageDraftBuilder
    {
        private static VillageProgressBoard board;

        private static void BuildGameplay()
        {
            board = CreateProgressBoard();
            var venueGroup = root.Find("04 Five activity places");
            if (venueGroup == null) throw new InvalidOperationException("Village venues are missing.");

            var market = VenueByPrefix(venueGroup, "01 MARKET");
            var cafe = VenueByPrefix(venueGroup, "02 CAFE");
            var workshop = VenueByPrefix(venueGroup, "03 WORKSHOP");
            var greenhouse = VenueByPrefix(venueGroup, "04 GREENHOUSE");
            var pool = VenueByPrefix(venueGroup, "05 POOL");

            BuildMarketGameplay(market);
            BuildCafeGameplay(cafe);
            BuildWorkshopGameplay(workshop);
            BuildGreenhouseGameplay(greenhouse);
            BuildPoolGameplay(pool);
            board.RefreshBoard();
        }

        private static Transform VenueByPrefix(Transform parent, string prefix)
        {
            foreach (Transform child in parent)
                if (child.name.StartsWith(prefix, StringComparison.Ordinal)) return child;
            throw new InvalidOperationException("Venue missing: " + prefix);
        }

        private static VillageProgressBoard CreateProgressBoard()
        {
            var plaza = root.Find("03 Welcome plaza");
            var boardObject = new GameObject("Village progress controller");
            boardObject.transform.SetParent(plaza);
            boardObject.transform.position = new Vector3(8.3f, 2.0f, -15);
            // The matching Blender frame is placed by BuildWorldRedesign.
            var textObject = new GameObject("Village progress UI text");
            textObject.transform.SetParent(plaza);
            textObject.transform.position = new Vector3(8.3f, 1.82f, -15.14f);
            AddText(textObject, "FESTIVAL PROGRESS", .16f, Color.white);
            var textMesh = textObject.GetComponent<TextMesh>();
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.characterSize = .050f;
            textMesh.lineSpacing = 1.25f;

            var controller = boardObject.AddComponent<VillageProgressBoard>();
            controller.boardText = textMesh;
            controller.heading = "FESTIVAL PROGRESS";
            controller.objectives = new[]
            {
                Objective("market", "Market checkout", 3, plaza, 3.2f),
                Objective("cafe", "Cafe barista", 2, plaza, 2.6f),
                Objective("workshop", "Workshop repair", 3, plaza, 2.0f),
                Objective("greenhouse", "Greenhouse planting", 2, plaza, 1.4f),
                Objective("pool", "Pool ring toss", 1, plaza, .8f)
            };
            return controller;
        }

        private static VillageProgressBoard.Objective Objective(string id, string label, int steps,
            Transform parent, float y)
        {
            var lamp = Primitive("Task lamp - " + id, PrimitiveType.Sphere, parent,
                new Vector3(11.8f, y, -15.2f), new Vector3(.20f, .20f, .20f), gold, false);
            return new VillageProgressBoard.Objective
            {
                id = id,
                label = label,
                stepsRequired = steps,
                indicator = lamp.GetComponent<Renderer>()
            };
        }

        private static void BuildMarketGameplay(Transform market)
        {
            var bread = GrabPrimitive("Grab bread", PrimitiveType.Cube, market,
                new Vector3(-2.7f, 1.86f, 1.25f), new Vector3(.42f, .26f, .34f), gold);
            var apple = GrabPrimitive("Grab apple", PrimitiveType.Sphere, market,
                new Vector3(-.35f, 1.88f, 1.45f), new Vector3(.34f, .34f, .34f), tomato);
            Socket("Bread basket socket", market, new Vector3(-1.82f, 2.04f, 1.45f),
                bread, "market", 0, roofRed, .4f);
            Socket("Apple basket socket", market, new Vector3(-1.18f, 2.04f, 1.45f),
                apple, "market", 1, roofRed, .4f);
            Button("Checkout button", market, new Vector3(2.2f, 1.93f, 1.08f),
                "market", 2, gold, new Vector3(.5f, .14f, .5f));
        }

        private static void BuildCafeGameplay(Transform cafe)
        {
            var brew = cafe.Find("Brew button");
            if (brew == null) throw new InvalidOperationException("Cafe brew button missing.");
            WireButton(brew.gameObject, "cafe", 0);
            var cup = GrabPrimitive("Grab coffee cup", PrimitiveType.Cylinder, cafe,
                new Vector3(.3f, 1.85f, 1.6f), new Vector3(.25f, .23f, .25f), white);
            Socket("Serving tray socket", cafe, new Vector3(2.8f, 1.0f, -2.1f),
                cup, "cafe", 1, roofOrange, .6f);
        }

        private static void BuildWorkshopGameplay(Transform workshop)
        {
            var switches = workshop.Cast<Transform>().Where(x => x.name == "Power switch").ToArray();
            if (switches.Length != 3) throw new InvalidOperationException("Expected three workshop switches.");
            foreach (var sw in switches) WireButton(sw.gameObject, "workshop", -1);
            var rotor = workshop.Find("Windmill rotor - scripted motion");
            if (rotor != null)
            {
                var motion = rotor.gameObject.AddComponent<VillageMovingProp>();
                motion.spinDegreesPerSecond = new Vector3(0, 0, 48);
                motion.bobAmplitude = 0;
            }
        }

        private static void BuildGreenhouseGameplay(Transform greenhouse)
        {
            Local("Seed tray", PrimitiveType.Cube, greenhouse,
                new Vector3(-4.35f, .46f, -2.9f), new Vector3(1.3f, .92f, 1.2f), timber);
            var seed = GrabPrimitive("Grab seed packet", PrimitiveType.Cube, greenhouse,
                new Vector3(-4.35f, 1.06f, -2.9f), new Vector3(.46f, .20f, .35f), gold);
            Socket("Planter seed socket", greenhouse, new Vector3(0, 1.1f, .5f),
                seed, "greenhouse", 0, roofGreen, .6f);
            Button("Water switch", greenhouse, new Vector3(4.4f, 2.1f, -2.8f),
                "greenhouse", 1, water, new Vector3(.42f, .14f, .42f));
        }

        private static void BuildPoolGameplay(Transform pool)
        {
            Local("Ring stand", PrimitiveType.Cylinder, pool,
                new Vector3(-4.6f, .55f, -3.0f), new Vector3(.45f, .55f, .45f), white);
            var ringRoot = new GameObject("Grab and toss ring");
            ringRoot.transform.SetParent(pool, false);
            ringRoot.transform.localPosition = new Vector3(-4.6f, 1.2f, -3.0f);
            var ringCollider = ringRoot.AddComponent<SphereCollider>();
            ringCollider.radius = .57f;
            var body = ringRoot.AddComponent<Rigidbody>();
            body.mass = .25f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var ring = ringRoot.AddComponent<XRGrabInteractable>();
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2 / 12;
                Local("Ring segment", PrimitiveType.Sphere, ringRoot.transform,
                    new Vector3(Mathf.Cos(a) * .42f, 0, Mathf.Sin(a) * .42f),
                    new Vector3(.18f, .18f, .18f), roofRed, false);
            }
            Socket("Floating pool ring target", pool, new Vector3(0, .68f, 2.2f),
                ring, "pool", 0, gold, .8f);
        }

        private static XRGrabInteractable GrabPrimitive(string name, PrimitiveType type, Transform parent,
            Vector3 localPosition, Vector3 scale, Material material)
        {
            var t = Local(name, type, parent, localPosition, scale, material);
            var body = t.gameObject.AddComponent<Rigidbody>();
            body.mass = .35f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            return t.gameObject.AddComponent<XRGrabInteractable>();
        }

        private static void Socket(string name, Transform parent, Vector3 localPosition,
            XRGrabInteractable item, string objectiveId, int step, Material accent, float radius)
        {
            var socketRoot = Group(name, parent);
            socketRoot.localPosition = localPosition;
            var trigger = socketRoot.gameObject.AddComponent<SphereCollider>();
            trigger.radius = radius;
            trigger.isTrigger = true;
            socketRoot.gameObject.AddComponent<XRSocketInteractor>();
            var target = Local("Placement marker", PrimitiveType.Cylinder, socketRoot,
                new Vector3(0, -.18f, 0), new Vector3(radius * .7f, .035f, radius * .7f), accent, false);
            var placement = socketRoot.gameObject.AddComponent<VillagePlaceTarget>();
            placement.progressBoard = board;
            placement.objectiveId = objectiveId;
            placement.requiredObject = item;
            placement.requiredCurrentStep = step;
            placement.targetVisual = target.GetComponent<Renderer>();
        }

        private static void Button(string name, Transform parent, Vector3 localPosition,
            string objectiveId, int step, Material material, Vector3 scale)
        {
            var t = Local(name, PrimitiveType.Cylinder, parent, localPosition, scale, material);
            WireButton(t.gameObject, objectiveId, step);
        }

        private static void WireButton(GameObject buttonObject, string objectiveId, int step)
        {
            buttonObject.AddComponent<XRSimpleInteractable>();
            var button = buttonObject.AddComponent<VillagePressButton>();
            button.progressBoard = board;
            button.objectiveId = objectiveId;
            button.requiredCurrentStep = step;
            button.buttonVisual = buttonObject.GetComponent<Renderer>();
        }
    }
}
