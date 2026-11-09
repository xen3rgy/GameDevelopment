using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    // Final, idempotent pass over the playable district, after all soil and
    // structures exist. Tests examine visible surfaces, not buried base terrain.
    internal static class BahnhofCoreRenewal
    {
        static Transform district, root;
        static readonly List<string> report = new List<string>();
        static bool InCore(Vector2 p) => p.x >= 98 && p.x <= 305 && p.y >= 58 && p.y <= 425;
        internal static bool Natural(Collider c)
        {
            string n = c.name;
            return c is TerrainCollider || n.StartsWith("BV2_Surface earth") || n.StartsWith("BV3_Ground ") ||
                n.Contains("railway earth bank") || n == "Passing loop earth formation" || n.StartsWith("Land below distant railway") ||
                n.Contains("retained soil", StringComparison.OrdinalIgnoreCase) || n.Contains("garden soil", StringComparison.OrdinalIgnoreCase) ||
                n.Contains("Raised bed soil", StringComparison.OrdinalIgnoreCase) || n == "BV4_Ground residential continuation" ||
                n.StartsWith("BV4_Supported bank garden shoulder");
        }
        static bool PlantCollider(Collider c) => c.GetComponentInParent<LODGroup>() != null;
        internal static bool Soil(float x, float z, float expected, out RaycastHit support)
        {
            support = default;
            // Pick actual planted earth, then reject pavement, slabs and solid
            // structures over it. Starting near ground ignores elevated canopies.
            var hits = Physics.RaycastAll(new Vector3(x, 35, z), Vector3.down, 80, ~0, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits.OrderByDescending(h => h.point.y))
            {
                if (!Natural(hit.collider) || hit.normal.y < .38f || hit.point.y > expected + 1.5f) continue;
                support = hit; break;
            }
            if (!support.collider) return false;
            float y = support.point.y;
            foreach (var hit in hits)
                if (!Natural(hit.collider) && !PlantCollider(hit.collider) && hit.point.y > y + .025f && hit.point.y < y + 1.4f)
                    return false;
            foreach (var c in Physics.OverlapSphere(new Vector3(x, y + .11f, z), .065f, ~0, QueryTriggerInteraction.Ignore))
                if (!Natural(c) && !PlantCollider(c)) return false;
            return true;
        }
        internal static void Apply(Transform neighbourhood)
        {
            district = neighbourhood;
            var old = district.Find("BV7_District ground renewal");
            if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            root = Group("BV7_District ground renewal", district, Vector3.zero).transform;
            report.Clear();
            ArrivalSurfaces();
            UnifyBankSurfaces();
            FixRailBoundary();
            Physics.SyncTransforms();
            RepairFoliage();
            RepairShrubs();
            PlantBank();
            // No disconnected concrete channel across the garden; runoff uses the planted toe.
            Physics.SyncTransforms();
            File.WriteAllLines("QA/district-core-renewal.txt", report);
            Debug.Log(string.Join("\n", report));
        }
        static void ArrivalSurfaces()
        {
            var names = new HashSet<string> { "Connected Nordring east carriageway", "Gewerbehof turning court",
                "Continuous underpass pavement", "Court west pavement", "Court end pavement", "Garage entrance apron pavement" };
            var sources = district.root.GetComponentsInChildren<MeshFilter>().Where(f => names.Contains(f.name)).ToArray();
            WestbruchAuditCompletion.RemoveRetainedTriangles(district, sources);
            foreach (var f in sources)
            {
                bool road = f.name.Contains("carriageway") || f.name.Contains("turning court");
                var renderer = f.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = BahnhofDetailKit.Load(road ? "BV5_Road_asphalt" : "BV5_Station_stone_setts");
                if (!road)
                {
                    var mesh = UnityEngine.Object.Instantiate(f.sharedMesh); var vertices = mesh.vertices; var normals = mesh.normals; var uv = new Vector2[vertices.Length];
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        var p = f.transform.TransformPoint(vertices[i]); var n = f.transform.TransformDirection(normals[i]);
                        uv[i] = (Mathf.Abs(n.y) > .65f ? new Vector2(p.x, p.z) : Mathf.Abs(n.x) > .65f ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y)) / 3.4f;
                    }
                    mesh.uv = uv; mesh.RecalculateTangents();
                    f.sharedMesh = Save(mesh, "Arrival_" + f.name.Replace(' ', '_') + "_" + f.transform.position.x + "_" + f.transform.position.z);
                }
                renderer.enabled = true;
            }
            report.Add("Continuous approach materials: " + sources.Length + " road/pavement sources through the underpass and turning court.");
        }
        static void UnifyBankSurfaces()
        {
            var filters = district.root.GetComponentsInChildren<MeshFilter>().Where(f =>
                f.name == "Continuous North railway earth bank" || f.name == "Passing loop earth formation").ToArray();
            // Remove the OLD copy from global render batches before enabling
            // its editable source. Changing only that source left mixed textures.
            WestbruchAuditCompletion.RemoveRetainedTriangles(district, filters);
            var ground = BahnhofDetailKit.Load("BV5_LayeredGardenGround");
            foreach (var f in filters)
            {
                var m = f.sharedMesh; var v = m.vertices;
                var baseline = new List<Vector2>(); m.GetUVs(4, baseline);
                if (baseline.Count != v.Length) baseline = v.Select(p => new Vector2(p.y, 1)).ToList();
                var uv = new Vector2[v.Length]; var color = new Color[v.Length];
                for (int i = 0; i < v.Length; i++)
                {
                    // Store the undeformed Y once, so repeat authoring cannot
                    // accumulate settlement or move the track crest.
                    v[i].y = baseline[i].x;
                    var p = f.transform.TransformPoint(v[i]);
                    float flank = Mathf.Pow(Mathf.Sin(Mathf.Clamp01(p.y / 5.95f) * Mathf.PI), 2);
                    float limits = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(112, 130, p.z)) *
                        (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(408, 428, p.z)));
                    float erosion = (Mathf.PerlinNoise(p.x * .73f + 11, p.z * .39f) - .5f) * .18f +
                        (Mathf.PerlinNoise(p.x * .24f, p.z * .16f + 9) - .5f) * .23f;
                    p.y += erosion * flank * limits;
                    v[i] = f.transform.InverseTransformPoint(p);
                    uv[i] = new Vector2(p.x, p.z);
                    float cover = Mathf.PerlinNoise(p.x * .31f + 8, p.z * .17f);
                    color[i] = new Color(Mathf.Lerp(.32f, 1f, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.32f, .67f, cover))), 1, 0, 1);
                }
                // Shared positions receive the same normal even on the old loop
                // mesh whose triangle vertices were duplicated for flat shading.
                var normals = new Dictionary<Vector3, Vector3>(); var triangles = m.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                    var normal = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
                    foreach (int index in new[] { a, b, c }) { normals.TryGetValue(v[index], out var sum); normals[v[index]] = sum + normal; }
                }
                m.vertices = v; m.SetUVs(4, baseline);
                m.normals = v.Select(p => normals.TryGetValue(p, out var n) ? n.normalized : Vector3.up).ToArray();
                m.RecalculateBounds();
                var collider = f.GetComponent<MeshCollider>(); if (collider) { collider.sharedMesh = null; collider.sharedMesh = m; }
                m.uv2 = uv; m.colors = color; m.RecalculateTangents(); EditorUtility.SetDirty(m);
                var r = f.GetComponent<MeshRenderer>(); r.sharedMaterial = ground; r.enabled = true;
            }
            report.Add("Unified railway soil: " + filters.Length + " source surfaces; old batched copies removed.");
        }
        static float LoopX(float z) => 118 - 6.5f * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(140, 205, z)) *
            (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(345, 410, z)));
        static void FixRailBoundary()
        {
            var names = new HashSet<string> { "Grounded rail boundary post", "Terminated railway safety wire", "Railway access barrier" };
            var obsolete = district.root.GetComponentsInChildren<MeshFilter>().Where(f =>
            {
                if (!names.Contains(f.name)) return false;
                var b = f.GetComponent<MeshRenderer>().bounds;
                return b.center.x < 118 && b.max.z > 137 && b.min.z < 413;
            }).ToArray();
            WestbruchAuditCompletion.RemoveRetainedTriangles(district, obsolete);
            foreach (var f in obsolete) if (f) UnityEngine.Object.DestroyImmediate(f.gameObject);
            var steel = Material("BV7_RailBoundarySteel", null, new Color(.22f, .25f, .24f), .22f, .55f);
            var points = new List<Vector3>();
            for (int i = 0; i <= 96; i++)
            {
                float z = Mathf.Lerp(97 + 13 * (523f / 175), 97 + 106 * (523f / 175), i / 96f);
                // Follow the OUTSIDE of the passing loop, including the platform.
                float platform = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(180, 215, z)) *
                    (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(330, 370, z)));
                float x = LoopX(z) - 3f - platform * 5.7f;
                var hits = Physics.RaycastAll(new Vector3(x, 20, z), Vector3.down, 40, 1, QueryTriggerInteraction.Ignore);
                var hit = hits.Where(h => Natural(h.collider)).OrderByDescending(h => h.point.y).FirstOrDefault();
                if (!hit.collider) throw new Exception("No ground beneath railway boundary " + x + "," + z);
                var p = new Vector3(x, hit.point.y - .035f, z); points.Add(p);
                if (i > 0 && i < 96) Cylinder("BV7_Grounded rail perimeter post", root, p + Vector3.up * .64f, .034f, 1.35f, steel, true);
                if (i == 0) continue;
                var a = points[i - 1];
                foreach (float height in new[] { .27f, .66f, 1.15f })
                    Beam("BV7_Outside track safety wire", root, a + Vector3.up * height, p + Vector3.up * height, .022f, steel);
                var barrier = Box("BV7_Rail perimeter collision", root, (a + p) * .5f + Vector3.up * .62f,
                    new Vector3(.08f, 1.25f, Vector3.Distance(a, p)), steel, true);
                barrier.transform.rotation = Quaternion.LookRotation(p - a); barrier.GetComponent<MeshRenderer>().enabled = false;
            }
            report.Add("Rail boundary: removed " + obsolete.Length + " legacy parts crossing the passing loop; built 96 outer spans.");
        }
        static void RepairFoliage()
        {
            int removed = 0, anchored = 0, measured = 0;
            var seen = new HashSet<Mesh>();
            foreach (var f in district.root.GetComponentsInChildren<MeshFilter>())
            {
                var m = f.sharedMesh; var r = f.GetComponent<MeshRenderer>();
                if (!m || !m.isReadable || !r || !r.sharedMaterial || !r.sharedMaterial.IsKeywordEnabled("_ZR_FOLIAGE") || !seen.Add(m)) continue;
                var roots = m.uv4; var heights = m.uv3;
                if (roots.Length != m.vertexCount || heights.Length != m.vertexCount) continue;
                var v = m.vertices; var triangles = m.triangles; var rejected = new HashSet<Vector2>();
                var firstIndex = new Dictionary<Vector2, int>();
                foreach (int i in triangles) if (InCore(roots[i]) && !firstIndex.ContainsKey(roots[i])) firstIndex[roots[i]] = i;
                var used = firstIndex.Keys;
                var floors = new Dictionary<Vector2, float>();
                foreach (var p in used)
                {
                    measured++;
                    int index = firstIndex[p];
                    bool valid = Soil(p.x, p.y, heights[index].x, out var hit);
                    if (valid) foreach (var offset in new[] { new Vector2(.12f, 0), new Vector2(-.12f, 0), new Vector2(0, .12f), new Vector2(0, -.12f) })
                    {
                        if (!Soil(p.x + offset.x, p.y + offset.y, hit.point.y, out var nearby) || Mathf.Abs(nearby.point.y - hit.point.y) > .25f)
                        { valid = false; break; }
                    }
                    if (!valid) { rejected.Add(p); removed++; continue; }
                    floors[p] = hit.point.y - .025f;
                    if (Mathf.Abs(floors[p] - heights[index].x) > .015f) anchored++;
                }
                if (used.Count == 0) continue;
                for (int i = 0; i < v.Length; i++) if (floors.TryGetValue(roots[i], out float floor))
                {
                    var point = f.transform.TransformPoint(v[i]); point.y += floor - heights[i].x;
                    v[i] = f.transform.InverseTransformPoint(point); heights[i].x = floor;
                }
                var keep = new List<int>();
                for (int i = 0; i < triangles.Length; i += 3)
                    if (!rejected.Contains(roots[triangles[i]]) && !rejected.Contains(roots[triangles[i + 1]]) && !rejected.Contains(roots[triangles[i + 2]]))
                        keep.AddRange(new[] { triangles[i], triangles[i + 1], triangles[i + 2] });
                m.vertices = v; m.uv3 = heights; m.SetTriangles(keep, 0); m.RecalculateBounds();
                var b = m.bounds; b.Expand(new Vector3(1, 1, 1)); m.bounds = b; EditorUtility.SetDirty(m);
            }
            report.Add($"Final visible-surface planting pass: {measured} root groups; {removed} rejected on paving/structures/edges, {anchored} anchored to final soil.");
        }
        static void RepairShrubs()
        {
            int removed = 0, moved = 0;
            foreach (var lod in district.GetComponentsInChildren<LODGroup>().ToArray())
            {
                var p = lod.transform.position;
                if (!InCore(new Vector2(p.x, p.z)) || p.y > 9) continue;
                if (!lod.GetComponentsInChildren<Renderer>().Any(r => r.sharedMaterial && r.sharedMaterial.IsKeywordEnabled("_ZR_SHRUB"))) continue;
                // Balcony planters belong to their facade, not the ground pass.
                if (lod.transform.GetComponentsInParent<Transform>().Any(t => t.name.Contains("Balcony") || t.name.Contains("balcony"))) continue;
                if (!Soil(p.x, p.z, p.y, out var hit)) { UnityEngine.Object.DestroyImmediate(lod.gameObject); removed++; continue; }
                p.y = hit.point.y - .025f;
                if (Mathf.Abs(lod.transform.position.y - p.y) > .015f) { lod.transform.position = p; moved++; }
            }
            report.Add($"Ground shrubs: {removed} on hard surfaces removed; {moved} roots seated.");
        }
        static Mesh Save(Mesh m, string name)
        {
            string path = Generated + "/Meshes/BV7_" + name + ".asset"; var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            m.name = "BV7_" + name;
            if (old) { EditorUtility.CopySerialized(m, old); UnityEngine.Object.DestroyImmediate(m); EditorUtility.SetDirty(old); return old; }
            AssetDatabase.CreateAsset(m, path); return m;
        }
        static void PlantBank()
        {
            var random = new System.Random(7112026);
            float R(float a, float b) => Mathf.Lerp(a, b, (float)random.NextDouble());
            var meshes = Enumerable.Range(0, 3).Select(BahnhofStreetscape.BuildMeadowGrass).ToArray();
            var groups = new Dictionary<Vector2Int, List<(Mesh mesh, Matrix4x4 matrix, Vector3 root)>>();
            for (int i = 0; i < 34000; i++)
            {
                float x = R(102, 134.4f), z = R(115, 412);
                float patches = Mathf.PerlinNoise(x * .21f + 4, z * .13f);
                if (R(0, 1) > Mathf.Lerp(.20f, .92f, Mathf.InverseLerp(.28f, .69f, patches))) continue;
                if (!Soil(x, z, 6, out var h) || h.point.y < .12f || h.point.y > 5.6f ||
                    !(h.collider.name.Contains("railway earth bank") || h.collider.name == "Passing loop earth formation")) continue;
                // Exclude elevated stairs, walkways and platforms as well.
                if (Physics.Raycast(h.point + Vector3.up * .1f, Vector3.up, out var roof, 12, 1) && !Natural(roof.collider)) continue;
                float width = R(.8f, 1.5f), height = R(.8f, 1.75f);
                var mesh = meshes[i % 3]; var p = h.point - Vector3.up * .035f;
                var rotation = Quaternion.FromToRotation(Vector3.up, h.normal) * Quaternion.Euler(0, R(0, 360), 0);
                var key = new Vector2Int(Mathf.FloorToInt(x / 20), Mathf.FloorToInt(z / 20));
                if (!groups.TryGetValue(key, out var list)) groups[key] = list = new List<(Mesh, Matrix4x4, Vector3)>();
                list.Add((mesh, Matrix4x4.TRS(p, rotation, new Vector3(width, height, width)), p));
            }
            foreach (var group in groups)
            {
                var holder = Group("BV7_Bank meadow LOD " + group.Key, root, Vector3.zero); var renderers = new Renderer[2];
                for (int lod = 0; lod < 2; lod++)
                {
                    var parts = group.Value.Where((p, i) => lod == 0 || i % 2 == 0).ToArray();
                    var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
                    mesh.CombineMeshes(parts.Select(p => new CombineInstance { mesh = p.mesh, transform = p.matrix }).ToArray(), true, true);
                    var uv2 = new List<Vector2>(); var uv3 = new List<Vector2>();
                    foreach (var part in parts) foreach (var v in part.mesh.vertices)
                    { uv2.Add(new Vector2(part.root.y, Mathf.Clamp01(v.y / part.mesh.bounds.max.y))); uv3.Add(new Vector2(part.root.x, part.root.z)); }
                    mesh.SetUVs(2, uv2); mesh.SetUVs(3, uv3); mesh = Save(mesh, "BankPlants_" + group.Key.x + "_" + group.Key.y + "_" + lod);
                    var go = Group("BV7_Rooted embankment grasses " + lod, holder.transform, Vector3.zero);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = BahnhofDetailKit.Load("DistrictFlora");
                    r.shadowCastingMode = lod == 0 ? ShadowCastingMode.On : ShadowCastingMode.Off; renderers[lod] = r;
                }
                var level = holder.AddComponent<LODGroup>(); level.SetLODs(new[] { new LOD(.17f, new[] { renderers[0] }), new LOD(.018f, new[] { renderers[1] }) }); level.RecalculateBounds();
            }
            report.Add("Authored uneven bank grass clumps: " + groups.Sum(g => g.Value.Count));
        }
        static void EdgeDetails()
        {
            var dark = Material("BV7_WeatheredDrainStone", "concrete", new Color(.25f, .27f, .25f), .10f);
            var light = Material("BV7_DrainCapStone", "concrete", new Color(.51f, .51f, .46f), .13f);
            // Shallow engineered drain at the toe, interrupted for the station
            // approach. Its foundation reaches the earth instead of hovering.
            foreach (var span in new[] { new Vector2(119, 229), new Vector2(288, 337) })
            for (float z = span.x; z < span.y; z += 1.5f)
            {
                float x = 134.0f;
                if (!Soil(x, z, 1, out var h)) continue;
                float y = h.point.y;
                Box("BV7_Bank drainage bed", root, new Vector3(x, y - .035f, z), new Vector3(.36f, .09f, 1.49f), dark, false, .8f);
                foreach (float side in new[] { -1f, 1f })
                    Box("BV7_Bank channel coping", root, new Vector3(x + side * .21f, y + .005f, z), new Vector3(.09f, .14f, 1.49f), light, false, .8f);
            }
        }
        internal static void Validate(Action<string, bool, string> record)
        {
            Physics.SyncTransforms();
            int checkedRoots = 0; var faults = new List<string>(); var unique = new HashSet<Vector3Int>();
            foreach (var f in district.root.GetComponentsInChildren<MeshFilter>())
            {
                var m = f.sharedMesh; var r = f.GetComponent<MeshRenderer>();
                if (!m || !m.isReadable || !r || !r.sharedMaterial || !r.sharedMaterial.IsKeywordEnabled("_ZR_FOLIAGE")) continue;
                var roots = m.uv4; var heights = m.uv3; if (roots.Length != m.vertexCount || heights.Length != m.vertexCount) continue;
                foreach (int index in m.triangles.Distinct())
                {
                    var p = roots[index]; if (!InCore(p)) continue;
                    var key = new Vector3Int(Mathf.RoundToInt(p.x * 1000), Mathf.RoundToInt(heights[index].x * 1000), Mathf.RoundToInt(p.y * 1000));
                    if (!unique.Add(key)) continue; checkedRoots++;
                    if (!Soil(p.x, p.y, heights[index].x, out var h) || heights[index].x - h.point.y > .025f || heights[index].x - h.point.y < -.12f)
                        if (faults.Count < 15) faults.Add(p + " " + (h.collider ? h.collider.name : "no soil"));
                }
            }
            record("District plants grow on exposed soil, not hard surfaces", checkedRoots > 0 && faults.Count == 0, checkedRoots + " roots; " + string.Join("; ", faults));
            var treeSoils = district.GetComponentsInChildren<Collider>().Where(c=>c.name=="BV3_Retained tree garden soil").ToArray();
            int domeErrors=0;
            foreach(var c in treeSoils)
            {
                var visible=c.GetComponent<MeshRenderer>().bounds;
                if(!(c is MeshCollider)||Mathf.Abs(c.bounds.max.y-visible.max.y)>.005f) domeErrors++;
                if(c.Raycast(new Ray(visible.center+Vector3.up*10,Vector3.down),out var hit,20)
                    && Mathf.Abs(hit.point.y-visible.max.y)>.005f)domeErrors++;
            }
            record("Round tree beds use flat visible soil, not capsule domes",treeSoils.Length==2&&domeErrors==0,treeSoils.Length+" beds; "+domeErrors+" mismatches");
            var rails = district.GetComponentsInChildren<MeshRenderer>().Where(r => r.name == "Passing loop rail").ToArray();
            var fences = district.root.GetComponentsInChildren<MeshRenderer>().Where(r => r.name == "Terminated railway safety wire" || r.name == "Grounded rail boundary post" || r.name == "BV7_Outside track safety wire" || r.name == "BV7_Grounded rail perimeter post").ToArray();
            int collisions = 0;
            foreach (var rail in rails)
            {
                var clearance = rail.bounds; clearance.Expand(new Vector3(.6f, 1.6f, .02f));
                collisions += fences.Count(f => f.bounds.Intersects(clearance));
            }
            record("Passing loop rails clear of boundary fencing", rails.Length > 0 && collisions == 0, rails.Length + " rail sections; " + collisions + " fence contacts");
            int kerbs = 0, badFaces = 0;
            foreach (var f in district.GetComponentsInChildren<MeshFilter>().Where(f => f.name.StartsWith("BV5_Chamfered kerb ")))
            {
                kerbs++; var m = f.sharedMesh; var v = m.vertices; var t = m.triangles; var center = m.bounds.center;
                for (int i = 0; i < t.Length; i += 3)
                {
                    var n = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]);
                    if (Vector3.Dot(n, (v[t[i]] + v[t[i + 1]] + v[t[i + 2]]) / 3 - center) < -.00001f) badFaces++;
                }
            }
            record("Kerb geometry faces outward along the street network", kerbs > 300 && badFaces == 0, kerbs + " kerbstones; " + badFaces + " inward faces");
            File.AppendAllLines("QA/district-core-renewal.txt", new[] { "Surface checks: " + checkedRoots + "; failures " + faults.Count, "Rail/fence contacts: " + collisions });
        }
    }
}
