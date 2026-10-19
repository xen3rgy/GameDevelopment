using UnityEditor;
using UnityEngine;

namespace ZeroRise.Editor
{
    public sealed class WestbruchImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/ZeroRise/Art/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.anisoLevel = 8;
            importer.maxTextureSize = 2048;
            importer.mipmapEnabled = true;
            if (assetPath.EndsWith("-height.jpg")||assetPath.EndsWith("-height.png"))
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.convertToNormalmap = true;
                importer.heightmapScale = .035f;
                importer.sRGBTexture = false;
            }
            if(assetPath.EndsWith("-mask.png"))importer.sRGBTexture=false;
            if(assetPath.EndsWith("urban-foliage.png"))importer.alphaIsTransparency=true;
        }
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/ZeroRise/Art/Trees/")) return;
            var importer = (ModelImporter)assetImporter;
            importer.importNormals = ModelImporterNormals.Calculate;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.generateSecondaryUV = false; // Real-time HDRP scene: no costly tree lightmap unwrap.
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.isReadable = true; // Build distinct cached tree crown variants in the editor.
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
        }
    }
}
