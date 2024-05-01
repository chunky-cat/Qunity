using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Qunity
{

    [System.Serializable]
    public class PointImporter : BaseEntityImporter
    {
        public List<PointEntity> pointEntities = new List<PointEntity>();
        public bool importLights = false;
        protected override GameObject parseEnt(string classname, Entity ent, int idx)
        {
            // skip if is solid entity
            if (ent.brushes.Count > 0) { return null; }
            var entName = string.Format("{0}_{1}", classname, classCount[classname]);
            var pe = getPeForClass(classname);
            if (pe != null)
            {
                var go = pe.SetupPrefab(ent, inverseScale);
                go.name = entName;
                ctx.AddObjectToAsset(go.name, go);
                return go;
            }
            return null;
        }

        private PointEntity getPeForClass(string classname)
        {
            PointEntity foundPe = null;
            foreach (PointEntity se in pointEntities)
            {
                if (se.className == classname)
                {
                    foundPe = se;
                    break;
                }
            }

            if (foundPe == null)
            {
                pointEntities.Add(new PointEntity { className = classname, prefab = searchStandardPrefabs(classname) });
                return null;
            }

            if (foundPe.prefab != null)
            {
                return foundPe;
            }

            foundPe.prefab = searchStandardPrefabs(classname);
            return foundPe.prefab != null ? foundPe : null;
        }

        private GameObject searchStandardPrefabs(string classname)
        {
            switch (classname)
            {
                case "light":
                    {
                        if (importLights)
                        {
                            var l = (GameObject)AssetDatabase.LoadAssetAtPath(PKGPATH + "Assets/Prefabs/PointLight.prefab", typeof(GameObject));
                            return l;
                        }
                        break;
                    }
            }
            return null;
        }
    }
}
