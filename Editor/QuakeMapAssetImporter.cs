using UnityEditor.AssetImporters;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEditor;

namespace Qunity
{
    /// <summary>
    /// Import any files with the .map extension
    /// </summary>
    [ScriptedImporter(1, "map", AllowCaching = true)]
    public sealed class QuakeMapAssetImporter : ScriptedImporter
    {
        private MapData mapData;
        private MapParser mapParser;

        [Header("General")]
        public float inverseScale = 16;
        public string extractPath = "Assets/textures";

        [InspectorButton("ExtractFromWAD", ButtonWidth = 120)]
        public bool extractFromWAD;
        [InspectorButton("SetupTextures", ButtonWidth = 120)]
        public bool setupTextures;

        [Header("Entities")]
        public SolidImporter solidImporter = new SolidImporter();
        public PointImporter pointImporter = new PointImporter();
        [HideInInspector]
        public bool warnTexturesReimported;
        [HideInInspector]
        public bool warnTexturesMissing;

        [HideInInspector]
        public string mapPath;
        [HideInInspector]
        public bool importByTextureConversion = false;

        public QuakeMapAssetImporter()
        {
            mapData = new MapData();
            mapParser = new MapParser(mapData);
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            Debug.Log("import map");

            if (importByTextureConversion)
            {
                Debug.Log("got prevention flag");
                importByTextureConversion = false;
                return;
            }
            if (ctx.assetPath.Contains("autosave/"))
            {
                return;
            }
            mapPath = ctx.assetPath;
            mapParser.Load(ctx.assetPath);
            GameObject worldspawn = null;
            solidImporter.inverseScale = inverseScale;

            warnTexturesMissing = false;
            warnTexturesReimported = false;
            solidImporter.OnWarn((SolidImporter.WarnType wt, bool v) =>
            {
                switch (wt)
                {
                    case SolidImporter.WarnType.MissingTexture:
                        {
                            warnTexturesMissing = v;
                            break;
                        }
                    case SolidImporter.WarnType.ReimportTexture:
                        {
                            warnTexturesReimported = v;
                            break;
                        }
                }
            });

            solidImporter.ParseEntities(ctx, mapData, (GameObject go, int idx) =>
            {
                if (idx == 0) { worldspawn = go; }
                else { go.transform.parent = worldspawn.transform; }
            });

            pointImporter.ParseEntities(ctx, mapData, (GameObject go, int idx) =>
            {
                if (worldspawn != null)
                {
                    go.transform.position /= inverseScale;
                    go.transform.parent = worldspawn.transform;
                }
            });

            var evbus = worldspawn.AddComponent<QunityEventBus>();
            foreach (var c in worldspawn.GetComponentsInChildren<EntityEventReceiver>())
            {
                evbus.test = c.targetName;
                evbus.AddEvent(c.targetName, c.OnTrigger);
            }

            foreach (var c in worldspawn.GetComponentsInChildren<EntityEventEmitter>())
            {
                c.SetLocalEventBus(evbus);
            }

            return;
        }

        public void ExtractFromWAD()
        {
            mapParser.Load(mapPath);
            foreach (var qtex in mapData.textures)
            {
                var folder = extractPath.Trim('/').TrimEnd('\\');
                Directory.CreateDirectory(folder);
                var tex = solidImporter.FindTextureInWAD(mapData, qtex.name);
                var fullPath = folder + "/" + tex.name + ".png";
                fullPath = fullPath.Replace("*", "").Replace("+", "");
                byte[] _bytes = tex.EncodeToPNG();
                File.WriteAllBytes(fullPath, _bytes);
            }
        }
        /*
                public void SetupTextures()
                {

                    Debug.Log("set prevention flag");
                    importByTextureConversion = true;
                    mapParser.Load(mapPath);
                    foreach (var qtex in mapData.textures)
                    {
                        var tex = solidImporter.FindTexture(mapData, qtex.name);
                        bool hasTransparancy = false;
                        bool hasBright = false;
                        Texture2D emissionTex = new Texture2D(qtex.width, qtex.height, TextureFormat.RGB24, true);
                        Color[] blackPixels = Enumerable.Repeat(Color.black, qtex.width * qtex.height).ToArray();
                        emissionTex.SetPixels(blackPixels);

                        if (tex.isReadable)
                        {
                            for (int h = 0; h < tex.height; h++)
                            {
                                for (int w = 0; w < tex.width; w++)
                                {
                                    Color32 c = tex.GetPixel(w, h);
                                    if (solidImporter.palette.isTransparent(c))
                                    {
                                        hasTransparancy = true;
                                        c.a = 0;
                                        tex.SetPixel(w, h, c);
                                    }
                                    else if (c.a == 0)
                                    {
                                        hasTransparancy = true;
                                    }

                                    if (solidImporter.palette.isBrightColor(c))
                                    {
                                        emissionTex.SetPixel(w, h, c);
                                        hasBright = true;
                                    }
                                }
                            }
                            tex.Apply();
                        }
                        else
                        {
                            warnTexturesReimported = true;
                            var path = QPathTools.GetTextureAssetPath(qtex.name, solidImporter.textureFolder);
                            Debug.Log("reimporting texture: " + path + "; please reimport map");
                            TextureProcessor.ReimportTexture(path);
                        }


                        var mat = solidImporter.defaultSolid;
                        if (hasTransparancy)
                        {
                            mat = solidImporter.alphaCutout;
                        }

                        var newMat = new Material(mat);
                        newMat.SetTexture(solidImporter.BaseColorShaderParam, tex);
                        newMat.name = qtex.name;
                        if (hasBright)
                        {
                            emissionTex.name = qtex.name + "_em";
                            emissionTex.filterMode = FilterMode.Point;
                            emissionTex.Apply();

                            AssetDatabase.AddObjectToAsset(emissionTex, mapPath);
                            var tt = AssetDatabase.GetAssetPath(emissionTex);
                            Debug.Log(tt);
                            newMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.AnyEmissive;
                            newMat.SetColor("_EmissionColor", Color.white);
                            newMat.SetTexture("_EmissionMap", emissionTex);
                        }

                        if (hasBright || hasTransparancy)
                        {
                            var obj = PrefabUtility.SaveAsPrefabAsset(GameObject.CreatePrimitive(PrimitiveType.Cube), mapPath + "/cube");
                            obj.name = "test";
                            AssetDatabase.AddObjectToAsset(obj, mapPath);
                            var tt = AssetDatabase.GetAssetPath(obj);
                            Debug.Log(tt);

                            //AssetDatabase.AddObjectToAsset(newMat, mapPath);
                        }
                    }
                    //AssetDatabase.SaveAssets();
                }
                */
    }
}
