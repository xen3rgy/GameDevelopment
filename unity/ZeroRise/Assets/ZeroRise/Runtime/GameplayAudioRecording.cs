using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace ZeroRise
{
    // Actual listener output. Instantiated only by explicit sound release QA.
    public sealed class GameplayAudioRecording : MonoBehaviour
    {
        public struct Stats { public int frames,channels;public float peak;public double rms,seconds; }
        float[] data;int count,channels=2,rate;volatile bool recording;
        public static GameplayAudioRecording Begin(Camera camera,int seconds)
        {
            var take=camera.gameObject.AddComponent<GameplayAudioRecording>();take.rate=AudioSettings.outputSampleRate;
            take.data=new float[take.rate*2*seconds];take.recording=true;return take;
        }
        void OnAudioFilterRead(float[] buffer,int channelCount)
        {
            if(!recording||data==null)return;channels=channelCount;
            int offset=Volatile.Read(ref count),length=Math.Min(buffer.Length,data.Length-offset);if(length<=0)return;
            Array.Copy(buffer,0,data,offset,length);Volatile.Write(ref count,offset+length);
        }
        public Stats End(string path)
        {
            recording=false;int length=Volatile.Read(ref count);var stats=new Stats{frames=length/channels,channels=channels,seconds=length/(double)(rate*channels)};
            double energy=0;for(int i=0;i<length;i++){stats.peak=Math.Max(stats.peak,Math.Abs(data[i]));energy+=data[i]*data[i];}
            stats.rms=length>0?Math.Sqrt(energy/length):0;
            using(var writer=new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+length*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);writer.Write((short)1);writer.Write((short)channels);writer.Write(rate);writer.Write(rate*channels*2);writer.Write((short)(channels*2));writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(length*2);for(int i=0;i<length;i++)writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(data[i],-1,1)*32767));
            }
            Destroy(this);return stats;
        }
    }
}
