
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;

namespace VLB
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [SelectionBase]
    [HelpURL(Consts.HelpUrlBeam)]
    public class VolumetricLightBeam : MonoBehaviour
    {
        public enum LOD
        {
            Level0,
            Level1
        }
        [Tooltip("细节等级")]
        public LOD lod = LOD.Level0;
        public Renderer QuadRenderer;
        public Color color = Color.white;
        public float Intensity = 1;
        [Tooltip("内部透明度")]
        [Range(0f, 1f)] public float alphaInside = Consts.Alpha;
        [Tooltip("外部透明度")]
        [Range(0f, 1f)] public float alphaOutside = Consts.Alpha;
        [Tooltip("角度")]
        [Range(Consts.SpotAngleMin, Consts.SpotAngleMax)]
        public float spotAngle = Consts.SpotAngleDefault;
        public float coneAngle { get { return Mathf.Atan2(coneRadiusEnd - coneRadiusStart, fadeEnd) * Mathf.Rad2Deg * 2f; } }
        public float coneRadiusStart = Consts.ConeRadiusStart;
        public float coneRadiusEnd { get { return fadeEnd * Mathf.Tan(spotAngle * Mathf.Deg2Rad * 0.5f); } }
        public float coneVolume { get { float r1 = coneRadiusStart, r2 = coneRadiusEnd; return (Mathf.PI / 3) * (r1*r1 + r1*r2 + r2*r2) * fadeEnd; } }
        public float coneApexOffsetZ {
            get { // simple intercept
                float ratioRadius = coneRadiusStart / coneRadiusEnd;
                return ratioRadius == 1f ? 0f : ((fadeEnd * ratioRadius) / (1 - ratioRadius));
            }
        }

        /// <summary>
        /// Number of Sides of the cone.
        /// Higher values give better looking results, but require more memory and graphic performance.
        /// </summary>
        public int geomSides = Consts.GeomSidesDefault;

        public enum AttenuationEquation
        {
            Linear = 0,     // Simple linear attenuation.
            Quadratic = 1,  // Quadratic attenuation, which usually gives more realistic results.
            Blend = 2       // Custom blending mix between linear and quadratic attenuation formulas. Use attenuationEquation property to tweak the mix.
        }

        /// <summary>
        /// Light attenuation formula used to compute fading between 'fadeStart' and 'fadeEnd'
        /// </summary>
        public AttenuationEquation attenuationEquation = Consts.AttenuationEquation;

        /// <summary>
        /// Custom blending mix between linear and quadratic attenuation formulas.
        /// Only used if attenuationEquation is set to AttenuationEquation.Blend.
        /// 0.0 = 100% Linear
        /// 0.5 = Mix between 50% Linear and 50% Quadratic
        /// 1.0 = 100% Quadratic
        /// </summary>
        [Range(0f, 1f)]
        public float attenuationCustomBlending = Consts.AttenuationCustomBlending;

        /// <summary>
        /// Proper lerp value between linear and quadratic attenuation, used by the shader.
        /// </summary>
        public float attenuationLerpLinearQuad {
            get {
                if(attenuationEquation == AttenuationEquation.Linear) return 0f;
                else if (attenuationEquation == AttenuationEquation.Quadratic) return 1f;
                return attenuationCustomBlending;
            }
        }

        /// <summary>
        /// Distance from the light source (in units) the beam will start to fade out.
        /// </summary>
        public float fadeStart = Consts.FadeStart;

        /// <summary>
        /// Distance from the light source (in units) the beam is entirely faded out (alpha = 0, no more cone mesh).
        /// </summary>
        public float fadeEnd = Consts.FadeEnd;

        /// <summary>
        /// Distance from the world geometry the beam will fade.
        /// 0 = hard intersection
        /// Higher values produce soft intersection when the beam intersects other opaque geometry.
        /// </summary>
        public float depthBlendDistance = Consts.DepthBlendDistance;

        /// <summary>
        /// Distance from the camera the beam will fade.
        /// 0 = hard intersection
        /// Higher values produce soft intersection when the camera is near the cone triangles.
        /// </summary>
        public float cameraClippingDistance = Consts.CameraClippingDistance;

        /// <summary>
        /// Boost intensity factor when looking at the beam from the inside directly at the source.
        /// </summary>
        [Range(0f, 1f)]
        public float glareFrontal = Consts.GlareFrontal;

        /// <summary>
        /// Boost intensity factor when looking at the beam from behind.
        /// </summary>
        [Range(0f, 1f)]
        public float glareBehind = Consts.GlareBehind;

        [System.Obsolete("Use 'glareFrontal' instead")]
        public float boostDistanceInside = 0.5f;

        [System.Obsolete("This property has been merged with 'fresnelPow'")]
        public float fresnelPowInside = 6f;

        /// <summary>
        /// Modulate the thickness of the beam when looking at it from the side.
        /// Higher values produce thinner beam with softer transition at beam edges.
        /// </summary>
        [FormerlySerializedAs("fresnelPowOutside")]
        public float fresnelPow = Consts.FresnelPow;

        /// <summary>
        /// Enable 3D Noise effect
        /// </summary>
        public bool noiseEnabled = false;

        /// <summary>
        /// Contribution factor of the 3D Noise (when enabled).
        /// Higher intensity means the noise contribution is stronger and more visible.
        /// </summary>
        [Range(0f, 1f)] public float noiseIntensity = Consts.NoiseIntensityDefault;

        /// <summary>
        /// Get the noiseScale value from the Global 3D Noise configuration
        /// </summary>
        public bool noiseScaleUseGlobal = true;

        /// <summary>
        /// 3D Noise texture scaling: higher scale make the noise more visible, but potentially less realistic.
        /// </summary>
        [Range(Consts.NoiseScaleMin, Consts.NoiseScaleMax)] public float noiseScaleLocal = Consts.NoiseScaleDefault;

        /// <summary>
        /// Get the noiseVelocity value from the Global 3D Noise configuration
        /// </summary>
        public bool noiseVelocityUseGlobal = true;

        /// <summary>
        /// World Space direction and speed of the 3D Noise scrolling, simulating the fog/smoke movement.
        /// </summary>
        public Vector3 noiseVelocityLocal = Consts.NoiseVelocityDefault;

        /// <summary>
        /// If true, the light beam will keep track of the changes of its own properties and the spotlight attached to it (if any) during playtime.
        /// This would allow you to modify the light beam in realtime from Script, Animator and/or Timeline.
        /// Enabling this feature is at very minor performance cost. So keep it disabled if you don't plan to modify this light beam during playtime.
        /// </summary>
        public bool trackChangesDuringPlaytime
        {
            get { return _TrackChangesDuringPlaytime; }
            set { _TrackChangesDuringPlaytime = value;}
        }

        /// <summary> Is the beam currently tracking property changes? </summary>
        public bool isCurrentlyTrackingChanges { get { return m_CoPlaytimeUpdate != null; } }

        /// <summary> Has the geometry already been generated? </summary>
        public bool hasGeometry { get { return m_BeamGeom != null; } }

        /// <summary> Bounds of the geometry's mesh (if the geometry exists) </summary>
        public Bounds bounds { get { return m_BeamGeom != null ? m_BeamGeom.meshRenderer.bounds : new Bounds(Vector3.zero, Vector3.zero); } }

        /// <summary> Set the clipping plane equation. This function is used internally by the DynamicOcclusion component. </summary>
        public void SetClippingPlane(Plane planeWS) { if (m_BeamGeom) m_BeamGeom.SetClippingPlane(planeWS); }

        /// <summary> Disable the clipping plane. This function is used internally by the DynamicOcclusion component. </summary>
        public void SetClippingPlaneOff() { if (m_BeamGeom) m_BeamGeom.SetClippingPlaneOff(); }


        [FormerlySerializedAs("trackChangesDuringPlaytime")]
        [SerializeField] bool _TrackChangesDuringPlaytime = false;

        //front
        [SerializeField]
        BeamGeometry m_BeamGeom = null;
        //
        [SerializeField]
        BeamGeometry m_BeamGeom_back = null;
        Coroutine m_CoPlaytimeUpdate = null;

        public Texture2D OverrideTexture;

        public string meshStats
        {
            get
            {
                Mesh mesh = m_BeamGeom ? m_BeamGeom.coneMesh : null;
                if (mesh) return string.Format("Cone angle: {0:0.0} degrees\nMesh: {1} vertices, {2} triangles", coneAngle, mesh.vertexCount, mesh.triangles.Length / 3);
                else return "no mesh available";
            }
        }

        Light _CachedLight = null;
        Light lightSpotAttached
        {
            get
            {
                if(_CachedLight == null) _CachedLight = GetComponent<Light>();
                if (_CachedLight && _CachedLight.type == LightType.Spot) return _CachedLight;
                return null;
            }
        }

        /// <summary>
        /// Returns a value indicating if the world position passed in argument is inside the light beam or not.
        /// This functions treats the beam like infinite (like the beam had an infinite length and never fell off)
        /// </summary>
        /// <param name="posWS">World position</param>
        /// <returns>
        /// < 0 position is out
        /// = 0 position is exactly on the beam geometry 
        /// > 0 position is inside the cone
        /// </returns>
        public float GetInsideBeamFactor(Vector3 posWS)
        {
            var posOS = transform.InverseTransformPoint(posWS);
            if (posOS.z < 0f) return -1f;

            // Compute a factor to know how far inside the beam cone the camera is
            var triangle2D = new Vector2(posOS.xy().magnitude, posOS.z + coneApexOffsetZ).normalized;
            const float maxRadiansDiff = 0.1f;
            float slopeRad = (coneAngle * Mathf.Deg2Rad) / 2;

            return Mathf.Clamp((Mathf.Abs(Mathf.Sin(slopeRad)) - Mathf.Abs(triangle2D.x)) / maxRadiansDiff, -1, 1);
        }

        /// <summary>
        /// Regenerate the beam mesh (and also the material).
        /// This can be slow (it recreates a mesh from scratch), so don't call this function during playtime.
        /// You would need to call this function only if you want to change the properties 'geomSides' and 'geomCap' during playtime.
        /// Otherwise, for the other properties, just enable 'trackChangesDuringPlaytime', or manually call 'UpdateAfterManualPropertyChange()'
        /// </summary>
        public void GenerateGeometry()
        {
            if (m_BeamGeom == null)
            {
                m_BeamGeom = Utils.NewWithComponent<BeamGeometry>("Beam Geometry");
            }
            m_BeamGeom.Initialize(this);
            m_BeamGeom.visible = enabled;
            if (m_BeamGeom_back==null)
            {
                m_BeamGeom_back = Utils.NewWithComponent<BeamGeometry>("Beam Geometry");
                m_BeamGeom_back.front = false;
            }
            m_BeamGeom_back.Initialize(this);
            m_BeamGeom_back.visible = enabled;
        }

        /// <summary>
        /// Update the beam material and its bounds.
        /// Calling manually this function is useless if your beam has its property 'trackChangesDuringPlaytime' enabled
        /// (because then this function is automatically called each frame).
        /// However, if 'trackChangesDuringPlaytime' is disabled, and you change a property via Script for example,
        /// you need to call this function to take the property change into account.
        /// All properties changes are took into account, expect 'geomSides' and 'geomCap' which require to regenerate the geometry via 'GenerateGeometry()'
        /// </summary>
        public void UpdateAfterManualPropertyChange()
        {
            if(QuadRenderer)
            {
              var block=  QuadRenderer.GetComponent<Renderer>().GetPropertyBlock();
                block.SetColor("_Color", this.color);
                QuadRenderer.GetComponent<Renderer>().SetPropertyBlock(block);
            }

            if (m_BeamGeom) m_BeamGeom.UpdateMaterialAndBounds();
            if (m_BeamGeom_back) m_BeamGeom_back.UpdateMaterialAndBounds();
        }

        void Start()
        {
            // In standalone builds, simply generate the geometry once in Start
            GenerateGeometry();
        }
        void Update()
        {
            if (trackChangesDuringPlaytime) // during Playtime, realtime changes are handled by CoUpdateDuringPlaytime
            {
                UpdateAfterManualPropertyChange();
            }
            else
            {
#if UNITY_EDITOR
                UpdateAfterManualPropertyChange();
#endif
            }
            // If we modify the attached Spotlight properties, or if we animate the beam via Unity 2017's timeline,
            // we are not notified of properties changes. So we update the material anyway.
        }

#if UNITY_EDITOR
        public void Reset()
        {
            color = Color.white;
            alphaInside = Consts.Alpha;
            alphaOutside = Consts.Alpha;

            spotAngle = Consts.SpotAngleDefault;

            coneRadiusStart = Consts.ConeRadiusStart;
            geomSides = Consts.GeomSidesDefault;
            fadeStart = Consts.FadeStart;
            fadeEnd = Consts.FadeEnd;

            depthBlendDistance = Consts.DepthBlendDistance;
            cameraClippingDistance = Consts.CameraClippingDistance;

            glareFrontal = Consts.GlareFrontal;
            glareBehind = Consts.GlareBehind;

            fresnelPow = Consts.FresnelPow;

            noiseEnabled = false;
            noiseIntensity = Consts.NoiseIntensityDefault;
            noiseScaleUseGlobal = true;
            noiseScaleLocal = Consts.NoiseScaleDefault;
            noiseVelocityUseGlobal = true;
            noiseVelocityLocal = Consts.NoiseVelocityDefault;

            trackChangesDuringPlaytime = false;

        }
#endif

        void OnEnable()
        {
            if (m_BeamGeom) m_BeamGeom.visible = true;
            if (m_BeamGeom_back) m_BeamGeom_back.visible = true;
        }

        void OnDisable()
        {
            if (m_BeamGeom) m_BeamGeom.visible = false;
            if (m_BeamGeom_back) m_BeamGeom_back.visible = false;
            m_CoPlaytimeUpdate = null;
        }
        void OnDestroy()
        {
            if (m_BeamGeom) DestroyImmediate(m_BeamGeom.gameObject); // Make sure to delete the GAO
            m_BeamGeom = null;
            if (m_BeamGeom_back) DestroyImmediate(m_BeamGeom_back.gameObject); // Make sure to delete the GAO
            m_BeamGeom_back = null;

        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position + transform.forward * -coneApexOffsetZ, transform.position);
        }
    }
}
