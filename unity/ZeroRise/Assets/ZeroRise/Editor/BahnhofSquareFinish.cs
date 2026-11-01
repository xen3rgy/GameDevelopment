using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ZeroRise.Editor.WestbruchGeometry;
namespace ZeroRise.Editor
{
    internal static class BahnhofSquareFinish
    {
        public static void Apply(Transform district)
        {
            var old=district.Find("BV3_Station square gardens");if(old)Object.DestroyImmediate(old.gameObject);
            var root=Group("BV3_Station square gardens",district,Vector3.zero).transform;
            var stone=Material("BV3_SquareWarmStone","concrete",new Color(.56f,.57f,.51f),.11f);
            var wood=Material("BV3_SquareSeatTimber","wood",new Color(.40f,.29f,.18f),.15f);
            var steel=Material("BV3_SquareSeatIron",null,new Color(.14f,.19f,.18f),.23f,.35f);
            var soil=BahnhofDetailKit.Load("GardenEarth");
            var shrub=Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None).FirstOrDefault(l=>l.name.StartsWith("Dense low courtyard shrub"));
            foreach(float z in new[]{242f,288})
            {
                var bed=Group("BV3_Circular station tree garden "+z,root,new Vector3(171,.14f,z)).transform;
                var vertices=new List<Vector3>();var indices=new List<int>();
                for(int i=0;i<32;i++)
                {
                    float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;
                    Vector3 P(float r,float y,float angle)=>new Vector3(Mathf.Cos(angle)*r,y,Mathf.Sin(angle)*r);
                    void Q(Vector3 a0,Vector3 b0,Vector3 c0,Vector3 d0){int n=vertices.Count;vertices.AddRange(new[]{a0,b0,c0,d0});indices.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
                    Q(P(3.1f,.30f,a),P(3.1f,.30f,b),P(3.4f,.30f,b),P(3.4f,.30f,a));
                    Q(P(3.4f,-.06f,a),P(3.4f,.30f,a),P(3.4f,.30f,b),P(3.4f,-.06f,b));
                    Q(P(3.1f,-.06f,b),P(3.1f,.30f,b),P(3.1f,.30f,a),P(3.1f,-.06f,a));
                }
                BahnhofDetailKit.Mesh("BV3_Closed sandstone tree surround",bed,vertices,indices,stone,true);
                Cylinder("BV3_Retained tree garden soil",bed,new Vector3(0,.10f,0),3.12f,.20f,soil,true);
                var tree=district.GetComponentsInChildren<LODGroup>().FirstOrDefault(t=>t.name=="Bahnhof street tree"&&Vector2.Distance(new Vector2(t.transform.position.x,t.transform.position.z),new Vector2(171,z))<.2f);
                if(tree){var p=tree.transform.position;p.y=.315f;tree.transform.position=p;}
                for(int i=0;i<10;i++)
                {
                    float angle=(28+i*13)*Mathf.Deg2Rad;var seat=Group("BV3_Curved timber seat segment",bed,new Vector3(Mathf.Cos(angle)*3.8f,0,Mathf.Sin(angle)*3.8f),-angle*Mathf.Rad2Deg).transform;
                    for(int slat=0;slat<4;slat++)Box("BV3_Seat timber slat",seat,new Vector3(-.23f+slat*.15f,.46f,0),new Vector3(.13f,.065f,.83f),wood,false);
                    if(i%2==0)foreach(float side in new[]{-.22f,.22f})Box("BV3_Seat buried iron leg",seat,new Vector3(side,.20f,0),new Vector3(.055f,.52f,.52f),steel,true);
                }
                if(shrub)for(int i=0;i<7;i++)
                {
                    float a=i*Mathf.PI*2/7;var plant=Object.Instantiate(shrub.gameObject,bed);plant.name="BV3_Rooted tree-garden shrub";
                    plant.transform.localPosition=new Vector3(Mathf.Cos(a)*2.2f,.18f,Mathf.Sin(a)*2.2f);plant.transform.localScale=Vector3.one*.48f;
                }
            }
            var border=Material("BV3_SquarePavingBands","brick",new Color(.44f,.38f,.29f),.08f);
            foreach(float z in new[]{254.0f,276})Box("BV3_Inlaid square paving band",root,new Vector3(174,.146f,z),new Vector3(48,.012f,.24f),border,false,1.2f);
            // A modest drinking fountain and dog bowl make the waiting area usable.
            var fountain=Group("BV3_Public drinking fountain",root,new Vector3(182,.14f,289)).transform;
            Cylinder("BV3_Fountain buried stone pedestal",fountain,Vector3.up*.42f,.22f,.9f,stone,true);
            Cylinder("BV3_Fountain shallow basin",fountain,new Vector3(0,.89f,0),.38f,.10f,steel);
            Box("BV3_Fountain push tap",fountain,new Vector3(0,1.0f,0),new Vector3(.09f,.16f,.15f),steel);
        }
    }
}
