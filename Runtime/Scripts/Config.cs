using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace VLB
{
    [HelpURL(Consts.HelpUrlConfig)]
    public class Config : ScriptableObject
    {
        /// <summary>
        /// The layer the procedural geometry gameObject is in
        /// </summary>
        public int geometryLayerID = 1;
        /// <summary>
        /// Main shader applied to the cone beam geometry
        /// </summary>
        [HighlightNull, FormerlySerializedAs("BeamShader")]
        public Shader beamShader = null;

        /// <summary>
        /// Global 3D Noise texture scaling: higher scale make the noise more visible, but potentially less realistic.
        /// </summary>
        [Range(Consts.NoiseScaleMin, Consts.NoiseScaleMax)]
        public float globalNoiseScale = Consts.NoiseScaleDefault;

        /// <summary>
        /// Global World Space direction and speed of the noise scrolling, simulating the fog/smoke movement
        /// </summary>
        public Vector3 globalNoiseVelocity = Consts.NoiseVelocityDefault;

        /// <summary>3D Noise param sent to the shader</summary>
        public Vector4 globalNoiseParam { get { return new Vector4(globalNoiseVelocity.x, globalNoiseVelocity.y, globalNoiseVelocity.z, globalNoiseScale); } }

        /// <summary>
        /// Binary file holding the 3D Noise texture data (a 3D array). Must be exactly Size * Size * Size bytes long.
        /// </summary>
        [HighlightNull]
        public TextAsset noise3DData = null;

        /// <summary>
        /// Size (of one dimension) of the 3D Noise data. Must be power of 2. So if the binary file holds a 32x32x32 texture, this value must be 32.
        /// </summary>
        public int noise3DSize = 64;

        public Mesh SharedMeshConeZ;
        public Mesh SharedMeshCubeZ;
        public Dictionary<int, Material> mats = new Dictionary<int, Material>();
        public Dictionary<int, Material> mats_Level1 = new Dictionary<int, Material>();
        public Dictionary<int, Material> mats_Level2 = new Dictionary<int, Material>();
        //https://docs.unity3d.com/Manual/GPUInstancing.html If you have more than two passes for multi-pass Shaders, only the first passes can be instanced. This is because Unity forces the later passes to be rendered together for each object, forcing Material changes.
        public Material GetSharedMaterial(bool noise, bool depthBlend, bool ClippingPlane,bool front,int Level=0,Texture2D OverrideTexture=null)
        {
            switch(Level)
            {
                case 0:return GetMaterialLevel0(noise, depthBlend, ClippingPlane, front);
                case 1:return GetMaterialLevel1(noise,front, OverrideTexture);
            }
            return null;
        }
        public Material GetMaterialLevel0(bool noise, bool depthBlend, bool ClippingPlane, bool front)
        {
            int currstate = 0;
            currstate |= noise ? 1 : 0;
            currstate |= depthBlend ? 1 << 1 : 0;
            currstate |= ClippingPlane ? 1 << 2 : 0;
            currstate |= front ? 1 << 3 : 0;
            Material mat = null;
            if (!mats.TryGetValue(currstate, out mat))
            {
                mat = new Material(beamShader);
                mat.enableInstancing = true;
                mat.name = currstate.ToString();
                mat.renderQueue = mat.renderQueue + mats.Count;
                mat.SetInt("_Cull", front ? 1 : 2);
                if (noise)
                    mat.EnableKeyword("VLB_NOISE_3D");
                if (depthBlend)
                    mat.EnableKeyword("VLB_DEPTH_BLEND");
                if (ClippingPlane)
                    mat.EnableKeyword("VLB_CLIPPING_PLANE");
                mats[currstate] = mat;
            }
            return mat;
        }
        public Material GetMaterialLevel1(bool noise, bool front,Texture2D LightTexture=null)
        {
            int currstate = 0;
            currstate |= noise ? 1 : 0;
            currstate |= front ? 1 << 1 : 0;
            if(LightTexture)
            {
                currstate +=LightTexture.GetInstanceID();
            }
            Material mat = null;
            if (!mats_Level1.TryGetValue(currstate, out mat))
            {
                mat = new Material(L1_beamShader);
                mat.name ="LOD1"+currstate.ToString();
                mat.renderQueue = mat.renderQueue + mats_Level1.Count+100;
                mat.SetInt("_Cull", front ? 1 : 2);
                mat.enableInstancing = true;
                if (noise)
                {
                    if(!LightTexture)
                    {
                        LightTexture = L1_LightNoise;
                    }
                    mat.EnableKeyword("VLB_NOISE_3D");
                }
                else
                {
                    if (!LightTexture)
                    {
                        LightTexture = L1_LightTexture;
                    }
                }
                mat.SetTexture("_MainTex", LightTexture);
                mats_Level1[currstate] = mat;
            }
            return mat;
        }
        /// <summary>
        /// ParticleSystem prefab instantiated for the Volumetric Dust Particles feature (Unity 5.5 or above)
        /// </summary>
        [HighlightNull]
        public ParticleSystem dustParticlesPrefab = null;

        public void Reset()
        {
            geometryLayerID = 1;
            beamShader = Shader.Find("VolumetricLightBeam/Beam");

            globalNoiseScale = Consts.NoiseScaleDefault;
            globalNoiseVelocity = Consts.NoiseVelocityDefault;

            noise3DData = Resources.Load("Noise3D_64x64x64") as TextAsset;
            noise3DSize = 64;

            dustParticlesPrefab = Resources.Load("DustParticles", typeof(ParticleSystem)) as ParticleSystem;
            
        }

        public ParticleSystem NewVolumetricDustParticles()
        {
            if (!dustParticlesPrefab)
            {
                if (Application.isPlaying)
                {
                    Debug.LogError("Failed to instantiate VolumetricDustParticles prefab.");
                }
                return null;
            }

            var instance = Instantiate(dustParticlesPrefab);
#if UNITY_5_4_OR_NEWER
            instance.useAutoRandomSeed = false;
#endif
            instance.name = "Dust Particles";
            instance.gameObject.hideFlags = Consts.ProceduralObjectsHideFlags;
            instance.gameObject.SetActive(true);
            return instance;
        }


        ///Level 1
        public Texture2D L1_LightTexture;
        public Texture2D L1_LightNoise;
        public Shader L1_beamShader;


#if UNITY_EDITOR
        public static void EditorSelectInstance()
        {
            Selection.activeObject = Config.Instance;
            if(Selection.activeObject == null)
                Debug.LogErrorFormat("Cannot find any Config resource");
        }
#endif

        // Singleton management
        static Config m_Instance = null;
        public static Config Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    var found = Resources.LoadAll<Config>("Config");
                    Debug.Assert(found.Length != 0, string.Format("Can't find any resource of type '{0}'. Make sure you have a ScriptableObject of this type in a 'Resources' folder.", typeof(Config)));
                    m_Instance = found[0];
#if UNITY_EDITOR
                    EditorSceneManager.activeSceneChangedInEditMode += SceneChanged;
#else
                    SceneManager.activeSceneChanged += SceneChanged;
#endif
                }
                return m_Instance;
            }
        }

        private static void SceneChanged(Scene arg0, Scene arg1)
        {
            foreach (var item in Instance.mats)
            {
                DestroyImmediate(item.Value);
            }
            foreach (var item in Instance.mats_Level1)
            {
                DestroyImmediate(item.Value);
            }
            foreach (var item in Instance.mats_Level2)
            {
                DestroyImmediate(item.Value);
            }
            Instance.mats.Clear();
            Instance.mats_Level1.Clear();
            Instance.mats_Level2.Clear();

        }
    }
}
