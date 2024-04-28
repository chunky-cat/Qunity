using UnityEditor.AssetImporters;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

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

        [InspectorButton("ExtractFromWAD")]
        public bool extractFromWAD;

        [Header("Entities")]
        public SolidImporter solidImporter = new SolidImporter();
        public PointImporter pointImporter = new PointImporter();
        [HideInInspector]
        public bool warnTexturesReimported;
        [HideInInspector]
        public bool warnTexturesMissing;

        [HideInInspector]
        public string mapPath;

        public QuakeMapAssetImporter()
        {
            mapData = new MapData();
            mapParser = new MapParser(mapData);
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
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
    }
}
