using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils;

namespace VillageDraft.Editor
{
    /// <summary>Builds a replaceable blockout. All generated scene objects live under one root.</summary>
    public static partial class VillageDraftBuilder
    {
        public const string ScenePath = "Assets/VillageDraft/Scenes/VillageDraft.unity";
        private const string BasicScenePath = "Assets/Scenes/BasicScene.unity";
        private const string MaterialFolder = "Assets/VillageDraft/Materials";
        private const string RootName = "_VILLAGE_DRAFT";

        private static Transform root;
        private static Font signFont;
        private static Material worldTextMaterial;
        private static Material grass, road, paving, timber, cream, roofRed, roofBlue, roofGreen,
            roofOrange, roofTeal, dark, white, water, leaf, trunk, gold, tomato, glass;

        [MenuItem("Tools/VR Village/Build Draft Scene")]
        public static void Build()
        {
            EnsureFolder("Assets", "VillageDraft");
            EnsureFolder("Assets/VillageDraft", "Scenes");
            EnsureFolder("Assets/VillageDraft", "Materials");

            if (!File.Exists(ScenePath))
            {
                var basic = EditorSceneManager.OpenScene(BasicScenePath, OpenSceneMode.Single);
                if (!EditorSceneManager.SaveScene(basic, ScenePath, true))
                    throw new InvalidOperationException("Could not copy the template VR scene.");
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var previous = GameObject.Find(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous);

            CreateMaterials();
            var rootObject = new GameObject(RootName);
            root = rootObject.transform;
            rootObject.transform.position = Vector3.zero;
            SetupTemplate(scene);
            BuildLandscape();
            BuildPlaza();
            BuildVenues();
            BuildGameplay();
            BuildExpandedGameplay();
            ExpandOriginalGameplay();
            BuildModels();
            ReplaceOriginalVenueVisuals();
            BuildWorldRedesign();
            BuildLighting();
            BuildVisuals();
            BuildAudio();
            BuildCelebration();
            BuildOverviewCamera();
            MarkStaticDecor();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save the village scene.");
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", false)
            };
            AssetDatabase.SaveAssets();
            Debug.Log($"[VillageDraft] Built and saved {ScenePath}. All blockout objects are under {RootName}.");
        }

        private static void SetupTemplate(Scene scene)
        {
            var rig = UnityEngine.Object.FindFirstObjectByType<XROrigin>();
            if (rig == null) throw new InvalidOperationException("BasicScene has no XR Origin. Keep the VR template rig.");
            rig.transform.SetPositionAndRotation(new Vector3(0, 0, -30), Quaternion.identity);

            var ground = GameObject.Find("Plane") ?? GameObject.Find("Teleportable ground (template)");
            if (ground == null) throw new InvalidOperationException("BasicScene teleportable Plane is missing.");
            ground.name = "Teleportable ground (template)";
            ground.transform.position = new Vector3(0, -0.08f, 0);
            ground.transform.localScale = new Vector3(22, 1, 22); // Teleport coverage for the full fifteen-place village.
            ground.GetComponent<Renderer>().enabled = false;
            // The Blender ground supplies the art; keep the template's TeleportationArea and collider.
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.69f, 0.77f, 0.84f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.73f, 0.85f, 0.91f);
            RenderSettings.fogDensity = 0.003f;
        }

        private static void BuildLandscape()
        {
            Group("01 Landscape", root); // Visible terrain is exported from Blender by BuildWorldRedesign.
        }

        private static void BuildRoads()
        {
            var roads = Group("02 Roads and paths", root);
            Road("Arrival lane", roads, new Vector3(0, 0, -39), new Vector3(0, 0, -3), 5.5f);
            Road("North promenade", roads, new Vector3(0, 0, 2), new Vector3(0, 0, 25), 5.5f);
            Road("West lane", roads, new Vector3(-31, 0, 0), new Vector3(-3, 0, 0), 4.5f);
            Road("East lane", roads, new Vector3(3, 0, 0), new Vector3(31, 0, 0), 4.5f);
            Road("Market path", roads, new Vector3(-13, 0, 0), new Vector3(-19, 0, 12), 3.5f);
            Road("Cafe path", roads, new Vector3(13, 0, 0), new Vector3(19, 0, 12), 3.5f);
            Road("Workshop path", roads, new Vector3(-16, 0, 0), new Vector3(-22, 0, -12), 3.5f);
            Road("Greenhouse path", roads, new Vector3(16, 0, 0), new Vector3(22, 0, -12), 3.5f);
            for (int z = -36; z <= 24; z += 6)
                Flat("North road marker", roads, new Vector3(0, 0.04f, z), new Vector3(.13f, .01f, 2.2f), gold);
            for (int x = -27; x <= 27; x += 6)
                Flat("Cross road marker", roads, new Vector3(x, 0.04f, 0), new Vector3(2.2f, .01f, .13f), gold);
        }

