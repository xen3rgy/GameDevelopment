# ZERO // RISE — recorded gameplay sound FX

All source recordings below are CC0 and permit modification, commercial use
and redistribution. Edited recordings are shipped as 48 kHz mono WAVs. The
source cache is under `QA/sfx-sources`, outside the shipped game. Reproducible
edits: `Tools/prepare_game_sfx.py` and `Editor/GameplayAudioAssets.cs`.
`RecordingEdits.json` records source/time ranges, filtering, loop overlaps and
measured levels. AudioImporter uses mono, preload, decompressed playback and
Vorbis quality 0.85. No external audio middleware or runtime generation.

## Recording provenance

Joseph SARDIN / LaSonotheque, each linked page explicitly marked CC0:

| Source | Original page | Download / local cache |
|---|---|---|
| Stationary Peugeot 107 1-litre petrol engine, start/idle/stop | https://lasonotheque.org/moteur-essence-1-litre-s0966.html | https://lasonotheque.org/UPLOAD/bwf-fr/0966.wav → EngineStationary.wav |
| Fast shoe footsteps on concrete | https://lasonotheque.org/pas-rapides-sur-beton-s1318.html | https://lasonotheque.org/UPLOAD/bwf-fr/1318.wav → FootConcrete.wav |
| Shoe footsteps on gravel | https://lasonotheque.org/pas-sur-gravier-2-s1117.html | https://lasonotheque.org/UPLOAD/bwf-fr/1117.wav → FootGravel.wav |
| Shoe footsteps in short grass | https://lasonotheque.org/pas-dans-l-herbe-courte-s0854.html | https://lasonotheque.org/UPLOAD/bwf-fr/0854.wav → FootGrass.wav |
| Physical movement of Chinese silver grass leaves | https://lasonotheque.org/roseau-de-chine-1-s1811.html | https://lasonotheque.org/UPLOAD/bwf-fr/1811.wav → BushLeaves.wav |
| Peugeot 107 starts, drives, brakes, stops | https://lasonotheque.org/voiture-sur-route-s1286.html | https://lasonotheque.org/UPLOAD/bwf-fr/1286.wav → CarDrive.wav |
| Peugeot 107 door open/close, exterior | https://lasonotheque.org/portiere-de-voiture-4-s1526.html | https://lasonotheque.org/UPLOAD/bwf-fr/1526.wav → CarDoor.wav |
| Car horn | https://lasonotheque.org/klaxon-de-voiture-recente-2-s0258.html | https://lasonotheque.org/UPLOAD/bwf-fr/0258.wav → CarHorn.wav |
| Tyre skidding | https://lasonotheque.org/crissement-de-pneus-1-s2368.html | https://lasonotheque.org/UPLOAD/bwf-fr/2368.wav → TyreSkid.wav |
| Synthetic-fabric coat rubbed by hand (a real recording of clothing) | https://lasonotheque.org/manteau-synthetique-frotte-s1104.html | https://lasonotheque.org/UPLOAD/bwf-fr/1104.wav → Cloth.wav |
| Water drops in a dish | https://lasonotheque.org/gouttes-eau-3-s1386.html | https://lasonotheque.org/UPLOAD/bwf-fr/1386.wav → Drain.wav |
| Sparrows settling in a hazel tree | https://lasonotheque.org/50-oiseaux-se-couchent-s1068.html | https://lasonotheque.org/UPLOAD/bwf-fr/1068.wav → Sparrows.wav |
| Wind and a branch moving in a tree | https://lasonotheque.org/vent-et-grincement-arbre-s1051.html | https://lasonotheque.org/UPLOAD/bwf-fr/1051.wav → WindTree.wav |

DenisChardonnet / LaSonotheque, CC0:

- Metal gate creak: https://lasonotheque.org/grincement-de-portail-en-metal-1-s0303.html
- https://lasonotheque.org/UPLOAD/bwf-fr/0303.wav → Fence.wav.

Kenney **Impact Sounds (1.0)**, CC0, retained `Kenney-License.txt`:

- https://kenney.nl/assets/impact-sounds
- https://kenney.nl/media/pages/assets/impact-sounds/87b4ddecda-1677589768/kenney_impact-sounds.zip
- Selected concrete, grass and wood footsteps (000–004), plate, soft and metal
  impacts. `Stepmetal*` combines recorded concrete footfalls with a quiet
  recorded plate resonance. `ImpactBody` and `SeatSettle` use the corresponding
  recorded material impacts. Originals retain their filenames; the normalized
  playback versions are WAVs. These are designed foley, not recordings of a
  destructive car crash.

## Gameplay coverage

- Own car: separate ignition, idle/load layers, audio gear changes, speed- and
  contact-dependent road tyres, actual wheel-slip scrub, physical collision
  foley, actual hinged-door open/latch/close, engine shutdown and H horn.
- Nearby traffic: engine and rolling layers follow real car positions and
  speeds, including idling queues. Four nearest cars are voiced; distant cars
  fade out. No fabricated horn spam or recorded phantom pass-bys.
- Character: material footfalls, gait/speed dynamics, no immediate sample
  repeats, quiet jump movement and weighted landing contacts. Steady walking
  has no additional repeating clothing layer. Leaf movement follows actual
  contact with individual shrub crowns, not plant proximity or a footfall timer.
- Residents: nearby actual walking gait contacts and restrained clothing while
  arriving/leaving. No invented spoken dialogue or door opening that has no
  corresponding visible action.
- Existing world: recorded wind/leaves, occasional birds at existing tree
  pockets, recorded drainage and fence details, retained real shop compressor
  and remote city bed. Barriers muffle sounds; underbridge reflections are short.
- Mix: master, vehicles, actions, steps and ambience controls, remembered in
  PlayerPrefs. Original scene lighting, navigation and physics are retained.

The audio-only gear/RPM model observes the existing simple powertrain. It does
not add a gearbox, modify torque or claim to reproduce the pictured vehicle's
exact engine model. No music, voices or fictional moving train are introduced.

## Feedback corrections, 22 October 2026

The engine loops now use stable sections of the stationary engine recording
0966. The load layer is a complementary filtered engine texture; the source
does not contain a separate verified load/RPM sweep. The road layer retains
higher broadband contact noise from 1286, with a 2100 Hz highpass to remove the
previous dominant engine/cabin resonance. Engine and road settings are recorded
in `RecordingEdits.json`.

`ShoeConcrete0–7`, `ShoeGravel0–7` and `ShoeGrass0–7` are individually selected
contacts from the real walking recordings 1318, 1117 and 0854. Selection uses
the filtered contact envelope and decay, rather than the eight loudest raw
peaks. Crops are 220 ms for concrete and 250 ms for gravel/grass, with 12 ms
edge fades. Secondary late impacts and the next foot scuff are excluded.
Concrete playback files measure RMS 0.050; gravel and grass measure RMS 0.045.
These conservative targets keep the takes consistent without compression or
amplifying isolated sharp peaks.

`BushRustle0–2` use three complete, overlapping 550 ms windows from the smooth
physical leaf movement in recording 1811. They retain different parts of that
single movement, with 55 ms fades and RMS 0.065. The quiet end and isolated
twig clicks are excluded. The preparation script rejects incomplete crops for
these footfalls and rustles, so a requested window cannot silently run beyond
the source. This is offline recorded-foley processing; no synthetic noise or
additional runtime audio generator is used.
