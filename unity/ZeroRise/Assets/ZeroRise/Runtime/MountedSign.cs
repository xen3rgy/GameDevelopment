using UnityEngine;
namespace ZeroRise
{
    // Baked physical signage metadata, used by placement/render validation.
    // Lettering is printed into the face texture, never camera-facing text.
    public sealed class MountedSign:MonoBehaviour
    {
        public string faceKey,readableText,mount;
        public Vector2 size;
        public Transform support;
    }
}
