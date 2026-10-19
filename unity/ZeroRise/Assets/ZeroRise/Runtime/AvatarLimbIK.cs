using UnityEngine;

namespace ZeroRise
{
    // Analytic IK for the existing generic rig. No Humanoid conversion or
    // geometry allocation; knees/elbows retain an explicit bend direction.
    public static class AvatarLimbIK
    {
        public static void Solve(Transform upper,Transform lower,Transform tip,Vector3 target,Vector3 pole,Quaternion tipRotation,float weight)
        {
            if(!upper||!lower||!tip||weight<=0)return;
            var upperRest=upper.localRotation;var lowerRest=lower.localRotation;var tipRest=tip.rotation;
            var origin=upper.position;float a=Vector3.Distance(origin,lower.position),b=Vector3.Distance(lower.position,tip.position);
            var delta=target-origin;float length=delta.magnitude;
            if(length<.0001f||a<.0001f||b<.0001f)return;
            var direction=delta/length;length=Mathf.Clamp(length,Mathf.Abs(a-b)+.001f,a+b-.002f);target=origin+direction*length;
            var bend=Vector3.ProjectOnPlane(pole-origin,direction).normalized;
            if(bend.sqrMagnitude<.01f)bend=Vector3.ProjectOnPlane(upper.forward,direction).normalized;
            float cosine=Mathf.Clamp((a*a+length*length-b*b)/(2*a*length),-1,1);
            var joint=origin+direction*(a*cosine)+bend*(a*Mathf.Sqrt(1-cosine*cosine));
            upper.rotation=Quaternion.FromToRotation(lower.position-origin,joint-origin)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(tip.position-lower.position,target-lower.position)*lower.rotation;
            var solvedUpper=upper.localRotation;var solvedLower=lower.localRotation;
            upper.localRotation=Quaternion.Slerp(upperRest,solvedUpper,weight);lower.localRotation=Quaternion.Slerp(lowerRest,solvedLower,weight);
            tip.rotation=Quaternion.Slerp(tipRest,tipRotation,weight);
        }
    }
}
