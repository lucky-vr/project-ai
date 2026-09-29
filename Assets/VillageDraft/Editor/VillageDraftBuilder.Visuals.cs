using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace VillageDraft.Editor
{
    public static partial class VillageDraftBuilder
    {
        private const string FxMaterialFolder = "Assets/VillageDraft/Materials/FX";

        /// <summary>
        /// A repeatable, inexpensive presentation layer. Call after venues, plants, gameplay,
        /// and BuildLighting(), while the builder's root and materials are still available.
        /// </summary>
        private static void BuildVisuals()
        {
            EnsureFolder("Assets/VillageDraft/Materials", "FX");
            var presentation = Group("08 Village presentation", root);

            ApplyVillageSurfaceMaterials();
            SetVillageAtmosphere();
            AddWaterAndParticles(presentation);
        }

        private static void ApplyVillageSurfaceMaterials()
        {
            const string textures = "Assets/VillageDraft/Textures/PolyHaven/";
            Surface(grass, "leafy_grass", new Color(.80f, .93f, .75f), 84f, .28f);
            Surface(road, "gravel_ground_01", new Color(.74f, .78f, .79f), 8f, .30f);
            Surface(paving, "cobblestone_floor_04", new Color(.97f, .91f, .79f), 4f, .34f);
            Surface(timber, "brown_planks_07", new Color(.82f, .69f, .55f), 2f, .30f);
            SurfaceByName("District limestone", "cobblestone_floor_04", new Color(.97f, .92f, .80f), 2f, .32f);
            SurfaceByName("District terracotta", "cobblestone_floor_04", new Color(.89f, .55f, .45f), 2f, .32f);
            SurfaceByName("District blue flagstone", "cobblestone_floor_04", new Color(.56f, .75f, .81f), 2f, .32f);
            SurfaceByName("District sand", "sand_02", new Color(.95f, .86f, .69f), 3f, .22f);
            SurfaceByName("District boardwalk", "brown_planks_07", new Color(.88f, .78f, .65f), 3f, .30f);
            SurfaceByName("Coast weathered timber", "brown_planks_07", new Color(.82f, .77f, .68f), 2f, .30f);
            SurfaceByName("Civic dressed stone", "cobblestone_floor_04", new Color(.98f, .92f, .81f), 2f, .25f);
            SurfaceByName("Maker gravel", "gravel_ground_01", new Color(.83f, .80f, .71f), 3f, .28f);

            void SurfaceByName(string name, string slug, Color tint, float tile, float bump) =>
                Surface(AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/" + name + ".mat"),
                    slug, tint, tile, bump);
            void Surface(Material material, string slug, Color tint, float tile, float bump)
            {
                if (material == null) return;
                var color = AssetDatabase.LoadAssetAtPath<Texture2D>(textures + slug + "_diff_1k.jpg");
                var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(textures + slug + "_normal_1k.jpg");
                if (color == null) throw new InvalidOperationException("Village texture missing: " + slug);
                material.SetColor("_BaseColor", tint);
                material.SetTexture("_BaseMap", color);
                material.SetTextureScale("_BaseMap", new Vector2(tile, tile));
                if (normal != null)
                {
                    material.SetTexture("_BumpMap", normal);
                    material.SetFloat("_BumpScale", bump);
                    material.EnableKeyword("_NORMALMAP");
                }
                material.SetFloat("_Smoothness", .12f);
                EditorUtility.SetDirty(material);
            }
        }

        private static void SetVillageAtmosphere()
        {
            var sun = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                .FirstOrDefault(l => l.type == LightType.Directional);
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(47, -38, 0);
                sun.color = new Color(1f, .94f, .84f);
                sun.intensity = 1.15f;
                sun.shadows = LightShadows.Hard;
                sun.shadowStrength = .38f;
                RenderSettings.sun = sun;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.67f, .78f, .90f);
            RenderSettings.ambientEquatorColor = new Color(.72f, .76f, .69f);
            RenderSettings.ambientGroundColor = new Color(.37f, .43f, .39f);
            RenderSettings.reflectionIntensity = .65f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.70f, .82f, .88f);
            RenderSettings.fogDensity = .0008f; // Clear across the full ring from the showcase camera.

            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var sky = FxMaterial("Village morning sky", skyShader);
                sky.SetColor("_SkyTint", new Color(.47f, .68f, .88f));
                sky.SetColor("_GroundColor", new Color(.58f, .74f, .72f));
                sky.SetFloat("_AtmosphereThickness", .78f);
                sky.SetFloat("_Exposure", 1.18f);
                sky.SetFloat("_SunSize", .025f);
                sky.SetFloat("_SunSizeConvergence", 6f);
                RenderSettings.skybox = sky;
                EditorUtility.SetDirty(sky);
            }
        }

        private static void AddFacadeDetails()
        {
            // Every venue made by Venue() has these two children, including pool/open-air sites.
            // This also picks up any additional houses placed on the outer ring.
            var venues = root.GetComponentsInChildren<Transform>(true)
                .Where(t => t.Find("Sign backing") != null && t.Find("Front fascia") != null)
                .ToArray();
            var glassFx = SolidFxMaterial("Window aqua", new Color(.24f, .54f, .58f));
            var warmBulb = EmissiveFxMaterial("Warm lantern glow", new Color(1f, .73f, .36f), 2.1f);
            var lightTrim = SolidFxMaterial("Facade pale trim", new Color(.96f, .91f, .78f));

            for (int i = 0; i < venues.Length; i++)
            {
                var venue = venues[i];
                var details = Group("Presentation - facade and entry", venue);
                var accent = venue.Find("Front fascia").GetComponent<Renderer>().sharedMaterial;

                VisualLocal("Roof edge highlight", PrimitiveType.Cube, details,
                    new Vector3(0, 4.84f, -5.52f), new Vector3(12.5f, .08f, .12f), lightTrim);
                VisualLocal("Sign lower accent", PrimitiveType.Cube, details,
                    new Vector3(0, 2.86f, -5.12f), new Vector3(7.8f, .055f, .10f), accent);
                VisualLocal("Entry mat", PrimitiveType.Cube, details,
                    new Vector3(0, .065f, -5.67f), new Vector3(4.4f, .025f, 1.1f), accent);

                for (int side = -1; side <= 1; side += 2)
                {
                    VisualLocal("Front glowing lantern", PrimitiveType.Sphere, details,
                        new Vector3(side * 4.85f, 3.5f, -5.43f),
                        new Vector3(.28f, .28f, .28f), warmBulb);
                    VisualLocal("Front color pilaster", PrimitiveType.Cube, details,
                        new Vector3(side * 5.66f, 2.45f, -5.05f),
                        new Vector3(.14f, 3.0f, .18f), accent);
                    VisualLocal("Entry planter box", PrimitiveType.Cube, details,
                        new Vector3(side * 5.1f, .29f, -5.7f),
                        new Vector3(.72f, .46f, .72f), timber);
                    VisualLocal("Entry planter foliage", PrimitiveType.Sphere, details,
                        new Vector3(side * 5.1f, .72f, -5.7f),
                        new Vector3(.75f, .68f, .75f), leaf);
                }

                // Ordinary houses get simple side windows. The pool's roofless pergola stays open.
                if (venue.Find("Roof") != null)
                {
                    for (int rib = -1; rib <= 1; rib++)
                        VisualLocal("Pale roof seam", PrimitiveType.Cube, details,
                            new Vector3(rib * 3.6f, 4.73f, 0),
                            new Vector3(.085f, .035f, 10.4f), lightTrim);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        VisualLocal("Side window", PrimitiveType.Cube, details,
                            new Vector3(side * 6.035f, 2.4f, .7f),
                            new Vector3(.055f, 1.45f, 2.05f), glassFx);
                        VisualLocal("Window mullion", PrimitiveType.Cube, details,
                            new Vector3(side * 6.075f, 2.4f, .7f),
                            new Vector3(.06f, 1.52f, .08f), lightTrim);
                    }
                }

                VisualLocal("Wayfinding beacon base", PrimitiveType.Cylinder, details,
                    new Vector3(0, 4.88f, 0), new Vector3(.28f, .13f, .28f), accent);
                VisualLocal("Wayfinding beacon globe", PrimitiveType.Sphere, details,
                    new Vector3(0, 5.22f, 0), new Vector3(.40f, .40f, .40f), warmBulb);

                // One local fill light per four venues; no additional shadow maps.
                if (i % 4 == 0)
                {
                    var lamp = new GameObject("Warm entry fill - no shadows");
                    lamp.transform.SetParent(details, false);
                    lamp.transform.localPosition = new Vector3(0, 3.2f, -5.5f);
                    var light = lamp.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.color = new Color(1f, .72f, .43f);
                    light.intensity = .50f;
                    light.range = 5.5f;
                    light.shadows = LightShadows.None;
                    light.renderMode = LightRenderMode.ForcePixel;
                }
            }

            Debug.Log($"[VillageDraft] Presentation details added to {venues.Length} venues.");
        }

        private static void AddPlazaDetails(Transform presentation)
        {
            var plaza = Group("Fountain gilding and lanterns", presentation);
            var pale = SolidFxMaterial("Fountain pale stone", new Color(.95f, .90f, .75f));
            for (int i = 0; i < 16; i++)
            {
                float angle = i * 360f / 16f;
                float rad = angle * Mathf.Deg2Rad;
                var segment = VisualLocal("Fountain rim segment", PrimitiveType.Cube, plaza,
                    new Vector3(Mathf.Sin(rad) * 1.36f, .41f, Mathf.Cos(rad) * 1.36f),
                    new Vector3(.53f, .065f, .11f), pale);
                segment.localRotation = Quaternion.Euler(0, angle, 0);
            }

            var warmBulb = EmissiveFxMaterial("Warm lantern glow", new Color(1f, .73f, .36f), 2.1f);
            var lanterns = root.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.gameObject.name == "Lantern").ToArray();
            foreach (var lantern in lanterns) lantern.sharedMaterial = warmBulb;

            // Most lanterns are visual only; three provide nearby fill on desktop quality.
            for (int i = 0; i < lanterns.Length; i += 3)
            {
                var source = new GameObject("Plaza lamp fill - no shadows");
                source.transform.SetParent(lanterns[i].transform, false);
                source.transform.localPosition = Vector3.zero;
                var light = source.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, .78f, .47f);
                light.intensity = .42f;
                light.range = 4.5f;
                light.shadows = LightShadows.None;
            }
        }

        private static void AddWaterAndParticles(Transform presentation)
        {
            var waterShader = Shader.Find("VillageDraft/WaterSurface");
            if (waterShader != null)
            {
                var waterFx = FxMaterial("Animated turquoise water", waterShader);
                waterFx.SetColor("_DeepColor", new Color(.045f, .24f, .31f));
                waterFx.SetColor("_ShallowColor", new Color(.18f, .45f, .50f));
                waterFx.SetColor("_RippleColor", new Color(.45f, .66f, .65f));
                waterFx.SetFloat("_RippleScale", 1.4f);
                waterFx.SetFloat("_RippleSpeed", .58f);
                EditorUtility.SetDirty(waterFx);
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    var name = renderer.gameObject.name;
                    if (name.IndexOf("water", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        name.IndexOf("tank", StringComparison.OrdinalIgnoreCase) < 0)
                        renderer.sharedMaterial = waterFx;
                }
            }

            var particles = Group("Subtle water and steam particles", presentation);
            var particleShader = Shader.Find("VillageDraft/ParticleGlow");
            if (particleShader == null) return;
            var particleMat = FxMaterial("Soft particle", particleShader);
            particleMat.SetColor("_Color", Color.white);
            EditorUtility.SetDirty(particleMat);

            var fountain = CreateEmitter("Fountain spray", particles, particleMat,
                new Vector3(0, 2.45f, 0), new Color(.72f, .96f, 1f, .70f),
                1.25f, 2.55f, .085f, 30f, .68f, 70);
            var fountainShape = fountain.shape;
            fountainShape.shapeType = ParticleSystemShapeType.Cone;
            fountainShape.radius = .13f;
            fountainShape.angle = 25f;

            foreach (var poolWater in root.GetComponentsInChildren<Transform>(true)
                         .Where(t => t.name == "Pool water"))
            {
                var pool = CreateEmitter("Pool surface sparkle", particles, particleMat,
                    poolWater.position + Vector3.up * .07f,
                    new Color(.79f, .98f, 1f, .43f), .65f, .12f, .07f,
                    5f, 0f, 20);
                var shape = pool.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(poolWater.lossyScale.x * .7f, .02f,
                    poolWater.lossyScale.z * .7f);
            }

            foreach (var machine in root.GetComponentsInChildren<Transform>(true)
                         .Where(t => t.name == "Coffee machine"))
            {
                var steam = CreateEmitter("Cafe steam", particles, particleMat,
                    machine.position + Vector3.up * .52f,
                    new Color(.95f, .96f, .93f, .28f), 1.8f, .35f, .13f,
                    3f, -.025f, 14);
                var shape = steam.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.radius = .10f;
                shape.angle = 11f;
            }
        }

        private static ParticleSystem CreateEmitter(string name, Transform parent, Material material,
            Vector3 position, Color tint, float lifetime, float speed, float size,
            float rate, float gravity, int maxParticles)
        {
            var emitter = new GameObject(name);
            emitter.transform.SetParent(parent, false);
            emitter.transform.position = position;
            var system = emitter.AddComponent<ParticleSystem>();
            var main = system.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = Mathf.Max(lifetime * 2f, 2f);
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = tint;
            main.gravityModifier = gravity;
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = system.emission;
            emission.rateOverTime = rate;
            var fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .18f),
                    new GradientAlphaKey(.7f, .72f), new GradientAlphaKey(0, 1) });
            fade.color = gradient;
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }

        private static Transform VisualLocal(string name, PrimitiveType type, Transform parent,
            Vector3 position, Vector3 scale, Material material)
        {
            var visual = Local(name, type, parent, position, scale, material, false);
            GameObjectUtility.SetStaticEditorFlags(visual.gameObject, StaticEditorFlags.BatchingStatic);
            var renderer = visual.GetComponent<Renderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            return visual;
        }

        private static Material SolidFxMaterial(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var material = FxMaterial(name, shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EmissiveFxMaterial(string name, Color color, float intensity)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = FxMaterial(name, shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", color * intensity);
            material.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material FxMaterial(string name, Shader shader)
        {
            if (shader == null) throw new InvalidOperationException($"No shader available for FX material {name}.");
            var path = $"{FxMaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }
            return material;
        }
    }
}
