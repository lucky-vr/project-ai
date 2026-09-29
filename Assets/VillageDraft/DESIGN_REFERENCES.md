# VR village reference study

These games informed the redesign of the **VR Village playable reference**. The project uses their broad design lessons, not their copyrighted models, maps, characters, or UI.

| VR game and primary source | Useful observation | Decision for this scene |
| --- | --- | --- |
| [A Township Tale — Alta](https://townshiptale.com/) and its [crafting design note](https://townshiptale.com/news/a-story-about-crafting) | A town is a hub, while the forge, forest, mines, and other professions each have a different purpose and physical loop. | Organize the village into civic, garden, arts, maker, coast, and sky districts. Give each place a readable activity rather than another generic counter. |
| [Garden of the Sea — Neat Corp](https://www.neatcorporation.com/gots) | Island exploration, gardening, and crafting make the outdoor setting part of play. | Make the greenhouse, pool, art garden, and harbor open or semi-open destinations, with water, planting, and outdoor props around paths. |
| [Townsmen VR — HandyGames](https://www.thqnordicmobile.com/en/games/townsmen-vr/) and [official press imagery](https://press.handy-games.com/townsmen-vr-showcasing-the-future-of-medieval-build-up-strategy-329024) | Islands, bridges, winding waterways, and varied landmark buildings make the settlement legible from a distance. | Replace the rigid radial road diagram with an asymmetrical path network, waterfront, bridge, gardens, and unique building silhouettes. |
| [Medieval Dynasty: New Settlement — Spectral Games](https://spectralgames.com/our-games/) | Village work uses hands-on gathering, craft, settlement building, and story objectives. | Use physical placement, aiming, sorting, sequences, and scripted machinery as venue-specific VR tasks and show progress through the village. |
| [Carnival Games VR — 2K](https://www.youtube.com/watch?v=yk5VnRRy4CA) | An alley links clearly themed attractions and rewards play with a shared prize loop. | Treat the village festival as a visible destination, give each venue a distinct attraction frontage, and let its controller-menu action affect a local item. |
| [Pierhead Arcade — Archiact](https://www.archiact.com/pierhead-arcade) | Boardwalk presentation connects several different physical game types through a common setting. | Use one cohesive promenade language while varying the shape, props, and hand actions at each stop. |

## Quality bar for this reference

1. A player at standing eye height can recognize a destination before reading its label.
2. A wide view shows distinct districts and a route through landscape, not a line of identical shop boxes.
3. A visit to one activity should feel spatially and mechanically different from a visit to the next.
4. Models have clean scale, upright orientation, UVs, and material detail in Unity, verified from live captures.
5. The Quest 3S build remains practical: prefer shared materials, static geometry, limited dynamic lights, and a measured on-device performance check.
6. Every visible 3D art mesh comes from a Blender source/export. Unity supplies colliders, XR behavior, changing text, and particle effects.
7. The 15 exteriors pass a material-neutral front/top silhouette audit with at most 0.60 average overlap for any pair; this is the working interpretation of the requested 40% visible difference, in addition to distinct function and façade details.

These criteria are derived from the references as design judgments; the cited games do not prescribe this project's exact layout or mechanics.
