#define GENERATE_CAP

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VLB
{
    public static class MeshGenerator
    {
        const float kMinTruncatedRadius = 0.001f;

        public static Mesh GenerateConeZ_RadiusAndAngle(float lengthZ, float radiusStart, float coneAngle, int numSides, int numSegments, bool cap)
        {
            Debug.Assert(lengthZ > 0f);
            Debug.Assert(coneAngle > 0f && coneAngle < 180f);
            var radiusEnd = lengthZ * Mathf.Tan(coneAngle * Mathf.Deg2Rad * 0.5f);

            return GenerateConeZ_Radius(lengthZ, radiusStart, radiusEnd, numSides, numSegments, cap);
        }

        public static Mesh GenerateConeZ_Angle(float lengthZ, float coneAngle, int numSides, int numSegments, bool cap)
        {
            return GenerateConeZ_RadiusAndAngle(lengthZ, 0f, coneAngle, numSides, numSegments, cap);
        }

        public static Mesh GenerateConeZ_Radius(float lengthZ, float radiusStart, float radiusEnd, int numSides, int numSegments, bool cap)
        {
            Debug.Assert(lengthZ > 0f);
            Debug.Assert(radiusStart >= 0f);
            Debug.Assert(numSides >= 3);
            Debug.Assert(numSegments >= 0);

            var mesh = new Mesh();
            bool genCap = false;
#if GENERATE_CAP
            genCap = cap && radiusStart > 0f;
#endif
            // We use the XY position of the vertices to compute the cone normal in the shader.
            // With a perfectly sharp cone, we couldn't compute accurate normals at its top.
            radiusStart = Mathf.Max(radiusStart, kMinTruncatedRadius); 

            int vertCountSides = numSides * (numSegments + 2);
            int vertCountTotal = vertCountSides;

            if (genCap)
                vertCountTotal += numSides + 1;

            // VERTICES
            {
                var vertices = new Vector3[vertCountTotal];

                for (int i = 0; i < numSides; i++)
                {
                    float angle = 2 * Mathf.PI * i / numSides;
                    float angleCos = Mathf.Cos(angle);
                    float angleSin = Mathf.Sin(angle);

                    for (int seg = 0; seg < numSegments + 2; seg++)
                    {
                        float tseg = (float)seg / (numSegments + 1);
                        Debug.Assert(tseg >= 0f && tseg <= 1f);
                        float radius = Mathf.Lerp(radiusStart, radiusEnd, tseg);
                        vertices[i + seg * numSides] = new Vector3(radius * angleCos, radius * angleSin, tseg * lengthZ);
                    }
                }

                if (genCap)
                {
                    int ind = vertCountSides;

                    vertices[ind] = Vector3.zero;
                    ind++;

                    for (int i = 0; i < numSides; i++)
                    {
                        float angle = 2 * Mathf.PI * i / numSides;
                        float angleCos = Mathf.Cos(angle);
                        float angleSin = Mathf.Sin(angle);
                        vertices[ind] = new Vector3(radiusStart * angleCos, radiusStart * angleSin, 0f);
                        ind++;
                    }

                    Debug.Assert(ind == vertices.Length);
                }

                mesh.vertices = vertices;
            }

            // UV (used to flags vertices as sides or cap)
            // (0;0) => sides
            // (1;1) => cap
            {
                var uv = new Vector2[vertCountTotal];
                int ind = 0;
                for (int i = 0; i < vertCountSides; i++)
                    uv[ind++] = Vector2.zero;

                if (genCap)
                {
                    for (int i = 0; i < numSides + 1; i++)
                        uv[ind++] = Vector2.one;
                }

                Debug.Assert(ind == uv.Length);
                mesh.uv = uv;
            }


            // INDICES
            {
                int triCountSides = numSides * 2 * Mathf.Max(numSegments + 1, 1);
                int indCountSides = triCountSides * 3;
                int indCountTotal = indCountSides;

                if (genCap)
                    indCountTotal += numSides * 3;

                var indices = new int[indCountTotal];
                int ind = 0;

                for (int i = 0; i < numSides; i++)
                {
                    int ip1 = i + 1;
                    if (ip1 == numSides)
                        ip1 = 0;

                    for (int k = 0; k < numSegments + 1; ++k)
                    {
                        var offset = k * numSides;

                        indices[ind++] = offset + i;
                        indices[ind++] = offset + ip1;
                        indices[ind++] = offset + i + numSides;

                        indices[ind++] = offset + ip1 + numSides;
                        indices[ind++] = offset + i + numSides;
                        indices[ind++] = offset + ip1;
                    }
                }

                if (genCap)
                {
                    for (int i = 0; i < numSides - 1; i++)
                    {
                        indices[ind++] = vertCountSides;
                        indices[ind++] = vertCountSides + i + 1;
                        indices[ind++] = vertCountSides + i + 2;
                    }

                    indices[ind++] = vertCountSides;
                    indices[ind++] = vertCountSides + numSides;
                    indices[ind++] = vertCountSides + 1;
                }

                Debug.Assert(ind == indices.Length);
                mesh.triangles = indices;
            }
            return mesh;
        }

        public static Mesh GetSharedConeZ_Mesh(int numSides)
        {
            if(Config.Instance.SharedMeshConeZ==null)
            {
                Config.Instance.SharedMeshConeZ = GenerateConeZ_Radius(1, 1, 1, numSides, 0, false);
            }
            return Config.Instance.SharedMeshConeZ;
        }
        public static Mesh GetSharedCubeZ_Mesh(int numSides)
        {
            if (Config.Instance.SharedMeshCubeZ == null)
            {
                Config.Instance.SharedMeshCubeZ = GenerateConeZ_Radius(1, 1, 1, 3, 0, true);
            }
            return Config.Instance.SharedMeshCubeZ;
        }
    }
}
