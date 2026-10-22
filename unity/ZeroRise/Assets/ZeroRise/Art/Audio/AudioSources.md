# Westbruch atmosphere audio

The following source recordings/assets are CC0. They may be used commercially and modified. Source files are retained for reproducible editing; the scene uses the quieter, filtered, seamless derived clips in this folder.

* `Sources/CityOriginal.mp3`: **Périphérique parisien #1**, Joseph SARDIN / LaSonotheque. https://lasonotheque.org/peripherique-parisien-s1080.html — original page explicitly states CC0/public domain, modification, redistribution and commercial use permitted. Download: https://lasonotheque.org/UPLOAD/mp3/1080.mp3 . Derived `CityDistant.wav`: low-pass filtered, reduced road/cabin transients, smooth loop seam, restrained level.
* `Sources/WindOriginal.wav`: **wind1**, Luke.RUSTLTD, OpenGameArt, CC0. https://opengameart.org/content/wind1 . Download: https://opengameart.org/sites/default/files/wind1.wav . Original is synthesized wind (PureData/Andy Farnell tutorial), not a field recording. Derived `CourtyardWind.wav`: band-limited, rebalanced and looped.
* `Sources/FridgeOriginal.wav`: **The Shop**, LEGIT Audio, free CC0 samples on OpenGameArt. https://opengameart.org/content/the-shop . Download archive: https://opengameart.org/sites/default/files/legit_audio_-_the_shop_free_sfx_wav.zip . Only the free OpenGameArt sample is used. Derived `ShopCompressor.wav`: short filtered loop, played spatially from the existing grocer.

`DrainResonance.wav`, `FenceTick.wav`, and the `Footstep*.wav` variations are original deterministic modal/noise synthesis for ZERO // RISE, generated offline by `WestbruchAudioAssets.cs`. No speech, music or train-pass recording is used. The empty freight line never produces a phantom passing train.

Audio is mixed by `WestbruchSoundscape`: outdoor wind/remote traffic, sheltered attenuation, spatial grocer machinery, infrequent drainage/fence detail and surface-aware player footsteps. It does not change locomotion, traffic or NPC decisions. Sound controls are available while the cursor is released with Esc.
