"""Write the read-only Unity Editor model path catalog from asset_manifest.json."""
import json
from pathlib import Path

HERE=Path(__file__).resolve().parent
OUT=HERE.parent/'Editor'/'VillageModelCatalog.cs'
manifest=json.loads((HERE/'asset_manifest.json').read_text(encoding='utf-8'))

def quote(s):
    return json.dumps(s,ensure_ascii=False)

def f(n):
    return f'{float(n):.4f}f'

lines=[
    'using System;',
    'using System.Collections.Generic;',
    'using UnityEditor;',
    'using UnityEngine;',
    '',
    'namespace VillageDraft.Editor',
    '{',
    '    /// <summary>AI-assisted reference model paths and placement hints; no scene mutations.</summary>',
    '    public static class VillageModelCatalog',
    '    {',
    '        public sealed class VenueAssets',
    '        {',
    '            public readonly string Id;',
    '            public readonly string ExteriorPath;',
    '            public readonly string[] PropPaths;',
    '            public readonly string[] PropRoles;',
    '            public readonly Vector3[] SuggestedLocalPositions;',
    '            public readonly float[] SuggestedYaws;',
    '            public readonly float[] SuggestedScales;',
    '            public readonly string[] PreferredActionObjects;',
    '            public readonly float[] ActionVisualScales;',
    '            public VenueAssets(string id, string exteriorPath, string[] propPaths,',
    '                string[] propRoles, Vector3[] positions, float[] yaws, float[] scales,',
    '                string[] preferredActions, float[] actionScales)',
    '            {',
    '                Id=id; ExteriorPath=exteriorPath; PropPaths=propPaths; PropRoles=propRoles;',
    '                SuggestedLocalPositions=positions; SuggestedYaws=yaws; SuggestedScales=scales;',
    '                PreferredActionObjects=preferredActions; ActionVisualScales=actionScales;',
    '            }',
    '        }',
    '',
    '        public static readonly string[] VenueIds = {',
    '            '+', '.join(quote(vid) for vid in manifest['venues']),
    '        };',
    '        private static readonly Dictionary<string, VenueAssets> Entries =',
    '            new Dictionary<string, VenueAssets>(StringComparer.OrdinalIgnoreCase)',
    '        {',
]

for vid,venue in manifest['venues'].items():
    assets=venue['assets']
    ext='Assets/VillageDraft/Models/'+assets[0]['fbx']
    props=assets[1:]
    paths=['Assets/VillageDraft/Models/'+a['fbx'] for a in props]
    roles=[a['role'] for a in props]
    positions=['new Vector3('+', '.join(f(n) for n in a['suggested_local_position'])+')' for a in props]
    yaws=[f(a['suggested_yaw_degrees']) for a in props]
    scales=[f(a['suggested_scale']) for a in props]
    actions=[a['preferred_action_object'] for a in props]
    action_scales=[f(a['action_visual_scale']) for a in props]
    lines += [
        '            { '+quote(vid)+', new VenueAssets(',
        '                '+quote(vid)+', '+quote(ext)+',',
        '                new[] { '+', '.join(quote(x) for x in paths)+' },',
        '                new[] { '+', '.join(quote(x) for x in roles)+' },',
        '                new[] { '+', '.join(positions)+' },',
        '                new[] { '+', '.join(yaws)+' },',
        '                new[] { '+', '.join(scales)+' },',
        '                new[] { '+', '.join(quote(x) for x in actions)+' },',
        '                new[] { '+', '.join(action_scales)+' }) },',
    ]

lines += [
    '        };',
    '',
    '        public static IEnumerable<VenueAssets> All => Entries.Values;',
    '        public static VenueAssets Get(string venueId)',
    '        {',
    '            if (!Entries.TryGetValue(venueId, out var entry))',
    '                throw new ArgumentException("Unknown village venue: " + venueId, nameof(venueId));',
    '            return entry;',
    '        }',
    '',
    '        public static GameObject LoadExterior(string venueId) =>',
    '            AssetDatabase.LoadAssetAtPath<GameObject>(Get(venueId).ExteriorPath);',
    '',
    '        public static GameObject LoadProp(string venueId, int index)',
    '        {',
    '            var entry = Get(venueId);',
    '            if (index < 0 || index >= entry.PropPaths.Length)',
    '                throw new ArgumentOutOfRangeException(nameof(index));',
    '            return AssetDatabase.LoadAssetAtPath<GameObject>(entry.PropPaths[index]);',
    '        }',
    '    }',
    '}',
]
OUT.write_text('\n'.join(lines)+'\n',encoding='utf-8')
print(OUT)
