using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace VillageDraft.Editor
{
    public static partial class VillageDraftBuilder
    {
        private static Transform expandedRoot;

        private sealed class ExpandedSite
        {
            public string id, title, subtitle, actions;
            public Vector3 position;
            public Material accent;
            public ExpandedSite(string id, string title, string subtitle, Vector3 position, Material accent, string actions)
            {
                this.id = id; this.title = title; this.subtitle = subtitle;
                this.position = position; this.accent = accent; this.actions = actions;
            }
        }

        private sealed class ActionStep
        {
            public bool isButton;
            public string item, destination, instruction;
        }

        /// <summary>Build the ten outer venues, ring roads, quest UI, and replay control.</summary>
        private static void BuildExpandedGameplay()
        {
            expandedRoot = Group("06 Expanded village", root);
            if (board.onProgressChanged == null) board.onProgressChanged = new UnityEvent();
            if (board.onVillageComplete == null) board.onVillageComplete = new UnityEvent();
            var sites = ExpandedSites();
            BuildOuterRoads(sites);
            var venues = Group("02 Ten outer activity places", expandedRoot);
            var objectives = new List<VillageProgressBoard.Objective>(board.objectives);
            foreach (var site in sites)
            {
                var place = Venue($"{objectives.Count + 1:00} {site.title} - {site.subtitle}",
                    venues, site.position, site.accent, site.title, site.subtitle);
                var actions = ParseActions(site.actions);
                if (actions.Length != 10)
                    throw new InvalidOperationException(site.title + " needs exactly 10 actions.");
                BuildActionStations(place, site.id, actions, site.accent, false);
                AddVenueExperience(place, site.id, site.accent);
                AddVenueGuide(place, site.id, site.title, actions.Select(s => s.instruction).ToArray(),
                    actions.Select(s => s.isButton).ToArray(), site.accent);
                AddSignatureMotion(place, site.id, site.accent);
                var lamp = Local("Completion lamp", PrimitiveType.Sphere, place,
                    new Vector3(5.4f, 3.35f, -5.04f), new Vector3(.28f, .28f, .28f), gold, false);
                objectives.Add(new VillageProgressBoard.Objective
                {
                    id = site.id, label = site.title, stepsRequired = 10,
                    indicator = lamp.GetComponent<Renderer>()
                });
            }
            board.objectives = objectives.ToArray();
            BuildReplayControl();
            BuildFestivalSquare();
            UpdateEntranceWelcome();
            board.RefreshBoard();
        }

        private static void UpdateEntranceWelcome()
        {
            var plaza = root.Find("03 Welcome plaza");
            if (plaza == null) return;
            foreach (var label in plaza.GetComponentsInChildren<TextMesh>(true))
            {
                if (label.text.StartsWith("WELCOME TO", StringComparison.Ordinal))
                {
                    label.text = "HELP PREPARE THE FESTIVAL";
                    label.characterSize = .064f;
                }
                else if (label.text.StartsWith("15 PLACES", StringComparison.Ordinal))
                {
                    label.text = "Help all 15 places get ready.\nStart at MARKET: bread to basket.";
                    label.characterSize = .046f;
                    label.lineSpacing = 1.32f;
                }
                else if (label.text.StartsWith("Move", StringComparison.Ordinal))
                {
                    label.text = "Move: stick / teleport  •  Grab: grip\nTrigger: press  •  Wrist menu: cue items";
                    label.characterSize = .034f;
                    label.lineSpacing = 1.18f;
                    var simulatorHint = label.gameObject.AddComponent<VillageSimulatorWelcomeHint>();
                    simulatorHint.label = label;
                }
            }
        }

        /// <summary>Add distinct steps to the original five without changing their existing interactions.</summary>
        private static void ExpandOriginalGameplay()
        {
            if (expandedRoot == null) expandedRoot = root.Find("06 Expanded village");
            if (expandedRoot == null) throw new InvalidOperationException("BuildExpandedGameplay must run first.");
            var extensions = Group("03 Original venue action extensions", expandedRoot);
            var old = root.Find("04 Five activity places");
            ExtendOriginal(extensions, VenueByPrefix(old, "01 MARKET"), "market", "MARKET", roofRed,
                new[] { "Place bread in the basket", "Place apple in the basket", "Press checkout" },
                new[] { false, false, true },
                @"P|Milk carton|Cold shelf|Stock milk on the cold shelf
P|Cheese wheel|Deli tray|Move cheese to the deli tray
B|Price scanner|Scan the purchase
P|Shopping basket|Packing table|Carry basket to packing table
P|Coin purse|Cash drawer|Put coins into the drawer
B|Receipt printer|Print the receipt
P|Receipt slip|Customer pickup|Hand the receipt to pickup");
            ExtendOriginal(extensions, VenueByPrefix(old, "02 CAFE"), "cafe", "CAFE", roofOrange,
                new[] { "Press brew on the coffee machine", "Place cup on serving tray" },
                new[] { true, false },
                @"P|Coffee beans|Grinder hopper|Fill the grinder with beans
B|Grinder switch|Grind the beans
P|Milk carton|Steaming pitcher|Pour milk into the pitcher
B|Steam control|Steam the milk
P|Sugar jar|Condiment tray|Set sugar on the tray
P|Saucer|Table setting|Prepare a saucer
B|Order bell|Ring for the customer
P|Menu card|Pickup stand|Present the completed menu");
            ExtendOriginal(extensions, VenueByPrefix(old, "03 WORKSHOP"), "workshop", "WORKSHOP", roofBlue,
                new[] { "Press power circuit one", "Press power circuit two", "Press power circuit three" },
                new[] { true, true, true },
                @"P|Wrench|Tool rack|Return wrench to the rack
P|Gear|Drive shaft|Install a gear on the drive shaft
B|Calibration dial|Calibrate the machine
P|Battery|Power bay|Install the backup battery
P|Oil can|Lubrication shelf|Set the oil can for lubrication
B|Safety reset|Reset the safety relay
P|Finished part|Inspection stand|Move the part to inspection");
            DistinguishWorkshopCircuits(VenueByPrefix(old, "03 WORKSHOP"));
            ExtendOriginal(extensions, VenueByPrefix(old, "04 GREENHOUSE"), "greenhouse", "GREENHOUSE", roofGreen,
                new[] { "Plant the seed packet", "Turn on water" },
                new[] { false, true },
                @"P|Soil bag|Potting bench|Bring soil to the potting bench
P|Clay pot|Seedling rack|Set a pot on the seedling rack
B|Grow lamp|Switch on the grow lamp
P|Fertilizer bag|Mixing shelf|Prepare fertilizer
P|Pruning shears|Tool hanger|Hang pruning shears
B|Vent control|Open the greenhouse vent
P|Harvest basket|Packing shelf|Move the harvest basket
P|Plant label|Display planter|Label the finished plant");
            ExtendOriginal(extensions, VenueByPrefix(old, "05 POOL"), "pool", "POOL", roofTeal,
                new[] { "Toss the ring onto the target" },
                new[] { false },
                @"P|Kickboard|Equipment rack|Put the kickboard on its rack
P|Goggles|Swimmer tray|Set goggles on the swimmer tray
B|Lane timer|Start the lane timer
P|Rescue buoy|Lifeguard hook|Hang the rescue buoy
P|Towel|Drying rack|Put a towel on the rack
B|Pool lights|Turn on the pool lights
P|Water sample|Testing stand|Place the water sample
B|Filter control|Start the pool filter
P|Award ribbon|Podium|Place the award on the podium");
            board.RefreshBoard();
            WireVenueTaskGuides();
            BuildFirstMarketCue(VenueByPrefix(old, "01 MARKET"));
            BuildControllerFestivalMenu();
            SkinExpandedGameplay();
        }

        private static void ExtendOriginal(Transform parent, Transform original, string id, string title,
            Material accent, string[] originalInstructions, bool[] originalPressSteps, string addedActions)
        {
            var extra = ParseActions(addedActions);
            var start = originalInstructions.Length;
            if (start + extra.Length != 10 || originalPressSteps.Length != start)
                throw new InvalidOperationException(title + " must have ten total steps.");
            var stage = Group(title + " extra activities", parent);
            stage.SetPositionAndRotation(original.position, original.rotation);
            BuildActionStations(stage, id, extra, accent, true, start);
            AddVenueExperience(stage, id, accent);
            var allInstructions = originalInstructions.Concat(extra.Select(s => s.instruction)).ToArray();
            var allPressSteps = originalPressSteps.Concat(extra.Select(s => s.isButton)).ToArray();
            AddVenueGuide(stage, id, title, allInstructions, allPressSteps, accent);
            foreach (var objective in board.objectives)
                if (objective != null && objective.id == id)
                    objective.stepsRequired = 10;
        }

        private static void DistinguishWorkshopCircuits(Transform workshop)
        {
            var switches = workshop.Cast<Transform>().Where(x => x.name == "Power switch")
                .OrderBy(x => x.localPosition.x).ToArray();
            var lamps = workshop.Cast<Transform>().Where(x => x.name == "Switch indicator")
                .OrderBy(x => x.localPosition.x).ToArray();
            if (switches.Length != 3 || lamps.Length != 3)
                throw new InvalidOperationException("Workshop circuits need three switches and lamps.");
            var names = new[] { "LIGHTING", "ROTOR", "SAFETY" };
            for (int i = 0; i < 3; i++)
            {
                var button = switches[i].GetComponent<VillagePressButton>();
                button.requiredCurrentStep = i;
                LocalLabel(names[i] + " CIRCUIT", workshop,
                    new Vector3(switches[i].localPosition.x, 2.55f, 1.50f), 0, .11f, dark.color);
                var effect = switches[i].gameObject.AddComponent<VillageActionEffect>();
                effect.sourceButton = button;
                effect.target = lamps[i];
                effect.effectKind = i == 0 ? VillageActionEffect.EffectKind.Grow :
                    i == 1 ? VillageActionEffect.EffectKind.Slide : VillageActionEffect.EffectKind.Raise;
                if (i == 1) effect.direction = Vector3.right;
            }
        }

        private static void BuildActionStations(Transform parent, string objectiveId, ActionStep[] steps,
            Material accent, bool exterior, int firstStep = 0)
        {
            var stations = Group("Ten-step interaction stations", parent);
            Local(objectiveId + " activity floor", PrimitiveType.Cube, stations,
                new Vector3(-9.6f, .015f, -2.85f), new Vector3(6.8f, .03f, 12.15f), paving, false);
            for (int row = 0; row < 5; row++)
                Local("Process lane " + (row + 1), PrimitiveType.Cube, stations,
                    new Vector3(-9.6f, .038f, -8.15f + row * 2.65f),
                    new Vector3(3.7f, .012f, .06f), accent, false);
            for (int i = 0; i < steps.Length; i++)
            {
                int absolute = firstStep + i;
                var step = steps[i];
                var station = Group($"Step {absolute + 1:00} - {step.instruction}", stations);
                var pose = StationPose(objectiveId, absolute);
                station.localPosition = new Vector3(pose.x, 0, pose.y);
                var towardMachine = new Vector3(-9.6f - pose.x, 0, -2.9f - pose.y);
                if (towardMachine.sqrMagnitude > .01f)
                    station.localRotation = Quaternion.LookRotation(-towardMachine.normalized);
                var workMaterial = objectiveId == "clinic" || objectiveId == "library" || objectiveId == "art"
                    ? white : objectiveId == "greenhouse" || objectiveId == "recycling" ? roofGreen :
                    objectiveId == "music" || objectiveId == "observatory" ? dark : timber;
                var workShape = objectiveId == "music" || objectiveId == "observatory" ||
                    objectiveId == "pool" || objectiveId == "art" ? PrimitiveType.Cylinder : PrimitiveType.Cube;
                int layout = absolute % 5;
                if (layout == 3)
                {
                    Local(objectiveId + " preparation counter", PrimitiveType.Cube, station,
                        new Vector3(-.45f, .53f, 0), new Vector3(.9f, 1.05f, 1.4f), workMaterial);
                    Local("Ground sorting bin", PrimitiveType.Cylinder, station,
                        new Vector3(.50f, .20f, .25f), new Vector3(.43f, .20f, .43f), accent);
                }
                else
                {
                    var surfaceName = objectiveId + (layout == 1 ? " elevated shelf" :
                        layout == 2 ? " side rack" : layout == 4 ? " inspection pedestal" : " action station");
                    Local(surfaceName, workShape, station,
                        new Vector3(0, .53f, 0), new Vector3(1.55f, 1.05f, 1.30f), workMaterial);
                    if (layout == 1)
                        Local("Elevated shelf", PrimitiveType.Cube, station,
                            new Vector3(.42f, 1.58f, .25f), new Vector3(.75f, .12f, .65f), accent);
                    if (layout == 2)
                        Local("Wall rack", PrimitiveType.Cube, station,
                            new Vector3(-.43f, 1.52f, .27f), new Vector3(.7f, .12f, .60f), accent);
                    if (layout == 4)
                        Local("Inspection riser", PrimitiveType.Cylinder, station,
                            new Vector3(.38f, 1.16f, -.22f), new Vector3(.42f, .13f, .42f), accent);
                }
                var band = Local("Station color band", PrimitiveType.Cube, station,
                    new Vector3(0, 1.075f, -.52f), new Vector3(1.45f, .08f, .14f), accent, false);
                // Put the station number on the counter face instead of floating over the controls.
                float plateX = layout == 3 ? -.45f : 0;
                float frontZ = layout == 3 ? -.72f : -.665f;
                Local("Step number plate", PrimitiveType.Cube, station,
                    new Vector3(plateX, .78f, frontZ), new Vector3(.52f, .31f, .035f), dark, false);
                LocalLabel((absolute + 1).ToString("00"), station,
                    new Vector3(plateX, .78f, frontZ - .025f), 0, .105f, Color.white);
                var beacon = Local("Current task inset light", PrimitiveType.Cube, station,
                    new Vector3(plateX + .19f, .78f, frontZ - .006f),
                    new Vector3(.075f, .22f, .045f), gold, false);
                beacon.GetComponent<Renderer>().enabled = absolute == 0;
                var marker = station.gameObject.AddComponent<VillageStepBeacon>();
                marker.progressBoard = board;
                marker.objectiveId = objectiveId;
                marker.stepIndex = absolute;
                marker.beacon = beacon.GetComponent<Renderer>();
                marker.band = band.GetComponent<Renderer>();
                if (step.isButton)
                {
                    var control = Local(step.item, PrimitiveType.Cylinder, station,
                        new Vector3(-.37f, 1.18f, 0), new Vector3(.32f, .10f, .32f), gold);
                    WireButton(control.gameObject, objectiveId, absolute);
                    var response = Local("Scripted action result", PrimitiveType.Sphere, station,
                        new Vector3(.40f, 1.3f, .10f), new Vector3(.30f, .30f, .30f), accent, false);
                    var effect = station.gameObject.AddComponent<VillageActionEffect>();
                    effect.target = response;
                    effect.effectKind = (VillageActionEffect.EffectKind)(absolute % 4);
                    effect.direction = absolute % 2 == 0 ? Vector3.up : Vector3.right;
                    var press = control.GetComponent<VillagePressButton>();
                    if (press.onActivated == null) press.onActivated = new UnityEvent();
                    effect.sourceButton = press;
                }
                else
                {
                    var type = absolute % 3 == 0 ? PrimitiveType.Cylinder :
                        absolute % 3 == 1 ? PrimitiveType.Cube : PrimitiveType.Sphere;
                    var sourcePosition = layout == 2 ? new Vector3(-.43f, 1.78f, .27f) :
                        layout == 3 ? new Vector3(-.43f, 1.32f, -.16f) :
                        layout == 1 ? new Vector3(-.43f, 1.33f, -.25f) :
                        new Vector3(-.40f, 1.33f, .10f);
                    var targetPosition = layout == 1 ? new Vector3(.42f, 1.86f, .25f) :
                        layout == 3 ? new Vector3(.50f, .61f, .25f) :
                        layout == 4 ? new Vector3(.38f, 1.50f, -.22f) :
                        layout == 2 ? new Vector3(.40f, 1.30f, -.20f) :
                        new Vector3(.40f, 1.30f, .10f);
                    var item = GrabPrimitive(step.item, type, station,
                        sourcePosition, new Vector3(.35f, .35f, .35f),
                        absolute % 2 == 0 ? accent : gold);
                    Socket(step.destination + " socket", station, targetPosition,
                        item, objectiveId, absolute, accent, .36f);
                }
            }
        }

        private static Vector2 StationPose(string id, int step)
        {
            // Every worktop is at least 2.65 m from its neighbour; the activity yard
            // remains within the world's 17 m clear zone around each venue.
            float row = -8.15f + (step / 2) * 2.65f;
            if (id == "music" || id == "observatory" || id == "art" || id == "pool")
            {
                // Alternating left/right performance or discovery stations.
                float stagger = (step / 2) % 2 == 0 ? .25f : -.25f;
                return new Vector2((step & 1) == 0 ? -11.60f - stagger : -7.60f + stagger, row);
            }
            if (id == "clinic" || id == "garage" || id == "workshop")
            {
                // Horseshoe around a patient, vehicle, or machine.
                return new Vector2(step < 5 ? -11.55f : -7.65f,
                    -8.15f + (step < 5 ? step : 9 - step) * 2.65f);
            }
            if (id == "harbor" || id == "post" || id == "recycling" || id == "library")
            {
                // Two lanes: receive/sort on one side, dispatch/store on the other.
                int laneStep = step < 5 ? step : 9 - step;
                float x = step < 5 ? -11.55f : -7.65f;
                return new Vector2(x, -8.15f + laneStep * 2.65f);
            }
            // Food and garden venues follow a serpentine process around the central workpiece.
            return new Vector2((step & 1) == 0 ? -11.55f : -7.65f, row);
        }

        private static void AddVenueGuide(Transform parent, string id, string title,
            string[] instructions, bool[] pressSteps, Material accent)
        {
            if (instructions.Length != 10 || pressSteps.Length != 10)
                throw new InvalidOperationException(title + " guide needs ten typed actions.");
            // Freestanding beside the entrance, clear of the facade's model shelves.
            var kiosk = Group("Freestanding current task kiosk", parent);
            kiosk.localPosition = new Vector3(6.8f, 0, -7.3f);
            kiosk.localRotation = Quaternion.Euler(0, 82f, 0);
            Local("Task kiosk left post", PrimitiveType.Cube, kiosk,
                new Vector3(-1.52f, .90f, 0), new Vector3(.12f, 1.8f, .14f), timber, false);
            Local("Task kiosk right post", PrimitiveType.Cube, kiosk,
                new Vector3(1.52f, .90f, 0), new Vector3(.12f, 1.8f, .14f), timber, false);
            Local("Task kiosk foot", PrimitiveType.Cube, kiosk,
                new Vector3(0, .10f, 0), new Vector3(3.35f, .20f, .56f), paving, false);
            var sign = Local("Current task UI kiosk", PrimitiveType.Cube, kiosk,
                new Vector3(0, 1.76f, 0), new Vector3(3.45f, 1.95f, .12f), dark, false);
            Local("Task kiosk accent header", PrimitiveType.Cube, kiosk,
                new Vector3(0, 2.75f, -.045f), new Vector3(3.45f, .12f, .08f), accent, false);
            var textObject = Group("Current task UI text", kiosk);
            textObject.localPosition = new Vector3(0, 1.80f, -.081f);
            AddText(textObject.gameObject,
                VillageVenueTaskGuide.FormatPrompt(title, 0, instructions, pressSteps), .13f, Color.white);
            var text = textObject.GetComponent<TextMesh>();
            text.characterSize = .036f;
            text.lineSpacing = 1.25f;
            var guide = sign.gameObject.AddComponent<VillageVenueTaskGuide>();
            guide.progressBoard = board;
            guide.objectiveId = id;
            guide.venueTitle = title;
            guide.instructions = instructions;
            guide.pressSteps = pressSteps;
            guide.promptText = text;
        }

        private static void AddSignatureMotion(Transform venue, string id, Material accent)
        {
            // A visibly mounted drive wheel on the yard machine, never a floating facade emblem.
            var machine = venue.Find(id + " festival contribution machine");
            if (machine == null) throw new InvalidOperationException(id + " is missing its festival machine.");
            Local(id + " drive axle", PrimitiveType.Cube, machine,
                new Vector3(-.75f, 1.42f, .45f), new Vector3(.11f, .85f, .11f), dark, false);
            var kinetic = Local(id + " mounted drive wheel", PrimitiveType.Cylinder, machine,
                new Vector3(-.75f, 1.84f, .45f), new Vector3(.30f, .08f, .30f), accent, false);
            var motion = kinetic.gameObject.AddComponent<VillageMovingProp>();
            motion.spinDegreesPerSecond = new Vector3(0, 45, 0);
            motion.bobAmplitude = 0;
        }

        private static void AddVenueExperience(Transform venue, string id, Material accent)
        {
            if (!Enum.TryParse(id, true, out VillageVenueExperience.Activity activity))
                throw new InvalidOperationException("Unknown venue activity: " + id);
            Local(id + " activity-yard paving spur", PrimitiveType.Cube, venue,
                new Vector3(-6.1f, .025f, -6.5f), new Vector3(7.4f, .035f, 1.9f), paving, false);
            Local("Activity yard direction sign", PrimitiveType.Cube, venue,
                new Vector3(-3.83f, 1.88f, -5.64f), new Vector3(2.45f, .44f, .10f), dark, false);
            LocalLabel("ACTIVITY YARD  ←", venue,
                new Vector3(-3.83f, 1.88f, -5.72f), 0, .10f, Color.white);
            var machine = Group(id + " festival contribution machine", venue);
            machine.localPosition = new Vector3(-9.6f, 0, -2.9f);
            var foundation = Local(id + " themed mechanism base", PrimitiveType.Cylinder, machine,
                new Vector3(0, .27f, 0), new Vector3(.80f, .27f, .80f), accent, false);
            Transform focus;
            Transform secondary;
            switch (activity)
            {
                case VillageVenueExperience.Activity.Market:
                    focus = Local("Cash drawer for festival supplies", PrimitiveType.Cube, machine,
                        new Vector3(0, 1.15f, -.24f), new Vector3(.70f, .22f, .50f), gold, false);
                    secondary = Local("Festival grocery basket fills", PrimitiveType.Cube, machine,
                        new Vector3(0, 1.65f, .18f), new Vector3(.50f, .35f, .50f), tomato, false);
                    break;
                case VillageVenueExperience.Activity.Cafe:
                    focus = Local("Coffee rises in serving cup", PrimitiveType.Cylinder, machine,
                        new Vector3(0, 1.10f, -.15f), new Vector3(.42f, .14f, .42f), trunk, false);
                    secondary = Local("Cafe steam wand", PrimitiveType.Cube, machine,
                        new Vector3(.35f, 1.78f, 0), new Vector3(.12f, .78f, .12f), dark, false);
                    break;
                case VillageVenueExperience.Activity.Workshop:
                    focus = Local("Festival light generator gear", PrimitiveType.Cylinder, machine,
                        new Vector3(0, 1.35f, -.15f), new Vector3(.55f, .13f, .55f), gold, false);
                    secondary = Local("Power column rises", PrimitiveType.Cube, machine,
                        new Vector3(.35f, 1.65f, .20f), new Vector3(.18f, .65f, .18f), roofBlue, false);
                    break;
                case VillageVenueExperience.Activity.Greenhouse:
                    focus = Local("Festival harvest plant grows", PrimitiveType.Sphere, machine,
                        new Vector3(0, 1.22f, 0), new Vector3(.42f, .42f, .42f), leaf, false);
                    secondary = Local("Greenhouse sprinkler", PrimitiveType.Cube, machine,
                        new Vector3(.32f, 1.80f, .06f), new Vector3(.12f, .70f, .12f), water, false);
                    break;
                case VillageVenueExperience.Activity.Pool:
                    focus = Local("Festival water marker floats", PrimitiveType.Sphere, machine,
                        new Vector3(0, 1.15f, 0), new Vector3(.48f, .24f, .48f), water, false);
                    secondary = Local("Race timer needle", PrimitiveType.Cube, machine,
                        new Vector3(.35f, 1.70f, 0), new Vector3(.10f, .62f, .10f), roofRed, false);
                    break;
                case VillageVenueExperience.Activity.Bakery:
                    Local("Festival bread oven", PrimitiveType.Cube, machine,
                        new Vector3(0, 1.45f, .30f), new Vector3(1.25f, 1.35f, .65f), trunk, false);
                    focus = Local("Dough rises for festival bread", PrimitiveType.Sphere, machine,
                        new Vector3(0, 1.12f, -.18f), new Vector3(.42f, .28f, .42f), gold, false);
                    secondary = Local("Oven door opens", PrimitiveType.Cube, machine,
                        new Vector3(0, 1.36f, -.12f), new Vector3(.88f, .60f, .09f), roofOrange, false);
                    break;
                case VillageVenueExperience.Activity.Post:
                    Local("Mail sorting conveyor", PrimitiveType.Cube, machine,
                        new Vector3(0, .90f, 0), new Vector3(1.35f, .22f, .70f), dark, false);
                    focus = Local("Parcel crosses the sorting belt", PrimitiveType.Cube, machine,
                        new Vector3(-.50f, 1.12f, 0), new Vector3(.30f, .30f, .30f), roofRed, false);
                    secondary = Local("Postal stamp press", PrimitiveType.Cube, machine,
                        new Vector3(.34f, 1.62f, .05f), new Vector3(.26f, .50f, .28f), gold, false);
                    break;
                case VillageVenueExperience.Activity.Clinic:
                    Local("Patient monitor screen", PrimitiveType.Cube, machine,
                        new Vector3(0, 1.55f, .24f), new Vector3(1.2f, .75f, .12f), dark, false);
                    focus = Local("Patient heartbeat trace", PrimitiveType.Cube, machine,
                        new Vector3(0, 1.52f, .13f), new Vector3(.85f, .08f, .08f), roofGreen, false);
                    secondary = Local("Diagnostic scanner arm", PrimitiveType.Cylinder, machine,
                        new Vector3(.35f, 1.98f, -.15f), new Vector3(.10f, .52f, .10f), roofTeal, false);
                    break;
                case VillageVenueExperience.Activity.Library:
                    focus = Local("Festival book stack grows", PrimitiveType.Cube, machine,
                        new Vector3(0, 1.12f, 0), new Vector3(.65f, .30f, .46f), roofBlue, false);
                    secondary = Local("Storybook pages open", PrimitiveType.Cube, machine,
                        new Vector3(.35f, 1.63f, -.15f), new Vector3(.48f, .08f, .38f), cream, false);
                    break;
                case VillageVenueExperience.Activity.Music:
                    Local("Concert stage", PrimitiveType.Cube, machine,
                        new Vector3(0, .72f, 0), new Vector3(1.35f, .30f, 1.05f), dark, false);
                    focus = Local("Festival music equalizer", PrimitiveType.Cube, machine,
                        new Vector3(0, 1.28f, 0), new Vector3(.34f, .70f, .32f), gold, false);
                    secondary = Local("Concert spotlight", PrimitiveType.Cylinder, machine,
                        new Vector3(.47f, 1.86f, 0), new Vector3(.20f, .36f, .20f), white, false);
                    break;
                case VillageVenueExperience.Activity.Garage:
                    Local("Festival float chassis", PrimitiveType.Cube, machine,
                        new Vector3(0, 1.48f, .05f), new Vector3(1.25f, .40f, .80f), roofBlue, false);
                    focus = Local("Vehicle lift rises", PrimitiveType.Cube, machine,
                        new Vector3(0, .88f, 0), new Vector3(1.35f, .16f, .85f), dark, false);
                    secondary = Local("Service wheel turns", PrimitiveType.Cylinder, machine,
                        new Vector3(.52f, 1.39f, -.18f), new Vector3(.30f, .10f, .30f), trunk, false);
                    secondary.localRotation = Quaternion.Euler(90, 0, 0);
                    break;
                case VillageVenueExperience.Activity.Observatory:
                    focus = Local("Telescope aims at festival star", PrimitiveType.Cylinder, machine,
                        new Vector3(0, 1.48f, 0), new Vector3(.22f, .70f, .22f), roofTeal, false);
                    focus.localRotation = Quaternion.Euler(90, 0, 0);
                    secondary = Local("Star alignment dial", PrimitiveType.Cube, machine,
                        new Vector3(.36f, 1.16f, -.22f), new Vector3(.55f, .08f, .55f), gold, false);
                    break;
                case VillageVenueExperience.Activity.Harbor:
                    Local("Dock crane mast", PrimitiveType.Cylinder, machine,
                        new Vector3(0, 1.45f, .28f), new Vector3(.10f, .92f, .10f), timber, false);
                    focus = Local("Crane boom swings to festival cargo", PrimitiveType.Cube, machine,
                        new Vector3(0, 2.26f, -.08f), new Vector3(.18f, .18f, 1.05f), roofOrange, false);
                    secondary = Local("Cargo hook lifts", PrimitiveType.Cube, machine,
                        new Vector3(0, 1.22f, -.48f), new Vector3(.16f, .36f, .16f), dark, false);
                    break;
                case VillageVenueExperience.Activity.Art:
                    Local("Festival painting canvas", PrimitiveType.Cube, machine,
                        new Vector3(0, 1.75f, .33f), new Vector3(1.30f, 1.15f, .10f), white, false);
                    focus = Local("Sculpture display turntable", PrimitiveType.Cylinder, machine,
                        new Vector3(0, 1.02f, -.27f), new Vector3(.55f, .10f, .55f), roofRed, false);
                    secondary = Local("Gallery spotlit orb", PrimitiveType.Sphere, machine,
                        new Vector3(.38f, 2.11f, -.10f), new Vector3(.21f, .21f, .21f), gold, false);
                    break;
                case VillageVenueExperience.Activity.Recycling:
                    Local("Festival recycling conveyor", PrimitiveType.Cube, machine,
                        new Vector3(0, .92f, 0), new Vector3(1.35f, .22f, .68f), dark, false);
                    focus = Local("Sorted bundle moves to compactor", PrimitiveType.Cube, machine,
                        new Vector3(-.45f, 1.15f, 0), new Vector3(.30f, .32f, .30f), roofGreen, false);
                    secondary = Local("Compactor plate descends", PrimitiveType.Cube, machine,
                        new Vector3(.36f, 2.10f, 0), new Vector3(.65f, .14f, .50f), roofBlue, false);
                    break;
                default: throw new InvalidOperationException("Missing venue machine for " + id);
            }

            var pips = new Renderer[10];
            for (int i = 0; i < pips.Length; i++)
            {
                var pip = Local("Process light " + (i + 1), PrimitiveType.Sphere, machine,
                    new Vector3(-.50f + (i % 5) * .25f, 2.66f + (i / 5) * .25f, -.53f),
                    new Vector3(.13f, .13f, .13f), dark, false);
                pips[i] = pip.GetComponent<Renderer>();
            }
            var status = Group("Festival contribution status", machine);
            status.localPosition = new Vector3(0, 3.18f, -.55f);
            AddText(status.gameObject, id.ToUpperInvariant() + "  0/10", .10f, Color.white);
            var text = status.GetComponent<TextMesh>();
            text.characterSize = .033f;

            var experience = machine.gameObject.AddComponent<VillageVenueExperience>();
            experience.progressBoard = board;
            experience.objectiveId = id;
            experience.activity = activity;
            experience.focus = focus;
            experience.secondary = secondary;
            experience.milestones = pips;
            experience.outcomeText = text;
            if (activity == VillageVenueExperience.Activity.Music)
            {
                var source = machine.gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 1f;
                source.maxDistance = 10f;
                source.volume = .55f;
                experience.musicSource = source;
            }
            if (activity == VillageVenueExperience.Activity.Art)
            {
                var strokes = new GameObject[10];
                for (int i = 0; i < strokes.Length; i++)
                {
                    var stroke = Local("Paint stroke " + (i + 1), PrimitiveType.Cube, machine,
                        new Vector3(-.48f + (i % 5) * .24f, 1.45f + (i / 5) * .35f, .26f),
                        new Vector3(.18f, .24f, .03f), i % 2 == 0 ? roofOrange : roofTeal, false);
                    stroke.gameObject.SetActive(false);
                    strokes[i] = stroke.gameObject;
                }
                experience.revealPieces = strokes;
            }
        }

        private static void BuildReplayControl()
        {
            var plaza = Group("04 Village win and replay UI", expandedRoot);
            var panel = Primitive("Quest status backing", PrimitiveType.Cube, plaza,
                new Vector3(15, 2.35f, -20), new Vector3(5.2f, 1.25f, .16f), dark, false);
            var status = Group("Quest status text", plaza);
            status.position = new Vector3(15, 2.35f, -20.1f);
            AddText(status.gameObject, "VILLAGE QUEST", .13f, Color.white);
            var replay = Primitive("REPLAY whole village button", PrimitiveType.Cylinder, plaza,
                new Vector3(15, 1.2f, -20.4f), new Vector3(.6f, .12f, .6f), roofTeal);
            replay.AddComponent<XRSimpleInteractable>();
            Label("REPLAY", plaza, new Vector3(15, 1.6f, -20.5f), 0, .13f, Color.white);
            var flow = panel.AddComponent<VillageGameFlow>();
            flow.progressBoard = board;
            flow.statusText = status.GetComponent<TextMesh>();
            flow.replayButton = replay.GetComponent<XRSimpleInteractable>();
        }

        private static void BuildFestivalSquare()
        {
            var festival = Group("05 Festival square fills as places complete", expandedRoot);
            var contributions = new GameObject[board.objectives.Length];
            var ids = new string[board.objectives.Length];
            for (int i = 0; i < board.objectives.Length; i++)
            {
                var id = board.objectives[i].id;
                ids[i] = id;
                float angle = (i * 360f / board.objectives.Length) * Mathf.Deg2Rad;
                var stand = Group("Festival place " + (i + 1) + " - " + id, festival);
                stand.position = new Vector3(Mathf.Sin(angle) * 7.1f, 0, Mathf.Cos(angle) * 7.1f);
                stand.rotation = Quaternion.LookRotation(-stand.position.normalized);
                Local("Contribution plinth", PrimitiveType.Cylinder, stand,
                    new Vector3(0, .35f, 0), new Vector3(.56f, .35f, .56f), paving, false);
                LocalLabel(id.ToUpperInvariant(), stand, new Vector3(0, 1.08f, -.36f),
                    0, .075f, Color.white);
                var item = Group("Finished festival contribution - " + id, stand);
                item.localPosition = new Vector3(0, .88f, 0);
                var accent = id == "greenhouse" || id == "recycling" ? roofGreen :
                    id == "pool" || id == "harbor" || id == "clinic" ? roofTeal :
                    id == "workshop" || id == "garage" || id == "library" ? roofBlue :
                    id == "market" || id == "post" || id == "art" ? roofRed : roofOrange;
                var type = id == "garage" || id == "recycling" || id == "pool" ? PrimitiveType.Cylinder :
                    id == "bakery" || id == "greenhouse" || id == "observatory" ? PrimitiveType.Sphere :
                    PrimitiveType.Cube;
                Local(id + " finished artifact", type, item, Vector3.zero,
                    new Vector3(.45f, .45f, .45f), accent, false);
                if (id == "music" || id == "workshop" || id == "observatory")
                {
                    var movement = item.gameObject.AddComponent<VillageMovingProp>();
                    movement.spinDegreesPerSecond = new Vector3(0, 24f, 0);
                    movement.bobAmplitude = .06f;
                }
                item.gameObject.SetActive(false);
                contributions[i] = item.gameObject;
            }
            var banner = Group("Festival progress message", festival);
            banner.position = new Vector3(0, 3.15f, -4.45f);
            AddText(banner.gameObject, "PREPARING THE FESTIVAL", .16f, Color.white);
            var square = festival.gameObject.AddComponent<VillageFestivalSquare>();
            square.progressBoard = board;
            square.objectiveIds = ids;
            square.contributions = contributions;
            square.bannerText = banner.GetComponent<TextMesh>();
        }

        private static void BuildOuterRoads(ExpandedSite[] sites)
        {
            var roads = Group("01 Outer ring promenade and radial paths", expandedRoot);
            for (int i = 0; i < sites.Length; i++)
            {
                var a = sites[i].position * .70f;
                var b = sites[(i + 1) % sites.Length].position * .70f;
                Road("Outer ring promenade segment " + (i + 1), roads, a, b, 4.3f);
                var nearFront = sites[i].position - sites[i].position.normalized * 8.0f;
                Road(sites[i].title + " radial approach", roads, a, nearFront, 3.4f);
            }
            Road("West inner-to-outer link", roads, new Vector3(-31, 0, 0), new Vector3(-45, 0, 0), 4.0f);
            Road("East inner-to-outer link", roads, new Vector3(31, 0, 0), new Vector3(45, 0, 0), 4.0f);
            Road("North inner-to-outer link", roads, new Vector3(0, 0, 25), new Vector3(0, 0, 47), 4.0f);
            Road("South inner-to-outer link", roads, new Vector3(0, 0, -37), new Vector3(0, 0, -47), 4.0f);
        }

        private static ActionStep[] ParseActions(string source)
        {
            var result = new List<ActionStep>();
            foreach (var line in source.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var fields = line.Trim().Split('|');
                if (fields[0] == "P" && fields.Length == 4)
                    result.Add(new ActionStep { item = fields[1], destination = fields[2], instruction = fields[3] });
                else if (fields[0] == "B" && fields.Length == 3)
                    result.Add(new ActionStep { isButton = true, item = fields[1], instruction = fields[2] });
                else throw new InvalidOperationException("Bad village action specification: " + line);
            }
            return result.ToArray();
        }

        private static ExpandedSite[] ExpandedSites() => new[]
        {
            new ExpandedSite("bakery", "BAKERY", "Bake and pack bread", new Vector3(-64,0,45), roofOrange, @"P|Flour scoop|Mixing bowl|Scoop flour into the bowl
P|Yeast jar|Dough tray|Add yeast to the dough tray
B|Mixer switch|Start the dough mixer
P|Dough ball|Proofing shelf|Move dough to the proofing shelf
B|Proofing timer|Set the proofing timer
P|Bread loaf|Oven rack|Put bread onto the oven rack
B|Oven heat switch|Heat the oven
P|Baked loaf|Cooling rack|Move loaf to the cooling rack
P|Cake slice|Display case|Stock the cake display
P|Pastry box|Pickup counter|Pack a pastry order"),
            new ExpandedSite("post", "POST OFFICE", "Sort and dispatch mail", new Vector3(-39,0,64), roofRed, @"P|Incoming letter|Sorting tray|Sort the incoming letter
P|Parcel box|Weighing scale|Place parcel on the scale
B|Scale control|Weigh the parcel
P|Stamp pad|Stamp desk|Prepare the stamp pad
P|Address label|Parcel label area|Add an address label
B|Sorting belt switch|Start the sorting belt
P|Priority envelope|Express bin|Sort priority mail
P|Delivery bag|Courier shelf|Prepare the delivery bag
B|Dispatch bell|Signal the courier
P|Final parcel|Outgoing chute|Dispatch the final parcel"),
            new ExpandedSite("clinic", "CLINIC", "Examine and treat", new Vector3(0,0,67), roofTeal, @"P|Patient card|Registration tray|Register the patient card
P|Thermometer|Exam table|Set out the thermometer
B|Vitals monitor|Start the vitals monitor
P|Bandage roll|Treatment tray|Prepare bandages
P|Medicine bottle|Dispensing shelf|Prepare the medicine
B|X-ray control|Run the diagnostic scan
P|Scan film|Review board|Place scan film for review
P|First aid kit|Patient bed|Bring a first aid kit
B|Call button|Call the nurse
P|Discharge form|Front desk|File the discharge form"),
            new ExpandedSite("library", "LIBRARY", "Catalog and shelve", new Vector3(39,0,64), roofBlue, @"P|Returned novel|Returns cart|Receive a returned novel
P|History book|History shelf|Shelve a history book
B|Catalog terminal|Search the catalog
P|Science atlas|Science shelf|Shelve a science atlas
P|Book label|Label printer tray|Prepare a spine label
B|Label printer|Print the spine label
P|Picture book|Children shelf|Shelve a picture book
P|Archive box|Archive cabinet|Store the archive box
B|Checkout scanner|Scan the borrowed book
P|Borrowed book|Pickup desk|Place the book at pickup"),
            new ExpandedSite("music", "MUSIC HALL", "Set up the concert", new Vector3(64,0,45), roofOrange, @"P|Microphone|Stage stand|Mount the microphone
P|Guitar|Instrument rack|Set the guitar on its rack
B|Soundboard power|Power the soundboard
P|Drumstick pair|Drum table|Prepare drumsticks
P|Music sheet|Piano stand|Place sheet music on the piano
B|Stage light switch|Aim the stage lights
P|Speaker box|Speaker mount|Place the speaker
P|Violin case|Backstage shelf|Store the violin case
B|Show cue button|Cue the performance
P|Ticket stack|Entrance counter|Prepare the ticket stack"),
            new ExpandedSite("garage", "GARAGE", "Service a vehicle", new Vector3(64,0,-45), roofBlue, @"P|Wheel|Axle mount|Fit the wheel onto the axle
P|Oil can|Engine shelf|Bring oil to the engine
B|Lift switch|Raise the vehicle lift
P|Battery|Battery bay|Install the battery
P|Wrench|Tool drawer|Return the wrench
B|Compressor control|Start the air compressor
P|Air hose|Tire valve|Attach the air hose
P|Filter|Engine housing|Fit a new engine filter
B|Diagnostics control|Run the diagnostics
P|Service card|Customer desk|File the service card"),
            new ExpandedSite("observatory", "OBSERVATORY", "Calibrate the telescope", new Vector3(39,0,-64), roofTeal, @"P|Lens|Telescope mount|Install the telescope lens
P|Star chart|Navigation table|Lay out the star chart
B|Dome motor|Open the dome
P|Focus knob|Control mount|Fit the focus knob
P|Eyepiece|Viewing tube|Install the eyepiece
B|Alignment control|Align the telescope
P|Calibration weight|Balance arm|Balance the telescope
P|Observation log|Writing desk|Open the observation log
B|Camera shutter|Capture the sky image
P|Sky image|Display board|Display the sky image"),
            new ExpandedSite("harbor", "HARBOR", "Load and sail", new Vector3(0,0,-67), roofTeal, @"P|Cargo crate|Dock scale|Weigh the cargo crate
P|Rope coil|Mooring post|Secure the mooring rope
B|Crane switch|Lower the loading crane
P|Supply barrel|Cargo pallet|Load the supply barrel
P|Map scroll|Navigation desk|Set out the sailing map
B|Harbor beacon|Turn on the harbor beacon
P|Life jacket|Safety rack|Hang the life jacket
P|Cargo manifest|Captain desk|File the cargo manifest
B|Departure horn|Sound the departure horn
P|Ship key|Wheelhouse|Deliver the ship key"),
            new ExpandedSite("art", "ART STUDIO", "Create an exhibition", new Vector3(-39,0,-64), roofRed, @"P|Blank canvas|Easel|Mount a blank canvas
P|Paint tube|Palette tray|Prepare paint on the palette
B|Studio lamp|Switch on the studio lamp
P|Brush|Painting table|Lay out a paint brush
P|Color swatch|Reference wall|Pin up the color swatch
B|Turntable control|Rotate the sculpture stand
P|Sculpture|Display pedestal|Display the sculpture
P|Frame|Framing table|Prepare the picture frame
B|Gallery lights|Light the exhibition
P|Finished painting|Gallery wall|Hang the finished painting"),
            new ExpandedSite("recycling", "RECYCLING", "Sort and process waste", new Vector3(-64,0,-45), roofGreen, @"P|Glass bottle|Glass bin|Sort the glass bottle
P|Paper bundle|Paper bin|Sort the paper bundle
B|Conveyor switch|Start the sorting conveyor
P|Metal can|Metal bin|Sort the metal can
P|Plastic bottle|Plastic bin|Sort the plastic bottle
B|Washer control|Wash the recyclable materials
P|Battery cell|Hazard bin|Separate the battery cell
P|Cardboard box|Baler shelf|Prepare cardboard for baling
B|Compactor button|Compact the sorted waste
P|Recycled bale|Shipping pallet|Move the recycled bale to shipping")
        };

        [MenuItem("Tools/VR Village/Validate Expanded Gameplay")]
        public static void ValidateExpandedGameplay()
        {
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (active.path != ScenePath)
                throw new InvalidOperationException("[VillageDraft] Open the village scene first.");
            var progress = UnityEngine.Object.FindFirstObjectByType<VillageProgressBoard>();
            if (progress == null || progress.objectives.Length != 15 ||
                progress.objectives.Any(o => o == null || o.stepsRequired != 10))
                throw new InvalidOperationException("[VillageDraft] Expected 15 objectives of 10 steps each.");
            var buttons = UnityEngine.Object.FindObjectsByType<VillagePressButton>(FindObjectsSortMode.None);
            var targets = UnityEngine.Object.FindObjectsByType<VillagePlaceTarget>(FindObjectsSortMode.None);
            var guides = UnityEngine.Object.FindObjectsByType<VillageVenueTaskGuide>(FindObjectsSortMode.None);
            var motion = UnityEngine.Object.FindObjectsByType<VillageMovingProp>(FindObjectsSortMode.None);
            var effects = UnityEngine.Object.FindObjectsByType<VillageActionEffect>(FindObjectsSortMode.None);
            if (buttons.Length + targets.Length != 150 || guides.Length != 15 || motion.Length < 11)
                throw new InvalidOperationException($"[VillageDraft] Gameplay count mismatch: {buttons.Length} buttons + {targets.Length} sockets, {guides.Length} guides, {motion.Length} scripted props.");
            if (effects.Length != 45 || effects.Any(x => x.sourceButton == null || x.target == null))
                throw new InvalidOperationException("[VillageDraft] Expected 45 persistent scripted button effects.");
            foreach (var objective in progress.objectives)
            {
                var id = objective.id;
                var matchingButtons = buttons.Where(x => x.objectiveId == id).ToArray();
                var matchingTargets = targets.Where(x => x.objectiveId == id).ToArray();
                if (matchingButtons.Length + matchingTargets.Length != 10 ||
                    matchingButtons.Any(x => x.progressBoard != progress || x.GetComponent<XRSimpleInteractable>() == null) ||
                    matchingTargets.Any(x => x.progressBoard != progress || x.requiredObject == null))
                    throw new InvalidOperationException("[VillageDraft] Missing or unwired actions at " + id);
                var guide = guides.SingleOrDefault(x => x.objectiveId == id);
                if (guide == null || guide.progressBoard != progress || guide.promptText == null ||
                    guide.instructions == null || guide.instructions.Length != 10 ||
                    guide.pressSteps == null || guide.pressSteps.Length != 10 ||
                    matchingButtons.Any(x => x.taskGuide != guide) ||
                    matchingTargets.Any(x => x.taskGuide != guide))
                    throw new InvalidOperationException("[VillageDraft] Missing or unwired task UI at " + id);
            }
            var marketCue = UnityEngine.Object.FindFirstObjectByType<VillageFirstMarketCue>();
            if (marketCue == null || marketCue.progressBoard != progress ||
                marketCue.sourceLight == null || marketCue.targetLight == null ||
                marketCue.sourceMarker == null || marketCue.targetMarker == null ||
                marketCue.sourceMarker.GetComponentInChildren<MeshRenderer>(true) == null ||
                marketCue.targetMarker.GetComponentInChildren<MeshRenderer>(true) == null)
                throw new InvalidOperationException("[VillageDraft] First Market pickup guidance is missing.");
            var flow = UnityEngine.Object.FindFirstObjectByType<VillageGameFlow>();
            if (flow == null || flow.progressBoard != progress || flow.replayButton == null || flow.statusText == null)
                throw new InvalidOperationException("[VillageDraft] Win/replay UI is not wired.");
            var festival = UnityEngine.Object.FindFirstObjectByType<VillageFestivalSquare>();
            if (festival == null || festival.progressBoard != progress || festival.objectiveIds.Length != 15 ||
                festival.contributions.Length != 15 || festival.contributions.Any(x => x == null))
                throw new InvalidOperationException("[VillageDraft] Festival contributions are not wired.");
            var experiences = UnityEngine.Object.FindObjectsByType<VillageVenueExperience>(FindObjectsSortMode.None);
            if (experiences.Length != 15 || experiences.Any(x => x.progressBoard != progress ||
                x.focus == null || x.secondary == null || x.outcomeText == null || x.milestones.Length != 10))
                throw new InvalidOperationException("[VillageDraft] Expected 15 themed venue mechanisms.");
            var menus = UnityEngine.Object.FindObjectsByType<VillageControllerMenu>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            var simulators = UnityEngine.Object.FindObjectsByType<XRInteractionSimulator>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            var eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            var rightController = menus.Length == 1
                ? menus[0].transform.root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(x => x.name == "Right Controller") : null;
            var rightRay = rightController != null
                ? rightController.GetComponentInChildren<NearFarInteractor>(true) : null;
            if (menus.Length != 1 || menus[0].VenueButtonCount != 15 ||
                menus[0].venues.Length != 15 || menus[0].venues.Any(x => x == null) ||
                menus[0].GetComponentInChildren<Canvas>(true)?.renderMode != RenderMode.WorldSpace ||
                menus[0].GetComponentInChildren<TrackedDeviceGraphicRaycaster>(true) == null ||
                menus[0].transform.parent == null || menus[0].transform.parent.name != "Left Controller")
                throw new InvalidOperationException("[VillageDraft] The fifteen-place left controller XR menu is missing.");
            if (simulators.Length != 1 || eventSystems.Length != 1 ||
                eventSystems[0].GetComponent<XRUIInputModule>() == null ||
                rightRay == null || !rightRay.enableUIInteraction)
                throw new InvalidOperationException("[VillageDraft] XR Simulator, input module, or ray interactor is missing.");
            if (GameObject.Find("06 Expanded village") == null)
                throw new InvalidOperationException("[VillageDraft] Expanded village root is missing.");
            Debug.Log("[VillageDraft] Expanded validation passed: 15 places, 150 wired actions, 15 prompts, win/replay and scripted motion.");
        }

        [MenuItem("Tools/VR Village/Exercise Expanded Objectives (Play Mode)")]
        public static void ExerciseExpandedObjectives()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("[VillageDraft] Enter Play Mode first.");
            var progress = UnityEngine.Object.FindFirstObjectByType<VillageProgressBoard>();
            if (progress == null || progress.objectives.Length != 15)
                throw new InvalidOperationException("[VillageDraft] Expanded progress board is missing.");
            var marketGuide = UnityEngine.Object.FindObjectsByType<VillageVenueTaskGuide>(FindObjectsSortMode.None)
                .SingleOrDefault(x => x.objectiveId == "market");
            var firstMarketCue = UnityEngine.Object.FindFirstObjectByType<VillageFirstMarketCue>();
            if (marketGuide == null || firstMarketCue == null ||
                !firstMarketCue.sourceLight.enabled || !firstMarketCue.targetLight.enabled ||
                !firstMarketCue.sourceMarker.activeSelf || !firstMarketCue.targetMarker.activeSelf)
                throw new InvalidOperationException("[VillageDraft] Market first-task cues did not start.");
            marketGuide.ShowRetryHint();
            if (!marketGuide.promptText.text.Contains("TRY THIS NEXT"))
                throw new InvalidOperationException("[VillageDraft] Rejected-action guidance did not appear.");
            var buttons = UnityEngine.Object.FindObjectsByType<VillagePressButton>(FindObjectsSortMode.None);
            var targets = UnityEngine.Object.FindObjectsByType<VillagePlaceTarget>(FindObjectsSortMode.None);
            foreach (var objective in progress.objectives)
            {
                if (progress.GetCompletedSteps(objective.id) != 0)
                    throw new InvalidOperationException("[VillageDraft] Start fresh Play Mode before exercising objectives.");
                var actions = buttons.Where(x => x.objectiveId == objective.id)
                    .Select(x => new { step = x.requiredCurrentStep, run = (Func<bool>)x.Press })
                    .Concat(targets.Where(x => x.objectiveId == objective.id)
                        .Select(x => new { step = x.requiredCurrentStep,
                            run = (Func<bool>)(() => x.TryPlace(x.requiredObject)) }))
                    .OrderBy(x => x.step)
                    .ToArray();
                if (actions.Length != 10)
                    throw new InvalidOperationException("[VillageDraft] Wrong action count at " + objective.id);
                foreach (var action in actions)
                {
                    if (!action.run())
                        throw new InvalidOperationException("[VillageDraft] Action failed at " + objective.id + " step " + action.step);
                    if (objective.id == "market" && action.step == 0 &&
                        (firstMarketCue.sourceLight.enabled || firstMarketCue.targetLight.enabled ||
                         firstMarketCue.sourceMarker.activeSelf || firstMarketCue.targetMarker.activeSelf ||
                         !marketGuide.promptText.text.Contains("DONE")))
                        throw new InvalidOperationException("[VillageDraft] First Market success did not advance guidance.");
                }
                if (progress.GetCompletedSteps(objective.id) != 10)
                    throw new InvalidOperationException("[VillageDraft] Objective did not complete: " + objective.id);
            }
            if (!progress.IsVillageComplete)
                throw new InvalidOperationException("[VillageDraft] Victory flow did not complete.");
            var effects = UnityEngine.Object.FindObjectsByType<VillageActionEffect>(FindObjectsSortMode.None);
            if (effects.Length != 45 || effects.Any(x => !x.HasPlayed))
                throw new InvalidOperationException("[VillageDraft] A button action did not trigger its scripted effect.");
            var flow = UnityEngine.Object.FindFirstObjectByType<VillageGameFlow>();
            if (flow == null || flow.statusText == null || !flow.statusText.text.Contains("VILLAGE COMPLETE"))
                throw new InvalidOperationException("[VillageDraft] Completion UI did not show victory.");
            var festival = UnityEngine.Object.FindFirstObjectByType<VillageFestivalSquare>();
            if (festival == null || festival.contributions.Any(x => !x.activeSelf))
                throw new InvalidOperationException("[VillageDraft] Festival square did not reveal all contributions.");
            var experiences = UnityEngine.Object.FindObjectsByType<VillageVenueExperience>(FindObjectsSortMode.None);
            if (experiences.Length != 15 || experiences.Any(x => x.LastDisplayedStep != 10))
                throw new InvalidOperationException("[VillageDraft] A venue mechanism missed progress events.");
            var menu = UnityEngine.Object.FindFirstObjectByType<VillageControllerMenu>();
            if (menu == null || menu.VenueButtonCount != 15 || menu.venues.Length != 15)
                throw new InvalidOperationException("[VillageDraft] Controller menu is unavailable in Play Mode.");
            for (int i = 0; i < menu.buttons.Length; i++)
            {
                var venue = menu.venues[i];
                if (menu.buttons[i] == null || venue == null || menu.venueIds[i] != venue.objectiveId)
                    throw new InvalidOperationException("[VillageDraft] Controller menu mapping is wrong at button " + i);
                var focusBefore = venue.focus.localPosition;
                var focusRotationBefore = venue.focus.localRotation;
                var secondaryBefore = venue.secondary.localPosition;
                var secondaryRotationBefore = venue.secondary.localRotation;
                menu.buttons[i].onClick.Invoke();
                if (!venue.RemoteCueActive ||
                    (Vector3.Distance(focusBefore, venue.focus.localPosition) < .001f &&
                     Quaternion.Angle(focusRotationBefore, venue.focus.localRotation) < .1f &&
                     Vector3.Distance(secondaryBefore, venue.secondary.localPosition) < .001f &&
                     Quaternion.Angle(secondaryRotationBefore, venue.secondary.localRotation) < .1f))
                    throw new InvalidOperationException("[VillageDraft] Controller cue did not move " + venue.objectiveId);
                menu.buttons[i].onClick.Invoke();
                if (venue.RemoteCueActive ||
                    Vector3.Distance(focusBefore, venue.focus.localPosition) > .001f ||
                    Quaternion.Angle(focusRotationBefore, venue.focus.localRotation) > .1f ||
                    Vector3.Distance(secondaryBefore, venue.secondary.localPosition) > .001f ||
                    Quaternion.Angle(secondaryRotationBefore, venue.secondary.localRotation) > .1f)
                    throw new InvalidOperationException("[VillageDraft] Controller cue did not reset " + venue.objectiveId);
            }
            Debug.Log("[VillageDraft] Play check passed: 150 actions, 15 objectives, 15 controller UI cues, and festival victory. Use REPLAY to reset.");
        }
    }
}
