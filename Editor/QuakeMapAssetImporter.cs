using UnityEditor.AssetImporters;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace Qunity
{

    /// <summary>
    /// Import any files with the .map extension
    /// </summary>
    [ScriptedImporter(1, "map", AllowCaching = true)]
    public sealed class QuakeMapAssetImporter : ScriptedImporter
    {
        private MapData mapData;
        private MapParser mapParser;
        private GeoGenerator geoGenerator;
        private SurfaceGatherer surfaceGatherer;
        private Dictionary<string, Material> m_materialDict = new Dictionary<string, Material>();
        private List<Material> m_materialList = new List<Material>();

        [Header("General")]
        public float inverseScale = 16;
        [Header("Lightmap Settings")]
        public float lightmapTexelSize = 1;
        public float lightmapUVPackMargin = 1.5f;

        [Header("Special Texutre Names")]
        public string clipTextureName = "_clip";
        public string skipTextureName = "_skip";

        [Header("Folders")]

        public string textureFolder = "Assets/textures";

        public QuakeMapAssetImporter()
        {
            mapData = new MapData();
            mapParser = new MapParser(mapData);
            geoGenerator = new GeoGenerator(mapData);
            surfaceGatherer = new SurfaceGatherer(mapData);
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            if (ctx.assetPath.Contains("autosave/"))
            {
                return;
            }

            mapParser.Load(ctx.assetPath);
            generateTextureMaterials();

            var levelObj = loadLevelGeometry(ctx);
            /*
                        foreach (var ent in mapData.entities)
                        {
                            Debug.Log(ent.properties["classname"]);
                            Debug.Log(ent.brushes.Count);
                        }
            */
        }

        private GameObject loadLevelGeometry(AssetImportContext ctx)
        {
            var meshes = generateMeshes();
            var level = new GameObject("Level");
            level.isStatic = true;

            var mr = level.AddComponent<MeshRenderer>();
            var mf = level.AddComponent<MeshFilter>();
            var mc = level.AddComponent<MeshCollider>();

            var mesh = new Mesh();
            var combineFilters = new CombineInstance[meshes.Count];
            mr.SetSharedMaterials(m_materialList);

            foreach (var matKey in m_materialDict.Keys)
            {
                ctx.AddObjectToAsset(matKey, m_materialDict[matKey]);
            }

            for (int i = 0; i < meshes.Count; i++)
            {
                combineFilters[i].mesh = meshes[i];
                combineFilters[i].transform = mf.transform.localToWorldMatrix;
            }

            mesh.CombineMeshes(combineFilters, false);
            mesh.name = "WorldSpawn";
            mf.sharedMesh = mesh;
            mc.sharedMesh = mesh;
            mr.scaleInLightmap = lightmapTexelSize / inverseScale;

            var uParam = new UnwrapParam();
            UnwrapParam.SetDefaults(out uParam);
            Debug.Log(string.Format("uparam.packMargin: {0}", uParam.packMargin));
            uParam.packMargin = lightmapUVPackMargin;
            Unwrapping.GenerateSecondaryUVSet(mesh, uParam);

            ctx.AddObjectToAsset("WorldSpawn", mesh);
            ctx.AddObjectToAsset("Level", level);
            ctx.SetMainObject(level);
            return level;
        }

        private List<Mesh> generateMeshes()
        {
            List<Mesh> surfsArray = new List<Mesh>();
            geoGenerator.Run();
            foreach (string textureKey in m_materialDict.Keys)
            {
                surfaceGatherer.ResetParams();
                surfaceGatherer.splitType = SurfaceSplitType.ENTITY;
                surfaceGatherer.SetTextureFilter(textureKey);
                surfaceGatherer.SetBrushFilterTexture(clipTextureName);
                surfaceGatherer.SetFaceFilterTexture(skipTextureName);
                surfaceGatherer.Run();

                Span<FaceGeometry> surfsSpan = surfaceGatherer.outSurfaces.ToArray().AsSpan();

                for (int s = 0; s < surfsSpan.Length; s++)
                {
                    if (surfsSpan[s] == null || surfsSpan[s].vertices.Count == 0)
                    {
                        continue;
                    }

                    Span<FaceVertex> vertexSpan = surfsSpan[s].vertices.ToArray().AsSpan();
                    m_materialList.Add(m_materialDict[textureKey]);
                    Vector3[] vertices = new Vector3[vertexSpan.Length];
                    Vector3[] normals = new Vector3[vertexSpan.Length];
                    Vector4[] tangents = new Vector4[vertexSpan.Length];
                    Vector2[] uvs = new Vector2[vertexSpan.Length];

                    for (int i = 0; i < vertexSpan.Length; i++)
                    {
                        ref FaceVertex v = ref vertexSpan[i];
                        vertices[i] = new Vector3(-v.vertex.y, v.vertex.z, v.vertex.x) / inverseScale;
                        normals[i] = new Vector3(-v.normal.y, v.normal.z, v.normal.x);
                        tangents[i] = v.tangent;
                        uvs[i] = new Vector2(v.uv.x, v.uv.y);
                    }

                    int[] indices = new int[surfsSpan[s].indices.Count];
                    for (int i = 0; i < surfsSpan[s].indices.Count; i++)
                    {
                        indices[i] = surfsSpan[s].indices[i];
                    }

                    var m = new Mesh();
                    m.vertices = vertices;
                    m.uv = uvs;
                    m.normals = normals;
                    m.tangents = tangents;
                    m.SetTriangles(indices, 0);
                    surfsArray.Add(m);
                }
            }
            return surfsArray;
        }

        private void generateTextureMaterials()
        {
            var mat = (Material)AssetDatabase.LoadAssetAtPath("Packages/com.chunkycat.qunity/Assets/Materials/Default.mat", typeof(Material));
            for (int i = 0; i < mapData.textures.Count; i++)
            {
                var qtex = mapData.textures[i];
                if (m_materialDict.ContainsKey(qtex.name))
                {
                    continue;
                }

                var path = string.Format("{0}/{1}.png", textureFolder, qtex.name);
                var tex = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
                tex.filterMode = FilterMode.Point;
                qtex.width = tex.width;
                qtex.height = tex.height;

                var newMat = new Material(mat);
                newMat.SetTexture("_BaseMap", tex);
                newMat.name = qtex.name;
                m_materialDict[qtex.name] = newMat;
                mapData.textures[i] = qtex;
            }

        }
    }
}
