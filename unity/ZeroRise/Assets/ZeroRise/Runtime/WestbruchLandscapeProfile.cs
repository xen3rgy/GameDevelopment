using UnityEngine;

namespace ZeroRise
{
    // Shared deterministic profile: the city apron stays unchanged. The same
    // function drives mesh vertices, biome placement and landscape verification.
    public static class WestbruchLandscapeProfile
    {
        public const float LakeLevel = 14f;
        public static readonly Vector2 LakeCentre = new Vector2(730, 360);
        public static float Fade(float a, float b, float v) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(a, b, v));
        public static float Noise(float x, float z, float scale, float seed = 0) => Mathf.PerlinNoise(x * scale + 31 + seed, z * scale + 73 + seed);
        public static float LakeRadius(float x, float z)
        {
            float dx = (x - LakeCentre.x) / 130, dz = (z - LakeCentre.y) / 96;
            float angle = Mathf.Atan2(dz, dx);
            return Mathf.Sqrt(dx * dx + dz * dz) / (1 + .10f * Mathf.Sin(angle * 3 + .8f) + .055f * Mathf.Sin(angle * 7));
        }
        static float Massif(float x, float z, float cx, float cz, float sx, float sz, float height)
        {
            float dx = (x - cx) / sx, dz = (z - cz) / sz;
            float envelope = Mathf.Exp(-(dx * dx + dz * dz) * 1.8f);
            float ridge = 1 - Mathf.Abs(Noise(x + Noise(x,z,.003f) * 110, z, .0065f) * 2 - 1);
            float erosion = 1 - Mathf.Abs(Noise(x + Noise(x,z,.008f)*48,z,.023f,9)*2-1);
            float detail = erosion * .075f + Noise(x,z,.041f,17) * .012f;
            return envelope * height * (.60f + ridge * .31f + detail);
        }
        public static float Height(float x, float z, float original)
        {
            float influence = Fade(402, 615, x) * Fade(-650, -120, z);
            if (influence <= 0) return original;
            float hills = 8 + 31 * Noise(x,z,.004f) + 11 * Noise(x,z,.013f) + 2.7f * Noise(x,z,.046f);
            float mountain = Mathf.Max(Massif(x,z,1240,960,400,600,410), Massif(x,z,1510,430,360,420,490), Massif(x,z,880,1450,350,470,370));
            mountain += Massif(x,z,1880,1510,680,620,270) * .35f;
            mountain = Mathf.Max(mountain,Massif(x,z,2800,2100,1000,900,480),Massif(x,z,2900,100,1000,800,360));
            float regional = hills + mountain;
            float radius = LakeRadius(x,z);
            if (radius < 1.72f)
            {
                // Dry shore above the water, then shallow gravel shelf and deep basin.
                float basin = Mathf.Lerp(2.4f, LakeLevel + 1.2f, Mathf.Pow(Mathf.Clamp01(radius), 2.7f));
                basin += (Noise(x,z,.12f) - .5f) * .35f;
                regional = Mathf.Lerp(basin, regional, Fade(1.03f,1.72f,radius));
            }
            return Mathf.Lerp(original, regional, influence);
        }
        public static float SnowCover(Vector3 point, Vector3 normal)
        {
            float line = 252 + (Noise(point.x,point.z,.011f) - .5f) * 65;
            return Fade(line, line + 48, point.y) * Fade(.43f,.80f,normal.y);
        }
    }
}
