using System.Collections.Generic;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Qunity
{
    using EntityCallback = System.Action<GameObject, int>;
    public class BaseEntityImporter
    {
        protected const string PKGPATH = "Packages/com.chunkycat.qunity/";

        protected Dictionary<string, int> classCount = new Dictionary<string, int>();
        protected MapData mapData;
        protected AssetImportContext ctx;
        [HideInInspector]
        public float inverseScale = 16;

        public void ParseEntities(AssetImportContext ctx, MapData mapData, EntityCallback cb)
        {
            this.ctx = ctx;
            this.mapData = mapData;
            setup();

            for (int i = 0; i < mapData.entities.Count; i++)
            {
                var ent = mapData.entities[i];
                var classname = ent.properties["classname"];
                if (!classCount.ContainsKey(classname))
                {
                    classCount[classname] = 0;
                }
                var go = parseEnt(classname, ent, i);
                if (go != null && cb != null) { cb(go, i); }
                classCount[classname] += 1;
            }
        }

        protected virtual void setup()
        {

        }

        protected virtual GameObject parseEnt(string classname, Entity ent, int idx) { return null; }
    }
}