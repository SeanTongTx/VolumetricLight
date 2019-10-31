using UnityEngine;
using System.Collections;

namespace VLB
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(VolumetricLightBeam))]
    [HelpURL(Consts.HelpUrlDynamicOcclusion)]
    public class DynamicOcclusion : MonoBehaviour
    {
        /// <summary>
        /// On which layers the beam will perform raycasts to check for colliders.
        /// Setting less layers means higher performances.
        /// </summary>
        public LayerMask layerMask = -1;

        /// <summary>
        /// Minimum 'area' of the collider to become an occluder.
        /// Colliders smaller than this value will not block the beam.
        /// </summary>
        public float minOccluderArea = 0f;

        /// <summary>
        /// How many frames we wait between 2 occlusion tests?
        /// If you want your beam to be super responsive to the changes of your environement, update it every frame by setting 1.
        /// If you want to save on performance, we recommend to wait few frames between each update by setting a higher value.
        /// </summary>
        public int waitFrameCount = 3;

        public enum PlaneAlignment
        {
            /// <summary>Align the plane to the surface normal which blocks the beam. Works better for large occluders such as floors and walls.</summary>
            Surface,
            /// <summary>Keep the plane aligned with the beam direction. Workds better with more complex occluders or with corners.</summary>
            Beam
        }

        /// <summary>
        /// Alignment of the computed clipping plane:
        /// </summary>
        public PlaneAlignment planeAlignment = PlaneAlignment.Surface;

        /// <summary>
        /// Translate the plane. We recommend to set a small positive offset in order to handle non-flat surface better.
        /// </summary>
        public float planeOffset = 0.1f;


        VolumetricLightBeam m_Master = null;
        int m_FrameCountToWait = 0;

#if UNITY_EDITOR
        public struct EditorDebugData
        {
            public Collider currentOccluder;
            public int lastFrameUpdate;
        }
        public EditorDebugData editorDebugData;

        public static bool editorShowDebugPlane = true;
        public static bool editorRaycastAtEachFrame = true;
        private static bool editorPrefsLoaded = false;

        public static void EditorLoadPrefs()
        {
            if (!editorPrefsLoaded)
            {
                editorShowDebugPlane = UnityEditor.EditorPrefs.GetBool("VLB_DYNOCCLUSION_SHOWDEBUGPLANE", true);
                editorRaycastAtEachFrame = UnityEditor.EditorPrefs.GetBool("VLB_DYNOCCLUSION_RAYCASTINEDITOR", true);
                editorPrefsLoaded = true;
            }
        }
#endif

        void OnValidate()
        {
            minOccluderArea = Mathf.Max(minOccluderArea, 0f);
            waitFrameCount = Mathf.Clamp(waitFrameCount, 1, 60);
        }

        void OnEnable()
        {
            m_Master = GetComponent<VolumetricLightBeam>();
            Debug.Assert(m_Master);

#if UNITY_EDITOR
            EditorLoadPrefs();
            editorDebugData.currentOccluder = null;
            editorDebugData.lastFrameUpdate = 0;
#endif
        }

        void OnDisable()
        {
            SetHitNull();
        }

        void LateUpdate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // In Editor, process raycasts at each frame update
                if (!editorRaycastAtEachFrame)
                    SetHitNull();
                else
                    ProcessRaycasts();
            }
            else
#endif
            {
                if (m_FrameCountToWait <= 0)
                {
                    ProcessRaycasts();
                    m_FrameCountToWait = waitFrameCount;
                }
                m_FrameCountToWait--;
            }
        }
        
        Vector3 GetRandomVectorAround(Vector3 direction, float angleDiff)
        {
            var halfAngle = angleDiff * 0.5f;
            return Quaternion.Euler(Random.Range(-halfAngle, halfAngle), Random.Range(-halfAngle, halfAngle), Random.Range(-halfAngle, halfAngle)) * direction;
        }

        RaycastHit GetBestHit()
        {
            var forward = transform.forward;
            var hits = Physics.RaycastAll(transform.position, forward, m_Master.fadeEnd, layerMask.value);

            int bestHit = -1;
            float bestLength = float.MaxValue;
            for (int i = 0; i < hits.Length; ++i)
            {
                if (hits[i].collider.bounds.GetMaxArea2D() >= minOccluderArea)
                {
                    if (hits[i].distance < bestLength)
                    {
                        bestLength = hits[i].distance;
                        bestHit = i;
                    }
                }
            }

            if (bestHit != -1)
                return hits[bestHit];
            else
                return new RaycastHit();
        }


        void ProcessRaycasts()
        {
#if UNITY_EDITOR
            editorDebugData.lastFrameUpdate = Time.frameCount;
#endif
            var bestHit = GetBestHit();

            if (bestHit.collider)
                SetHit(bestHit);
            else
                SetHitNull();
        }

        void SetHit(RaycastHit hit)
        {
            switch (planeAlignment)
            {
            case PlaneAlignment.Beam:
                SetClippingPlane(new Plane(-transform.forward, hit.point));
                break;
            case PlaneAlignment.Surface:
            default:
                SetClippingPlane(new Plane(hit.normal, hit.point));
                break;
            }

#if UNITY_EDITOR
            editorDebugData.currentOccluder = hit.collider;
#endif
        }

        void SetHitNull()
        {
            SetClippingPlaneOff();
#if UNITY_EDITOR
            editorDebugData.currentOccluder = null;
#endif
        }

        void SetClippingPlane(Plane planeWS)
        {
            planeWS = planeWS.TranslateCustom(planeWS.normal * planeOffset);
            m_Master.SetClippingPlane(planeWS);
#if UNITY_EDITOR
            SetDebugPlane(planeWS);
#endif
        }

        void SetClippingPlaneOff()
        {
            m_Master.SetClippingPlaneOff();
#if UNITY_EDITOR
            SetDebugPlane(new Plane());
#endif
        }

#if UNITY_EDITOR
        Vector3 m_EditorPlaneNormal;
        float m_EditorPlaneDistance;

        void SetDebugPlane(Plane planeWS)
        {
            m_EditorPlaneNormal = planeWS.normal;
            if (m_EditorPlaneNormal.sqrMagnitude > 0.5f)
            {
                float dist;
                if (planeWS.Raycast(new Ray(transform.position, transform.forward), out dist))
                    m_EditorPlaneDistance = dist;
            }
        }

        void OnDrawGizmos()
        {
            if (!editorShowDebugPlane)
                return;

            if (m_EditorPlaneNormal.sqrMagnitude > 0.5f)
                Utils.GizmosDrawPlane(
                    m_EditorPlaneNormal,
                    transform.position + m_EditorPlaneDistance * transform.forward,
                    m_Master.color.Opaque(),
                    Mathf.Lerp(m_Master.coneRadiusStart, m_Master.coneRadiusEnd, Mathf.InverseLerp(0f, m_Master.fadeEnd, m_EditorPlaneDistance)));
        }
#endif
    }
}
