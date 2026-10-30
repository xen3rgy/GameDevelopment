# Westbruch weather recordings

These are edited real field recordings under CC0. No runtime downloads, noise
generator, audio middleware, music or spoken material are involved. Originals
are cached outside Assets in `QA/weather-sources`. The reproducible edits are
in `Tools/prepare_weather_sfx.py`; measured levels, crop ranges and PCM hashes
are in `RecordingEdits.json`. All shipped clips are 48 kHz mono 16-bit PCM WAV.
Importer peak normalization is disabled, so the deliberately restrained edit
levels survive import.

| Playback file | Recording and author | CC0 source page / downloaded original |
|---|---|---|
| RainRoad.wav | Summer rain on a concrete terrace, Joseph SARDIN | https://lasonotheque.org/pluie-d-ete-sur-terrasse-s1019.html ; https://lasonotheque.org/UPLOAD/bwf-fr/1019.wav → RainTerrace.wav |
| RainRoof.wav | Rain on a car roof, recorded from inside, Joseph SARDIN | https://lasonotheque.org/pluie-sur-toit-voiture-s1293.html ; https://lasonotheque.org/UPLOAD/bwf-fr/1293.wav → RainRoof.wav |
| RainLeaves.wav | Rain on leaves and bushes, SamsterBirdies | https://freesound.org/people/SamsterBirdies/sounds/584272/ ; author's high-quality preview https://cdn.freesound.org/previews/584/584272_5487341-hq.mp3 → RainLeaves.mp3, decoded to RainLeaves.wav |
| Thunder.wav | One short thunderclap without rain, Joseph SARDIN & Axeline T. | https://lasonotheque.org/tonnerre-2-s3113.html ; https://lasonotheque.org/UPLOAD/bwf-fr/3113.wav → Thunder.wav |
| WetTyres.wav | Car passage on a wet street, SamuelGremaud | https://freesound.org/people/SamuelGremaud/sounds/522574/ ; author's high-quality preview https://cdn.freesound.org/previews/522/522574_8031303-hq.mp3 → WetTyres.mp3, decoded to WetTyres.wav |

The Freesound source pages explicitly mark the respective recordings Creative
Commons 0, including modification and commercial use. The preview is an MP3
encoding of the same CC0 recording, not a separate unlicensed music asset.
It is decoded locally to 48 kHz mono PCM using FFmpeg before the offline edit.
The original FLAC/WAV quality is not claimed for those two preview sources.

The close-mic concrete source 1289 was evaluated, then rejected because its
individual drops contain clipped transients. The late, steadier portion of
terrace recording 1019 provides the urban rain bed instead. Loop overlaps are
1.5 seconds for the three rain layers and 350 ms for the short tyre texture.
The wet-tyre layer keeps only the broadband 1.9–7.6 kHz water/contact texture;
actual game vehicle speed, WheelCollider contact and position control playback.
It never plays an unrelated recorded pass-by through the world.

Additional existing CC0 assets are reused without making duplicate files:

- `../GameSfx/LeavesAir.wav`: real wind in a tree by Joseph SARDIN,
  https://lasonotheque.org/vent-et-grincement-arbre-s1051.html .
- `../GameSfx/DrainDrop0..2.wav`: recorded water drops by Joseph SARDIN,
  https://lasonotheque.org/gouttes-eau-3-s1386.html .

Runtime behavior: the existing ambience and vehicle sliders control weather
voices; the master listener remains owned by the existing soundscape. Rain,
wind and leaf layers crossfade in real time. Five upward roof queries muffle
rain under shelter. Metal roof taps play only for metal cover or the exposed
occupied car, not concrete bridge cover. Nearby individual shrub crowns
control leaf-rain presence. Wet tyre hiss needs actual moving ground contact,
hard surface, outdoor road and the existing nearby-traffic voice budget.
Water draining from sheltered eaves persists with accumulated water after rain
and disappears as that water dries. No gait-timed repeated drip is added.

Storm events are spaced in real seconds regardless of test clock speed: the
first flash after 24–42 seconds of storm, then 45–95 seconds between events.
The single flash callback precedes sound by 0.9–3.5 seconds. Switching away
from storm cancels a pending clap. Thunder is a restrained distant event,
never a repeating loop or sudden maximum-volume jump scare.
