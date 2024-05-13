using System;
using UnityEngine;

namespace Qunity
{
    [Serializable]
    public class SolidEntity
    {
        public string className;
        public GameObject prefab;

        static public void SetupPrefab(GameObject go, Entity ent)
        {
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

            return;
        }
    }
}
