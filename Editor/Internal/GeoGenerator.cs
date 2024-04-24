using System;
using System.Collections.Generic;
using System.Linq;
using PlasticGui.WorkspaceWindow.PendingChanges;
using Unity.VisualScripting;
using UnityEngine;

namespace Qunity
{
    public class GeoGenerator
    {
        // Min distance between two verts in a brush before they're merged. Higher values fix angled brushes near extents.
        private const float CMP_EPSILON = 0.008f;

        private readonly Vector3 UP_VECTOR = new Vector3(0.0f, 0.0f, 1.0f);
        private readonly Vector3 RIGHT_VECTOR = new Vector3(0.0f, 1.0f, 0.0f);
        private readonly Vector3 FORWARD_VECTOR = new Vector3(1.0f, 0.0f, 0.0f);

        public MapData mapData;

        public GeoGenerator(MapData mapData)
        {
            this.mapData = mapData;
        }

        public void Run()
        {
            Span<Entity> entitySpan = mapData.GetEntitiesSpan();

            // Resize lists.
            mapData.entityGeo.Capacity = mapData.entities.Count;

            for (int i = 0; i < mapData.entityGeo.Capacity; i++)
            {
                mapData.entityGeo.Add(new EntityGeometry());
            }

            Span<EntityGeometry> entityGeoSpan = mapData.GetEntityGeoSpan();

            for (int e = 0; e < entitySpan.Length; e++)
            {
                entityGeoSpan[e].brushes.Capacity = entitySpan[e].brushes.Count;
                for (int i = 0; i < entityGeoSpan[e].brushes.Capacity; i++)
                {
                    entityGeoSpan[e].brushes.Add(new BrushGeometry());
                }

                Span<Brush> brushSpan = mapData.GetBrushesSpan(e);
                Span<BrushGeometry> brushGeoSpan = mapData.GetBrushGeoSpan(e);
                for (int b = 0; b < entitySpan[e].brushes.Count; b++)
                {
                    brushGeoSpan[b].faces.Capacity = brushSpan[b].faces.Count;
                    for (int i = 0; i < brushGeoSpan[b].faces.Capacity; i++)
                    {
                        brushGeoSpan[b].faces.Add(new FaceGeometry());
                    }
                }
            }

            // TODO: Multithread?
            GenerateAndFindCenters(0, entitySpan.Length);
            WindFaceVertices(0, entitySpan.Length);
            IndexFaceVertices(0, entitySpan.Length);
        }

        private void IndexFaceVertices(int startEntityIdx, int endEntityIdx)
        {
            for (int e = startEntityIdx; e < endEntityIdx; e++)
            {
                Span<BrushGeometry> brushGeoSpan = mapData.GetBrushGeoSpan(e);
                for (int b = 0; b < brushGeoSpan.Length; b++)
                {
                    Span<FaceGeometry> faceGeoSpan = mapData.GetFaceGeoSpan(e, b);
                    for (int f = 0; f < faceGeoSpan.Length; f++)
                    {
                        ref FaceGeometry faceGeo = ref faceGeoSpan[f];
                        if (faceGeo.vertices.Count < 3) continue;

                        faceGeo.indices.Capacity = (faceGeo.vertices.Count - 2) * 3;
                        for (int i = 0; i < faceGeo.vertices.Count - 2; i++)
                        {
                            faceGeo.indices.Add(0);
                            faceGeo.indices.Add(i + 1);
                            faceGeo.indices.Add(i + 2);
                        }
                    }
                }
            }
        }

