# ZERO // RISE — Unity 6 migration prototype

**Status: playable technical prototype, NOT a full Unity port of the browser game.**

Open **this `UnityProject` folder** in Unity Hub with Unity 6 (6000.x); do not open the repository root as a Unity project.

## Quick start

1. In Git, check out branch `zero-rise-unity-prototype`.
2. Install a Unity 6 Editor (6000.x). The version written to ProjectSettings is 6000.0.60f1; compatible later Unity 6 patches may ask for an Editor upgrade.
3. Unity Hub → **Add project from disk** → select the repository's `UnityProject` directory.
4. In the Unity Editor, open `Assets/Scenes/ZeroRise.unity`.
5. Press **Play**. The bootstrap creates the world automatically.
6. Choose **Bei null beginnen** or **Freies Spiel (100.000 €)**.

If Unity cannot open the minimal pre-generated scene, select **ZERO // RISE → Rebuild Empty Bootstrap Scene** from the editor menu. Then press Play. No imported asset store assets or third-party C# packages are required.

### Controls

| Key | Action |
|---|---|
| WASD | Move / drive |
| Right mouse button + mouse | Orbit third-person camera |
| Left Shift | Sprint |
| Space | Jump / brake while driving |
| E | Interact with nearby marker/item |
| F | Enter / exit owned vehicle |
| 1 / 2 | Eat / drink |
| M | Large city map |
| Esc | Pause / close menu |
| F5 | Save |
| F9 | Load |

Use Unity Game view at 16:9 if possible. Inputs currently use Unity's legacy Input API: if the Editor reports an Input System error, change **Project Settings → Player → Active Input Handling** to **Both** and restart Unity.

## What is genuinely implemented

- **Separate Unity project** under `UnityProject/`; original Three.js/JavaScript game is still intact in `dist/` on the same branch.
- Generated native Unity city prototype with core + outer borough coordinates transcribed from `dist/city-layout.js`, `dist/data.js`, and `dist/expansion-layout.js`.
- Playable character with CharacterController, mouse-driven third-person camera, jump and a basic procedural character rig.
- Named districts, roads, building approximations, trees, simple roaming NPCs and a map with location selection.
- Pick up bottles, deposit them at the original recycle coordinates, buy food at MARKT 24, consume food/water, pay for shelter, rent a starter room, take and deliver one simple courier route.
- Buy and drive one vehicle per save (bike/car/van/sport), simple fuel/condition use and smoke on low condition.
- Clock, needs, cash in **integer cents**, HUD, basic quest milestones, bank deposits/withdrawals.
- Separate, local Unity JSON save with backup, Autosave/F5/F9.
- Unity Editor **ZERO // RISE → Validate Unity Foundation** menu, basic save JSON roundtrip check.

## What is NOT migrated yet

**Do not interpret this prototype as 1:1 feature parity or a finished Steam build.**

- Full café service, staffing, workshop, detailed courier contracts, job progression, business simulation.
- Existing Three.js interiors, clothing/wardrobe UI and geometry, inventory weights/cooking, full housing tiers.
- Vehicle insurance, detailed tuning, damage/repair economics, AI traffic, proper transit/timetables.
- The exact source layout geometry, high-fidelity art direction, glTF/GLB scene assets, custom textures and PBR pipelines.
- Original collision and A* navigation, advanced NPC routines and dialogue, dozens of source save fields.
- Robust native quest parity: the prototype only covers initial milestones.
- Complete browser save migration. Unity stores its own save **separately**; no browser localStorage is touched.
- Verified Unity Editor compilation and a successful Unity Player build: a Unity Editor is **not available in the generation environment**.

The port reuses the browser project's existing brick, limestone and meadow PNG textures. It still uses **proxy geometry**. It is meant to establish a testable Unity architecture and coordinate system, not yet to improve visual quality above the browser game.

## Browser save import — LIMITED

Export your old JavaScript save JSON manually. In Unity choose **ZERO // RISE → Import Browser Save (BASIC FIELDS ONLY)**. This only transfers money, day, clock, needs, bank/debt, outdoor X/Z and quest index. Missing features are not represented; **never rely on this to replace a full browser game save**.

Native Unity saves live under `Application.persistentDataPath/zero-rise-unity-v1.json`, not in `dist/` or browser localStorage.

See [MIGRATION_MATRIX.md](MIGRATION_MATRIX.md) for the exact source modules, implemented/native features and remaining parity gates.\n\n## Recommended next development order

1. Open in Unity 6, fix any Editor-version compile or import errors; capture Console logs.
2. Verify 10-minute manual gameplay smoke test (walk, interact, camera, save/reload, drive, map).
3. Replace procedural proxy buildings with licensed modular 3D assets; set up URP and test renderer on target GPUs.
4. Build deterministic asset/coordinate exporters from JS city data, rather than manually copying visuals.
5. Port one economy subsystem at a time into testable C# domain modules, comparing numeric results to original JS fixtures.
6. Only consider shipping on Steam after parity tests, optimized content streaming, UI accessibility, installer/build tests and proper rights for every third-party asset.

### Steam

Steam does not require Unity. A sale-ready Unity game needs considerably more work beyond this scaffold. Maintain the existing browser branch as the authoritative functional reference until feature-complete parity is independently verified.
