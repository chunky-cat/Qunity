using System;
using UnityEngine;
using UnityEditor;

namespace Qunity
{
    [Serializable]
    public class SolidEntity
    {
        public string className;
        public GameObject prefab;

        static public GameObject SetupPrefab(Entity ent, GameObject prefab)
        {
            if (prefab == null) { return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var parser = go.GetComponent<EntityPropertyReceiver>();
            var emmitter = go.GetComponent<EntityEventEmitter>();
            foreach (var prop in ent.properties)
            {
                if (prop.Key == "target" && emmitter != null) emmitter.SetTarget(prop.Value);
                if (prop.Key == "targetname")
                {
                    var receiver = go.GetComponent<EntityEventReceiver>();
                    if (receiver == null) receiver = go.AddComponent<EntityEventReceiver>();
                    receiver.targetName = prop.Value;
                }

                if (prop.Key != "classname" && parser != null) parser.OnProperty(prop.Key, prop.Value);
            }

            return go;
        }
    }
}