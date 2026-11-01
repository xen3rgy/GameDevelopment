using UnityEngine;
namespace ZeroRise
{
    // The physical plate is the invariant; font-atlas rebuilds may never change its footprint.
    [ExecuteAlways]
    public sealed class FittedWorldSign:MonoBehaviour
    {
        public float boardWidth=1,boardHeight=.3f;
        public TextMesh lettering;
        bool pending;
        void OnEnable(){Font.textureRebuilt+=FontChanged;pending=true;}
        void OnDisable(){Font.textureRebuilt-=FontChanged;}
        void FontChanged(Font font){if(lettering&&lettering.font==font)pending=true;}
        void LateUpdate(){if(pending){pending=false;Refit();}}
        public void Refit()
        {
            if(!lettering||!lettering.font)return;
            var tr=lettering.transform;tr.localScale=Vector3.one;
            var bounds=lettering.GetComponent<MeshRenderer>().localBounds;
            if(bounds.size.x<.0001f||bounds.size.y<.0001f){pending=true;return;}
            float scale=Mathf.Min(boardWidth*.88f/bounds.size.x,boardHeight*.74f/bounds.size.y);
            tr.localScale=Vector3.one*scale;
            tr.localPosition=new Vector3(-bounds.center.x*scale,-bounds.center.y*scale,-.034f);
        }
    }
}
