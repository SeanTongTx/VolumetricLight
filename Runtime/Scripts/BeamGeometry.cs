#if DEBUG
//#define DEBUG_SHOW_MESH_NORMALS
//#define DEBUG_SHOW_BOUNDING_BOX
#endif
using UnityEngine;

namespace VLB
{
    [AddComponentMenu("")] // hide it from Component search
    [ExecuteInEditMode]
    public class BeamGeometry : MonoBehaviour
    {
        public bool front = true;
        public VolumetricLightBeam.LOD Lod;
        public Texture2D overrideTexture;
        const int kNbSegments = 0;

        public VolumetricLightBeam m_Master = null;

        public MeshRenderer meshRenderer { get; private set; }
        public MeshFilter meshFilter { get; private set; }
        public Material material { get; private set; }
        public Mesh coneMesh { get; private set; }

        public bool visible
        {
            get { return meshRenderer??meshRenderer.enabled; }
            set { if(meshRenderer)meshRenderer.enabled = value; }
        }

        void Start()
        {
            // Handle copy / paste the LightBeam in Editor
            if (!m_Master)
            {
                DestroyImmediate(gameObject);
            }
        }

        void OnDestroy()
        {
            if (material)
            {
                //修改为共享材质
                // DestroyImmediate(material);
                material = null;
            }
        }

        public void Initialize(VolumetricLightBeam master)
        {
            var hideFlags = Consts.ProceduralObjectsHideFlags;
            m_Master = master;
            this.Lod = m_Master.lod;
            bool noise = (m_Master.noiseEnabled && m_Master.noiseIntensity > 0f && Noise3D.isSupported);
            bool depth = (m_Master.depthBlendDistance > 0f);
            transform.SetParent(master.transform, false);
            meshRenderer = gameObject.GetOrAddComponent<MeshRenderer>();

            ChangeMat(noise, depth, false);
            meshRenderer.hideFlags = hideFlags;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            meshFilter = gameObject.GetOrAddComponent<MeshFilter>();
            meshRenderer.material = material;

            material.hideFlags = hideFlags;

            gameObject.hideFlags = hideFlags;

            gameObject.layer = Config.Instance.geometryLayerID;

            coneMesh = MeshGenerator.GetSharedConeZ_Mesh(m_Master.geomSides);
            meshFilter.mesh = coneMesh;
            UpdateMaterialAndBounds();
        }
        void ComputeBounds()
        {
            if (coneMesh)
            {
                var radiusStart = m_Master.coneRadiusStart;
                var radiusEnd = m_Master.coneRadiusEnd;
                var radiusMax = Mathf.Max(radiusStart, radiusEnd);
                var length = m_Master.fadeEnd;

                var bounds = new Bounds(
                    new Vector3(0, 0, length / 2),
                    new Vector3(radiusMax * 2, radiusMax * 2, length)
                    );
                coneMesh.bounds = bounds;
            }
        }
        public void ChangeMat(bool noise, bool depthBlend, bool ClippingPlane)
        {
            this.Lod = m_Master.lod;
            overrideTexture = m_Master.OverrideTexture;
            material = Config.Instance.GetSharedMaterial(noise, depthBlend, ClippingPlane, front,(int)Lod, overrideTexture);
            //front ?  : Config.Instance.GetBackMaterial(noise, depthBlend, ClippingPlane);
            if (meshRenderer)
            {
                meshRenderer.material = material;
            }
        }
        Color Linear2GammaColor(Color linRGB)
        {
            float r = Mathf.Max(1.055f * Mathf.Pow(linRGB.r, 0.416666667f) - 0.055f, 0);
            float g = Mathf.Max(1.055f * Mathf.Pow(linRGB.g, 0.416666667f) - 0.055f, 0);
            float b = Mathf.Max(1.055f * Mathf.Pow(linRGB.b, 0.416666667f) - 0.055f, 0);
            return new Color(r, g, b, linRGB.a);
        }

