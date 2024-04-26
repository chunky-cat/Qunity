using UnityEditor.AssetImporters;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Rendering;
using System.Linq;
using Quinity;

namespace Qunity
{
    /// <summary>
    /// Import any files with the .map extension
    /// </summary>
    [ScriptedImporter(1, "map", AllowCaching = true)]
    public sealed class QuakeMapAssetImporter : ScriptedImporter
    {
        const string internalMatFolder = "Packages/com.chunkycat.qunity/Assets/Materials/";
        private MapData mapData;
        private MapParser mapParser;
        private Dictionary<string, Material> m_materialDict = new Dictionary<string, Material>();

        [Header("General")]
        public float inverseScale = 16;

        [Header("Entities")]
        public SolidImporter solidImporter = new SolidImporter();
        public PointImporter pointImporter = new PointImporter();
        [HideInInspector]
        public bool warnTexturesReimported;
        [HideInInspector]
        public bool warnTexturesMissing;



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
            return;
        }
    }
}
