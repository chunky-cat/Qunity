using UnityEditor.AssetImporters;
using UnityEngine;
using System.IO;
using System;
using UnityEditor;
using UnityEngine.Events;
using UnityEditor.Events;

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

        public override void OnImportAsset(AssetImportContext ctx)
        {
            if (ctx.assetPath.Contains("autosave/"))
            {
                return;
            }

            mapData = new MapData();
            mapParser = new MapParser(mapData);
            mapPath = ctx.assetPath;
            mapParser.Load(ctx.assetPath);
            GameObject worldspawn = null;
            solidImporter.inverseScale = inverseScale;
            pointImporter.inverseScale = inverseScale;


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
            var startTime = DateTime.Now;

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
                AddEventToBus(ref evbus, c.targetName, c.OnTrigger);
            }

            foreach (var c in worldspawn.GetComponentsInChildren<EntityEventEmitter>())
            {
                c.SetLocalEventBus(evbus);
            }

            var endTime = DateTime.Now;
            Debug.LogFormat("import time: {0}", (endTime - startTime).TotalSeconds);
            return;
        }

        private void AddEventToBus(ref QunityEventBus evbus, string targetName, UnityAction cb)
        {
            var ev = evbus.FindEvent(targetName);
            if (ev == null)
            {
                var uev = new QunityEventEntry(targetName);
                evbus.eventList.Add(uev);
                UnityEventTools.AddVoidPersistentListener(uev.unityEvent, cb);
                return;
            }
            UnityEventTools.AddVoidPersistentListener(ev, cb);
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