        private void WindFaceVertices(int startEntityIdx, int endEntityIdx)
        {
            Span<EntityGeometry> entityGeoSpan = mapData.GetEntityGeoSpan();
            for (int e = startEntityIdx; e < endEntityIdx; e++)
            {
                for (int b = 0; b < entityGeoSpan[e].brushes.Count; b++)
                {
                    Span<Face> faceSpan = mapData.GetFacesSpan(e, b);
                    Span<FaceGeometry> faceGeoSpan = mapData.GetFaceGeoSpan(e, b);
                    for (int f = 0; f < faceSpan.Length; f++)
                    {
                        ref Face face = ref faceSpan[f];
                        Span<FaceVertex> vertexSpan = faceGeoSpan[f].vertices.ToArray().AsSpan();

                        if (vertexSpan.Length < 3) continue;

                        Vector3 windFaceBasis = (vertexSpan[1].vertex - vertexSpan[0].vertex).normalized;
                        Vector3 windFaceCenter = Vector3.zero;
                        Vector3 windFaceNormal = face.planeNormal.normalized;

                        for (int v = 0; v < vertexSpan.Length; v++)
                        {
                            windFaceCenter += vertexSpan[v].vertex;
                        }
                        windFaceCenter /= (float)vertexSpan.Length;

                        var vs = vertexSpan.ToArray();
                        Array.Sort(vs, (l, r) =>
                        {

                            Vector3 u = windFaceBasis.normalized;
                            Vector3 v = Vector3.Cross(u, windFaceNormal).normalized;

                            Vector3 loc_a = l.vertex - windFaceCenter;
                            float a_pu = Vector3.Dot(loc_a, u);
                            float a_pv = Vector3.Dot(loc_a, v);

                            Vector3 loc_b = r.vertex - windFaceCenter;
                            float b_pu = Vector3.Dot(loc_b, u);
                            float b_pv = Vector3.Dot(loc_b, v);

                            float a_angle = Mathf.Atan2(a_pv, a_pu);
                            float b_angle = Mathf.Atan2(b_pv, b_pu);

                            if (a_angle == b_angle) return 0;
                            return a_angle > b_angle ? 1 : -1;
                        });

                        // TODO find correct way to sort
                        for (int i = 0; i < vs.Length; i++)
                        {
                            faceGeoSpan[f].vertices[i] = vs[i];
                        }
                    }
                }
            }
        }

        // Theoretically thread safe.
        private void GenerateAndFindCenters(int startEntityIdx, int endEntityIdx)
        {
            Span<Entity> entitySpan = mapData.GetEntitiesSpan();

            for (int e = startEntityIdx; e < endEntityIdx; e++)
            {
                ref Entity entity = ref entitySpan[e];
                entity.center = Vector3.zero;

                Span<Brush> brushSpan = mapData.GetBrushesSpan(e);
                Span<BrushGeometry> brushGeoSpan = mapData.GetBrushGeoSpan(e);
                for (int b = 0; b < brushSpan.Length; b++)
                {
                    ref Brush brush = ref brushSpan[b];
                    brush.center = Vector3.zero;
                    int vertexCount = 0;

                    GenerateBrushVertices(e, b);

                    Span<FaceGeometry> faceGeoSpan = mapData.GetFaceGeoSpan(e, b);
                    for (int f = 0; f < faceGeoSpan.Length; f++)
                    {
                        Span<FaceVertex> vertexSpan = faceGeoSpan[f].vertices.ToArray().AsSpan();
                        for (int v = 0; v < vertexSpan.Length; v++)
                        {
                            brush.center += vertexSpan[v].vertex;
                            vertexCount++;
                        }
                    }

                    if (vertexCount > 0)
                    {
                        brush.center /= (float)vertexCount;
                        //							entity.center += brush.center;
                    }
                }

                if (brushSpan.Length > 0)
                {
                    entity.center /= (float)brushSpan.Length;
                }
            }
        }

