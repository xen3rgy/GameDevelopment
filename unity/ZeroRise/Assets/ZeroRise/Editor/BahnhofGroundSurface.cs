using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ZeroRise.Editor
{
    internal static class BahnhofGroundSurface
    {
        const string MaskPath = "Assets/ZeroRise/Art/Streetscape2K/MeadowBlend.png";
        const int Resolution = 1024;
        const float Period = 768f;
        const float Origin = -192f;

        static readonly Vector2[] footpath =
        {
            new Vector2(254, 114), new Vector2(257, 120), new Vector2(259, 129),
            new Vector2(262, 138), new Vector2(263, 146), new Vector2(262, 154),
            new Vector2(264, 169)
        };

        internal static void SetBlendMask(Material material)
        {
            var pixels = new Color32[Resolution * Resolution];
            for (int row = 0; row < Resolution; row++)
            for (int column = 0; column < Resolution; column++)
            {
                float x = Origin + Period * (column + .5f) / Resolution;
                float z = Origin + Period * (row + .5f) / Resolution;
                var (grass, damp) = LayersAt(x, z);
                pixels[row * Resolution + column] = new Color(grass, damp, 0, 1);
            }

            var map = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false, true);
            map.SetPixels32(pixels);
            map.Apply(false);
            byte[] png = map.EncodeToPNG();
            Object.DestroyImmediate(map);
            if (!File.Exists(MaskPath) || !File.ReadAllBytes(MaskPath).SequenceEqual(png))
            {
                File.WriteAllBytes(MaskPath, png);
                AssetDatabase.ImportAsset(MaskPath, ImportAssetOptions.ForceSynchronousImport);
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(MaskPath);
            if (importer.sRGBTexture || importer.wrapMode != TextureWrapMode.Repeat || !importer.mipmapEnabled || importer.anisoLevel != 8)
            {
                importer.sRGBTexture = false;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.mipmapEnabled = true;
                importer.anisoLevel = 8;
                importer.SaveAndReimport();
            }
            material.SetTexture("_LayerMaskMap", AssetDatabase.LoadAssetAtPath<Texture2D>(MaskPath));
            material.SetTextureScale("_LayerMaskMap", Vector2.one / Period);
            material.SetTextureOffset("_LayerMaskMap", Vector2.one * (-Origin / Period));
            material.SetFloat("_UVBlendMask", 1);
            material.SetVector("_UVMappingMaskBlendMask", new Vector4(0, 1, 0, 0));
        }

        internal static (float grass, float damp) LayersAt(float x, float z)
        {
            float u = (x - Origin) / Period;
            float v = (z - Origin) / Period;
            float warpX = u + (Noise(u, v, 31, 17) - .5f) * .022f;
            float warpZ = v + (Noise(u, v, 27, 63) - .5f) * .024f;
            float broad = Noise(warpX, warpZ, 35, 36);
            float patches = Noise(warpX, warpZ, 119, 81);
            float crumbs = Noise(u, v, 367, 46);
            float cover = broad * .48f + patches * .33f + crumbs * .19f;
            float grass = Mathf.Lerp(.08f, .99f, Fade(.29f, .69f, cover));
            float damp = .08f + .72f * Fade(.54f, .82f, Noise(warpX, warpZ, 71, 12) * .73f + crumbs * .27f);
            float wear = TrackWear(x, z);
            return (grass * (1f - wear * .86f), Mathf.Lerp(damp, .68f, wear * .62f));
        }

        internal static float TrackWear(float x, float z)
        {
            if (x < 251 || x > 267 || z < 111 || z > 173) return 0;
            var point = new Vector2(x, z);
            float squaredDistance = float.PositiveInfinity;
            for (int i = 1; i < footpath.Length; i++)
            {
                Vector2 start = footpath[i - 1];
                Vector2 segment = footpath[i] - start;
                float along = Mathf.Clamp01(Vector2.Dot(point - start, segment) / segment.sqrMagnitude);
                squaredDistance = Mathf.Min(squaredDistance, (point - start - along * segment).sqrMagnitude);
            }
            float edge = Mathf.PerlinNoise(x * .72f, z * .64f);
            float width = Mathf.Lerp(.65f, 1.25f, edge);
            float breakup = Mathf.Lerp(.51f, .92f, Mathf.PerlinNoise(x * .23f + 7, z * .48f));
            return (1f - Fade(.16f, width, Mathf.Sqrt(squaredDistance))) * breakup;
        }

        static float Fade(float a, float b, float x) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(a, b, x));

        // Periodic value noise keeps the world-space mask continuous at its
        // distant wrap boundary. Domain warping hides the underlying lattice.
        static float Noise(float u, float v, int frequency, int seed)
        {
            float x = Mathf.Repeat(u, 1f) * frequency;
            float z = Mathf.Repeat(v, 1f) * frequency;
            int ix = Mathf.FloorToInt(x);
            int iz = Mathf.FloorToInt(z);
            float fx = Mathf.SmoothStep(0, 1, x - ix);
            float fz = Mathf.SmoothStep(0, 1, z - iz);
            int nx = (ix + 1) % frequency;
            int nz = (iz + 1) % frequency;
            return Mathf.Lerp(Mathf.Lerp(Hash(ix, iz, seed), Hash(nx, iz, seed), fx),
                Mathf.Lerp(Hash(ix, nz, seed), Hash(nx, nz, seed), fx), fz);
        }

        static float Hash(int x, int z, int seed)
        {
            unchecked
            {
                uint value = (uint)x * 747796405u ^ (uint)z * 2891336453u ^ (uint)seed * 277803737u;
                value ^= value >> 16;
                value *= 2246822519u;
                value ^= value >> 13;
                return (value & 65535u) / 65535f;
            }
        }
    }
}
