
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#pragma warning disable 0429, 0162 // Unreachable expression code detected (because of Noise3D.isSupported on mobile)

namespace VLB
{
    [CustomEditor(typeof(VolumetricLightBeam))]
    [CanEditMultipleObjects]
    public class VolumetricLightBeamEditor : EditorCommon
    {
        SerializedProperty lod;

        SerializedProperty trackChangesDuringPlaytime;
        SerializedProperty colorFromLight, color, Intensity;
        SerializedProperty alphaInside, alphaOutside;
        SerializedProperty fresnelPow;
        SerializedProperty glareFrontal, glareBehind;
        SerializedProperty spotAngleFromLight, spotAngle;
        SerializedProperty coneRadiusStart, geomSides, geomCap;
        SerializedProperty fadeEndFromLight, fadeStart, fadeEnd;
        SerializedProperty attenuationEquation, attenuationCustomBlending;
        SerializedProperty depthBlendDistance, cameraClippingDistance;
        SerializedProperty OverrideTexture;
        SerializedProperty QuadRenderer;

        // NOISE
        SerializedProperty noiseEnabled, noiseIntensity, noiseScaleUseGlobal, noiseScaleLocal, noiseVelocityUseGlobal, noiseVelocityLocal;

        List<VolumetricLightBeam> m_Entities;
        static bool ms_ShowTips = true;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_Entities = new List<VolumetricLightBeam>();
            foreach (var ent in targets)
            {
                if (ent is VolumetricLightBeam)
                    m_Entities.Add(ent as VolumetricLightBeam);
            }
            Debug.Assert(m_Entities.Count > 0);
            lod = FindProperty((VolumetricLightBeam x) => x.lod);
            color = FindProperty((VolumetricLightBeam x) => x.color);
            Intensity = FindProperty((VolumetricLightBeam x) => x.Intensity);
            alphaInside = FindProperty((VolumetricLightBeam x) => x.alphaInside);
            alphaOutside = FindProperty((VolumetricLightBeam x) => x.alphaOutside);

            fresnelPow = FindProperty((VolumetricLightBeam x) => x.fresnelPow);

            glareFrontal = FindProperty((VolumetricLightBeam x) => x.glareFrontal);
            glareBehind = FindProperty((VolumetricLightBeam x) => x.glareBehind);

            spotAngle = FindProperty((VolumetricLightBeam x) => x.spotAngle);

            coneRadiusStart = FindProperty((VolumetricLightBeam x) => x.coneRadiusStart);

            geomSides = FindProperty((VolumetricLightBeam x) => x.geomSides);

            fadeStart = FindProperty((VolumetricLightBeam x) => x.fadeStart);
            fadeEnd = FindProperty((VolumetricLightBeam x) => x.fadeEnd);

            attenuationEquation = FindProperty((VolumetricLightBeam x) => x.attenuationEquation);
            attenuationCustomBlending = FindProperty((VolumetricLightBeam x) => x.attenuationCustomBlending);

            depthBlendDistance = FindProperty((VolumetricLightBeam x) => x.depthBlendDistance);
            cameraClippingDistance = FindProperty((VolumetricLightBeam x) => x.cameraClippingDistance);

            // NOISE
            noiseEnabled = FindProperty((VolumetricLightBeam x) => x.noiseEnabled);
            noiseIntensity = FindProperty((VolumetricLightBeam x) => x.noiseIntensity);
            noiseScaleUseGlobal = FindProperty((VolumetricLightBeam x) => x.noiseScaleUseGlobal);
            noiseScaleLocal = FindProperty((VolumetricLightBeam x) => x.noiseScaleLocal);
            noiseVelocityUseGlobal = FindProperty((VolumetricLightBeam x) => x.noiseVelocityUseGlobal);
            noiseVelocityLocal = FindProperty((VolumetricLightBeam x) => x.noiseVelocityLocal);
            //lod level
            //OverrideTexture
            OverrideTexture = FindProperty((VolumetricLightBeam x) => x.OverrideTexture);
            //Quad
            QuadRenderer = FindProperty((VolumetricLightBeam x) => x.QuadRenderer);

