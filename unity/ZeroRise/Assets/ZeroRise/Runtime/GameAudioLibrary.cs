using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroRise
{
    public enum GroundSound { Concrete, Soil, Wood, Metal, Gravel, Grass }

    // Authored clips and mix data only. No middleware or runtime downloads.
    public sealed class GameAudioLibrary : ScriptableObject
    {
        public AudioClip engineIdle,engineLoad,engineStart,engineStop,roadTyres,tyreSlip;
        public AudioClip doorOpen,doorClose,cabinClick,horn,bodyImpact,lightMetal,seatImpact;
        public AudioClip[] concrete,soil,wood,metal,clothing,birds,drips;
        public AudioClip[] gravel,grass,bushRustles;
        public AudioClip leaves,fence;
        // These are individual visible shrub crowns baked by the editor. Large
        // render batches, trees and empty space between plants are never used.
        [Serializable]
        public struct FoliageVolume
        {
            public Vector3 center,radii;
        }
        public FoliageVolume[] foliage=Array.Empty<FoliageVolume>();

        sealed class TerrainSurface
        {
            public float[,,] weights;
            public GroundSound[] surfaces;
        }
        static readonly Dictionary<Collider,GroundSound> colliderSurfaces=new Dictionary<Collider,GroundSound>();
        static readonly Dictionary<TerrainData,TerrainSurface> terrainSurfaces=new Dictionary<TerrainData,TerrainSurface>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSurfaceCache(){colliderSurfaces.Clear();terrainSurfaces.Clear();}

        public AudioClip[] Steps(GroundSound surface)
        {
            switch(surface)
            {
                case GroundSound.Soil:return soil;
                case GroundSound.Wood:return wood;
                case GroundSound.Metal:return metal;
                case GroundSound.Gravel:return gravel!=null&&gravel.Length>0?gravel:soil;
                case GroundSound.Grass:return grass!=null&&grass.Length>0?grass:soil;
                default:return concrete;
            }
        }

        public static GroundSound SurfaceAt(Vector3 position)
        {
            if(!Physics.Raycast(position+Vector3.up*.4f,Vector3.down,out var hit,1.15f,1,QueryTriggerInteraction.Ignore))return GroundSound.Concrete;
            return SurfaceAt(hit);
        }
        public static GroundSound SurfaceAt(RaycastHit hit)
        {
            var collider=hit.collider;if(!collider)return GroundSound.Concrete;
            if(collider is TerrainCollider terrain)return TerrainAt(terrain,hit.point);
            if(colliderSurfaces.TryGetValue(collider,out var cached))return cached;
            // Authoring keeps a collider on each original surface even when its
            // renderer is disabled in favour of a spatial rendering batch.
            var renderer=collider.GetComponent<Renderer>();
            if(renderer&&renderer.sharedMaterial&&Classify(renderer.sharedMaterial.name,out cached))
            {colliderSurfaces[collider]=cached;return cached;}
            Classify(collider.name,out cached);colliderSurfaces[collider]=cached;return cached;
        }
        static GroundSound TerrainAt(TerrainCollider collider,Vector3 point)
        {
            var data=collider.terrainData;if(!data||data.alphamapLayers==0)return GroundSound.Soil;
            if(!terrainSurfaces.TryGetValue(data,out var cached))
            {
                // One small read per terrain at first contact; subsequent steps
                // sample the real paint mask without per-footfall allocations.
                var layers=data.terrainLayers;cached=new TerrainSurface{weights=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight),surfaces=new GroundSound[layers.Length]};
                for(int i=0;i<layers.Length;i++)
                {
                    cached.surfaces[i]=GroundSound.Soil;
                    if(layers[i]&&Classify(layers[i].name,out var surface))cached.surfaces[i]=surface;
                }
                terrainSurfaces[data]=cached;
            }
            var local=collider.transform.InverseTransformPoint(point);
            int x=Mathf.Clamp(Mathf.FloorToInt(local.x/data.size.x*data.alphamapWidth),0,data.alphamapWidth-1);
            int z=Mathf.Clamp(Mathf.FloorToInt(local.z/data.size.z*data.alphamapHeight),0,data.alphamapHeight-1);
            int best=0;float weight=-1;
            for(int i=0;i<cached.surfaces.Length;i++)
                if(cached.weights[z,x,i]>weight){best=i;weight=cached.weights[z,x,i];}
            return cached.surfaces[best];
        }
        static bool Classify(string label,out GroundSound surface)
        {
            string name=label.ToLowerInvariant();surface=GroundSound.Concrete;
            // Material names are authoritative: a concrete maintenance ramp or
            // sleeper should not acquire a metal sound from its descriptive name.
            if(name.Contains("concrete")||name.Contains("asphalt")||name.Contains("paving")||name.Contains("pavement")||name.Contains("kerb")||name.Contains("sidewalk"))return true;
            if(name.Contains("gravel")||name.Contains("ballast")||name.Contains("urban_yard")||name.Contains("compacted aggregate")){surface=GroundSound.Gravel;return true;}
            if(name.Contains("grass")||name.Contains("turf")){surface=GroundSound.Grass;return true;}
            if(name.Contains("earth")||name.Contains("soil")||name.Contains("mud")||name.Contains("sand")||name.Contains("sleeping court")){surface=GroundSound.Soil;return true;}
            if(name.Contains("timber")||name.Contains("pallet")||name.Contains("wood")){surface=GroundSound.Wood;return true;}
            if(name.Contains("grate")||name.Contains("drain cover")||name.Contains("steel")||name.Contains("metal")||name.Contains("railweb")||name.Contains("railhead")){surface=GroundSound.Metal;return true;}
            return false;
        }
    }
}
