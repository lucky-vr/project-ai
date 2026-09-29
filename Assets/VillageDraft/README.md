# VR Village festival reference

Open `Assets/VillageDraft/Scenes/VillageDraft.unity` in the `project-ai` Unity project, then press Play. The saved scene is a playable VR festival village with an XR Origin, left and right controllers, the XR Interaction Simulator, grab/place interactions, press controls, and teleport surfaces. The [VR reference study](DESIGN_REFERENCES.md) explains the village and carnival games used for design direction.

## Explore and play

The village has 15 destinations across a civic square, old town, gardens and arts area, maker yard, waterfront, and observatory hill: market, café, workshop, greenhouse, pool, bakery, post office, clinic, library, music hall, garage, observatory, harbor, art studio, and recycling depot. Each has a distinct Blender exterior, themed props, activity yard, guide, and ten ordered actions. Completing all 150 actions finishes the shared festival objective and opens a replay control.

The left controller carries a world-space XR Canvas field guide. Its 15 buttons each cue a different physical item at the corresponding venue. In the Editor simulator, mouse view control starts in HMD mode with the menu hidden; select the right controller to reveal and point at the menu. On a headset, the panel stays on the left wrist. The guide, step beacon, venue plaques, and progress board show what to do next. Grab/place items, press local controls, or teleport along the ground and docks.

## Blender art and previews

All visible 3D mesh renderers in the scene come from Blender exports. Unity supplies invisible collision and interaction surfaces, changing text, UI, particles, and behavior. Editable sources are under [`Models/Source/`](Models/Source/) (15 venues), [`Models/EnvironmentFestival/Source/`](Models/EnvironmentFestival/Source/) (landscape and public spaces), and [`Models/GameplayFestival/`](Models/GameplayFestival/) (activity stations and controller frame). The venue kit contains 15 exteriors and 105 themed props; the gameplay kit contains 157 FBX models. Keep the FBX `.meta` files with the exports: their importer scale is set for the intended Unity dimensions.

Visual QA captures: [player start in HMD mode](Preview/final_play_default.png), [the controller menu](Preview/final_play_menu.png), [village overview](Preview/redesign_final_overview.png), [market](Preview/redesign_final_market.png), [harbor](Preview/redesign_final_harbor.png), and [observatory](Preview/redesign_final_observatory.png). These are Unity Editor captures of the saved reference scene.

The outline calls for **10 actions and 8 distinct 3D models per house**. This reference has ten wired actions and at least one exterior plus seven themed venue props at each destination. The [appearance audit](Models/prop_appearance_audit.json) finds all exterior pairs and same-role prop pairs below 0.60 three-view silhouette overlap. Across every possible prop pair, 113 of 5,460 pairs exceed that overlap, so an exact 40% difference for every asset pair is not established by this metric.

## Editor tools and verification

- `Tools > VR Village > Build Draft Scene` regenerates and saves the village hierarchy.
- `Tools > VR Village > Validate Draft Wiring` checks the XR rig, venue objectives, scene references, and interactions.
- `Tools > VR Village > Validate Expanded Gameplay` checks the venue prompts, motion, controller menu, and victory flow.
- `Tools > VR Village > Audit Blender Visuals` scans visible scene meshes and writes `Library/VillageDraftVisualAudit.txt`.
- `Tools > VR Village > Exercise Expanded Objectives (Play Mode)` drives all 150 actions, all 15 controller menu cues, victory, and replay reset. Stop Play mode afterward.

The last Edit mode check found 15 venues, 150 wired actions, 102 grabbable items, and zero visible non-Blender mesh renderers. The Play mode exercise passed all 150 actions, 15 objectives, 15 controller cues, and the festival victory flow. On-device controller reach, comfort, and frame rate have not been measured. No APK build is needed for this reference.
