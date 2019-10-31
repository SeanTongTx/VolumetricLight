using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VLB;

[ExecuteInEditMode]
public class SimpleProjector : MonoBehaviour
{
    public GameObject Prefab;
    public float Distance = 10;
    public float Scale = 1;
    public float Offset=0.01f;
    Ray ray = new Ray();
    RaycastHit hit;
    [HideInInspector]
    public GameObject prefabInstance;

    public VolumetricLightBeam VolumBeam;
    public LayerMask layermask;
    private void OnEnable()
    {
        if (prefabInstance)
        {
            prefabInstance.gameObject.SetActive(true);
        }
        else if (Prefab)
        {
            prefabInstance = GameObject.Instantiate(Prefab);
            prefabInstance.transform.SetParent(this.transform);
        }
    }
    private void OnDestroy()
    {
        if (prefabInstance)
        {
            DestroyImmediate(prefabInstance);
        }
    }
    private void OnDisable()
    {
        if (prefabInstance)
        {
            prefabInstance.gameObject.SetActive(false);
        }
    }
    void Update()
    {
        ray.origin = transform.position;
        ray.direction = transform.forward;
        Physics.Raycast(ray, out hit, Distance, layermask);
        if (prefabInstance && hit.transform)
        {
            Vector3 showPostion = hit.point + new Vector3(0, Offset, 0);
            prefabInstance.transform.position = Vector3.Lerp(showPostion, prefabInstance.transform.position, Time.deltaTime);
            prefabInstance.transform.rotation = Quaternion.identity;
            prefabInstance.transform.localScale = new Vector3(Scale, Scale, Scale);
            if (VolumBeam)
            {
                float deg = VolumBeam.spotAngle;
                float H = hit.distance;
                Scale = Mathf.Tan(Mathf.Deg2Rad * deg / 2f) * H * 2;
                prefabInstance.transform.localScale = new Vector3(Scale, Scale, Scale);
            }

        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * Distance);
        if (hit.transform)
        {
            Gizmos.DrawWireSphere(hit.point, 0.1f);
        }
    }

}
