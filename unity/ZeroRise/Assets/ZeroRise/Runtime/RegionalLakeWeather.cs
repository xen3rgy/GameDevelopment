using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace ZeroRise
{
    [RequireComponent(typeof(WaterSurface))]
    public sealed class RegionalLakeWeather : MonoBehaviour
    {
        WaterSurface water;
        void Awake() => water=GetComponent<WaterSurface>();
        void Update()
        {
            var clock=WorldWeatherClock.Instance;if(!clock||!water)return;
            float wind=Mathf.Clamp01(clock.WindStrength*clock.GustStrength);
            water.largeWindSpeed=Mathf.Lerp(4,25,wind);
            water.largeBand0Multiplier=Mathf.Lerp(.08f,.28f,wind);
            water.ripplesWindSpeed=Mathf.Lerp(2,16,wind);
            water.simulationFoamAmount=Mathf.Lerp(.005f,.08f,wind*wind);
        }
    }
}
