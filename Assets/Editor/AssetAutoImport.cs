using UnityEditor;
using UnityEngine;

public class AssetAutoImport : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        var importer = (TextureImporter)assetImporter;
        importer.mipmapEnabled = false;
        // if (assetPath.StartsWith("Assets/Asset/bg/") || assetPath.StartsWith("Assets/Asset/fashion/"))
        // {
        //     importer.textureType = TextureImporterType.Sprite;
        // }
        if (assetPath.StartsWith("Assets/Asset/fgui/"))
        {
            importer.mipmapEnabled = false;
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Bilinear;
        }
    }
}