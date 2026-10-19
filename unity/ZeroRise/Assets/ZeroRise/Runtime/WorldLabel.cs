using UnityEngine;

namespace ZeroRise
{
    // Keep HDRP's unlit glyph material aligned with Unity's dynamic font atlas.
    // Font atlases can be rebuilt on entering Play or in the standalone player.
    [ExecuteAlways, RequireComponent(typeof(TextMesh))]
    public sealed class WorldLabel:MonoBehaviour
    {
        TextMesh label;
        void OnEnable()
        {
            label=GetComponent<TextMesh>();
            if(!label.font)return;
            label.font.RequestCharactersInTexture(label.text,label.fontSize,label.fontStyle);
            Font.textureRebuilt+=Refresh;
            Refresh(label.font);
        }
        void OnDisable(){Font.textureRebuilt-=Refresh;}
        void Refresh(Font font)
        {
            if(!label||label.font!=font)return;
            var material=GetComponent<MeshRenderer>().sharedMaterial;
            if(material)
            {
                material.SetTexture("_MainTex",font.material.mainTexture);
            }
        }
    }
}
