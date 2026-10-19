# Urban ground, road and vegetation rendering

## Sculpted district ground

`UrbanGround.shader` and `UrbanGroundSplatmap.hlsl` derive the raster terrain
passes from the same pinned HDRP 17.6 package, under `HDRP-LICENSE.md`.
Regenerate both with `node tools/build-unity-ground-shader.cjs` (optional package
directory argument). The generator checks the pinned hooks and fails if they
change. It keeps native terrain instancing, height blending, lighting, normals,
depth and shadow passes, but removes the unused ray tracing subshader.

Surface color, normal and mask samples share three hash-offset texture reads,
triangular blending and coordinate warping. Texture derivatives remain stable
across hash boundaries; control-map coordinates stay fixed so painted materials
do not swim or move between parcels. Macro color variation is in metre space.
The four existing derived surface textures remain artistic PBR approximations,
not measured scans. Stochastic sampling reduces obvious repeat patterns; it
does not create new source imagery. The terrain basemap distance exceeds the
prototype camera's far plane so the default distant basemap cannot reintroduce
the original tiling in playable views.

Actual relief and collision come from a native 1025-square `TerrainData` asset,
not from a color image or shader-only displacement. Smooth circulation masks
are built from the same 46 connected paths used for the visible paving. Low
mounds, swales, soil folds and flat bearing pads are baked once. Existing
vegetation is subsequently placed at these new ground heights.

`WestbruchGroundDetails` bakes 12,000 embedded pieces from twelve cached gravel,
rubble and clod variants into at most 48 authoring chunks / 220,000 triangles.
These supply real silhouettes and shadows without per-piece objects, colliders
or per-frame mesh changes. Source chunks remain editable and are included in
the regular scene static batching. `WestbruchGroundAsset.prefab` saves the
Terrain and relief together before that scene batching pass; its TerrainData
and materials are shared with the scene. No new dynamic lights are added.

`WestbruchGroundQA.RebuildAndCapture` checks the native terrain, circulation,
triangle budget, foliage support/scale and shader, then renders ten actual 2048 x 1152 HDRP ground views.
`WestbruchGroundQA.Play` measures traversal, support, standing and orbit collision
on six real terrain routes at 30 and 60 Hz. The opt-in Windows smoke check
also verifies that the sculpted terrain and supported ground shader ship in
the executable. Raster cost is higher than one unmodified terrain sample;
production performance still needs hardware profiling.

## Roads and vegetation

`WestbruchGroundCover` is the shared authoring field for both the four terrain
layers and the flora. Protected building/tree edges and damp pockets support
unequal colonies; compacted circulation and gravel retain bare breaks. Actual
tree positions update the paint field after canopy clearance, and flora is
placed after courtyard props. Its final yaw, terrain alignment, nonuniform
scale and off-centre mesh bounds participate in the clearance tests.

`WestbruchFloraMeshes` generates six botanical silhouettes with three variants
each in real metres. Short grass uses 8–10 thin curved folded blades; leaves,
rosettes, sparse seed heads and woody branches have actual spatial geometry.
Microscopic grass thickness is omitted where it adds no useful silhouette;
the material is double-sided. Other leaves retain their closed lamina. Muted
artist-space olive/straw/bark tones are converted to linear vertex colors,
with darker roots and lighter tips. One neutral `DistrictFlora` material is
shared across all plants. The saved district has 39,109 clumps, 18 source meshes,
5.8 million triangles and 64 spatial render chunks; no plant colliders or per-plant
runtime scripts are added. This is authored procedural vegetation, not scanned
botanical assets. The former oversized four-type vegetation generator is removed.

`UrbanLit.shader` derives the raster passes from the pinned HDRP 17.6 Lit shader.
Unity's license is preserved in `HDRP-LICENSE.md`. It retains HDRP lighting,
shadow, depth, normal and motion-vector passes; no ray tracing is required.
Regenerate the wrapper with `node tools/build-unity-urban-shader.cjs` when
updating the pinned render pipeline (an optional argument supplies its package
directory), then regenerate and validate Westbruch in Unity.

`UrbanLitData.hlsl` extends those passes. Asphalt uses absolute world-space
coordinates, three coherently blended hash-offset samples for color/normal/mask,
gentle coordinate warping and large-scale tonal variation. Surface texture
pixels are still reused; the prior visibly identical road-cell tiling is removed.
`WestbruchRoadWear` bakes seeded, individually shaped crack/branch ribbons and
irregular repair polygons. These are visual surfaces without new colliders.

Plant types use shared folded blade meshes or curved stalks with individual
folded leaves, not intersecting cards showing a whole plant. Each baked render
chunk stores absolute root X/Z in UV channel 3 and root Y/bend weight in channel
2. Keep these channels when changing mesh batching/import settings. Source
plant objects remain hidden authoring geometry; regenerate chunks after moving
them. Conservative chunk bounds include bending, and normals/tangents rotate
with the foliage. Animated plants should not be baked into static lightmaps.