        private static void BuildPlaza()
        {
            var plaza = Group("03 Welcome plaza", root);
            // Blender civic meshes carry the arrival paving, fountain, lanterns and board frame.
            Label("WELCOME TO THE VILLAGE FESTIVAL", plaza, new Vector3(-7.8f, 2.65f, -15.13f), 0, .145f, Color.white);
            Label("15 PLACES   •   150 ACTIONS\nEXPLORE, HELP, CELEBRATE", plaza,
                new Vector3(-7.8f, 1.75f, -15.13f), 0, .12f, Color.white);
            Label("Move • Grab • Use your controller menu", plaza,
                new Vector3(-7.8f, 1.05f, -15.13f), 0, .09f, new Color(1, .83f, .43f));
        }

        private static void BuildVenues()
        {
            var venues = Group("04 Five activity places", root);
            var market = Venue("01 MARKET - Checkout challenge", venues, new Vector3(-20, 0, 13), roofRed, "MARKET", "Collect goods + checkout");
            BuildMarket(market);
            var cafe = Venue("02 CAFE - Barista challenge", venues, new Vector3(20, 0, 13), roofOrange, "CAFE", "Brew + serve a drink");
            BuildCafe(cafe);
            var workshop = Venue("03 WORKSHOP - Repair challenge", venues, new Vector3(-23, 0, -13), roofBlue, "WORKSHOP", "Power the windmill");
            BuildWorkshop(workshop);
            var greenhouse = Venue("04 GREENHOUSE - Planting challenge", venues, new Vector3(23, 0, -13), roofGreen, "GREENHOUSE", "Plant + water a seed");
            BuildGreenhouse(greenhouse);
            var pool = Venue("05 POOL - Ring toss challenge", venues, new Vector3(0, 0, 29), roofTeal, "POOL", "Toss a ring to the target");
            BuildPool(pool);
        }

        private static Transform Venue(string name, Transform parent, Vector3 at, Material accent, string title, string subtitle)
        {
            var t = Group(name, parent);
            t.position = at;
            t.rotation = Quaternion.LookRotation(new Vector3(at.x, 0, at.z).normalized);
            Local("Floor", PrimitiveType.Cube, t, new Vector3(0, .045f, 0), new Vector3(12, .09f, 10), paving, false);
            Local("Back wall", PrimitiveType.Cube, t, new Vector3(0, 2.15f, 4.9f), new Vector3(12, 4.3f, .32f), cream);
            Local("Left wall", PrimitiveType.Cube, t, new Vector3(-5.85f, 2.15f, 0), new Vector3(.3f, 4.3f, 10), cream);
            Local("Right wall", PrimitiveType.Cube, t, new Vector3(5.85f, 2.15f, 0), new Vector3(.3f, 4.3f, 10), cream);
            Local("Front left post", PrimitiveType.Cube, t, new Vector3(-5.75f, 2.2f, -4.8f), new Vector3(.42f, 4.4f, .42f), timber);
            Local("Front right post", PrimitiveType.Cube, t, new Vector3(5.75f, 2.2f, -4.8f), new Vector3(.42f, 4.4f, .42f), timber);
            Local("Roof", PrimitiveType.Cube, t, new Vector3(0, 4.5f, 0), new Vector3(12.7f, .44f, 10.7f), accent);
            Local("Front fascia", PrimitiveType.Cube, t, new Vector3(0, 4.55f, -5.35f), new Vector3(12.7f, 1.0f, .25f), accent);
            Local("Sign backing", PrimitiveType.Cube, t, new Vector3(0, 3.45f, -4.99f), new Vector3(7.6f, 1.1f, .18f), dark);
            LocalLabel(title, t, new Vector3(0, 3.47f, -5.13f), 0, .21f, Color.white);
            LocalLabel(subtitle, t, new Vector3(0, 2.72f, -5.04f), 0, .105f, dark.color);
            return t;
        }

