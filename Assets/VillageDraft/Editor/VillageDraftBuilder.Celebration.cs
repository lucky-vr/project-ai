using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace VillageDraft.Editor
{
    public static partial class VillageDraftBuilder
    {
        private static void BuildCelebration()
        {
            var celebration = Group("09 Village completion celebration", root);
            var fountain = new GameObject("Plaza victory confetti");
            fountain.transform.SetParent(celebration, false);
            fountain.transform.position = new Vector3(0, 2.9f, 0);

            var system = fountain.AddComponent<ParticleSystem>();
            var main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 3.0f;
            main.startLifetime = 2.4f;
            main.startSpeed = 5.2f;
            main.startSize = .12f;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, .78f, .25f, .82f), new Color(.18f, .77f, .77f, .82f));
            main.gravityModifier = 1.0f;
            main.maxParticles = 180;

            var emission = system.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, 150) });
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = .65f;
            shape.angle = 30;

            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/VillageDraft/Materials/FX/Soft particle.mat");
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var trigger = celebration.gameObject.AddComponent<VillageCelebration>();
            trigger.progressBoard = board;
            trigger.confetti = system;
        }
    }
}
