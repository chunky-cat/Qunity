using System;
using UnityEngine;

namespace Qunity
{
    [Serializable]
    public class PointEntity
    {
        public string className;
        public GameObject prefab;

        public void SetupPrefab(GameObject go, Entity ent, float inverseScale)
        {
            var parser = go.GetComponent<EntityPropertyReceiver>();
            if (parser != null) parser.inverseScale = inverseScale;

            foreach (var prop in ent.properties)
            {
                switch (prop.Key)
                {
                    case "origin": parseOrigin(go, prop.Value); break;
                }
                if (prop.Key != "classname" && parser != null) parser.OnProperty(prop.Key, prop.Value);
            }

            return;
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
