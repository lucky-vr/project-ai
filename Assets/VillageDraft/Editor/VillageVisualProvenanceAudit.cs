using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VillageDraft.Editor
{
    /// <summary>Checks the source of visible scene meshes; text and particles are runtime effects.</summary>
    public static class VillageVisualProvenanceAudit
    {
        [MenuItem("Tools/VR Village/Audit Blender Visuals")]
        public static void Audit()
        {
            var root = GameObject.Find("_VILLAGE_DRAFT");
            if (root == null) throw new InvalidOperationException("Build the village scene before auditing visuals.");

            var blender = 0;
            var other = new List<string>();
            var sections = new Dictionary<string, int>();
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    // TextMesh is generated at runtime so prompts can reflect progress.
                    if (renderer.GetComponent<TextMesh>() == null)
                        other.Add(renderer.name + " (no mesh asset)");
                    continue;
                }

                var path = AssetDatabase.GetAssetPath(filter.sharedMesh);
                if (path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".blend", StringComparison.OrdinalIgnoreCase))
                    blender++;
                else
                {
                    var section = Section(renderer.transform, root.transform);
                    sections[section] = sections.TryGetValue(section, out var count) ? count + 1 : 1;
                    other.Add(section + "/" + renderer.name + " [" +
                              (string.IsNullOrEmpty(path) ? "built-in/procedural" : path) + "]");
                }
            }

            var reportPath = Path.Combine(Application.dataPath, "../Library/VillageDraftVisualAudit.txt");
            File.WriteAllLines(reportPath, other.OrderBy(x => x, StringComparer.Ordinal));
            var breakdown = string.Join(", ", sections.OrderByDescending(x => x.Value)
                .Select(x => x.Key + "=" + x.Value));
            var report = $"[VillageDraft] Visual provenance: {blender} visible Blender mesh renderers, " +
                         $"{other.Count} visible non-Blender mesh renderers. " +
                         "Dynamic text and particles are excluded. Sections: " + breakdown +
                         ". Full report: " + reportPath;
            if (other.Count == 0) Debug.Log(report);
            else Debug.LogWarning(report);
        }

        private static string Section(Transform child, Transform root)
        {
            var cursor = child;
            while (cursor.parent != null && cursor.parent != root)
                cursor = cursor.parent;
            return cursor.parent == root ? cursor.name : "Outside village root";
        }
    }
}
