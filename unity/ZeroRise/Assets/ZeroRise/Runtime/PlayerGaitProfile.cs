using UnityEngine;

namespace ZeroRise
{
    public static class PlayerGaitProfile
    {
        public const float WalkSpeed=1.45f,JogSpeed=3.2f,RunSpeed=5.8f;
        public const float WalkCycle=.64f,JogCycle=.56f,RunCycle=.50f;
        public const float WalkStance=.58f,JogStance=.30f,RunStance=.18f;
        public static float Stride(float speed)
        {
            if(speed<WalkSpeed)return WalkSpeed*WalkCycle;
            if(speed<JogSpeed)return Mathf.Lerp(WalkSpeed*WalkCycle,JogSpeed*JogCycle,Mathf.InverseLerp(WalkSpeed,JogSpeed,speed));
            return Mathf.Lerp(JogSpeed*JogCycle,RunSpeed*RunCycle,Mathf.InverseLerp(JogSpeed,RunSpeed,speed));
        }
    }
}
