using UnityEngine;

namespace Qunity
{
    public class EntityPropertyReceiver : MonoBehaviour
    {
        [HideInInspector]
        public float inverseScale = 1;

        static public bool ToVector3(string val, out Vector3 vec3)
        {
            var vals = val.Split(" ");
            if (vals.Length < 3)
            {
                vec3 = Vector3.zero;
                return false;
            }
            vec3 = new Vector3(-float.Parse(vals[1]), float.Parse(vals[2]), float.Parse(vals[0]));
            return true;
        }

        static public Vector3 ToVector3(string val)
        {
            Vector3 vec;
            ToVector3(val, out vec);
            return vec;
        }

        public virtual void OnProperty(string name, string value)
        { }
    }
}