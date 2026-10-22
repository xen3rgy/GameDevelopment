using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ZeroRise.Editor
{
    // Offline sound editing. There is no synthesis, downloading or asset import
    // during gameplay. Sources and licensing are documented beside the WAVs.
    internal static class WestbruchAudioAssets
    {
        public const string Folder="Assets/ZeroRise/Art/Audio/";
        const int Rate=48000;
        public static AudioClip Clip(string name)=>AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+name+".wav");
        public static void Prepare()
        {
            AssetDatabase.Refresh();
            Derive("CityOriginal.mp3","CityDistant",55,900,62,2.0f);
            Derive("WindOriginal.wav","CourtyardWind",90,3400,54,2.0f);
            Derive("FridgeOriginal.wav","ShopCompressor",55,1300,24,1.0f);
            if(!File.Exists(Folder+"DrainResonance.wav"))
            {
                var rng=new System.Random(221026);
                float[] drain=new float[Rate*2];
                foreach(float at in new[]{.035f,.27f,.61f,1.19f})
                {
                    int offset=(int)(at*Rate);double phase=0;
                    for(int i=0;i<Rate*.37&&offset+i<drain.Length;i++)
                    {
                        double t=i/(double)Rate,f=980+1900*Math.Exp(-t*30);phase+=2*Math.PI*f/Rate;
                        double attack=1-Math.Exp(-t*1800),env=attack*Math.Exp(-t*18);
                        drain[offset+i]+=(float)((Math.Sin(phase)*.48+Math.Sin(phase*1.63)*.13+(rng.NextDouble()*2-1)*.12)*env);
                    }
                }
                Normalize(drain,.10f,.65f);Save("DrainResonance",drain);
                var tick=new float[(int)(Rate*.8f)];
                for(int i=0;i<tick.Length;i++){double t=i/(double)Rate;tick[i]=(float)((1-Math.Exp(-t*1200))*Math.Exp(-t*18)*(Math.Sin(t*2*Math.PI*740)*.28+Math.Sin(t*2*Math.PI*1247)*.17+Math.Sin(t*2*Math.PI*2113)*.10));}
                Normalize(tick,.075f,.55f);Save("FenceTick",tick);
                for(int variant=0;variant<3;variant++)foreach(bool soil in new[]{false,true})
                {
                    var foot=new float[(int)(Rate*.26f)];double low=0;
                    for(int i=0;i<foot.Length;i++)
                    {
                        double t=i/(double)Rate,n=rng.NextDouble()*2-1;low+=.14*(n-low);
                        double env=(1-Math.Exp(-t*1300))*Math.Exp(-t*(soil?34:42));
                        double body=Math.Sin(2*Math.PI*(92+variant*9)*t)*Math.Exp(-t*54);
                        foot[i]=(float)(env*((soil?low*.85:(n*.12+low*.35))+body*.32));
                    }
                    Normalize(foot,.11f,.68f);Save("Footstep"+(soil?"Soil":"Paving")+variant,foot);
                }
            }
            AssetDatabase.Refresh();
            foreach(string path in Directory.GetFiles(Folder,"*.wav"))
            {
                var importer=AssetImporter.GetAtPath(path.Replace('\\','/')) as AudioImporter;if(!importer)continue;
                var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.7f;settings.sampleRateSetting=AudioSampleRateSetting.OverrideSampleRate;settings.sampleRateOverride=Rate;
                settings.preloadAudioData=true;importer.defaultSampleSettings=settings;importer.forceToMono=true;importer.loadInBackground=false;importer.SaveAndReimport();
            }
        }
        static void Derive(string source,string name,float highPass,float lowPass,float seconds,float overlapSeconds)
        {
            if(File.Exists(Folder+name+".wav"))return;
            string path=Folder+"Sources/"+source;var importer=AssetImporter.GetAtPath(path) as AudioImporter;
            if(!importer)throw new Exception("Missing licensed audio source: "+path);
            var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;
            settings.sampleRateSetting=AudioSampleRateSetting.OverrideSampleRate;settings.sampleRateOverride=Rate;
            settings.preloadAudioData=true;importer.defaultSampleSettings=settings;importer.forceToMono=true;importer.SaveAndReimport();
            var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);clip.LoadAudioData();
            var original=new float[clip.samples*clip.channels];if(!clip.GetData(original,0))throw new Exception("Could not decode "+source);
            int count=Math.Min(clip.samples,(int)(seconds*Rate));var mono=new float[count];
            for(int i=0;i<count;i++){float sample=0;for(int c=0;c<clip.channels;c++)sample+=original[i*clip.channels+c];mono[i]=sample/clip.channels;}
            double lp=0,dc=0,a=1-Math.Exp(-2*Math.PI*lowPass/Rate),b=1-Math.Exp(-2*Math.PI*highPass/Rate);
            // Four settling passes prevent an artificial start transient in a
            // repeating recorded bed. A second lowpass gently reduces presence.
            for(int pass=0;pass<4;pass++)for(int i=0;i<mono.Length;i++){double sample=mono[i];dc+=b*(sample-dc);lp+=a*((sample-dc)-lp);if(pass==3)mono[i]=(float)lp;}
            int fade=Math.Min((int)(overlapSeconds*Rate),count/4);var loop=new float[count-fade];int core=count-fade*2;
            Array.Copy(mono,fade,loop,0,core);
            for(int i=0;i<fade;i++){double t=i/(double)(fade-1);loop[core+i]=(float)(mono[count-fade+i]*Math.Cos(t*Math.PI*.5)+mono[i]*Math.Sin(t*Math.PI*.5));}
            Normalize(loop,.13f,.65f);Save(name,loop);
        }
        static void Normalize(float[] data,float rmsTarget,float ceiling)
        {
            double sum=0,mean=0;foreach(float x in data)mean+=x;mean/=data.Length;float peak=0;
            for(int i=0;i<data.Length;i++){data[i]-=(float)mean;sum+=data[i]*data[i];peak=Math.Max(peak,Math.Abs(data[i]));}
            float gain=Math.Min(rmsTarget/(float)Math.Sqrt(sum/data.Length),ceiling/Math.Max(.00001f,peak));
            for(int i=0;i<data.Length;i++)data[i]*=gain;
        }
        static void Save(string name,float[] data)
        {
            if(name=="DrainResonance"||name=="FenceTick"||name.StartsWith("Footstep"))
                for(int i=0;i<96;i++){float fade=Mathf.SmoothStep(0,1,i/95f);data[i]*=fade;data[data.Length-1-i]*=fade;}
            using(var stream=File.Create(Folder+name+".wav"))using(var writer=new BinaryWriter(stream))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+data.Length*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(Rate);writer.Write(Rate*2);writer.Write((short)2);writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(data.Length*2);foreach(float x in data)writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(x,-1,1)*32767));
            }
        }
        public static void Validate(Action<string,bool,string> record)
        {
            int clips=0;float maxPeak=0,maxSeam=0;bool good=true;
            foreach(string name in new[]{"CityDistant","CourtyardWind","ShopCompressor","DrainResonance","FenceTick","FootstepPaving0","FootstepPaving1","FootstepPaving2","FootstepSoil0","FootstepSoil1","FootstepSoil2"})
            {
                var clip=Clip(name);if(!clip){good=false;continue;}clips++;clip.LoadAudioData();var data=new float[clip.samples*clip.channels];good&=clip.GetData(data,0);double square=0;
                foreach(float x in data){if(float.IsNaN(x)||float.IsInfinity(x))good=false;maxPeak=Mathf.Max(maxPeak,Mathf.Abs(x));square+=x*x;}
                good&=clip.frequency==Rate&&clip.channels==1&&square/data.Length>.00001&&square/data.Length<.08;
                if(name=="CityDistant"||name=="CourtyardWind"||name=="ShopCompressor")maxSeam=Mathf.Max(maxSeam,Mathf.Abs(data[0]-data[data.Length-1]));
            }
            record("Atmosphere clips decode with restrained levels and continuous loop boundaries",good&&clips==11&&maxPeak<.76f&&maxSeam<.05f,"Clips "+clips+"; peak "+maxPeak.ToString("F4")+"; maximum seam step "+maxSeam.ToString("F5"));
        }
    }
}
