using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.ComponentModel;

namespace Qunity
{
    class TextureProcessor : AssetPostprocessor
    {
        static BackgroundWorker worker = new BackgroundWorker();
        const int DEFAULT_TIMEOUT_SECS = 10 * 1000;
        static Dictionary<string, bool> toReimport = new Dictionary<string, bool>();

        public static void ReimportTexture(string path)
        {
            if (toReimport.ContainsKey(path)) { return; }
            var tex = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
            if (tex.isReadable && tex.format == TextureFormat.RGBA32) { return; }

            toReimport[path] = true;
            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(path);
            var defaultPlatform = importer.GetDefaultPlatformTextureSettings();
            defaultPlatform.format = TextureImporterFormat.RGBA32;
            importer.SetPlatformTextureSettings(defaultPlatform);

            importer.wrapMode = TextureWrapMode.Repeat;
            importer.alphaIsTransparency = true;
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.filterMode = FilterMode.Point;
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            foreach (string str in importedAssets)
            {
                if (toReimport.ContainsKey(str))
                {
                    toReimport.Remove(str);
                    Debug.Log(string.Format("finished reimport: {0}", str));
                }
            }
        }
    }
}