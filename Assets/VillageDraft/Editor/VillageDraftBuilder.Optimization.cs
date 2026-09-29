using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VillageDraft.Editor
{
    public static partial class VillageDraftBuilder
    {
        /// <summary>Batch only fixed world geometry; keep every XR control and changing label dynamic.</summary>
        private static void MarkStaticDecor()
        {
            var count = 0;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer || renderer.GetComponent<TextMesh>() != null ||
                    renderer.gameObject.name.StartsWith("Task lamp") ||
                    renderer.gameObject.name == "Completion lamp")
                    continue;

                var transform = renderer.transform;
                var moving = false;
                while (transform != null && transform != root)
                {
                    if (transform.GetComponent<XRGrabInteractable>() != null ||
                        transform.GetComponent<XRSimpleInteractable>() != null ||
                        transform.GetComponent<XRSocketInteractor>() != null ||
                        transform.GetComponent<VillageMovingProp>() != null ||
                        transform.GetComponent<VillageActionEffect>() != null)
                    {
                        moving = true;
                        break;
                    }
                    transform = transform.parent;
                }
                if (moving) continue;
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic);
                count++;
            }
            Debug.Log($"[VillageDraft] Marked {count} fixed renderers for static batching.");
        }
    }
}
