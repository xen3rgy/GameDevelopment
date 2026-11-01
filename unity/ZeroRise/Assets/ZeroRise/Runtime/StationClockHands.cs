using UnityEngine;
namespace ZeroRise
{
    public sealed class StationClockHands:MonoBehaviour
    {
        Transform hour,minute;
        void Start(){hour=transform.Find("Hour hand");minute=transform.Find("Minute hand");}
        void Update()
        {
            if(!WorldWeatherClock.Instance||!hour||!minute)return;
            float t=WorldWeatherClock.Instance.Hour;
            hour.localRotation=Quaternion.Euler(0,0,-t*30);minute.localRotation=Quaternion.Euler(0,0,-t*360);
            hour.localPosition=hour.localRotation*new Vector3(0,.19f,0);
            minute.localPosition=minute.localRotation*new Vector3(0,.28f,-.02f);
        }
    }
}
