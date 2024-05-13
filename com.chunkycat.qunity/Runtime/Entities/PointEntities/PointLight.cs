using UnityEngine;
using Unity;

namespace Qunity
{
    public class PointLightEntity : EntityPropertyReceiver
    {
        private float defaultLight = 300;
        private float defaultAttenuation = 0;
        private float defaultRange = 16;

        public override void OnProperty(string name, string value)
        {
            var invScale = inverseScale == 0 ? 1 : inverseScale / 16;
            var light = GetComponent<Light>();
            light.type = LightType.Point;
            // TODO: does not build somehow
            //light.lightmapBakeType = LightmapBakeType.Baked;
            switch (name)
            {
                case "light":
                    {
                        light.intensity = float.Parse(value) / invScale;
                        break;
                    }
                case "wait":
                    {
                        var wait = float.Parse(value);
                        wait = wait == 0 ? 1 : wait;
                        light.range = defaultRange / (wait * 0.9f) / invScale;
                        break;
                    }
                case "_color":
                    {
                        var c = ToVector3(value);
                        light.color = new Color32((byte)c.x, (byte)c.y, (byte)c.z, 1);
                        break;
                    }
            }
        }
    }
}

