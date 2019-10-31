﻿#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace VLB
{
    [CustomEditor(typeof(DynamicOcclusion))]
    [CanEditMultipleObjects]
    public class DynamicOcclusionEditor : EditorCommon
    {
        SerializedProperty layerMask, minOccluderArea, planeAlignment, planeOffset, waitFrameCount;

        public override bool RequiresConstantRepaint() { return Application.isPlaying || DynamicOcclusion.editorRaycastAtEachFrame; }

        protected override void OnEnable()
        {
            base.OnEnable();
            DynamicOcclusion.EditorLoadPrefs();

            layerMask = FindProperty((DynamicOcclusion x) => x.layerMask);
            minOccluderArea = FindProperty((DynamicOcclusion x) => x.minOccluderArea);
            planeAlignment = FindProperty((DynamicOcclusion x) => x.planeAlignment);
            planeOffset = FindProperty((DynamicOcclusion x) => x.planeOffset);
            waitFrameCount = FindProperty((DynamicOcclusion x) => x.waitFrameCount);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            Header("Raycasting");
            EditorGUILayout.PropertyField(layerMask, new GUIContent("Layer Mask", "On which layers the beam will perform raycasts to check for colliders.\nTry to set it as restrictive as possible (checking only the layers which are necessary) to perform more efficient raycasts in order to increase the performance."));
            EditorGUILayout.PropertyField(minOccluderArea, new GUIContent("Min Occluder Area", "Minimum 'area' of the collider to become an occluder.\nColliders smaller than this value will not block the beam."));
            EditorGUILayout.PropertyField(waitFrameCount, new GUIContent("Wait frame count", "How many frames we wait between 2 occlusion tests?\nIf you want your beam to be super responsive to the changes of your environment, update it every frame but setting 1.\nIf you want to save on performance, we recommend to wait few frames between each update by setting a higher value."));

            Header("Clipping Plane");
            EditorGUILayout.PropertyField(planeAlignment, new GUIContent("Alignment", "Alignment of the computed clipping plane:\n- Surface: align to the surface normal which blocks the beam. Works better for large occluders such as floors and walls.\n- Beam: keep the plane aligned with the beam direction. Works better with more complex occluders or with corners."));
            EditorGUILayout.PropertyField(planeOffset, new GUIContent("Offset Units", "Translate the plane. We recommend to set a small positive offset in order to handle non-flat surface better."));

            Header("Editor Debug");
            EditorGUI.BeginChangeCheck();
            DynamicOcclusion.editorShowDebugPlane = EditorGUILayout.Toggle(new GUIContent("Show Debug Plane", "Draw debug plane on the scene view."), DynamicOcclusion.editorShowDebugPlane);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool("VLB_DYNOCCLUSION_SHOWDEBUGPLANE", DynamicOcclusion.editorShowDebugPlane);
                SceneView.RepaintAll();
            }

            EditorGUI.BeginChangeCheck();
            DynamicOcclusion.editorRaycastAtEachFrame = EditorGUILayout.Toggle(new GUIContent("Update in Editor", "Perform occlusion tests and raycasts in Editor."), DynamicOcclusion.editorRaycastAtEachFrame);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool("VLB_DYNOCCLUSION_RAYCASTINEDITOR", DynamicOcclusion.editorRaycastAtEachFrame);
                SceneView.RepaintAll();
            }

            if (Application.isPlaying || DynamicOcclusion.editorRaycastAtEachFrame)
            {
                if (!serializedObject.isEditingMultipleObjects)
                {
                    var instance = (target as DynamicOcclusion);
                    Debug.Assert(instance);
                    var occluder = instance.editorDebugData.currentOccluder;
                    var lastFrameUpdate = instance.editorDebugData.lastFrameUpdate;

                    var occluderInfo = string.Format("Last update {0} frame(s) ago\n", Time.frameCount - lastFrameUpdate);
                    occluderInfo += occluder ? string.Format("Current occluder: '{0}'\nEstimated occluder area: {1} units²", occluder.name, occluder.bounds.GetMaxArea2D()) : "No occluder found";
                    EditorGUILayout.HelpBox(occluderInfo, MessageType.Info);
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