            trackChangesDuringPlaytime = serializedObject.FindProperty("_TrackChangesDuringPlaytime");
        }

        static void PropertyThickness(SerializedProperty sp)
        {
            sp.FloatSlider(
                new GUIContent("Side Thickness", "Thickness of the beam when looking at it from the side.\n1 = the beam is fully visible (no difference between the center and the edges), but produces hard edges.\nLower values produce softer transition at beam edges."),
                0, 1,
                (value) => Mathf.Clamp01(1 - (value / Consts.FresnelPowMaxValue)),    // conversion value to slider
                (value) => (1 - value) * Consts.FresnelPowMaxValue                    // conversion slider to value
                );
        }


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            Debug.Assert(m_Entities.Count > 0);
            EditorGUILayout.PropertyField(lod);

            bool hasLightSpot = false;
            var light = m_Entities[0].GetComponent<Light>();
            if (light)
            {
                hasLightSpot = light.type == LightType.Spot;
                if (!hasLightSpot)
                {
                    EditorGUILayout.HelpBox("To bind properties from the Light and the Beam together, this component must be attached to a Light of type 'Spot'", MessageType.Warning);
                }
            }

            Header("Basic");
            if (hasLightSpot)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(spotAngleFromLight.boolValue);
            }
            EditorGUILayout.PropertyField(spotAngle, new GUIContent("Spot Angle", "Define the angle (in degrees) at the base of the beam's cone"));
            if (hasLightSpot)
            {
                EditorGUI.EndDisabledGroup();
                spotAngleFromLight.ToggleFromLight();
                EditorGUILayout.EndHorizontal();
            }

            if (hasLightSpot)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(colorFromLight.boolValue);
            }
            EditorGUILayout.PropertyField(color, new GUIContent("Color", "Use the color picker to set the RGBA color of the beam (takes account of the alpha value)."));
            if (hasLightSpot)
            {
                EditorGUI.EndDisabledGroup();
                colorFromLight.ToggleFromLight();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.PropertyField(Intensity);

            PropertyThickness(fresnelPow);

            trackChangesDuringPlaytime.ToggleLeft(
                new GUIContent(
                        "Track changes during Playtime",
                        "Check this box to be able to modify properties during Playtime via Script, Animator and/or Timeline.\nEnabling this feature is at very minor performance cost. So keep it disabled if you don't plan to modify this light beam during playtime.")
                );
            DrawAnimatorWarning();

            Header("Inside");
            EditorGUILayout.PropertyField(alphaInside, new GUIContent("Alpha", "Modulate the opacity of the inside geometry of the beam. Is multiplied to Color's alpha."));
            EditorGUILayout.PropertyField(glareFrontal, new GUIContent("Glare (frontal)", "Boost intensity factor when looking at the beam from the inside directly at the source."));

            Header("Outside");
            EditorGUILayout.PropertyField(alphaOutside, new GUIContent("Alpha", "Modulate the opacity of the outside geometry of the beam. Is multiplied to Color's alpha."));
            EditorGUILayout.PropertyField(glareBehind, new GUIContent("Glare (from behind)", "Boost intensity factor when looking at the beam from behind."));


            Header("Attenuation");
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PropertyField(attenuationEquation, new GUIContent("Equation", "Attenuation equation used to compute fading between 'Fade Start Distance' and 'Range Distance'.\n- Linear: Simple linear attenuation\n- Quadratic: Quadratic attenuation, which usually gives more realistic results\n- Blend: Custom blending mix between linear (0.0) and quadratic attenuation (1.0)"));
                if (attenuationEquation.enumValueIndex == (int)VolumetricLightBeam.AttenuationEquation.Blend)
                    EditorGUILayout.PropertyField(attenuationCustomBlending, new GUIContent("", "Blending value between Linear (0.0) and Quadratic (1.0) attenuation equations."));
            }
            EditorGUILayout.EndHorizontal();

            if (hasLightSpot)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(fadeEndFromLight.boolValue);
            }
            EditorGUILayout.PropertyField(fadeEnd, new GUIContent("Range Distance", "Distance from the light source (in units) the beam is entirely faded out"));
            if (hasLightSpot)
            {
                EditorGUI.EndDisabledGroup();
                fadeEndFromLight.ToggleFromLight();
                EditorGUILayout.EndHorizontal();
            }

            var fadeEndGUI = new GUIContent("Fade Start Distance", "Distance from the light source (in units) the beam intensity will start to fall off.");
            if (fadeEnd.hasMultipleDifferentValues)
                EditorGUILayout.PropertyField(fadeStart, fadeEndGUI);
            else
                fadeStart.FloatSlider(fadeEndGUI, 0f, fadeEnd.floatValue - Consts.FadeMinThreshold);

            Header("3D Noise");
            EditorGUILayout.PropertyField(noiseEnabled, new GUIContent("Enabled", "Enable 3D Noise effect"));

            if (noiseEnabled.boolValue)
            {
                EditorGUILayout.PropertyField(noiseIntensity, new GUIContent("Intensity", "Higher intensity means the noise contribution is stronger and more visible"));

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledGroupScope(noiseScaleUseGlobal.boolValue))
                    {
                        EditorGUILayout.PropertyField(noiseScaleLocal, new GUIContent("Scale", "3D Noise texture scaling: higher scale make the noise more visible, but potentially less realistic"));
                    }
                    noiseScaleUseGlobal.ToggleUseGlobalNoise();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledGroupScope(noiseVelocityUseGlobal.boolValue))
                    {
                        EditorGUILayout.PropertyField(noiseVelocityLocal, new GUIContent("Velocity", "World Space direction and speed of the noise scrolling, simulating the fog/smoke movement"));
                    }
                    noiseVelocityUseGlobal.ToggleUseGlobalNoise();
                }

                ButtonOpenConfig();

                if (Noise3D.isSupported && !Noise3D.isProperlyLoaded)
                    EditorGUILayout.HelpBox("Fail to load 3D noise texture. Please check your Config.", MessageType.Error);

                if (!Noise3D.isSupported)
                    EditorGUILayout.HelpBox(Noise3D.isNotSupportedString, MessageType.Info);
            }

            // Header("Soft Intersections Blending Distances");
            // EditorGUILayout.PropertyField(cameraClippingDistance, new GUIContent("Camera", "Distance from the camera the beam will fade.\n0 = hard intersection\nHigher values produce soft intersection when the camera is near the cone triangles."));
            // EditorGUILayout.PropertyField(depthBlendDistance, new GUIContent("Opaque geometry", "Distance from the world geometry the beam will fade.\n0 = hard intersection\nHigher values produce soft intersection when the beam intersects other opaque geometry."));

            Header("Cone Geometry");
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(coneRadiusStart, new GUIContent("Truncated Radius", "Radius (in units) at the beam's source (the top of the cone).\n0 will generate a perfect cone geometry.\nHigher values will generate truncated cones."));
                /* EditorGUI.BeginChangeCheck();
                 {
                     geomCap.ToggleLeft(new GUIContent("Cap Geom", "Generate Cap Geometry (only visible from inside)"), GUILayout.MaxWidth(80.0f));
                 }
                 if (EditorGUI.EndChangeCheck()) { foreach (var entity in m_Entities) entity._EditorSetMeshDirty(); }*/
            }

            /* EditorGUI.BeginChangeCheck();
             {
                 EditorGUILayout.PropertyField(
                     geomSides,
                     new GUIContent("Sides", "Number of Sides of the cone. Higher values give better looking results, but require more memory and graphic performance."));
             }
             if (EditorGUI.EndChangeCheck()) { foreach (var entity in m_Entities) entity._EditorSetMeshDirty(); }
             */
            if (m_Entities.Count == 1)
            {
                EditorGUILayout.HelpBox(m_Entities[0].meshStats, MessageType.Info);
            }

            if ((target as VolumetricLightBeam).lod > 0)
            {
                Header("Lod");
                EditorGUILayout.PropertyField(OverrideTexture);
            }
            Header("Quad");
            EditorGUILayout.PropertyField(QuadRenderer);

            EditorGUILayout.Separator();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Default values", "Reset properties to their default values."), EditorStyles.miniButtonLeft))
                {
                    UnityEditor.Undo.RecordObjects(m_Entities.ToArray(), "Reset Light Beam Properties");
                    foreach (var entity in m_Entities) { entity.Reset(); entity.GenerateGeometry(); }
                }
                /*if (GUILayout.Button(new GUIContent("Regenerate geometry", "Force to re-create the Beam Geometry GameObject."), EditorStyles.miniButtonRight))
                {
                    foreach (var entity in m_Entities) entity.GenerateGeometry();
                }*/
            }

            DrawAdditionalFeatures();
            DrawTips();

            serializedObject.ApplyModifiedProperties();
        }

        void DrawAnimatorWarning()
        {
            var showAnimatorWarning = false;
            foreach (var entity in m_Entities)
            {
                if (entity.GetComponent<Animator>() != null && entity.trackChangesDuringPlaytime == false)
                {
                    showAnimatorWarning = true;
                    break;
                }
            }

            if (showAnimatorWarning)
                EditorGUILayout.HelpBox("If you want to animate your light beam in real-time, you should enable the 'trackChangesDuringPlaytime' property.", MessageType.Warning);
        }

        void DrawAdditionalFeatures()
        {
            bool showFloatingDustButton = false;
            bool showButtonOcclusion = false;
#if UNITY_5_5_OR_NEWER
            foreach (var entity in m_Entities)
                if (entity.GetComponent<VolumetricDustParticles>() == null)
                {
                    showFloatingDustButton = true;
                    break;
                }
#endif
            foreach (var entity in m_Entities)
                if (entity.GetComponent<DynamicOcclusion>() == null)
                {
                    showButtonOcclusion = true;
                    break;
                }

            if (showFloatingDustButton || showButtonOcclusion)
            {
                EditorGUILayout.Separator();
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (showFloatingDustButton && GUILayout.Button(new GUIContent("Add Dust Particles", "Add a 'VolumetricDustParticles' component."), EditorStyles.miniButton))
                    {
                        UnityEditor.Undo.RecordObjects(m_Entities.ToArray(), "Add Floating Dust Particles.");
                        foreach (var entity in m_Entities) { entity.gameObject.AddComponent<VolumetricDustParticles>(); }
                    }

                    if (showButtonOcclusion && GUILayout.Button(new GUIContent("Add Dynamic Occlusion", "Add a 'DynamicOcclusion' component."), EditorStyles.miniButton))
                    {
                        UnityEditor.Undo.RecordObjects(m_Entities.ToArray(), "Add Dynamic Occlusion.");
                        foreach (var entity in m_Entities) { entity.gameObject.AddComponent<DynamicOcclusion>(); }
                    }
                }
            }
        }

        void DrawTips()
        {
            if (m_Entities.Count == 1)
            {
                if (depthBlendDistance.floatValue > 0f || !Noise3D.isSupported || trackChangesDuringPlaytime.boolValue)
                {
                    ms_ShowTips = EditorGUILayout.Foldout(ms_ShowTips, "Infos");
                    if (ms_ShowTips)
                    {
                        if (depthBlendDistance.floatValue > 0f)
                        {
                            EditorGUILayout.HelpBox("To support 'Soft Intersection with Opaque Geometry', your camera must use 'DepthTextureMode.Depth'.", MessageType.Info);
#if UNITY_IPHONE || UNITY_IOS || UNITY_ANDROID
                            EditorGUILayout.HelpBox("On mobile platforms, the depth buffer precision can be pretty low. Try to keep a small depth range on your cameras: the difference between the near and far clip planes should stay as low as possible.", MessageType.Info);
#endif
                        }

                        if (trackChangesDuringPlaytime.boolValue)
                            EditorGUILayout.HelpBox("This beam will keep track of the changes of its own properties and the spotlight attached to it (if any) during playtime. You can modify every properties except 'geomSides'.", MessageType.Info);
                    }
                }
            }
        }
    }
}
#endif
