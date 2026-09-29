using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace VillageDraft.Editor
{
    public static partial class VillageDraftBuilder
    {
        private struct DistrictSite
        {
            public string prefix, extension;
            public Vector2 position;
            public DistrictSite(string prefix, string extension, float x, float z)
            {
                this.prefix = prefix; this.extension = extension; position = new Vector2(x, z);
            }
        }

        // Venue rotations face the civic spine; their local -Z is the entry and their
        // local -X side yard remains clear for each activity set.
        private static readonly DistrictSite[] DistrictSites =
        {
            new DistrictSite("01 MARKET", "MARKET", -29, 18),
            new DistrictSite("02 CAFE", "CAFE", 17, 14),
            new DistrictSite("03 WORKSHOP", "WORKSHOP", -47, -20),
            new DistrictSite("04 GREENHOUSE", "GREENHOUSE", -20, -12),
            new DistrictSite("05 POOL", "POOL", 25, -38),
            new DistrictSite("06 BAKERY", null, -57, 34),
            new DistrictSite("07 POST OFFICE", null, -17, 54),
            new DistrictSite("08 CLINIC", null, 19, 55),
            new DistrictSite("09 LIBRARY", null, 48, 36),
            new DistrictSite("10 MUSIC HALL", null, -46, -54),
            new DistrictSite("11 GARAGE", null, 66, -12),
            new DistrictSite("12 OBSERVATORY", null, 69, 65),
            new DistrictSite("13 HARBOR", null, 53, -56),
            new DistrictSite("14 ART STUDIO", null, -14, -54),
            new DistrictSite("15 RECYCLING", null, -75, -32)
        };
        private static readonly Dictionary<string, Transform> DistrictVenues =
            new Dictionary<string, Transform>();
        private const string FestivalKit = "Assets/VillageDraft/Models/EnvironmentFestival/FBX/";
        private const string FoliageKit = "Assets/VillageDraft/Models/FBX/environment/";

        private static void BuildWorldRedesign()
        {
            DestroyChild(root, "02 Roads and paths");
            DestroyChild(root, "06 Expanded village/01 Outer ring promenade and radial paths");
            DestroyChild(root, "05 Trees and village details");
            DestroyChild(root, "01 Landscape/Village boundary path");
            DestroyChild(root, "01 Landscape/Village green");
            DestroyChild(root, "01 Landscape/Distant meadow (visual only)");

            MoveDistrictVenues();
            var world = Group("07 Blender festival landscape, routes and landmarks", root);
            PlaceFestivalKit(world);
            AddInvisibleDeckTeleportColliders(world);
            RelocateHarborBoat(world);
            PlantBlenderGroves(world);
            Debug.Log("[VillageDraft] Blender festival geography: 15 venues, 10 authored environment meshes, coastal crossing, groves, and Market wayfinding.");
        }

        private static void DestroyChild(Transform ancestor, string path)
        {
            var child = ancestor.Find(path);
            if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        private static void MoveDistrictVenues()
        {
            DistrictVenues.Clear();
            var all = root.GetComponentsInChildren<Transform>(true);
            foreach (var site in DistrictSites)
            {
                var venue = all.FirstOrDefault(t => t.name.StartsWith(site.prefix, StringComparison.Ordinal) &&
                                                    t.Find("Floor") != null);
                if (venue == null) throw new InvalidOperationException("District venue missing: " + site.prefix);
                var destination = new Vector3(site.position.x, 0, site.position.y);
                venue.SetPositionAndRotation(destination, Quaternion.LookRotation(destination.normalized));
                DistrictVenues[site.prefix] = venue;
                if (site.extension == null) continue;
                var extension = root.Find("06 Expanded village/03 Original venue action extensions/" +
                                          site.extension + " extra activities");
                if (extension == null) throw new InvalidOperationException("Activity extension missing: " + site.extension);
                extension.SetParent(venue, false);
                extension.localPosition = Vector3.zero;
                extension.localRotation = Quaternion.identity;
            }
        }

        private static void PlaceFestivalKit(Transform world)
        {
            // Each FBX has one Blender-authored mesh and world-space UVs. Ten renderers
            // replace hundreds of Unity primitive road, landscape and landmark renderers.
            var entries = new[]
            {
                new[] { "base_ground", "Festival base meadow" },
                new[] { "district_ground", "Festival district ground" },
                new[] { "paths", "Festival routes and paving" },
                new[] { "civic", "Festival civic clock, stage, fountain and sign frames" },
                new[] { "garden_arts", "Festival gardens and public art" },
                new[] { "waterfront", "Festival coast furniture and piers" },
                new[] { "water", "Festival tidal water" },
                new[] { "maker_sky", "Festival maker yard and sky garden" },
                new[] { "perimeter", "Festival distant foothills and groves" },
                new[] { "market_wayfinding", "Festival Market pennant, trailpost and route inlays" }
            };
            foreach (var entry in entries)
            {
                var instance = PlaceModel(FestivalKit + entry[0] + ".fbx", entry[1],
                    world, Vector3.zero, 1f);
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.gameObject.name = entry[1] + " mesh";
                    renderer.sharedMaterials = renderer.sharedMaterials
                        .Select(m => FestivalMaterial(m == null ? "" : m.name)).ToArray();
                    renderer.shadowCastingMode = entry[0] == "base_ground" ||
                                                  entry[0] == "district_ground" ||
                                                  entry[0] == "paths" || entry[0] == "water" ||
                                                  entry[0] == "perimeter" ||
                                                  entry[0] == "market_wayfinding"
                        ? ShadowCastingMode.Off : ShadowCastingMode.On;
                    renderer.receiveShadows = entry[0] != "water" && entry[0] != "perimeter";
                }
            }
        }

        private static Material FestivalMaterial(string sourceName)
        {
            var name = sourceName.Trim().ToLowerInvariant()
                .Replace(" (instance)", "").Split('.')[0];
            var namespaceEnd = name.LastIndexOf("::", StringComparison.Ordinal);
            if (namespaceEnd >= 0) name = name.Substring(namespaceEnd + 2);
            switch (name)
            {
                case "grass": return TexturedFestivalMaterial("Festival meadow grass", "leafy_grass",
                    new Color(.79f, .87f, .69f));
                case "meadow": return TexturedFestivalMaterial("Ground meadow sage", "leafy_grass",
                    new Color(.72f, .83f, .69f));
                case "drygrass": return TexturedFestivalMaterial("Ground sunlit meadow", "leafy_grass",
                    new Color(.89f, .82f, .66f));
                case "sand": return TexturedFestivalMaterial("Ground coast shingle", "sand_02",
                    new Color(.81f, .76f, .65f));
                case "gravel": return TexturedFestivalMaterial("Maker gravel", "gravel_ground_01",
                    new Color(.71f, .70f, .64f));
                case "stone": return Mat("District limestone", new Color(.76f, .72f, .60f));
                case "terracotta": return Mat("District terracotta", new Color(.59f, .38f, .30f));
                case "bluestone": return Mat("District blue flagstone", new Color(.38f, .52f, .57f));
                case "plank": return Mat("District boardwalk", new Color(.43f, .30f, .22f));
                case "timber": return timber;
                case "ivory": return cream;
                case "bronze": return Mat("Festival brushed brass", new Color(.67f, .49f, .25f));
                case "metal": return Mat("Festival painted steel", new Color(.23f, .35f, .42f));
                case "coral": return roofRed;
                case "gold": return gold;
                case "teal": return roofTeal;
                case "navy": return roofBlue;
                case "leaf": return leaf;
                case "flower": return tomato;
                case "water": return water;
                case "ridge_sage": return TexturedFestivalMaterial("Festival distant sage", "leafy_grass",
                    new Color(.65f, .75f, .64f));
                case "ridge_ochre": return TexturedFestivalMaterial("Festival distant ochre", "leafy_grass",
                    new Color(.78f, .76f, .60f));
                case "ridge_leaf": return Mat("Festival distant broadleaf", new Color(.39f, .55f, .42f));
                case "ridge_pine": return Mat("Festival distant pine", new Color(.29f, .46f, .43f));
                case "ridge_bark": return Mat("Festival distant bark", new Color(.43f, .40f, .33f));
                default: throw new InvalidOperationException("Unmapped Blender festival material: " + sourceName);
            }
        }

        private static Material TexturedFestivalMaterial(string name, string slug, Color tint)
        {
            var material = Mat(name, tint);
            const string textureRoot = "Assets/VillageDraft/Textures/PolyHaven/";
            var color = AssetDatabase.LoadAssetAtPath<Texture2D>(textureRoot + slug + "_diff_1k.jpg");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(textureRoot + slug + "_normal_1k.jpg");
            if (color == null) throw new InvalidOperationException("Missing festival surface map: " + slug);
            material.SetTexture("_BaseMap", color);
            material.SetTextureScale("_BaseMap", Vector2.one);
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.SetFloat("_BumpScale", .16f);
                material.EnableKeyword("_NORMALMAP");
            }
            material.SetFloat("_Smoothness", .07f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void AddInvisibleDeckTeleportColliders(Transform world)
        {
            var deck = Group("Invisible piers: collision and XR teleport", world);
            var spans = new[]
            {
                new Vector4(52,-63,52,-70), new Vector4(52,-70,51,-80),
                new Vector4(20,-64,18,-69), new Vector4(18,-69,19,-78),
                new Vector4(19,-78,22,-86)
            };
            for (var i = 0; i < spans.Length; i++)
            {
                var span = spans[i];
                var a = new Vector3(span.x, .10f, span.y);
                var b = new Vector3(span.z, .10f, span.w);
                var objectName = "XR dock surface " + i;
                var go = new GameObject(objectName);
                go.transform.SetParent(deck, false);
                go.transform.position = (a + b) * .5f;
                go.transform.rotation = Quaternion.LookRotation(b - a);
                var collider = go.AddComponent<BoxCollider>();
                collider.size = new Vector3(i < 2 ? 3f : 2.5f, .18f, Vector3.Distance(a,b) + .12f);
                go.AddComponent<TeleportationArea>();
            }
        }

        private static void RelocateHarborBoat(Transform world)
        {
            var harbor = DistrictVenues["13 HARBOR"];
            var boat = harbor.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name.StartsWith("Blender prop 1 - boat", StringComparison.Ordinal));
            if (boat == null) throw new InvalidOperationException("Harbor Blender boat display missing.");
            boat.SetParent(world, false);
            boat.position = new Vector3(39, .09f, -77);
            boat.rotation = Quaternion.Euler(-90, 180, 0);
            boat.localScale = Vector3.one;
            FitDisplayFootprint(boat.gameObject, 5.8f);
            AlignBlenderBase(boat.gameObject, .09f);
        }

        private static void PlantBlenderGroves(Transform world)
        {
            var trees = Group("Blender village groves and flowering pockets", world);
            var positions = new[]
            {
                new Vector2(-62,11),new Vector2(-55,4),new Vector2(-66,54),new Vector2(-44,53),
                new Vector2(-33,47),new Vector2(-35,-4),new Vector2(-34,-31),new Vector2(-27,-46),
                new Vector2(-59,-75),new Vector2(-75,-76),new Vector2(5,48),new Vector2(7,67),
                new Vector2(35,70),new Vector2(50,61),new Vector2(45,52),new Vector2(8,-20),
                new Vector2(3,-43),new Vector2(7,-69),new Vector2(32,-88),new Vector2(66,-76),
                new Vector2(83,8),new Vector2(74,22),new Vector2(59,17),new Vector2(86,49),
                new Vector2(88,73),new Vector2(-85,60),new Vector2(-80,68),new Vector2(-73,74),
                new Vector2(-64,77),new Vector2(-34,80),new Vector2(-26,78),new Vector2(43,83),
                new Vector2(50,81),new Vector2(57,84),new Vector2(84,34),new Vector2(86,26),
                new Vector2(87,-44),new Vector2(83,-56),new Vector2(70,-85),new Vector2(62,-88),
                new Vector2(0,-86),new Vector2(-17,-86),new Vector2(-30,-86),new Vector2(-87,-67),
                new Vector2(-86,-5),new Vector2(-89,6),new Vector2(-86,18)
            };
            var planted = 0;
            foreach (var site in positions)
            {
                var position = new Vector3(site.x, 0, site.y);
                if (NearAnyVenue(position, 16f)) continue;
                var coastal = site.y < -55 || site.x > 66 || planted % 7 == 0;
                var holder = Group("Foliage site", trees);
                holder.position = position;
                holder.rotation = Quaternion.Euler(0, (planted * 73) % 360, 0);
                var model = PlaceModel(FoliageKit + (coastal ? "coastal_pine" : "deciduous_tree") + ".fbx",
                    coastal ? "Blender coastal pine" : "Blender deciduous tree",
                    holder, Vector3.zero, .90f + (planted % 5) * .065f);
                AlignBlenderBase(model, 0);
                planted++;
            }
            var shrubs = new[]
            {
                new Vector2(-12,28),new Vector2(-8,36),new Vector2(-40,10),new Vector2(-48,13),
                new Vector2(-37,-37),new Vector2(-31,-47),new Vector2(-68,14),new Vector2(13,32),
                new Vector2(33,42),new Vector2(37,18),new Vector2(35,-14),new Vector2(8,-61),
                new Vector2(11,-74),new Vector2(65,-79),new Vector2(80,40),new Vector2(-70,-73)
            };
            for (var i = 0; i < shrubs.Length; i++)
            {
                var site = new Vector3(shrubs[i].x, 0, shrubs[i].y);
                if (NearAnyVenue(site, 13f)) continue;
                var holder = Group("Blender flowering path pocket", trees);
                holder.position = site;
                holder.rotation = Quaternion.Euler(0, (i * 47) % 360, 0);
                var shrub = PlaceModel(FoliageKit + "flowering_shrub.fbx", "Blender flowering shrub",
                    holder, Vector3.zero, .73f + (i % 3) * .08f);
                AlignBlenderBase(shrub, 0);
            }
            Debug.Log("[VillageDraft] Planted " + planted + " Blender trees without venue or side-yard collisions.");
        }

        private static bool NearAnyVenue(Vector3 point, float distance)
        {
            foreach (var venue in DistrictVenues.Values)
                if ((venue.position - point).sqrMagnitude < distance * distance) return true;
            return false;
        }

        private static void AlignBlenderBase(GameObject instance, float groundY)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Blender FBX has no renderer: " + instance.name);
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            instance.transform.position += Vector3.up * (groundY - bounds.min.y);
        }
    }
}
