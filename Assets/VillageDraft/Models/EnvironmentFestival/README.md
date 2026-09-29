# Village festival environment kit

Editable source: [Source/festival_environment.blend](Source/festival_environment.blend).
Regenerate the FBX files with:

```bash
blender --background --python Assets/VillageDraft/Models/EnvironmentFestival/generate_festival_environment.py
```

All visible geometry in this kit is modeled and exported from Blender. The scene
builder instantiates the ten FBX meshes in `FBX/` and supplies Unity materials.
The old Unity Plane stays only as an invisible XR teleport collider. Five dock
BoxColliders are invisible and align with the Blender-authored pier decks. The
`perimeter.fbx` mesh adds rolling distant foothills and 131 clustered broadleaf
and coastal pine silhouettes beyond the playable area. It uses one renderer,
five additional material slots, and 7,854 triangles. Its inner radius stays
outside venue footprints and XR routes.

`market_wayfinding.fbx` is one additional Blender mesh. It puts a coral
pennant above Market's 7.26 m roof, a raised-letter trailpost off the right
edge of the first fork, and seven flush coral/gold direction inlays on the
route to the Market entrance. These pieces have no colliders. Their world
positions are recorded in `asset_manifest.json`.
The wayfinding export is one renderer with 1,054 triangles and no collider;
Blender MCP geometry checks are in `market_wayfinding_validation.json`.

The 500 by 500 m ground is flat within the playable village, then rises gently
beyond the template collider. Paths carry distance-based UVs and fork around
the fountain, festival stage, and clock tower. The civic FBX includes the
fountain, physical welcome/progress/quest sign frames, stage, garland, benches,
and clock. The harbor boat is an existing Blender model moved to the inlet;
this kit contains no duplicate boat.

The export applies the project's 180-degree source turn for
`PlaceModel`'s `Quaternion.Euler(-90, 180, 0)` placement basis. The source
`.blend` retains human-readable village coordinates. See
`asset_manifest.json` for per-FBX triangle counts and material slots;
`geometry_validation.json` records Blender MCP checks for the original eight
meshes; `perimeter_validation.json` checks the new mesh for UV coverage,
degenerate faces, and bounds. `overview_preview.png`,
`arrival_preview.png`, and `perimeter_preview.png` are Blender Workbench
references. Final lighting and
placement require a Unity scene rebuild.

This is AI-assisted Blender artwork. Disclose its AI origin if the course
requires authorship attribution.
