using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Qunity
{
    public class QPathTools
    {
        public static string GetTextureAssetPath(string texName, string folder)
        {
            var fullPath = folder;
            var segments = texName.Split("/");
            texName = segments[segments.Length - 1];
            for (int i = 0; i < segments.Length - 1; i++)
            {
                fullPath += "/" + segments[i];
            }

            string[] guids = AssetDatabase.FindAssets(string.Format("\"{0}\" t:texture2D", texName), new[] { fullPath });
            if (guids.Length > 0)
            {
                if (guids.Length > 1)
                {
                    foreach (var guid in guids)
                    {
                        var p = AssetDatabase.GUIDToAssetPath(guid).Split("/");
                        var cmpName = p[p.Length - 1];
                        cmpName = cmpName.Substring(0, cmpName.LastIndexOf('.'));
                        if (cmpName == texName)
                        {
                            return AssetDatabase.GUIDToAssetPath(guids[0]);
                        }
                    }
                    return "";
                }
                return AssetDatabase.GUIDToAssetPath(guids[0]);
            }
            return "";
        }

        static public string GetPipelineFolder(out string BaseColorShaderParam)
        {
            var pipeline = "Base";
            BaseColorShaderParam = "_MainTex";
            if (GraphicsSettings.renderPipelineAsset != null)
            {

                switch (GraphicsSettings.renderPipelineAsset.GetType().Name)
                {
                    case "UniversalRenderPipelineAsset":
                        {
                            pipeline = "URP";
                            BaseColorShaderParam = "_BaseMap";
                            break;
                        }
                    case "HDRenderPipelineAsset":
                        {
                            pipeline = "HDRP";
                            BaseColorShaderParam = "_BaseColorMap";
                            break;
                        }
                }
            }
            return pipeline;
        }
    }
}
