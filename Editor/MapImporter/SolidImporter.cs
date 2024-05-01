using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Qunity
{
    [Serializable]
    public class WadEntry
    {
        [ReadOnly] public string name;
        public WadTexture2DCollection wad;
    }

    [System.Serializable]
    public class SolidImporter : BaseEntityImporter
    {
        public enum WarnType { MissingTexture, ReimportTexture };

        const string WORLDSPAWNCLASS = "worldspawn";

        [Header("Lightmap Settings")]
        public float lightmapTexelSize = 1;
        public float lightmapUVPackMargin = 1.5f;

        [Space(5)]
        [Header("Textures")]
        public string clipTextureName = "clip";
        public string skipTextureName = "skip";

        [Space(5)]
        public string textureFolder = "Assets/textures";
        public LmpPalette palette;
        public List<WadEntry> wadFiles = new List<WadEntry>();

        [Space(5)]
        [Header("Materials Overrides")]
        public Material defaultSolid;
        public Material alphaCutout;
        public string BaseColorShaderParam = "_MainTex";

        [Space(5)]
        [Header("Prefabs / Mappings")]
        public GameObject defaultSolidPrefab;
        public GameObject defaultTriggerPrefab;
        public List<SolidEntity> solidEntities = new List<SolidEntity>();
        private Action<WarnType, bool> onWarnCallback;

        private Dictionary<string, Material> m_materialDict = new Dictionary<string, Material>();
        private GeoGenerator geoGenerator;
        private SurfaceGatherer surfaceGatherer;

        protected override void setup()
        {
            if (palette == null)
            {
                palette = (LmpPalette)AssetDatabase.LoadAssetAtPath(PKGPATH + "Assets/palette.lmp", typeof(LmpPalette));
            }
            if (defaultSolidPrefab == null)
            {
                defaultSolidPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(PKGPATH + "Assets/Prefabs/SolidDefault.prefab", typeof(GameObject));
            }
            if (defaultTriggerPrefab == null)
            {
                defaultTriggerPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(PKGPATH + "Assets/Prefabs/SolidTrigger.prefab", typeof(GameObject));
            }
            textureFolder = textureFolder.TrimEnd('/');

            if (mapData.entities[0].properties.ContainsKey("wad"))
            {
                var wads = mapData.entities[0].properties["wad"].Split(';');
                foreach (var wpath in wads)
                {
                    WadTexture2DCollection wad = (WadTexture2DCollection)AssetDatabase.LoadAssetAtPath("Assets/" + wpath, typeof(WadTexture2DCollection));

                    wadFiles.Add(new WadEntry { name = wpath, wad = wad });
                }
            }


            geoGenerator = new GeoGenerator(mapData);
            surfaceGatherer = new SurfaceGatherer(mapData);
            setupBaseMaterials();
            generateTextureMaterials();

            geoGenerator.Run();
            foreach (var matKey in m_materialDict.Keys)
            {
                ctx.AddObjectToAsset(matKey, m_materialDict[matKey]);
            }

        }

        public void OnWarn(Action<WarnType, bool> cb)
        {
            onWarnCallback = cb;
        }

        private void warn(WarnType wt, bool v)
        {
            if (onWarnCallback != null) onWarnCallback(wt, v);
        }

        protected override GameObject parseEnt(string classname, Entity ent, int idx)
        {
            // skip if is point entity
            if (ent.brushes.Count == 0) { return null; }

            string entName = classname == WORLDSPAWNCLASS ? classname : string.Format("{0}_{1}", classname, classCount[classname]);
            var prefab = getPrefabForClass(classname);

            var go = SolidEntity.SetupPrefab(ent, prefab);
            go.name = entName;
            loadMeshObject(go, idx);
            ctx.AddObjectToAsset(go.name, go);
            if (classname == WORLDSPAWNCLASS)
            {
                ctx.SetMainObject(go);
                go.isStatic = true;
            }
            else
            {
                go.transform.position /= inverseScale;
            }
            return go;
        }

        private GameObject getPrefabForClass(string classname)
        {
            SolidEntity foundSe = null;
            foreach (SolidEntity se in solidEntities)
            {
                if (se.className == classname)
                {
                    foundSe = se;
                    break;
                }
            }
            if (foundSe == null)
            {
                solidEntities.Add(new SolidEntity { className = classname });
            }

            return findCorrectPrefab(classname, foundSe);
        }

        private GameObject findCorrectPrefab(string classname, SolidEntity se)
        {
            if (se != null && se.prefab != null) return se.prefab;
            if (classname.ToLower().Contains("trigger"))
            {
                return defaultTriggerPrefab;
            }
            return defaultSolidPrefab;
        }

        #region "Mesh Stuff"
        void loadMeshObject(GameObject obj, int entIdx = 0)
        {
            List<Material> materialList;
            var meshes = generateMeshes(out materialList, entIdx);
            var mr = obj.GetComponent<MeshRenderer>();

            if (mr != null)
            {
                mr.SetSharedMaterials(materialList);
                mr.scaleInLightmap = lightmapTexelSize / inverseScale;
            }

            var mf = obj.GetComponent<MeshFilter>();
            if (mf == null)
            {
                mf = obj.AddComponent<MeshFilter>();
            }
            var mc = obj.GetComponent<MeshCollider>();

            var mesh = new Mesh();
            var combineFilters = new CombineInstance[meshes.Count];

            for (int i = 0; i < meshes.Count; i++)
            {
                combineFilters[i].mesh = meshes[i];
                combineFilters[i].transform = mf.transform.localToWorldMatrix;
            }

            mesh.CombineMeshes(combineFilters, false);
            mesh.name = obj.name + "_mesh";
            mf.sharedMesh = mesh;
            mc.sharedMesh = mesh;

            var uParam = new UnwrapParam();
            UnwrapParam.SetDefaults(out uParam);
            uParam.packMargin = lightmapUVPackMargin;
            try
            {
                Unwrapping.GenerateSecondaryUVSet(mesh, uParam);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
            ctx.AddObjectToAsset(mesh.name, mesh);
        }

        private List<Mesh> generateMeshes(out List<Material> materials, int entityFilter = 0)
        {
            var materialList = new List<Material>();
            materials = materialList;
            List<Mesh> surfsArray = new List<Mesh>();
            foreach (string textureKey in m_materialDict.Keys)
            {
                surfaceGatherer.ResetParams();
                surfaceGatherer.splitType = SurfaceSplitType.ENTITY;
                surfaceGatherer.SetTextureFilter(textureKey);
                surfaceGatherer.SetBrushFilterTexture(clipTextureName);
                surfaceGatherer.SetFaceFilterTexture(skipTextureName);
                surfaceGatherer.entityFilterIdx = entityFilter;
                surfaceGatherer.Run();

                Span<FaceGeometry> surfsSpan = surfaceGatherer.outSurfaces.ToArray().AsSpan();

                for (int s = 0; s < surfsSpan.Length; s++)
                {
                    if (surfsSpan[s] == null || surfsSpan[s].vertices.Count == 0)
                    {
                        continue;
                    }

                    Span<FaceVertex> vertexSpan = surfsSpan[s].vertices.ToArray().AsSpan();
                    materialList.Add(m_materialDict[textureKey]);
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
                        uvs[i] = new Vector2(v.uv.x, -v.uv.y);
                    }

                    int[] indices = new int[surfsSpan[s].indices.Count];
                    for (int i = 0; i < surfsSpan[s].indices.Count; i++)
                    {
                        indices[i] = surfsSpan[s].indices[i];
                    }

                    var m = new Mesh();
                    m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
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
            for (int i = 0; i < mapData.textures.Count; i++)
            {
                var qtex = mapData.textures[i];
                var qtexname = qtex.name;
                if (m_materialDict.ContainsKey(qtexname))
                {
                    continue;
                }

                var tex = FindTexture(mapData, qtexname);
                if (tex != null)
                {
                    qtex.width = tex.width;
                    qtex.height = tex.height;

                    tex.filterMode = FilterMode.Point;

                    bool hasTransparancy = false;
                    bool hasBright = false;
                    Texture2D emissionTex = new Texture2D(qtex.width, qtex.height, TextureFormat.RGB24, true);
                    Color[] blackPixels = Enumerable.Repeat(Color.black, qtex.width * qtex.height).ToArray();
                    emissionTex.SetPixels(blackPixels);

                    if (tex.isReadable)
                    {
                        for (int h = 0; h < tex.height; h++)
                        {
                            for (int w = 0; w < tex.width; w++)
                            {
                                Color32 c = tex.GetPixel(w, h);
                                if (palette.isTransparent(c))
                                {
                                    hasTransparancy = true;
                                    c.a = 0;
                                    tex.SetPixel(w, h, c);
                                }
                                else if (c.a == 0)
                                {
                                    hasTransparancy = true;
                                }

                                if (palette.isBrightColor(c))
                                {
                                    emissionTex.SetPixel(w, h, c);
                                    hasBright = true;
                                }
                            }
                        }
                        tex.Apply();
                    }
                    else
                    {
                        warn(WarnType.ReimportTexture, true);
                        var path = QPathTools.GetTextureAssetPath(qtexname, textureFolder);
                        Debug.LogWarning("reimporting texture: " + path + "; please reimport map");
                        TextureProcessor.ReimportTexture(path);
                    }


                    var mat = defaultSolid;
                    if (hasTransparancy)
                    {
                        mat = alphaCutout;
                    }

                    var newMat = new Material(mat);
                    m_materialDict[qtexname] = newMat;
                    newMat.SetTexture(BaseColorShaderParam, tex);
                    newMat.name = qtexname;
                    if (hasBright)
                    {
                        emissionTex.name = qtexname + "_em";
                        emissionTex.filterMode = FilterMode.Point;
                        emissionTex.Apply();
                        ctx.AddObjectToAsset(emissionTex.name + "_em", emissionTex);
                        newMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.AnyEmissive;
                        newMat.SetColor("_EmissionColor", Color.white);
                        newMat.SetTexture("_EmissionMap", emissionTex);
                    }
                }
                else
                {
                    var newMat = new Material(defaultSolid);
                    m_materialDict[qtexname] = newMat;
                    warn(WarnType.MissingTexture, true);
                    Debug.LogWarning("cannot find texture: " + qtexname);
                    continue;
                }

                mapData.textures[i] = qtex;
            }
        }

        #endregion
        #region Material Stuff
        public Texture2D FindTexture(MapData md, string texName)
        {
            var tex = FindTextureInWAD(md, texName);
            if (tex != null)
            {
                return tex;
            }
            var path = QPathTools.GetTextureAssetPath(texName, textureFolder);
            return (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
        }

        public Texture2D FindTextureInWAD(MapData md, string texName)
        {
            if (md.entities[0].properties.ContainsKey("wad"))
            {
                foreach (var entry in wadFiles)
                {
                    if (entry.wad != null)
                    {
                        var tex = entry.wad.FindTexture(texName);
                        if (tex != null)
                        {
                            return tex;
                        }
                    }
                }
            }
            return null;
        }

        private void setupBaseMaterials()
        {
            var baseMat = defaultSolid;
            var alphaMat = alphaCutout;
            var internalMatFolder = PKGPATH + "/Assets/Materials/";
            var pipeline = QPathTools.GetPipelineFolder(out BaseColorShaderParam);
            if (baseMat == null)
            {
                defaultSolid = (Material)AssetDatabase.LoadAssetAtPath(internalMatFolder + pipeline + "/Base.mat", typeof(Material));
            }
            if (alphaMat == null)
            {
                alphaCutout = (Material)AssetDatabase.LoadAssetAtPath(internalMatFolder + pipeline + "/Alpha.mat", typeof(Material));
                if (alphaCutout == null)
                {
                    alphaCutout = defaultSolid;
                }
            }
        }
    }
    #endregion

}