        private void GenerateBrushVertices(int entityIdx, int brushIdx)
        {
            Span<Entity> entities = mapData.GetEntitiesSpan();
            ref Entity entity = ref entities[entityIdx];

            Span<Brush> brushes = mapData.GetBrushesSpan(entityIdx);
            ref Brush brush = ref brushes[brushIdx];
            int faceCount = brush.faces.Count;

            Span<Face> faces = mapData.GetFacesSpan(entityIdx, brushIdx);
            Span<FaceGeometry> faceGeos = mapData.GetFaceGeoSpan(entityIdx, brushIdx);
            Span<TextureData> textures = mapData.textures.ToArray().AsSpan();

            bool phong = entity.properties.GetValueOrDefault("_phong", "0") == "1";
            string phongAngleStr = entity.properties.GetValueOrDefault("_phong_angle", "89.0");
            // TODO: float phongAngle = phongAngleStr.IsValidFloat() ? phongAngleStr.ToFloat() : 89.0f;
            float phongAngle = 89.0f;
            for (int f0 = 0; f0 < faceCount; f0++)
            {
                ref Face face = ref faces[f0];
                ref FaceGeometry faceGeo = ref faceGeos[f0];
                ref TextureData texture = ref textures[face.textureIdx];

                for (int f1 = 0; f1 < faceCount; f1++)
                {
                    for (int f2 = 0; f2 < faceCount; f2++)
                    {
                        Vector3? vertex = IntersectFaces(ref faces[f0], ref faces[f1], ref faces[f2]);
                        if (vertex == null || !VertexInHull(brush.faces, vertex.Value)) continue;

                        // If we already generated a vertex close enough to this one, then merge them.
                        bool merged = false;
                        for (int f3 = 0; f3 <= f0; f3++)
                        {
                            ref FaceGeometry otherFaceGeo = ref faceGeos[f3];
                            for (int i = 0; i < otherFaceGeo.vertices.Count; i++)
                            {
                                if (Vector3.Distance(otherFaceGeo.vertices[i].vertex, vertex.Value) < CMP_EPSILON)
                                {
                                    vertex = otherFaceGeo.vertices[i].vertex;
                                    merged = true;
                                    break;
                                }
                            }
                            if (merged)
                            {
                                break;
                            }
                        }

                        Vector3 normal = Vector3.zero;
                        if (phong)
                        {
                            float threshold = Mathf.Cos((phongAngle + 0.01f) * 0.0174533f);
                            normal = face.planeNormal;
                            if (Vector3.Dot(face.planeNormal, faces[f1].planeNormal) > threshold)
                                normal += faces[f1].planeNormal;
                            if (Vector3.Dot(face.planeNormal, faces[f2].planeNormal) > threshold)
                                normal += faces[f2].planeNormal;
                        }
                        else
                        {
                            normal = face.planeNormal;
                        }

                        Vector2 uv;
                        Vector4 tangent;

                        if (face.isValveUV)
                        {
                            uv = GetValveUV(vertex.Value, ref face, texture.width, texture.height);
                            tangent = GetValveTangent(ref face);
                        }
                        else
                        {
                            uv = GetStandardUV(vertex.Value, ref face, texture.width, texture.height);
                            tangent = GetStandardTangent(ref face);
                        }

                        // Check for a duplicate vertex in the current face.
                        int duplicateIdx = -1;
                        for (int i = 0; i < faceGeo.vertices.Count; i++)
                        {
                            if (faceGeo.vertices[i].vertex == vertex.Value)
                            {
                                duplicateIdx = i;
                                break;
                            }
                        }

                        if (duplicateIdx < 0)
                        {
                            FaceVertex newVert = new FaceVertex();
                            newVert.vertex = vertex.Value;
                            newVert.normal = normal;
                            newVert.tangent = tangent;
                            newVert.uv = uv;
                            faceGeo.vertices.Add(newVert);
                        }
                        else if (phong)
                        {
                            FaceVertex duplicate = faceGeo.vertices[duplicateIdx];
                            duplicate.normal += normal;
                            faceGeo.vertices[duplicateIdx] = duplicate;
                        }
                    }
                }
            }

            for (int i = 0; i < faceGeos.Length; i++)
            {
                Span<FaceVertex> verts = faceGeos[i].vertices.ToArray().AsSpan();
                for (int j = 0; j < verts.Length; j++)
                {
                    verts[j].normal = verts[j].normal.normalized;
                }
            }
        }

        private Vector3? IntersectFaces(ref Face f0, ref Face f1, ref Face f2)
        {
            Vector3 n0 = f0.planeNormal;
            Vector3 n1 = f1.planeNormal;
            Vector3 n2 = f2.planeNormal;

            float denom = Vector3.Dot(Vector3.Cross(n0, n1), n2);
            if (denom < CMP_EPSILON) return null;

            var v = (Vector3.Cross(n1, n2) * f0.planeDist + Vector3.Cross(n2, n0) * f1.planeDist + Vector3.Cross(n0, n1) * f2.planeDist) / denom;
            return v;
        }

        private bool VertexInHull(List<Face> faces, Vector3 vertex)
        {
            for (int i = 0; i < faces.Count; i++)
            {
                float proj = Vector3.Dot(faces[i].planeNormal, vertex);
                if (proj > faces[i].planeDist && Mathf.Abs(faces[i].planeDist - proj) > CMP_EPSILON) return false;
            }

            return true;
        }

