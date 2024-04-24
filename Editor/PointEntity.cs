using System;
using UnityEngine;
using UnityEditor;

namespace Qunity
{
    [Serializable]
    public class PointEntity
    {
        public string className;
        public GameObject prefab;

        public GameObject SetupPrefab(Entity ent)
        {
            if (prefab == null) { return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var parser = go.GetComponent<EntityPropertyReceiver>();
            foreach (var prop in ent.properties)
            {
                switch (prop.Key)
                {
                    case "origin": parseOrigin(go, prop.Value); break;
                }
                if (prop.Key != "classname") parser.OnProperty(prop.Key, prop.Value);
            }

            return go;
        }

        private void parseOrigin(GameObject go, string value)
        {
            Vector3 res = Vector3.zero;
            if (!EntityPropertyReceiver.ToVector3(value, out res))
            {
                Debug.LogError(string.Format("origin for {0} has wrong number of components", className));
            }
            go.transform.position = res;
        }
    }
}