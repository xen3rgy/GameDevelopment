using UnityEngine;

namespace ZeroRise
{
    public sealed class VehicleDriver:MonoBehaviour
    {
        public PlayerAvatar avatar;public Transform wheel;public TrafficVehicle vehicle;
        void Start(){avatar.BindSteeringWheel(wheel);avatar.VehiclePose("Drive",0);}
        void Update()
        {
            float steer=vehicle&&vehicle.steering.Length>0?Mathf.DeltaAngle(0,vehicle.steering[0].localEulerAngles.y):0;
            wheel.localRotation=Quaternion.Euler(55,0,0)*Quaternion.AngleAxis(-steer*2,Vector3.up);avatar.Steer(steer/40);
        }
    }
}
