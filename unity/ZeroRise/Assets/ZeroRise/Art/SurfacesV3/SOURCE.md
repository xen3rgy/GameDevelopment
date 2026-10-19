# Westbruch material and foliage set V3

Two original raster atlases were generated for this update using the built-in `image_gen` tool and the `imagegen` skill. The user's reference pictures guided the worn European residential/industrial look; they are not shipped textures.

`tools/build-unity-materials.cjs` deterministically extracts the nine surface tiles, matches the narrow tile borders, and derives grayscale height and HDRP mask maps. Tile files are stored at 1024 pixels; their original atlas cells are about 412 pixels, so these are not native 1K scans. Heights, AO and roughness are artistic approximations derived from the diffuse image, not measured PBR data. Unity converts the height images into normal maps. The foliage atlas preserves its genuine transparent alpha and is sampled on cached intersecting mesh cards.

Surface atlas: `material-atlas-source.png`. Consumed tiles: asphalt, paving, yard, grass, plaster, brick, concrete, sand, mud. Foliage: `urban-foliage.png`. Existing timber/steel and the base tree atlas retain their previous source notes.

The complete generation prompts are saved in `PROMPTS.md` alongside these assets. No paid external asset packs or downloaded models were used for this update.