        private static void BuildMarket(Transform t)
        {
            Local("Checkout counter", PrimitiveType.Cube, t, new Vector3(0, .75f, 1.5f), new Vector3(6.7f, 1.5f, 1.1f), timber);
            Local("Register", PrimitiveType.Cube, t, new Vector3(2.2f, 1.62f, 1.5f), new Vector3(1.1f, .3f, .85f), dark);
            Local("Basket", PrimitiveType.Cube, t, new Vector3(-1.5f, 1.62f, 1.45f), new Vector3(1.15f, .32f, .8f), roofRed);
            for (int side = -1; side <= 1; side += 2)
            {
                Local("Shelf", PrimitiveType.Cube, t, new Vector3(side * 4.7f, 1.25f, 1.6f), new Vector3(.7f, 2.5f, 4.7f), timber);
                for (int row = 0; row < 3; row++)
                    for (int col = 0; col < 3; col++)
                        Local("Colorful goods", PrimitiveType.Cube, t,
                            new Vector3(side * 4.15f, .65f + row * .7f, -.15f + col * 1.5f),
                            new Vector3(.55f, .5f, .65f), col % 2 == 0 ? tomato : gold);
            }
            LocalLabel("Pick up goods, place in basket, checkout", t, new Vector3(0, 2.25f, 4.62f), 0, .12f, dark.color);
        }

        private static void BuildCafe(Transform t)
        {
            Local("Bar counter", PrimitiveType.Cube, t, new Vector3(0, .8f, 1.8f), new Vector3(8, 1.6f, 1.2f), timber);
            Local("Coffee machine", PrimitiveType.Cube, t, new Vector3(-2.8f, 1.9f, 2.0f), new Vector3(1.5f, .8f, .8f), dark);
            Local("Brew button", PrimitiveType.Cylinder, t, new Vector3(-2.8f, 2.35f, 1.65f), new Vector3(.22f, .08f, .22f), gold);
            for (int i = -1; i <= 1; i += 2)
            {
                Local("Cafe table", PrimitiveType.Cylinder, t, new Vector3(i * 2.8f, .74f, -2.1f), new Vector3(1.25f, .06f, 1.25f), timber);
                Local("Table leg", PrimitiveType.Cylinder, t, new Vector3(i * 2.8f, .36f, -2.1f), new Vector3(.12f, .36f, .12f), dark);
                Local("Stool", PrimitiveType.Cylinder, t, new Vector3(i * 2.8f + 1.6f, .45f, -2.1f), new Vector3(.42f, .45f, .42f), roofOrange);
            }
            LocalLabel("Brew, grab a cup, and serve a table", t, new Vector3(0, 2.55f, 4.62f), 0, .12f, dark.color);
        }

        private static void BuildWorkshop(Transform t)
        {
            Local("Tool bench", PrimitiveType.Cube, t, new Vector3(0, .85f, 2.3f), new Vector3(8.4f, 1.7f, 1.25f), timber);
            Local("Machine base", PrimitiveType.Cube, t, new Vector3(0, 1.55f, 3.1f), new Vector3(2.7f, 1.5f, 1.1f), dark);
            for (int i = -1; i <= 1; i++)
            {
                Local("Power switch", PrimitiveType.Cube, t, new Vector3(i * 2.2f, 1.88f, 1.55f), new Vector3(.7f, .32f, .32f), i == 0 ? gold : roofRed);
                Local("Switch indicator", PrimitiveType.Sphere, t, new Vector3(i * 2.2f, 2.15f, 1.55f), new Vector3(.22f, .22f, .22f), roofGreen, false);
            }
            Local("Windmill mast", PrimitiveType.Cylinder, t, new Vector3(0, 2.0f, -2.3f), new Vector3(.28f, 2.0f, .28f), timber);
            var rotor = Group("Windmill rotor - scripted motion", t);
            rotor.localPosition = new Vector3(0, 3.5f, -2.65f);
            for (int i = 0; i < 4; i++)
            {
                var blade = Local("Blade", PrimitiveType.Cube, rotor,
                    Quaternion.Euler(0, 0, i * 90) * new Vector3(0, 1.05f, 0),
                    new Vector3(.3f, 1.7f, .12f), cream, false);
                blade.localRotation = Quaternion.Euler(0, 0, i * 90);
            }
            LocalLabel("Press 3 switches to repair the windmill", t, new Vector3(0, 2.65f, 4.62f), 0, .12f, dark.color);
        }

