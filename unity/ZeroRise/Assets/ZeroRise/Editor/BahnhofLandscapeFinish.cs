using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    // The neighbourhood has a foreground, enclosed yards and a continuous
    // landscape beyond them. Nothing here is a camera-facing map-edge screen.
    internal static class BahnhofLandscapeFinish
    {
        const string Name = "BV3_Landscape";
        static Transform district, root;
        static Material brick, coping, iron, timber, roof, earth, grass;
        static GameObject treeSource, shrubSource;
        static GameObject[] treeVariants;
        static int treeCount, shrubCount, grassCount;
        static readonly List<Vector3> treeSites = new List<Vector3>();

        internal static void Apply(Transform neighbourhood)
        {
            district = neighbourhood;
            var old = neighbourhood.Find(Name);
            if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            root = Group(Name, neighbourhood, Vector3.zero).transform;
            treeCount = shrubCount = grassCount = 0; treeSites.Clear();
            brick = Material("BV3_YardBrick", "brick", new Color(.67f, .62f, .54f), .10f);
            coping = Material("BV3_YardSandstone", "concrete", new Color(.62f, .63f, .58f), .08f);
            iron = Material("BV3_YardIron", "steel", new Color(.22f, .27f, .25f), .19f, .35f);
            timber = Material("BV3_YardTimber", "wood", new Color(.45f, .42f, .32f), .08f);
            roof = Material("BV3_YardRoof", "asphalt", new Color(.25f, .29f, .30f), .10f);
            earth = BahnhofDetailKit.Load("OverpassGround_LivingEarth");
            grass = BahnhofDetailKit.Load("DistrictFlora");
            var lods = UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None);
            treeVariants=lods.Where(t=>t.name.StartsWith("Urban tree ")).GroupBy(t=>t.GetComponentInChildren<MeshFilter>().sharedMesh).Select(g=>g.First().gameObject).ToArray();
            treeSource = lods.FirstOrDefault(t => t.name.StartsWith("Urban tree "))?.gameObject;
            shrubSource = lods.FirstOrDefault(t => t.name.StartsWith("Dense low courtyard shrub"))?.gameObject;

            RemoveTemporaryBoundary();
            ContinueLandscape();
            RepairBankAndFoliage();
            RetainedStationGarden();
            ArrivalStreet();
            RearGardens();
            DistantWorkingNeighbourhood();
            NorthernCourt();
            RepairBankAndFoliage(false);
            GreenerSoil();
            Physics.SyncTransforms();
            PlantGroundCover();
            RegroundExistingTrees();
            Debug.Log($"Station landscape finished: {treeCount} mature trees, {shrubCount} rooted shrubs, {grassCount} ground plants; supported bank and continuous context land.");
        }

        static void RemoveTemporaryBoundary()
        {
            foreach (var t in district.Cast<Transform>().Where(t => t.name == "Station rear property boundary" || t.name == "Rail service storage" || t.name == "Railway neighbourhood perimeter tree").ToArray())
                UnityEngine.Object.DestroyImmediate(t.gameObject);
        }

        static bool NewContext(float x, float z)
            => x >= 300 && x <= 1800 && z >= -1800 && z <= 1800
               || x >= 134 && x < 300 && z >= 364 && z <= 1800
               || x >= 166 && x < 300 && z >= 30 && z < 58
               || x >= 200 && x < 300 && z >= -1800 && z < 30;

        static float ContextHeight(float x, float z)
        {
            float depth = Mathf.Max(x - 300, z - 364, 30 - z, 0);
            if (z < 58) depth = Mathf.Max(x - 300, 0);
            float rise = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(24, 220, depth));
            // Keep the rail-side foot level while the distant land rises gently
            // behind it. This avoids a second exposed edge alongside the track.
            rise *= Mathf.SmoothStep(0, 1, Mathf.InverseLerp(134, 185, x));
            float land = Mathf.PerlinNoise(x * .011f + 19, z * .012f + 7) * 8;
            return -.055f + rise * (3.5f + land);
        }

        static void ContinueLandscape()
        {
            // Bury the old heightmap under the new continuous apron only beyond
            // the station development. The original residential district is untouched.
            var terrain = Terrain.activeTerrain;
            if (terrain)
            {
                var data = terrain.terrainData; int n = data.heightmapResolution;
                var h = data.GetHeights(0, 0, n, n); var origin = terrain.transform.position;
                for (int iz = 0; iz < n; iz++) for (int ix = 0; ix < n; ix++)
                {
                    float x = origin.x + ix / (n - 1f) * data.size.x;
                    float z = origin.z + iz / (n - 1f) * data.size.z;
                    if (NewContext(x, z)) h[iz, ix] = Mathf.Max(0, (ContextHeight(x, z) - .20f - origin.y) / data.size.y);
                }
                data.SetHeights(0, 0, h); EditorUtility.SetDirty(data);
            }

            // Remove the obsolete distant railway apron wherever this continuation
            // now owns the ground. Both its retained source and rendered batch are cut.
            var bridge = GameObject.Find(WestbruchBridgeStart.BridgeName);
            if (bridge) foreach (var f in bridge.GetComponentsInChildren<MeshFilter>().Where(f => f.name == "Land below distant railway 1" || f.name == "Local render batch OverpassGround_DistantGround"))
            {
                var m = f.sharedMesh; if (!m || !m.isReadable) continue;
                var v = m.vertices; var t = m.triangles; var keep = new List<int>();
                for (int i = 0; i < t.Length; i += 3)
                {
                    bool replaced = false;
                    for (int j = 0; j < 3; j++) { var p = f.transform.TransformPoint(v[t[i + j]]); replaced |= NewContext(p.x, p.z); }
                    if (!replaced) keep.AddRange(new[] { t[i], t[i + 1], t[i + 2] });
                }
                m.SetTriangles(keep, 0); m.RecalculateBounds(); EditorUtility.SetDirty(m); RefreshCollider(f, m);
            }

            GroundPatch("East continued landscape", 300, 600, 30, 600);
            GroundPatch("North continued landscape", 134, 300, 364, 600);
            GroundPatch("South continued landscape", 166, 300, 30, 58);
            GroundPatch("South horizon land", 200, 600, -1800, 30, 32);
            GroundPatch("Far east horizon land", 600, 1800, -1800, 1800, 32);
            GroundPatch("Far north horizon land", 134, 600, 600, 1800, 32);
            ContinueNorthwestLandscape();
        }

        static float NorthwestHeight(float x, float z)
        {
            var terrain = Terrain.activeTerrain;
            float north = terrain.transform.position.z + terrain.terrainData.size.z;
            float sampleX = Mathf.Clamp(x, terrain.transform.position.x, 44);
            float edge = terrain.SampleHeight(new Vector3(sampleX, 0, north)) + terrain.transform.position.y;
            float blend = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0, 40, z - north));
            float baseline = Mathf.Lerp(edge, .12f, blend);
            if (x >= 44 && z >= 619.999f) return Mathf.Lerp(.12f, -.055f, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(130.4f, 134, x)));
            float rise = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(60, 350, z - north));
            rise *= Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0, 180, 44 - x));
            return baseline + rise * (4 + Mathf.PerlinNoise(x * .009f + 3, z * .011f + 8) * 12);
        }

        static void ContinueNorthwestLandscape()
        {
            var terrain = Terrain.activeTerrain; if (!terrain) return;
            float north = terrain.transform.position.z + terrain.terrainData.size.z;
            float west = terrain.transform.position.x;
            float[] Axis(float a, float b, int count) => Enumerable.Range(0, count + 1).Select(i => Mathf.Lerp(a, b, i / (float)count)).ToArray();
            // The near boundary shares every heightmap sample. At x44 it shares
            // all 100 rows of the retained railway apron, so neither join floats.
            var nearX = new SortedSet<float>(Axis(-600, west, Mathf.CeilToInt((west + 600) / 24)));
            for (int i = 0; i < terrain.terrainData.heightmapResolution; i++)
            { float x = west + i / (terrain.terrainData.heightmapResolution - 1f) * terrain.terrainData.size.x; if (x <= 44) nearX.Add(x); }
            nearX.Add(44);
            var nearZ = Axis(north, 620, 100);
            var farZ = Axis(620, 1800, 38);
            var topX = new SortedSet<float>(nearX);
            for (int i = 0; i <= 25; i++) { float x = 44 + 148 * i / 25f; if (x < 134) topX.Add(x); }
            topX.Add(134);
            NorthwestPatch("Northwest soil at terrain edge", nearX.ToArray(), nearZ);
            NorthwestPatch("Northwest distant landscape", topX.ToArray(), farZ);
            NorthwestPatch("West horizon beyond rail apron", Axis(-1800, -600, 38), nearZ.Concat(farZ).Distinct().OrderBy(v => v).ToArray());
        }

        static void NorthwestPatch(string id, float[] xs, float[] zs)
        {
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var colors = new List<Color>(); var indices = new List<int>();
            int nx = xs.Length - 1, nz = zs.Length - 1;
            for (int iz = 0; iz <= nz; iz++) for (int ix = 0; ix <= nx; ix++)
            {
                float x = xs[ix], z = zs[iz]; vertices.Add(new Vector3(x, NorthwestHeight(x, z), z)); uv.Add(new Vector2(x, z));
                colors.Add(new Color(.70f + Mathf.PerlinNoise(x * .051f + 4, z * .046f + 9) * .25f, .025f, 0, 1));
                if (ix == nx || iz == nz) continue; int a = iz * (nx + 1) + ix;
                indices.AddRange(new[] { a, a + nx + 1, a + 1, a + 1, a + nx + 1, a + nx + 2 });
            }
            var m = new Mesh { indexFormat = IndexFormat.UInt32 }; m.SetVertices(vertices); m.SetUVs(0, uv); m.SetUVs(1, uv); m.SetColors(colors); m.SetTriangles(indices, 0); m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
            AddMesh("BV3_Ground " + id, m, earth, true);
        }

        static void GroundPatch(string id, float x0, float x1, float z0, float z1, float spacing = 5)
        {
            float[] Axis(float a, float b, float step)
            { int n = Mathf.CeilToInt((b - a) / step); return Enumerable.Range(0, n + 1).Select(i => Mathf.Lerp(a, b, i / (float)n)).ToArray(); }
            var xs = Axis(x0, x1, spacing); var zs = Axis(z0, z1, spacing);
            // Distant meshes use the near mesh's exact boundary samples, then
            // become coarse farther away. There are no mismatching T-junctions.
            if (id == "Far east horizon land") zs = Axis(-1800, 30, 32).Concat(Axis(30, 600, 5)).Concat(Axis(600, 1800, 32)).Distinct().OrderBy(v => v).ToArray();
            if (id == "Far north horizon land") xs = Axis(134, 300, 5).Concat(Axis(300, 600, 5)).Distinct().OrderBy(v => v).ToArray();
            if (id == "South horizon land") xs = Axis(200, 300, 5).Concat(Axis(300, 600, 5)).Distinct().OrderBy(v => v).ToArray();
            int nx = xs.Length - 1, nz = zs.Length - 1;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var colors = new List<Color>(); var t = new List<int>();
            for (int iz = 0; iz <= nz; iz++) for (int ix = 0; ix <= nx; ix++)
            {
                float x = xs[ix], z = zs[iz];
                v.Add(new Vector3(x, ContextHeight(x, z), z)); uv.Add(new Vector2(x, z));
                colors.Add(new Color(.68f + Mathf.PerlinNoise(x * .037f, z * .047f) * .30f, .025f, 0, 1));
                if (ix == nx || iz == nz) continue;
                int a = iz * (nx + 1) + ix;
                t.AddRange(new[] { a, a + nx + 1, a + 1, a + 1, a + nx + 1, a + nx + 2 });
            }
            var m = new Mesh { indexFormat = IndexFormat.UInt32 }; m.SetVertices(v); m.SetUVs(0, uv); m.SetUVs(1, uv); m.SetColors(colors); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
            AddMesh("BV3_Ground " + id, m, earth, true);
        }

        static void RepairBankAndFoliage(bool geometry = true)
        {
            var bridge = GameObject.Find(WestbruchBridgeStart.BridgeName);
            if (bridge && geometry)
            {
                foreach (var f in bridge.GetComponentsInChildren<MeshFilter>().Where(f => f.name == "Continuous North railway earth bank" || f.name == "Local render batch OverpassGround_LivingEarth"))
                {
                    var m = f.sharedMesh; if (!m || !m.isReadable) continue;
                    var v = m.vertices;
                    for (int i = 0; i < v.Length; i++)
                    {
                        var p = f.transform.TransformPoint(v[i]);
                        if (p.x < 132 || p.x > 134.6f || p.z < 115 || p.z > 620) continue;
                        float foot = NewContext(p.x, p.z) ? ContextHeight(p.x, p.z) - .035f : -.09f;
                        // An absolute envelope is idempotent. Repeated authoring
                        // passes must not progressively collapse the old slope.
                        float fromFoot = Mathf.Max(0, 134.5f - p.x);
                        p.y = Mathf.Min(p.y, foot + fromFoot * fromFoot * .12f);
                        v[i] = f.transform.InverseTransformPoint(p);
                    }
                    m.vertices = v; m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds(); EditorUtility.SetDirty(m); RefreshCollider(f, m);
                    if (f.name == "Continuous North railway earth bank") CloseCutEdges(f);
                }
            }
            Physics.SyncTransforms();
            if (geometry) return;
            var touched = new HashSet<Mesh>();
            var groundCache = new Dictionary<Vector2, (bool found, float floor, string support)>();
            foreach (var f in district.root.GetComponentsInChildren<MeshFilter>())
            {
                var m = f.sharedMesh; var renderer = f.GetComponent<MeshRenderer>();
                if (!m || !m.isReadable || !renderer || !renderer.sharedMaterial || !renderer.sharedMaterial.IsKeywordEnabled("_ZR_FOLIAGE") || !touched.Add(m)) continue;
                var roots = m.uv4; var heights = m.uv3; if (roots.Length != m.vertexCount || heights.Length != m.vertexCount) continue;
                var v = m.vertices; var t = m.triangles; var remove = new bool[v.Length]; bool changed = false;
                for (int i = 0; i < v.Length; i++)
                {
                    var p = roots[i]; if (p.x < 125.6f || p.x > 600 || p.y < 112 || p.y > 600) continue;
                    // This land was excavated for the lower station approach.
                    // Plants from the original slope must not survive the cut.
                    if (p.x >= 125.6f && p.x <= 134.6f && p.y > 237.5f && p.y < 285.5f)
                    { remove[i] = true; changed = true; continue; }
                    if (!groundCache.TryGetValue(p, out var ground))
                    {
                        bool found = GroundAt(p.x, p.y, out float sample, out string name);
                        groundCache[p] = ground = (found, sample, name);
                    }
                    if (!ground.found) continue;
                    float floor = ground.floor; string support = ground.support;
                    if (support.Contains("walk") || support.Contains("road")) { remove[i] = true; changed = true; continue; }
                    float target = floor - .035f;
                    var at = f.transform.TransformPoint(v[i]); at.y += target - heights[i].x; v[i] = f.transform.InverseTransformPoint(at);
                    heights[i].x = target; changed = true;
                }
                if (!changed) continue;
                var keep = new List<int>();
                for (int i = 0; i < t.Length; i += 3) if (!remove[t[i]] && !remove[t[i + 1]] && !remove[t[i + 2]]) keep.AddRange(new[] { t[i], t[i + 1], t[i + 2] });
                m.vertices = v; m.uv3 = heights; m.SetTriangles(keep, 0); m.RecalculateBounds(); var bounds = m.bounds; bounds.Expand(new Vector3(2.6f, 1.2f, 2.6f)); m.bounds = bounds; EditorUtility.SetDirty(m);
            }
        }

        static void CloseCutEdges(MeshFilter source)
        {
            var mesh = source.sharedMesh; var v = mesh.vertices; var t = mesh.triangles;
            var counts = new Dictionary<(int, int), int>();
            void Edge(int a, int b) { var key = a < b ? (a, b) : (b, a); counts.TryGetValue(key, out int n); counts[key] = n + 1; }
            for (int i = 0; i < t.Length; i += 3) { Edge(t[i], t[i + 1]); Edge(t[i + 1], t[i + 2]); Edge(t[i + 2], t[i]); }
            var points = new List<Vector3>(); var triangles = new List<int>();
            foreach (var edge in counts.Where(p => p.Value == 1))
            {
                var a = source.transform.TransformPoint(v[edge.Key.Item1]); var b = source.transform.TransformPoint(v[edge.Key.Item2]); var mid = (a + b) * .5f;
                if (mid.x < 124.5f || mid.x > 134.6f || mid.z < 237 || mid.z > 286 || Mathf.Max(a.y, b.y) < .05f) continue;
                int n = points.Count; points.AddRange(new[] { a, b, new Vector3(b.x, -.16f, b.z), new Vector3(a.x, -.16f, a.z) });
                triangles.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3, n + 2, n + 1, n, n + 3, n + 2, n });
            }
            if (triangles.Count > 0) BahnhofDetailKit.Mesh("BV3_Closed excavated earth edges", root, points, triangles, brick, true);
        }

        static void RetainedStationGarden()
        {
            // Small brick bays and a coping give the retaining structure a scale
            // readable from a pedestrian, rather than a single raw concrete sheet.
            Box("BV3_Retaining brick facing", root, new Vector3(126.265f, 2.10f, 261.5f), new Vector3(.08f, 4.2f, 46), brick, false, 2.1f);
            for (float z = 239.3f; z < 284; z += 5)
            {
                Box("BV3_Retaining buttress", root, new Vector3(126.40f, 2.1f, z), new Vector3(.27f, 4.2f, .46f), brick, true, 2.1f);
                Box("BV3_Buttress coping", root, new Vector3(126.40f, 4.25f, z), new Vector3(.36f, .13f, .57f), coping, false);
                var drain = Cylinder("BV3_Wall drainage outlet", root, new Vector3(126.455f, .43f, z + 1.4f), .045f, .10f, iron);
                drain.transform.localRotation = Quaternion.Euler(0, 0, 90);
            }
            Box("BV3_South excavation return", root, new Vector3(129.5f, 2.05f, 238.45f), new Vector3(7.5f, 4.1f, .32f), brick, true, 2.1f);
            Box("BV3_South return coping", root, new Vector3(129.5f, 4.14f, 238.45f), new Vector3(7.65f, .14f, .45f), coping, false);
            Box("BV3_North return facing", root, new Vector3(129.5f, 2.1f, 284.02f), new Vector3(7.5f, 4.2f, .09f), brick, false, 2.1f);
            // The rail bank is planted on the retained side only, well clear of
            // the lower hall exit and stairs. Every height comes from ground.
            foreach (float z in new[] { 232f, 288, 296, 305, 334, 346 })
                Shrub(130.7f, z, .9f, z * 13);
            foreach (float z in new[] { 140f, 159, 184, 205, 216 })
                Shrub(132.0f, z, .65f, z * 7);
        }

        static void ArrivalStreet()
        {
            // A sequence of workshop yards tightens the space after the
            // underpass, while leaving both 3 m footways and road unobstructed.
            Wall(new Vector3(166, -.055f, 64), new Vector3(225, -.055f, 64), 1.20f);
            Wall(new Vector3(225, -.055f, 64), new Vector3(225, -.055f, 70), 1.20f);
            Shed("Southern dispatch workshop", new Vector3(245, -.055f, 56), 30, 11, 4.5f, 0);
            // The road turns north in front of a real workshop facade. This
            // closes the former empty view from the underpass without a gate.
            Shed("Corner metalworkers", new Vector3(232, .14f, 80), 18, 16, 4.6f, 90);
            BahnhofDetailKit.Plate(root, "METALLBAU · SEIT 1968", new Vector3(223.84f, 3.93f, 80), 90, 8.5f, .43f, iron, new Color(.87f, .85f, .75f));
            Box("BV3_Court paving workshop service yard", root, new Vector3(191, -.06f, 146), new Vector3(14, .20f, 38), BahnhofDetailKit.Load("Asphalt"));
            Box("BV3_Court paving workshop entrance", root, new Vector3(199, -.06f, 145.5f), new Vector3(2, .20f, 13), BahnhofDetailKit.Load("Asphalt"));
            Wall(new Vector3(184, -.055f, 127), new Vector3(198, -.055f, 127), 1.0f);
            Wall(new Vector3(184, -.055f, 165), new Vector3(198, -.055f, 165), 1.0f);
            Wall(new Vector3(198, -.055f, 127), new Vector3(198, -.055f, 139), .85f);
            Wall(new Vector3(198, -.055f, 152), new Vector3(198, -.055f, 165), .85f);
            OpenGate(new Vector3(198, -.055f, 139), new Vector3(198, -.055f, 152), false);
            YardGoods(new Vector3(190, .04f, 130), 0);
            Workbench(new Vector3(194, .04f, 160), 0);
            BahnhofDetailKit.Plate(root, "WERKSTATTHOF", new Vector3(198.17f, .53f, 134), -90, 3.3f, .30f, iron, new Color(.88f, .85f, .75f));
            foreach (var p in new[] { new Vector2(168, 68), new Vector2(195, 68), new Vector2(219, 66), new Vector2(244, 70), new Vector2(275, 68) }) Tree(p.x, p.y, .92f);
            Hedge(new Vector2(166, 66), new Vector2(181, 66), .75f);
            Hedge(new Vector2(188, 66), new Vector2(218, 66), .75f);
            Wall(new Vector3(158, -.055f, 113.8f), new Vector3(195, -.055f, 113.8f), .78f);
            Fence(new Vector3(158, .725f, 113.8f), new Vector3(195, .725f, 113.8f), .75f);
            foreach (var p in new[] { new Vector2(148, 128), new Vector2(146, 146), new Vector2(149, 175), new Vector2(148, 213), new Vector2(199, 118), new Vector2(198, 145) }) Tree(p.x, p.y, .83f);
            Hedge(new Vector2(145.8f, 130), new Vector2(145.8f, 155), .72f);
            Hedge(new Vector2(145.8f, 178), new Vector2(145.8f, 211), .72f);
            // Brick piers mark workshop ownership, not a district border.
            foreach (float z in new[] { 125f, 185, 212 })
                Box("BV3_Workshop yard pier", root, new Vector3(150, .70f, z), new Vector3(.55f, 1.55f, .55f), brick);
            WestbruchProps.Bin(root, new Vector3(197, -.055f, 110), 90);
            WestbruchProps.Pallet(root, new Vector3(189, -.055f, 114.7f));
        }

        static void RearGardens()
        {
            // Rear courtyards occupy the space between the two streets. They
            // leave front entrances and their cross passages completely open.
            int index = 0;
            foreach (float z in new[] { 116f, 147, 202, 233, 275 })
            {
                var yard = Group("BV3_Rear garden " + index, root, Vector3.zero).transform;
                Wall(new Vector3(253.3f, -.055f, z - 12.4f), new Vector3(253.3f, -.055f, z + 12.4f), .72f, yard);
                Fence(new Vector3(253.3f, .665f, z - 12.4f), new Vector3(253.3f, .665f, z + 12.4f), .80f, yard);
                foreach (float side in new[] { -1f, 1 }) Wall(new Vector3(242.9f, -.055f, z + side * 12.4f), new Vector3(253.3f, -.055f, z + side * 12.4f), .85f, yard);
                Hedge(new Vector2(251.5f, z - 10.6f), new Vector2(251.5f, z + 10.6f), .62f);
                Tree(247.0f, z + (index % 2 == 0 ? 5 : -5), .78f);
                if (index % 2 == 0) Shed("Garden cycle store " + index, new Vector3(248.3f, -.055f, z - 8.2f), 5.1f, 3.1f, 2.25f, 0);
                else WashingLine(new Vector3(247, -.055f, z + 7));
                WestbruchProps.Bin(yard, new Vector3(243.6f, -.055f, z - 3), 90);
                index++;
            }
            foreach (float z in new[] { 202f, 235, 278 })
            {
                Wall(new Vector3(299.0f, -.055f, z - 13), new Vector3(299.0f, -.055f, z + 13), 1.10f);
                foreach (float side in new[] { -1f, 1 }) Wall(new Vector3(290.3f, -.055f, z + side * 13), new Vector3(299.0f, -.055f, z + side * 13), 1.10f);
                Hedge(new Vector2(296.8f, z - 11), new Vector2(296.8f, z + 11), .68f);
                Tree(293, z + 6, .90f);
                Shed("Rear allotment store " + z, new Vector3(294.0f, -.055f, z - 8), 5, 3.4f, 2.3f, 90);
            }
            // Green street corners frame each block; trees are behind the lamp
            // line and do not grow through the repeated raised front planters.
            foreach (var p in new[] { new Vector2(220, 95), new Vector2(220, 166), new Vector2(220, 186), new Vector2(220, 254), new Vector2(220, 294), new Vector2(256, 187), new Vector2(256, 297), new Vector2(274, 315) }) Tree(p.x, p.y, .94f);
            Hedge(new Vector2(291, 184), new Vector2(297, 184), .60f);
            Hedge(new Vector2(291, 252), new Vector2(297, 252), .60f);
        }

        static void NorthernCourt()
        {
            // The existing street ends at the edge of this pedestrian forecourt.
            // Its 313.5 m boundary meets existing east-west paving exactly.
            Box("BV3_Terminal pedestrian court", root, new Vector3(208, .045f, 319.25f), new Vector3(20, .19f, 11.5f), BahnhofDetailKit.Load("Paving"));
            Wall(new Vector3(198, -.055f, 330), new Vector3(225, -.055f, 330), .68f);
            Hedge(new Vector2(199, 332), new Vector2(225, 332), .95f);
            foreach (var p in new[] { new Vector2(197, 327), new Vector2(221, 327), new Vector2(211, 344), new Vector2(239, 339), new Vector2(266, 341) }) Tree(p.x, p.y, 1.12f);
            Shed("Railway stores and workshop", new Vector3(161, -.055f, 345), 20, 17, 4.15f, 0);
            Wall(new Vector3(143, -.055f, 330), new Vector3(143, -.055f, 358), 1.3f);
            Wall(new Vector3(143, -.055f, 358), new Vector3(189, -.055f, 358), 1.3f);
            Wall(new Vector3(189, -.055f, 342), new Vector3(189, -.055f, 358), 1.3f);
            foreach (var p in new[] { new Vector2(147, 335), new Vector2(179, 354), new Vector2(191, 351), new Vector2(275, 357), new Vector2(296, 349) }) Tree(p.x, p.y, 1.15f);
            WestbruchProps.Pallet(root, new Vector3(175, -.055f, 346));
            WestbruchProps.Pallet(root, new Vector3(175, -.055f, 349));
            WestbruchProps.Bench(root, new Vector3(203, .14f, 322.5f), 0);
            WestbruchProps.Bench(root, new Vector3(213, .14f, 322.5f), 0);
            WestbruchProps.Bin(root, new Vector3(217, .14f, 322), 0);
        }

        static void DistantWorkingNeighbourhood()
        {
            // Backland has real roofs and enclosure, at a respectful distance
            // from playable houses. Irregular groups avoid a straight tree wall.
            WorkingDepotCourt();
            MunicipalCourt();
            Shed("East joinery workshop", new Vector3(344, ContextHeight(344, 238), 238), 36, 16, 4.8f, 90);
            foreach (float z in new[] { 111f, 145, 181, 213, 251, 289, 328, 366 })
            {
                Tree(308 + Mathf.Sin(z) * 5, z, 1.16f);
                Tree(324 + Mathf.Cos(z * .2f) * 7, z + 9, 1.25f);
                Tree(357 + Mathf.Sin(z * .4f) * 8, z + 20, 1.35f);
                Hedge(new Vector2(305, z - 6), new Vector2(305, z + 7), .95f);
            }
            for (int i = 0; i < 12; i++)
            {
                float x = 146 + i * 17;
                Tree(x, 371 + Mathf.Sin(i * 2.7f) * 10, 1.12f + (i % 3) * .10f);
                Tree(x + 7, 402 + Mathf.Cos(i * 1.9f) * 9, 1.30f);
            }
            for (int i = 0; i < 11; i++)
                Tree(385 + Mathf.Sin(i * 1.7f) * 13, 84 + i * 33, 1.45f);
            WoodlandGroups();
        }

        static void WorkingDepotCourt()
        {
            const float floor = .04f;
            Box("BV3_Court paving east depot", root, new Vector3(305.5f, -.06f, 156), new Vector3(39, .20f, 52), BahnhofDetailKit.Load("Asphalt"));
            Shed("East railway depot", new Vector3(310, floor, 139), 26, 14, 5.0f, 180);
            Shed("Depot caretaker workshop", new Vector3(290.4f, floor, 142), 6.0f, 10, 3.15f, 180);
            Wall(new Vector3(286, -.055f, 130), new Vector3(325, -.055f, 130), 1.85f);
            Wall(new Vector3(325, -.055f, 130), new Vector3(325, -.055f, 182), 1.85f);
            Wall(new Vector3(286, -.055f, 182), new Vector3(325, -.055f, 182), 1.85f);
            Wall(new Vector3(286, -.055f, 130), new Vector3(286, -.055f, 171.5f), 1.85f);
            Wall(new Vector3(286, -.055f, 180.5f), new Vector3(286, -.055f, 182), 1.85f);
            OpenGate(new Vector3(286, -.055f, 171.5f), new Vector3(286, -.055f, 180.5f), true);
            BahnhofDetailKit.Plate(root, "BAHNHOF · BETRIEBSHOF", new Vector3(285.82f, 1.25f, 165), 90, 4.6f, .40f, iron, new Color(.88f, .85f, .75f));
            // Paving is a working space: stacked goods, a rack and a bench give
            // it a purpose without littering the walking route through the gate.
            YardGoods(new Vector3(321, floor, 154), 90);
            YardGoods(new Vector3(319, floor, 176), 0);
            WestbruchProps.Bin(root, new Vector3(290, floor, 152), 0);
            WestbruchProps.Bin(root, new Vector3(291, floor, 152), 0);
            Workbench(new Vector3(292, floor, 165), 90);
            foreach (float z in new[] { 151f, 157, 163 })
                Box("BV3_Depot marked service bay", root, new Vector3(307, .046f, z), new Vector3(9, .008f, .085f), BahnhofDetailKit.Load("FadedRoadPaint"), false);
            Shed("Depot covered materials bay", new Vector3(320, floor, 166), 6.0f, 7, 2.7f, 90);
        }

        static void MunicipalCourt()
        {
            const float floor = .04f;
            Box("BV3_Court paving municipal yard", root, new Vector3(260.5f, -.06f, 357), new Vector3(63, .20f, 34), BahnhofDetailKit.Load("Asphalt"));
            Shed("Northern municipal yard", new Vector3(259, floor, 361), 48, 18, 5.4f, 0);
            Wall(new Vector3(229, -.055f, 340), new Vector3(259.5f, -.055f, 340), 1.65f);
            Wall(new Vector3(268.5f, -.055f, 340), new Vector3(292, -.055f, 340), 1.65f);
            Wall(new Vector3(229, -.055f, 340), new Vector3(229, -.055f, 374), 1.65f);
            Wall(new Vector3(292, -.055f, 340), new Vector3(292, -.055f, 374), 1.65f);
            Wall(new Vector3(229, -.055f, 374), new Vector3(292, -.055f, 374), 1.65f);
            OpenGate(new Vector3(259.5f, -.055f, 340), new Vector3(268.5f, -.055f, 340), false);
            BahnhofDetailKit.Plate(root, "STADTBETRIEBE WESTBRUCH", new Vector3(248, 1.15f, 339.82f), 0, 6.5f, .45f, iron, new Color(.88f, .85f, .75f));
            YardGoods(new Vector3(233, floor, 345), 0);
            YardGoods(new Vector3(287, floor, 347), 90);
            foreach (float x in new[] { 237f, 241, 245 }) WestbruchProps.Pallet(root, new Vector3(x, floor, 349));
            Workbench(new Vector3(281, floor, 347), 0);
            // A small planted forecourt and a woodland path separate the public
            // square from the municipal working yard.
            Hedge(new Vector2(230, 336.8f), new Vector2(255, 336.8f), .8f);
            Hedge(new Vector2(273, 336.8f), new Vector2(291, 336.8f), .8f);
        }

        static void OpenGate(Vector3 a, Vector3 b, bool opensEast)
        {
            float length = Vector3.Distance(a, b);
            var p = Group("BV3_Open service yard gate", root, (a + b) * .5f, Quaternion.LookRotation(b - a).eulerAngles.y).transform;
            foreach (float side in new[] { -1f, 1 })
            {
                Box("BV3_Gate masonry post", p, new Vector3(0, 1.12f, side * length * .5f), new Vector3(.60f, 2.24f, .60f), brick);
                Box("BV3_Gate post coping", p, new Vector3(0, 2.28f, side * length * .5f), new Vector3(.72f, .10f, .72f), coping, false);
                float swing = side * (opensEast ? -65 : 65);
                var leaf = Group("BV3_Open gate leaf", p, new Vector3(0, .12f, side * length * .5f), swing).transform;
                float center = -side * length * .25f;
                foreach (float y in new[] { .16f, 1.65f }) Box("BV3_Gate leaf frame rail", leaf, new Vector3(0, y, center), new Vector3(.07f, .07f, length * .5f), iron, false);
                for (int i = 0; i <= 16; i++)
                    Box("BV3_Gate upright", leaf, new Vector3(0, .90f, -side * length * .5f * i / 16), new Vector3(.045f, 1.55f, .045f), iron, false);
                Beam("BV3_Gate diagonal brace", leaf, new Vector3(0, .16f, 0), new Vector3(0, 1.65f, -side * length * .5f), .045f, iron);
            }
        }

        static void Workbench(Vector3 at, float yaw)
        {
            var p = Group("BV3_Yard repair bench", root, at, yaw).transform;
            Box("BV3_Bench working top", p, new Vector3(0, .93f, 0), new Vector3(2.0f, .11f, .9f), timber, true);
            foreach (float x in new[] { -.8f, .8f }) foreach (float z in new[] { -.30f, .30f }) Box("BV3_Bench steel leg", p, new Vector3(x, .43f, z), new Vector3(.07f, .86f, .07f), iron);
            Box("BV3_Bench lower shelf", p, new Vector3(0, .18f, 0), new Vector3(1.8f, .06f, .72f), timber);
            Box("BV3_Bench toolbox", p, new Vector3(.5f, 1.12f, .03f), new Vector3(.56f, .26f, .35f), iron, false);
            foreach (float z in new[] { -.18f, .18f }) Beam("BV3_Bench clamps", p, new Vector3(-.65f, 1.0f, z), new Vector3(-.65f, 1.17f, z), .045f, iron);
        }

        static void YardGoods(Vector3 at, float yaw)
        {
            var p = Group("BV3_Working yard stored goods", root, at, yaw).transform;
            foreach (var item in new[] { new Vector3(0, 0, 0), new Vector3(1.6f, 0, .15f) })
            {
                WestbruchProps.Pallet(p, item);
                Box("BV3_Timber delivery crate", p, item + new Vector3(0, .64f, 0), new Vector3(1.05f, .94f, .82f), timber, true);
                foreach (float x in new[] { -.39f, .39f }) Box("BV3_Crate binding", p, item + new Vector3(x, .65f, 0), new Vector3(.035f, .97f, .85f), iron, false);
            }
            var drum = Material("BV3_YardDrum", "steel", new Color(.34f, .38f, .32f), .18f, .30f);
            foreach (var item in new[] { new Vector3(-1.35f, 0, 0), new Vector3(-1.35f, 0, .68f) })
            {
                Cylinder("BV3_Service oil drum", p, item + Vector3.up * .43f, .28f, .86f, drum, true);
                foreach (float h in new[] { .10f, .45f, .78f }) Cylinder("BV3_Drum strengthening hoop", p, item + Vector3.up * h, .292f, .035f, iron);
                Cylinder("BV3_Drum cap", p, item + new Vector3(.10f, .867f, 0), .035f, .02f, iron);
            }
            WestbruchProps.Pallet(p, new Vector3(.3f, 0, 1.3f));
            WestbruchProps.Pallet(p, new Vector3(.3f, .16f, 1.3f));
        }

        static void WoodlandGroups()
        {
            // Overlapping canopies and a layered understory create depth. The
            // footprint follows pockets around working yards, not a map border.
            var centers = new[] { new Vector2(305, 94), new Vector2(329, 114), new Vector2(315, 194), new Vector2(317, 224), new Vector2(319, 270), new Vector2(308, 311), new Vector2(324, 331), new Vector2(343, 326), new Vector2(351, 346), new Vector2(350, 199), new Vector2(349, 286), new Vector2(215, 348), new Vector2(195, 373), new Vector2(310, 374), new Vector2(81, 248), new Vector2(65, 278), new Vector2(36, 270), new Vector2(12, 295), new Vector2(-22, 275), new Vector2(-58, 310), new Vector2(-84, 254), new Vector2(-103, 341) };
            for (int c = 0; c < centers.Length; c++)
            {
                var center = centers[c];
                for (int i = 0; i < 8; i++)
                {
                    float a = i * 2.39996f + c * .7f, radius = i == 0 ? 0 : 4.5f + (i % 3) * 2.7f;
                    Tree(center.x + Mathf.Cos(a) * radius, center.y + Mathf.Sin(a) * radius, 1.10f + (i % 3) * .15f);
                }
                for (int i = 0; i < 15; i++)
                {
                    float a = i * 2.39996f + c, radius = 3 + Mathf.Sqrt(i) * 2.0f;
                    Shrub(center.x + Mathf.Cos(a) * radius, center.y + Mathf.Sin(a) * radius, .8f + (i % 3) * .18f, i * 137.5f);
                }
            }
        }

        static void GreenerSoil()
        {
            var surfaces = district.Find("BV2_Continuous surfaces"); if (!surfaces) return;
            foreach (var f in surfaces.GetComponentsInChildren<MeshFilter>().Where(f => f.name.StartsWith("BV2_Surface earth")))
            {
                var m = f.sharedMesh; var colors = m.colors; var v = m.vertices;
                for (int i = 0; i < v.Length; i++)
                {
                    var p = f.transform.TransformPoint(v[i]);
                    float garden = p.x > 242 || p.z > 325 || p.x < 153 ? .80f : .48f;
                    colors[i] = new Color(Mathf.Clamp01(garden + (Mathf.PerlinNoise(p.x * .081f + 5, p.z * .064f + 9) - .5f) * .35f), .035f, 0, 1);
                }
                m.colors = colors; EditorUtility.SetDirty(m);
            }
        }

        static void PlantGroundCover()
        {
            if (!grass) return;
            var meshes = new[]{"0_0","0_1","0_2","2_0","3_0","5_0"}.Select(i => AssetDatabase.LoadAssetAtPath<Mesh>(Generated + "/Meshes/UrbanFlora_" + i + ".asset")).ToArray();
            if (meshes.Any(m => !m)) return;
            var rng = new System.Random(3111001); var chunks = new Dictionary<Vector2Int, List<CombineInstance>>();
            var heights = new Dictionary<Vector2Int, List<Vector2>>(); var roots = new Dictionary<Vector2Int, List<Vector2>>();
            for (int i = 0; i < 12500; i++)
            {
                float x = 143 + (float)rng.NextDouble() * 240, z = 62 + (float)rng.NextDouble() * 348;
                bool nearGreen = x > 293 || z > 328 || x < 151 || x > 243 && x < 255 || x > 215 && x < 221;
                if (!nearGreen || Mathf.PerlinNoise(x * .23f, z * .21f) < .40f || !GroundAt(x, z, out float y, out string support) || support.Contains("walk") || support.Contains("road")) continue;
                if (Physics.CheckCapsule(new Vector3(x, y + .31f, z), new Vector3(x, y + 1.1f, z), .23f, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (treeSites.Any(p => Vector2.Distance(new Vector2(p.x, p.z), new Vector2(x, z)) < .40f)) continue;
                var key = new Vector2Int(Mathf.FloorToInt(x / 35), Mathf.FloorToInt(z / 35));
                if (!chunks.TryGetValue(key, out var list)) { chunks[key] = list = new List<CombineInstance>(); heights[key] = new List<Vector2>(); roots[key] = new List<Vector2>(); }
                if (list.Count >= 95) continue;
                var m = meshes[rng.Next(meshes.Length)]; float scale = .63f + (float)rng.NextDouble() * .45f;
                var pos = new Vector3(x, y - .035f, z); var rot = Quaternion.Euler(0, (float)rng.NextDouble() * 360, 0);
                list.Add(new CombineInstance { mesh = m, transform = Matrix4x4.TRS(pos, rot, Vector3.one * scale) });
                foreach (var v in m.vertices) { heights[key].Add(new Vector2(pos.y, Mathf.Clamp01(v.y / m.bounds.max.y))); roots[key].Add(new Vector2(x, z)); }
                grassCount++;
            }
            foreach (var pair in chunks)
            {
                var m = new Mesh { indexFormat = IndexFormat.UInt32 }; m.CombineMeshes(pair.Value.ToArray(), true, true); m.SetUVs(2, heights[pair.Key]); m.SetUVs(3, roots[pair.Key]); m.RecalculateBounds(); var bounds = m.bounds; bounds.Expand(new Vector3(2.6f, 1.2f, 2.6f)); m.bounds = bounds;
                AddMesh("BV3_Rooted garden plants " + pair.Key.x + "_" + pair.Key.y, m, grass, false);
            }
        }

        static void RegroundExistingTrees()
        {
            foreach (var lod in district.GetComponentsInChildren<LODGroup>())
            {
                if (lod.transform.IsChildOf(root) || !(lod.name == "Bahnhof street tree" || lod.name == "Railway neighbourhood perimeter tree")) continue;
                var p = lod.transform.position;
                if (GroundAt(p.x, p.z, out float ground, out _)) { p.y = ground - .025f; lod.transform.position = p; }
            }
        }

        static bool GroundAt(float x, float z, out float y, out string support)
        {
            y = -.055f; support = "earth"; bool found = false;
            foreach (var hit in Physics.RaycastAll(new Vector3(x, 25, z), Vector3.down, 60, ~0, QueryTriggerInteraction.Ignore))
            {
                string name = hit.collider.name;
                bool court = name.StartsWith("BV3_Court paving ") || name == "BV3_Terminal pedestrian court";
                bool land = court || hit.collider is TerrainCollider || name.StartsWith("BV2_Surface ") || name.StartsWith("BV3_Ground ") || name == "Continuous North railway earth bank" || name == "Passing loop earth formation" || name == "Land below distant railway 1";
                if (!land || found && hit.point.y <= y) continue;
                found = true; y = hit.point.y; support = court ? "walk " + name : name;
            }
            return found;
        }

        static void Tree(float x, float z, float scale)
        {
            if (!treeSource) return;
            Physics.SyncTransforms();
            float y = NewContext(x, z) ? ContextHeight(x, z) : -.055f;
            GroundAt(x, z, out y, out string support);
            if (support.Contains("walk") || support.Contains("road")) return;
            // Context vegetation is also checked against actual authored shells;
            // a picturesque tree position must not plant its trunk in a shed.
            if (Physics.CheckCapsule(new Vector3(x, y + .52f, z), new Vector3(x, y + 4.2f, z), .45f, ~0, QueryTriggerInteraction.Ignore)) return;
            var chosen=treeVariants[treeCount%treeVariants.Length];
            var go = UnityEngine.Object.Instantiate(chosen, root); go.name = "BV3_Rooted mature tree " + treeCount++;
            go.transform.SetPositionAndRotation(new Vector3(x, y - .025f, z), Quaternion.Euler(0, treeCount * 137.5f, 0));
            go.transform.localScale = Vector3.Scale(chosen.transform.lossyScale,new Vector3(scale*(.88f+treeCount%3*.09f),scale,scale*(1.04f-treeCount%3*.06f)));
            treeSites.Add(go.transform.position);
        }

        static void Shrub(float x, float z, float scale, float yaw)
        {
            if (!shrubSource || !GroundAt(x, z, out float y, out string support) || support.Contains("walk") || support.Contains("road")) return;
            var go = UnityEngine.Object.Instantiate(shrubSource, root); go.name = "BV3_Rooted yard shrub " + shrubCount++;
            go.transform.SetPositionAndRotation(new Vector3(x, y - .035f, z), Quaternion.Euler(0, yaw, 0)); go.transform.localScale = shrubSource.transform.lossyScale * scale;
        }

        static void Hedge(Vector2 a, Vector2 b, float scale)
        {
            int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / (2.6f * scale)));
            for (int i = 0; i <= n; i++) { var p = Vector2.Lerp(a, b, i / (float)n); Shrub(p.x, p.y, scale * (1 + Mathf.Sin(i * 5.7f) * .08f), i * 137.5f); }
        }

        static void Wall(Vector3 a, Vector3 b, float height, Transform parent = null)
        {
            parent = parent ? parent : root; var mid = (a + b) * .5f; float length = Vector3.Distance(a, b);
            var p = Group("BV3_Brick property wall", parent, mid, Quaternion.LookRotation(b - a).eulerAngles.y).transform;
            Box("BV3_Wall masonry", p, new Vector3(0, height * .5f, 0), new Vector3(.28f, height, length), brick, true, 2.1f);
            Box("BV3_Wall weathering cap", p, new Vector3(0, height + .045f, 0), new Vector3(.38f, .09f, length + .12f), coping, false);
            int count = Mathf.Max(1, Mathf.CeilToInt(length / 5.0f));
            for (int i = 0; i <= count; i++)
            {
                float z = Mathf.Lerp(-length * .5f, length * .5f, i / (float)count);
                Box("BV3_Wall end and intermediate pier", p, new Vector3(0, (height + .18f) * .5f, z), new Vector3(.43f, height + .18f, .43f), brick);
                Box("BV3_Pier coping", p, new Vector3(0, height + .23f, z), new Vector3(.52f, .10f, .52f), coping, false);
            }
        }

        static void Fence(Vector3 a, Vector3 b, float height, Transform parent = null)
        {
            parent = parent ? parent : root; float length = Vector3.Distance(a, b);
            var p = Group("BV3_Garden paling fence", parent, (a + b) * .5f, Quaternion.LookRotation(b - a).eulerAngles.y).transform;
            foreach (float y in new[] { height * .27f, height * .75f }) Box("BV3_Fence crossrail", p, new Vector3(0, y, 0), new Vector3(.07f, .07f, length), timber, false);
            int count = Mathf.CeilToInt(length / .18f);
            for (int i = 0; i <= count; i++) Box("BV3_Fence individual paling", p, new Vector3(.045f, height * .5f, Mathf.Lerp(-length * .5f, length * .5f, i / (float)count)), new Vector3(.035f, height, .115f), timber, false);
            var collision = p.gameObject.AddComponent<BoxCollider>(); collision.center = Vector3.up * height * .5f; collision.size = new Vector3(.12f, height, length);
        }

        static void WashingLine(Vector3 at)
        {
            var p = Group("BV3_Resident washing line", root, at).transform;
            foreach (float x in new[] { -1.7f, 1.7f }) { Cylinder("BV3_Washing line standard", p, new Vector3(x, 1.05f, 0), .035f, 2.1f, iron, true); Beam("BV3_Washing line crossarm", p, new Vector3(x, 2, -.55f), new Vector3(x, 2, .55f), .035f, iron); }
            foreach (float z in new[] { -.4f, .4f }) Beam("BV3_Washing cord", p, new Vector3(-1.7f, 1.99f, z), new Vector3(1.7f, 1.99f, z), .008f, iron);
            var cloth = Material("BV3_YardLaundry", null, new Color(.66f, .68f, .61f), .02f);
            Box("BV3_Hanging kitchen linen", p, new Vector3(-.55f, 1.65f, -.4f), new Vector3(.72f, .67f, .018f), cloth, false);
            Box("BV3_Hanging small linen", p, new Vector3(.65f, 1.76f, .4f), new Vector3(.55f, .44f, .018f), cloth, false);
        }

        static void Shed(string title, Vector3 at, float width, float depth, float height, float yaw)
        {
            var p = Group("BV3_" + title, root, at, yaw).transform;
            Box("BV3_Working shed foundation", p, new Vector3(0, -.40f, 0), new Vector3(width + .12f, 1.10f, depth + .12f), coping);
            Box("BV3_Shed rear wall", p, new Vector3(0, height * .5f, depth * .5f), new Vector3(width, height, .22f), brick);
            foreach (float x in new[] { -width * .5f, width * .5f }) Box("BV3_Shed gable wall", p, new Vector3(x, height * .5f, 0), new Vector3(.22f, height, depth), brick);
            int doors = width > 15 ? 3 : 1; float bay = width / doors;
            var shutter = Material("BV3_ServiceDoor", "steel", new Color(.28f, .35f, .33f), .16f, .25f);
            for (int i = 0; i < doors; i++)
            {
                float x = -width * .5f + bay * (i + .5f); float opening = Mathf.Min(bay - .65f, height * 1.0f), doorH = Mathf.Min(height - .5f, 3.1f);
                Box("BV3_Shed door", p, new Vector3(x, doorH * .5f, -depth * .5f), new Vector3(opening, doorH, .14f), shutter, true);
                foreach (float side in new[] { -1f, 1 }) Box("BV3_Shed front pier", p, new Vector3(x + side * (opening * .5f + (bay - opening) * .25f), height * .5f, -depth * .5f), new Vector3((bay - opening) * .5f, height, .24f), brick);
                Box("BV3_Shed lintel masonry", p, new Vector3(x, (height + doorH) * .5f, -depth * .5f), new Vector3(opening, height - doorH, .24f), brick);
                Box("BV3_Shed stone lintel", p, new Vector3(x, doorH + .10f, -depth * .5f - .05f), new Vector3(opening + .25f, .20f, .29f), coping, false);
                for (float y = .25f; y < doorH; y += .23f) Box("BV3_Door horizontal stiffener", p, new Vector3(x, y, -depth * .5f - .086f), new Vector3(opening - .10f, .025f, .028f), iron, false);
                Box("BV3_Door handle", p, new Vector3(x + opening * .32f, .95f, -depth * .5f - .12f), new Vector3(.04f, .20f, .04f), iron, false);
            }
            BahnhofDetailKit.GableRoof(p, "BV3_" + title, width + .55f, depth + .55f, height + .05f, Mathf.Min(1.8f, depth * .19f), roof, brick);
            foreach (float side in new[] { -1f, 1 })
            {
                Box("BV3_Shed rain gutter", p, new Vector3(0, height, side * (depth * .5f + .31f)), new Vector3(width + .65f, .12f, .15f), iron, false);
                Cylinder("BV3_Shed rainwater pipe", p, new Vector3(side * (width * .5f - .25f), height * .5f, -depth * .5f - .20f), .045f, height, iron);
            }
        }

        static void AddMesh(string name, Mesh mesh, Material material, bool collision)
        {
            string key = new string(name.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_').ToArray());
            string path = Generated + "/Meshes/" + key + ".asset"; var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (old) { EditorUtility.CopySerialized(mesh, old); UnityEngine.Object.DestroyImmediate(mesh); mesh = old; EditorUtility.SetDirty(mesh); } else AssetDatabase.CreateAsset(mesh, path);
            var go = Group(name, root, Vector3.zero); go.isStatic = true; go.AddComponent<MeshFilter>().sharedMesh = mesh; var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial = material;
            if(name.StartsWith("BV3_Ground "))renderer.shadowCastingMode=ShadowCastingMode.Off;
            if (collision) go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        static void RefreshCollider(MeshFilter f, Mesh m)
        {
            var c = f.GetComponent<MeshCollider>(); if (!c) return; c.sharedMesh = null; c.sharedMesh = m;
        }
    }
}
