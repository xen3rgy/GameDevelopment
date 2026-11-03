using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    public static class WestbruchSupportAudit
    {
        // These are ground-mounted assembly roots. Decorative pieces are tested
        // against their respective assemblies instead of being dropped to earth.
        static readonly HashSet<string> furniture = new HashSet<string>
        {
            "Worn public bench", "Wheelie bin", "Discarded pallet", "Resident bicycle",
            "Station commuter bicycle", "BV2_Repair bicycle", "BV4_Cycle awaiting service",
            "BV2_Street directions", "BV2_Bus shelter", "Old Westbruch bus shelter",
            "BV4_Promenade lantern", "BV4_Place lantern standard", "BV4_Bus stop display",
            "BV4_Cafe terrace set", "BV4_North waiting pergola", "BV4_Cycle repair courtyard",
            "BV3_Public drinking fountain", "BV3_Washing line standard", "BV3_Service oil drum",
            "BV4_Garden cycle shelter", "BV4_Pedestrian lantern base", "BV3_Fence grounded post",
        };

        public static void Run()
        {
            EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);
            WriteReport();
            EditorApplication.Exit(0);
        }

        static void WriteReport()
        {
            Physics.SyncTransforms();
            var lines = new List<string>();
            foreach (var item in FindAssemblies())
            {
                var contacts = Contacts(item);
                if (contacts.Count == 0) continue;
                var min = contacts.Min(c => c.gap);
                var max = contacts.Max(c => c.gap);
                var highest = contacts.OrderByDescending(c => c.gap).First();
                bool supportedStack = min <= .025f && contacts.Any(c => c.surface == "Pallet deck slat");
                string status = min > .035f ? "FLOAT" : max > .075f && !supportedStack ? "EDGE " : "OK   ";
                lines.Add($"{status} | {item.name} | {item.position:F2} | {min:F3}..{max:F3} m | {highest.surface} at {highest.point:F2} | {item.parent?.name}");
            }
            Directory.CreateDirectory("QA");
            File.WriteAllLines("QA/map-support-audit.txt", lines.OrderBy(l => l));
            Debug.Log($"Map support audit measured {lines.Count} furniture assemblies");
        }

        internal static void Repair(Transform district)
        {
            var old = district.Find("BV6_Furniture foundations");
            if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            Physics.SyncTransforms();
            var foundationRoot = Group("BV6_Furniture foundations", district, Vector3.zero).transform;
            var stone = Material("BV6_FoundationConcrete", "concrete", new Color(.57f, .56f, .51f), .12f);
            var moving = new Dictionary<Transform, Vector3>();
            foreach (var item in FindAssemblies())
            {
                var contacts = Contacts(item);
                if (contacts.Count == 0) continue;
                float gap = contacts.Max(c => c.gap);
                if (item.IsChildOf(district) && item.name.StartsWith("Old European streetlight ", StringComparison.Ordinal) && gap > .035f && gap < .4f)
                {
                    float bottom = contacts.Min(c => c.point.y);
                    AddFooting(foundationRoot, item.position, bottom + .012f, contacts.Min(c => c.point.y - c.gap) - .035f, .32f, .32f, stone);
                }
                if (item.name == "BV2_Street directions" || item.name == "Discarded pallet" && item.parent == district)
                {
                    float lowestGap = contacts.Min(c => c.gap);
                    if (lowestGap > .035f && lowestGap < .4f) moving[item] = item.position - Vector3.up * lowestGap;
                }
                if (item.name == "Discarded pallet" && item.parent.name == "Workshop forecourt props")
                {
                    int layer = Mathf.RoundToInt((item.position.y - item.parent.position.y) / .17f);
                    var position = item.position;
                    position.y = item.parent.position.y + layer * .17f;
                    if (Mathf.Abs(position.y - item.position.y) > .001f) moving[item] = position;
                }
                if (item.name == "Discarded pallet" && item.parent.name == "Refuse and salvage alcove" && gap > .075f && gap < .12f)
                {
                    // Let the runners settle into this uneven dirt pocket. The
                    // deck stays above the highest sampled ground point.
                    moving[item] = item.position - Vector3.up * (gap - .01f);
                }
                if (item.name != "Old Westbruch bus shelter") continue;
                foreach (var foot in item.GetComponentsInChildren<MeshRenderer>().Where(r => r.name == "Steel leg" || r.name == "Shelter post"))
                {
                    var bounds = foot.bounds;
                    var hit = SupportAt(item, new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
                    if (hit.gap > .025f && hit.gap < .25f)
                        AddFooting(foundationRoot, bounds.center, bounds.min.y + .008f, bounds.min.y - hit.gap - .035f, bounds.size.x + .05f, bounds.size.z + .05f, stone);
                }
            }

            // Old Westbruch pallets already have copies in the global batches.
            // Remove those triangles at their old positions before moving the sources.
            var originalSources = moving.Keys.Where(t => !t.IsChildOf(district))
                .SelectMany(t => t.GetComponentsInChildren<MeshFilter>()).ToArray();
            WestbruchAuditCompletion.RemoveRetainedTriangles(district, originalSources);
            foreach (var move in moving) move.Key.position = move.Value;
            foreach (var source in originalSources) source.GetComponent<MeshRenderer>().enabled = true;
            WriteReport();
        }

        static void AddFooting(Transform parent, Vector3 position, float top, float bottom, float width, float depth, Material material)
        {
            position.y = (top + bottom) * .5f;
            Box("BV6_Buried furniture footing", parent, position, new Vector3(width, top - bottom, depth), material, true);
        }

        static IEnumerable<Transform> FindAssemblies() => UnityEngine.Object.FindObjectsByType<Transform>()
            .Where(t => furniture.Contains(t.name)
                || t.name.StartsWith("Old European streetlight ", StringComparison.Ordinal)
                || t.name.StartsWith("BV4_Individual resident bicycle ", StringComparison.Ordinal));

        static List<(float gap, string surface, Vector3 point)> Contacts(Transform item)
        {
            var points = new List<Vector3>();
            foreach (var filter in item.GetComponentsInChildren<MeshFilter>())
            {
                if (!filter.sharedMesh || !filter.sharedMesh.isReadable || !filter.GetComponent<MeshRenderer>()) continue;
                var vertices = filter.sharedMesh.vertices;
                foreach (var vertex in vertices) points.Add(filter.transform.TransformPoint(vertex));
            }
            var contacts = new List<(float gap, string surface, Vector3 point)>();
            if (points.Count == 0) return contacts;
            float bottom = points.Min(p => p.y);
            foreach (var point in points.Where(p => p.y <= bottom + .025f).Distinct()) contacts.Add(SupportAt(item, point));
            return contacts;
        }

        static (float gap, string surface, Vector3 point) SupportAt(Transform item, Vector3 point)
        {
            var hits = Physics.RaycastAll(point + Vector3.up * .3f, Vector3.down, 20, 1, QueryTriggerInteraction.Ignore);
            var hit = hits.Where(h => !h.collider.transform.IsChildOf(item) && h.normal.y > .35f)
                .OrderBy(h => h.distance).FirstOrDefault();
            float gap = hit.collider ? point.y - hit.point.y : 20;
            string surface = hit.collider ? hit.collider.name : "no support";
            if (item.name == "Discarded pallet" && item.parent)
            {
                foreach (Transform other in item.parent)
                {
                    if (other == item || other.name != item.name || other.position.y >= item.position.y - .06f) continue;
                    foreach (var slat in other.GetComponentsInChildren<MeshFilter>().Where(f => f.name == "Slat"))
                    {
                        var box = slat.sharedMesh.bounds;
                        var local = slat.transform.InverseTransformPoint(point);
                        if (Mathf.Abs(local.x - box.center.x) > box.extents.x + .002f || Mathf.Abs(local.z - box.center.z) > box.extents.z + .002f) continue;
                        float deck = slat.transform.TransformPoint(new Vector3(local.x, box.max.y, local.z)).y;
                        if (deck > point.y + .03f || point.y - deck >= gap) continue;
                        gap = point.y - deck;
                        surface = "Pallet deck slat";
                    }
                }
            }
            return (gap, surface, point);
        }
    }
}