        private static void BuildGreenhouse(Transform t)
        {
            for (int x = -1; x <= 1; x++)
            {
                Local("Raised planter", PrimitiveType.Cube, t, new Vector3(x * 3.15f, .42f, 1.1f), new Vector3(2.5f, .82f, 4.5f), timber);
                Local("Soil", PrimitiveType.Cube, t, new Vector3(x * 3.15f, .87f, 1.1f), new Vector3(2.25f, .09f, 4.25f), trunk, false);
                for (int z = -1; z <= 1; z++)
                {
                    Local("Sprout", PrimitiveType.Cylinder, t, new Vector3(x * 3.15f, 1.1f, z * 1.1f + 1.1f), new Vector3(.06f, .23f, .06f), leaf, false);
                    Local("Leaf", PrimitiveType.Sphere, t, new Vector3(x * 3.15f, 1.37f, z * 1.1f + 1.1f), new Vector3(.55f, .3f, .35f), leaf, false);
                }
            }
            Local("Water tank", PrimitiveType.Cylinder, t, new Vector3(4.4f, 1.0f, -2.8f), new Vector3(.75f, 1.0f, .75f), water);
            Local("Sprinkler pipe", PrimitiveType.Cylinder, t, new Vector3(0, 3.8f, 1.3f), new Vector3(.07f, 4.5f, .07f), dark, false).localRotation = Quaternion.Euler(90, 0, 0);
            LocalLabel("Plant a seed and turn on water", t, new Vector3(0, 2.55f, 4.62f), 0, .12f, dark.color);
        }

        private static void BuildPool(Transform t)
        {
            // The pool is intentionally open-air, so it reads as a different place from the four shops.
            UnityEngine.Object.DestroyImmediate(t.Find("Roof").gameObject);
            UnityEngine.Object.DestroyImmediate(t.Find("Back wall").gameObject);
            UnityEngine.Object.DestroyImmediate(t.Find("Left wall").gameObject);
            UnityEngine.Object.DestroyImmediate(t.Find("Right wall").gameObject);
            Local("Rear left pergola post", PrimitiveType.Cube, t, new Vector3(-5.75f, 2.2f, 4.8f),
                new Vector3(.42f, 4.4f, .42f), timber);
            Local("Rear right pergola post", PrimitiveType.Cube, t, new Vector3(5.75f, 2.2f, 4.8f),
                new Vector3(.42f, 4.4f, .42f), timber);
            for (int i = -2; i <= 2; i++)
                Local("Open pergola beam", PrimitiveType.Cube, t, new Vector3(i * 2.2f, 4.48f, 0),
                    new Vector3(.24f, .28f, 10.2f), roofTeal, false);
            Local("Pool border", PrimitiveType.Cube, t, new Vector3(0, .18f, .2f), new Vector3(8.7f, .36f, 6.9f), white);
            Local("Pool water", PrimitiveType.Cube, t, new Vector3(0, .38f, .2f), new Vector3(7.8f, .04f, 6), water, false);
            for (int i = -1; i <= 1; i++)
            {
                Local("Lane stripe", PrimitiveType.Cube, t, new Vector3(i * 2.2f, .41f, .2f), new Vector3(.08f, .02f, 5.3f), cream, false);
                Local("Floating target", PrimitiveType.Cylinder, t, new Vector3(i * 2.2f, .5f, 2.2f), new Vector3(.62f, .08f, .62f), gold, false);
            }
            Local("Lifeguard seat", PrimitiveType.Cube, t, new Vector3(4.7f, 1.25f, -2.6f), new Vector3(1.0f, .18f, 1), roofRed);
            Local("Lifeguard post", PrimitiveType.Cylinder, t, new Vector3(4.7f, .62f, -2.6f), new Vector3(.1f, .62f, .1f), dark);
            LocalLabel("Grab a ring and toss it onto the target", t, new Vector3(0, 2.65f, 4.62f), 0, .12f, dark.color);
        }

