# ZERO // RISE — source-to-Unity migration matrix

Reference browser branch: `zero-rise-v0.7.2`, commit `1e13431899734e3e39ce2102478dbb4840b8d132`.
Native prototype is isolated in `UnityProject/`. The original `dist/` remains the source of truth for game rules until a subsystem passes parity checks.

Status legend: **Foundation** = Unity implementation demonstrable but simplified; **Missing** = no Unity equivalent yet; **Data copied** = coordinate/value references transcribed, not identical rendering.

| Domain | Browser source | Native counterpart | Status | Required parity checks |
|---|---|---|---|---|
| Game state, new story/sandbox | `model.js`, `data.js` | `ZeroRiseState.cs` | Foundation | EUR cents, zero story money, €100k sandbox, needs and clock |
| Save/load | `persistence.js`, `model.js` | `ZeroRiseState.cs` | Foundation, separate format | New save, reload, backup recovery; browser remains untouched |
| City districts/streets | `city-layout.js`, `expansion-layout.js` | `ZeroRiseWorldData.cs` | Data copied; geometry approximate | POI coordinates, road continuity, no major blockers |
| City renderer | `world.js`, `art.js`, `district-architecture.js` | `ZeroRiseWorldView.cs` | Foundation, not visual parity | Screenshot QA, shadows, scale, lights, framerate |
| Materials, environment | `district-materials.js`, `atmosphere.js` | `ZeroRiseVisuals.cs`, `ZeroRiseWorldView.cs` | Foundation (three original textures reused) | Lighting, color correctness, import performance |
| Third-person character | `citizen.js`, `animation.js`, `player-movement.js` | `ZeroRiseVisuals.cs`, `ZeroRiseGame.cs` | Foundation, simplified geometry | Walk/run/jump, camera distance, ground contacts |
| NPC simulation | `pedestrian-life.js`, `pedestrian-scene.js` | `RoamingResident` | Foundation with simplified wandering | Daily schedules, pathfinding, interaction, identity persistence |
| Navigation | `navigation.js`, `street-layout.js` | Map target selection | Missing route A* | Reachability, route detours, waypoint arrival |
| Scavenging | `scavenge.js`, `scavenge-layout.js` | `BottlePickup`, `ZeroRiseGame.cs` | Foundation, no daily reroll/bin search | Seed deterministic layout, full inventory, duplicate prevention |
| Survival/daily actions | `daily-life.js`, `game-time.js` | `ZeroRiseGame.cs` | Foundation with limited actions | Decay per minute, sleep crossing day, hunger/thirst |
| Market/inventory/cooking | `inventory.js`, `shop-interior.js`, `data.js` | `ZeroRiseGame.cs` | Foundation purchases, full inventory Missing | Pricing, checkout, use, storage, recipes |
| Housing | `housing.js`, `home-scene.js`, `home-furnishings.js` | Starter room purchase | Foundation only | Home tiers, fixtures, interior camera, rent |
| Courier | `contracts.js`, `delivery-routes.js`, `courier-scene.js` | One simple delivery | Foundation only | Timers, routes, pay, duplicate prevention |
| Vehicles | `fleet.js`, `movement.js`, `vehicle-damage.js` | `CarVisual`, `ZeroRiseGame.cs` | Foundation basic buy/drive/condition | Dynamics, collisions, maintenance, per-vehicle fleet |
| Tuning/insurance/service | `tuning.js`, `vehicle-upgrades.js`, `vehicle-service.js` | None | Missing | Stats, pricing, preview/rollback/save, repair |
| Traffic and transit | `traffic.js`, `traffic-signals.js`, `transit.js` | None | Missing | AI collision priority, stops, lights |
| Café simulation | `cafe.js`, `cafe-interior.js` | None | Missing | Shift accounting, staff, served orders, save |
| Workshop | `workshop.js`, `workshop-life.js` | None | Missing | Actions, wages, supply loop, animations |
| Business and property | `model.js`, `cafe.js` | None | Missing | Ownership, daily ledger, banking, rent, debt |
| Wardrobe and clothing | `clothing.js`, `citizen.js` | None | Missing | Catalog, try-on, owned/equipped visual parity |
| UI | `app.js`, `style.css` | Unity IMGUI in `ZeroRiseGame.cs` | Foundation, not visual parity | Navigation, legibility, gamepad, menus |
| Weather/sounds | `weather.js`, `soundscape.js` | Time-of-day lighting only | Missing weather/audio parity | Rain, visibility, environment mixes |
| Achievements/storyline | `data.js` quests, `app.js` journal | Initial quest milestones only | Foundation | Completion criteria, rewards, narrative state |

## Acceptance gates (do not bypass)

1. **Editor gate:** Unity 6 imports all files without red Console errors; verify the test menu.
2. **Playable gate:** 10-minute walk; camera, pickup/recycle, buy/use food, job and native save/load.
3. **World gate:** sample core and outer districts, inspect colliders and visibility at normal FPS.
4. **Feature gate:** each migrated JS subsystem passes a deterministic input/output fixture comparison to C#.
5. **Save gate:** explicit migration mapping for all persistent fields, with rollback copies and validation.
6. **Steam gate:** desktop build/start/exit, sustained FPS on target hardware, third-party asset licenses, QA.

**Do not remove the JavaScript implementation until every required domain has reached feature parity.**
