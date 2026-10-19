using System;
using UnityEngine;

namespace ZeroRise
{
    [Serializable] public class Point { public float x, z; public Vector3 Position(float y = 0) => new Vector3(x, y, z); }
    [Serializable] public class Plot : Point { public string id; public float w, d; }
    [Serializable] public class BuildingPlot : Plot { public int floors; public float yaw; public string kind, surface, tone; }
    [Serializable] public class CarPlot : Point { public float yaw; public bool driveable; }
    [Serializable] public class WestbruchLayout
    {
        public string name;
        public float width, depth;
        public Point spawn;
        public Plot[] roads, paths;
        public BuildingPlot[] buildings;
        public Point[] trees;
        public CarPlot[] cars;
    }
}