        private static void BuildVegetation()
        {
            var plants = Group("05 Trees and village details", root);
            var positions = new[]
            {
                new Vector3(-37,0,-32), new Vector3(-37,0,-18), new Vector3(-36,0,6), new Vector3(-34,0,26),
                new Vector3(-12,0,33), new Vector3(13,0,34), new Vector3(36,0,27), new Vector3(37,0,8),
                new Vector3(37,0,-19), new Vector3(36,0,-34), new Vector3(14,0,-35), new Vector3(-13,0,-35)
            };
            foreach (var at in positions) Tree(plants, at);
            for (int x = -88; x <= 88; x += 8)
            {
                if (Mathf.Abs(x) > 8) FencePost(plants, new Vector3(x, 0, -96));
                FencePost(plants, new Vector3(x, 0, 96));
            }
            for (int z = -88; z <= 88; z += 8)
            {
                FencePost(plants, new Vector3(-96, 0, z));
                FencePost(plants, new Vector3(96, 0, z));
            }
            for (int level = 0; level < 2; level++)
            {
                float y = .45f + level * .55f;
                Primitive("North fence rail", PrimitiveType.Cube, plants,
                    new Vector3(0, y, 96), new Vector3(192, .14f, .16f), white);
                Primitive("South fence rail left", PrimitiveType.Cube, plants,
                    new Vector3(-52, y, -96), new Vector3(88, .14f, .16f), white);
                Primitive("South fence rail right", PrimitiveType.Cube, plants,
                    new Vector3(52, y, -96), new Vector3(88, .14f, .16f), white);
                Primitive("West fence rail", PrimitiveType.Cube, plants,
                    new Vector3(-96, y, 0), new Vector3(.16f, .14f, 192), white);
                Primitive("East fence rail", PrimitiveType.Cube, plants,
                    new Vector3(96, y, 0), new Vector3(.16f, .14f, 192), white);
            }
            foreach (var at in new[]
            {
                new Vector3(-89,0,-84), new Vector3(-90,0,-9), new Vector3(-89,0,83),
                new Vector3(-8,0,89), new Vector3(8,0,89), new Vector3(89,0,83),
                new Vector3(90,0,9), new Vector3(89,0,-83), new Vector3(8,0,-89),
                new Vector3(-8,0,-89)
            }) Tree(plants, at);
        }