        private Vector2 GetStandardUV(Vector3 vertex, ref Face face, int texW, int texH)
        {
            Vector2 uvOut = Vector2.zero;

            float du = Mathf.Abs(Vector3.Dot(face.planeNormal, UP_VECTOR));
            float dr = Mathf.Abs(Vector3.Dot(face.planeNormal, RIGHT_VECTOR));
            float df = Mathf.Abs(Vector3.Dot(face.planeNormal, FORWARD_VECTOR));

            if (du >= dr && du >= df)
                uvOut = new Vector2(vertex.x, -vertex.y);
            else if (dr >= du && dr >= df)
                uvOut = new Vector2(vertex.x, -vertex.z);
            else if (df >= du && df >= dr)
                uvOut = new Vector2(vertex.y, -vertex.z);

            float angle = Mathf.Deg2Rad * face.uvExtra.rot;
            uvOut = new Vector2(
                uvOut.x * Mathf.Cos(angle) - uvOut.y * Mathf.Sin(angle),
                uvOut.x * Mathf.Sin(angle) + uvOut.y * Mathf.Cos(angle));

            uvOut.x /= texW;
            uvOut.y /= texH;

            uvOut.x /= face.uvExtra.scaleX;
            uvOut.y /= face.uvExtra.scaleY;

            uvOut.x += face.uvStandard.x / texW;
            uvOut.y += face.uvStandard.y / texH;

            return uvOut;
        }

        private Vector2 GetValveUV(Vector3 vertex, ref Face face, int texW, int texH)
        {
            Vector2 uvOut = Vector2.zero;
            Vector3 uAxis = face.uvValve.U.axis;
            Vector3 vAxis = face.uvValve.V.axis;
            float uShift = face.uvValve.U.offset;
            float vShift = face.uvValve.V.offset;

            uvOut.x = Vector3.Dot(uAxis, vertex);
            uvOut.y = Vector3.Dot(vAxis, vertex);

            uvOut.x /= texW;
            uvOut.y /= texH;

            uvOut.x /= face.uvExtra.scaleX;
            uvOut.y /= face.uvExtra.scaleY;

            uvOut.x += uShift / texW;
            uvOut.y += vShift / texH;

            return uvOut;
        }

        private Vector4 GetStandardTangent(ref Face face)
        {
            float du = Vector3.Dot(face.planeNormal, UP_VECTOR);
            float dr = Vector3.Dot(face.planeNormal, RIGHT_VECTOR);
            float df = Vector3.Dot(face.planeNormal, FORWARD_VECTOR);
            float dua = Mathf.Abs(du);
            float dra = Mathf.Abs(dr);
            float dfa = Mathf.Abs(df);

            Vector3 uAxis = Vector3.zero;
            float vSign = 0.0f;

            if (dua >= dra && dua >= dfa)
            {
                uAxis = FORWARD_VECTOR;
                vSign = Mathf.Sign(du);
            }
            else if (dra >= dua && dra >= dfa)
            {
                uAxis = FORWARD_VECTOR;
                vSign = -Mathf.Sign(dr);
            }
            else if (dfa >= dua && dfa >= dra)
            {
                uAxis = RIGHT_VECTOR;
                vSign = Mathf.Sign(df);
            }

            vSign *= Mathf.Sign(face.uvExtra.scaleY);
            // TODO: correct?
            uAxis = Vector3.RotateTowards(uAxis, face.planeNormal, Mathf.Deg2Rad * -face.uvExtra.rot * vSign, 0);

            return new Vector4(uAxis.x, uAxis.y, uAxis.z, vSign);
        }

        private Vector4 GetValveTangent(ref Face face)
        {
            Vector3 uAxis = face.uvValve.U.axis.normalized;
            Vector3 vAxis = face.uvValve.V.axis.normalized;
            float vSign = Mathf.Sign(Vector3.Dot(Vector3.Cross(face.planeNormal, uAxis), vAxis));
            return new Vector4(uAxis.x, uAxis.y, uAxis.z, vSign);
        }

        private int GetBrushVertexCount(int entityIdx, int brushIdx)
        {
            int count = 0;
            BrushGeometry brushGeo = mapData.entityGeo[entityIdx].brushes[brushIdx];
            for (int i = 0; i < brushGeo.faces.Count; i++)
            {
                count += brushGeo.faces[i].vertices.Count;
            }

            return count;
        }

        private int GetBrushIndexCount(int entityIdx, int brushIdx)
        {
            int count = 0;
            BrushGeometry brushGeo = mapData.entityGeo[entityIdx].brushes[brushIdx];
            for (int i = 0; i < brushGeo.faces.Count; i++)
            {
                count += brushGeo.faces[i].indices.Count;
            }

            return count;
        }
    }
}
