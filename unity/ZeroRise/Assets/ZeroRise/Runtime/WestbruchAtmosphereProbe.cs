using System;
using System.Collections;
using System.IO;
using System.Threading;
using UnityEngine;

namespace ZeroRise
{
    // Only instantiated by explicit QA. Normal game sessions never record sound
    // or move the camera. Capture receives the real AudioListener DSP output.
    public sealed class WestbruchAtmosphereProbe : MonoBehaviour
    {
        float[] pcm;int written,channels=2;bool recording;
        void OnAudioFilterRead(float[] data,int count)
        {
            if(!recording||pcm==null)return;channels=count;
            int start=Volatile.Read(ref written),length=Math.Min(data.Length,pcm.Length-start);if(length<=0)return;
            Array.Copy(data,0,pcm,start,length);Volatile.Write(ref written,start+length);
        }
        public static IEnumerator Run(Camera camera,string output,Action<string,bool,string> record,bool captureAudio)
        {
            var sound=FindAnyObjectByType<WestbruchSoundscape>();if(!sound)yield break;
            WestbruchAtmosphereProbe probe=null;Vector3 original=camera.transform.position;Quaternion rotation=camera.transform.rotation;
            double began=AudioSettings.dspTime;
            if(captureAudio)
            {
                probe=camera.gameObject.AddComponent<WestbruchAtmosphereProbe>();
                probe.pcm=new float[AudioSettings.outputSampleRate*2*30];probe.recording=true;
            }
            string[] names={"street","underbridge","courtyard"};
            Vector3[] positions={new Vector3(72,1.8f,72.8f),new Vector3(118,1.8f,80),new Vector3(-39,1.8f,42)};
            Vector3[] looks={new Vector3(70,2,82),new Vector3(118,3.3f,66),new Vector3(-68,2.2f,32)};
            bool blends=true;var levels="";
            for(int pose=0;pose<positions.Length;pose++)
            {
                camera.transform.SetPositionAndRotation(positions[pose],Quaternion.LookRotation(looks[pose]-positions[pose]));
                float start=Time.realtimeSinceStartup;bool previewed=false;
                while(Time.realtimeSinceStartup-start<7)
                {
                    if(captureAudio&&!previewed&&Time.realtimeSinceStartup-start>4)
                    {
                        previewed=true;if(pose==1)sound.PlayDrain();
                        sound.EmitFootstep(positions[pose]+new Vector3(.25f,-1.6f,0),pose==2);
                    }
                    yield return null;
                }
                blends&=pose==1?sound.BridgeBlend>.97f:sound.BridgeBlend<.01f;
                levels+=names[pose]+" weight="+sound.BridgeBlend.ToString("F3")+" city="+sound.MixCityGain.ToString("F4")+" wind="+sound.MixWindGain.ToString("F4")+"; ";
                if(captureAudio){ScreenCapture.CaptureScreenshot(Path.Combine(output,"atmosphere-windows-"+names[pose]+".png"));for(int frame=0;frame<4;frame++)yield return null;}
            }
            record("Sheltered acoustic transition fades in and out with the actual camera",blends,levels);
            if(captureAudio)
            {
                probe.recording=false;yield return null;int length=Math.Min(Volatile.Read(ref probe.written),probe.pcm.Length);float peak=0;double energy=0;
                for(int i=0;i<length;i++){float x=probe.pcm[i];peak=Mathf.Max(peak,Mathf.Abs(x));energy+=x*x;}
                double rms=length>0?Math.Sqrt(energy/length):0;
                bool valid=length>AudioSettings.outputSampleRate*2&&peak>.0001f&&peak<.85f&&rms>.00001&&AudioSettings.dspTime-began>15;
                record("Standalone audio mixer produces audible unclipped stereo output",valid,"Samples "+length+"; channels "+probe.channels+"; peak "+peak.ToString("F5")+"; RMS "+rms.ToString("F6")+"; DSP elapsed "+(AudioSettings.dspTime-began).ToString("F2")+"s");
                if(length>0)Save(Path.Combine(output,"atmosphere-gameplay-mix.wav"),probe.pcm,length,probe.channels,AudioSettings.outputSampleRate);
                Destroy(probe);
            }
            camera.transform.SetPositionAndRotation(original,rotation);
        }
        static void Save(string path,float[] samples,int count,int channels,int rate)
        {
            using(var writer=new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);writer.Write((short)1);writer.Write((short)channels);writer.Write(rate);writer.Write(rate*channels*2);writer.Write((short)(channels*2));writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);for(int i=0;i<count;i++)writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(samples[i],-1,1)*32767));
            }
        }
    }
}
