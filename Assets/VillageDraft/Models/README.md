# Blender village reference library

This folder contains **AI-assisted reference models**, made procedurally in Blender 5.2. They are useful for visualizing and building the VR game. **They are not student self-modeled work.** The course outline says AI-generated models must be declared, earn no model marks, and can zero a house if presented as original student work. Do not claim these toward the eight required self-modeled assets per house. Students can use the `.blend` sources to study proportions and then create their own distinguishable models, UVs, and materials for submission.

## Files

- `generate_village_assets.py`: reproducible Blender authoring/export pipeline.
- `Source/<venue>.blend`: one editable source project per venue, with a walk-in exterior and seven themed props. The greenhouse has one additional seed-packet pickup collection.
- `FBX/<venue>/<asset>.fbx`: 120 venue models and the separate greenhouse seed packet; front of each building faces local negative Z after FBX import.
- `Environment/generate_foliage.py`, `Environment/Source/*.blend`, and `FBX/environment/*.fbx`: three editable foliage references for the village grounds.
- `Environment/foliage_contact_sheet.png`: street-height review of the three foliage references.
- `Textures/*.png`: seven shared 512 px tileable colour maps and fifteen 256 px venue finishes.
- `Materials/*.mat` and `Editor/VillageModelMaterialImport.cs`: shared URP/Lit materials and a scoped FBX material remap that reconnects the authored `_BaseMap` textures.
- `asset_manifest.json`: venue IDs, exact asset names, relative paths, triangle counts, and material slots.
- `prop_variant_scales.json`: authored proportion choices baked into repeated-role props by the generator.
- `silhouette_audit.json`, `prop_silhouette_audit.json`, and `prop_appearance_audit.json`: reproducible front/top/three-quarter shape and material-colour comparisons. Run the matching `audit_*.py` script in Blender followed by `analyze_*.py` with Python, Pillow, NumPy, and scikit-image. Raw QA frames are written to `/tmp/village_model_audit` by default so Unity does not import hundreds of inspection PNGs.
- `street_contact_sheet.png`, `prop_contact_sheet.png`, and `cross_role_review.png`: inspected exterior, prop, and remaining close-pair previews.

All mesh pieces have UV layers and material assignments. The redesigned venues have different structures and heights: open pool court and amphitheater, bazaar, glasshouse, dock, domed observatory, towers, pavilion, garage, and yard. Exterior platforms range from 12–16 m wide and 10–12 m deep. The usable centre remains open for XR stations, except for thematic seating and perimeter fixtures. Every FBX has a single joined renderer, with multiple material slots. The models are visual references; Unity colliders and gameplay come from the scene builder.

**Unity import contract:** keep `ModelImporter.useFileScale=false` for these FBX. Their imported mesh is Z-up; the scene builder uses `Quaternion.Euler(-90,180,0)` and an instance scale of one to stand them up with entrances facing local negative Z. Default FBX prefab root scale is 100 and should be reset to one on scene instances. Material maps are assigned via **Tools → Village Draft → Import Authored Model Materials** after a fresh import. The supplied shared material assets already have this mapping in the current project.

To regenerate from the Unity project root:

```bash
blender --background --python Assets/VillageDraft/Models/generate_village_assets.py
blender --background --python Assets/VillageDraft/Models/Environment/generate_foliage.py
```

## Venue IDs

`market`, `cafe`, `workshop`, `greenhouse`, `pool`, `bakery`, `post`, `clinic`, `library`, `music`, `garage`, `observatory`, `harbor`, `art`, `recycling`.

The colour maps contain procedural grain, paver joints, limestone courses, ribbed metal, glass streaks, and venue-specific finish seams. They are meant as low-cost game reference surfaces, not photorealistic scans. The current 120 venue assets total 336,463 triangles and 513 material slots; the separate seed packet is 864 triangles. Static batching and careful draw-call profiling still matter on standalone VR hardware.

The neutral silhouette audit excludes the common exterior floor. Across 105 exterior venue pairs, no pair exceeds mean front/top intersection-over-union of 0.60 (worst 0.5968). Across the 105 prop pairs sharing the same functional role, no pair exceeds mean front/top/three-quarter overlap of 0.60. The broader all-role prop audit still has 113 of 5,460 pairs above 0.60; 29 of those also have median material-colour difference below CIELAB DeltaE76 20. Three-quarter review shows many are clearly different functional forms despite similar occupied screen area, but the numeric test does **not** prove the user's literal 40% difference for every pair. The high-overlap exceptions remain recorded in `prop_appearance_audit.json` and the highest-priority 16 are shown in `cross_role_review.png`. All models remain AI-assisted references and do not earn self-modeled rubric credit.

The three foliage exports each have one joined renderer: deciduous tree 2,128 triangles and two material slots; coastal pine 4,484 triangles and two slots; flowering shrub 2,024 triangles and three slots. Their modeled leaves and visible boughs replace the earlier faceted sphere/cone crowns. Keep their FBX importer `useFileScale=false` and use the same `Quaternion.Euler(-90,180,0)` instance orientation as the venue models.
