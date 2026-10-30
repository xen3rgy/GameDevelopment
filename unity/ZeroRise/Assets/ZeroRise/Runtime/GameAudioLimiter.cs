using System;
using UnityEngine;

namespace ZeroRise
{
    // Linked-channel safety headroom after the listener mix. The normal mix
    // sits below this threshold; release prevents abrupt gain pumping.
    public sealed class GameAudioLimiter : MonoBehaviour
    {
        float gain=1;double release;
        void Awake()=>release=Math.Exp(-1.0/(AudioSettings.outputSampleRate*.09));
        void OnAudioFilterRead(float[] data,int channels)
        {
            for(int i=0;i<data.Length;i+=channels)
            {
                float peak=0;for(int c=0;c<channels;c++)peak=Math.Max(peak,Math.Abs(data[i+c]));
                float target=peak>.90f?.90f/peak:1;
                gain=target<gain?target:(float)(1-(1-gain)*release);
                for(int c=0;c<channels;c++)data[i+c]*=gain;
            }
        }
    }
}
