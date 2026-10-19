using UnityEngine;

namespace ZeroRise
{
    public sealed class OrbitCamera : MonoBehaviour
    {
        public Transform target;
        public LayerMask obstacles = 1;
        public float distance = 5.5f;
        public float targetHeight = 1.5f;
        float yaw, pitch = 19, boom = 5.5f;
        Vector3 currentAim,aimVelocity;
        bool initialized;
        Transform ignoredVehicle;
        bool releasingVehicle;
        readonly RaycastHit[] hits = new RaycastHit[32];
        public void Follow(Transform next, float height, float desiredDistance)
        { target = next; targetHeight = height; distance = desiredDistance; }
        public void IgnoreVehicle(Transform vehicle){ignoredVehicle=vehicle;releasingVehicle=false;}
        public void ReleaseVehicle(Transform vehicle){if(ignoredVehicle==vehicle)releasingVehicle=true;}
        public void Rotate(float yawDegrees,float pitchDegrees){yaw+=yawDegrees;pitch=Mathf.Clamp(pitch+pitchDegrees,10,64);}
        void LateUpdate()
        {
            if (!target) return;
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Rotate(Input.GetAxis("Mouse X")*2.3f,-Input.GetAxis("Mouse Y")*1.7f);
            }
            var desiredAim = target.position + Vector3.up * targetHeight;
            if(!initialized){currentAim=desiredAim;initialized=true;}
            currentAim=Vector3.SmoothDamp(currentAim,desiredAim,ref aimVelocity,.12f);
            var aim=currentAim;
            if(ignoredVehicle&&releasingVehicle&&Vector3.Distance(aim,desiredAim)<.08f){ignoredVehicle=null;releasingVehicle=false;}
            var rotation = Quaternion.Euler(pitch, yaw, 0);
            var backwards = rotation * Vector3.back;
            float allowed = distance;
            int count = Physics.SphereCastNonAlloc(aim, .24f, backwards, hits, distance, obstacles, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (hits[i].transform.IsChildOf(target)) continue;
                if(ignoredVehicle&&hits[i].transform.IsChildOf(ignoredVehicle))continue;
                allowed = Mathf.Min(allowed, Mathf.Max(.35f, hits[i].distance - .18f));
            }
            // Pull in immediately before an obstacle, restore gently. No lerp across a wall.
            boom = allowed < distance-.01f && allowed < boom ? allowed : Mathf.Lerp(boom, allowed, 1 - Mathf.Exp(-3.5f * Time.deltaTime));
            transform.SetPositionAndRotation(aim + backwards * boom, rotation);
        }
    }
}