One `VegetationMotion` component records bounded histories for the player and
car. The shared GPU deformation adds subtle wind, swept contact, eased bending
and recovery with rooted bases. Separate actor bounds keep distant vegetation
out of the contact loop. There are no per-plant scripts, colliders, frame-by-frame
mesh changes or plant physics forces. This is visual vegetation response, not a
full botanical/cloth simulation.

`WestbruchPlayQA.Run` verifies the actual shared deformation on the GPU using
saved chunk metadata and a CharacterController walk. It also renders the shipped
plant meshes through the actual HDRP shader in resting/contact/recovered states.
The compute probe and render stage are Editor-only test tools, excluded from
normal gameplay and Windows builds.

## Branched trees and courtyard litter

`WestbruchTreeMeshes` authors four original broadleaf silhouettes with connected
curved trunk/fork topology, a rounded root collar and separate folded leaf laminae. Separate pointed root tubes are removed.
The four foliage atlas tiles vary the leaf shapes. Each tree has two shared
geometry LODs with stable root origins, separate bark/leaf submeshes and padded
wind bounds. Near trees use 68,688–85,776 triangles; reduced trees use
27,424–30,828. Actual transformed crowns are checked against buildings before
placement, and only the trunk has collision. Source meshes and four reusable
prefabs remain editable; no tree geometry is built during play.

`WestbruchNatureMaterials` packs the generated leaf quadrants with transparent
gutters into a 2048-square atlas and derives normal/mask maps from the leaf,
bark and earth images. Source images are 1254 square, not native 2K scans;
`Nature2K/SOURCE.md` records provenance and exact prompts. Alpha-tested foliage
preserves mip coverage. The `_ZR_TREE` shader variant applies gentle spatial
crown wind to both bark and leaves with fixed roots, consistent depth/shadow
passes and vertex tints. It does not use the low-plant player contact metadata.

`WestbruchYardLitter` places individual curled fallen leaves, connected twig
segments and embedded chips against the actual Terrain height, then bakes them
into at most 48 source chunks / 250,000 triangles. Canopy litter follows only
actual tree roots, not shrub LOD groups. Fallen leaves use a shared muted brown
cutout material without canopy wind. No litter colliders are added.
`WestbruchCourtyardDetails` adds clustered low shrubs and a supported seating
corner. A native soil shoulder smooths the transition from paving to earth. Its worn approach joins the existing paved path through the same shared
wear field used for ground paint and plant exclusion. Shrub bounds participate
in circulation/prop clearance. Only the seating has physical collision; soft shrub groups are entirely passable.

Shrubs use `_ZR_SHRUB` with separate shared bark/leaf materials. Their object
origin supplies the planted world root, and a smooth height-dependent bend
samples the existing swept contact history at nearby branch positions. This
lets different portions of the crown yield without moving the base. Normals,
tangents, depth, shadows and motion-vector passes use the same deformation.
Small tip flutter remains after contact, with eased recovery as the history
expires. Shrub objects must remain nonstatic and outside transform batching;
the shared mesh has conservative padded bounds for bending. No new runtime
plant components, physics bodies, colliders or per-frame mesh updates are used.
`WestbruchShrubQA` exercises real character crossings and probes this same HLSL
function on the GPU, then captures the shipped mesh/material response through
HDRP.

## Earth support and street furniture

`WestbruchLandscape.OnEarth` combines the circulation masks with nine actual
physics support rays around each footprint. Dressing and street furniture are
built before trees, so original sites and relocation candidates also respect
existing hard surfaces and obstacles. Roots use the native terrain height;
no plant is snapped onto paving by the generic surface-height helper. Ground
flora covers the eastern parcels as well as the original western courtyards.
Fifty-one soft shrubs are grouped across all four map quadrants, sharing two
cached source meshes per LOD and the existing contact materials. Far geometry
retains distributed leaf cards; there are no per-frame CPU mesh changes.

`WestbruchStreetFurniture` places 33 lamps at actual pavement edges facing
into their roads. Junctions, entrance approaches, existing objects, adjacent
poles and the railway span are excluded. Tapered round poles and curved round
arms are cached mesh assets, with shared rounded housings, frosted emissive
lenses, maintenance covers and small foot collars. Each pole has one narrow
capsule; the lamp pass adds no dynamic lights.

Public bench collision now follows the real timber seat surface, three back
slats and two legs. The seat collider bridges only the small slat gaps; it does
not fill the air to the top of the backrest. Visible back uprights and seat
bearers complete the existing bench model. Scene QA compares seat collision
height against the actual source mesh bounds, and Play QA lands the actual
player capsule on all 22 benches and checks steady support with the unchanged
controller skin. Runtime character/vehicle controllers are unchanged.
