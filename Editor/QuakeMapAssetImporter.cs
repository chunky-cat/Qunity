using UnityEditor.AssetImporters;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine.Rendering;
using System.Collections;

namespace Qunity
{
    /// <summary>
    /// Import any files with the .map extension
    /// </summary>
    [ScriptedImporter(1, "map", AllowCaching = true)]
    public sealed class QuakeMapAssetImporter : ScriptedImporter
    {
        const string internalMatFolder = "Packages/com.chunkycat.qunity/Assets/Materials/";
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

        [Header("Textures")]
        public string clipTextureName = "_clip";
        public string skipTextureName = "_skip";

        [Space(5)]
        public string wadFolder = "Assets/wads";
        public string textureFolder = "Assets/textures";

        [Space(5)]
        [InspectorButton("ReimporTextures")]
        public bool setupTextures;

        [Header("Materials")]
        public Material baseMaterialOverride;
        public Material alphaCutoutMaterialOverride;
        private Material baseMaterial;
        private Material alphaCutoutMaterial;
        public string BaseColorShaderParam = "_MainTex";

        [HideInInspector]
        public bool warnTexturesReimported;
        [HideInInspector]
        public bool warnTexturesMissing;

        [Header("Entities")]
        public List<PointEntity> pointEntities = new List<PointEntity>();

        public QuakeMapAssetImporter()
        {
            mapData = new MapData();
            mapParser = new MapParser(mapData);
            geoGenerator = new GeoGenerator(mapData);
            surfaceGatherer = new SurfaceGatherer(mapData);
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            wadFolder = wadFolder.TrimEnd('/');
            textureFolder = textureFolder.TrimEnd('/');

            if (baseMaterial == null)
            {
                getBaseMaterial();
            }

            if (ctx.assetPath.Contains("autosave/"))
            {
                return;
            }

            mapParser.Load(ctx.assetPath);

            generateTextureMaterials(ctx);
            var levelObj = loadLevelGeometry(ctx);


            var tmpClasses = new Dictionary<string, PointEntity>();
            foreach (var ent in mapData.entities)
            {
                var className = ent.properties["classname"];
                if (className == "worldspawn") { continue; }
                PointEntity pe = null;
                if (!tmpClasses.ContainsKey(className))
                {
                    pe = findPointEntity(className);
                    if (pe == null)
                    {
                        pe = new PointEntity { className = className };
                        pointEntities.Add(pe);
                    }
                    tmpClasses[className] = pe;
                }
                else
                {
                    pe = tmpClasses[className];
                }
                if (tmpClasses[className] != null)
                {
                    var instance = pe.SetupPrefab(ent);
                    if (instance != null)
                    {
                        ctx.AddObjectToAsset(className, instance);
                        instance.transform.position /= inverseScale;
                        instance.transform.parent = levelObj.transform;
                    }
                }
            }
        }

        private PointEntity findPointEntity(string name)
        {
            foreach (var pe in pointEntities)
            {
                if (pe.className == name)
                {
                    return pe;
                }
            }
            return null;
        }

        private void OnGUI()

        {
            EditorGUILayout.HelpBox("Some warning text", MessageType.Warning);
        }