        public void UpdateMaterialAndBounds()
        {

            bool change = false;
            bool noise = material.IsKeywordEnabled("VLB_NOISE_3D");
            bool depthBlend = material.IsKeywordEnabled("VLB_DEPTH_BLEND");
            bool ClippingPlane = material.IsKeywordEnabled("VLB_CLIPPING_PLANE");
            Debug.Assert(m_Master);

            if (m_Master.lod!= Lod)
            {
                change = true;
            }
            if(m_Master.OverrideTexture!= overrideTexture)
            {
                change = true;
            }
            float slopeRad = (m_Master.coneAngle * Mathf.Deg2Rad) / 2; // use coneAngle (instead of spotAngle) which is more correct with the geometry
            var coneRadius = new Vector2(m_Master.coneRadiusStart, m_Master.coneRadiusEnd);
            var block = meshRenderer.GetPropertyBlock();
            block.SetVector("_ConeSlopeCosSin", new Vector2(Mathf.Cos(slopeRad), Mathf.Sin(slopeRad)));
            block.SetVector("_ConeRadius", coneRadius);
            block.SetFloat("_ConeApexOffsetZ", m_Master.coneApexOffsetZ);
            block.SetFloat("_Intensity", m_Master.Intensity);
            block.SetColor("_Color", m_Master.color);
            block.SetFloat("_AlphaInside", m_Master.alphaInside);
            block.SetFloat("_AlphaOutside", m_Master.alphaOutside);
            block.SetFloat("_AttenuationLerpLinearQuad", m_Master.attenuationLerpLinearQuad);
            block.SetFloat("_DistanceFadeStart", m_Master.fadeStart);
            block.SetFloat("_DistanceFadeEnd", m_Master.fadeEnd);
            block.SetFloat("_DistanceCamClipping", m_Master.cameraClippingDistance);
            block.SetFloat("_FresnelPow", m_Master.fresnelPow);
            block.SetFloat("_GlareBehind", m_Master.glareBehind);
            block.SetFloat("_GlareFrontal", m_Master.glareFrontal);

            if (m_Master.depthBlendDistance > 0f)
            {
                if (depthBlend == false)
                {
                    depthBlend = true;
                    change = true;
                }
                // material.EnableKeyword("VLB_DEPTH_BLEND");
                block.SetFloat("_DepthBlendDistance", m_Master.depthBlendDistance);
            }
            else
            {
                if (depthBlend)
                {
                    depthBlend = false;
                    change = true;
                }
            }
            /*   material.DisableKeyword("VLB_DEPTH_BLEND");*/

            if (m_Master.noiseEnabled && m_Master.noiseIntensity > 0f && Noise3D.isSupported) // test Noise3D.isSupported the last
            {
                Noise3D.LoadIfNeeded();
                if (noise == false)
                {
                    noise = true;
                    change = true;
                }
                /* material.EnableKeyword("VLB_NOISE_3D");*/
                block.SetVector("_NoiseLocal", new Vector4(m_Master.noiseVelocityLocal.x, m_Master.noiseVelocityLocal.y, m_Master.noiseVelocityLocal.z, m_Master.noiseScaleLocal));
                block.SetVector("_NoiseParam", new Vector3(m_Master.noiseIntensity, m_Master.noiseVelocityUseGlobal ? 1f : 0f, m_Master.noiseScaleUseGlobal ? 1f : 0f));
            }
            else
            {
                if (noise)
                {
                    noise = false;
                    change = true;
                }
            }
            /*   material.DisableKeyword("VLB_NOISE_3D");*/
            if (change)
            {
                ChangeMat(noise, depthBlend, ClippingPlane);
            }
            meshRenderer.SetPropertyBlock(block);
            // Need to manually compute mesh bounds since the shape of the cone is generated in the Vertex Shader
            ComputeBounds();

#if DEBUG_SHOW_MESH_NORMALS
            for (int vertexInd = 0; vertexInd < coneMesh.vertexCount; vertexInd++)
            {
                var vertex = coneMesh.vertices[vertexInd];

                // apply modification done inside VS
                vertex.x *= Mathf.Lerp(coneRadius.x, coneRadius.y, vertex.z);
                vertex.y *= Mathf.Lerp(coneRadius.x, coneRadius.y, vertex.z);
                vertex.z *= m_Master.fadeEnd;

                var cosSinFlat = new Vector2(vertex.x, vertex.y).normalized;
                var normal = new Vector3(cosSinFlat.x * Mathf.Cos(slopeRad), cosSinFlat.y * Mathf.Cos(slopeRad), -Mathf.Sin(slopeRad)).normalized;

                vertex = transform.TransformPoint(vertex);
                normal = transform.TransformDirection(normal);
                Debug.DrawRay(vertex, normal * 0.25f);
            }
#endif
        }

        public void SetClippingPlane(Plane planeWS)
        {
            bool noise = material.IsKeywordEnabled("VLB_NOISE_3D");
            bool depthBlend = material.IsKeywordEnabled("VLB_DEPTH_BLEND");
            bool ClippingPlane = true;

            var normal = planeWS.normal;
            /*material.EnableKeyword("VLB_CLIPPING_PLANE");*/
            ChangeMat(noise, depthBlend, ClippingPlane);
            var block = meshRenderer.GetPropertyBlock();
            block.SetVector("_ClippingPlaneWS", new Vector4(normal.x, normal.y, normal.z, planeWS.distance));
            meshRenderer.SetPropertyBlock(block);
        }

        public void SetClippingPlaneOff()
        {
            bool noise = material.IsKeywordEnabled("VLB_NOISE_3D");
            bool depthBlend = material.IsKeywordEnabled("VLB_DEPTH_BLEND");
            bool ClippingPlane = false;
            ChangeMat(noise, depthBlend, ClippingPlane);
            // material.DisableKeyword("VLB_CLIPPING_PLANE");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            //DrawMeshBounds();
           // DrawRendererBounds();
        }
        void DrawRendererBounds()
        {
            var bounds = meshRenderer.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
        void DrawMeshBounds()
        {
            var bounds = meshFilter.sharedMesh.bounds;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
#if DEBUG_SHOW_BOUNDING_BOX
       
#endif

        void OnWillRenderObject()
        {
            if (m_Master)
            {
                var cam = Camera.current;
                if (material)
                {
                    var block = meshRenderer.GetPropertyBlock();
                    var camForwardVectorOSN = transform.InverseTransformDirection(cam.transform.forward).normalized;
                    float camIsInsideBeamFactor = cam.orthographic ? -1f : m_Master.GetInsideBeamFactor(cam.transform.position);
                    block.SetVector("_CameraParams", new Vector4(camForwardVectorOSN.x, camForwardVectorOSN.y, camForwardVectorOSN.z, camIsInsideBeamFactor));
                    meshRenderer.SetPropertyBlock(block);
                }
                /*
#if FORCE_CURRENT_CAMERA_DEPTH_TEXTURE_MODE
                if (m_Master.depthBlendDistance > 0f)
                    cam.depthTextureMode |= DepthTextureMode.Depth;
#endif
*/
            }
        }
    }
}