        private static void BuildLighting()
        {
            var sun = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                .FirstOrDefault(l => l.type == LightType.Directional);
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(42, -35, 0);
                sun.intensity = 1.25f;
                sun.color = new Color(1, .94f, .82f);
            }
        }

        private static void BuildOverviewCamera()
        {
            var cameraObject = new GameObject("Editor overview camera (disabled in play)");
            cameraObject.transform.SetParent(root);
            cameraObject.transform.position = new Vector3(0, 125, -100);
            cameraObject.transform.LookAt(new Vector3(0, 0, 0));
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 82;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.enabled = false;
            camera.tag = "Untagged";

            var showcaseObject = new GameObject("Editor showcase camera (disabled in play)");
            showcaseObject.transform.SetParent(root);
            showcaseObject.transform.position = new Vector3(-113, 72, -102);
            showcaseObject.transform.LookAt(new Vector3(0, 0, 4));
            var showcase = showcaseObject.AddComponent<Camera>();
            showcase.fieldOfView = 68;
            showcase.clearFlags = CameraClearFlags.Skybox;
            showcase.enabled = false;
            showcase.tag = "Untagged";
        }

        private static void Road(string name, Transform parent, Vector3 a, Vector3 b, float width)
        {
            var delta = b - a;
            var p = Flat(name, parent, (a + b) * .5f + Vector3.up * .015f,
                new Vector3(width, .035f, delta.magnitude), road);
            p.rotation = Quaternion.LookRotation(delta.normalized);
        }

        private static void Tree(Transform parent, Vector3 p)
        {
            var tree = Group("Tree", parent);
            tree.position = p;
            Local("Trunk", PrimitiveType.Cylinder, tree, new Vector3(0, 1.3f, 0), new Vector3(.25f, 1.3f, .25f), trunk);
            Local("Canopy lower", PrimitiveType.Sphere, tree, new Vector3(0, 3.3f, 0), new Vector3(2.8f, 2.8f, 2.8f), leaf, false);
            Local("Canopy upper", PrimitiveType.Sphere, tree, new Vector3(.4f, 4.4f, .1f), new Vector3(2.15f, 2.1f, 2.15f), roofGreen, false);
        }

        private static void Lamp(Transform parent, Vector3 p)
        {
            var lamp = Group("Path lamp", parent);
            lamp.position = p;
            Local("Post", PrimitiveType.Cylinder, lamp, new Vector3(0, 1.6f, 0), new Vector3(.09f, 1.6f, .09f), dark);
            Local("Lantern", PrimitiveType.Sphere, lamp, new Vector3(0, 3.3f, 0), new Vector3(.55f, .55f, .55f), gold, false);
        }

        private static void FencePost(Transform parent, Vector3 p)
        {
            Primitive("Boundary fence post", PrimitiveType.Cube, parent,
                p + Vector3.up * .6f, new Vector3(.13f, 1.2f, .13f), white);
        }

        private static Transform Flat(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            return Primitive(name, PrimitiveType.Cube, parent, pos, scale, mat, false).transform;
        }

        private static GameObject Primitive(string name, PrimitiveType type, Transform parent,
            Vector3 pos, Vector3 scale, Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider && go.TryGetComponent<Collider>(out var existing)) UnityEngine.Object.DestroyImmediate(existing);
            return go;
        }

        private static Transform Local(string name, PrimitiveType type, Transform parent,
            Vector3 pos, Vector3 scale, Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider && go.TryGetComponent<Collider>(out var existing)) UnityEngine.Object.DestroyImmediate(existing);
            return go.transform;
        }

        private static Transform Group(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static void Label(string text, Transform parent, Vector3 pos, float yaw, float size, Color color)
        {
            var t = Group("Text - " + text.Split('\n')[0], parent);
            t.position = pos;
            t.rotation = Quaternion.Euler(0, yaw, 0);
            AddText(t.gameObject, text, size, color);
        }

        private static void LocalLabel(string text, Transform parent, Vector3 pos, float yaw, float size, Color color)
        {
            var t = Group("Text - " + text, parent);
            t.localPosition = pos;
            t.localRotation = Quaternion.Euler(0, yaw, 0);
            AddText(t.gameObject, text, size, color);
        }

        private static void AddText(GameObject go, string text, float size, Color color)
        {
            if (signFont == null)
                signFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/VRTemplateAssets/Fonts/Inter/Inter-Regular.ttf");
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.font = signFont;
            mesh.fontSize = 64;
            mesh.characterSize = size * .4f;
            mesh.color = color;
            if (signFont != null)
            {
                if (worldTextMaterial == null)
                {
                    const string path = MaterialFolder + "/World text.mat";
                    worldTextMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
                    var shader = Shader.Find("VillageDraft/WorldText");
                    if (shader == null) throw new InvalidOperationException("VillageDraft/WorldText shader is missing.");
                    if (worldTextMaterial == null)
                    {
                        worldTextMaterial = new Material(shader) { name = "World text" };
                        AssetDatabase.CreateAsset(worldTextMaterial, path);
                    }
                    worldTextMaterial.shader = shader;
                    worldTextMaterial.SetTexture("_MainTex", signFont.material.mainTexture);
                    EditorUtility.SetDirty(worldTextMaterial);
                }
                go.GetComponent<Renderer>().sharedMaterial = worldTextMaterial;
            }
        }

        private static void CreateMaterials()
        {
            grass = Mat("Grass", new Color(.37f, .59f, .31f));
            road = Mat("Slate road", new Color(.22f, .30f, .35f));
            paving = Mat("Warm paving", new Color(.72f, .69f, .59f));
            timber = Mat("Timber", new Color(.49f, .30f, .19f));
            cream = Mat("Cream plaster", new Color(.90f, .84f, .69f));
            roofRed = Mat("Coral red", new Color(.83f, .27f, .23f));
            roofBlue = Mat("Workshop blue", new Color(.22f, .48f, .71f));
            roofGreen = Mat("Garden green", new Color(.27f, .56f, .40f));
            roofOrange = Mat("Cafe orange", new Color(.94f, .59f, .25f));
            roofTeal = Mat("Pool teal", new Color(.14f, .62f, .66f));
            dark = Mat("Ink", new Color(.10f, .14f, .19f));
            white = Mat("White trim", new Color(.93f, .94f, .89f));
            water = Mat("Water blue", new Color(.16f, .61f, .87f));
            leaf = Mat("Leaf green", new Color(.42f, .70f, .31f));
            trunk = Mat("Soil and bark", new Color(.31f, .23f, .17f));
            gold = Mat("Sun yellow", new Color(1.00f, .78f, .27f));
            tomato = Mat("Produce red", new Color(.87f, .22f, .20f));
            glass = Mat("Glass hint", new Color(.61f, .84f, .81f));
        }

        private static Material Mat(string name, Color color)
        {
            var path = $"{MaterialFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void EnsureFolder(string parent, string name)
        {
            var path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