        private GameObject loadLevelGeometry(AssetImportContext ctx)
        {
            var meshes = generateMeshes();
            var level = new GameObject("Level");

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
            uParam.packMargin = lightmapUVPackMargin;
            Unwrapping.GenerateSecondaryUVSet(mesh, uParam);

            ctx.AddObjectToAsset("mesh", mesh);
            ctx.AddObjectToAsset("Level", level);
            ctx.SetMainObject(level);
            level.isStatic = true;
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

        private void generateTextureMaterials(AssetImportContext ctx)
        {
            warnTexturesReimported = false;
            for (int i = 0; i < mapData.textures.Count; i++)
            {
                var qtex = mapData.textures[i];
                if (m_materialDict.ContainsKey(qtex.name))
                {
                    continue;
                }

                var tex = findTexture(qtex.name);
                if (tex != null)
                {
                    qtex.width = tex.width;
                    qtex.height = tex.height;
                    tex.filterMode = FilterMode.Point;
                    bool hasTransparancy = false;
                    Color chroma = new Color32(0x9f, 0x5b, 0x53, 0xFF);

                    if (tex.isReadable)
                    {
                        for (int h = 0; h < tex.height; h++)
                        {
                            for (int w = 0; w < tex.width; w++)
                            {
                                var c = tex.GetPixel(w, h);
                                if (c == chroma)
                                {
                                    hasTransparancy = true;
                                    c.a = 0;
                                    tex.SetPixel(w, h, c);
                                }
                                if (c.a == 0)
                                {
                                    hasTransparancy = true;
                                }
                            }
                        }
                        tex.Apply();
                    }
                    else
                    {
                        warnTexturesReimported = true;
                        var path = getTextureAssetPath(qtex.name);
                        Debug.Log("reimporting texture: " + path + "; please reimport map");
                        TextureProcessor.ReimportTexture(path);
                    }


                    var mat = baseMaterial;
                    if (hasTransparancy)
                    {
                        mat = alphaCutoutMaterial;
                    }
                    var newMat = new Material(mat);
                    m_materialDict[qtex.name] = newMat;
                    newMat.SetTexture(BaseColorShaderParam, tex);
                    newMat.name = qtex.name;
                }
                else
                {
                    var newMat = new Material(baseMaterial);
                    m_materialDict[qtex.name] = newMat;
                    warnTexturesMissing = true;
                    Debug.LogError("cannot find texture: " + qtex.name);
                    return;
                }

                mapData.textures[i] = qtex;
            }
        }

        private void ReimporTextures()
        {
            string[] guids = AssetDatabase.FindAssets(name + " t:texture2D", new[] { textureFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                TextureProcessor.ReimportTexture(path);
            }
        }

        private void getBaseMaterial()
        {
            var baseMat = baseMaterialOverride;
            var alphaMat = alphaCutoutMaterialOverride;

            var pipeline = "Base";
            if (GraphicsSettings.renderPipelineAsset != null)
            {
                switch (GraphicsSettings.renderPipelineAsset.GetType().Name)
                {
                    case "UniversalRenderPipelineAsset":
                        {
                            pipeline = "URP";
                            BaseColorShaderParam = "_BaseMap";
                            break;
                        }
                    case "HDRenderPipelineAsset":
                        {
                            pipeline = "HDRP";
                            BaseColorShaderParam = "_BaseColorMap";
                            break;
                        }
                }
            }

            if (baseMat == null)
            {
                baseMaterial = (Material)AssetDatabase.LoadAssetAtPath(internalMatFolder + pipeline + "/Base.mat", typeof(Material));
            }
            if (alphaMat == null)
            {
                alphaCutoutMaterial = (Material)AssetDatabase.LoadAssetAtPath(internalMatFolder + pipeline + "/Alpha.mat", typeof(Material));
                if (alphaCutoutMaterial == null)
                {
                    alphaCutoutMaterial = baseMaterial;
                }
            }

        }

        private Texture2D findTexture(string texName)
        {
            var baseDir = "Assets";
            if (mapData.entities[0].properties.ContainsKey("wad"))
            {
                var wads = mapData.entities[0].properties["wad"].Split(';');
                foreach (var wpath in wads)
                {
                    var currentBasedir = wadFolder;
                    if (wpath.Contains("/"))
                    {
                        currentBasedir = baseDir;
                    }


                    WadTexture2DCollection wad = (WadTexture2DCollection)AssetDatabase.LoadAssetAtPath(currentBasedir + "/" + wpath, typeof(WadTexture2DCollection));
                    if (wad == null)
                    {
                        Debug.LogError("could not open wad: " + currentBasedir + "/" + wpath);
                        continue;
                    }
                    var tex = wad.FindTexture(texName);
                    if (tex != null)
                    {
                        return tex;
                    }
                }
                return null;
            }
            var path = getTextureAssetPath(texName);
            return (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
        }

        private string getTextureAssetPath(string name)
        {
            string[] guids = AssetDatabase.FindAssets(name + " t:texture2D", new[] { textureFolder });
            if (guids.Length > 0)
            {
                return AssetDatabase.GUIDToAssetPath(guids[0]);
            }
            return "";
        }
    }
}
