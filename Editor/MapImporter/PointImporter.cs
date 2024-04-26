using System.Collections.Generic;
using Quinity;
using UnityEditor;
using UnityEngine;

namespace Qunity
{

    [System.Serializable]
    public class PointImporter : BaseEntityImporter
    {
        public List<PointEntity> pointEntities = new List<PointEntity>();

        protected override GameObject parseEnt(string classname, Entity ent, int idx)
        {
            // skip if is solid entity
            if (ent.brushes.Count > 0) { return null; }
            var entName = string.Format("{0}_{1}", classname, classCount[classname]);
            var pe = getPeForClass(classname);
            if (pe != null)
            {

                var go = pe.SetupPrefab(ent);
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
                pointEntities.Add(new PointEntity { className = classname });
                return null;
            }
            if (foundPe.prefab != null)
            {
                return foundPe;
            }
            return null;
        }
    }
}
