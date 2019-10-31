#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace VLB
{
    [CustomEditor(typeof(Config))]
    public class ConfigEditor : EditorCommon
    {
        SerializedProperty geometryLayerID, beamShader;
        SerializedProperty globalNoiseScale, globalNoiseVelocity;
        SerializedProperty noise3DData, noise3DSize;
        SerializedProperty dustParticlesPrefab;

        SerializedProperty SharedMesh;
        //Level1
        SerializedProperty L1_Texture;
        SerializedProperty L1_Noise;
        SerializedProperty L1_beamShader;
        protected override void OnEnable()
        {
            base.OnEnable();

            geometryLayerID = FindProperty((Config x) => x.geometryLayerID);
            beamShader = FindProperty((Config x) => x.beamShader);

            globalNoiseScale = FindProperty((Config x) => x.globalNoiseScale);
            globalNoiseVelocity = FindProperty((Config x) => x.globalNoiseVelocity);

            noise3DData = FindProperty((Config x) => x.noise3DData);
            noise3DSize = FindProperty((Config x) => x.noise3DSize);

            dustParticlesPrefab = FindProperty((Config x) => x.dustParticlesPrefab);
            SharedMesh = FindProperty((Config x) => x.SharedMeshConeZ);

            Noise3D.LoadIfNeeded(); // Try to load Noise3D, maybe for the 1st time
            L1_Texture = FindProperty((Config x) => x.L1_LightTexture);
            L1_Noise = FindProperty((Config x) => x.L1_LightNoise);
            L1_beamShader = FindProperty((Config x) => x.L1_beamShader);

        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.LabelField(string.Format("Current Plugin Version: {0}", Version.Current), EditorStyles.miniBoldLabel);

            Header("Beam Geometry");
            EditorGUI.BeginChangeCheck();
            geometryLayerID.intValue = EditorGUILayout.LayerField(new GUIContent("Layer", "The layer the GameObjects holding the procedural cone meshes are created on"), geometryLayerID.intValue);
            if (EditorGUI.EndChangeCheck())
            {

            }
            EditorGUILayout.PropertyField(beamShader, new GUIContent("Shader", "Main shader applied to the cone beam geometry"));
            EditorGUILayout.PropertyField(SharedMesh, new GUIContent("SharedMesh", "共享模型"));

            Header("Global 3D Noise");
            EditorGUILayout.PropertyField(globalNoiseScale, new GUIContent("Scale", "Global 3D Noise texture scaling: higher scale make the noise more visible, but potentially less realistic"));
            EditorGUILayout.PropertyField(globalNoiseVelocity, new GUIContent("Velocity", "Global World Space direction and speed of the noise scrolling, simulating the fog/smoke movement"));

            Header("3D Noise Texture Data");
            bool reloadNoise = false;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(noise3DData, new GUIContent("Binary file", "Binary file holding the 3D Noise texture data (a 3D array). Must be exactly Size * Size * Size bytes long."));
            int newSizeValue = EditorGUILayout.IntField(new GUIContent("Data dimension", "Size (of one dimension) of the 3D Noise data. Must be power of 2. So if the binary file holds a 32x32x32 texture, this value must be 32.")
                , noise3DSize.intValue);
            noise3DSize.intValue = Mathf.Max(2, Mathf.NextPowerOfTwo(newSizeValue));
            if (EditorGUI.EndChangeCheck())
                reloadNoise = true;

            if (Noise3D.isSupported && !Noise3D.isProperlyLoaded)
                EditorGUILayout.HelpBox("Fail to load 3D noise texture", MessageType.Error);

            Header("Volumetric Dust Particles");
            EditorGUILayout.PropertyField(dustParticlesPrefab, new GUIContent("Prefab", "ParticleSystem prefab instantiated for the Volumetric Dust Particles feature (Unity 5.5 or above)"));

            Header("Level 1");
            EditorGUILayout.PropertyField(L1_Texture, new GUIContent("Level 1 LightTexture"));
            EditorGUILayout.PropertyField(L1_Noise, new GUIContent("Level 1 Noise"));
            EditorGUILayout.PropertyField(L1_beamShader, new GUIContent("Level 1 Shader"));


            EditorGUILayout.Space();
            if (GUILayout.Button(new GUIContent("Default values", "Reset properties to their default values."), EditorStyles.miniButton))
            {
                UnityEditor.Undo.RecordObject(target, "Reset Config Properties");
                (target as Config).Reset();
            }

            serializedObject.ApplyModifiedProperties();

            if(reloadNoise)
                Noise3D._EditorForceReloadData(); // Should be called AFTER ApplyModifiedProperties so the Config instance has the proper values when reloading data
        }
    }
}
#endif
