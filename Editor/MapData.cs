using System;
using System.Collections.Generic;
using UnityEngine;

namespace Qunity
{
    public class MapData
    {
        public List<Entity> entities = new List<Entity>();
        public List<EntityGeometry> entityGeo = new List<EntityGeometry>();
        public List<TextureData> textures = new List<TextureData>();
        public List<WorldspawnLayer> worldspawnLayers = new List<WorldspawnLayer>();

        public int FindWorldspawnLayer(int textureIdx)
        {
            for (int i = 0; i < worldspawnLayers.Count; i++)
            {
                if (worldspawnLayers[i].textureIdx == textureIdx) return i;
            }

            return -1;
        }

        public int FindTexture(string name)
        {
            for (int i = 0; i < textures.Count; i++)
            {
                if (textures[i].name == name) return i;
            }

            return -1;
        }

        public int RegisterTexture(string name)
        {
            int foundIdx = FindTexture(name);
            if (foundIdx != -1) return foundIdx;

            textures.Add(new TextureData(name));
            return textures.Count - 1;
        }

        public void SetTextureSize(string name, int width, int height)
        {
            int idx = FindTexture(name);
            if (idx == -1) return;

            textures[idx] = new TextureData(name, width, height);
        }

        public void SetSpawnTypeByClassname(string key, EntitySpawnType spawnType)
        {
            Span<Entity> entitySpan = GetEntitiesSpan();
            for (int i = 0; i < entitySpan.Length; i++)
            {
                if (entitySpan[i].properties.ContainsKey("classname") && entitySpan[i].properties["classname"] == key)
                {
                    entitySpan[i].spawnType = spawnType;
                }
            }
        }

        public Span<Entity> GetEntitiesSpan()
        {
            var r = new Span<Entity>(entities.ToArray());
            return r;
        }

        public Span<Brush> GetBrushesSpan(int entityIdx)
        {
            return new Span<Brush>(GetEntitiesSpan()[entityIdx].brushes.ToArray());
        }

        public Span<Face> GetFacesSpan(int entityIdx, int brushIdx)
        {

            return new Span<Face>(GetBrushesSpan(entityIdx)[brushIdx].faces.ToArray());
        }

        public Span<EntityGeometry> GetEntityGeoSpan()
        {
            return new Span<EntityGeometry>(entityGeo.ToArray());
        }

        public Span<BrushGeometry> GetBrushGeoSpan(int entityIdx)
        {
            return new Span<BrushGeometry>(GetEntityGeoSpan()[entityIdx].brushes.ToArray());
        }

        public Span<FaceGeometry> GetFaceGeoSpan(int entityIdx, int brushIdx)
        {
            return new Span<FaceGeometry>(GetBrushGeoSpan(entityIdx)[brushIdx].faces.ToArray());
        }

        public Span<TextureData> GetTexturesSpan()
        {
            return new Span<TextureData>(textures.ToArray());
        }

        public void Reset()
        {
            entities.Clear();
            entityGeo.Clear();
            textures.Clear();
            worldspawnLayers.Clear();
        }
    }
}
