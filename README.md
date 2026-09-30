# VR Village: Festival Reference

A playable Unity VR village reference for the KMITL AR final project. Help prepare a festival by completing activities at **15 different places**. The saved scene includes ten ordered actions per place, two XR controllers, an interaction simulator, teleportable paths, and a world-space menu on the left controller.

![The village's starting area shows the goal and the route to Market](Assets/VillageDraft/Preview/onboarding_start_play.png)

## Open the project

1. Install **Unity 6000.3.19f1** through Unity Hub and install **Git LFS** before cloning. If you already cloned without LFS, run `git lfs pull` in the project folder to fetch the Blender models and other binary assets.
2. Clone this repository and open its root folder in Unity Hub. Let Unity import assets and resolve the packages in `Packages/manifest.json`.
3. Open [`Assets/VillageDraft/Scenes/VillageDraft.unity`](Assets/VillageDraft/Scenes/VillageDraft.unity) and press **Play**. No build is needed to explore the reference in the Editor.

## First five minutes

1. Read the welcome and progress boards where you spawn. The goal is to help all 15 places get ready.
2. Follow the **coral arrows** and Market pennant to **Market**. Its first task is to grab bread and place it in the basket.
3. Read the task board at each place for the current action. Activities include grabbing and placing items, pressing controls, and operating venue-specific mechanisms. Out-of-order presses and placements show a retry hint.
4. Use the **left-controller XR Canvas** to cue a physical item at any of the 15 places. Complete each venue's ten actions to advance the shared progress board. Venues can be visited in any order; Market is only the suggested start.
5. Finish all 15 venues to see the festival completion state and use **Replay** to reset.

In the Editor's XR Interaction Simulator, the view starts in HMD mouse mode. Use **H** for the view and **]** to select the right controller; click to select or press. The simulator's on-screen control panel shows its active grip, movement, and teleport bindings. In VR, use grip to grab, trigger to press, and look at the menu on the left wrist.

![Market's entrance and its current task board](Assets/VillageDraft/Preview/onboarding_market_play.png)

## Scene tools

The **Tools > VR Village** menu contains:

- **Build Draft Scene**: regenerate and save the scene from the Blender assets and C# builder. Use it only when you intend to replace the current generated layout.
- **Validate Draft Wiring** and **Validate Expanded Gameplay**: check the XR rig, venue tasks, interaction references, menu, and completion flow.
- **Audit Blender Visuals**: inspect the origin of visible scene meshes.
- **Exercise Expanded Objectives (Play Mode)**: drive all 150 actions and 15 controller cues. Stop Play mode afterward to discard the exercise's runtime progress.

The last recorded validation passed 150 actions at 15 venues. The visual audit found 1,355 visible Blender mesh renderers and zero visible non-Blender mesh renderers; dynamic text, UI, and particles are excluded. Physical headset comfort, controller reach, and frame rate still need device testing.

## Project files and asset disclosure

- [`Assets/VillageDraft/README.md`](Assets/VillageDraft/README.md): scene features, previews, editing notes, and verification details.
- [`Assets/VillageDraft/DESIGN_REFERENCES.md`](Assets/VillageDraft/DESIGN_REFERENCES.md): VR game references and the design decisions drawn from them.
- [`Assets/VillageDraft/Models/README.md`](Assets/VillageDraft/Models/README.md): Blender source and export structure, regeneration, and appearance audit.
- [`Assets/VillageDraft/Textures/README.md`](Assets/VillageDraft/Textures/README.md) and [`Assets/VillageDraft/Audio/README.md`](Assets/VillageDraft/Audio/README.md): texture credits and audio provenance.

The Blender models are **AI-assisted reference artwork**, not student self-modeled submissions. The course outline requires AI-generated assets to be declared and does not count them toward the eight self-modeled assets per house. See the model README before using this scene for assessment. The requested 40% difference between every pair of assets has not been fully established; the audit records the remaining close pairs.

Keep Unity `.meta` files with their assets. Binary models, images, audio, and Blender sources are tracked with Git LFS; `Library/`, `Temp/`, builds, and other generated Unity files are excluded by `.gitignore`.
