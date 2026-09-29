// Keeps Blender-authored village material maps connected in Unity URP.
// The models are AI-assisted reference assets and do not count as self-modeled coursework.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class VillageModelMaterialImport : AssetPostprocessor
{
    private const string Root = "Assets/VillageDraft/Models";
    private const string MaterialFolder = Root + "/Materials";

    private static readonly Dictionary<string, string> Textures = new Dictionary<string, string>
    {
        { "Limestone plaster", "limestone_blocks" },
        { "Chalk painted timber", "chalk_grain" },
        { "Warm cedar timber", "cedar_planks" },
        { "Blue charcoal metal", "charcoal_ribbed" },
        { "Brushed brass", "brushed_brass" },
        { "Pale aqua glass", "aqua_glazing" },
        { "Warm sandstone paving", "sandstone_pavers" },
    };

    private static readonly Dictionary<string, Color> Solids = new Dictionary<string, Color>
    {
        { "Warm lantern glass", new Color(1f, .84f, .43f) },
        { "Garden leaf", new Color(.21f, .48f, .27f) },
        { "Baked crust", new Color(.78f, .44f, .16f) },
        { "Orchard red", new Color(.73f, .13f, .12f) },
    };

    private static readonly string[] Venues =
    {
        "market", "cafe", "workshop", "greenhouse", "pool", "bakery", "post",
        "clinic", "library", "music", "garage", "observatory", "harbor", "art", "recycling"
    };

    [MenuItem("Tools/Village Draft/Import Authored Model Materials")]
    public static void CreateAndRemapMaterials()
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder(Root, "Materials");

        foreach (var entry in Textures)
            EnsureMaterial(entry.Key, entry.Value, Color.white);
        foreach (var entry in Solids)
            EnsureMaterial(entry.Key, null, entry.Value);
        foreach (var venue in Venues)
            EnsureMaterial(venue + "_finish", venue + "_finish", Color.white);

        AssetDatabase.SaveAssets();
        foreach (var venue in Venues)
        {
            string folder = Root + "/FBX/" + venue;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { folder }))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Village model material remap ready: " + Venues.Length + " venues with URP albedo maps.");
    }

    private static void EnsureMaterial(string name, string textureName, Color color)
    {
        string path = MaterialFolder + "/" + name.Replace(' ', '_') + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP/Lit is unavailable for village model materials.");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        if (textureName != null)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/" + textureName + ".png");
            if (texture == null) throw new FileNotFoundException("Missing authored model albedo map: " + textureName);
            material.SetTexture("_BaseMap", texture);
        }
        else material.SetTexture("_BaseMap", null);
        material.SetFloat("_Smoothness", name == "Pale aqua glass" ? .48f : .14f);
        EditorUtility.SetDirty(material);
    }

    private Material OnAssignMaterialModel(Material imported, Renderer renderer)
    {
        if (!assetPath.StartsWith(Root + "/FBX/", StringComparison.Ordinal)) return null;
        string name = imported.name;
        if (name == "Venue accent")
        {
            string relative = assetPath.Substring((Root + "/FBX/").Length);
            string venue = relative.Split('/')[0];
            return AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/" + venue + "_finish.mat");
        }
        if (!Textures.ContainsKey(name) && !Solids.ContainsKey(name)) return null;
        return AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/" + name.Replace(' ', '_') + ".mat");
    }
}
