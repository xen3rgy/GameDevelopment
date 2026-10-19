using UnityEngine;

namespace ZeroRise
{
    public sealed class TrafficVehicle:MonoBehaviour
    {
        public int kind;
        public float length=4,width=1.72f;
        public BoxCollider bodyCollider;
        public Transform[] steering,rolling;
        public Renderer[] rearLamps,indicators;
        public Material tailMaterial,brakeMaterial,amberMaterial,offMaterial;
        [System.NonSerialized] public int from,to,next,completedTurns,leftTurns,rightTurns,straightMoves;
        [System.NonSerialized] public float progress,speed,desiredSpeed,travelled,waitingSince=-1,waitingTime,maxWait;
        [System.NonSerialized] public bool turning;
        [System.NonSerialized] public int heldJunction=-1;
        [System.NonSerialized] public Vector3[] curve;
        [System.NonSerialized] public float[] arc;
        [System.NonSerialized] public float curveLength;
        [System.NonSerialized] public Rigidbody body;
        float wheelAngle;
        public void Pose(Vector3 position,Vector3 forward,float distance,float turn,float dt,bool immediate,float indicatorTurn=0)
        {
            var rotation=Quaternion.LookRotation(forward);
            if(!body)body=GetComponent<Rigidbody>();
            if(!immediate&&Application.isPlaying){body.MovePosition(position);body.MoveRotation(rotation);}
            else transform.SetPositionAndRotation(position,rotation);
            wheelAngle=Mathf.Repeat(wheelAngle+distance/.34f*Mathf.Rad2Deg,360);
            for(int i=0;i<rolling.Length;i++)
            {
                rolling[i].localRotation=Quaternion.AngleAxis(wheelAngle,Vector3.right);
                if(i<2)steering[i].localRotation=Quaternion.Slerp(steering[i].localRotation,Quaternion.Euler(0,Mathf.Clamp(turn,-40,40),0),1-Mathf.Exp(-8*dt));
            }
            bool braking=desiredSpeed<speed-.2f||speed<.4f;
            foreach(var lamp in rearLamps)lamp.sharedMaterial=braking?brakeMaterial:tailMaterial;
            int signal=indicatorTurn>5?1:indicatorTurn< -5?-1:0;bool on=Mathf.Repeat(Time.realtimeSinceStartup+from*.17f+to*.13f,1)<.5f;
            for(int i=0;i<indicators.Length;i++)indicators[i].sharedMaterial=on&&signal!=0&&(i%2==0?signal<0:signal>0)?amberMaterial:offMaterial;
        }
    }
}
